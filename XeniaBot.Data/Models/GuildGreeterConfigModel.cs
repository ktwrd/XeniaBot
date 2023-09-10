using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class GuildGreeterConfigModel : BaseModel
{
    public static string CollectionName => "guildGreeterConfig";
    
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    
    
    public string? T_Title { get; set; }
    public string? T_Description { get; set; }
    public string? T_Url { get; set; }
    public string? T_ThumbnailUrl { get; set; }
    public string? T_ImageUrl { get; set; }
    public uint? T_Color { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    public string? T_Color_Hex
    {
        get
        {
            var discordColor = new Discord.Color(T_Color ?? 0);
            string res = "";
            res += discordColor.R.ToString("X").PadLeft(2, '0');
            res += discordColor.G.ToString("X").PadLeft(2, '0');
            res += discordColor.B.ToString("X").PadLeft(2, '0');
            return $"#{res}";
        }
        set
        {
            string v = value ?? "#000000";
            int offset = 0;
            if (v.StartsWith("#"))
                offset = 1;
            uint val = uint.Parse(v.Substring(offset), NumberStyles.HexNumber);
            T_Color = val;
        }
    }
    public string? T_AuthorName { get; set; }
    public string? T_AuthorIconUrl { get; set; }
    public string? T_AuthorUrl { get; set; }
    public string? T_FooterText { get; set; }
    public string? T_FooterUrl { get; set; }
    public string? T_FooterImgUrl { get; set; }

    public EmbedBuilder GetEmbed(SocketGuildUser user)
    {
        var embed = new EmbedBuilder();

        var replaceDict = new Dictionary<string, object>()
        {
            {"userId", user.Id},
            {"guildId", user.Guild.Id},
            {"username", user.Username},
            {"mention", $"<@{user.Id}>"},
            {"guildName", user.Guild.Name}
        };

        string InjectDict(string i)
        {
            string m = i;
            if (i.Length > 0 && i != null)
            {
                foreach (var pair in replaceDict)
                {
                    m = i.Replace("{" + pair.Key + "}", pair.Value.ToString());
                }
            }

            return m;
        }

        if (T_Title?.Length > 0)
            embed.WithTitle(InjectDict(T_Title));
        if (T_Description?.Length > 0)
            embed.WithDescription(InjectDict(T_Description));
        if (T_Url?.Length > 0)
            embed.WithUrl(T_Url);
        if (T_ThumbnailUrl?.Length > 0)
            embed.WithThumbnailUrl(T_ThumbnailUrl);
        if (T_ImageUrl?.Length > 0)
            embed.WithImageUrl(T_ImageUrl);
        embed.WithColor(new Discord.Color(T_Color ?? 0));
        if (T_AuthorName != null)
        {
            embed.WithAuthor(InjectDict(T_AuthorName), T_AuthorIconUrl, T_AuthorUrl);
        }

        if (T_FooterText != null)
        {
            embed.WithFooter(InjectDict(T_FooterText), T_FooterUrl);
        }
        
        return embed;
    }
    
    public bool Enable { get; set; }
    public long ModifiedAtTimestamp { get; set; }

    public GuildGreeterConfigModel()
    {
        T_Description = "Welcome {mention}";
    }
}