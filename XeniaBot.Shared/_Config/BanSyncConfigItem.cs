﻿namespace XeniaBot.Shared;

public class BanSyncConfigItem
{
    public ulong GuildId { get; set; }
    public ulong LogChannelId { get; set; }
    public ulong RequestChannelId { get; set; }

    public static BanSyncConfigItem Default(BanSyncConfigItem? i = null)
    {
        i ??= new BanSyncConfigItem();
        i.GuildId = default;
        i.LogChannelId = default;
        i.RequestChannelId = default;
        return i;
    }
}