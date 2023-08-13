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
using XeniaBot.Core.Helpers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules
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
                var url = $"https://api.tinyfox.dev/img?animal={animalType}";
                var jsonUrl = url + "&json";
                var response = Program.HttpClient.GetAsync(jsonUrl).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var substr = content.Substring(0, Math.Min(content.Length, 900));
                    await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                    {
                        Color = Color.Red,
                        Description = $"Failed to fetch image\n```\n{substr}\n```"
                    }.Build());
                    return;
                };
                var stringContent = response.Content.ReadAsStringAsync().Result;
                var deserialized = JsonSerializer.Deserialize<TinyFoxImageModel>(stringContent, Program.SerializerOptions);

                if (deserialized == null)
                    throw new Exception("Deserialized content to null");
                
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                {
                    Title = "Random Animal",
                    Color = new Color(255, 255, 255),
                    ImageUrl = deserialized.ImageLocation,
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = url
                    }
                }.Build());
            }
            catch (Exception exception)
            {
                Log.Error(exception.ToString());
                await Context.Interaction.RespondAsync($"A fatal error occoured. The developers have been notified\n```\n{exception.Message}\n```");
                await DiscordHelper.ReportError(exception, Context);
            }
        }
    }
}
