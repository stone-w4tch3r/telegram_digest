using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using FluentResults;

namespace TelegramDigest.Application.Core;

public readonly partial record struct ChannelTgId
{
    [GeneratedRegex("^[a-zA-Z0-9_]{5,32}$")]
    private static partial Regex ChannelNamePattern();

    public ChannelTgId(string ChannelName)
    {
        this.ChannelName = ChannelNamePattern().IsMatch(ChannelName)
            ? ChannelName
            : throw new ArgumentException(
                $"Can't create ChannelId from invalid channel name. Got [{ChannelName}], expected [{ChannelNamePattern()}]"
            );
    }

    public static Result<ChannelTgId> TryFromString(string channelName) =>
        Result.Try(() => new ChannelTgId(channelName));

    public override string ToString() => ChannelName;

    public string ChannelName { get; }
}

public readonly record struct DigestId(Guid Id)
{
    public static DigestId NewId() => new(Guid.NewGuid());

    public override string ToString() => Id.ToString();
}

public readonly record struct TimeUtc(TimeOnly Time)
{
    public override string ToString() => Time.ToString();
}

public readonly record struct Html(string HtmlString)
{
    public override string ToString() => HtmlString;
}

/// <summary>
/// Describes the importance of a post. Importance value must be between 1 and 10, inclusive
/// </summary>
public readonly record struct Importance
{
    public Importance(int Value)
    {
        this.Value = Value is > 0 and <= 10
            ? Value
            : throw new ArgumentOutOfRangeException(
                nameof(Value),
                "Importance value must be between 1 and 10, inclusive"
            );
    }

    public int Value { get; }
}

public readonly record struct Hostname
{
    public string Host { get; }

    public Hostname(string value)
    {
        Host = ValidateHostname(value);
    }

    public static Result<Hostname> TryCreateHostname(string hostname) =>
        Result.Try(() => new Hostname(hostname));

    public static bool IsValidHostName(string hostname) =>
        Result.Try(() => ValidateHostname(hostname)).IsSuccess;

    private static string ValidateHostname(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim();

        if (trimmed.Length == 0)
            throw new ArgumentException("Hostname cannot be empty or whitespace", nameof(value));

        if (!IsValidHostname(trimmed))
            throw new ArgumentException("Invalid hostname: " + trimmed);

        return trimmed;
    }

    public override string ToString()
    {
        return Host;
    }

    private static bool IsValidHostname(string hostname)
    {
        if (IPAddress.TryParse(hostname, out _))
            return true;

        return Uri.CheckHostName(hostname) == UriHostNameType.Dns;
    }
}
