using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;

namespace Sentry;

internal class PersistedSessionUpdate
{
    public SessionUpdate Update { get; }

    public DateTimeOffset? PauseTimestamp { get; }

    public PersistedSessionUpdate(SessionUpdate update, DateTimeOffset? pauseTimestamp)
    {
        Update = update;
        PauseTimestamp = pauseTimestamp;
    }

    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("update");
        Update.WriteTo(writer, logger);

        if (PauseTimestamp is { } pauseTimestamp)
        {
            writer.WritePropertyName("paused");
            writer.WriteValue(pauseTimestamp);
        }

        writer.WriteEndObject();
    }

    public static PersistedSessionUpdate FromJson(JToken json)
    {
        var update = SessionUpdate.FromJson((json["update"] as JObject)!);
        var pauseTimestamp = json["paused"]?.ToObject<DateTimeOffset?>();

        return new PersistedSessionUpdate(update, pauseTimestamp);
    }
}
