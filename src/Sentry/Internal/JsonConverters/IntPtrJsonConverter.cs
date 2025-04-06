namespace Sentry.Internal.JsonConverters;

using System;
using Newtonsoft.Json;

internal class IntPtrJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IntPtr);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return IntPtr.Zero;
        }

        return new IntPtr(Convert.ToInt64(reader.Value));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is IntPtr intPtr)
        {
            writer.WriteValue(intPtr.ToInt64());
        }
    }
}
