using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheOverwritePermissions
{
    public ulong AllowValue { get; set; }
    public ulong DenyValue { get; set; }

    public OverwritePermissions ToOverwritePermissions()
    {
        return new OverwritePermissions(this.AllowValue, this.DenyValue);
    }

    public CacheOverwritePermissions Update(OverwritePermissions perm)
    {
        this.AllowValue = perm.AllowValue;
        this.DenyValue = perm.DenyValue;
        return this;
    }
    public static CacheOverwritePermissions FromExisting(OverwritePermissions perm)
    {
        var instance = new CacheOverwritePermissions();
        return instance.Update(perm);
    }
}