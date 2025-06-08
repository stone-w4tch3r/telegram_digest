using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentResults;

namespace TelegramDigest.Backend.Models;

public readonly record struct DigestId(Guid Guid = new())
{
    public DigestId()
        : this(Guid.NewGuid()) { }

    public static DigestId NewId() => new(Guid.NewGuid());

    public override string ToString() => Guid.ToString();

    public static implicit operator string(DigestId id) => id.ToString();
}

public readonly record struct TimeUtc(TimeOnly Time)
{
    [Obsolete("Do not use parameterless constructor", true)]
    public TimeUtc()
        : this(TimeOnly.MinValue) { }

    public override string ToString() => Time.ToString();

    public static implicit operator string(TimeUtc time) => time.ToString();
}

public readonly record struct Html(string HtmlString)
{
    [Obsolete("Do not use parameterless constructor", true)]
    public Html()
        : this(string.Empty) { }

    public override string ToString() => HtmlString;

    public static implicit operator string(Html html) => html.ToString();
}

/// <summary>
/// Represents a generic RSS/Atom feed URL.
/// </summary>
public readonly record struct FeedUrl
{
    [Obsolete("Do not use parameterless constructor", true)]
    public FeedUrl()
        : this("") { }

    [JsonConstructor]
    public FeedUrl(string url)
    {
        if (
            !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (
                uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps
                && uri.Scheme != Uri.UriSchemeFile
            )
        )
        {
            throw new ArgumentException($"Invalid feed URL: {url}", nameof(url));
        }

        Url = uri;
    }

    public Uri Url { get; }

    public override string ToString() => Url.ToString();

    public static implicit operator string(FeedUrl feedUrl) => feedUrl.ToString();
}

/// <summary>
/// Describes the importance of a post. Importance value must be between 1 and 10, inclusive
/// </summary>
public readonly record struct Importance
{
    [Obsolete("Do not use parameterless constructor", true)]
    public Importance()
        : this(default) { }

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
    [Obsolete("Do not use parameterless constructor", true)]
    public Template()
        : this(string.Empty, string.Empty) { }

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
    [Obsolete("Do not use parameterless constructor", true)]
    public TemplateWithContent()
        : this(string.Empty) { }

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
