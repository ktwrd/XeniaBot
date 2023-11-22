using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageInteraction
{
    public ulong MessageInteractionId { get; set; }
    public InteractionType Type { get; set; }
    public string Name { get; set; }
    public CacheUserModel? User { get; set; }

    public CacheMessageInteraction Update(IMessageInteraction interaction)
    {
        MessageInteractionId = interaction.Id;
        Type = interaction.Type;
        Name = interaction.Name;
        User = CacheUserModel.FromExisting(interaction.User);
        return this;
    }
    public static CacheMessageInteraction? FromExisting(IMessageInteraction? interaction)
    {
        if (interaction == null)
            return null;
        var instance = new CacheMessageInteraction();
        return instance.Update(interaction);
    }
}