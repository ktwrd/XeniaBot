using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Interactions.Modules.Admin;

[Group("adm-data", "Admin: Data administration")]
[DeveloperModule]
[CommandContextType(InteractionContextType.Guild)]
[RequireDeveloper]
[UsedImplicitly]
public partial class AdmDataModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly DiscordSnapshotService _snapshotService;
    private readonly DiscordCacheService _discordCacheService;
    private readonly DiscordSocketClient _client;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IServiceProvider _services;
    public AdmDataModule(IServiceProvider services)
    {
        _services = services;
        _config = services.GetRequiredService<ConfigData>();
        _snapshotService = services.GetRequiredService<DiscordSnapshotService>();
        _discordCacheService = services.GetRequiredService<DiscordCacheService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
    }

    private async Task FollowUpWithException(Exception exception, string message = "Failed to update database.")
    {
        await FollowupWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(exception.ToString())),
                "exception.txt",
                message);
    }

    [SlashCommand("guild", "Update data for specific guild")]
    [UsedImplicitly]
    public async Task UpdateGuild(
        [Summary(description: "Guild ID. Xenia must be a member")]
        string guildId,
        [Summary(description: DescriptionIncludeCache)]
        bool cache = true,
        [Summary(description: DescriptionIncludeMemberCache)]
        bool cacheMember = false,
        [Summary(description: DescriptionIncludeSnapshots)]
        bool snapshot = false)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        if (!ulong.TryParse(guildId, out var guildIdValue))
        {
            await RespondAsync($"Could not parse the provided Guild Id\n{guildId}");
            return;
        }

        if (!cache && !cacheMember && !snapshot)
        {
            await RespondAsync("At least one option must be enabled!");
            return;
        }

        await DeferAsync();
        IGuild? guild = null;
        try
        {
            guild = await ExceptionHelper.RetryOnTimedOut(async () => await Context.Client.GetGuildAsync(guildIdValue));
            if (guild == null)
            {
                await Context.Interaction.FollowupAsync($"Could not find guild: `{guildId}`\n" +
                    "-# I might not be a member of it anymore.");
                return;
            }

            var now = DateTime.UtcNow;
            var elapsed = await UpdateGuildTask(guild, now, new UpdateGuildTaskFlags(cache, cacheMember, snapshot));

            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var name = guild.Name.Replace("`", "'");
            await Context.Interaction.FollowupAsync($"Updated Guild `{name}` (`{guild.Id}`)\nTook: {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Guild \"{guild?.Name}\" ({guildId})");
            await FollowUpWithException(ex, $"Failed to update guild: `{guildId}`");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                $"Failed to update guild: `{guildId}`");
        }
    }

    [SlashCommand("guild-current", "Update data for current Guild")]
    [UsedImplicitly]
    public async Task UpdateCurrentGuild(
        [Summary(description: DescriptionIncludeCache)]
        bool cache = true,
        [Summary(description: DescriptionIncludeMemberCache)]
        bool cacheMember = false,
        [Summary(description: DescriptionIncludeSnapshots)]
        bool snapshot = false)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        else if (!Context.Interaction.GuildId.HasValue || Context.Guild == null)
        {
            await Context.Interaction.RespondAsync(Emotes.Warning + " This command must be executed in a guild.");
            return;
        }

        if (!cache && !cacheMember && !snapshot)
        {
            await RespondAsync(Emotes.Warning + " At least one option must be enabled!");
            return;
        }

        await DeferAsync();
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = await UpdateGuildTask(Context.Guild, now, new UpdateGuildTaskFlags(cache, cacheMember, snapshot));
            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            await FollowupAsync($"Done! Took {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})");
            await FollowUpWithException(ex, "Failed to update current guild.");
        }
    }

    [SlashCommand("guilds-all", "Update all guilds. Might take a while")]
    [UsedImplicitly]
    public async Task UpdateAllGuilds(
        [Summary(description: DescriptionIncludeCache)]
        bool cache = true,
        [Summary(description: DescriptionIncludeMemberCache)]
        bool cacheMember = false,
        [Summary(description: DescriptionIncludeSnapshots)]
        bool snapshot = false)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }

        if (!cache && !cacheMember && !snapshot)
        {
            await RespondAsync(Emotes.Warning + " At least one option must be enabled!");
            return;
        }

        await DeferAsync();
        
        // na, not awaiting that...
        await Task.Delay(5_000);
        UpdateAllGuildsInternal(new UpdateGuildTaskFlags(cache, cacheMember, snapshot));
    }

    [SlashCommand("role-guild", "Update role data for specific guild")]
    [UsedImplicitly]
    public async Task UpdateGuildRoles(
        [Summary(description: "Guild ID. Xenia must be a member")]
        string guildId)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        if (!ulong.TryParse(guildId, out var guildIdValue))
        {
            await RespondAsync($"{Emotes.Warning} Could not parse the provided Guild Id\n{guildId}");
            return;
        }

        await DeferAsync();
        IGuild? guild = null;
        try
        {
            guild = await ExceptionHelper.RetryOnTimedOut(async () => await Context.Client.GetGuildAsync(guildIdValue));
            if (guild == null)
            {
                await Context.Interaction.FollowupAsync(
                    $"Could not find guild: `{guildId}`\n-# I might not be a member of it anymore.");
                return;
            }

            var now = DateTime.UtcNow;
            var elapsed = await UpdateGuildRolesTask(guild, now);

            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var name = guild.Name.Replace("`", "'");
            await Context.Interaction.FollowupAsync($"Updated Guild `{name}` (`{guild.Id}`) in {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Roles for Guild \"{guild?.Name}\" ({guildId})");
            await FollowUpWithException(ex, $"Failed to update guild: `{guildId}`");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                $"Failed to update guild: `{guildId}`");
        }
    }
    
    [SlashCommand("role-guild-curr", "Update role data for current guild")]
    [UsedImplicitly]
    public async Task UpdateCurrentGuildRoles()
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        else if (!Context.Interaction.GuildId.HasValue || Context.Guild == null)
        {
            await Context.Interaction.RespondAsync(Emotes.Warning + " This command must be executed in a guild.");
            return;
        }

        await DeferAsync();
        var guildId = Context.Guild.Id;
        var name = Context.Guild.Name.Replace("`", "'");
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = await UpdateGuildRolesTask(Context.Guild, now);

            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            await Context.Interaction.FollowupAsync($"Updated Guild `{name}` (`{guildId}`) in {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Roles for Guild \"{Context.Guild.Name}\" ({guildId})");
            await FollowUpWithException(ex, $"Failed to update guild: `{guildId}`");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                $"Failed to update guild: `{guildId}`");
        }
    }

    [SlashCommand("role-guild-all", "Update role data for ALL guilds")]
    [UsedImplicitly]
    public async Task UpdateAllGuildRoles()
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }

        await DeferAsync();
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = TimeSpan.Zero;
            var count = 0;
            foreach (var guild in _client.Guilds)
            {
                try
                {
                    var currentGuildElapsed = await UpdateGuildRolesTask(Context.Guild, now);
                    elapsed = TimeSpan.FromTicks(currentGuildElapsed.Ticks + elapsed.Ticks);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to process guild: {guild.Id}", ex);
                }

                count++;
            }

            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var durationAvg = Math.Round(elapsed.TotalMilliseconds / (float)count);
            await Context.Interaction.FollowupAsync("Updated " + "guild".ToQuantity(count, "N0") + $"\n-# total: {duration}s\n-# {durationAvg}ms per guild (avg)");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update all guild roles");
            await FollowUpWithException(ex, "Failed to update all guild roles");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to update all guild roles");
        }
    }

    private async Task<TimeSpan> UpdateGuildRolesTask(IGuild guild, DateTime now)
    {
        var elapsedSnapshot = await ModuleHelper.PerformTransaction(_services, async db =>
        {
            await _snapshotService.UpdateGuildRoles(db, guild, now: now, DiscordSnapshotSource.AdminTask);
            return true;
        });
        var elapsedCache = await ModuleHelper.PerformTransaction(_services, async db =>
        {
            await _discordCacheService.UpdateGuildRoles(db, guild, now: now);
            return true;
        });
        return elapsedSnapshot + elapsedCache;
    }

    private Task<TimeSpan> UpdateGuildTask(
        IGuild guild, DateTime now, UpdateGuildTaskFlags flags)
        => UpdateGuildTask(_services.GetRequiredService<IDbContextFactory<XeniaDbContext>>(), guild, now, flags);
    private async Task<TimeSpan> UpdateGuildTask(
        IDbContextFactory<XeniaDbContext> dbFactory,
        IGuild guild, DateTime now, UpdateGuildTaskFlags flags)
    {
        if (flags is { Cache: false, CacheMember: false, Snapshot: false }) return TimeSpan.Zero;
        
        var elapsed = TimeSpan.Zero;
        if (flags.Cache || flags.CacheMember)
        {
            var elapsedCache = await ModuleHelper.PerformTransaction(dbFactory, async db =>
            {
                await _discordCacheService.UpdateGuild(db, guild, now: now, includeMembers: flags.CacheMember);
                return true;
            });
            elapsed = TimeSpan.FromTicks(elapsed.Ticks + elapsedCache.Ticks);
        }

        if (flags.Snapshot)
        {
            var elapsedSnapshot = await ModuleHelper.PerformTransaction(dbFactory, async db =>
            {
                await _snapshotService.UpdateGuild(db, guild, now, DiscordSnapshotSource.AdminTask);
                return true;
            });
            elapsed = TimeSpan.FromTicks(elapsed.Ticks + elapsedSnapshot.Ticks);
        }

        return elapsed;
    }

    private async Task UpdateAllGuildsInternal(UpdateGuildTaskFlags flags)
    {
        var dbContextFactory = _services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = TimeSpan.Zero;
            var count = 0;
            foreach (var guild in _client.Guilds)
            {
                if (guild.Id == 826825694205444107) continue; // lmfao fuck this server. it has 16k bots in it
                try
                {
                    var elapsedInner = await UpdateGuildTask(dbContextFactory, guild, now, flags);
                    elapsed = TimeSpan.FromTicks(elapsed.Ticks + elapsedInner.Ticks);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to process guild: \"{guild.Name}\" ({guild.Id})", ex);
                }

                count++;
            }
            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var durationAvg = Math.Round(elapsed.TotalMilliseconds / (float)count);
            await Context.Interaction.FollowupAsync("Updated " + "guild".ToQuantity(count, "N0") + $"\n-# total: {duration}s\n-# {durationAvg}ms per guild (avg)");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update all guilds (count: {_client.Guilds.Count})");
            await FollowUpWithException(ex, $"Failed to update all guilds (count: {_client.Guilds.Count})");
        }
    }

    private sealed record UpdateGuildTaskFlags(bool Cache, bool CacheMember, bool Snapshot);
    private const string DescriptionIncludeCache = "Update Guild Cache?";
    private const string DescriptionIncludeMemberCache = "Update Guild Member Cache?";
    private const string DescriptionIncludeSnapshots = "Update Guild Snapshots?";
}
