using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed class UpdateGymLeadStageRequest
{
    public GymLeadStage Stage { get; init; }

    public Guid? ConvertedMembershipId { get; init; }

    public DateTime? LastContactedAtUtc { get; init; }

    public DateTime? NextFollowUpAtUtc { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
