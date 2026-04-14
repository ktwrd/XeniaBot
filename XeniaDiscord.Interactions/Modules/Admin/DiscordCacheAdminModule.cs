using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Diagnostics;
using XeniaBot.Shared;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions.Modules.Admin;

[Group("cacheadmin", "Cache administration")]
[DeveloperModule]
[CommandContextType(InteractionContextType.Guild)]
[RequireDeveloper]
public partial class DiscordCacheAdminModule : InteractionModuleBase
{
    private readonly ConfigData _config;
    private readonly XeniaDbContext _db;
    private readonly DiscordSnapshotService _snapshotService;
    private readonly DiscordCacheService _discordCacheService;
    private readonly DiscordSocketClient _client;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public DiscordCacheAdminModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _db = services.GetRequiredService<XeniaDbContext>();
        _snapshotService = services.GetRequiredService<DiscordSnapshotService>();
        _discordCacheService = services.GetRequiredService<DiscordCacheService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
    }

    [SlashCommand("update-guild", "Update cache for specific guild")]
    public async Task UpdateGuild(
        string guildIdStr,
        bool includeSnapsnots = false)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        if (!ulong.TryParse(guildIdStr, out var guildId))
        {
            await Context.Interaction.FollowupAsync($"Could not parse the provided Guild Id\n{guildIdStr}");
            return;
        }

        await DeferAsync();
        try
        {
            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                await Context.Interaction.FollowupAsync($"Could not find guild: `{guildId}`\n" +
                    "-# I might not be a member of it anymore.");
                return;
            }
            var now = DateTime.UtcNow;
            var sw = new Stopwatch();
            sw.Start();
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await _discordCacheService.UpdateGuild(db, guild);
                if (includeSnapsnots)
                {
                    await _snapshotService.UpdateGuild(db, guild, now, Data.Models.Snapshot.DiscordSnapshotSource.Unknown);
                }
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
            finally
            {
                sw.Stop();
            }
            var duration = Math.Round(sw.Elapsed.TotalMilliseconds / 1000f, 3);
            var name = guild.Name.Replace("`", "'");
            await Context.Interaction.FollowupAsync($"Updated Guild `{name}` (`{guild.Id}`) in {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                $"Failed to update guild: `{guildId}`");
        }
    }

    [SlashCommand("update-current-guild", "Update cache for current guild")]
    public async Task UpdateCurrentGuild(
        [Summary(description: "Snapshot tables will be updated when this is enabled.")]
        bool includeSnapsnots = false)
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

        await DeferAsync();
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var now = DateTime.Now;
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await _discordCacheService.UpdateGuild(db, Context.Guild);
                if (includeSnapsnots)
                {
                    await _snapshotService.UpdateGuild(db, Context.Guild, now, Data.Models.Snapshot.DiscordSnapshotSource.Unknown);
                }
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
            finally
            {
                sw.Stop();
            }
            var duration = Math.Round(sw.Elapsed.TotalMilliseconds / 1000f, 3);
            await Context.Interaction.FollowupAsync($"Done! Took {duration}s");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to update current guild.");
        }
    }

    [SlashCommand("update-all-guilds", "Update all guilds")]
    public async Task UpdateAllGuilds(
        bool includeSnapsnots = false)
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }

        await DeferAsync();
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var now = DateTime.Now;
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                foreach (var guild in _client.Guilds)
                {
                    try
                    {
                        await _discordCacheService.UpdateGuild(db, guild);
                        if (includeSnapsnots)
                        {
                            await _snapshotService.UpdateGuild(db, guild, now, Data.Models.Snapshot.DiscordSnapshotSource.Unknown);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to update Guild \"{guild.Name}\" ({guild.Id})", ex);
                    }
                }
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
            finally
            {
                sw.Stop();
            }
            var duration = Math.Round(sw.Elapsed.TotalMilliseconds / 1000f, 3);
            var count = _client.Guilds.Count.ToString("n0");
            await Context.Interaction.FollowupAsync($"Took {duration}s to update {count} guild(s)");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to update all guilds");
            await Context.Interaction.FollowupWithFileAsync(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",
                "Failed to update all guilds.");
        }
    }
}
