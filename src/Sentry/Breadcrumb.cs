using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sentry;

/// <summary>
/// Series of application events.
/// </summary>
[DebuggerDisplay("Message: {" + nameof(Message) + "}, Type: {" + nameof(Type) + "}")]
public sealed class Breadcrumb : ISentryJsonSerializable
{
    private readonly IReadOnlyDictionary<string, string>? _data;
    private readonly string? _message;

    private bool _sendDefaultPii = true;
    internal void Redact() => _sendDefaultPii = false;

    /// <summary>
    /// A timestamp representing when the breadcrumb occurred.
    /// </summary>
    /// <remarks>
    /// This can be either an ISO datetime string, or a Unix timestamp.
    /// </remarks>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// If a message is provided, it’s rendered as text and the whitespace is preserved.
    /// Very long text might be abbreviated in the UI.
    /// </summary>
    public string? Message
    {
        get => _sendDefaultPii ? _message : _message?.RedactUrl();
        private init => _message = value;
    }

    /// <summary>
    /// The type of breadcrumb.
    /// </summary>
    /// <remarks>
    /// The default type is default which indicates no specific handling.
    /// Other types are currently http for HTTP requests and navigation for navigation events.
    /// </remarks>
    public string? Type { get; }

    /// <summary>
    /// Data associated with this breadcrumb.
    /// </summary>
    /// <remarks>
    /// Contains a sub-object whose contents depend on the breadcrumb type.
    /// Additional parameters that are unsupported by the type are rendered as a key/value table.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Data
    {
        get => _sendDefaultPii
            ? _data
            : _data?.ToDictionary(
                x => x.Key,
                x => x.Value.RedactUrl()
            )
        ;
        private init => _data = value;
    }

    /// <summary>
    /// Dotted strings that indicate what the crumb is or where it comes from.
    /// </summary>
    /// <remarks>
    /// Typically it’s a module name or a descriptive string.
    /// For instance aspnet.mvc.filter could be used to indicate that it came from an Action Filter.
    /// </remarks>
    public string? Category { get; }

    /// <summary>
    /// The level of the event.
    /// </summary>
    /// <remarks>
    /// Levels are used in the UI to emphasize and de-emphasize the crumb.
    /// </remarks>
    public BreadcrumbLevel Level { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Breadcrumb"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="type">The type.</param>
    /// <param name="data">The data.</param>
    /// <param name="category">The category.</param>
    /// <param name="level">The level.</param>
    public Breadcrumb(
        string message,
        string type,
        IReadOnlyDictionary<string, string>? data = null,
        string? category = null,
        BreadcrumbLevel level = default)
        : this(
            null,
            message,
            type,
            data,
            category,
            level)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Breadcrumb"/> class.
    /// </summary>
    /// <param name="timestamp"></param>
    /// <param name="message">The message.</param>
    /// <param name="type">The type.</param>
    /// <param name="data">The data.</param>
    /// <param name="category">The category.</param>
    /// <param name="level">The level.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal Breadcrumb(
        DateTimeOffset? timestamp = null,
        string? message = null,
        string? type = null,
        IReadOnlyDictionary<string, string>? data = null,
        string? category = null,
        BreadcrumbLevel level = default)
    {
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        Message = message;
        Type = type;
        Data = data;
        Category = category;
        Level = level;
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("timestamp");
        writer.WriteValue(Timestamp.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ", DateTimeFormatInfo.InvariantInfo));

        writer.WritePropertyName("message");
        writer.WriteValue(Message);
        writer.WritePropertyName("type");
        writer.WriteValue(Type);
        writer.WritePropertyName("data");
        writer.WriteStartObject();
        if (Data != null)
        {
            foreach (var kvp in Data)
            {
                writer.WritePropertyName(kvp.Key);
                writer.WriteValue(kvp.Value);
            }
        }
        writer.WriteEndObject();
        writer.WritePropertyName("category");
        writer.WriteValue(Category);
        writer.WritePropertyName("level");
        writer.WriteValue(Level.NullIfDefault()?.ToString().ToLowerInvariant());

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Breadcrumb FromJson(JToken json)
    {
        var timestamp = json["timestamp"]?.ToObject<DateTimeOffset>();
        var message = json["message"]?.ToString();
        var type = json["type"]?.ToString();
        var dataToken = json["data"];
        var data = dataToken != null ? dataToken.ToObject<Dictionary<string, string>>() : null;
        var category = json["category"]?.ToString();

        var levelString = json["level"]?.ToString();
        var level = levelString?.ToUpper() switch
        {
            "DEBUG" => BreadcrumbLevel.Debug,
            "INFO" => BreadcrumbLevel.Info,
            "WARNING" => BreadcrumbLevel.Warning,
            "ERROR" => BreadcrumbLevel.Error,
            "CRITICAL" => BreadcrumbLevel.Critical,
            "FATAL" => BreadcrumbLevel.Critical,
            _ => default
        };
        return new Breadcrumb(timestamp, message, type, data, category, level);
    }
}
