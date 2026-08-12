using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using XeniaBot.Core.Helpers;
using XeniaBot.MongoData.Helpers;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.LevelSystem.Services;

[XeniaController]
public class LevelSystemService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(LevelSystemService));
    private readonly DiscordSocketClient _client;
    private readonly Random _random;
    private readonly SemaphoreSlim _randomLock = new(1, 1);
    private readonly LevelMemberRepository _memberConfig;
    private readonly LevelSystemConfigRepository _config;
    private readonly ConfigData _configData;
    public LevelSystemService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<LevelSystemConfigRepository>();
        _memberConfig = services.GetRequiredService<LevelMemberRepository>();
        _configData = services.GetRequiredService<ConfigData>();
        _random = new Random();
        _client.MessageReceived += _client_MessageReceived;
        _client.UserJoined += ClientOnUserJoined;
        UserLevelUp += OnUserLevelUp_RoleGrant;
    }

    public override Task OnReadyDelay()
    {
        if (!_configData.RefreshLevelSystemOnStart)
        {
            _log.Info($"Not going to run since {nameof(_configData.RefreshLevelSystemOnStart)} is false");
            return Task.CompletedTask;
        }

        // i dont care about CS4014, i want this method to continue running to not block the event :sob:
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        PerformPostReadyTasks();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return Task.CompletedTask;
    }

    private async Task PerformPostReadyTasks()
    {
        if (!_configData.RefreshLevelSystemOnStart)
        {
            _log.Info($"Not going to run since {nameof(_configData.RefreshLevelSystemOnStart)} is false");
            return;
        }

        try
        {
            foreach (var guild in _client.Guilds)
            {
                try
                {
                    await ReGrantGuildMembers(guild);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Failed to re-grant possibly missed roles in Guild \"{guild.Name}\" ({guild.Id})", e);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to process all guilds");
        }
    }

    private async Task ClientOnUserJoined(SocketGuildUser arg)
    {
        var memberModel = await _memberConfig.Get(arg.Id, arg.Guild.Id);
        if (memberModel == null)
            return;
        
        var metadata =  LevelSystemHelper.Generate(memberModel);
        OnUserLevelUp_RoleGrant(memberModel, metadata, metadata);
    }

    public async Task ReGrantGuildMembers(ulong guildId)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        await ReGrantGuildMembers(guild);
    }

    public async Task ReGrantGuildMembers(SocketGuild? guild)
    {
        if (guild == null) return;
        var exList = new List<Exception>();
        var taskList = new List<Task>();
        foreach (var member in guild.Users)
        {
            var memberValue = member;
            if (memberValue is { IsBot: false })
            {
                taskList.Add(new Task(delegate
                {
                    try
                    {
                        ClientOnUserJoined(memberValue).GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        exList.Add(new InvalidOperationException(
                            $"Failed to check regrant for member \"{member.FormatUsername()}\" in guild \"{guild.Name}\" (userId={member.Id}, guildId={guild.Id})",
                            e));
                    }
                }));
            }
        }
        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
        if (exList.Count > 0)
            throw new AggregateException($"Failed to process one or more members in guild \"{guild.Name}\" ({guild.Id})", exList);
    }

    /// <summary>
    /// Grant a member a role if they reach a level milestone. This can be defined in the dashboard.
    /// </summary>
    public async void OnUserLevelUp_RoleGrant(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current)
    {
        try
        {
            await PerformRoleGrantInternal(model, previous, current);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to run with user: {model.UserId} and guild {model.GuildId}");
        }
    }

    private async Task PerformRoleGrantInternal(
        LevelMemberModel model,
        ExperienceMetadata metaBefore,
        ExperienceMetadata metaAfter)
    {
        var gmodel = await _config.Get(model.GuildId);
        if (gmodel == null)
            return;

        var roleGrantList = gmodel?.RoleGrant ?? [];
        if (roleGrantList.Count < 1) return;
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(model.GuildId));
        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(model.UserId));
        var memberRoles = member.Roles.Select(e => e.Id).Distinct().ToArray();
        foreach (var item in roleGrantList.Where(e => !memberRoles.Contains(e.RoleId)))
        {
            if (metaAfter.UserLevel < item.RequiredLevel) continue;
            try
            {
                var role = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(item.RoleId));
                var addOpts = new RequestOptions()
                {
                    AuditLogReason = $"XP Level Up (req lvl: {item.RequiredLevel})"
                };
                await ExceptionHelper.RetryOnTimedOut(async () => await member.AddRoleAsync(role, addOpts));
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to grant Role {item.RoleId} to member {model.UserId} in guild {model.GuildId}");
                await DiscordHelper.ReportError(
                    ex,
                    $"Failed to grant Role {item.RoleId} to member {model.UserId} in guild {model.GuildId}");
            }
        }
    }

    private async Task ClientMessageReceived(SocketMessage rawMessage)
    {
        // Ignore messages from bots & webhooks
        if (rawMessage.Author.IsBot || rawMessage.Author.IsWebhook)
            return;
        
        // ensures we don't process system/other bot messages
        if (rawMessage is not SocketUserMessage message) return;
        
        var context = new SocketCommandContext(_client, message);
        if (context.Guild == null) return;
        
        var data = await _memberConfig.Get(message.Author.Id, context.Guild.Id);
        if (data == null)
        {
            data = new LevelMemberModel()
            {
                UserId = message.Author.Id,
                GuildId = context.Guild.Id
            };
            await _memberConfig.Set(data);
        }
        
        var guildConfig = await _config.Get(context.Guild.Id)
            ?? new LevelSystemConfigModel()
            {
                GuildId = context.Guild.Id
            };
        if (!guildConfig.Enable)
            return;

        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var previousMessageDiff = currentTimestamp - data.LastMessageTimestamp;
        if (previousMessageDiff >= 8000)
        {
            try
            {
                var result = await GrantXp(data, message);
                var targetChannel = message.Channel;
                if (guildConfig.LevelUpChannel != null)
                {
                    var tc = context.Guild.GetTextChannel((ulong)guildConfig.LevelUpChannel);
                    if (tc != null)
                        targetChannel = tc;
                }

                if (!guildConfig.ShowLeveUpMessage) targetChannel = null;
                
                if (result.DidLevelUp && targetChannel != null)
                {
                    var text = $"<@{message.Author.Id}> You've advanced to *level {result.Metadata.UserLevel}*!";
                    await ExceptionHelper.RetryOnTimedOut(async () => await targetChannel.SendMessageAsync(text));
                }
            }
            catch (Exception e)
            {
                await DiscordHelper.ReportError(e, context);
            }
        }
    }
    private Task _client_MessageReceived(SocketMessage rawMessage)
    {
        ThreadPool.QueueUserWorkItem(ThreadProcessMessageReceived, rawMessage);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Thread logic for when <see cref="_client_MessageReceived"/> is invoked
    /// </summary>
    /// <param name="state">Must be <see cref="SocketMessage"/> or this does nothing</param>
    private void ThreadProcessMessageReceived(object? state)
    {
        if (state is not SocketMessage message) return;
        try
        {
            ClientMessageReceived(message).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to process message (id={message.Id}, author={message.Author.Id},{message.Author.Username})");
        }
    }

    /// <summary>
    /// Result data for <see cref="GrantXp"/>
    /// </summary>
    public class GrantXpResult
    {
        /// <summary>
        /// Did this cause the user to level up
        /// </summary>
        public bool DidLevelUp { get; init; }
        /// <summary>
        /// New XP Metadata
        /// </summary>
        public ExperienceMetadata Metadata { get; init; } = new();
    }
    
    /// <summary>
    /// Grant user a random amount of xp between <see cref="RandomXpMin"/> and <see cref="RandomXpMax"/>
    /// </summary>
    /// <param name="model">User XP Data</param>
    /// <param name="message">Message that triggered this event</param>
    /// <returns>Result information. See <see cref="GrantXpResult"/></returns>
    public async Task<GrantXpResult> GrantXp(LevelMemberModel model, SocketUserMessage message)
    {
        var data = await _memberConfig.Get(model.UserId, model.GuildId)
            ?? model;
        int amount;
        await _randomLock.WaitAsync();
        try
        {
            amount = _random.Next(RandomXpMin, RandomXpMax);
        }
        finally
        {
            _randomLock.Release();
        }

        // Generate previous and current metadata
        var metadataPrevious = LevelSystemHelper.Generate(data);
        data.Xp += Convert.ToUInt32(Math.Max(0, amount));
        var metadata = LevelSystemHelper.Generate(data);

        // Set previous Ids
        data.LastMessageChannelId = message.Channel.Id;
        data.LastMessageId = message.Id;
        await _memberConfig.Set(data);
        
        var levelUp = metadataPrevious.UserLevel < metadata.UserLevel;
        if (levelUp)
        {
            try
            {
                OnUserLevelUp(data, metadataPrevious, metadata);
            }
            catch (Exception e)
            {
                _log.Warn(e, $"Failed to invoke event {nameof(UserLevelUp)} (userId={model.UserId}, guildId={model.GuildId})");
            }
        }

        await _memberConfig.Set(data);
        return new GrantXpResult
        {
            DidLevelUp = levelUp,
            Metadata = metadata
        };
    }

    private const int RandomXpMin = 4;
    private const int RandomXpMax = 16;
    
    private void OnUserLevelUp(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current)
    {
        UserLevelUp?.Invoke(model, previous, current);
    }
    public event ExperienceComparisonDelegate UserLevelUp;
}
