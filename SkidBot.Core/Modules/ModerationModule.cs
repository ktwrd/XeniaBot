using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Modules;


public class ModerationModule : InteractionModuleBase
{
    private async Task<SocketGuildUser?> SafelyFetchUser(ulong userId)
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithColor(Color.Red)
            .WithTitle("Failed to safely fetch user");
        try
        {
            DiscordSocketClient client = (DiscordSocketClient)Context.Client;
            if (client == null)
            {

                throw new NonfatalException("Failed to fetch discord client");
            }

            SocketGuild? guild = client.GetGuild(Context.Guild.Id);
            if (guild == null)
            {
                throw new NonfatalException($"Failed to fetch guild ({Context.Guild.Id})");
            }

            SocketGuildUser? member = guild.GetUser(userId);
            if (member == null)
            {
                throw new NonfatalException($"Member not found ({userId})");
            }

            return member;
        }
        catch (NonfatalException e)
        {
            await Context.Interaction.RespondAsync(
                embed: embed.WithDescription(e.Message).Build(),
                ephemeral: true);
            return null;
        }
        catch (Exception e)
        {
            embed.WithDescription($"Fatal Error! This has been reported to the developers.\n```\n{e.Message}\n```");
            await Context.Interaction.RespondAsync(
                embed: embed.Build(),
                ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
        }
        return null;
    }
    
    [SlashCommand("kick", "Kick member from server")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task KickMember(SocketGuildUser user, string? reason = null)
    {
        SocketGuildUser? member = await SafelyFetchUser(user.Id);
        if (member == null)
            return;

        var embed = DiscordHelper.BaseEmbed().WithTitle("Kick Member");
        
        try
        {
            await member.KickAsync(reason);
        }
        catch (Exception e)
        {
            embed.WithDescription(string.Join("\n", new string[]
            {
                "Failed to kick member, this has been reported to the developers",
                "```",
                e.Message,
                "```"
            }));
            embed.WithColor(Color.Red);
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
        }

        embed.WithDescription($"Successfully kicked `{user.Username}#{user.DiscriminatorValue}`")
             .WithColor(Color.Blue);
        if (reason != null)
            embed.AddField("Reason", reason);
        
        await Context.Interaction.RespondAsync(
            embed: embed.Build(),
            ephemeral: true);
    }
}