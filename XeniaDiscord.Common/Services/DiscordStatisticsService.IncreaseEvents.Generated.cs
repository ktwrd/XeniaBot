using Discord;
using Discord.Rest;
using Discord.WebSocket;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Common.Services;

partial class DiscordStatisticsService
{
    protected void InitializeIncreaseEvents()
    {
        _client.﻿ApplicationCommandCreated += ClientIncOn﻿ApplicationCommandCreated;
        _client.ApplicationCommandDeleted += ClientIncOnApplicationCommandDeleted;
        _client.ApplicationCommandUpdated += ClientIncOnApplicationCommandUpdated;
        _client.AuditLogCreated += ClientIncOnAuditLogCreated;
        _client.AutocompleteExecuted += ClientIncOnAutocompleteExecuted;
        _client.AutoModActionExecuted += ClientIncOnAutoModActionExecuted;
        _client.AutoModRuleCreated += ClientIncOnAutoModRuleCreated;
        _client.AutoModRuleDeleted += ClientIncOnAutoModRuleDeleted;
        _client.AutoModRuleUpdated += ClientIncOnAutoModRuleUpdated;
        _client.ButtonExecuted += ClientIncOnButtonExecuted;
        _client.ChannelCreated += ClientIncOnChannelCreated;
        _client.ChannelDestroyed += ClientIncOnChannelDestroyed;
        _client.ChannelUpdated += ClientIncOnChannelUpdated;
        _client.CurrentUserUpdated += ClientIncOnCurrentUserUpdated;
        _client.EntitlementCreated += ClientIncOnEntitlementCreated;
        _client.EntitlementDeleted += ClientIncOnEntitlementDeleted;
        _client.EntitlementUpdated += ClientIncOnEntitlementUpdated;
        _client.GuildAvailable += ClientIncOnGuildAvailable;
        _client.GuildJoinRequestDeleted += ClientIncOnGuildJoinRequestDeleted;
        _client.GuildMembersDownloaded += ClientIncOnGuildMembersDownloaded;
        _client.GuildMemberUpdated += ClientIncOnGuildMemberUpdated;
        _client.GuildScheduledEventCancelled += ClientIncOnGuildScheduledEventCancelled;
        _client.GuildScheduledEventCompleted += ClientIncOnGuildScheduledEventCompleted;
        _client.GuildScheduledEventCreated += ClientIncOnGuildScheduledEventCreated;
        _client.GuildScheduledEventStarted += ClientIncOnGuildScheduledEventStarted;
        _client.GuildScheduledEventUpdated += ClientIncOnGuildScheduledEventUpdated;
        _client.GuildScheduledEventUserAdd += ClientIncOnGuildScheduledEventUserAdd;
        _client.GuildScheduledEventUserRemove += ClientIncOnGuildScheduledEventUserRemove;
        _client.GuildStickerCreated += ClientIncOnGuildStickerCreated;
        _client.GuildStickerDeleted += ClientIncOnGuildStickerDeleted;
        _client.GuildStickerUpdated += ClientIncOnGuildStickerUpdated;
        _client.GuildUnavailable += ClientIncOnGuildUnavailable;
        _client.GuildUpdated += ClientIncOnGuildUpdated;
        _client.IntegrationCreated += ClientIncOnIntegrationCreated;
        _client.IntegrationDeleted += ClientIncOnIntegrationDeleted;
        _client.IntegrationUpdated += ClientIncOnIntegrationUpdated;
        _client.InteractionCreated += ClientIncOnInteractionCreated;
        _client.InviteCreated += ClientIncOnInviteCreated;
        _client.InviteDeleted += ClientIncOnInviteDeleted;
        _client.JoinedGuild += ClientIncOnJoinedGuild;
        _client.LeftGuild += ClientIncOnLeftGuild;
        _client.MessageCommandExecuted += ClientIncOnMessageCommandExecuted;
        _client.MessageDeleted += ClientIncOnMessageDeleted;
        _client.MessageReceived += ClientIncOnMessageReceived;
        _client.MessagesBulkDeleted += ClientIncOnMessagesBulkDeleted;
        _client.MessageUpdated += ClientIncOnMessageUpdated;
        _client.ModalSubmitted += ClientIncOnModalSubmitted;
        _client.PollVoteAdded += ClientIncOnPollVoteAdded;
        _client.PollVoteRemoved += ClientIncOnPollVoteRemoved;
        _client.PresenceUpdated += ClientIncOnPresenceUpdated;
        _client.ReactionAdded += ClientIncOnReactionAdded;
        _client.ReactionRemoved += ClientIncOnReactionRemoved;
        _client.ReactionsCleared += ClientIncOnReactionsCleared;
        _client.ReactionsRemovedForEmote += ClientIncOnReactionsRemovedForEmote;
        _client.RecipientAdded += ClientIncOnRecipientAdded;
        _client.RecipientRemoved += ClientIncOnRecipientRemoved;
        _client.RequestToSpeak += ClientIncOnRequestToSpeak;
        _client.RoleCreated += ClientIncOnRoleCreated;
        _client.RoleDeleted += ClientIncOnRoleDeleted;
        _client.RoleUpdated += ClientIncOnRoleUpdated;
        _client.SelectMenuExecuted += ClientIncOnSelectMenuExecuted;
        _client.SlashCommandExecuted += ClientIncOnSlashCommandExecuted;
        _client.SpeakerAdded += ClientIncOnSpeakerAdded;
        _client.SpeakerRemoved += ClientIncOnSpeakerRemoved;
        _client.StageEnded += ClientIncOnStageEnded;
        _client.StageStarted += ClientIncOnStageStarted;
        _client.StageUpdated += ClientIncOnStageUpdated;
        _client.SubscriptionCreated += ClientIncOnSubscriptionCreated;
        _client.SubscriptionDeleted += ClientIncOnSubscriptionDeleted;
        _client.SubscriptionUpdated += ClientIncOnSubscriptionUpdated;
        _client.ThreadCreated += ClientIncOnThreadCreated;
        _client.ThreadDeleted += ClientIncOnThreadDeleted;
        _client.ThreadMemberJoined += ClientIncOnThreadMemberJoined;
        _client.ThreadMemberLeft += ClientIncOnThreadMemberLeft;
        _client.ThreadUpdated += ClientIncOnThreadUpdated;
        _client.UserBanned += ClientIncOnUserBanned;
        _client.UserCommandExecuted += ClientIncOnUserCommandExecuted;
        _client.UserIsTyping += ClientIncOnUserIsTyping;
        _client.UserJoined += ClientIncOnUserJoined;
        _client.UserLeft += ClientIncOnUserLeft;
        _client.UserUnbanned += ClientIncOnUserUnbanned;
        _client.UserUpdated += ClientIncOnUserUpdated;
        _client.UserVoiceStateUpdated += ClientIncOnUserVoiceStateUpdated;
        _client.VoiceChannelStatusUpdated += ClientIncOnVoiceChannelStatusUpdated;
        _client.VoiceServerUpdated += ClientIncOnVoiceServerUpdated;
        _client.WebhooksUpdated += ClientIncOnWebhooksUpdated;
    }

    private Task ClientIncOn﻿ApplicationCommandCreated(SocketApplicationCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.﻿ApplicationCommandCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnApplicationCommandDeleted(SocketApplicationCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ApplicationCommandDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnApplicationCommandUpdated(SocketApplicationCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ApplicationCommandUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAuditLogCreated(SocketAuditLogEntry arg1, SocketGuild arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.AuditLogCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAutocompleteExecuted(SocketAutocompleteInteraction arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.AutocompleteExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAutoModActionExecuted(SocketGuild arg1, AutoModRuleAction arg2, AutoModActionExecutedData arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.AutoModActionExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAutoModRuleCreated(SocketAutoModRule arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.AutoModRuleCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAutoModRuleDeleted(SocketAutoModRule arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.AutoModRuleDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnAutoModRuleUpdated(Cacheable<SocketAutoModRule, ulong> arg1, SocketAutoModRule arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.AutoModRuleUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnButtonExecuted(SocketMessageComponent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ButtonExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnChannelCreated(SocketChannel arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ChannelCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnChannelDestroyed(SocketChannel arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ChannelDestroyed);
        return Task.CompletedTask;
    }
    private Task ClientIncOnChannelUpdated(SocketChannel arg1, SocketChannel arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.ChannelUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnCurrentUserUpdated(SocketSelfUser arg1, SocketSelfUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.CurrentUserUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnEntitlementCreated(SocketEntitlement arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.EntitlementCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnEntitlementDeleted(Cacheable<SocketEntitlement, ulong> arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.EntitlementDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnEntitlementUpdated(Cacheable<SocketEntitlement, ulong> arg1, SocketEntitlement arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.EntitlementUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildAvailable(SocketGuild arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildAvailable);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildJoinRequestDeleted(Cacheable<SocketGuildUser, ulong> arg1, SocketGuild arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildJoinRequestDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildMembersDownloaded(SocketGuild arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildMembersDownloaded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildMemberUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventCancelled(SocketGuildEvent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventCancelled);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventCompleted(SocketGuildEvent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventCompleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventCreated(SocketGuildEvent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventStarted(SocketGuildEvent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventStarted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventUpdated(Cacheable<SocketGuildEvent,ulong> arg1, SocketGuildEvent arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventUserAdd(Cacheable<SocketUser, RestUser, IUser, ulong> arg1, SocketGuildEvent arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventUserAdd);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildScheduledEventUserRemove(Cacheable<SocketUser, RestUser, IUser, ulong> arg1, SocketGuildEvent arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildScheduledEventUserRemove);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildStickerCreated(SocketCustomSticker arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildStickerCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildStickerDeleted(SocketCustomSticker arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildStickerDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildStickerUpdated(SocketCustomSticker arg1, SocketCustomSticker arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildStickerUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildUnavailable(SocketGuild arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildUnavailable);
        return Task.CompletedTask;
    }
    private Task ClientIncOnGuildUpdated(SocketGuild arg1, SocketGuild arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.GuildUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnIntegrationCreated(IIntegration arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.IntegrationCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnIntegrationDeleted(IGuild arg1, ulong arg2, Optional<ulong> arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.IntegrationDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnIntegrationUpdated(IIntegration arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.IntegrationUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnInteractionCreated(SocketInteraction arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.InteractionCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnInviteCreated(SocketInvite arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.InviteCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnInviteDeleted(SocketGuildChannel arg1, string arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.InviteDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnJoinedGuild(SocketGuild arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.JoinedGuild);
        return Task.CompletedTask;
    }
    private Task ClientIncOnLeftGuild(SocketGuild arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.LeftGuild);
        return Task.CompletedTask;
    }
    private Task ClientIncOnMessageCommandExecuted(SocketMessageCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.MessageCommandExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnMessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.MessageDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnMessageReceived(SocketMessage arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.MessageReceived);
        return Task.CompletedTask;
    }
    private Task ClientIncOnMessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.MessagesBulkDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.MessageUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnModalSubmitted(SocketModal arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ModalSubmitted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnPollVoteAdded(Cacheable<IUser, ulong> arg1, Cacheable<ISocketMessageChannel, IRestMessageChannel, IMessageChannel, ulong> arg2, Cacheable<IUserMessage, ulong> arg3, Cacheable<SocketGuild, RestGuild, IGuild, ulong>? arg4, ulong arg5)
    {
        IncreaseEvent(DiscordStatisticsEventType.PollVoteAdded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnPollVoteRemoved(Cacheable<IUser, ulong> arg1, Cacheable<ISocketMessageChannel, IRestMessageChannel, IMessageChannel, ulong> arg2, Cacheable<IUserMessage, ulong> arg3, Cacheable<SocketGuild, RestGuild, IGuild, ulong>? arg4, ulong arg5)
    {
        IncreaseEvent(DiscordStatisticsEventType.PollVoteRemoved);
        return Task.CompletedTask;
    }
    private Task ClientIncOnPresenceUpdated(SocketUser arg1, SocketPresence arg2, SocketPresence arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.PresenceUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.ReactionAdded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.ReactionRemoved);
        return Task.CompletedTask;
    }
    private Task ClientIncOnReactionsCleared(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.ReactionsCleared);
        return Task.CompletedTask;
    }
    private Task ClientIncOnReactionsRemovedForEmote(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, IEmote arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.ReactionsRemovedForEmote);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRecipientAdded(SocketGroupUser arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.RecipientAdded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRecipientRemoved(SocketGroupUser arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.RecipientRemoved);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRequestToSpeak(SocketStageChannel arg1, SocketGuildUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.RequestToSpeak);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRoleCreated(SocketRole arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.RoleCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRoleDeleted(SocketRole arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.RoleDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnRoleUpdated(SocketRole arg1, SocketRole arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.RoleUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSelectMenuExecuted(SocketMessageComponent arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.SelectMenuExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSlashCommandExecuted(SocketSlashCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.SlashCommandExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSpeakerAdded(SocketStageChannel arg1, SocketGuildUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.SpeakerAdded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSpeakerRemoved(SocketStageChannel arg1, SocketGuildUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.SpeakerRemoved);
        return Task.CompletedTask;
    }
    private Task ClientIncOnStageEnded(SocketStageChannel arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.StageEnded);
        return Task.CompletedTask;
    }
    private Task ClientIncOnStageStarted(SocketStageChannel arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.StageStarted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnStageUpdated(SocketStageChannel arg1, SocketStageChannel arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.StageUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSubscriptionCreated(SocketSubscription arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.SubscriptionCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSubscriptionDeleted(Cacheable<SocketSubscription, ulong> arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.SubscriptionDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnSubscriptionUpdated(Cacheable<SocketSubscription, ulong> arg1, SocketSubscription arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.SubscriptionUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnThreadCreated(SocketThreadChannel arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ThreadCreated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnThreadDeleted(Cacheable<SocketThreadChannel, ulong> arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ThreadDeleted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnThreadMemberJoined(SocketThreadUser arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ThreadMemberJoined);
        return Task.CompletedTask;
    }
    private Task ClientIncOnThreadMemberLeft(SocketThreadUser arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.ThreadMemberLeft);
        return Task.CompletedTask;
    }
    private Task ClientIncOnThreadUpdated(Cacheable<SocketThreadChannel, ulong> arg1, SocketThreadChannel arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.ThreadUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserBanned(SocketUser arg1, SocketGuild arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserBanned);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserCommandExecuted(SocketUserCommand arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserCommandExecuted);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserIsTyping(Cacheable<IUser, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserIsTyping);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserJoined(SocketGuildUser arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserJoined);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserLeft(SocketGuild arg1, SocketUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserLeft);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserUnbanned(SocketUser arg1, SocketGuild arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserUnbanned);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserUpdated(SocketUser arg1, SocketUser arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnUserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.UserVoiceStateUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnVoiceChannelStatusUpdated(Cacheable<SocketVoiceChannel, ulong> arg1, string arg2, string arg3)
    {
        IncreaseEvent(DiscordStatisticsEventType.VoiceChannelStatusUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnVoiceServerUpdated(SocketVoiceServer arg1)
    {
        IncreaseEvent(DiscordStatisticsEventType.VoiceServerUpdated);
        return Task.CompletedTask;
    }
    private Task ClientIncOnWebhooksUpdated(SocketGuild arg1, SocketChannel arg2)
    {
        IncreaseEvent(DiscordStatisticsEventType.WebhooksUpdated);
        return Task.CompletedTask;
    }
}