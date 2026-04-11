using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Common.Services;

public partial class GuildApprovalService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly ValidationService _validation;

    public GuildApprovalService(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _validation = services.GetRequiredService<ValidationService>();
    }                       

    public async Task<bool> Exists(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr);
    }
    public async Task<bool> IsEnabled(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled);
    }
    public async Task<bool> IsGreeterEnabled(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled && e.EnableGreeter);
    }

    public static readonly ChannelPermission[] RequiredChannelPermissions = new[]
    {
        ChannelPermission.SendMessages,
        ChannelPermission.ViewChannel,
        ChannelPermission.ReadMessageHistory,
        ChannelPermission.AttachFiles,
        ChannelPermission.EmbedLinks,
    };
    public static readonly GuildPermission[] RequiredGuildPermissions = new[]
    {
        GuildPermission.ViewAuditLog,
        GuildPermission.ManageRoles,
        GuildPermission.ModerateMembers,
        
        GuildPermission.SendMessages,
        GuildPermission.ViewChannel,
        GuildPermission.ReadMessageHistory,
        GuildPermission.AttachFiles,
        GuildPermission.EmbedLinks
    };

    public class SetupModalResult
    {
        public SetupModalResultFlags Flags { get; }

        public ChannelPermission[] MissingLogChannelPermissions { get; }
        public ChannelPermission[] MissingGreeterChannelPermissions { get; }
        public GuildPermission[] MissingPermissions { get; } 

        public ITextChannel TargetLogChannel { get; }
        public ITextChannel TargetGreeterChannel { get; }

        public bool IsSuccess => Flags == SetupModalResultFlags.Success;
        public bool IsFailure => !Flags.HasFlag(SetupModalResultFlags.Success);
    }

    [Flags]
    public enum SetupModalResultFlags
    {
        None = 0,
        Success = 1 << 0,

        MissingPermissions_LogChannel = 1 << 1,
        MissingPermissions_GreeterChannel = 1 << 2,

        MissingPermissionsInGuild = 1 << 3,
    }

    public async Task ValidateAsync(GuildApprovalModel model)
    {
        // TODO return a markdown string used in the setup modal to validate config.
        // use ValidationService for checking channel permissions
    }
}
