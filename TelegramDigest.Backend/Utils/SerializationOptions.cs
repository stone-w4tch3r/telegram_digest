using System.Text.Json;

namespace TelegramDigest.Backend.Utils;

internal static class SerializationOptions
{
    public static readonly JsonSerializerOptions ExceptionSerializerOptions = new()
    {
        Converters = { new ExceptionJsonConverter() },
    };

    public static readonly JsonSerializerOptions FeedUrlSerializerOptions = new()
    {
        Converters = { new FeedUrlJsonConverter() },
    };
}
