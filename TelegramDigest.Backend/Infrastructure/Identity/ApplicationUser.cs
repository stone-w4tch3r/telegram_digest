using Microsoft.AspNetCore.Identity;

namespace TelegramDigest.Backend.Infrastructure.Identity;

/// <summary>
/// Application user for Identity, using GUID as primary key.
/// Only Email is overridden for future extensibility.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    public override string? Email { get; set; }
}
