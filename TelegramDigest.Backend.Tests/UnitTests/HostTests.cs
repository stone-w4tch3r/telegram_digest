using TelegramDigest.Types.Host;

namespace TelegramDigest.Application.Tests.UnitTests;

[TestFixture]
public sealed class HostTests
{
    [TestCase("example.com", Host.HostnameType.Hostname)]
    [TestCase("1.1.1.1", Host.HostnameType.IPv4)]
    [TestCase("1.1.1.1:80", Host.HostnameType.IPv4WithPort)]
    [TestCase("[2001:db8::1]", Host.HostnameType.IPv6)]
    [TestCase("[2001:db8::1]:123", Host.HostnameType.IPv6WithPort)]
    [TestCase("example.com:443", Host.HostnameType.HostnameWithPort)]
    [TestCase("::1", Host.HostnameType.IPv6)]
    [TestCase("[::1]", Host.HostnameType.IPv6)]
    [TestCase("2001:db8::1:80", Host.HostnameType.IPv6)]
    [TestCase("invalid:host:80", Host.HostnameType.Invalid)]
    [TestCase("example.com:abc", Host.HostnameType.Invalid)]
    [TestCase("https://example.com", Host.HostnameType.Invalid)]
    [TestCase("host..example.com", Host.HostnameType.Invalid)]
    public void DetermineHostType_ValidInput_ReturnsCorrectType(
        string input,
        Host.HostnameType expectedType
    )
    {
        var actualType = Host.DetermineHostType(input);
        Assert.That(actualType, Is.EqualTo(expectedType));
    }

    [TestCase("example.com", "example.com", null)]
    [TestCase("1.1.1.1:80", "1.1.1.1", 80)]
    [TestCase("[2001:db8::1]", "2001:db8::1", null)]
    [TestCase("[2001:db8::1]:123", "2001:db8::1", 123)]
    [TestCase("::1", "::1", null)]
    [TestCase("example.com:443", "example.com", 443)]
    public void Constructor_ValidInput_ParsesCorrectly(
        string input,
        string expectedHost,
        int? expectedPort
    )
    {
        var host = new Host(input);
        Assert.That(expectedHost, Is.EqualTo(host.HostPart));
        Assert.That(expectedPort, Is.EqualTo(host.Port));
    }

    [TestCase("https://example.com")]
    [TestCase("example.com:abc")]
    [TestCase("1.1.1.1:99999")]
    [TestCase("[2001:db8::1")]
    [TestCase("2001:db8::1]:80")]
    [TestCase("host..example.com")]
    [TestCase("-example.com")]
    [TestCase("example.com:-80")]
    [TestCase(":example.com")]
    [TestCase("example.com:")]
    public void Constructor_InvalidInput_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => _ = new Host(input));
    }

    [TestCase("example.com", true, "example.com", null)]
    [TestCase("1.1.1.1:80", true, "1.1.1.1", 80)]
    [TestCase("[2001:db8::1]:123", true, "2001:db8::1", 123)]
    [TestCase("invalid:host:80", false, null, null)]
    [TestCase("https://example.com", false, null, null)]
    public void TryParseHost_ValidInput_ReturnsExpectedResult(
        string input,
        bool expectedSuccess,
        string? expectedHost,
        int? expectedPort
    )
    {
        var result = Host.TryParseHost(input, out var host);
        Assert.That(expectedSuccess, Is.EqualTo(result));
        if (expectedSuccess)
        {
            Assert.That(expectedHost, Is.EqualTo(host?.HostPart));
            Assert.That(expectedPort, Is.EqualTo(host?.Port));
        }
        else
        {
            Assert.That(host, Is.Null);
        }
    }

    [TestCase("example.com", "example.com")]
    [TestCase("1.1.1.1:80", "1.1.1.1:80")]
    [TestCase("[2001:db8::1]", "2001:db8::1")]
    [TestCase("[2001:db8::1]:123", "[2001:db8::1]:123")]
    [TestCase("::1", "::1")]
    [TestCase("example.com:443", "example.com:443")]
    public void ToString_ValidHost_ReturnsExpectedString(string input, string expected)
    {
        var host = new Host(input);
        Assert.That(expected, Is.EqualTo(host.ToString()));
    }
}
