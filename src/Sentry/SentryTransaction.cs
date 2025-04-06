using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using Sentry.Protocol.Metrics;

namespace Sentry;

// https://develop.sentry.dev/sdk/event-payloads/transaction
/// <summary>
/// Sentry performance transaction.
/// </summary>
public class SentryTransaction : ITransactionData, ISentryJsonSerializable
{
    /// <summary>
    /// Transaction's event ID.
    /// </summary>
    public SentryId EventId { get; private set; }

    /// <inheritdoc />
    public SpanId SpanId
    {
        get => Contexts.Trace.SpanId;
        private set => Contexts.Trace.SpanId = value;
    }

    /// <inheritdoc />
    public string? Origin
    {
        get => Contexts.Trace.Origin;
        private set => Contexts.Trace.Origin = value;
    }

    // A transaction normally does not have a parent because it represents
    // the top node in the span hierarchy.
    // However, a transaction may also be continued from a trace header
    // (i.e. when another service sends a request to this service),
    // in which case the newly created transaction refers to the incoming
    // transaction as the parent.

    /// <inheritdoc />
    public SpanId? ParentSpanId
    {
        get => Contexts.Trace.ParentSpanId;
        private set => Contexts.Trace.ParentSpanId = value;
    }

    /// <inheritdoc />
    public SentryId TraceId
    {
        get => Contexts.Trace.TraceId;
        private set => Contexts.Trace.TraceId = value;
    }

    /// <inheritdoc />
    public string Name { get; private set; }

    /// <inheritdoc />
    public TransactionNameSource NameSource { get; }

    /// <inheritdoc />
    public bool? IsParentSampled { get; set; }

    /// <inheritdoc />
    public string? Platform { get; set; } = SentryConstants.Platform;

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp { get; internal set; } // internal for testing

    // Not readonly because of deserialization
    private Dictionary<string, Measurement>? _measurements;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements ??= new Dictionary<string, Measurement>();

    /// <inheritdoc />
    public void SetMeasurement(string name, Measurement measurement) =>
        (_measurements ??= new Dictionary<string, Measurement>())[name] = measurement;

    /// <inheritdoc />
    public string Operation
    {
        get => Contexts.Trace.Operation;
        private set => Contexts.Trace.Operation = value;
    }

    /// <inheritdoc />
    public string? Description
    {
        get => Contexts.Trace.Description;
        set => Contexts.Trace.Description = value;
    }

    /// <inheritdoc />
    public SpanStatus? Status
    {
        get => Contexts.Trace.Status;
        private set => Contexts.Trace.Status = value;
    }

    /// <inheritdoc />
    public bool? IsSampled
    {
        get => Contexts.Trace.IsSampled;
        internal set
        {
            Contexts.Trace.IsSampled = value;
            SampleRate ??= value == null ? null : value.Value ? 1.0 : 0.0;
        }
    }

    /// <inheritdoc />
    public double? SampleRate { get; internal set; }

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    private SentryRequest? _request;

    /// <inheritdoc />
    public SentryRequest Request
    {
        get => _request ??= new SentryRequest();
        set => _request = value;
    }

    private readonly SentryContexts _contexts = new();

    /// <inheritdoc />
    public SentryContexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    private SentryUser? _user;

    /// <inheritdoc />
    public SentryUser User
    {
        get => _user ??= new SentryUser();
        set => _user = value;
    }

    /// <inheritdoc />
    public string? Environment { get; set; }

    // This field exists on SentryEvent and Scope, but not on Transaction
    string? IEventLike.TransactionName
    {
        get => Name;
        set => Name = value ?? "";
    }

    /// <inheritdoc />
    public SdkVersion Sdk { get; internal set; } = new();

    private IReadOnlyList<string>? _fingerprint;

    /// <inheritdoc />
    public IReadOnlyList<string> Fingerprint
    {
        get => _fingerprint ?? Array.Empty<string>();
        set => _fingerprint = value;
    }

    // Not readonly because of deserialization
    private List<Breadcrumb> _breadcrumbs = new();

    /// <inheritdoc />
    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

    // Not readonly because of deserialization
    private Dictionary<string, string> _tags = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags;

    // Not readonly because of deserialization
    private SentrySpan[] _spans = Array.Empty<SentrySpan>();
    private readonly MetricsSummary? _metricsSummary;

    /// <summary>
    /// Flat list of spans within this transaction.
    /// </summary>
    public IReadOnlyCollection<SentrySpan> Spans => _spans;

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    internal ITransactionProfiler? TransactionProfiler { get; set; }

    // This constructor is used for deserialization purposes.
    // It's required because some of the fields are mapped on 'contexts.trace'.
    // When deserializing, we don't parse those fields explicitly, but
    // instead just parse the trace context and resolve them later.
    // Hence why we need a constructor that doesn't take the operation to avoid
    // overwriting it.
    private SentryTransaction(string name, TransactionNameSource nameSource)
    {
        EventId = SentryId.Create();
        Name = name;
        NameSource = nameSource;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(string name, string operation)
        : this(name, TransactionNameSource.Custom)
    {
        SpanId = SpanId.Create();
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(string name, string operation, TransactionNameSource nameSource)
        : this(name, nameSource)
    {
        SpanId = SpanId.Create();
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(ITransactionTracer tracer)
        : this(tracer.Name, tracer.NameSource)
    {
        // Contexts have to be set first because other fields use that
        Contexts = tracer.Contexts;

        ParentSpanId = tracer.ParentSpanId;
        SpanId = tracer.SpanId;
        TraceId = tracer.TraceId;
        Operation = tracer.Operation;
        Platform = tracer.Platform;
        Release = tracer.Release;
        Distribution = tracer.Distribution;
        StartTimestamp = tracer.StartTimestamp;
        EndTimestamp = tracer.EndTimestamp;
        Description = tracer.Description;
        Status = tracer.Status;
        IsSampled = tracer.IsSampled;
        Level = tracer.Level;
        Request = tracer.Request;
        User = tracer.User;
        Environment = tracer.Environment;
        Sdk = tracer.Sdk;
        Fingerprint = tracer.Fingerprint;
        _breadcrumbs = tracer.Breadcrumbs.ToList();
        _tags = tracer.Tags.ToDict();

        _spans = FromTracerSpans(tracer);
        _measurements = tracer.Measurements.ToDict();

        // Some items are not on the interface, but we only ever pass in a TransactionTracer anyway.
        if (tracer is TransactionTracer transactionTracer)
        {
            SampleRate = transactionTracer.SampleRate;
            DynamicSamplingContext = transactionTracer.DynamicSamplingContext;
            TransactionProfiler = transactionTracer.TransactionProfiler;
            if (transactionTracer.HasMetrics)
            {
                _metricsSummary = new MetricsSummary(transactionTracer.MetricsSummary);
            }
        }
    }

    internal static SentrySpan[] FromTracerSpans(ITransactionTracer tracer)
    {
        // Filter sentry requests created by Sentry.OpenTelemetry.SentrySpanProcessor
        var nonSentrySpans = tracer.Spans
            .Where(s => s is not SpanTracer { IsSentryRequest: true });

        if (tracer is not IBaseTracer { IsOtelInstrumenter: true })
        {
            return nonSentrySpans.Select(s => new SentrySpan(s)).ToArray();
        }

        Dictionary<SpanId, SpanId?> reHome = new();
        var spans = nonSentrySpans.ToList();
        foreach (var value in spans.ToArray())
        {
            if (value is not SpanTracer child)
            {
                continue;
            }

            // Remove any filtered spans
            if (child.IsFiltered?.Invoke() == true)
            {
                reHome.Add(child.SpanId, child.ParentSpanId);
                spans.Remove(child);
            }
        }

        // Re-home any children of filtered spans
        foreach (var value in spans)
        {
            if (value is not SpanTracer child)
            {
                continue;
            }

            while (child.ParentSpanId.HasValue && reHome.TryGetValue(child.ParentSpanId.Value, out var newParentId))
            {
                child.ParentSpanId = newParentId;
            }
        }

        return spans.Select(s => new SentrySpan(s)).ToArray();
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) =>
        _breadcrumbs.Add(breadcrumb);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Data => _contexts.Trace.Data;

    /// <inheritdoc />
    [Obsolete("Use Data")]
    public IReadOnlyDictionary<string, object?> Extra => _contexts.Trace.Data;

    /// <inheritdoc />
    [Obsolete("Use SetData")]
    public void SetExtra(string key, object? value) =>
        SetData(key, value);

    /// <inheritdoc />
    public void SetData(string key, object? value) =>
        _contexts.Trace.SetData(key, value);

    /// <inheritdoc />
    public void SetTag(string key, string value) =>
        _tags[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) =>
        _tags.Remove(key);

    /// <inheritdoc />
    public SentryTraceHeader GetTraceHeader() => new(
        TraceId,
        SpanId,
        IsSampled);

    /// <summary>
    /// Redacts PII from the transaction
    /// </summary>
    internal void Redact()
    {
        Description = Description?.RedactUrl();
        foreach (var breadcrumb in Breadcrumbs)
        {
            breadcrumb.Redact();
        }

        foreach (var span in Spans)
        {
            span.Redact();
        }
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteValue("transaction");

        writer.WritePropertyName("event_id");
        EventId.WriteTo(writer, logger);

        if (Level.HasValue)
        {
            writer.WritePropertyName("level");
            writer.WriteValue(Level.Value.ToString().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(Platform))
        {
            writer.WritePropertyName("platform");
            writer.WriteValue(Platform);
        }

        if (!string.IsNullOrWhiteSpace(Release))
        {
            writer.WritePropertyName("release");
            writer.WriteValue(Release);
        }

        if (!string.IsNullOrWhiteSpace(Distribution))
        {
            writer.WritePropertyName("dist");
            writer.WriteValue(Distribution);
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            writer.WritePropertyName("transaction");
            writer.WriteValue(Name);
        }

        writer.WritePropertyName("transaction_info");
        writer.WriteStartObject();
        writer.WritePropertyName("source");
        writer.WriteValue(NameSource.ToString().ToLowerInvariant());
        writer.WriteEndObject();

        writer.WritePropertyName("start_timestamp");
        writer.WriteValue(StartTimestamp);

        if (EndTimestamp.HasValue)
        {
            writer.WritePropertyName("timestamp");
            writer.WriteValue(EndTimestamp.Value);
        }

        if (_request != null)
        {
            writer.WritePropertyName("request");
            _request.WriteTo(writer, logger);
        }

        if (!_contexts.Values.Any())
        {
            writer.WritePropertyName("contexts");
            _contexts.WriteTo(writer, logger);
        }

        if (_user != null)
        {
            writer.WritePropertyName("user");
            _user.WriteTo(writer, logger);
        }

        if (!string.IsNullOrWhiteSpace(Environment))
        {
            writer.WritePropertyName("environment");
            writer.WriteValue(Environment);
        }

        writer.WritePropertyName("sdk");
        Sdk.WriteTo(writer, logger);

        if (_fingerprint?.Count > 0)
        {
            writer.WritePropertyName("fingerprint");
            writer.WriteStartArray();
            foreach (var value in _fingerprint)
            {
                writer.WriteValue(value);
            }
            writer.WriteEndArray();
        }

        if (_breadcrumbs.Count > 0)
        {
            writer.WritePropertyName("breadcrumbs");
            writer.WriteStartArray();
            foreach (var breadcrumb in _breadcrumbs)
            {
                breadcrumb.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
        }

        if (_tags.Count > 0)
        {
            writer.WritePropertyName("tags");
            writer.WriteStartObject();
            foreach (var tag in _tags)
            {
                writer.WritePropertyName(tag.Key);
                writer.WriteValue(tag.Value);
            }
        writer.WriteEndObject();
        }

        if (_spans.Length > 0)
        {
            writer.WritePropertyName("spans");
            writer.WriteStartArray();
            foreach (var span in _spans)
            {
                span.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
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
    /// Parses transaction from JSON.
    /// </summary>
    public static SentryTransaction FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var eventId = json["event_id"]?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
        var name = json["transaction"]?.Value<string>() ?? throw new InvalidOperationException("Transaction name is required");
        var nameSource = json["transaction_info"]?["source"]?.Value<string>()?.ParseEnum<TransactionNameSource>() ?? TransactionNameSource.Custom;
        var startTimestamp = json["start_timestamp"]?.Value<DateTimeOffset>() ?? DateTimeOffset.UtcNow;
        var endTimestamp = json["timestamp"]?.Value<DateTimeOffset?>();
        var level = json["level"]?.Value<string>()?.ParseEnum<SentryLevel>();
        var platform = json["platform"]?.Value<string>();
        var release = json["release"]?.Value<string>();
        var distribution = json["dist"]?.Value<string>();
        var request = json["request"]?.Pipe(SentryRequest.FromJson);
        var contexts = json["contexts"]?.Pipe(SentryContexts.FromJson) ?? new();
        var user = json["user"]?.Pipe(SentryUser.FromJson);
        var environment = json["environment"]?.Value<string>();
        var sdk = json["sdk"]?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
        var fingerprint = json["fingerprint"]?.ToObject<string[]>();
        var breadcrumbs = json["breadcrumbs"]?.Select(Breadcrumb.FromJson).ToList() ?? new();
        var extra = json["extra"]?.ToObject<Dictionary<string, object?>>() ?? new();
        var tags = json["tags"]?.ToObject<Dictionary<string, string?>>()?.WhereNotNullValue().ToDict() ?? new();
        var measurements = json["measurements"]?.ToObject<Dictionary<string, Measurement>>() ?? new();
        var spans = json["spans"]?.Select(SentrySpan.FromJson).ToArray() ?? Array.Empty<SentrySpan>();

        return new SentryTransaction(name, nameSource)
        {
            EventId = eventId,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp,
            Level = level,
            Platform = platform,
            Release = release,
            Distribution = distribution,
            _request = request,
            Contexts = contexts,
            _user = user,
            Environment = environment,
            Sdk = sdk,
            _fingerprint = fingerprint,
            _breadcrumbs = breadcrumbs,
            _tags = tags,
            _measurements = measurements,
            _spans = spans
        };
    }
}
