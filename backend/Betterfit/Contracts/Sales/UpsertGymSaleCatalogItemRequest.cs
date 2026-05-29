using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed record UpsertGymSaleCatalogItemRequest(
    Guid LocationId,
    GymSaleItemType ItemType,
    string Name,
    decimal UnitPriceAmount,
    int DefaultQuantity,
    decimal DefaultDiscountAmount,
    int? DefaultCreditsGranted,
    int? ServicePeriodDays,
    string? Notes,
    bool IsActive);
