using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public sealed class GymSaleCatalogItem
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid LocationId { get; set; }

    public GymSaleItemType ItemType { get; set; } = GymSaleItemType.Service;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    public decimal UnitPriceAmount { get; set; }

    public int DefaultQuantity { get; set; } = 1;

    public decimal DefaultDiscountAmount { get; set; }

    public int? DefaultCreditsGranted { get; set; }

    public int? ServicePeriodDays { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;
}
