using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed class CreateGymLeadRequest
{
    public Guid LocationId { get; init; }

    public Guid? OwnerAssignmentId { get; init; }

    [Required]
    [MaxLength(160)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(32)]
    public string? PhoneNumber { get; init; }

    public GymLeadSource Source { get; init; } = GymLeadSource.Other;

    [MaxLength(200)]
    public string? Interest { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DateTime? NextFollowUpAtUtc { get; init; }
}
