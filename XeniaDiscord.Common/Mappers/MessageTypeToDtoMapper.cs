using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models;

namespace XeniaDiscord.Common.Mappers;

public class MessageTypeToDtoMapper
    : IMapper<MessageType, DiscordMessageTypeDto>
{
    public static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<MessageTypeToDtoMapper>()
            .AddSingleton<IMapper<MessageType, DiscordMessageTypeDto>, MessageTypeToDtoMapper>();
    }
    public DiscordMessageTypeDto Map(MessageType value)
        => value switch
        {
            MessageType.Default => DiscordMessageTypeDto.Default,
            MessageType.RecipientAdd => DiscordMessageTypeDto.RecipientAdd,
            MessageType.RecipientRemove => DiscordMessageTypeDto.RecipientRemove,
            MessageType.Call => DiscordMessageTypeDto.Call,
            MessageType.ChannelNameChange => DiscordMessageTypeDto.ChannelNameChange,
            MessageType.ChannelIconChange => DiscordMessageTypeDto.ChannelIconChange,
            MessageType.ChannelPinnedMessage => DiscordMessageTypeDto.ChannelPinnedMessage,
            MessageType.GuildMemberJoin => DiscordMessageTypeDto.GuildMemberJoin,
            MessageType.UserPremiumGuildSubscription => DiscordMessageTypeDto.UserPremiumGuildSubscription,
            MessageType.UserPremiumGuildSubscriptionTier1 => DiscordMessageTypeDto.UserPremiumGuildSubscriptionTier1,
            MessageType.UserPremiumGuildSubscriptionTier2 => DiscordMessageTypeDto.UserPremiumGuildSubscriptionTier2,
            MessageType.UserPremiumGuildSubscriptionTier3 => DiscordMessageTypeDto.UserPremiumGuildSubscriptionTier3,
            MessageType.ChannelFollowAdd => DiscordMessageTypeDto.ChannelFollowAdd,
            MessageType.GuildDiscoveryDisqualified => DiscordMessageTypeDto.GuildDiscoveryDisqualified,
            MessageType.GuildDiscoveryRequalified => DiscordMessageTypeDto.GuildDiscoveryRequalified,
            MessageType.GuildDiscoveryGracePeriodInitialWarning => DiscordMessageTypeDto.GuildDiscoveryGracePeriodInitialWarning,
            MessageType.GuildDiscoveryGracePeriodFinalWarning => DiscordMessageTypeDto.GuildDiscoveryGracePeriodFinalWarning,
            MessageType.ThreadCreated => DiscordMessageTypeDto.ThreadCreated,
            MessageType.Reply => DiscordMessageTypeDto.Reply,
            MessageType.ApplicationCommand => DiscordMessageTypeDto.ApplicationCommand,
            MessageType.ThreadStarterMessage => DiscordMessageTypeDto.ThreadStarterMessage,
            MessageType.GuildInviteReminder => DiscordMessageTypeDto.GuildInviteReminder,
            MessageType.ContextMenuCommand => DiscordMessageTypeDto.ContextMenuCommand,
            MessageType.AutoModerationAction => DiscordMessageTypeDto.AutoModerationAction,
            MessageType.RoleSubscriptionPurchase => DiscordMessageTypeDto.RoleSubscriptionPurchase,
            MessageType.InteractionPremiumUpsell => DiscordMessageTypeDto.InteractionPremiumUpsell,
            MessageType.StageStart => DiscordMessageTypeDto.StageStart,
            MessageType.StageEnd => DiscordMessageTypeDto.StageEnd,
            MessageType.StageSpeaker => DiscordMessageTypeDto.StageSpeaker,
            MessageType.StageRaiseHand => DiscordMessageTypeDto.StageRaiseHand,
            MessageType.StageTopic => DiscordMessageTypeDto.StageTopic,
            MessageType.GuildApplicationPremiumSubscription => DiscordMessageTypeDto.GuildApplicationPremiumSubscription,
            MessageType.IncidentAlertModeEnabled => DiscordMessageTypeDto.IncidentAlertModeEnabled,
            MessageType.IncidentAlertModeDisabled => DiscordMessageTypeDto.IncidentAlertModeDisabled,
            MessageType.IncidentReportRaid => DiscordMessageTypeDto.IncidentReportRaid,
            MessageType.IncidentReportFalseAlarm => DiscordMessageTypeDto.IncidentReportFalseAlarm,
            MessageType.PurchaseNotification => DiscordMessageTypeDto.PurchaseNotification,
            MessageType.PollResult => DiscordMessageTypeDto.PollResult,
            _ => default
        };
}