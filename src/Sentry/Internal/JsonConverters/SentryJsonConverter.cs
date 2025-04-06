using Newtonsoft.Json;
using System;

namespace Sentry.Internal.JsonConverters;

/// <summary>
/// A converter that removes dangerous classes from being serialized,
/// and, also formats some classes like Exception and Type.
/// </summary>
internal class SentryJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        typeof(Type).IsAssignableFrom(objectType) ||
        objectType.FullName?.StartsWith("System.Reflection") == true;

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        return null;
    }

    public override void WriteJson(
        JsonWriter writer,
        object? value,
        JsonSerializer serializer)
    {
        if (value is Type type &&
            type.FullName != null)
        {
            writer.WriteValue(type.FullName);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
