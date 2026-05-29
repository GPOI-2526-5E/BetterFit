using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed class UpdateGymLeadTaskStatusRequest
{
    public GymLeadTaskStatus Status { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}
