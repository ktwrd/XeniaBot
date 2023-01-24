using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeyRed.Mime;
using ShortcakeBot.Core.Helpers;

namespace ShortcakeBot.Core.Modules
{
    [Discord.Interactions.Group("randomanimal", "Random Animal Pic")]
    public class RandomAnimalModule : InteractionModuleBase
    {
        [SlashCommand("fox", "Get a random image of a fox")]
        public async Task Fox()
        {
            await Boilerplate("fox");
        }
        [SlashCommand("yeen", "Get a random image of a yeen")]
        public async Task Yeen()
        {
            await Boilerplate("yeen");
        }
        [SlashCommand("dog", "Get a random image of a dog")]
        public async Task Dog()
        {
            await Boilerplate("dog");
        }

        public async Task Boilerplate(string animalType)
        {
            try
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                {
                    ImageUrl = @"https://api.tinyfox.dev/img?animal=" + animalType
                }.Build());
            }
            catch (Exception exception)
            {
                Log.Error(exception.ToString());
                await Context.Interaction.RespondAsync($"A fatal error occoured. The developers have been notified");
                await DiscordHelper.ReportError(exception, Context);
            }
        }
    }
}
