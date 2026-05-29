using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Sales;

public sealed class CreateGymSaleRequest
{
    public Guid MembershipId { get; init; }

    public Guid? LocationId { get; init; }

    public DateTime? SoldAtUtc { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    [MinLength(1)]
    public List<CreateGymSaleLineRequest> Lines { get; init; } = [];

    [MinLength(1)]
    public List<CreateGymSalePaymentRequest> Payments { get; init; } = [];
}
