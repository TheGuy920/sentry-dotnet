using Newtonsoft.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Helps serialization of Sentry protocol types which include a values property.
/// </summary>
internal sealed class SentryValues<T> : ISentryJsonSerializable
{
    /// <summary>
    /// The values.
    /// </summary>
    public IEnumerable<T> Values { get; }

    /// <summary>
    /// Creates an instance from the specified <see cref="IEnumerable{T}"/>.
    /// </summary>
    public SentryValues(IEnumerable<T>? values) => Values = values ?? Enumerable.Empty<T>();

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("values");
        writer.WriteStartArray();

        foreach (var i in Values)
        {
            if (i is ISentryJsonSerializable serializable)
            {
                serializable.WriteTo(writer, logger);
            }
            else
            {
                JsonSerializer.Create().Serialize(writer, i);
            }
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
