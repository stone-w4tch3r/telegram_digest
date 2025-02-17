using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace TelegramDigest.Web.DeploymentOptions;

public class UrlBasePathAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var path = value.ToString()!;
        if (path == "/")
        {
            return ValidationResult.Success;
        }

        if (path.Contains(' '))
        {
            return new("Path must not contain spaces");
        }
        if (!path.StartsWith('/'))
        {
            return new("Path must start with '/'");
        }
        if (path.EndsWith('/') && path != "/")
        {
            return new("Path must not end with '/' (except single '/')");
        }
        if (path.Contains("//"))
        {
            return new("Path must not contain consecutive slashes");
        }
        if (path.Any(c => !char.IsAscii(c)))
        {
            return new("Path can only contain ASCII characters");
        }
        if (path.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '/'))
        {
            return new("Path can only contain letters, numbers, and /-_");
        }
        foreach (var segment in path.Split('/').Skip(1))
        {
            if (string.IsNullOrEmpty(segment))
            {
                throw new UnreachableException("Failed to validate url path");
            }
            if (!char.IsLetterOrDigit(segment[0]))
            {
                return new("Path segment must start with a letter or digit");
            }
        }

        return ValidationResult.Success;
    }
}
