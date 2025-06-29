using Microsoft.AspNetCore.Identity;

namespace TelegramDigest.Backend.Infrastructure.Identity;

/// <summary>
/// Application user for Identity, using GUID as a primary key.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>;
