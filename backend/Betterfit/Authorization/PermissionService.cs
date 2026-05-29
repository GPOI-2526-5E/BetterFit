using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Authorization;

public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _dbContext;

    private sealed record PermissionMatch(
        Guid GymId,
        TenantRoleAssignmentScopeType ScopeType,
        Guid? ScopeLocationId,
        bool IsAllowed);

    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymPermissionScope> GetGymPermissionScopeAsync(
        string userId,
        Guid gymId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var matches = await LoadMatchesAsync(userId, resource, action, gymId, cancellationToken);
        return BuildScope(matches);
    }

    public async Task<PermissionEvaluationResult> EvaluateGymPermissionAsync(
        string userId,
        Guid gymId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var scope = await GetGymPermissionScopeAsync(userId, gymId, resource, action, cancellationToken);

        return scope switch
        {
            { HasAnyAccess: true } => PermissionEvaluationResult.Allowed,
            { HasAnyDenials: true } => PermissionEvaluationResult.Denied,
            _ => PermissionEvaluationResult.None
        };
    }

    public async Task<IReadOnlyCollection<Guid>> GetAuthorizedGymIdsAsync(
        string userId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var matches = await LoadMatchesAsync(userId, resource, action, gymId: null, cancellationToken);

        return matches
            .GroupBy(match => match.GymId)
            .Where(group => BuildScope(group).HasAnyAccess)
            .Select(group => group.Key)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, GymPermissionScope>> GetGymPermissionScopesAsync(
        string userId,
        IEnumerable<Guid> gymIds,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var gymIdSet = gymIds.ToHashSet();
        if (gymIdSet.Count == 0)
        {
            return new Dictionary<Guid, GymPermissionScope>();
        }

        var matches = await LoadMatchesForGymsAsync(userId, resource, action, gymIdSet, cancellationToken);

        var scopes = new Dictionary<Guid, GymPermissionScope>(gymIdSet.Count);
        foreach (var group in matches.GroupBy(match => match.GymId))
        {
            scopes[group.Key] = BuildScope(group);
        }

        // Ensure all requested gym IDs are present with at least a None scope
        foreach (var gymId in gymIdSet.Where(id => !scopes.ContainsKey(id)))
        {
            scopes[gymId] = GymPermissionScope.None;
        }

        return scopes;
    }

    private async Task<List<PermissionMatch>> LoadMatchesAsync(
        string userId,
        string resource,
        string action,
        Guid? gymId,
        CancellationToken cancellationToken)
    {
        var normalizedResource = Normalize(resource);
        var normalizedAction = Normalize(action);

        return await (
            from assignment in _dbContext.TenantRoleAssignments.AsNoTracking()
            join rolePermission in _dbContext.RolePermissions.AsNoTracking()
                on assignment.RoleId equals rolePermission.RoleId
            join catalogPermission in _dbContext.PermissionCatalogItems.AsNoTracking()
                on rolePermission.PermissionId equals catalogPermission.Id
            where assignment.UserId == userId
                  && assignment.Status == TenantRoleAssignmentStatus.Active
                  && assignment.RevokedAtUtc == null
                  && (!gymId.HasValue || assignment.GymId == gymId.Value)
                  && catalogPermission.NormalizedResource == normalizedResource
                  && catalogPermission.NormalizedAction == normalizedAction
            select new PermissionMatch(
                assignment.GymId,
                assignment.ScopeType,
                assignment.ScopeLocationId,
                rolePermission.IsAllowed))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PermissionMatch>> LoadMatchesForGymsAsync(
        string userId,
        string resource,
        string action,
        IReadOnlyCollection<Guid> gymIds,
        CancellationToken cancellationToken)
    {
        var normalizedResource = Normalize(resource);
        var normalizedAction = Normalize(action);

        return await (
            from assignment in _dbContext.TenantRoleAssignments.AsNoTracking()
            join rolePermission in _dbContext.RolePermissions.AsNoTracking()
                on assignment.RoleId equals rolePermission.RoleId
            join catalogPermission in _dbContext.PermissionCatalogItems.AsNoTracking()
                on rolePermission.PermissionId equals catalogPermission.Id
            where assignment.UserId == userId
                  && assignment.Status == TenantRoleAssignmentStatus.Active
                  && assignment.RevokedAtUtc == null
                  && gymIds.Contains(assignment.GymId)
                  && catalogPermission.NormalizedResource == normalizedResource
                  && catalogPermission.NormalizedAction == normalizedAction
            select new PermissionMatch(
                assignment.GymId,
                assignment.ScopeType,
                assignment.ScopeLocationId,
                rolePermission.IsAllowed))
            .ToListAsync(cancellationToken);
    }

    private static GymPermissionScope BuildScope(IEnumerable<PermissionMatch> matches)
    {
        var matchList = matches.ToList();
        if (matchList.Count == 0)
        {
            return GymPermissionScope.None;
        }

        var tenantMatches = matchList
            .Where(match => match.ScopeType == TenantRoleAssignmentScopeType.Tenant)
            .Select(match => match.IsAllowed)
            .ToList();

        var tenantEvaluation = tenantMatches.Count switch
        {
            0 => PermissionEvaluationResult.None,
            _ when tenantMatches.Any(match => !match) => PermissionEvaluationResult.Denied,
            _ => PermissionEvaluationResult.Allowed
        };

        var allowedLocationIds = matchList
            .Where(match => match.ScopeType == TenantRoleAssignmentScopeType.Location && match.ScopeLocationId.HasValue)
            .GroupBy(match => match.ScopeLocationId!.Value)
            .Where(group => group.Any(match => match.IsAllowed) && group.All(match => match.IsAllowed))
            .Select(group => group.Key)
            .ToList();

        var deniedLocationIds = matchList
            .Where(match => match.ScopeType == TenantRoleAssignmentScopeType.Location && match.ScopeLocationId.HasValue)
            .GroupBy(match => match.ScopeLocationId!.Value)
            .Where(group => group.Any(match => !match.IsAllowed))
            .Select(group => group.Key)
            .ToList();

        return new GymPermissionScope(tenantEvaluation, allowedLocationIds, deniedLocationIds);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}

