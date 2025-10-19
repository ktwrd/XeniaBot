using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Confession;

namespace XeniaDiscord.Common.Services;

public class ConfessionService : IConfessionService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    private readonly IDiscordClient _client;
    public ConfessionService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _client = services.GetRequiredService<IDiscordClient>();
    }


    public async Task<EmbedBuilder> CreateAsync(IGuild pGuild, IUser user, string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length < 10)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription("Confession content is required. (min 10 chars)")
                .WithColor(Color.Red);
        }
        if (content.Length > DbGlobals.MaxLength.Confession)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Content is too long! (max: {DbGlobals.MaxLength.Confession})")
                .WithColor(Color.Red);
        }

        var guildIdStr = pGuild.Id.ToString();
        var guildRecord = await _db.GuildConfessionConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == guildIdStr);
        if (guildRecord == null)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Confession Module has not been setup on this server.")
                .WithColor(Color.Red);
        }

        var outputChannelId = guildRecord.GetOutputChannelId();
        if (outputChannelId == null || outputChannelId < 10)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Confession Module has not been setup **properly** on this server. (invalid output channel)")
                .WithColor(Color.Red);
        }

        var guild = await _client.GetGuildAsync(pGuild.Id);
        if (guild == null)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"This is really weird. I couldn't find the guild you're requesting this from...")
                .WithColor(Color.Red);
        }

        var outputChannel = await guild.GetTextChannelAsync(outputChannelId.Value);
        if (outputChannel == null)
        {
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Could not find output channel `{outputChannelId.Value}`")
                .WithColor(Color.Red);
        }

        var logErrorAppend =
            $"from user \"{user.GlobalName}\" ({user.Username}, {user.Id}) to channel \"{outputChannel.Name}\" ({outputChannel.Id}) in guild \"{guild.Name}\" ({guild.Id})";

        try
        {
            await outputChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Confession")
                .WithDescription(content)
                .WithCurrentTimestamp()
                .WithColor(new Color(255, 255, 255))
                .Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to send confession {logErrorAppend}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra("author.global_name", user.GlobalName);
                scope.SetExtra("author.username", user.Username);
                scope.SetExtra("author.id", user.Id);
                scope.SetExtra("channel.id", outputChannel.Id);
                scope.SetExtra("channel.name", outputChannel.Name);
                scope.SetExtra("guild.id", guild.Id);
                scope.SetExtra("guild.name", guild.Name);
            });
            return new EmbedBuilder()
                .WithTitle("Create Confession - Error")
                .WithDescription($"Failed to send message in <#{outputChannel.Id}>")
                .AddField("Error Message", ex.Message.Substring(0, Math.Min(ex.Message.Length, 1900)))
                .WithColor(Color.Red);
        }


        await using (var ctx = _db.CreateSession())
        {
            await using var transaction = await ctx.Database.BeginTransactionAsync();
            try
            {
                var model = new GuildConfessionModel()
                {
                    GuildId = guild.Id.ToString(),
                    GuildConfessionConfigId = guildRecord.Id,
                    Content = content,
                    CreatedByUserId = user.Id.ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                await ctx.GuildConfessions.AddAsync(model);
                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to store confession in database {logErrorAppend}");
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra("author.global_name", user.GlobalName);
                    scope.SetExtra("author.username", user.Username);
                    scope.SetExtra("author.id", user.Id);
                    scope.SetExtra("channel.id", outputChannel.Id);
                    scope.SetExtra("channel.name", outputChannel.Name);
                    scope.SetExtra("guild.id", guild.Id);
                    scope.SetExtra("guild.name", guild.Name);
                });
                await transaction.RollbackAsync();
            }
        }

        return new EmbedBuilder()
            .WithTitle("Created Confession")
            .WithDescription("Successfully created confession.")
            .WithColor(Color.Green);
    }

    public async Task<EmbedBuilder> SetOutputChannelAsync(IGuild guild, ITextChannel channel, IUser? createdByUser)
    {
        try
        {
            await using var ctx = _db.CreateSession();
            await using var transaction = await ctx.Database.BeginTransactionAsync();

            var guildIdStr = guild.Id.ToString();
            var guildRecord = await ctx.GuildConfessionConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == guildIdStr);
            guildRecord ??= new GuildConfessionConfigModel()
            {
                Id = guildIdStr,
                OutputChannelId = channel.Id.ToString(),
                CreatedByUserId = createdByUser?.Id.ToString() ?? "0"
            };

            try
            {
                if (await ctx.GuildConfessionConfigs.AnyAsync(e => e.Id == guildIdStr))
                {
                    await ctx.GuildConfessionConfigs.Where(e => e.Id == guildIdStr)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.OutputChannelId, guildRecord.OutputChannelId));
                }
                else
                {
                    await ctx.GuildConfessionConfigs.AddAsync(guildRecord);
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update record in database.");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra("channel.id", channel.Id);
                scope.SetExtra("channel.name", channel.Name);
                scope.SetExtra("guild.id", guild.Id);
                scope.SetExtra("guild.name", guild.Name);
            });
            return new EmbedBuilder()
                .WithTitle("Confession Admin - Set Channel - Error")
                .WithDescription("```\n" + ex.Message.Substring(0, Math.Min(ex.Message.Length, 1900)) + "\n```")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
        }

        return new EmbedBuilder()
            .WithTitle("Confession Admin - Set Channel")
            .WithDescription($"Updated output channel to: <#{channel.Id}>")
            .WithColor(Color.Green)
            .WithCurrentTimestamp();
    }
    public Task<EmbedBuilder> SetOutputChannelAsync(IGuild guild, ITextChannel channel)
    {
        return SetOutputChannelAsync(guild, channel, null);
    }

    public async Task<GuildConfessionConfigModel> GetOrCreateGuildConfig(IGuild guild, IUser? createdByUser)
    {
        var guildIdStr = guild.Id.ToString();
        await using var ctx = _db.CreateSession();
        var model = await ctx.GuildConfessionConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == guildIdStr);
        if (model != null)
            return model;
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            model ??= new()
            {
                Id = guildIdStr,
                CreatedByUserId = createdByUser?.Id.ToString() ?? "0"
            };
            await ctx.GuildConfessionConfigs.AddAsync(model);
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            return model;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public Task<GuildConfessionConfigModel> GetOrCreateGuildConfig(IGuild guild)
    {
        return GetOrCreateGuildConfig(guild, null);
    }

    public async Task<IUserMessage> SendModalMessage(GuildConfessionConfigModel configModel, IGuild guild, ITextChannel channel)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Confessions")
            .WithDescription($"Send an anonymous confession into: <#{configModel.OutputChannelId}>");
        var component = new ComponentBuilder()
            .WithButton(
                "Create",
                InteractionIdentifier.ConfessionModalCreate,
                ButtonStyle.Primary);

        var message = await channel.SendMessageAsync(
            embed: embed.Build(),
            components: component.Build());

        using (var ctx = _db.CreateSession())
        {
            await using var transaction = await ctx.Database.BeginTransactionAsync();
            try
            {
                await DeleteButtonMessage(configModel, guild);

                configModel.ButtonMessageId = message.Id.ToString();
                configModel.ButtonChannelId = channel.Id.ToString();

                await ctx.GuildConfessionConfigs.Where(e => e.Id == configModel.Id)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(p => p.ButtonMessageId, configModel.ButtonMessageId)
                        .SetProperty(p => p.ButtonChannelId, configModel.ButtonChannelId));
                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return message;
    }
    private async Task DeleteButtonMessage(GuildConfessionConfigModel configModel, IGuild guild)
    {
        try
        {
            var btnChannelId = configModel.GetButtonChannelId();
            var btnMessageId = configModel.GetButtonMessageId();
            if (btnChannelId != null && btnMessageId != null)
            {
                var btnChannel = await guild.GetTextChannelAsync(btnChannelId.Value);
                if (btnChannel != null)
                {
                    var msg = await btnChannel.GetMessageAsync(btnMessageId.Value);
                    if (msg != null)
                    {
                        await msg.DeleteAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to delete previous confession button message (msg id: {configModel.ButtonMessageId}, cha id: {configModel.ButtonChannelId}, guild name: \"{guild.Name}\", guild id: {guild.Id})");
        }
    }
}
