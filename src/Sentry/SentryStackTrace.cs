using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Instruction Address Adjustments
/// </summary>
public enum InstructionAddressAdjustment
{
    /// <summary>
    /// Symbolicator will use the `"all_but_first"` strategy **unless** the event has a crashing `signal`
    /// attribute and the Stack Trace has a `registers` map, and the instruction pointer register (`rip` / `pc`)
    /// does not match the first frame. In that case, `"all"` frames will be adjusted.
    /// </summary>
    Auto,

    /// <summary>
    /// All frames of the stack trace will be adjusted, subtracting one instruction with (or `1`) from the
    /// incoming `instruction_addr` before symbolication.
    /// </summary>
    All,

    /// <summary>
    /// All frames but the first (in callee to caller / child to parent direction) should be adjusted.
    /// </summary>
    AllButFirst,

    /// <summary>
    /// No adjustment will be applied whatsoever.
    /// </summary>
    None
}

/// <summary>
/// Sentry Stacktrace interface.
/// </summary>
/// <remarks>
/// A stacktrace contains a list of frames, each with various bits (most optional) describing the context of that frame.
/// Frames should be sorted from oldest to newest.
/// </remarks>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
public class SentryStackTrace : ISentryJsonSerializable
{
    internal IList<SentryStackFrame>? InternalFrames { get; private set; }

    /// <summary>
    /// The list of frames in the stack.
    /// </summary>
    /// <remarks>
    /// The list of frames should be ordered by the oldest call first.
    /// </remarks>
    public IList<SentryStackFrame> Frames
    {
        get => InternalFrames ??= new List<SentryStackFrame>();
        set => InternalFrames = value;
    }

    /// <summary>
    /// The optional instruction address adjustment.
    /// </summary>
    /// <remarks>
    /// Tells the symbolicator if and what adjustment for is needed.
    /// </remarks>
    public InstructionAddressAdjustment? AddressAdjustment { get; set; }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (InternalFrames != null && InternalFrames.Count > 0)
        {
            writer.WritePropertyName("frames");
            writer.WriteStartArray();
            foreach (var frame in InternalFrames)
            {
                frame.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
        }

        if (AddressAdjustment is { } instructionAddressAdjustment)
        {
            var adjustmentType = instructionAddressAdjustment switch
            {
                InstructionAddressAdjustment.Auto => "auto",
                InstructionAddressAdjustment.All => "all",
                InstructionAddressAdjustment.AllButFirst => "all_but_first",
                InstructionAddressAdjustment.None => "none",
                _ => "auto"
            };

            writer.WritePropertyName("instruction_addr_adjustment");
            writer.WriteValue(adjustmentType);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryStackTrace FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var frames = json["frames"]?
            .Select(token => SentryStackFrame.FromJson(token))
            .ToArray();

        var instructionAddressAdjustmentStr = json["instruction_addr_adjustment"]?.Value<string>();
        InstructionAddressAdjustment? instructionAddressAdjustment = null;

        if (instructionAddressAdjustmentStr != null &&
            Enum.TryParse<InstructionAddressAdjustment>(instructionAddressAdjustmentStr, true, out var adjustment))
        {
            instructionAddressAdjustment = adjustment;
        }

        return new SentryStackTrace
        {
            InternalFrames = frames,
            AddressAdjustment = instructionAddressAdjustment
        };
    }
}
