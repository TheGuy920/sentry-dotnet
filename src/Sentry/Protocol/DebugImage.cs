using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// The Sentry Debug Meta Images interface.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta#debug-images"/>
public sealed class DebugImage : ISentryJsonSerializable
{
    /// <summary>
    /// Type of the debug image.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Memory address, at which the image is mounted in the virtual address space of the process.
    /// </summary>
    public long? ImageAddress { get; set; }

    /// <summary>
    /// The size of the image in virtual memory.
    /// If missing, Sentry will assume that the image spans up to the next image, which might lead to invalid stack traces.
    /// </summary>
    public long? ImageSize { get; set; }

    /// <summary>
    /// Unique debug identifier of the image.
    /// </summary>
    public string? DebugId { get; set; }

    /// <summary>
    /// Checksum of the companion debug file.
    /// </summary>
    public string? DebugChecksum { get; set; }

    /// <summary>
    /// Path and name of the debug companion file.
    /// </summary>
    public string? DebugFile { get; set; }

    /// <summary>
    /// Optional identifier of the code file.
    /// </summary>
    public string? CodeId { get; set; }

    /// <summary>
    /// The absolute path to the dynamic library or executable.
    /// This helps to locate the file if it is missing on Sentry.
    /// </summary>
    public string? CodeFile { get; set; }

    internal Guid? ModuleVersionId { get; set; }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(Type))
        {
            writer.WritePropertyName("type");
            writer.WriteValue(Type);
        }

        if (ImageAddress.HasValue)
        {
            writer.WritePropertyName("image_addr");
            writer.WriteValue(ImageAddress.Value.ToHexString());
        }

        if (ImageSize.HasValue)
        {
            writer.WritePropertyName("image_size");
            writer.WriteValue(ImageSize.Value);
        }

        if (!string.IsNullOrWhiteSpace(DebugId))
        {
            writer.WritePropertyName("debug_id");
            writer.WriteValue(DebugId);
        }

        if (!string.IsNullOrWhiteSpace(DebugChecksum))
        {
            writer.WritePropertyName("debug_checksum");
            writer.WriteValue(DebugChecksum);
        }

        if (!string.IsNullOrWhiteSpace(DebugFile))
        {
            writer.WritePropertyName("debug_file");
            writer.WriteValue(DebugFile);
        }

        if (!string.IsNullOrWhiteSpace(CodeId))
        {
            writer.WritePropertyName("code_id");
            writer.WriteValue(CodeId);
        }

        if (!string.IsNullOrWhiteSpace(CodeFile))
        {
            writer.WritePropertyName("code_file");
            writer.WriteValue(CodeFile);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static DebugImage FromJson(JObject json)
    {
        var type = json["type"]?.Value<string>();
        var imageAddress = json["image_addr"]?.Value<string>()?.GetHexAsLong();
        var imageSize = json["image_size"]?.Value<long>();
        var debugId = json["debug_id"]?.Value<string>();
        var debugChecksum = json["debug_checksum"]?.Value<string>();
        var debugFile = json["debug_file"]?.Value<string>();
        var codeId = json["code_id"]?.Value<string>();
        var codeFile = json["code_file"]?.Value<string>();

        return new DebugImage
        {
            Type = type,
            ImageAddress = imageAddress,
            ImageSize = imageSize,
            DebugId = debugId,
            DebugChecksum = debugChecksum,
            DebugFile = debugFile,
            CodeId = codeId,
            CodeFile = codeFile,
        };
    }
}
