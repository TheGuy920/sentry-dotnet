using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// A thread running at the time of an event.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
public sealed class SentryThread : ISentryJsonSerializable
{
    /// <summary>
    /// The Id of the thread.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// The name of the thread.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether the crash happened on this thread.
    /// </summary>
    public bool? Crashed { get; set; }

    /// <summary>
    /// An optional flag to indicate that the thread was in the foreground.
    /// </summary>
    public bool? Current { get; set; }

    /// <summary>
    /// Stack trace.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
    public SentryStackTrace? Stacktrace { get; set; }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (Id.HasValue)
        {
            writer.WritePropertyName("id");
            writer.WriteValue(Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            writer.WritePropertyName("name");
            writer.WriteValue(Name);
        }

        if (Crashed.HasValue)
        {
            writer.WritePropertyName("crashed");
            writer.WriteValue(Crashed.Value);
        }

        if (Current.HasValue)
        {
            writer.WritePropertyName("current");
            writer.WriteValue(Current.Value);
        }

        if (Stacktrace != null)
        {
            writer.WritePropertyName("stacktrace");
            Stacktrace.WriteTo(writer, logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryThread FromJson(JToken json)
    {
        var id = json["id"]?.Value<int>();
        var name = json["name"]?.Value<string>();
        var crashed = json["crashed"]?.Value<bool>();
        var current = json["current"]?.Value<bool>();
        var stacktrace = json["stacktrace"] != null ? SentryStackTrace.FromJson(json["stacktrace"]!) : null;

        return new SentryThread
        {
            Id = id,
            Name = name,
            Crashed = crashed,
            Current = current,
            Stacktrace = stacktrace
        };
    }
}
