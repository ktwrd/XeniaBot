using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;


[Group("auth", "Authentik Administration")]
[RequireOwner]
public partial class AuthentikAdminModule : InteractionModuleBase
{
    [SlashCommand("usercreate", "Create a user")]
    [RequireOwner]
    public async Task Cmd_CreateUser(string username)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Create User").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }
        AuthentikUserResponse? response = null;
        try
        {
            response = await CreateAccountAsync(username);
        }
        catch (Exception e)
        {
            embed.WithColor(Color.Red)
                .WithTitle("Authentik - Failed to run task")
                .WithDescription($"```\n{e}\n```");
            await FollowupAsync(embed: embed.Build(), ephemeral:true);
            return;
        }
        if (response == null)
        {
            embed.WithColor(Color.Red)
                .WithTitle("Authentik - Failed to run task")
                .WithDescription($"idfk what happened, you gotta fix this kate. api returned null you goofy goober");
            await FollowupAsync(embed: embed.Build(), ephemeral:true);
            return;
        }

        embed.WithColor(Color.Green)
            .WithDescription(string.Join("\n", new string[]
            {
                $"Account created! ([view](https://{Program.ConfigData.AuthentikUrl}/if/admin/#/identity/users/{response.Id};%7B%22page%22%3A%22page-overview%22%7D))",
                "```",
                $"Id: {response.Id}",
                $"Username: {response.Username}",
                "```"
            }));
        await FollowupAsync(embed: embed.Build(), ephemeral:true);
    }
}