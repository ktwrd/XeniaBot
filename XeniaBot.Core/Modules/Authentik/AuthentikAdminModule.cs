using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
    public async Task Cmd_CreateUser(string username)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Create User").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        try
        {
            if (!Program.ConfigData.AuthentikEnable)
            {
                embed.WithDescription("Disabled");
                await FollowupAsync(embed: embed.Build());
                return;
            }
            AuthentikUserResponse? response = await CreateAccountAsync(username);;

            if (response == null)
                throw new Exception("Response is null");

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
        catch (Exception ex)
        {
            embed.WithColor(Color.Red)
                .WithTitle("Authentik - Failed to run task")
                .WithDescription($"idfk what happened, you gotta fix this kate. api returned null you goofy goober");
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString()));
            await FollowupWithFileAsync(
                embed: embed.Build(), ephemeral: true, fileStream: ms, fileName: "exception.txt");
        }
    }

    [SlashCommand("userpassreset", "Create a password reset link for a user")]
    public async Task Cmd_CreateResetLink(string userId)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Password Reset Link").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        try
        {
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
                targetUserId = await SafelyGetUserId(targetUserId);
                if (targetUserId == null)
                    throw new Exception("targetUserId is null (not found)");
            }
            var link = await CreatePasswordResetLink(targetUserId);
            if (link == null)
                throw new Exception("Failed to get reset link (null)");

            embed.WithDescription($"[Reset Link]({link})")
                .WithColor(Color.Blue);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }

    [SlashCommand("userdelete", "Delete a user")]
    public async Task Cmd_DeleteUser(string userId)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Delete User").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        try
        {
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
                targetUserId = await SafelyGetUserId(targetUserId);
                if (targetUserId == null)
                    throw new Exception("targetUserId is null (user not found)");
            }
            if (await DeleteUser(targetUserId))
            {
                embed.WithDescription("Account deleted successfully").WithColor(Color.Green);
            }
            else
            {
                embed.WithDescription("Failed to delete user ;w;").WithColor(Color.Red);
            }
        }
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }

    [SlashCommand("userlist", "List all users")]
    public async Task Cmd_ListUsers()
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - List Users").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        try
        {
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
            var data = await GetUsers();
            if (data == null)
                throw new Exception("Data is null");

            foreach (var item in data.Results.OrderBy(v => v.Id))
            {
                descLines.Add($"`| {item.Id,-4} | {item.Username}`");
            }

            embed.WithDescription(string.Join("\n", descLines)).WithColor(Color.Blue);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }

    [SlashCommand("userinfo", "List all users")]
    public async Task Cmd_UserInfo(string user)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - User Info").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        try
        {
            if (!Program.ConfigData.AuthentikEnable)
            {
                embed.WithDescription("Disabled");
                await FollowupAsync(embed: embed.Build());
                return;
            }
            
            string? targetUserId = user;
            var integerRegex = new Regex(@"^[0-9]+$");
            if (!integerRegex.IsMatch(targetUserId))
            {
                targetUserId = await SafelyGetUserId(targetUserId);
                if (targetUserId == null)
                    throw new Exception("targetUserId is null (user not found)");
            }
            
            var userData = await GetUser(targetUserId);
            if (userData == null)
                throw new Exception("User Data is null");
            embed.WithDescription(string.Join("\n", new string[]
            {
                $"Id:           {userData.Id}",
                $"Username:     {userData.Username}",
                $"DisplayName:  {userData.DisplayName}",
                $"IsActive:     {userData.IsActive}",
                $"LastLogin:    {userData.LastLogin}",
                $"Email:        {userData.Email}",
                $"Groups:       " + string.Join(", ", userData.Groups.OrderBy(v => v))
            }.Select(v => $"`{v,-1}`")));
            embed.WithColor(Color.Blue);
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }

    [SlashCommand("addtogroup", "Add a user to a group")]
    public async Task Cmd_AddUserToGroup(string user, string group)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Add to Group").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;
        
        try
        {
            if (!Program.ConfigData.AuthentikEnable)
            {
                embed.WithDescription("Disabled");
                await FollowupAsync(embed: embed.Build());
                return;
            }
        
            string? targetUser = user;
            targetUser = await SafelyGetUserId(targetUser);
            if (targetUser == null)
                throw new Exception("targetUser is null (not found)");
        
            string? targetGroup = group;
            targetGroup = await SafelyGetGroupId(targetGroup);
            if (targetGroup == null)
            {
                throw new Exception("targetGroup is null (not found)");
            }
            
            bool success = await AddToGroup(int.Parse(targetUser), targetGroup);

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
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }

    [SlashCommand("removefromgroup", "Remove a user from a group")]
    public async Task Cmd_RemoveUserFromGroup(string user, string group)
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder().WithTitle("Authentik - Remove from Group").WithCurrentTimestamp();
        if (!Program.ConfigData.UserWhitelist.Contains(Context.User.Id))
            return;

        try
        {
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
                throw new Exception("targetUser is null (not found)");
            }
        
            string? targetGroup = group;
            targetGroup = await SafelyGetGroupId(targetGroup);
            if (targetGroup == null)
            {
                throw new Exception("targetGroup is null (not found)");
            }
            bool success = await RemoveFromGroup(int.Parse(targetUser), targetGroup);

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
        catch (Exception e)
        {
            embed.WithDescription($"Failed to process").WithColor(Color.Red);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
            await FollowupWithFileAsync(embed: embed.Build(), ephemeral: true, fileStream:ms, fileName:"exception.txt");
            await DiscordHelper.ReportError(e, Context);
        }
    }
}