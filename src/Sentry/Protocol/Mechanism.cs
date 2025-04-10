using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Sentry Exception Mechanism.
/// </summary>
/// <remarks>
/// The exception mechanism is an optional field residing in the Exception Interface.
/// It carries additional information about the way the exception was created on the target system.
/// This includes general exception values obtained from operating system or runtime APIs, as well as mechanism-specific values.
/// </remarks>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/exception/#exception-mechanism"/>
public sealed class Mechanism : ISentryJsonSerializable
{
    /// <summary>
    /// Key found inside of <c>Exception.Data</c> to inform if the exception was handled.
    /// </summary>
    public static readonly string HandledKey = "Sentry:Handled";

    /// <summary>
    /// Key found inside of <c>Exception.Data</c> to inform which mechanism captured the exception.
    /// </summary>
    public static readonly string MechanismKey = "Sentry:Mechanism";

    /// <summary>
    /// Key found inside of <c>Exception.Data</c> to provide a description of the mechanism.
    /// </summary>
    public static readonly string DescriptionKey = "Sentry:Description";

    internal Dictionary<string, object>? InternalData { get; private set; }

    internal Dictionary<string, object>? InternalMeta { get; private set; }

    private const string DefaultType = "generic";
    private string _type = DefaultType;

    /// <summary>
    /// Required unique identifier of this mechanism determining rendering and processing of the mechanism data.
    /// Defaults to <c>"generic"</c>.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, empty, or whitespace are set, reverts to the default string <c>"generic"</c>.
    /// Nullability is for backwards compatibility, and may be removed in a future major version.
    /// </remarks>
    [AllowNull]
    public string Type
    {
        get => _type;
        set => _type = string.IsNullOrWhiteSpace(value) ? DefaultType : value;
    }

    /// <summary>
    /// Optional human readable description of the error mechanism and a possible hint on how to solve this error.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// An optional value to explain the source of the exception.
    /// </summary>
    /// <remarks>
    /// For chained exceptions, this should be the property name where the exception was retrieved from its parent
    /// exception.  In .NET, either &quot;<see cref="Exception.InnerException"/>&quot; or <c>&quot;InnerExceptions[i]&quot;</c>
    /// (where <c>i</c> is replaced with the numeric index within <see cref="AggregateException.InnerExceptions"/>).
    /// </remarks>
    public string? Source { get; set; }

    /// <summary>
    /// Optional fully qualified URL to an online help resource, possible interpolated with error parameters.
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Optional flag indicating whether the exception has been handled by the user (e.g. via try..catch).
    /// </summary>
    public bool? Handled { get; set; }

    /// <summary>
    /// Optional flag indicating whether the exception is synthetic.
    /// </summary>
    public bool Synthetic { get; set; }

    /// <summary>
    /// Whether the exception represents an exception group.
    /// In .NET, an <see cref="AggregateException"/>.
    /// </summary>
    public bool IsExceptionGroup { get; set; }

    /// <summary>
    /// A numeric identifier assigned to the exception by the SDK.
    /// </summary>
    /// <remarks>
    /// The SDK should assign a different ID to each exception in an event, starting with the root exception as 0,
    /// and incrementing thereafter. This ID can be used with <see cref="ParentId"/> to reconstruct the logical
    /// structure of an exception group.  When <c>null</c>, Sentry will assume that all exceptions in an event are
    /// in a single chain.
    /// </remarks>
    public int? ExceptionId { get; set; }

    /// <summary>
    /// The parent exception's identifier, or <c>null</c> for the root exception.
    /// </summary>
    /// <remarks>
    /// This ID can be used with <see cref="ExceptionId"/> to reconstruct the logical structure of an exception group.
    /// </remarks>
    public int? ParentId { get; set; }

    /// <summary>
    /// Optional information from the operating system or runtime on the exception mechanism.
    /// </summary>
    /// <remarks>
    /// The mechanism meta data usually carries error codes reported by the runtime or operating system,
    /// along with a platform dependent interpretation of these codes.
    /// SDKs can safely omit code names and descriptions for well known error codes, as it will be filled out by Sentry.
    /// For proprietary or vendor-specific error codes, adding these values will give additional information to the user.
    /// </remarks>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception/#meta-information"/>
    public IDictionary<string, object> Meta => InternalMeta ??= new Dictionary<string, object>();

    /// <summary>
    /// Arbitrary extra data that might help the user understand the error thrown by this mechanism.
    /// </summary>
    public IDictionary<string, object> Data => InternalData ??= new Dictionary<string, object>();

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteValue(Type);

        if (!string.IsNullOrWhiteSpace(Description))
        {
            writer.WritePropertyName("description");
            writer.WriteValue(Description);
        }

        if (!string.IsNullOrWhiteSpace(Source))
        {
            writer.WritePropertyName("source");
            writer.WriteValue(Source);
        }

        if (!string.IsNullOrWhiteSpace(HelpLink))
        {
            writer.WritePropertyName("help_link");
            writer.WriteValue(HelpLink);
        }

        if (Handled.HasValue)
        {
            writer.WritePropertyName("handled");
            writer.WriteValue(Handled.Value);
        }

        if (Synthetic)
        {
            writer.WritePropertyName("synthetic");
            writer.WriteValue(Synthetic);
        }

        if (IsExceptionGroup)
        {
            writer.WritePropertyName("is_exception_group");
            writer.WriteValue(IsExceptionGroup);
        }

        if (ExceptionId.HasValue)
        {
            writer.WritePropertyName("exception_id");
            writer.WriteValue(ExceptionId.Value);
        }

        if (ParentId.HasValue)
        {
            writer.WritePropertyName("parent_id");
            writer.WriteValue(ParentId.Value);
        }

        if (InternalData?.Count > 0)
        {
            writer.WritePropertyName("data");
            writer.WriteDictionaryValue(InternalData.Select(kvp => new KeyValuePair<string,object?>(kvp.Key, kvp.Value)), logger);
        }

        if (InternalMeta?.Count > 0)
        {
            writer.WritePropertyName("meta");
            writer.WriteDictionaryValue(InternalMeta.Select(kvp => new KeyValuePair<string,object?>(kvp.Key, kvp.Value)), logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Mechanism FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var type = json["type"]?.Value<string>();
        var description = json["description"]?.Value<string>();
        var source = json["source"]?.Value<string>();
        var helpLink = json["help_link"]?.Value<string>();
        var handled = json["handled"]?.Value<bool?>();
        var synthetic = json["synthetic"]?.Value<bool>() ?? false;
        var isExceptionGroup = json["is_exception_group"]?.Value<bool>() ?? false;
        var exceptionId = json["exception_id"]?.Value<int?>();
        var parentId = json["parent_id"]?.Value<int?>();
        var data = json["data"]?.ToObject<Dictionary<string, object?>>();
        var meta = json["meta"]?.ToObject<Dictionary<string, object?>>();

        return new Mechanism
        {
            Type = type,
            Description = description,
            Source = source,
            HelpLink = helpLink,
            Handled = handled,
            Synthetic = synthetic,
            IsExceptionGroup = isExceptionGroup,
            ExceptionId = exceptionId,
            ParentId = parentId,
            InternalData = data?.WhereNotNullValue().ToDict(),
            InternalMeta = meta?.WhereNotNullValue().ToDict()
        };
    }

    internal bool IsDefaultOrEmpty() =>
        Handled is null &&
        Synthetic == false &&
        IsExceptionGroup == false &&
        ExceptionId is null &&
        ParentId is null &&
        Type == DefaultType &&
        string.IsNullOrWhiteSpace(Description) &&
        string.IsNullOrWhiteSpace(HelpLink) &&
        string.IsNullOrWhiteSpace(Source) &&
        !(InternalData?.Count > 0) &&
        !(InternalMeta?.Count > 0);
}
