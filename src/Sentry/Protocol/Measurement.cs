using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;

namespace Sentry.Protocol;

/// <summary>
/// A measurement, containing a numeric value and a unit.
/// </summary>
public sealed class Measurement : ISentryJsonSerializable
{
    /// <summary>
    /// The numeric value of the measurement.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The unit of measurement.
    /// </summary>
    public MeasurementUnit Unit { get; }

    private Measurement(object value, MeasurementUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(int value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(long value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(ulong value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(double value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("value");
        switch (Value)
        {
            case int number:
                writer.WriteValue(number);
                break;
            case long number:
                writer.WriteValue(number);
                break;
            case ulong number:
                writer.WriteValue(number);
                break;
            case double number:
                writer.WriteValue(number);
                break;
        }

        if (!string.IsNullOrWhiteSpace(Unit.ToString()))
        {
            writer.WritePropertyName("unit");
            writer.WriteValue(Unit.ToString());
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Measurement FromJson(JToken json)
    {
        var value = json["value"]?.ToObject<object>()!;
        var unit = json["unit"]?.Value<string>();
        return new Measurement(value, MeasurementUnit.Parse(unit));
    }
}
