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

    [SlashCommand("userpassreset", "Create a password reset link for a user")]
    [RequireOwner]
    public async Task Cmd_CreateResetLink(string userId)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Password Reset Link").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }
        
        string? targetUserId = userId;
        var integerRegex = new Regex(@"^[0-9]+$");
        if (!integerRegex.IsMatch(targetUserId))
        {
            try
            {
                targetUserId = await SafelyGetUserId(targetUserId);
            }
            catch (Exception e)
            {
                Log.Error(e);
                embed.WithDescription($"{e.Message}").AddField("Exception", $"```\n{e}\n```").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
                await DiscordHelper.ReportError(e, Context);
                return;
            }
        }

        if (targetUserId == null)
        {
            embed.WithDescription($"No users found with the username or ID of `{targetUserId,-1}`")
                .WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }
        
        try
        {
            var link = await CreatePasswordResetLink(targetUserId);
            if (link == null)
            {
                embed.WithDescription($"Failed to get reset link. It's null...")
                    .WithColor(Color.Orange);
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            embed.WithDescription($"[Reset Link]({link})")
                .WithColor(Color.Blue);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }
        catch (Exception e)
        {
            Log.Error(e);
            embed.WithDescription($"{e.Message}").AddField("Exception", $"```\n{e}\n```").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
            return;
        }
    }

    [SlashCommand("userdelete", "Delete a user")]
    [RequireOwner]
    public async Task Cmd_DeleteUser(string userId)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Delete User").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }
        
        string? targetUserId = userId;
        var integerRegex = new Regex(@"^[0-9]+$");
        if (!integerRegex.IsMatch(targetUserId))
        {
            try
            {
                targetUserId = await SafelyGetUserId(targetUserId);
            }
            catch (Exception e)
            {
                Log.Error(e);
                embed.WithDescription($"{e.Message}").AddField("Exception", $"```\n{e}\n```").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
                await DiscordHelper.ReportError(e, Context);
                return;
            }
        }

        if (targetUserId == null)
        {
            embed.WithDescription($"No users found with the username or ID of `{targetUserId,-1}`")
                .WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }

        bool success = true;
        try
        {
            success = await DeleteUser(targetUserId);
        }
        catch (Exception e)
        {
            Log.Error(e);
            embed.WithDescription($"{e.Message}").AddField("Exception", $"```\n{e}\n```").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        if (success)
        {
            embed.WithDescription("Account deleted successfully").WithColor(Color.Green);
        }
        else
        {
            embed.WithDescription("Failed to delete user ;w;").WithColor(Color.Red);
        }
    }
}