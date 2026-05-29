using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed class CreateGymSaleLineRequest
{
    public GymSaleItemType ItemType { get; init; }

    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 999)]
    public int Quantity { get; init; } = 1;

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitPriceAmount { get; init; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal DiscountAmount { get; init; }

    public DateTime? ServicePeriodStartUtc { get; init; }

    public DateTime? ServicePeriodEndUtc { get; init; }

    [Range(0, 999999)]
    public int? CreditsGranted { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
