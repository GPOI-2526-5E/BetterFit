using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed record GymSaleLineResponse(
    Guid Id,
    GymSaleItemType ItemType,
    string Name,
    int Quantity,
    decimal UnitPriceAmount,
    decimal DiscountAmount,
    decimal LineTotalAmount,
    DateTime? ServicePeriodStartUtc,
    DateTime? ServicePeriodEndUtc,
    int? CreditsGranted,
    string? Notes);
