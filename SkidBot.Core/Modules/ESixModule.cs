﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Noppes.E621;
using SkidBot.Core.Controllers.Wrappers;
using SkidBot.Core.Helpers;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    [Group("esix", "e926 Intergration")]
    public class ESixModule : InteractionModuleBase
    {
        [SlashCommand("query", "Query for posts. Fetches the first one")]
        public async Task Query(string query, bool allowNsfw = false, bool random = false)
        {
            var controller = Program.Services.GetRequiredService<ESixController>();
            
            // Manually get current channel as text channel
            var discordClient = Program.Services.GetRequiredService<DiscordSocketClient>();
            var guild = discordClient.GetGuild(Context.Guild.Id);
            var channel = guild.GetTextChannel(Context.Channel.Id);

            // If channel is not nsfw, then enforce allowNsfw
            allowNsfw = channel.IsNsfw ? allowNsfw : false;
            if (!allowNsfw)
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
                 .WithUrl($"https://e926.net/posts/{targetPost.Id}")
                 .WithImageUrl(url?.ToString() ?? "");
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}