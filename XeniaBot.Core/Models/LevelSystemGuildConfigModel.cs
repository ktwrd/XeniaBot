﻿namespace XeniaBot.Core.Models;

public class LevelSystemGuildConfigModel : BaseModel
{
    public ulong GuildId { get; set; }
    public ulong? LevelUpChannel { get; set; }
    public bool ShowLeveUpMessage { get; set; }

    public LevelSystemGuildConfigModel()
    {
        LevelUpChannel = null;
        ShowLeveUpMessage = true;
    }
}