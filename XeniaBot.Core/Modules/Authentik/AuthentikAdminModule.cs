﻿using System;
using System.Collections.Generic;
using System.Linq;
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

    [SlashCommand("userlist", "List all users")]
    [RequireOwner]
    public async Task Cmd_ListUsers()
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - List Users").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }

        var descLines = new List<string>()
        {
            $"`| id   | username`"
        };
        try
        {
            var data = await GetUsers();
            if (data == null)
                throw new Exception("Data is null");

            foreach (var item in data.Results.OrderBy(v => v.Id))
            {
                descLines.Add($"`| {item.Id,-4} | {item.Username}`");
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            embed.WithDescription($"{e.Message}").AddField("Exception", $"```\n{e}\n```").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        embed.WithDescription(string.Join("\n", descLines)).WithColor(Color.Blue);
        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("addtogroup", "Add a user to a group")]
    [RequireOwner]
    public async Task Cmd_AddUserToGroup(string user, string group)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Add to Group").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }
        
        string? targetUser = user;
        targetUser = await SafelyGetUserId(targetUser);
        if (targetUser == null)
        {
            embed.WithDescription($"User `{user,-1}` not found").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }
        
        string? targetGroup = group;
        targetGroup = await SafelyGetGroupId(targetGroup);
        if (targetGroup == null)
        {
            embed.WithDescription($"Group `{group,-1}` not found.").WithColor(Color.Red);
            await FollowupAsync(embed: embed.Build());
            return;
        }
        
        bool success = true;
        try
        {
            success = await AddToGroup(int.Parse(targetUser), targetGroup);
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
            embed.WithDescription($"Added user to group").WithColor(Color.Green);
        }
        else
        {
            embed.WithDescription($"Failed to add user to group (might be in it already)").WithColor(Color.Orange);
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("removefromgroup", "Remove a user from a group")]
    [RequireOwner]
    public async Task Cmd_RemoveUserFromGroup(string user, string group)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Remove from Group").WithCurrentTimestamp();
        if (!Program.ConfigData.AuthentikEnable)
        {
            embed.WithDescription("Disabled");
            await FollowupAsync(embed: embed.Build());
            return;
        }
        string? targetUser = user;
        targetUser = await SafelyGetUserId(targetUser);
        if (targetUser == null)
        {
            embed.WithColor(Color.Red).WithDescription($"User `{user,-1}` not found.");
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }
        
        string? targetGroup = group;
        targetGroup = await SafelyGetGroupId(targetGroup);
        if (targetGroup == null)
        {
            embed.WithColor(Color.Red).WithDescription($"Group `{group}` not found.");
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
            return;
        }

        bool success = true;
        try
        {
            success = await RemoveFromGroup(int.Parse(targetUser), targetGroup);
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
            embed.WithDescription($"Removed user from group!").WithColor(Color.Green);
        }
        else
        {
            embed.WithDescription($"Failed to remove user from group").WithColor(Color.Orange);
        }
        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}