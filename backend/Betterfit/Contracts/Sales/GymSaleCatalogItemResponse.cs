using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed record GymSaleCatalogItemResponse(
    Guid CatalogItemId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    GymSaleItemType ItemType,
    string Name,
    decimal UnitPriceAmount,
    int DefaultQuantity,
    decimal DefaultDiscountAmount,
    int? DefaultCreditsGranted,
    int? ServicePeriodDays,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
