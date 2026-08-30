using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildRoleColorSnapshotModel
{
    public const string TableName = "Snapshot_GuildRole_Color";

    public GuildRoleColorSnapshotModel()
    {
        GuildRoleSnapshotId = Guid.Empty;
        RoleId = "0";
        R = 0;
        G = 0;
        B = 0;
    }

    public GuildRoleColorSnapshotModel(RoleColors colors) : this()
    {
        R = colors.PrimaryColor.R;
        G = colors.PrimaryColor.G;
        B = colors.PrimaryColor.B;
        if (colors.SecondaryColor.HasValue)
        {
            SecondaryRed = colors.SecondaryColor.Value.R;
            SecondaryGreen = colors.SecondaryColor.Value.G;
            SecondaryBlue = colors.SecondaryColor.Value.B;
        }
        if (colors.TertiaryColor.HasValue)
        {
            TertiaryRed = colors.TertiaryColor.Value.R;
            TertiaryGreen = colors.TertiaryColor.Value.G;
            TertiaryBlue = colors.TertiaryColor.Value.B;
        }
    }

    /// <summary>
    /// Primary Key and Foreign Key to <see cref="GuildRoleSnapshotModel.Id"/>
    /// </summary>
    public Guid GuildRoleSnapshotId { get; set; }

    /// <summary>
    /// (assumed to be) Foreign Key to <see cref="Cache.GuildRoleCacheModel.RoleId"/>
    /// Also ulong as string.
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; }

    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public bool IsHolographic => ToRoleColors().IsHolographic;
    public bool IsGradient => HasSecondary && !HasTertiary;
    public bool IsSolid => !HasSecondary && !HasTertiary;

    public bool HasSecondary => SecondaryRed.HasValue && SecondaryGreen.HasValue && SecondaryBlue.HasValue;
    public bool HasTertiary => TertiaryRed.HasValue && TertiaryGreen.HasValue && TertiaryBlue.HasValue;

    public byte? SecondaryRed { get; set; }
    public byte? SecondaryGreen { get; set; }
    public byte? SecondaryBlue { get; set; }
    public byte? TertiaryRed { get; set; }
    public byte? TertiaryGreen { get; set; }
    public byte? TertiaryBlue { get; set; }

    public RoleColors ToRoleColors()
        => new(GetPrimaryColor(), GetSecondaryColor(), GetTertiaryColor());

    public Color GetPrimaryColor()
        => new(R, G, B);

    public Color? GetSecondaryColor()
    {
        if (HasSecondary)
        {
            return new Color(SecondaryRed!.Value, SecondaryGreen!.Value, SecondaryBlue!.Value);
        }
        return null;
    }

    public Color? GetTertiaryColor()
    {
        if (HasTertiary)
        {
            return new Color(TertiaryRed!.Value, TertiaryGreen!.Value, TertiaryBlue!.Value);
        }

        return null;
    }
}