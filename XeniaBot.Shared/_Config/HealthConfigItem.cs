﻿namespace XeniaBot.Shared;

public class HealthConfigItem
{
    public bool Enable { get; set; }
    public int Port { get; set; }

    public static HealthConfigItem Default(HealthConfigItem? i = null)
    {
        i ??= new HealthConfigItem();
        i.Enable = false;
        i.Port = 4829;
        return i;
    }

    public HealthConfigItem()
    {
        Default(this);
    }
}