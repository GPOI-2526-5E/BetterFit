namespace Betterfit.Models;

public class GymAuthenticationPolicy
{
    public Guid GymId { get; set; }

    public bool RequireTwoFactorForStaff { get; set; }

    public bool RequireTwoFactorForMembers { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;
}
