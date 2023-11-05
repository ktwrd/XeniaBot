using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheOverwrite
{
    public ulong TargetId { get; set; }
    public PermissionTarget TargetType { get; set; }
    public CacheOverwritePermissions Permissions { get; set; }

    public CacheOverwrite FromOverwrite(Overwrite overwrite)
    {
        this.TargetId = overwrite.TargetId;
        this.TargetType = overwrite.TargetType;
        this.Permissions = CacheOverwritePermissions.FromExisting(overwrite.Permissions);
        return this;
    }
    public static CacheOverwrite FromExisting(Overwrite overwrite)
    {
        var instance = new CacheOverwrite();
        return instance.FromOverwrite(overwrite);
    }
}