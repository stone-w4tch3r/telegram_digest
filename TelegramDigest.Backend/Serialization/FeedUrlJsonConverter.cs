using System.Text.Json;
using System.Text.Json.Serialization;
using TelegramDigest.Backend.Models;

namespace TelegramDigest.Backend.Serialization
{
    public sealed class FeedUrlJsonConverter : JsonConverter<FeedUrl>
    {
        public override FeedUrl Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var urlString = reader.GetString();
            return new(urlString!);
        }

        public override void Write(
            Utf8JsonWriter writer,
            FeedUrl value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Url.ToString());
        }
    }
}
