using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageInteraction
{
    public ulong Id { get; set; }
    public InteractionType Type { get; set; }
    public string Name { get; set; }
    public CacheUserModel User { get; set; }

    public static CacheMessageInteraction FromInteraction(IMessageInteraction interaction)
    {
        var instance = new CacheMessageInteraction();
        instance.Id = interaction.Id;
        instance.Type = interaction.Type;
        instance.Name = interaction.Name;
        instance.User = CacheUserModel.FromUser(interaction.User);
        return instance;
    }
}