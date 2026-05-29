using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymSalePayment
{
    public Guid Id { get; set; }

    public Guid GymSaleId { get; set; }

    public decimal Amount { get; set; }

    public GymSalePaymentMethod Method { get; set; }

    public GymSalePaymentStatus Status { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    [MaxLength(40)]
    public string? ReceiptCode { get; set; }

    public DateTime? ReceiptIssuedAtUtc { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public GymSale Sale { get; set; } = null!;
}
