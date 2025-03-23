using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;

namespace TelegramDigest.Backend.Db;

internal sealed partial class DigestStepsRepository
{
    private static readonly JsonSerializerOptions ExceptionSerializerOptions = new()
    {
        Converters = { new ExceptionConverter() },
    };

    private sealed record ErrorSerializationDto
    {
        public string? Message { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
        public List<ErrorSerializationDto> Reasons { get; init; } = [];
        public string? ErrorType { get; init; }
        public string? SerializedException { get; init; }
    }

    private static class ErrorSerializationHelper
    {
        private static Error ToError(ErrorSerializationDto dto)
        {
            Error error;

            if (dto.ErrorType == nameof(ExceptionalError) && dto.SerializedException != null)
            {
                var exception = JsonSerializer.Deserialize<Exception>(
                    dto.SerializedException,
                    ExceptionSerializerOptions
                );
                error = new ExceptionalError(dto.Message ?? string.Empty, exception);
            }
            else
            {
                error = new(dto.Message ?? string.Empty);
            }

            foreach (var item in dto.Metadata)
                error.WithMetadata(item.Key, item.Value);

            foreach (var reason in dto.Reasons)
                error.CausedBy(ToError(reason));

            return error;
        }

        private static ErrorSerializationDto ToDto(Error error)
        {
            var dto = new ErrorSerializationDto
            {
                Message = error.Message,
                Metadata = new(error.Metadata),
                Reasons = error.Reasons.OfType<Error>().Select(ToDto).ToList(),
                ErrorType = error.GetType().Name,
                SerializedException = error
                    is ExceptionalError { Exception: not null } exceptionalError
                    ? JsonSerializer.Serialize(
                        exceptionalError.Exception,
                        ExceptionSerializerOptions
                    )
                    : null,
            };

            return dto;
        }

        public static string SerializeErrors(List<IError> errors)
        {
            if (errors.Any(e => e is not Error))
            {
                throw new UnreachableException("Only native errors are supported");
            }

            return JsonSerializer.Serialize(errors.Select(x => ToDto((Error)x)));
        }

        public static List<IError> DeserializeErrors(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            var errorDtos = JsonSerializer.Deserialize<List<ErrorSerializationDto>>(json);
            if (errorDtos is null)
            {
                throw new JsonException("ErrorDto deserialization returned null");
            }

            return errorDtos.Select<ErrorSerializationDto, IError>(ToError).ToList();
        }
    }

    private sealed class ExceptionConverter : JsonConverter<Exception>
    {
        public override Exception Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var doc = JsonDocument.ParseValue(ref reader);
            using (doc)
            {
                var root = doc.RootElement;
                var typeName = root.GetProperty("$type").GetString()!;
                var exceptionType = Type.GetType(typeName) ?? typeof(Exception);

                var exception = CreateException(exceptionType, root, options);
                PopulateException(exception, root, options);
                return exception;
            }
        }

        private static Exception CreateException(
            Type exceptionType,
            JsonElement root,
            JsonSerializerOptions options
        )
        {
            try
            {
                // Try to use the most common constructor
                var message = root.GetProperty("Message").GetString();
                var inner = DeserializeInnerException(root.GetProperty("InnerException"), options);
                return (Exception)Activator.CreateInstance(exceptionType, message, inner)!;
            }
            catch
            {
                // Fallback for parameterless constructors
                try
                {
                    var ex = (Exception)Activator.CreateInstance(exceptionType)!;
                    SetPrivateField(ex, "_message", root.GetProperty("Message").GetString());
                    return ex;
                }
                catch
                {
                    // Ultimate fallback
                    return new(root.GetProperty("Message").GetString());
                }
            }
        }

        private static void PopulateException(
            Exception exception,
            JsonElement root,
            JsonSerializerOptions options
        )
        {
            SetPrivateField(
                exception,
                "_stackTraceString",
                root.GetProperty("StackTrace").GetString()
            );
            SetPrivateField(exception, "_source", root.GetProperty("Source").GetString());
            exception.HResult = root.GetProperty("HResult").GetInt32();

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                root.GetProperty("Data").GetRawText(),
                options
            );

            foreach (var item in data ?? new())
                exception.Data[item.Key] = item.Value;
        }

        private static Exception? DeserializeInnerException(
            JsonElement element,
            JsonSerializerOptions options
        ) =>
            element.ValueKind == JsonValueKind.Null
                ? null
                : JsonSerializer.Deserialize<Exception>(element.GetRawText(), options);

        private static void SetPrivateField(Exception ex, string fieldName, object? value) =>
            typeof(Exception)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ex, value);

        public override void Write(
            Utf8JsonWriter writer,
            Exception value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();

            writer.WriteString("$type", value.GetType().AssemblyQualifiedName);
            writer.WriteString("Message", value.Message);
            writer.WriteString("StackTrace", value.StackTrace);
            writer.WriteString("Source", value.Source);
            writer.WriteNumber("HResult", value.HResult);

            writer.WritePropertyName("Data");
            JsonSerializer.Serialize(
                writer,
                value
                    .Data.Cast<DictionaryEntry>()
                    .ToDictionary(de => de.Key.ToString()!, de => de.Value),
                options
            );

            writer.WritePropertyName("InnerException");
            if (value.InnerException != null)
            {
                JsonSerializer.Serialize(writer, value.InnerException, options);
            }
            else
            {
                writer.WriteNullValue();
            }

            writer.WriteEndObject();
        }
    }
}
