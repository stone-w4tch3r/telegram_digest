using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using TelegramDigest.Web.Options;

namespace TelegramDigest.Web.Tests;

[TestFixture]
public sealed class AuthOptionsValidationAttributeTests
{
    private static readonly string?[] StringOptions = [null, "", "v"];
    private static readonly bool[] BoolOptions = [true, false];

    private static IEnumerable<TestCaseData> DynamicAuthOptionsTestCases()
    {
        // For tractability, only permute the fields that affect validation logic
        foreach (var singleUserMode in BoolOptions)
        foreach (var singleUserId in StringOptions)
        foreach (var singleUserEmail in StringOptions)
        foreach (var openIdAuthority in StringOptions)
        foreach (var openIdClientId in StringOptions)
        foreach (var openIdClientSecret in StringOptions)
        foreach (var proxyEmail in StringOptions)
        foreach (var proxyId in StringOptions)
        {
            // Compose AuthOptions
            var options = new AuthOptions
            {
                SingleUserMode = singleUserMode,
                SingleUserId = singleUserId,
                SingleUserEmail = singleUserEmail,
                OpenIdAuthority = openIdAuthority,
                OpenIdClientId = openIdClientId,
                OpenIdClientSecret = openIdClientSecret,
                ReverseProxyHeaderEmail = proxyEmail,
                ReverseProxyHeaderId = proxyId,
            };
            // Dynamically compute expected errors
            var errors = new List<string>();
            if (singleUserMode)
            {
                if (string.IsNullOrWhiteSpace(singleUserId))
                {
                    errors.Add("SINGLE_USER_ID must be set in SingleUserMode.");
                }
                if (string.IsNullOrWhiteSpace(singleUserEmail))
                {
                    errors.Add("SINGLE_USER_EMAIL must be set in SingleUserMode.");
                }
                if (!string.IsNullOrWhiteSpace(openIdAuthority))
                {
                    errors.Add("OPENID_AUTHORITY must not be set in SingleUserMode.");
                }
                if (!string.IsNullOrWhiteSpace(openIdClientId))
                {
                    errors.Add("OPENID_CLIENT_ID must not be set in SingleUserMode.");
                }
                if (!string.IsNullOrWhiteSpace(openIdClientSecret))
                {
                    errors.Add("OPENID_CLIENT_SECRET must not be set in SingleUserMode.");
                }
                if (!string.IsNullOrWhiteSpace(proxyEmail))
                {
                    errors.Add("REVERSE_PROXY_HEADER_EMAIL must not be set in SingleUserMode.");
                }
                if (!string.IsNullOrWhiteSpace(proxyId))
                {
                    errors.Add("REVERSE_PROXY_HEADER_ID must not be set in SingleUserMode.");
                }
            }
            else if (
                !string.IsNullOrWhiteSpace(openIdAuthority)
                || !string.IsNullOrWhiteSpace(openIdClientId)
                || !string.IsNullOrWhiteSpace(openIdClientSecret)
            )
            {
                if (string.IsNullOrWhiteSpace(openIdClientId))
                {
                    errors.Add("OPENID_CLIENT_ID must be set in OIDC mode.");
                }
                if (string.IsNullOrWhiteSpace(openIdClientSecret))
                {
                    errors.Add("OPENID_CLIENT_SECRET must be set in OIDC mode.");
                }
                if (!string.IsNullOrWhiteSpace(proxyEmail))
                {
                    errors.Add("REVERSE_PROXY_HEADER_EMAIL must not be set in OIDC mode.");
                }
                if (!string.IsNullOrWhiteSpace(proxyId))
                {
                    errors.Add("REVERSE_PROXY_HEADER_ID must not be set in OIDC mode.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(proxyEmail) || !string.IsNullOrWhiteSpace(proxyId))
            {
                if (!string.IsNullOrWhiteSpace(openIdAuthority))
                {
                    errors.Add(
                        "OPENID_AUTHORITY must not be set when using ProxyHeader authentication."
                    );
                }
                if (!string.IsNullOrWhiteSpace(openIdClientId))
                {
                    errors.Add(
                        "OPENID_CLIENT_ID must not be set when using ProxyHeader authentication."
                    );
                }
                if (!string.IsNullOrWhiteSpace(openIdClientSecret))
                {
                    errors.Add(
                        "OPENID_CLIENT_SECRET must not be set when using ProxyHeader authentication."
                    );
                }
            }
            else
            {
                errors.Add(
                    "AuthenticationOptions must be configured for SingleUserMode, OpenIdConnect, or ReverseProxy (headers) authentication."
                );
            }
            var shouldBeValid = errors.Count == 0;
            var name =
                $"SU:{singleUserMode},SID:{singleUserId ?? "null"},SE:{singleUserEmail ?? "null"},OA:{openIdAuthority ?? "null"},OCID:{openIdClientId ?? "null"},OCS:{openIdClientSecret ?? "null"},PE:{proxyEmail ?? "null"},PID:{proxyId ?? "null"}";
            yield return new TestCaseData(options, shouldBeValid, errors).SetName(name);
        }
    }

    [Test, TestCaseSource(nameof(DynamicAuthOptionsTestCases))]
    public void AuthOptions_Validation_Dynamic_Works(
        AuthOptions options,
        bool shouldBeValid,
        List<string> expectedErrors
    )
    {
        var ctx = new ValidationContext(options);
        var attr = new AuthOptionsValidationAttribute();
        var result = attr.GetValidationResult(options, ctx);

        if (shouldBeValid)
        {
            result.Should().Be(ValidationResult.Success);
        }
        else
        {
            result.Should().NotBeNull();
            foreach (var expected in expectedErrors)
            {
                result!.ErrorMessage.Should().Contain(expected);
            }
            // Also ensure no unexpected errors
            var actualErrors = result!
                .ErrorMessage!.Split(';')
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
            foreach (var actual in actualErrors)
            {
                expectedErrors.Should().Contain(actual);
            }
        }
    }
}
