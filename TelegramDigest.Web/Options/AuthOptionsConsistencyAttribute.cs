using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Options;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthOptionsConsistencyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not AuthenticationOptions o)
        {
            return ValidationResult.Success;
        }

        var failures = new List<ValidationResult>();

        // SingleUserMode: No OIDC or Proxy fields allowed
        if (o.SingleUserMode)
        {
            if (!string.IsNullOrWhiteSpace(o.Authority))
            {
                failures.Add(
                    new("Authority must not be set in SingleUserMode.", [nameof(o.Authority)])
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ClientId))
            {
                failures.Add(
                    new("ClientId must not be set in SingleUserMode.", [nameof(o.ClientId)])
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ClientSecret))
            {
                failures.Add(
                    new("ClientSecret must not be set in SingleUserMode.", [nameof(o.ClientSecret)])
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ProxyHeaderEmail))
            {
                failures.Add(
                    new(
                        "ProxyHeaderEmail must not be set in SingleUserMode.",
                        [nameof(o.ProxyHeaderEmail)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ProxyHeaderId))
            {
                failures.Add(
                    new(
                        "ProxyHeaderId must not be set in SingleUserMode.",
                        [nameof(o.ProxyHeaderId)]
                    )
                );
            }
        }
        // OIDC Mode: Authority set, must have ClientId/ClientSecret, no Proxy fields
        else if (
            !string.IsNullOrWhiteSpace(o.Authority)
            || !string.IsNullOrWhiteSpace(o.ClientId)
            || !string.IsNullOrWhiteSpace(o.ClientSecret)
        )
        {
            if (string.IsNullOrWhiteSpace(o.ClientId))
            {
                failures.Add(new("ClientId must be set in OIDC mode.", [nameof(o.ClientId)]));
            }
            if (string.IsNullOrWhiteSpace(o.ClientSecret))
            {
                failures.Add(
                    new("ClientSecret must be set in OIDC mode.", [nameof(o.ClientSecret)])
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ProxyHeaderEmail))
            {
                failures.Add(
                    new(
                        "ProxyHeaderEmail must not be set in OIDC mode.",
                        [nameof(o.ProxyHeaderEmail)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ProxyHeaderId))
            {
                failures.Add(
                    new("ProxyHeaderId must not be set in OIDC mode.", [nameof(o.ProxyHeaderId)])
                );
            }
        }
        // Proxy Mode: ProxyHeader fields set, no OIDC fields
        else if (
            !string.IsNullOrWhiteSpace(o.ProxyHeaderEmail)
            || !string.IsNullOrWhiteSpace(o.ProxyHeaderId)
        )
        {
            if (!string.IsNullOrWhiteSpace(o.Authority))
            {
                failures.Add(
                    new(
                        "Authority must not be set when using ProxyHeader authentication.",
                        [nameof(o.Authority)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ClientId))
            {
                failures.Add(
                    new(
                        "ClientId must not be set when using ProxyHeader authentication.",
                        [nameof(o.ClientId)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ClientSecret))
            {
                failures.Add(
                    new(
                        "ClientSecret must not be set when using ProxyHeader authentication.",
                        [nameof(o.ClientSecret)]
                    )
                );
            }
        }
        // If none of the modes apply, must be misconfigured
        else
        {
            failures.Add(
                new(
                    "AuthenticationOptions must be configured for SingleUserMode, Authority, or ProxyHeader authentication."
                )
            );
        }

        if (failures.Count == 0)
        {
            return ValidationResult.Success;
        }

        // Aggregate messages and member names
        var allMessages = string.Join("; ", failures.ConvertAll(f => f.ErrorMessage));
        var allMembers = new List<string>();
        foreach (var f in failures)
        {
            allMembers.AddRange(f.MemberNames);
        }

        return new(allMessages, allMembers);
    }
}
