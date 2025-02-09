using System.Text.RegularExpressions;
using FluentResults;

namespace TelegramDigest.Backend.Core;

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

    public static implicit operator string(ChannelTgId id) => id.ToString();
}

public readonly record struct DigestId(Guid Id)
{
    public static DigestId NewId() => new(Guid.NewGuid());

    public override string ToString() => Id.ToString();

    public static implicit operator string(DigestId id) => id.ToString();
}

public readonly record struct TimeUtc(TimeOnly Time)
{
    public override string ToString() => Time.ToString();

    public static implicit operator string(TimeUtc time) => time.ToString();
}

public readonly record struct Html(string HtmlString)
{
    public override string ToString() => HtmlString;

    public static implicit operator string(Html html) => html.ToString();
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

    public override string ToString() => Value.ToString();

    public static implicit operator string(Importance importance) => importance.ToString();
}

internal readonly partial record struct Template
{
    [GeneratedRegex(@"^\{[a-zA-Z0-9_]+\}$")]
    private static partial Regex TemplateRegex();

    private readonly string _placeholder = "{Post}";

    public Template(string text, string placeholder)
    {
        if (!TemplateRegex().IsMatch(placeholder))
        {
            throw new ArgumentException($"Placeholder must be in the format {TemplateRegex()}");
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(text));
        }
        if (!text.Contains(placeholder))
        {
            throw new ArgumentException(
                $"Prompt must contain {placeholder} placeholder",
                nameof(text)
            );
        }

        _placeholder = placeholder;
        Text = text;
    }

    public string Text { get; }

    public override string ToString() => Text;

    public string ReplacePlaceholder(string content) => Text.Replace(_placeholder, content);
}

public readonly record struct TemplateWithPost
{
    private readonly Template _template;

    public TemplateWithPost(string text)
    {
        _template = new(text, POST_PLACEHOLDER);
    }

    public const string POST_PLACEHOLDER = "{Post}";

    public string ReplacePlaceholder(string content) => _template.ReplacePlaceholder(content);

    public string Text => _template.Text;

    public static Result<TemplateWithPost> TryCreate(string text) =>
        Result.Try(() => new TemplateWithPost(text));

    public override string ToString() => _template.ToString();

    public static implicit operator string(TemplateWithPost templateWithPost) =>
        templateWithPost.ToString();
}

public readonly record struct TemplateWithDigest
{
    private readonly Template _template;

    public TemplateWithDigest(string text)
    {
        _template = new(text, DIGEST_PLACEHOLDER);
    }

    public const string DIGEST_PLACEHOLDER = "{Digest}";

    public string ReplacePlaceholder(string content) => _template.ReplacePlaceholder(content);

    public string Text => _template.Text;

    public static Result<TemplateWithDigest> TryCreate(string text) =>
        Result.Try(() => new TemplateWithDigest(text));

    public override string ToString() => _template.ToString();

    public static implicit operator string(TemplateWithDigest templateWithDigest) =>
        templateWithDigest.ToString();
}
