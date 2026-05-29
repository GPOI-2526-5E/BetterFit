using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload to create or update a member relationship within a gym.
/// </summary>
public sealed class AssignUserToGymRequest
{
    public string? UserId { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    public List<Guid> LocationIds { get; init; } = [];

    public Guid? PrimaryLocationId { get; init; }

    public GymMembershipStatus? Status { get; init; }

    public GymMembershipSource? Source { get; init; }

    [MaxLength(32)]
    public string? TaxCode { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public MemberProfileRequest? Profile { get; init; }

    public List<GymCustomFieldValueInput> CustomFields { get; init; } = [];
}
