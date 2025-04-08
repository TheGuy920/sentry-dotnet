using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Globalization;
using Sentry.Extensibility;
using Sentry.Internal.JsonConverters;

namespace Sentry.Internal.Extensions;

internal static class JsonExtensions
{
    private static readonly JsonConverter[] DefaultConverters =
    {
        new SentryJsonConverter(),
        new IntPtrJsonConverter(),
        new IntPtrNullableJsonConverter(),
        new UIntPtrJsonConverter(),
        new UIntPtrNullableJsonConverter()
    };

    private static List<JsonConverter> CustomConverters = new List<JsonConverter>();

    internal static bool JsonPreserveReferences { get; set; } = true;

    static JsonExtensions()
    {
        ResetSerializerOptions();
    }

    private static JsonSerializerSettings BuildOptions(bool preserveReferences)
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = preserveReferences ?
                PreserveReferencesHandling.Objects :
                PreserveReferencesHandling.None
        };

        foreach (var converter in DefaultConverters)
        {
            settings.Converters.Add(converter);
        }
        foreach (var converter in CustomConverters)
        {
            settings.Converters.Add(converter);
        }

        return settings;
    }

    private static JsonSerializerSettings SerializerSettings = null!;
    private static JsonSerializerSettings AltSerializerSettings = null!;

    private static List<SentryJsonContext> DefaultSerializerContexts = new();
    private static List<SentryJsonContext> ReferencePreservingSerializerContexts = new();

    private static List<Func<JsonSerializerSettings, SentryJsonContext>> JsonSerializerContextBuilders = new()
    {
        settings => new SentryJsonContext(settings)
    };

    internal static void AddJsonSerializerContext<T>(Func<JsonSerializerSettings, T> jsonSerializerContextBuilder)
        where T : SentryJsonContext
    {
        JsonSerializerContextBuilders.Add(jsonSerializerContextBuilder);
        ResetSerializerOptions();
    }

    internal static void ResetSerializerOptions()
    {
        // For our classic reflection based serialization
        SerializerSettings = BuildOptions(false);
        AltSerializerSettings = BuildOptions(true);

        // For the new AOT serialization
        DefaultSerializerContexts.Clear();
        ReferencePreservingSerializerContexts.Clear();
        foreach (var builder in JsonSerializerContextBuilders)
        {
            DefaultSerializerContexts.Add(builder(BuildOptions(false)));
            ReferencePreservingSerializerContexts.Add(builder(BuildOptions(true)));
        }
    }

    internal static void AddJsonConverter(JsonConverter converter)
    {
        // only add if we don't have this instance already
        if (CustomConverters.Contains(converter))
        {
            return;
        }

        try
        {
            CustomConverters.Add(converter);
            ResetSerializerOptions();
        }
        catch (InvalidOperationException)
        {
            // If we've already started using the serializer, then it's too late to add more converters.
            // The following exception message may occur (depending on STJ version):
            // "Serializer options cannot be changed once serialization or deserialization has occurred."
            // We'll swallow this, because it's likely to only have occurred in our own unit tests,
            // or in a scenario where the Sentry SDK has been initialized multiple times,
            // in which case we have the converter from the first initialization already.
            // TODO: .NET 8 is getting an IsReadOnly flag we could check instead of catching
            // See https://github.com/dotnet/runtime/pull/74431
        }
    }

    public static Dictionary<string, object?>? GetDictionaryOrNull(this JToken json)
    {
        if (json.Type != JTokenType.Object)
        {
            return null;
        }

        var result = new Dictionary<string, object?>();

        foreach (var prop in ((JObject)json).Properties())
        {
            result[prop.Name] = prop.Value?.GetDynamicOrNull();
        }

        return result;
    }

    public static Dictionary<string, TValue>? GetDictionaryOrNull<TValue>(
        this JToken json,
        Func<JToken, TValue> factory)
        where TValue : ISentryJsonSerializable?
    {
        if (json.Type != JTokenType.Object)
        {
            return null;
        }

        var result = new Dictionary<string, TValue>();

        foreach (var prop in ((JObject)json).Properties())
        {
            result[prop.Name] = factory(prop.Value);
        }

        return result;
    }

    public static Dictionary<string, string?>? GetStringDictionaryOrNull(this JToken json)
    {
        if (json.Type != JTokenType.Object)
        {
            return null;
        }

        var result = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var prop in ((JObject)json).Properties())
        {
            if (prop.Value?.Type == JTokenType.String)
            {
                result[prop.Name] = prop.Value.ToString();
            }
            else
            {
                result[prop.Name] = prop.Value?.ToString();
            }
        }

        return result;
    }

    public static JToken? GetPropertyOrNull(this JToken json, string name)
    {
        if (json.Type != JTokenType.Object)
        {
            return null;
        }

        var property = ((JObject)json)[name];
        if (property != null && property.Type != JTokenType.Null)
        {
            return property;
        }

        return null;
    }

    public static object? GetDynamicOrNull(this JToken json) => json.Type switch
    {
        JTokenType.Boolean => json.Value<bool>(),
        JTokenType.Integer => json.Value<long>(),
        JTokenType.Float => GetNumber(json),
        JTokenType.String => json.Value<string>(),
        JTokenType.Array => json.Select(GetDynamicOrNull).ToArray(),
        JTokenType.Object => json.GetDictionaryOrNull(),
        _ => null
    };

    private static object? GetNumber(this JToken json)
    {
        var result = json.Value<double>();
        if (result != 0)
        {
            // We got a value, as expected.
            return result;
        }

        // We might have 0 when there's actually a value there.
        // This happens on Unity IL2CPP targets.  Let's workaround that.
        // See https://github.com/getsentry/sentry-unity/issues/690

        // If the number is an integer, we can avoid extra string parsing
        var stringValue = json.ToString();
        if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
        {
            return longResult;
        }

        // Otherwise, let's get the value as a string and parse it ourselves.
        return double.Parse(stringValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Safety value to deal with native serialization - allows datetimeoffset to come in as a long or string value
    /// </summary>
    /// <param name="json"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static DateTimeOffset? GetSafeDateTimeOffset(this JToken json, string propertyName)
    {
        DateTimeOffset? result = null;
        var dtRaw = json.GetPropertyOrNull(propertyName);
        if (dtRaw != null)
        {
            if (dtRaw.Type == JTokenType.Integer || dtRaw.Type == JTokenType.Float)
            {
                var epoch = Convert.ToInt64(dtRaw.Value<double>());
                result = DateTimeOffset.FromUnixTimeSeconds(epoch);
            }
            else
            {
                result = dtRaw.Value<DateTimeOffset>();
            }
        }
        return result;
    }

    public static long? GetHexAsLong(this string? s)
    {
        if (s == null)
        {
            return null;
        }

        // It should be in hex format, such as "0x7fff5bf346c0"
        if (s.StartsWith("0x") &&
            long.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException();
    }

    public static long? GetHexAsLong(this JToken json)
    {
        // If the address is in json as a number, we can just use it.
        if (json.Type == JTokenType.Integer)
        {
            return json.Value<long>();
        }

        // Otherwise it will be a string, but we need to convert it to a number.
        var s = json.Value<string>();
        if (s == null)
        {
            return null;
        }

        return s.GetHexAsLong();
    }

    public static string GetStringOrThrow(this JToken json) =>
        json.Value<string>() ?? throw new InvalidOperationException("JSON string is null.");

    public static void WriteDictionaryValue(
        this SentryJsonWriter writer,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger,
        bool includeNullValues = true)
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            if (includeNullValues)
            {
                foreach (var (key, value) in dic)
                {
                    writer.WriteDynamic(key, value, logger);
                }
            }
            else
            {
                foreach (var (key, value) in dic)
                {
                    if (value is not null)
                    {
                        writer.WriteDynamic(key, value, logger);
                    }
                }
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteDictionaryValue<TValue>(
        this SentryJsonWriter writer,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger,
        bool includeNullValues = true)
        where TValue : ISentryJsonSerializable?
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            foreach (var (key, value) in dic)
            {
                if (value is not null)
                {
                    writer.WriteSerializable(key, value, logger);
                }
                else if (includeNullValues)
                {
                    writer.WriteNull(key);
                }
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteStringDictionaryValue(
        this SentryJsonWriter writer,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            foreach (var (key, value) in dic)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteDictionary(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteDictionaryValue(dic, logger);
    }

    public static void WriteDictionary<TValue>(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger)
        where TValue : ISentryJsonSerializable?
    {
        writer.WritePropertyName(propertyName);
        writer.WriteDictionaryValue(dic, logger);
    }

    public static void WriteStringDictionary(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStringDictionaryValue(dic);
    }

    public static void WriteArrayValue<T>(
        this SentryJsonWriter writer,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        if (arr is not null)
        {
            writer.WriteStartArray();

            foreach (var i in arr)
            {
                writer.WriteDynamicValue(i, logger);
            }

            writer.WriteEndArray();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteArray<T>(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteArrayValue(arr, logger);
    }

    public static void WriteStringArrayValue(
        this SentryJsonWriter writer,
        IEnumerable<string?>? arr)
    {
        if (arr is not null)
        {
            writer.WriteStartArray();

            foreach (var i in arr)
            {
                writer.WriteStringValue(i);
            }

            writer.WriteEndArray();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteStringArray(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<string?>? arr)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStringArrayValue(arr);
    }

    public static void WriteSerializableValue(
        this SentryJsonWriter writer,
        ISentryJsonSerializable value,
        IDiagnosticLogger? logger)
    {
        value.WriteTo(writer, logger);
    }

    public static void WriteSerializable(
        this SentryJsonWriter writer,
        string propertyName,
        ISentryJsonSerializable value,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteSerializableValue(value, logger);
    }

    public static void WriteDynamicValue(
        this SentryJsonWriter writer,
        object? value,
        IDiagnosticLogger? logger)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else if (value is ISentryJsonSerializable serializable)
        {
            writer.WriteSerializableValue(serializable, logger);
        }
        else if (value is IEnumerable<KeyValuePair<string, string?>> sdic)
        {
            writer.WriteStringDictionaryValue(sdic);
        }
        else if (value is IEnumerable<KeyValuePair<string, object?>> dic)
        {
            writer.WriteDictionaryValue(dic, logger);
        }
        else if (value is string str)
        {
            writer.WriteStringValue(str);
        }
        else if (value is bool b)
        {
            writer.WriteBooleanValue(b);
        }
        else if (value is int i)
        {
            writer.WriteNumberValue(i);
        }
        else if (value is long l)
        {
            writer.WriteNumberValue(l);
        }
        else if (value is double d)
        {
            writer.WriteNumberValue(d);
        }
        else if (value is DateTime dt)
        {
            writer.WriteStringValue(dt);
        }
        else if (value is DateTimeOffset dto)
        {
            writer.WriteStringValue(dto);
        }
        else if (value is TimeSpan timeSpan)
        {
            writer.WriteStringValue(timeSpan.ToString("g", CultureInfo.InvariantCulture));
        }
        else if (value is IFormattable formattable)
        {
            writer.WriteStringValue(formattable.ToString(null, CultureInfo.InvariantCulture));
        }
        else if (value.GetType().ToString() == "System.RuntimeType")
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            if (!JsonPreserveReferences)
            {
                InternalSerialize(writer, value, preserveReferences: false);
                return;
            }

            try
            {
                // Use an intermediate byte array, so we can retry if serialization fails.
                var bytes = InternalSerializeToUtf8Bytes(value);
                writer.WriteRawValue(Encoding.UTF8.GetString(bytes));
            }
            catch (JsonException)
            {
                // Retry, preserving references to avoid cyclical dependency.
                InternalSerialize(writer, value, preserveReferences: true);
            }
        }
    }

    public static void WriteNumberValue(
        this SentryJsonWriter writer,
        long value)
    {
        writer.WriteValue(value);
    }

    public static void WriteNumberValue(
        this SentryJsonWriter writer,
        double value)
    {
        writer.WriteValue(value);
    }

    public static void WriteStringValue(
        this SentryJsonWriter writer,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteValue(value);
        }
    }

    public static void WriteStringValue(
        this SentryJsonWriter writer,
        DateTime value)
    {
        writer.WriteValue(value);
    }

    public static void WriteStringValue(
        this SentryJsonWriter writer,
        DateTimeOffset value)
    {
        writer.WriteValue(value);
    }

    public static void WriteBooleanValue(
        this SentryJsonWriter writer,
        bool value)
    {
        writer.WriteValue(value);
    }

    public static void WriteNullValue(
        this SentryJsonWriter writer)
    {
        writer.WriteNull();
    }

    internal static string ToUtf8Json(this object value, bool preserveReferences = false)
    {
        using var stream = new MemoryStream();
        using var writer = new SentryJsonWriter(stream);
        InternalSerialize(writer, value, preserveReferences);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonSerializerSettings GetSerializerSettings(bool preserveReferences = false)
    {
        return preserveReferences ? AltSerializerSettings : SerializerSettings;
    }

    private static byte[] InternalSerializeToUtf8Bytes(object value)
    {
        using var ms = new MemoryStream();
        using var jsonWriter = new SentryJsonWriter(ms);

        var serializer = JsonSerializer.Create(SerializerSettings);
        serializer.Serialize(jsonWriter, value);

        jsonWriter.Flush();

        return ms.ToArray();
    }

    private static void InternalSerialize(SentryJsonWriter writer, object value, bool preserveReferences = false)
    {
        var serializer = JsonSerializer.Create(GetSerializerSettings(preserveReferences));
        serializer.Serialize(writer, value);
    }
    public static void WriteDynamic(
        this SentryJsonWriter writer,
        string propertyName,
        object? value,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        int originalDepth = GetApproximateDepth(writer);
        try
        {
            writer.WriteDynamicValue(value, logger);
        }
        catch (Exception e)
        {
            // In the event of an instance that can't be serialized, we don't want to throw away a whole event
            // so we'll suppress issues here.
            logger?.LogError(e, "Failed to serialize object for property '{0}'.",
                propertyName);

            // The only location in the protocol we allow dynamic objects are Extra and Contexts.
            // Render an empty JSON object instead of null. This allows a round trip where this property name is the
            // key to a map which would otherwise not be set and result in a different object.
            // This affects envelope size which isn't recomputed after a roundtrip.

            // If the last token written was ":", then we must write a property value.
            // If the last token written was "{", then we can't write a property value.
            // Since either could happen, we will *try* to write a "{" and ignore any failure.
            try
            {
                writer.WriteStartObject();
            }
            catch (InvalidOperationException)
            {
                // Already in an object, just write an empty object value
                writer.WriteRawValue("{}");
                return;
            }

            // Close the object we just opened
                writer.WriteEndObject();
            }
        }

    private static int GetApproximateDepth(JsonWriter writer)
    {
        // Since CurrentDepth isn't available, we'll use the Path property as a fallback
        // This is not perfect but helps track nesting level
        return writer.Path?.Count(c => c == '.') ?? 0;
    }


    public static void WriteBooleanIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        bool? value)
    {
        if (value is not null)
        {
            writer.WriteBoolean(propertyName, value.Value);
        }
    }

    public static void WriteBooleanIfTrue(
        this SentryJsonWriter writer,
        string propertyName,
        bool? value)
    {
        if (value is true)
        {
            writer.WriteBoolean(propertyName, value.Value);
        }
    }

    public static void WriteBoolean(
        this SentryJsonWriter writer,
        string propertyName,
        bool value)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteValue(value);
    }

    public static void WriteNumber(
        this SentryJsonWriter writer,
        string propertyName,
        long? value)
    {
        if (value is not null)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteValue(value.Value);
        }
    }

    public static void WriteNumber(
        this SentryJsonWriter writer,
        string propertyName,
        double? value)
    {
        if (value is not null)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteValue(value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        short? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        int? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        long? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        float? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        double? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotZero(
        this SentryJsonWriter writer,
        string propertyName,
        short value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this SentryJsonWriter writer,
        string propertyName,
        int value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this SentryJsonWriter writer,
        string propertyName,
        long value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this SentryJsonWriter writer,
        string propertyName,
        float value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this SentryJsonWriter writer,
        string propertyName,
        double value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteStringIfNotWhiteSpace(
        this SentryJsonWriter writer,
        string propertyName,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(propertyName, value);
        }
    }

    public static void WriteStringIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        DateTimeOffset? value)
    {
        if (value is not null)
        {
            writer.WriteString(propertyName, value.Value);
        }
    }

    public static void WriteString(
        this SentryJsonWriter writer,
        string propertyName,
        object? value)
    {
        if (value is not null)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteValue(value.ToString());
        }
    }

    public static void WriteSerializableIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        ISentryJsonSerializable? value,
        IDiagnosticLogger? logger)
    {
        if (value is not null)
        {
            writer.WriteSerializable(propertyName, value, logger);
        }
    }

    public static void WriteDictionaryIfNotEmpty(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger)
    {
        var dictionary = dic as IReadOnlyDictionary<string, object?> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteDictionary(propertyName, dictionary, logger);
        }
    }

    public static void WriteDictionaryIfNotEmpty<TValue>(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger)
        where TValue : ISentryJsonSerializable?
    {
        var dictionary = dic as IReadOnlyDictionary<string, TValue> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteDictionary(propertyName, dictionary, logger);
        }
    }

    public static void WriteStringDictionaryIfNotEmpty(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        var dictionary = dic as IReadOnlyDictionary<string, string?> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteStringDictionary(propertyName, dictionary);
        }
    }

    public static void WriteArrayIfNotEmpty<T>(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        var list = arr as IReadOnlyList<T> ?? arr?.ToArray();
        if (list is not null && list.Count > 0)
        {
            writer.WriteArray(propertyName, list, logger);
        }
    }

    public static void WriteStringArrayIfNotEmpty(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumerable<string?>? arr)
    {
        var list = arr as IReadOnlyList<string?> ?? arr?.ToArray();
        if (list is not null && list.Count > 0)
        {
            writer.WriteStringArray(propertyName, list);
        }
    }

    public static void WriteDynamicIfNotNull(
        this SentryJsonWriter writer,
        string propertyName,
        object? value,
        IDiagnosticLogger? logger)
    {
        if (value is not null)
        {
            writer.WriteDynamic(propertyName, value, logger);
        }
    }

    public static void WriteString(
        this SentryJsonWriter writer,
        string propertyName,
        IEnumeration? value)
    {
        if (value == null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value.Value);
        }
    }

    public static void WriteNull(
        this SentryJsonWriter writer,
        string propertyName)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteNull();
    }

    public static void ConfigureJsonSettings(Action<JsonSerializerSettings> configureSettings)
    {throw new NotImplementedException();
    }
}

internal class SentryJsonContext
{
    private readonly JsonSerializerSettings _settings;

    public SentryJsonContext(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();

        // Register known converters
        RegisterConverters();
    }

    private void RegisterConverters()
    {
        // Register converters for GrowableArray<int>, Dictionary<string, bool>, Dictionary<string, object>
        // if needed, they would be added to _settings.Converters
    }

    public JsonSerializerSettings GetSettings() => _settings;
}
