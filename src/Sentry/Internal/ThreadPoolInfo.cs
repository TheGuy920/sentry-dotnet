using Newtonsoft.Json;
using Sentry.Extensibility;

namespace Sentry.Internal;

internal sealed class ThreadPoolInfo : ISentryJsonSerializable
{
    public ThreadPoolInfo(
        int minWorkerThreads,
        int minCompletionPortThreads,
        int maxWorkerThreads,
        int maxCompletionPortThreads,
        int availableWorkerThreads,
        int availableCompletionPortThreads)
    {
        MinWorkerThreads = minWorkerThreads;
        MinCompletionPortThreads = minCompletionPortThreads;
        MaxWorkerThreads = maxWorkerThreads;
        MaxCompletionPortThreads = maxCompletionPortThreads;
        AvailableWorkerThreads = availableWorkerThreads;
        AvailableCompletionPortThreads = availableCompletionPortThreads;
    }

    public int MinWorkerThreads { get; }
    public int MinCompletionPortThreads { get; }
    public int MaxWorkerThreads { get; }
    public int MaxCompletionPortThreads { get; }
    public int AvailableWorkerThreads { get; }
    public int AvailableCompletionPortThreads { get; }

    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("min_worker_threads");
        writer.WriteValue(MinWorkerThreads);
        writer.WritePropertyName("min_completion_port_threads");
        writer.WriteValue(MinCompletionPortThreads);
        writer.WritePropertyName("max_worker_threads");
        writer.WriteValue(MaxWorkerThreads);
        writer.WritePropertyName("max_completion_port_threads");
        writer.WriteValue(MaxCompletionPortThreads);
        writer.WritePropertyName("available_worker_threads");
        writer.WriteValue(AvailableWorkerThreads);
        writer.WritePropertyName("available_completion_port_threads");
        writer.WriteValue(AvailableCompletionPortThreads);

        writer.WriteEndObject();
    }
}
