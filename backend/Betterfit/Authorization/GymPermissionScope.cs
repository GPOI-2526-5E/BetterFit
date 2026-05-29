namespace Betterfit.Authorization;

public sealed class GymPermissionScope
{
    public static readonly GymPermissionScope None = new(
        PermissionEvaluationResult.None,
        [],
        []);

    public GymPermissionScope(
        PermissionEvaluationResult tenantEvaluation,
        IReadOnlyCollection<Guid> allowedLocationIds,
        IReadOnlyCollection<Guid> deniedLocationIds)
    {
        TenantEvaluation = tenantEvaluation;
        AllowedLocationIds = allowedLocationIds;
        DeniedLocationIds = deniedLocationIds;
    }

    public PermissionEvaluationResult TenantEvaluation { get; }

    public IReadOnlyCollection<Guid> AllowedLocationIds { get; }

    public IReadOnlyCollection<Guid> DeniedLocationIds { get; }

    public bool HasAnyAccess =>
        TenantEvaluation != PermissionEvaluationResult.Denied
        && (TenantEvaluation == PermissionEvaluationResult.Allowed || AllowedLocationIds.Count > 0);

    public bool HasAnyDenials => TenantEvaluation == PermissionEvaluationResult.Denied || DeniedLocationIds.Count > 0;

    public bool HasTenantWideAccess => TenantEvaluation == PermissionEvaluationResult.Allowed && DeniedLocationIds.Count == 0;

    public bool CanAccessLocation(Guid locationId)
    {
        if (TenantEvaluation == PermissionEvaluationResult.Denied)
        {
            return false;
        }

        if (DeniedLocationIds.Contains(locationId))
        {
            return false;
        }

        return TenantEvaluation == PermissionEvaluationResult.Allowed || AllowedLocationIds.Contains(locationId);
    }

    public bool CanAccessAllLocations(IEnumerable<Guid> locationIds)
    {
        return locationIds.All(CanAccessLocation);
    }
}
