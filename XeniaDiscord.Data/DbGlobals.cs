namespace XeniaDiscord.Data;

public static class DbGlobals
{
    public const string EmptyGuid = "00000000-0000-0000-0000-000000000000";
    public const int GuidLength = 36;
    public const int UnsignedLongStringLength = 24;
    public const int MaxStringSize = 4000;
    public static class MaxLength
    {
        public const int ULong = 24;
        public const int Confession = 2000;
        public static class Discord
        {
            public const int RoleName = 100;
            public const int GuildName = 100;
            public const int DisplayName = 100;
            public const int Username = 32 + 4;
            public const int BanReason = 1000;
        }
    }
}
