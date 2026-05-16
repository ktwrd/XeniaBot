using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
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
public partial class AdmDataModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly XeniaDbContext _db;
    private readonly DiscordSnapshotService _snapshotService;
    private readonly DiscordCacheService _discordCacheService;
    private readonly DiscordSocketClient _client;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IServiceProvider _services;
    public AdmDataModule(IServiceProvider services)
    {
        _services = services;
        _config = services.GetRequiredService<ConfigData>();
        _db = services.GetRequiredService<XeniaDbContext>();
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
            var elapsed = await ModuleHelper.PerformTransaction(_services, async db =>
            {
                if (cache || cacheMember)
                {
                    await _discordCacheService.UpdateGuild(db, guild, now: now, includeMembers: cacheMember);
                }
                if (snapshot)
                {
                    await _snapshotService.UpdateGuild(db, guild, now, DiscordSnapshotSource.GuildUpdated);
                }
                return true;
            });

            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var name = guild.Name.Replace("`", "'");
            await Context.Interaction.FollowupAsync($"Updated Guild `{name}` (`{guild.Id}`) in {duration}s");
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
            await Context.Interaction.RespondAsync("This command must be executed in a guild.");
            return;
        }

        if (!cache && !cacheMember && !snapshot)
        {
            await RespondAsync("At least one option must be enabled!");
            return;
        }

        await DeferAsync();
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = await ModuleHelper.PerformTransaction(_services, async db =>
            {
                if (cache)
                {
                    await _discordCacheService.UpdateGuild(db, Context.Guild, now, includeMembers: cacheMember);
                }
                if (snapshot)
                {
                    await _snapshotService.UpdateGuild(db, Context.Guild, now, DiscordSnapshotSource.GuildUpdated);
                }
                return true;
            });
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
            await RespondAsync("At least one option must be enabled!");
            return;
        }

        await DeferAsync();
        try
        {
            var now = DateTime.Now;
            var elapsed = await ModuleHelper.PerformTransaction(_services, async db =>
            {
                foreach (var guild in _client.Guilds)
                {
                    try
                    {
                        if (cache || cacheMember)
                        {
                            await _discordCacheService.UpdateGuild(db, guild, now: now, includeMembers: cacheMember);
                        }
                        if (snapshot)
                        {
                            await _snapshotService.UpdateGuild(db, guild, now, DiscordSnapshotSource.GuildUpdated);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to update Guild \"{guild.Name}\" ({guild.Id})", ex);
                    }
                }
                return true;
            });
            var duration = Math.Round(elapsed.TotalMilliseconds / 1000f, 3);
            var count = _client.Guilds.Count.ToString("n0");
            await Context.Interaction.FollowupAsync($"Took {duration}s to update {count} guilds");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update all guilds (count: {_client.Guilds.Count})");
            await FollowUpWithException(ex, $"Failed to update all guilds (count: {_client.Guilds.Count})");
        }
    }
    private const string DescriptionIncludeCache = "Update Guild Cache?";
    private const string DescriptionIncludeMemberCache = "Update Guild Member Cache?";
    private const string DescriptionIncludeSnapshots = "Update Guild Snapshots?";
}
