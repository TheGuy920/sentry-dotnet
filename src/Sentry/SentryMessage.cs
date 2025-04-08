using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry Message interface.
/// </summary>
/// <remarks>
/// This interface enables support to structured logging.
/// </remarks>
/// <example>
/// "sentry.interfaces.Message": {
///   "message": "Message for event: {eventId}",
///   "params": [10]
/// }
/// </example>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/message/"/>
public sealed class SentryMessage : ISentryJsonSerializable
{
    /// <summary>
    /// The raw message string (un-interpolated).
    /// </summary>
    /// <remarks>
    /// Must be no more than 1000 characters in length.
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// The optional list of formatting parameters.
    /// </summary>
    public IEnumerable<object>? Params { get; set; }

    /// <summary>
    /// The formatted message.
    /// </summary>
    public string? Formatted { get; set; }

    /// <summary>
    /// Coerces <see cref="string"/> into <see cref="SentryMessage"/>.
    /// </summary>
    public static implicit operator SentryMessage(string? message) => new() { Message = message };

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(Message))
        {
            writer.WritePropertyName("message");
            writer.WriteValue(Message);
        }

        if (Params?.Any() == true)
        {
            writer.WritePropertyName("params");
            writer.WriteStartArray();
            foreach (var param in Params)
            {
                writer.WriteValue(param);
            }
            writer.WriteEndArray();
        }

        if (!string.IsNullOrWhiteSpace(Formatted))
        {
            writer.WritePropertyName("formatted");
            writer.WriteValue(Formatted);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryMessage FromJson(JToken json)
    {
        var message = json["message"]?.Value<string>();
        var @params = json["params"]?.ToObject<JArray>()?.Select(j => j.ToObject<object>()).Where(o => o != null).ToArray();
        var formatted = json["formatted"]?.Value<string>();

        return new SentryMessage
        {
            Message = message,
            Params = @params!,
            Formatted = formatted
        };
    }
}
