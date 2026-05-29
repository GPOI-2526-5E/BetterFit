using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed class UpdateGymSalePaymentRequest
{
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; init; }

    [Required]
    public GymSalePaymentMethod Method { get; init; } = GymSalePaymentMethod.Cash;

    [Required]
    public GymSalePaymentStatus Status { get; init; } = GymSalePaymentStatus.Pending;

    public DateTime? DueAtUtc { get; init; }

    public DateTime? PaidAtUtc { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
