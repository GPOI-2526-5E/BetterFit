namespace Betterfit.Authorization;

public interface IPermissionService
{
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
}
