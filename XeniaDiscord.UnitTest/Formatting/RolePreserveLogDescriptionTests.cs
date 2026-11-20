using XeniaDiscord.Common.Services;
using XeniaDiscord.Data.Models.RolePreserve;

namespace XeniaDiscord.UnitTest.Formatting;

public class RolePreserveLogDescriptionTests
{
    [Test(Description = "Default (no roles)")]
    public void DefaultTest()
    {
        var model = new RolePreserveAuditModel();
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("No actions applied."));
    }
    
    [Test(Description = "Success=1")]
    public void SuccessTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                }
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("No actions applied."));
    }
    
    [Test(Description = "Success=2")]
    public void SuccessPluralTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                }
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("No actions applied."));
    }

    [Test(Description = "Success=1, SkippedBlacklisted=1")]
    public void SuccessAndSkipAsBlacklistTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedBlacklisted
                },
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("Successfully added 1 role and skipped 1 role."));
    }
    
    [Test(Description = "Success=1, SkippedRoleDoesNotExist=1")]
    public void SuccessAndSkipAsMissingTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedRoleDoesNotExist
                },
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("Successfully added 1 role and skipped 1 role."));
    }
    
    [Test(Description = "Success=1, SkippedBlacklisted=1")]
    public void SuccessAndSkipBlacklistedAndSkipAsMissingTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedBlacklisted
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedRoleDoesNotExist
                },
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("Successfully added 1 role and skipped 2 roles."));
    }
    
    [Test(Description = "SkippedBlacklisted=1, FailureUnknown=1")]
    public void SkipBlacklistedAndFailUnknownTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.FailureUnknown
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedBlacklisted
                }
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("Skipped 1 role and failed to add 1 role."));
    }
    
    [Test(Description = "SkippedBlacklisted=1, FailureUnknown=1")]
    public void SuccessAndSkipBlacklistedAndFailUnknownTest()
    {
        var model = new RolePreserveAuditModel()
        {
            AppliedRoles = [
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SuccessGrant
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.FailureUnknown
                },
                new RolePreserveAuditAppliedRoleModel()
                {
                    Action = RolePreserveAuditAppliedRoleAction.SkippedBlacklisted
                }
            ]
        };
        var text = RolePreserveLogService.FormatDescriptionText(model);
        Assert.That(text, Is.EqualTo("Successfully added 1 role, skipped 1 role, and failed to add 1 role."));
    }
}