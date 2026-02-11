using Betterfit.Data;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Authorization;

public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _dbContext;

    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PermissionEvaluationResult> EvaluateGymPermissionAsync(
        string userId,
        Guid gymId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var normalizedResource = Normalize(resource);
        var normalizedAction = Normalize(action);

        var matches = await (
            from membership in _dbContext.GymMemberships.AsNoTracking()
            join rolePermission in _dbContext.RolePermissions.AsNoTracking()
                on membership.RoleId equals rolePermission.RoleId
            join catalogPermission in _dbContext.PermissionCatalogItems.AsNoTracking()
                on rolePermission.PermissionId equals catalogPermission.Id
            where membership.UserId == userId
                  && membership.GymId == gymId
                  && catalogPermission.NormalizedResource == normalizedResource
                  && catalogPermission.NormalizedAction == normalizedAction
            select rolePermission.IsAllowed)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            return PermissionEvaluationResult.None;
        }

        if (matches.Any(value => !value))
        {
            return PermissionEvaluationResult.Denied;
        }

        return PermissionEvaluationResult.Allowed;
    }

    public async Task<IReadOnlyCollection<Guid>> GetAuthorizedGymIdsAsync(
        string userId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var normalizedResource = Normalize(resource);
        var normalizedAction = Normalize(action);

        var matches = await (
            from membership in _dbContext.GymMemberships.AsNoTracking()
            join rolePermission in _dbContext.RolePermissions.AsNoTracking()
                on membership.RoleId equals rolePermission.RoleId
            join catalogPermission in _dbContext.PermissionCatalogItems.AsNoTracking()
                on rolePermission.PermissionId equals catalogPermission.Id
            where membership.UserId == userId
                  && catalogPermission.NormalizedResource == normalizedResource
                  && catalogPermission.NormalizedAction == normalizedAction
            select new { membership.GymId, rolePermission.IsAllowed })
            .ToListAsync(cancellationToken);

        return matches
            .GroupBy(match => match.GymId)
            .Where(group => group.Any(match => match.IsAllowed) && group.All(match => match.IsAllowed))
            .Select(group => group.Key)
            .ToList();
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
