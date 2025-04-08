using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Sentry Exception interface.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/exception"/>
public sealed class SentryException : ISentryJsonSerializable
{
    /// <summary>
    /// Exception Type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The exception value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The optional module, or package which the exception type lives in.
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// An optional value which refers to a thread in the threads interface.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
    /// <seealso cref="SentryThread"/>
    public int ThreadId { get; set; }

    /// <summary>
    /// Stack trace.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
    public SentryStackTrace? Stacktrace { get; set; }

    /// <summary>
    /// An optional mechanism that created this exception.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception/#exception-mechanism"/>
    public Mechanism? Mechanism { get; set; }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteValue(Type);

        writer.WritePropertyName("value");
        writer.WriteValue(Value);

        writer.WritePropertyName("module");
        writer.WriteValue(Module);

        if (ThreadId != 0)
        {
            writer.WritePropertyName("thread_id");
            writer.WriteValue(ThreadId);
        }

        if (Stacktrace != null)
        {
            writer.WritePropertyName("stacktrace");
            Stacktrace.WriteTo(writer, logger);
        }

        if (Mechanism?.IsDefaultOrEmpty() == false)
        {
            writer.WritePropertyName("mechanism");
            Mechanism.WriteTo(writer, logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryException FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var type = json["type"]?.Value<string>();
        var value = json["value"]?.Value<string>();
        var module = json["module"]?.Value<string>();
        var threadId = json["thread_id"]?.Value<int>() ?? 0;
        var stacktrace = json["stacktrace"] != null ? SentryStackTrace.FromJson(json["stacktrace"]!) : null;
        var mechanism = json["mechanism"] != null ? Mechanism.FromJson(json["mechanism"]!) : null;

        if (mechanism?.IsDefaultOrEmpty() == true)
        {
            mechanism = null;
        }

        return new SentryException
        {
            Type = type,
            Value = value,
            Module = module,
            ThreadId = threadId,
            Stacktrace = stacktrace,
            Mechanism = mechanism
        };
    }
}
