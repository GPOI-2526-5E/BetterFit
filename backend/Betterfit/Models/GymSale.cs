using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymSale
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymMembershipId { get; set; }

    public Guid LocationId { get; set; }

    [MaxLength(32)]
    public string ReferenceCode { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime SoldAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public GymSaleStatus Status { get; set; } = GymSaleStatus.PendingPayment;

    public GymSalePaymentStatus PaymentStatus { get; set; } = GymSalePaymentStatus.Pending;

    public Gym Gym { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ICollection<GymSaleLine> Lines { get; set; } = new List<GymSaleLine>();

    public ICollection<GymSalePayment> Payments { get; set; } = new List<GymSalePayment>();
}
