namespace Sentry.Internal.JsonConverters;

using Newtonsoft.Json;

internal class UIntPtrJsonConverter : JsonConverter<UIntPtr>
{
    public override UIntPtr ReadJson(JsonReader reader, Type objectType, UIntPtr existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new UIntPtr(Convert.ToUInt64(reader.Value));
    }

    public override void WriteJson(JsonWriter writer, UIntPtr value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToUInt64());
    }

    public override bool CanRead => true;
}
