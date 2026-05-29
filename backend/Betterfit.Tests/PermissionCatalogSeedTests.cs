using Betterfit.Data;

namespace Betterfit.Tests;

public sealed class PermissionCatalogSeedTests
{
    [Fact]
    public void DefaultRoleTemplates_IncludeCrmReportsStaffAssignmentSecurityIntegrationAndWorkoutPermissions()
    {
        var templates = PermissionCatalogSeed.GetDefaultRoleTemplates();
        var owner = templates.Single(template => template.Name == "Owner");
        var manager = templates.Single(template => template.Name == "Manager");
        var reception = templates.Single(template => template.Name == "Reception");
        var support = templates.Single(template => template.Name == "Support Operator");

        Assert.Contains(owner.Permissions, permission => permission == ("staff_assignments", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("staff_assignments", "write"));
        Assert.Contains(owner.Permissions, permission => permission == ("crm", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("crm", "write"));
        Assert.Contains(owner.Permissions, permission => permission == ("reports", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("security_policy", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("security_policy", "write"));
        Assert.Contains(owner.Permissions, permission => permission == ("integrations", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("integrations", "write"));
        Assert.Contains(owner.Permissions, permission => permission == ("checkins", "approve"));
        Assert.Contains(owner.Permissions, permission => permission == ("classes", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("classes", "write"));
        Assert.Contains(owner.Permissions, permission => permission == ("workouts", "read"));
        Assert.Contains(owner.Permissions, permission => permission == ("workouts", "write"));
        Assert.Contains(manager.Permissions, permission => permission == ("staff_assignments", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("crm", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("crm", "write"));
        Assert.Contains(manager.Permissions, permission => permission == ("reports", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("security_policy", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("integrations", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("integrations", "write"));
        Assert.Contains(manager.Permissions, permission => permission == ("billing", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("billing", "write"));
        Assert.Contains(manager.Permissions, permission => permission == ("checkins", "approve"));
        Assert.Contains(manager.Permissions, permission => permission == ("workouts", "read"));
        Assert.Contains(manager.Permissions, permission => permission == ("workouts", "write"));
        Assert.Contains(reception.Permissions, permission => permission == ("billing", "read"));
        Assert.Contains(reception.Permissions, permission => permission == ("billing", "write"));
        Assert.Contains(reception.Permissions, permission => permission == ("checkins", "approve"));
        Assert.Contains(reception.Permissions, permission => permission == ("crm", "read"));
        Assert.Contains(reception.Permissions, permission => permission == ("crm", "write"));
        Assert.Contains(support.Permissions, permission => permission == ("reports", "read"));
        Assert.Contains(support.Permissions, permission => permission == ("integrations", "read"));
    }
}
