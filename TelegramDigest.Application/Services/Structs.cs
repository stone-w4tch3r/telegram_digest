using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace TelegramDigest.Application.Services;

internal readonly partial record struct ChannelId
{
    [GeneratedRegex("^[a-zA-Z0-9_]{5,32}$")]
    private static partial Regex ChannelNamePattern();

    public ChannelId(string ChannelName)
    {
        this.ChannelName = ChannelNamePattern().IsMatch(ChannelName)
            ? ChannelName
            : throw new ArgumentException(
                $"Can't create ChannelId from invalid channel name. Got [{ChannelName}], expected [{ChannelNamePattern()}]"
            );
    }

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
