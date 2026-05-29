namespace Betterfit.Authorization;

public interface IPermissionService
{
    Task<GymPermissionScope> GetGymPermissionScopeAsync(
        string userId,
        Guid gymId,
        string resource,
        string action,
        CancellationToken cancellationToken);

    Task<PermissionEvaluationResult> EvaluateGymPermissionAsync(
        string userId,
        Guid gymId,
        string resource,
        string action,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetAuthorizedGymIdsAsync(
        string userId,
        string resource,
        string action,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, GymPermissionScope>> GetGymPermissionScopesAsync(
        string userId,
        IEnumerable<Guid> gymIds,
        string resource,
        string action,
        CancellationToken cancellationToken);
}
