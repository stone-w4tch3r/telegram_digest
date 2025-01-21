namespace TelegramDigest.Application.Services;

public readonly record struct ChannelId(string Value)
{
    public static ChannelId From(string channelName) => new(channelName);

    public override string ToString() => Value;
}

public readonly record struct DigestId(Guid Value)
{
    public static DigestId NewId() => new(Guid.NewGuid());

    public static DigestId From(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

public readonly record struct PostId(Guid Value)
{
    public static PostId NewId() => new(Guid.NewGuid());

    public static PostId From(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}
