using Discord;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace XeniaDiscord.Shared;

public static class Identifier
{
    public static class Event
    {
        private const string EventBase = "ns:event";
        public static class Discord
        {
            private const string Base = EventBase + ":discord";

            public const string Interaction = Base + ":interaction";
            public const string ModalInteraction = Interaction + ":interaction";
        }

        public static class Xenia
        {
            private const string Base = EventBase + ":xenia";

            public const string Health = Base + ":health";
            public const string Exception = Base + ":exception";
        }
    }

    public static class Feature
    {
        private const string FeatureBase = "ns:feature:xenia";

        public const string BanSync = FeatureBase + ":bansync";
        public const string WarnSystem = FeatureBase + ":warnsys";

        public static class ServerLog
        {
            private const string ServerLogBase = FeatureBase + ":serverlog";
            public const string Messages = ServerLogBase + ":messages";
        }
    }

    public static readonly IReadOnlyDictionary<string, GuildPermission> RequiredPermissions = new Dictionary<string, GuildPermission>()
    {
        {
            Feature.BanSync,
            GuildPermission.BanMembers | GuildPermission.ViewAuditLog | GuildPermission.SendMessages | GuildPermission.EmbedLinks
        }
    }.ToImmutableDictionary();
}
