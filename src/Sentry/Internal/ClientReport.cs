using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class ClientReport : ISentryJsonSerializable
{
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<DiscardReasonWithCategory, int> DiscardedEvents { get; }

    public ClientReport(DateTimeOffset timestamp,
        IReadOnlyDictionary<DiscardReasonWithCategory, int> discardedEvents)
    {
        Timestamp = timestamp;
        DiscardedEvents = discardedEvents;
    }

    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("timestamp");
        writer.WriteValue(Timestamp);

        writer.WritePropertyName("discarded_events");
        writer.WriteStartArray();

        // filter out empty counters, and sort the counters to allow for deterministic testing
        var discardedEvents = DiscardedEvents
            .Where(x => x.Value > 0)
            .OrderBy(x => x.Key.Reason)
            .ThenBy(x => x.Key.Category);

        foreach (var item in discardedEvents)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("reason");
            writer.WriteValue(item.Key.Reason);

            writer.WritePropertyName("category");
            writer.WriteValue(item.Key.Category);

            writer.WritePropertyName("quantity");
            writer.WriteValue(item.Value);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses <see cref="ClientReport"/> from <paramref name="json"/>.
    /// </summary>
    public static ClientReport FromJson(JToken json)
    {
        var timestamp = json["timestamp"]!.Value<DateTimeOffset>();
        var discardedEventsArray = (JArray)json["discarded_events"]!;

        var discardedEvents = discardedEventsArray
            .Select(x => new
            {
                Reason = x["reason"]!.Value<string>()!,
                Category = x["category"]!.Value<string>()!,
                Quantity = x["quantity"]!.Value<int>()
            })
            .ToDictionary(
                x => new DiscardReasonWithCategory(x.Reason, x.Category),
                x => x.Quantity);

        return new ClientReport(timestamp, discardedEvents);
    }
}
