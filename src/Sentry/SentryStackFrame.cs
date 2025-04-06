using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// A frame of a stacktrace.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
[DebuggerDisplay("{Function}")]
public sealed class SentryStackFrame : ISentryJsonSerializable
{

    private static readonly Lazy<PrefixOrPatternMatcher> LazyModuleMatcher = new(() => new());
    private static readonly Lazy<DelimitedPrefixOrPatternMatcher> LazyFunctionMatcher = new(() => new());

    internal List<string>? InternalPreContext { get; private set; }

    internal List<string>? InternalPostContext { get; private set; }

    internal Dictionary<string, string>? InternalVars { get; private set; }

    internal List<int>? InternalFramesOmitted { get; private set; }

    /// <summary>
    /// When serializing a stack frame as part of the Code Location metadata for Metrics, we need to include an
    /// additional "type" property in the serialized payload. This flag indicates whether the stack frame is for
    /// a code location or not.
    /// </summary>
    internal bool IsCodeLocation { get; set; } = false;

    /// <summary>
    /// The relative file path to the call.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// The name of the function being called.
    /// </summary>
    public string? Function { get; set; }

    /// <summary>
    /// Platform-specific module path.
    /// </summary>
    public string? Module { get; set; }

    // Optional fields

    /// <summary>
    /// The line number of the call.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// The column number of the call.
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// The absolute path to filename.
    /// </summary>
    public string? AbsolutePath { get; set; }

    /// <summary>
    /// Source code in filename at line number.
    /// </summary>
    public string? ContextLine { get; set; }

    /// <summary>
    /// A list of source code lines before context_line (in order) – usually [lineno - 5:lineno].
    /// </summary>
    public IList<string> PreContext => InternalPreContext ??= new List<string>();

    /// <summary>
    /// A list of source code lines after context_line (in order) – usually [lineno + 1:lineno + 5].
    /// </summary>
    public IList<string> PostContext => InternalPostContext ??= new List<string>();

    /// <summary>
    /// Signifies whether this frame is related to the execution of the relevant code in this stacktrace.
    /// </summary>
    /// <example>
    /// For example, the frames that might power the framework’s web server of your app are probably not relevant,
    /// however calls to the framework’s library once you start handling code likely are.
    /// </example>
    public bool? InApp { get; set; }

    /// <summary>
    /// A mapping of variables which were available within this frame (usually context-locals).
    /// </summary>
    public IDictionary<string, string> Vars => InternalVars ??= new Dictionary<string, string>();

    /// <summary>
    /// Which frames were omitted, if any.
    /// </summary>
    /// <remarks>
    /// If the list of frames is large, you can explicitly tell the system that you’ve omitted a range of frames.
    /// The frames_omitted must be a single tuple two values: start and end.
    /// </remarks>
    /// <example>
    /// If you only removed the 8th frame, the value would be (8, 9), meaning it started at the 8th frame,
    /// and went until the 9th (the number of frames omitted is end-start).
    /// The values should be based on a one-index.
    /// </example>
    public IList<int> FramesOmitted => InternalFramesOmitted ??= new List<int>();

    /// <summary>
    /// The assembly where the code resides.
    /// </summary>
    public string? Package { get; set; }

    /// <summary>
    /// This can override the platform for a single frame. Otherwise the platform of the event is assumed.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// Optionally an address of the debug image to reference.
    /// If this is set and a known image is defined by debug_meta then symbolication can take place.
    /// </summary>
    public long? ImageAddress { get; set; }

    /// <summary>
    /// An optional address that points to a symbol.
    /// We actually use the instruction address for symbolication but this can be used to calculate an instruction offset automatically.
    /// </summary>
    public long? SymbolAddress { get; set; }

    /// <summary>
    /// An optional instruction address for symbolication.<br/>
    /// If this is set and a known image is defined in the <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta/">Debug Meta Interface</see>, then symbolication can take place.<br/>
    /// </summary>
    public long? InstructionAddress { get; set; }

    /// <summary>
    /// Optionally changes the addressing mode. The default value is the same as
    /// `"abs"` which means absolute referencing. This can also be set to
    /// `"rel:DEBUG_ID"` or `"rel:IMAGE_INDEX"` to make addresses relative to an
    /// object referenced by debug id or index.
    /// </summary>
    public string? AddressMode { get; set; }

    /// <summary>
    /// The optional Function Id.<br/>
    /// This is derived from the `MetadataToken`, and should be the record id of a `MethodDef`.
    /// </summary>
    public long? FunctionId { get; set; }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (IsCodeLocation)
        {
            // See https://develop.sentry.dev/sdk/metrics/#meta-data
            writer.WritePropertyName("type");
            writer.WriteValue("location");
        }

        if (InternalPreContext?.Count > 0)
        {
            writer.WritePropertyName("pre_context");
            writer.WriteStartArray();
            foreach (var item in InternalPreContext)
            {
                writer.WriteValue(item);
            }
            writer.WriteEndArray();
        }

        if (InternalPostContext?.Count > 0)
        {
            writer.WritePropertyName("post_context");
            writer.WriteStartArray();
            foreach (var item in InternalPostContext)
            {
                writer.WriteValue(item);
            }
            writer.WriteEndArray();
        }

        if (InternalVars?.Count > 0)
        {
            writer.WritePropertyName("vars");
            writer.WriteStartObject();
            foreach (var kv in InternalVars)
            {
                writer.WritePropertyName(kv.Key);
                writer.WriteValue(kv.Value);
            }
            writer.WriteEndObject();
        }

        if (InternalFramesOmitted?.Count > 0)
        {
            writer.WritePropertyName("frames_omitted");
            writer.WriteStartArray();
            foreach (var item in InternalFramesOmitted)
            {
                writer.WriteValue(item);
            }
            writer.WriteEndArray();
        }

        if (!string.IsNullOrWhiteSpace(FileName))
        {
            writer.WritePropertyName("filename");
            writer.WriteValue(FileName);
        }

        if (!string.IsNullOrWhiteSpace(Function))
        {
            writer.WritePropertyName("function");
            writer.WriteValue(Function);
        }

        if (!string.IsNullOrWhiteSpace(Module))
        {
            writer.WritePropertyName("module");
            writer.WriteValue(Module);
        }

        if (LineNumber.HasValue)
        {
            writer.WritePropertyName("lineno");
            writer.WriteValue(LineNumber.Value);
        }

        if (ColumnNumber.HasValue)
        {
            writer.WritePropertyName("colno");
            writer.WriteValue(ColumnNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(AbsolutePath))
        {
            writer.WritePropertyName("abs_path");
            writer.WriteValue(AbsolutePath);
        }

        if (!string.IsNullOrWhiteSpace(ContextLine))
        {
            writer.WritePropertyName("context_line");
            writer.WriteValue(ContextLine);
        }

        if (InApp.HasValue)
        {
            writer.WritePropertyName("in_app");
            writer.WriteValue(InApp.Value);
        }

        if (!string.IsNullOrWhiteSpace(Package))
        {
            writer.WritePropertyName("package");
            writer.WriteValue(Package);
        }

        if (!string.IsNullOrWhiteSpace(Platform))
        {
            writer.WritePropertyName("platform");
            writer.WriteValue(Platform);
        }

        var imageAddrStr = ImageAddress?.NullIfDefault()?.ToHexString();
        if (!string.IsNullOrWhiteSpace(imageAddrStr))
        {
            writer.WritePropertyName("image_addr");
            writer.WriteValue(imageAddrStr);
        }

        var symbolAddrStr = SymbolAddress?.NullIfDefault()?.ToHexString();
        if (!string.IsNullOrWhiteSpace(symbolAddrStr))
        {
            writer.WritePropertyName("symbol_addr");
            writer.WriteValue(symbolAddrStr);
        }

        var instructionAddrStr = InstructionAddress?.ToHexString();
        if (!string.IsNullOrWhiteSpace(instructionAddrStr))
        {
            writer.WritePropertyName("instruction_addr");
            writer.WriteValue(instructionAddrStr);
        }

        if (!string.IsNullOrWhiteSpace(AddressMode))
        {
            writer.WritePropertyName("addr_mode");
            writer.WriteValue(AddressMode);
        }

        var functionIdStr = FunctionId?.ToHexString();
        if (!string.IsNullOrWhiteSpace(functionIdStr))
        {
            writer.WritePropertyName("function_id");
            writer.WriteValue(functionIdStr);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Configures <see cref="InApp"/> based on the <see cref="SentryOptions.InAppInclude"/> and <see cref="SentryOptions.InAppExclude"/> or <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The Sentry options.</param>
    /// <remarks><see cref="InApp"/> will remain with the same value if previously set.</remarks>
    public void ConfigureAppFrame(SentryOptions options)
    {
        if (InApp != null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(Module))
        {
            ConfigureAppFrame(options, Module, LazyModuleMatcher.Value);
        }
        else if (!string.IsNullOrEmpty(Function))
        {
            ConfigureAppFrame(options, Function, LazyFunctionMatcher.Value);
        }
        else if (ImageAddress is null or 0 && InstructionAddress is null or 0) // Leave InApp=null on NativeAOT
        {
            InApp = true;
        }
    }

    private void ConfigureAppFrame(SentryOptions options, string parameter, IStringOrRegexMatcher matcher) =>
        InApp = parameter.MatchesAny(options.InAppInclude, matcher)
                || !parameter.MatchesAny(options.InAppExclude, matcher);

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryStackFrame FromJson(JToken json)
    {
        var preContext = json["pre_context"]?.ToObject<List<string>>();
        var postContext = json["post_context"]?.ToObject<List<string>>();
        var vars = json["vars"]?.ToObject<Dictionary<string, string>>();
        var framesOmitted = json["frames_omitted"]?.ToObject<List<int>>();
        var filename = json["filename"]?.Value<string>();
        var function = json["function"]?.Value<string>();
        var module = json["module"]?.Value<string>();
        var lineNumber = json["lineno"]?.Value<int>();
        var columnNumber = json["colno"]?.Value<int>();
        var absolutePath = json["abs_path"]?.Value<string>();
        var contextLine = json["context_line"]?.Value<string>();
        var inApp = json["in_app"]?.Value<bool>();
        var package = json["package"]?.Value<string>();
        var platform = json["platform"]?.Value<string>();
        var imageAddress = json["image_addr"]?.Value<string>()?.GetHexAsLong();
        var symbolAddress = json["symbol_addr"]?.Value<string>()?.GetHexAsLong();
        var instructionAddress = json["instruction_addr"]?.Value<string>()?.GetHexAsLong();
        var addressMode = json["addr_mode"]?.Value<string>();
        var functionId = json["function_id"]?.Value<string>()?.GetHexAsLong();

        return new SentryStackFrame
        {
            InternalPreContext = preContext,
            InternalPostContext = postContext,
            InternalVars = vars?.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value),
            InternalFramesOmitted = framesOmitted,
            FileName = filename,
            Function = function,
            Module = module,
            LineNumber = lineNumber,
            ColumnNumber = columnNumber,
            AbsolutePath = absolutePath,
            ContextLine = contextLine,
            InApp = inApp,
            Package = package,
            Platform = platform,
            ImageAddress = imageAddress,
            SymbolAddress = symbolAddress,
            InstructionAddress = instructionAddress,
            AddressMode = addressMode,
            FunctionId = functionId,
        };
    }
}
