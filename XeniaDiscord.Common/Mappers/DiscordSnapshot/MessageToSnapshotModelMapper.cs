using System.Globalization;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Mappers.DiscordSnapshot;

public class MessageToSnapshotModelMapper
    : IMapper<IMessage, MessageSnapshotModel>
{
    private readonly MessageTypeToDtoMapper _messageTypeMapper = new();
    private readonly MessageSourceToDtoMapper _messageSourceMapper = new ();
    public static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<MessageToSnapshotModelMapper>()
            .AddSingleton<IMapper<IMessage, MessageSnapshotModel>, MessageToSnapshotModelMapper>();
    }

    public MessageSnapshotModel Map(IMessage source) => MapInternal(source);
    private MessageSnapshotModel MapInternal(IMessage? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var recordId = Guid.NewGuid();
        var messageIdStr = message.Id.ToString("D", CultureInfo.InvariantCulture);
        var instance = new MessageSnapshotModel
        {
            RecordId = recordId,
            MessageId = messageIdStr,
            MessageCreatedAt = message.CreatedAt.UtcDateTime,
            MessageType = _messageTypeMapper.Map(message.Type),
            MessageSource = _messageSourceMapper.Map(message.Source),
        };

        return instance;
    }
}