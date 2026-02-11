namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Gym DTO returned by gym endpoints.
/// </summary>
/// <param name="Id">Gym identifier.</param>
/// <param name="Name">Gym display name.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the gym was created.</param>
public sealed record GymResponse(Guid Id, string Name, DateTime CreatedAtUtc);
