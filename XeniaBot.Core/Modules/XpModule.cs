using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using XeniaBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Core.Models;

namespace XeniaBot.Core.Modules
{
    [Group("xp", "Experience")]
    public class XpModule : InteractionModuleBase
    {
        [SlashCommand("profile", "See the amount of XP you have and what level you are")]
        public async Task Profile()
        {
            var controller = Program.Services.GetRequiredService<LevelSystemController>();
            var data = await controller.Get(Context.User.Id, Context.Guild.Id) ?? new Models.LevelMemberModel();
            var metadata = LevelSystemHelper.Generate(data);

            var embed = new EmbedBuilder()
            {
                Footer = new EmbedFooterBuilder()
                {
                    Text = "XP: Profile"
                },
                Description = string.Join("\n", new string[]
                {
                    $"**XP**: {data?.Xp ?? 0}",
                    $"**Progress**: {Math.Round(metadata.NextLevelProgress * 100, 3)}% ({metadata.UserXp - metadata.CurrentLevelStart}/{metadata.CurrentLevelEnd})",
                    $"**Level**: {metadata.UserLevel}"
                })
            };

            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        public async Task<EmbedBuilder> GenerateServerLeaderboard(ulong guildId)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Xp System - Guild Leaderboard");
            
            var controller = Program.Services.GetRequiredService<LevelSystemController>();
            if (controller == null)
            {
                embed.WithDescription($"Could not fetch LevelSystemController");
                embed.WithColor(Color.Red);
                return embed;
            }

            var data = await controller.GetGuild(guildId);
            var resultLines = new List<string>()
            {
                $"Top 5 of {data.Length} records."
            };

            var sorted = data.OrderByDescending(v => v.Xp).ToArray();
            var length = Math.Min(5, sorted.Length);
            string[] rankText = new string[]
            {
                ":first_place:",
                ":second_place:",
                ":third_place:",
                ":four:",
                ":five:"
            };
            for (int i = 0; i < length; i++)
            {
                var item = sorted[i];
                var details = LevelSystemHelper.Generate(item.Xp);
                resultLines.Add(string.Join(' ', new string[]
                {
                    rankText[i],
                    $"`{item.Xp}xp, level {details.UserLevel}`",
                    $"<@{item.UserId}>"
                }));
            }

            embed.Description = string.Join("\n", resultLines);
            return embed;
        }
        

        [SlashCommand("leaderboard", "View this guild's XP Leaderboard")]
        public async Task Leaderboard()
        {
            try
            {
                var embed = await GenerateServerLeaderboard(Context.Guild.Id);
                await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral:true);
            }
            catch (Exception ex)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Xp System - Error")
                    .WithDescription($"Failed to generate server leaderboard\n```\n{ex.Message}\n```")
                    .WithColor(Color.Red);
                await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
        }

        [SlashCommand("setchannel", "Set Log Channel")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetLogChannel(
            [ChannelTypes(ChannelType.Text)] ITextChannel logChannel)
        {
            await DeferAsync();
            try
            {
                var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
                var model = await controller.Get(Context.Guild.Id) ??
                            new LevelSystemGuildConfigModel()
                            {
                                GuildId = Context.Guild.Id
                            };

                model.LevelUpChannel = logChannel.Id;
                await controller.Set(model);
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Set Log Channel")
                        .WithDescription($"Log Channel updated to <#{model.LevelUpChannel}>")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build());
            }
            catch (Exception e)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Set Log Channel")
                        .WithDescription($"Failed to update channel. `{e.Message}`")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
                await DiscordHelper.ReportError(e, Context);
            }
        }

        [SlashCommand("enable", "Enable level up messages")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            await DeferAsync();
            try
            {
                var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
                var model = await controller.Get(Context.Guild.Id) ??
                            new LevelSystemGuildConfigModel()
                            {
                                GuildId = Context.Guild.Id
                            };

                model.ShowLeveUpMessage = true;
                await controller.Set(model);
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Show Level Up Message")
                        .WithDescription($"Enabled")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build());
            }
            catch (Exception e)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Show Level Up Message")
                        .WithDescription($"Failed to update data. `{e.Message}`")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
                await DiscordHelper.ReportError(e, Context);
            }
        }

        [SlashCommand("disable", "Disable level up messages")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Disable()
        {
            await DeferAsync();
            try
            {
                var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
                var model = await controller.Get(Context.Guild.Id) ??
                            new LevelSystemGuildConfigModel()
                            {
                                GuildId = Context.Guild.Id
                            };

                model.ShowLeveUpMessage = false;
                await controller.Set(model);
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Show Level Up Message")
                        .WithDescription($"Enabled")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build());
            }
            catch (Exception e)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Xp System - Show Level Up Message")
                        .WithDescription($"Failed to update data. `{e.Message}`")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
                await DiscordHelper.ReportError(e, Context);
            }
        }
    }
}
