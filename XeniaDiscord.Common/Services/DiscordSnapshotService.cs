using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Services;

public class DiscordSnapshotService : BaseService
{
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _client;
    private readonly IMapper<IGuildUser, GuildMemberSnapshotModel> _guildMemberMapper;

    private readonly ErrorReportService _err;
    public DiscordSnapshotService(IServiceProvider services) : base(services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
        _client = services.GetRequiredService<DiscordSocketClient>();
        
        _guildMemberMapper = services.GetRequiredService<IMapper<IGuildUser, GuildMemberSnapshotModel>>();

        _err = services.GetRequiredService<ErrorReportService>();

        var programDetails = services.GetRequiredService<ProgramDetails>();
        if (programDetails.Platform == XeniaPlatform.Bot)
        {
            _client.GuildMemberUpdated += OnGuildMemberUpdated;
        }
    }

    public event DiscordSnapshotComparisonDelegate<GuildMemberSnapshotModel>? GuildMemberUpdated;

    private async Task OnGuildMemberUpdated(
        Cacheable<SocketGuildUser, ulong> socketMemberBefore,
        SocketGuildUser socketMemberAfter)
    {
        var userIdStr = socketMemberAfter.Id.ToString();
        var guildIdStr = socketMemberAfter.Guild.Id.ToString();
        GuildMemberSnapshotModel? modelBefore = null;
        try
        {
            modelBefore = await _db.GuildMemberSnapshots.AsNoTracking()
                .Include(e => e.Roles)
                .Include(e => e.Permissions)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to find Guild Member Snapshot (userId={userIdStr},guildId={guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
        }

        GuildMemberSnapshotModel model;
        try
        {
            model = _guildMemberMapper.Map(socketMemberAfter);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to map {socketMemberAfter.GetType()} (userId: {userIdStr}, guildId: {guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
            return;
        }
        try
        {
            await _db.AddAsync(model);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var msg = $"Failed to add record into database";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter)
                .AddSerializedAttachment("model.json", model));
            return;
        }
        GuildMemberUpdated?.Invoke(modelBefore, model);
    }
}

public delegate Task DiscordSnapshotComparisonDelegate<TModel>(TModel? before, TModel model);