using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace XeniaBot.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequireUserWhitelistAttribute : PreconditionAttribute
{
    /// <inheritdoc />
    public override string ErrorMessage { get; set; }

    /// <inheritdoc />
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        switch (context.Client.TokenType)
        {
            case TokenType.Bot:
                var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                if (Program.ConfigData.UserWhitelist.Contains(context.User.Id))
                    return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by whitelisted users.");
                return PreconditionResult.FromSuccess();
            default:
                return PreconditionResult.FromError($"{nameof(RequireOwnerAttribute)} is not supported by this {nameof(TokenType)}.");
        }
    }
}