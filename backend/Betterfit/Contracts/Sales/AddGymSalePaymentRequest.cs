using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed class AddGymSalePaymentRequest
{
    [Range(typeof(decimal), "0.01", "999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }

    public GymSalePaymentMethod Method { get; init; }

    public GymSalePaymentStatus Status { get; init; } = GymSalePaymentStatus.Paid;

    public DateTime? DueAtUtc { get; init; }

    public DateTime? PaidAtUtc { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
