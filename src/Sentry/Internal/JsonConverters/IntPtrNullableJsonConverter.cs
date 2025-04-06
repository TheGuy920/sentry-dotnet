using Newtonsoft.Json;

namespace Sentry.Internal.JsonConverters;

internal class IntPtrNullableJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IntPtr?);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        return new IntPtr(Convert.ToInt64(reader.Value));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            var intPtr = (IntPtr)value;
            writer.WriteValue(intPtr.ToInt64());
        }
    }
}
