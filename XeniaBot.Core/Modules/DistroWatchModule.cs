using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Interactions;

namespace XeniaBot.Core.Modules;

[Group("distrowatch", "Get information about a specific distribution")]
public class DistroWatchModule : InteractionModuleBase
{
    [SlashCommand("random", "Get information about a random distro")]
    public async Task Random()
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("DistroWatch - Random");
        var client = new HttpClient();
        var url = $"https://api.redfur.cloud/dw/random";
        var response = await client.GetAsync(url);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var deser = JsonSerializer.Deserialize<DistroWatchResponseItem[]>(stringContent, Program.SerializerOptions);
            if (deser is  { Length: > 0 })
            {
                var first = deser.FirstOrDefault();
                if (first == null)
                {
                    embed.WithDescription($"Failed to fetch!").WithColor(Color.Red);
                }
                else
                {
                    embed = GenerateEmbed(first).WithTitle("DistroWatch - Random");
                }
            }
            else
            {
                embed.WithDescription($"Failed to fetch! No results found")
                    .WithColor(Color.Red);
            }
        }
        else
        {
            embed.WithDescription($"Failed to fetch! `{response.StatusCode}`")
                .WithColor(Color.Red);
        }

        await FollowupAsync(embed: embed.Build());
    }

    public EmbedBuilder GenerateEmbed(DistroWatchResponseItem item)
    {
        return new EmbedBuilder()
            .WithDescription(string.Join("\n", new string[]
            {
                $"Name: `{item.Name}`",
                $"Architecture: `{item.Arch}`" ,
                $"Based on: `{string.Join(", ", item.BasedOn)}`",
                $"Latest version: `{item.LatestVersion}`",
                $"Status: `{item.Status}`",
                $"https://distrowatch.com/table.php?distribution={HttpUtility.UrlEncode(item.Name)}"
            }))
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
    }
}
public class DistroWatchResponseItem
{
    [JsonPropertyName("arch")]
    public string Arch { get; set; }
    [JsonPropertyName("basedon")]
    public string[] BasedOn { get; set; }
    [JsonPropertyName("category")]
    public string Category { get; set; }
    [JsonPropertyName("desc")]
    public string Description { get; set; }
    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("origin")]
    public string Origin { get; set; }
    [JsonPropertyName("status")]
    public string Status { get; set; }
}