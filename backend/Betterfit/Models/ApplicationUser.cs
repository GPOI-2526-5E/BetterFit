using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Betterfit.Models;

/// <summary>
/// Application user account used for authentication in Betterfit.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Optional display name shown in the CRM UI.
    /// </summary>
    [MaxLength(120)]
    public string? FullName { get; set; }

    /// <summary>
    /// Gym assignments for this user. Each membership links the user to a gym and a role.
    /// </summary>
    public ICollection<GymMembership> GymMemberships { get; set; } = new List<GymMembership>();
}
