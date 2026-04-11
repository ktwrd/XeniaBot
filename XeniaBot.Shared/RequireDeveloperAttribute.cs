using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared;

/// <remarks>
/// Used to deduplicate code for restricting command usage to specific developers.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireDeveloperAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var config = services.GetService<ConfigData>();
        if (config?.UserWhitelist.Contains(context.User.Id) != true)
        {
            return PreconditionResult.FromError("This command can only be executed by trusted developers.");
        }
        return PreconditionResult.FromSuccess();
    }
}
