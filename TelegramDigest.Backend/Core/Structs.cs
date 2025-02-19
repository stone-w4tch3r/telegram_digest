using System.Text.RegularExpressions;
using FluentResults;

namespace TelegramDigest.Backend.Core;

public readonly record struct DigestId(Guid Guid)
{
    public static DigestId NewId() => new(Guid.NewGuid());

    public override string ToString() => Guid.ToString();

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
/// Telegram channel name, e.g. https://t.me/this_is_channel_name
/// </summary>
public readonly partial record struct ChannelTgId
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]{4,31}$")]
    private static partial Regex ChannelNamePattern();

    public ChannelTgId(string ChannelName)
    {
        this.ChannelName = ChannelNamePattern().IsMatch(ChannelName)
            ? ChannelName
            : throw new ArgumentException(
                "Channel name must be 5-32 characters, only letters, numbers, and underscores, starting with a letter"
            );
    }

    public static Result<ChannelTgId> TryFromString(string channelName) =>
        Result.Try(() => new ChannelTgId(channelName));

    public override string ToString() => ChannelName;

    public string ChannelName { get; }

    public static implicit operator string(ChannelTgId id) => id.ToString();
}

/// <summary>
/// Describes the importance of a post. Importance value must be between 1 and 10, inclusive
/// </summary>
public readonly record struct Importance
{
    public Importance(int Number)
    {
        this.Number = Number is > 0 and <= 10
            ? Number
            : throw new ArgumentOutOfRangeException(
                nameof(Number),
                "Importance value must be between 1 and 10, inclusive"
            );
    }

    public int Number { get; }

    public override string ToString() => Number.ToString();

    public static implicit operator string(Importance importance) => importance.ToString();
}

/// <summary>
/// Basic template with a {placeholder} inside. Provides validation and helper methods such as ReplacePlaceholder
/// </summary>
internal readonly partial record struct Template
{
    [GeneratedRegex(@"^\{[a-zA-Z0-9_]+\}$")]
    private static partial Regex TemplateRegex();

    private readonly string _placeholder;

    public Template(string text, string placeholder)
    {
        if (!TemplateRegex().IsMatch(placeholder))
        {
            throw new ArgumentException($"Placeholder must be in the format {TemplateRegex()}");
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }
        if (!text.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Prompt must contain {placeholder} placeholder",
                nameof(text)
            );
        }
        if (text.ToLower().Split([placeholder.ToLower()], StringSplitOptions.None).Length != 2)
        {
            throw new ArgumentException(
                $"Prompt must contain exactly one {placeholder} placeholder",
                nameof(text)
            );
        }

        _placeholder = placeholder;
        Text = text;
    }

    public string Text { get; }

    public override string ToString() => Text;

    public string ReplacePlaceholder(string content, StringComparison comparison) =>
        Text.Replace(_placeholder, content, comparison);
}

/// <summary>
/// Template with {Content} placeholder
/// </summary>
/// <see cref="Template"/>
public readonly record struct TemplateWithContent
{
    private readonly Template _template;

    public TemplateWithContent(string text)
    {
        _template = new(text, PLACEHOLDER);
    }

    public const string PLACEHOLDER = "{Content}";

    public string ReplacePlaceholder(string content) =>
        _template.ReplacePlaceholder(content, StringComparison.OrdinalIgnoreCase);

    public string Text => _template.Text;

    public static Result<TemplateWithContent> TryCreate(string text) =>
        Result.Try(() => new TemplateWithContent(text));

    public override string ToString() => _template.ToString();

    public static implicit operator string(TemplateWithContent templateWithPost) =>
        templateWithPost.ToString();
}
