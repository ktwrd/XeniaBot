using System;
using System.Linq;
using CSharpFunctionalExtensions;
using Humanizer;
using XeniaDiscord.Common;

namespace XeniaBot.WebPanel.Models;

public class RolePillComponentModel
{
    private readonly Guid _id = Guid.NewGuid();
    public Guid Guid => _id;
    public string ElementId => $"role_pill_{Role.Id}_{Guid}";
    public required StrippedRole Role { get; set; }
    public RolePillComponentTooltipMode TooltipMode { get; set; } = RolePillComponentTooltipMode.RoleId;
    public string? CustomTooltipText { get; set; }

    public Maybe<RolePillInputOptions> InputOptions { get; set; }

    public string GetBadgeStyle()
    {
        var cssColor = Role.RoleUnicolour.ToCss(100.0);
        if (cssColor == "transparent") return "background-color: RGBA(var(--bs-dark-rgb), var(--bs-bg-opacity, 1)) !important;";
        return $"background-color: {cssColor} !important;";
    }
    public string GetTooltipText()
    {
        var tooltipText = string.Empty;
        switch (TooltipMode)
        {
            case RolePillComponentTooltipMode.RoleIdAndPermissions:
                tooltipText = $"<code>{Role.Id}</code>";
                Role.Permissions.Tap(gp =>
                {
                    if (gp.RawValue == 0)
                    {
                        tooltipText += "<br/><strong>No permissions</strong>";
                        return;
                    }

                    tooltipText += "<br/><hr/><strong>Permissions:</strong><br/>";
                    tooltipText += string.Join(", ", gp.ToList().Select(e => e.Humanize(LetterCasing.Title)));
                });
                break;
            case RolePillComponentTooltipMode.RoleId:
                tooltipText = $"<code>{Role.Id}</code>";
                break;
            case RolePillComponentTooltipMode.Custom:
                if (!string.IsNullOrEmpty(CustomTooltipText?.Trim())) tooltipText = CustomTooltipText;
                break;
        }

        return tooltipText;
    }
}

public class RolePillInputOptions
{
    public required string Name { get; set; }
    public required string Value { get; set; }
    public bool AllowRemoval { get; set; }
}

public enum RolePillComponentTooltipMode
{
    None,
    RoleId,
    RoleIdAndPermissions,
    Custom
}