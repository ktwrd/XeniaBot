using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.BotAdditions;
using SkidBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    [Group("xp", "Experience")]
    public class XpModule : InteractionModuleBase
    {
        [SlashCommand("about", "What is the XP module")]
        public async Task About()
        {
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder(){
                Title = "XP Module: About",
                Description = "Every time you send a message (and the previous one was <8s ago) you get a random amount of XP (between 1 and 5)"
            }.Build(), ephemeral: true);
        }

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
    }
}
