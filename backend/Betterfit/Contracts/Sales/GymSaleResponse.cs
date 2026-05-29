using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed record GymSaleResponse(
    Guid SaleId,
    Guid GymId,
    Guid MembershipId,
    Guid LocationId,
    string ReferenceCode,
    string MemberName,
    string MemberEmail,
    string LocationName,
    DateTime SoldAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    GymSaleStatus Status,
    GymSalePaymentStatus PaymentStatus,
    string? Notes,
    IReadOnlyCollection<GymSaleLineResponse> Lines,
    IReadOnlyCollection<GymSalePaymentResponse> Payments);
