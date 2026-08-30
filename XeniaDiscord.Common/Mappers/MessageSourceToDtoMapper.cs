using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models;

namespace XeniaDiscord.Common.Mappers;

public class MessageSourceToDtoMapper
    : IMapper<MessageSource, DiscordMessageSourceDto>
{
    public static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<MessageSourceToDtoMapper>()
            .AddSingleton<IMapper<MessageSource, DiscordMessageSourceDto>, MessageSourceToDtoMapper>();
    }
    public DiscordMessageSourceDto Map(MessageSource value)
        => value switch
        {
            MessageSource.System => DiscordMessageSourceDto.System,
            MessageSource.User => DiscordMessageSourceDto.User,
            MessageSource.Bot => DiscordMessageSourceDto.Bot,
            MessageSource.Webhook => DiscordMessageSourceDto.Webhook,
            _ => default
        };
}