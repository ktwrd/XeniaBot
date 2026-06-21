using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace XeniaBot.Shared;

/// <summary>
/// Precondition Attribute used to limit a module to only be used by users who have any of the defined Channel or Guild permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequireAnyUserPermissionAttribute : PreconditionAttribute
{
    public RequireAnyUserPermissionAttribute(GuildPermission guildPermission)
    {
        GuildPermission = guildPermission;
    }
    public RequireAnyUserPermissionAttribute(ChannelPermission channelPermission)
    {
        ChannelPermission = channelPermission;
    }
    public GuildPermission? GuildPermission { get; set; }
    public ChannelPermission? ChannelPermission { get; set; }

    public string? NotAGuildErrorMessage { get; set; }

    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        if (context.User is not IGuildUser user)
        {
            return Task.FromResult(PreconditionResult.FromError(this.NotAGuildErrorMessage ?? "Command must be used in a guild channel."));
        }

        var found = false;
        if (this.GuildPermission.HasValue)
        {
            foreach (var flag in Enum.GetValues<GuildPermission>())
            {
                if (GuildPermission.Value.HasFlag(flag) &&
                    user.GuildPermissions.Has(flag))
                {
                    found = true;
                    break;
                }
            }
        }
        if (this.ChannelPermission.HasValue)
        {
            ChannelPermissions perms;
            if (context.Channel is IGuildChannel guildChannel)
            {
                perms = user.GetPermissions(guildChannel);
            }
            else
            {
                perms = ChannelPermissions.All(context.Channel);
            }
            foreach (var flag in Enum.GetValues<ChannelPermission>())
            {
                if (ChannelPermission.Value.HasFlag(flag) &&
                    perms.Has(flag))
                {
                    found = true;
                    break;
                }
            }
        }

        if (found) return Task.FromResult(PreconditionResult.FromSuccess());

        var reason = this.ErrorMessage ?? string.Empty;
        if (!string.IsNullOrEmpty(reason))
            return Task.FromResult(PreconditionResult.FromError(reason));
        
        if (GuildPermission.HasValue)
        {
            reason = "User requires any of the following guild permissions: ";
            reason += string.Join(", ",
                Enum.GetValues<GuildPermission>()
                    .Where(e => GuildPermission.Value.HasFlag(e))
                    .Select(e => e.ToString()));
        }

        if (ChannelPermission.HasValue)
        {
            if (GuildPermission.HasValue)
            {
                reason += ". Or any of the following channel permissions: ";
            }
            else
            {
                reason = "User required any of the following channel permissions: ";
            }
            reason += string.Join(", ",
                Enum.GetValues<ChannelPermission>()
                    .Where(e => ChannelPermission.Value.HasFlag(e))
                    .Select(e => e.ToString()));
        }
        return Task.FromResult(PreconditionResult.FromError(reason));
    }
    
}