using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Noppes.E621;
using XeniaBot.Core.Controllers.Wrappers;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Modules
{
    [Group("esix", "e926 Integration")]
    public class ESixModule : InteractionModuleBase
    {
        [SlashCommand("query", "Query for posts. Fetches the first one")]
        public async Task Query(string query, 
            [Discord.Interactions.Summary(description: "Select a random post from all posts on the first page of this query.")]
            bool random = false)
        {
            var controller = Program.Core.GetRequiredService<ESixController>();
            
            // Manually get current channel as text channel
            var discordClient = Program.Core.GetRequiredService<DiscordSocketClient>();
            var guild = discordClient.GetGuild(Context.Guild.Id);
            var channel = guild.GetTextChannel(Context.Channel.Id);

            // If channel is not nsfw, then enforce allowNsfw
            if (!channel.IsNsfw)
                query += " rating:s";

            var embed = new EmbedBuilder()
                .WithFooter(query);

            Post[] results = Array.Empty<Post>();
            try
            {
                results = await controller.Query(query);
            }
            catch (Exception ex)
            {
                embed.WithTitle("Failed to query posts")
                     .WithDescription(ex.Message)
                     .WithColor(Color.Red);
                await Context.Interaction.RespondAsync(embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }

            if (results.Length < 1)
            {
                embed.WithTitle("No posts found!")
                     .WithColor(Color.Red);

                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }

            if (random)
                new Random().Shuffle(results);
            Post targetPost = results[0];
            var url = targetPost.File?.Location
                ?? targetPost.Sample?.Location
                ?? targetPost.Preview?.Location;
            embed.WithTitle("View Post")
                 .WithUrl((channel.IsNsfw ? $"https://e621.net" : $"https://e926.net") + $"/posts/{targetPost.Id}")
                 .WithImageUrl(url?.ToString() ?? "");
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}
