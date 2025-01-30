using System.Text.RegularExpressions;
using FluentResults;
using JetBrains.Annotations;

namespace TelegramDigest.Application.Services;

internal readonly partial record struct ChannelTgId
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

internal readonly record struct DigestId(Guid Id)
{
    internal static DigestId NewId() => new(Guid.NewGuid());

    public override string ToString() => Id.ToString();
}

internal readonly record struct TimeUtc(TimeOnly Time)
{
    public override string ToString() => Time.ToString();
}

internal readonly record struct Html(string HtmlString)
{
    public override string ToString() => HtmlString;
}

/// <summary>
/// Describes the importance of a post. Importance value must be between 1 and 10, inclusive
/// </summary>
internal readonly record struct Importance
{
    internal Importance(int Value)
    {
        this.Value = Value is > 0 and <= 10
            ? Value
            : throw new ArgumentOutOfRangeException(
                nameof(Value),
                "Importance value must be between 1 and 10, inclusive"
            );
    }

    internal int Value { get; }
}
