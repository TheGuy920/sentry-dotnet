using Newtonsoft.Json;

namespace Sentry.Internal.JsonConverters;

internal class UIntPtrNullableJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(UIntPtr?);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.Integer)
        {
            return new UIntPtr(Convert.ToUInt64(reader.Value));
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing UIntPtr?");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            var uintPtr = (UIntPtr)value;
            writer.WriteValue(uintPtr.ToUInt64());
        }
    }
}
