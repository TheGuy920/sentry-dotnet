using Newtonsoft.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif

namespace Sentry.Protocol;

/// <summary>
/// Sentry sampling profiler output profile
/// </summary>
internal sealed class SampleProfile : ISentryJsonSerializable
{
    // Note: changing these to properties would break because GrowableArray is a struct.
    internal Internal.GrowableArray<Sample> Samples = new(10000);
    internal Internal.GrowableArray<SentryStackFrame> Frames = new(100);
    internal Internal.GrowableArray<Internal.GrowableArray<int>> Stacks = new(100);
    internal List<SentryThread> Threads = new(10);

    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("thread_metadata");
        writer.WriteStartObject();
        for (var i = 0; i < Threads.Count; i++)
        {
            writer.WritePropertyName(i.ToString());
            Threads[i].WriteTo(writer, logger);
        }
        writer.WriteEndObject();

        writer.WritePropertyName("stacks");
        JsonSerializer.CreateDefault().Serialize(writer, Stacks);

        writer.WritePropertyName("frames");
        JsonSerializer.CreateDefault().Serialize(writer, Frames);

        writer.WritePropertyName("samples");
        JsonSerializer.CreateDefault().Serialize(writer, Samples);

        writer.WriteEndObject();
    }

    public class Sample : ISentryJsonSerializable
    {
        /// <summary>
        /// Timestamp in nanoseconds relative to the profile start.
        /// </summary>
        public ulong Timestamp = 0;

        public int ThreadId = 0;
        public int StackId = 0;

        public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("elapsed_since_start_ns");
            writer.WriteValue(Timestamp);
            writer.WritePropertyName("thread_id");
            writer.WriteValue(ThreadId);
            writer.WritePropertyName("stack_id");
            writer.WriteValue(StackId);
            writer.WriteEndObject();
        }
    }
}
