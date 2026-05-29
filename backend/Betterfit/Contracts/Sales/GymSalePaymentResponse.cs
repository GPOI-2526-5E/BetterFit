using Betterfit.Models;

namespace Betterfit.Contracts.Sales;

public sealed record GymSalePaymentResponse(
    Guid Id,
    decimal Amount,
    GymSalePaymentMethod Method,
    GymSalePaymentStatus Status,
    DateTime? DueAtUtc,
    DateTime? PaidAtUtc,
    string? ReceiptCode,
    DateTime? ReceiptIssuedAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);
