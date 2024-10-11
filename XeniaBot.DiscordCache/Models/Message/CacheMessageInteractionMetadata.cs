using Discord;
using MongoDB.Bson;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageInteractionMetadata
{
    public ulong Snowflake { get; set; }
    /// <summary>
    /// Unix Timestamp (Seconds, UTC)
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; } = new(0);
    public InteractionType Type {get;set;}
    public ulong UserId {get;set;}
    public CacheUserModel? User { get; set; }
    public Dictionary<string, ulong> IntegrationOwners { get; set; } = [];
    public ulong? OriginalResponseMessageId { get; set; }

    public CacheMessageInteractionMetadata Update(IMessageInteractionMetadata interaction)
    {
        this.Snowflake = interaction.Id;
        this.CreatedAt = new BsonTimestamp(interaction.CreatedAt.ToUnixTimeSeconds());
        this.Type = interaction.Type;
        this.UserId = interaction.UserId;
        if (interaction.User == null)
        {
            User = null;
        }
        else
        {
            this.User = CacheUserModel.FromExisting(interaction.User);
        }
        if (interaction.IntegrationOwners != null)
        {
            var d = new Dictionary<string, ulong>();
            foreach (var (k,v) in interaction.IntegrationOwners)
            {
                d[k.ToString()] = v;
            }
            IntegrationOwners = d;
        }
        else
        {
            IntegrationOwners = [];
        }

        this.OriginalResponseMessageId = interaction.OriginalResponseMessageId.HasValue
            ? interaction.OriginalResponseMessageId
            : null;
        return this;
    }
    public static CacheMessageInteractionMetadata? FromExisting(IMessageInteractionMetadata? interaction)
    {
        if (interaction == null)
            return null;
        var instance = new CacheMessageInteractionMetadata();
        return instance.Update(interaction);
    }
}