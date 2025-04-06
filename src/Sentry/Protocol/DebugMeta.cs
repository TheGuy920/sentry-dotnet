using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// The Sentry Debug Meta interface.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta"/>
internal sealed class DebugMeta : ISentryJsonSerializable
{
    public List<DebugImage>? Images { get; set; }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (Images?.Count > 0)
        {
            writer.WritePropertyName("images");
            writer.WriteStartArray();
            foreach (var image in Images)
            {
                image.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static DebugMeta FromJson(JToken json)
    {
        var images = json["images"]?.ToObject<JArray>()?.Select(token => DebugImage.FromJson((JObject)token)).ToList();

        return new DebugMeta
        {
            Images = images
        };
    }
}
