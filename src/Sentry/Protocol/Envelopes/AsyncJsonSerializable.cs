using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents a task producing an object serializable to JSON format.
/// </summary>
internal sealed class AsyncJsonSerializable : ISerializable
{
    /// <summary>
    /// Source object.
    /// </summary>
    public Task<ISentryJsonSerializable> Source { get; }

    /// <summary>
    /// Initializes an instance of <see cref="AsyncJsonSerializable"/>.
    /// </summary>
    public static AsyncJsonSerializable CreateFrom<T>(Task<T> source)
        where T : ISentryJsonSerializable
    {
        var task = source.ContinueWith(t => t.Result as ISentryJsonSerializable);
        return new AsyncJsonSerializable(task);
    }

    private AsyncJsonSerializable(Task<ISentryJsonSerializable> source) => Source = source;

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        var source = await Source.ConfigureAwait(false);
        using var jsonWriter = new SentryJsonWriter(stream);
        source.WriteTo(jsonWriter, logger);
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        using var jsonWriter = new SentryJsonWriter(stream);
        Source.Result.WriteTo(jsonWriter, logger);
        jsonWriter.Flush();
    }
}
