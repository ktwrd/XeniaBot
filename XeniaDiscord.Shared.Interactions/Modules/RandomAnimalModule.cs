using Discord;
using Discord.Interactions;
using NLog;
using System.Text.Json;
using XeniaBot.Shared.Schema.TinyFox;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("randomanimal", "Random Animal Pic")]
public class RandomAnimalModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
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
            var response = new HttpClient().GetAsync(jsonUrl).Result;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var substr = content[..Math.Min(content.Length, 900)];
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                {
                    Color = Color.Red,
                    Description = $"Failed to fetch image\n```\n{substr}\n```"
                }.Build());
                return;
            }
            var stringContent = response.Content.ReadAsStringAsync().Result;
            var deserialized = JsonSerializer.Deserialize<TinyFoxImageModel>(stringContent, new JsonSerializerOptions()
            {
                IncludeFields = true
            });

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
            _log.Error(exception, $"animalType={animalType}");
            await Context.Interaction.RespondAsync($"A fatal error occoured. The developers have been notified\n```\n{exception.Message}\n```");
        }
    }
}
