using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using Sentry.Protocol.Metrics;

namespace Sentry;

// https://develop.sentry.dev/sdk/event-payloads/span
/// <summary>
/// Transaction span.
/// </summary>
public class SentrySpan : ISpanData, ISentryJsonSerializable
{
    /// <inheritdoc />
    public SpanId SpanId { get; private set; }

    /// <inheritdoc />
    public SpanId? ParentSpanId { get; private set; }

    /// <inheritdoc />
    public SentryId TraceId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp { get; private set; }

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    // Not readonly because of deserialization
    private Dictionary<string, Measurement>? _measurements;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements ??= new Dictionary<string, Measurement>();

    /// <inheritdoc />
    public void SetMeasurement(string name, Measurement measurement) =>
        (_measurements ??= new Dictionary<string, Measurement>())[name] = measurement;

    /// <inheritdoc />
    public string Operation { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public SpanStatus? Status { get; set; }

    /// <inheritdoc />
    public bool? IsSampled { get; internal set; }

    private Dictionary<string, string>? _tags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

    /// <inheritdoc />
    public void SetTag(string key, string value) =>
        (_tags ??= new Dictionary<string, string>())[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) =>
        (_tags ??= new Dictionary<string, string>()).Remove(key);

    // Aka 'data'
    private readonly MetricsSummary? _metricsSummary;


    private Dictionary<string, object?>? _data;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Data =>
        _data ??= new Dictionary<string, object?>();

    /// <inheritdoc />
    public void SetData(string key, object? value) =>
        (_data ??= new Dictionary<string, object?>())[key] = value;

    /// <inheritdoc />
    [Obsolete("Use SetData")]
    public IReadOnlyDictionary<string, object?> Extra => Data;

    /// <inheritdoc />
    [Obsolete("Use Data")]
    public void SetExtra(string key, object? value) => SetData(key, value);

    /// <summary>
    /// Initializes an instance of <see cref="SentrySpan"/>.
    /// </summary>
    public SentrySpan(SpanId? parentSpanId, string operation)
    {
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentrySpan"/>.
    /// </summary>
    public SentrySpan(ISpan tracer)
        : this(tracer.ParentSpanId, tracer.Operation)
    {
        SpanId = tracer.SpanId;
        TraceId = tracer.TraceId;
        StartTimestamp = tracer.StartTimestamp;
        EndTimestamp = tracer.EndTimestamp;
        Description = tracer.Description;
        Status = tracer.Status;
        IsSampled = tracer.IsSampled;
        _data = tracer.Data.ToDict();

        if (tracer is SpanTracer spanTracer)
        {
            _measurements = spanTracer.InternalMeasurements?.ToDict();
            _tags = spanTracer.InternalTags?.ToDict();
            if (spanTracer.HasMetrics)
            {
                _metricsSummary = new MetricsSummary(spanTracer.MetricsSummary);
            }
        }
        else
        {
            _measurements = tracer.Measurements.ToDict();
            _tags = tracer.Tags.ToDict();
        }
    }

    /// <inheritdoc />
    public SentryTraceHeader GetTraceHeader() => new(
        TraceId,
        SpanId,
        IsSampled);

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("span_id");
        SpanId.WriteTo(writer, logger);

        if (ParentSpanId.HasValue)
        {
            writer.WritePropertyName("parent_span_id");
            ParentSpanId.Value.WriteTo(writer, logger);
        }

        writer.WritePropertyName("trace_id");
        TraceId.WriteTo(writer, logger);

        if (!string.IsNullOrWhiteSpace(Operation))
        {
            writer.WritePropertyName("op");
            writer.WriteValue(Operation);
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            writer.WritePropertyName("description");
            writer.WriteValue(Description);
        }

        if (Status.HasValue)
        {
            writer.WritePropertyName("status");
            writer.WriteValue(Status.Value.ToString().ToSnakeCase());
        }

        writer.WritePropertyName("start_timestamp");
        writer.WriteValue(StartTimestamp);

        if (EndTimestamp.HasValue)
        {
            writer.WritePropertyName("timestamp");
            writer.WriteValue(EndTimestamp.Value);
        }

        if (_tags?.Count > 0)
        {
            writer.WriteStringDictionary("tags", _tags.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
        }

        if (_data?.Count > 0)
        {
            writer.WritePropertyName("data");
            writer.WriteDictionaryValue(_data, logger);
        }

        if (_measurements?.Count > 0)
        {
            writer.WritePropertyName("measurements");
            writer.WriteDictionaryValue(_measurements, logger);
        }

        if (_metricsSummary != null)
        {
            writer.WritePropertyName("_metrics_summary");
            _metricsSummary.WriteTo(writer, logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses a span from JSON.
    /// </summary>
    public static SentrySpan FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var spanId = json["span_id"] != null ? SpanId.FromJson(json["span_id"]!) : SpanId.Empty;
        var parentSpanId = json["parent_span_id"]?.ToObject<SpanId?>();
        var traceId = json["trace_id"] != null ? SentryId.FromJson(json["trace_id"]!) : SentryId.Empty;
        var startTimestamp = json["start_timestamp"]!.Value<DateTimeOffset>();
        var endTimestamp = json["timestamp"]?.Value<DateTimeOffset?>();
        var operation = json["op"]?.Value<string>() ?? "unknown";
        var description = json["description"]?.Value<string>();
        var status = json["status"]?.Value<string>()?.Replace("_", "").ParseEnum<SpanStatus>();
        var isSampled = json["sampled"]?.Value<bool?>();
        var tags = json["tags"]?.ToObject<Dictionary<string, string>?>()?.ToDict();
        var measurements = json["measurements"]?.ToObject<Dictionary<string, Measurement>?>();
        var data = json["data"]?.ToObject<Dictionary<string, object?>?>()?.ToDict();

        return new SentrySpan(parentSpanId, operation)
        {
            SpanId = spanId,
            TraceId = traceId,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp,
            Description = description,
            Status = status,
            IsSampled = isSampled,
            _tags = tags!,
            _data = data!,
            _measurements = measurements,
        };
    }

    internal void Redact()
    {
        Description = Description?.RedactUrl();
    }

    /// <inheritdoc />
    public string? Origin
    {
        get => _origin;
        internal set
        {
            if (!OriginHelper.IsValidOrigin(value))
            {
                throw new ArgumentException("Invalid origin");
            }
            _origin = value;
        }
    }
    private string? _origin;
}
