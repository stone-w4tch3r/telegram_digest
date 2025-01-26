namespace TelegramDigest.Application.Services;

internal readonly record struct ChannelId(string Value)
{
    internal static ChannelId From(string channelName) => new(channelName);

    public override string ToString() => Value;
}

internal readonly record struct DigestId(Guid Value)
{
    internal static DigestId NewId() => new(Guid.NewGuid());

    internal static DigestId From(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}
