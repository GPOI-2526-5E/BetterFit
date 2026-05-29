using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Roles;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.StaffAssignments;

public sealed class StaffAssignmentService : IStaffAssignmentService
{
    private readonly AppDbContext _dbContext;

    public StaffAssignmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StaffAssignmentOperationResult> AssignAsync(
        Guid gymId,
        ApplicationUser user,
        StaffProfile staffProfile,
        GymRole role,
        TenantRoleAssignmentScopeType scopeType,
        Guid? scopeLocationId,
        TenantRoleAssignmentStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        var isOwnerRole = role.NormalizedName == DefaultGymRoleNames.Owner;

        if (isOwnerRole && scopeType != TenantRoleAssignmentScopeType.Tenant)
        {
            return StaffAssignmentOperationResult.Invalid(
                "invalid_owner_scope",
                "Owner assignments must be tenant-scoped.");
        }

        if (isOwnerRole && requestedStatus != TenantRoleAssignmentStatus.Active)
        {
            return StaffAssignmentOperationResult.Invalid(
                "invalid_owner_status",
                "Owner assignments must be active.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        if (isOwnerRole)
        {
            var existingOwners = await _dbContext.TenantRoleAssignments
                .Where(assignment =>
                    assignment.GymId == gymId
                    && assignment.IsPrimaryOwner
                    && assignment.Status == TenantRoleAssignmentStatus.Active
                    && assignment.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            if (existingOwners.Any(assignment => assignment.UserId == user.Id && assignment.RoleId == role.Id))
            {
                return StaffAssignmentOperationResult.Conflict(
                    "owner_already_assigned",
                    "This user is already the active owner for this tenant.");
            }

            foreach (var existingOwner in existingOwners)
            {
                existingOwner.Status = TenantRoleAssignmentStatus.Revoked;
                existingOwner.RevokedAtUtc = now;
            }
        }
        else
        {
            var duplicateActiveAssignment = await _dbContext.TenantRoleAssignments
                .AnyAsync(assignment =>
                    assignment.GymId == gymId
                    && assignment.UserId == user.Id
                    && assignment.RoleId == role.Id
                    && assignment.ScopeType == scopeType
                    && assignment.ScopeLocationId == scopeLocationId
                    && assignment.Status == TenantRoleAssignmentStatus.Active
                    && assignment.RevokedAtUtc == null,
                    cancellationToken);

            if (duplicateActiveAssignment)
            {
                return StaffAssignmentOperationResult.Conflict(
                    "duplicate_staff_assignment",
                    "This user already holds the same role and scope in this tenant.");
            }
        }

        var assignment = new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            UserId = user.Id,
            StaffProfileId = staffProfile.Id,
            RoleId = role.Id,
            ScopeType = scopeType,
            ScopeLocationId = scopeLocationId,
            IsPrimaryOwner = isOwnerRole,
            Status = requestedStatus,
            GrantedAtUtc = now
        };

        _dbContext.TenantRoleAssignments.Add(assignment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (isOwnerRole)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaffAssignmentOperationResult.Conflict(
                "owner_assignment_conflict",
                "Another active owner already exists for this tenant. Retry the transfer.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaffAssignmentOperationResult.Conflict(
                "duplicate_staff_assignment",
                "This user already holds the same role and scope in this tenant.");
        }

        return StaffAssignmentOperationResult.Success(assignment);
    }

    public async Task<StaffAssignmentOperationResult> UpdateAsync(
        TenantRoleAssignment assignment,
        StaffProfile staffProfile,
        GymRole role,
        TenantRoleAssignmentScopeType scopeType,
        Guid? scopeLocationId,
        TenantRoleAssignmentStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        var isOwnerRole = role.NormalizedName == DefaultGymRoleNames.Owner;

        if (assignment.IsPrimaryOwner && !isOwnerRole)
        {
            return StaffAssignmentOperationResult.Invalid(
                "owner_role_change_forbidden",
                "Primary owner assignments cannot be reassigned to another role from the staff workspace.");
        }

        if (isOwnerRole && scopeType != TenantRoleAssignmentScopeType.Tenant)
        {
            return StaffAssignmentOperationResult.Invalid(
                "invalid_owner_scope",
                "Owner assignments must be tenant-scoped.");
        }

        if (isOwnerRole && requestedStatus != TenantRoleAssignmentStatus.Active)
        {
            return StaffAssignmentOperationResult.Invalid(
                "invalid_owner_status",
                "Owner assignments must be active.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        if (isOwnerRole && !assignment.IsPrimaryOwner)
        {
            var existingOwners = await _dbContext.TenantRoleAssignments
                .Where(item =>
                    item.GymId == assignment.GymId
                    && item.Id != assignment.Id
                    && item.IsPrimaryOwner
                    && item.Status == TenantRoleAssignmentStatus.Active
                    && item.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var existingOwner in existingOwners)
            {
                existingOwner.Status = TenantRoleAssignmentStatus.Revoked;
                existingOwner.RevokedAtUtc = now;
            }
        }
        else if (!isOwnerRole && requestedStatus == TenantRoleAssignmentStatus.Active)
        {
            var duplicateActiveAssignment = await _dbContext.TenantRoleAssignments
                .AnyAsync(item =>
                    item.GymId == assignment.GymId
                    && item.Id != assignment.Id
                    && item.UserId == assignment.UserId
                    && item.RoleId == role.Id
                    && item.ScopeType == scopeType
                    && item.ScopeLocationId == scopeLocationId
                    && item.Status == TenantRoleAssignmentStatus.Active
                    && item.RevokedAtUtc == null,
                    cancellationToken);

            if (duplicateActiveAssignment)
            {
                return StaffAssignmentOperationResult.Conflict(
                    "duplicate_staff_assignment",
                    "This user already holds the same role and scope in this tenant.");
            }
        }

        assignment.RoleId = role.Id;
        assignment.StaffProfileId = staffProfile.Id;
        assignment.ScopeType = scopeType;
        assignment.ScopeLocationId = scopeLocationId;
        assignment.IsPrimaryOwner = isOwnerRole;
        assignment.Status = requestedStatus;
        assignment.RevokedAtUtc = requestedStatus == TenantRoleAssignmentStatus.Revoked ? assignment.RevokedAtUtc ?? now : null;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (isOwnerRole)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaffAssignmentOperationResult.Conflict(
                "owner_assignment_conflict",
                "Another active owner already exists for this tenant. Retry the transfer.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StaffAssignmentOperationResult.Conflict(
                "duplicate_staff_assignment",
                "This user already holds the same role and scope in this tenant.");
        }

        return StaffAssignmentOperationResult.Success(assignment);
    }
}
