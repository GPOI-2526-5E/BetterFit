using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Represents a gym in Betterfit.
/// </summary>
public class Gym
{
    /// <summary>
    /// Unique identifier of the gym.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public gym name.
    /// </summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the gym record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// User memberships assigned to this gym.
    /// </summary>
    public ICollection<GymMembership> Memberships { get; set; } = new List<GymMembership>();

    /// <summary>
    /// Role definitions available in this gym.
    /// </summary>
    public ICollection<GymRole> Roles { get; set; } = new List<GymRole>();
}
