using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymSaleLine
{
    public Guid Id { get; set; }

    public Guid GymSaleId { get; set; }

    public GymSaleItemType ItemType { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotalAmount { get; set; }

    public DateTime? ServicePeriodStartUtc { get; set; }

    public DateTime? ServicePeriodEndUtc { get; set; }

    public int? CreditsGranted { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public GymSale Sale { get; set; } = null!;
}
