using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// An event to be sent to Sentry.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/" />
[DebuggerDisplay("{GetType().Name,nq}: {" + nameof(EventId) + ",nq}")]
public sealed class SentryEvent : IEventLike, ISentryJsonSerializable
{
    private IDictionary<string, string>? _modules;

    /// <summary>
    /// The <see cref="System.Exception"/> used to create this event.
    /// </summary>
    /// <remarks>
    /// The information from this exception is used by the Sentry SDK
    /// to add the relevant data to the event prior to sending to Sentry.
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// The unique identifier of this event.
    /// </summary>
    /// <remarks>
    /// Hexadecimal string representing a uuid4 value.
    /// The length is exactly 32 characters (no dashes!).
    /// </remarks>
    public SentryId EventId { get; }

    /// <summary>
    /// Indicates when the event was created.
    /// </summary>
    /// <example>2018-04-03T17:41:36</example>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the structured message that describes this event.
    /// </summary>
    /// <remarks>
    /// This helps Sentry group events together as the grouping happens
    /// on the template message instead of the result string message.
    /// </remarks>
    /// <example>
    /// SentryMessage will have a template like: 'user {0} logged in'
    /// Or structured logging template: '{user} has logged in'
    /// </example>
    public SentryMessage? Message { get; set; }

    /// <summary>
    /// Name of the logger (or source) of the event.
    /// </summary>
    public string? Logger { get; set; }

    /// <inheritdoc />
    public string? Platform { get; set; }

    /// <summary>
    /// Identifies the computer from which the event was recorded.
    /// </summary>
    public string? ServerName { get; set; }

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    internal SentryValues<SentryException>? SentryExceptionValues { get; set; }

    /// <summary>
    /// The Sentry Exception interface.
    /// </summary>
    public IEnumerable<SentryException>? SentryExceptions
    {
        get => SentryExceptionValues?.Values ?? Enumerable.Empty<SentryException>();
        set => SentryExceptionValues = value != null ? new SentryValues<SentryException>(value) : null;
    }

    private SentryValues<SentryThread>? SentryThreadValues { get; set; }

    /// <summary>
    /// The Sentry Thread interface.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
    public IEnumerable<SentryThread>? SentryThreads
    {
        get => SentryThreadValues?.Values ?? Enumerable.Empty<SentryThread>();
        set => SentryThreadValues = value != null ? new SentryValues<SentryThread>(value) : null;
    }

    /// <summary>
    /// The Sentry Debug Meta Images interface.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta#debug-images"/>
    public List<DebugImage>? DebugImages
    {
        get => _debugMeta?.Images;
        set
        {
            _debugMeta ??= new();
            _debugMeta.Images = value;
        }
    }

    private DebugMeta? _debugMeta;

    /// <summary>
    /// A list of relevant modules and their versions.
    /// </summary>
    public IDictionary<string, string> Modules => _modules ??= new Dictionary<string, string>();

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    /// <inheritdoc />
    public string? TransactionName { get; set; }

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

    /// <inheritdoc />
    public SdkVersion Sdk { get; internal set; } = new();

    private IReadOnlyList<string>? _fingerprint;

    /// <inheritdoc />
    public IReadOnlyList<string> Fingerprint
    {
        get => _fingerprint ?? Array.Empty<string>();
        set => _fingerprint = value;
    }

    // Default values are null so no serialization of empty objects or arrays
    private List<Breadcrumb>? _breadcrumbs;

    /// <inheritdoc />
    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs ??= new List<Breadcrumb>();

    private Dictionary<string, object?>? _extra;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Extra => _extra ??= new Dictionary<string, object?>();

    private Dictionary<string, string>? _tags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

    internal bool HasException() => Exception is not null || SentryExceptions?.Any() == true;

    internal bool HasTerminalException()
    {
        // The exception is considered terminal if it is marked unhandled,
        // UNLESS it comes from the UnobservedTaskExceptionIntegration

        if (Exception?.Data[Mechanism.HandledKey] is false)
        {
            return Exception.Data[Mechanism.MechanismKey] as string != UnobservedTaskExceptionIntegration.MechanismKey;
        }

        return SentryExceptions?.Any(e =>
            e.Mechanism is { Handled: false } mechanism &&
            mechanism.Type != UnobservedTaskExceptionIntegration.MechanismKey
        ) ?? false;
    }

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="T:Sentry.SentryEvent" />.
    /// </summary>
    public SentryEvent() : this(null)
    {
    }

    /// <summary>
    /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public SentryEvent(Exception? exception)
        : this(exception, null)
    {
    }

    internal SentryEvent(
        Exception? exception = null,
        DateTimeOffset? timestamp = null,
        SentryId eventId = default)
    {
        Exception = exception;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        EventId = eventId != default ? eventId : SentryId.Create();
        Platform = SentryConstants.Platform;
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) =>
        (_breadcrumbs ??= new List<Breadcrumb>()).Add(breadcrumb);

    /// <inheritdoc />
    public void SetExtra(string key, object? value) =>
        (_extra ??= new Dictionary<string, object?>())[key] = value;

    /// <inheritdoc />
    public void SetTag(string key, string value) =>
        (_tags ??= new Dictionary<string, string>())[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) =>
        (_tags ??= new Dictionary<string, string>()).Remove(key);

    internal void Redact()
    {
        foreach (var breadcrumb in Breadcrumbs)
        {
            breadcrumb.Redact();
        }
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (_modules != null && _modules.Count > 0)
        {
            writer.WritePropertyName("modules");
            JsonSerializer.Create().Serialize(writer, _modules);
        }

        writer.WritePropertyName("event_id");
        EventId.WriteTo(writer, logger);

        writer.WritePropertyName("timestamp");
        writer.WriteValue(Timestamp);

        if (Message != null)
        {
            writer.WritePropertyName("logentry");
            Message.WriteTo(writer, logger);
        }

        if (!string.IsNullOrWhiteSpace(Logger))
        {
            writer.WritePropertyName("logger");
            writer.WriteValue(Logger);
        }

        if (!string.IsNullOrWhiteSpace(Platform))
        {
            writer.WritePropertyName("platform");
            writer.WriteValue(Platform);
        }

        if (!string.IsNullOrWhiteSpace(ServerName))
        {
            writer.WritePropertyName("server_name");
            writer.WriteValue(ServerName);
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

        if (SentryExceptionValues != null)
        {
            writer.WritePropertyName("exception");
            SentryExceptionValues.WriteTo(writer, logger);
        }

        if (SentryThreadValues != null)
        {
            writer.WritePropertyName("threads");
            SentryThreadValues.WriteTo(writer, logger);
        }

        if (Level.HasValue)
        {
            writer.WritePropertyName("level");
            writer.WriteValue(Level.Value.ToString().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(TransactionName))
        {
            writer.WritePropertyName("transaction");
            writer.WriteValue(TransactionName);
        }

        if (_request != null)
        {
            writer.WritePropertyName("request");
            _request.WriteTo(writer, logger);
        }

        var contextsNotEmpty = _contexts.NullIfEmpty();
        if (contextsNotEmpty != null)
        {
            writer.WritePropertyName("contexts");
            contextsNotEmpty.WriteTo(writer, logger);
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

        if (_fingerprint != null && _fingerprint.Count > 0)
        {
            writer.WritePropertyName("fingerprint");
            writer.WriteStartArray();
            foreach (var item in _fingerprint)
            {
                writer.WriteValue(item);
            }
            writer.WriteEndArray();
        }

        if (_breadcrumbs != null && _breadcrumbs.Count > 0)
        {
            writer.WritePropertyName("breadcrumbs");
            writer.WriteStartArray();
            foreach (var breadcrumb in _breadcrumbs)
            {
                breadcrumb.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
        }

        if (_extra != null && _extra.Count > 0)
        {
            writer.WritePropertyName("extra");
            writer.WriteStartObject();
            foreach (var item in _extra)
            {
                writer.WritePropertyName(item.Key);
                JsonSerializer.Create().Serialize(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        if (_tags != null && _tags.Count > 0)
        {
            writer.WritePropertyName("tags");
            JsonSerializer.Create().Serialize(writer, _tags);
        }

        if (_debugMeta != null)
        {
            writer.WritePropertyName("debug_meta");
            _debugMeta.WriteTo(writer, logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryEvent FromJson(JToken json) => FromJson(json, null);

    private static SentryLevel? SafeLevelFromJson(JToken json)
    {
        var levelToken = json["level"];
        if (levelToken == null || levelToken.Type == JTokenType.Null)
            return null;

        var levelString = levelToken.Value<string>();
        if (levelString == null)
            return null;

        // Native SentryLevel.None does not exist in dotnet
        return levelString.ToLowerInvariant() switch
        {
            "debug" => SentryLevel.Debug,
            "info" => SentryLevel.Info,
            "warning" => SentryLevel.Warning,
            "fatal" => SentryLevel.Fatal,
            "error" => SentryLevel.Error,
            _ => null
        };
    }

    internal static SentryEvent FromJson(JToken json, Exception? exception)
    {
        var modules = json["modules"]?.ToObject<Dictionary<string, string?>>();
        var eventId = json["event_id"] != null ? SentryId.FromJson(json["event_id"]!) : SentryId.Empty;
        var timestamp = json["timestamp"]?.Value<DateTimeOffset>() ?? DateTimeOffset.MinValue; // Native sentryevents are serialized to epoch timestamps
        var message = json["logentry"] != null ? SentryMessage.FromJson(json["logentry"]!) : null;
        var logger = json["logger"]?.Value<string>();
        var platform = json["platform"]?.Value<string>();
        var serverName = json["server_name"]?.Value<string>();
        var release = json["release"]?.Value<string>();
        var distribution = json["dist"]?.Value<string>();

        var exceptionValues = json["exception"]?["values"]?.ToObject<JArray>()?.Select(token => SentryException.FromJson(token)).ToList();
        var sentryExceptionValues = exceptionValues != null ? new SentryValues<SentryException>(exceptionValues) : null;

        var threadValues = json["threads"]?["values"]?.ToObject<JArray>()?.Select(token => SentryThread.FromJson(token)).ToList();
        var sentryThreadValues = threadValues != null ? new SentryValues<SentryThread>(threadValues) : null;

        var transaction = json["transaction"]?.Value<string>();
        var request = json["request"] != null ? SentryRequest.FromJson(json["request"]!) : null;
        var contexts = json["contexts"] != null ? SentryContexts.FromJson(json["contexts"]!) : null;
        var user = json["user"] != null ? SentryUser.FromJson(json["user"]!) : null;
        var environment = json["environment"]?.Value<string>();
        var sdk = json["sdk"] != null ? SdkVersion.FromJson(json["sdk"]!) : new SdkVersion();
        var fingerprint = json["fingerprint"]?.ToObject<JArray>()?.Select(j => j.Value<string>()).ToArray();
        var breadcrumbs = json["breadcrumbs"]?.ToObject<JArray>()?.Select(token => Breadcrumb.FromJson((JObject)token)).ToList();
        var extra = json["extra"]?.ToObject<Dictionary<string, object?>>();
        var tags = json["tags"]?.ToObject<Dictionary<string, string?>>();
        var level = SafeLevelFromJson(json);
        var debugMeta = json["debug_meta"] != null ? DebugMeta.FromJson(json["debug_meta"]!) : null;

        return new SentryEvent(exception, timestamp, eventId)
        {
            _modules = modules?.WhereNotNullValue().ToDict(),
            Message = message,
            Logger = logger,
            Platform = platform,
            ServerName = serverName,
            Release = release,
            Distribution = distribution,
            SentryExceptionValues = sentryExceptionValues,
            SentryThreadValues = sentryThreadValues,
            _debugMeta = debugMeta,
            Level = level,
            TransactionName = transaction,
            _request = request,
            Contexts = contexts ?? new(),
            _user = user,
            Environment = environment,
            Sdk = sdk,
            _fingerprint = fingerprint!,
            _breadcrumbs = breadcrumbs,
            _extra = extra?.ToDict(),
            _tags = tags?.WhereNotNullValue().ToDict()
        };
    }
}
