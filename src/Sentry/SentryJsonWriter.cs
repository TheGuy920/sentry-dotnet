using Newtonsoft.Json;
using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// JSON writer for Sentry.
/// </summary>
public class SentryJsonWriter : JsonTextWriter
{
    private readonly StreamWriter _textWriter;
    private readonly JsonTextWriter _writer;
    private readonly IDiagnosticLogger? _logger;

    /// <summary>
    /// Creates a new instance of <see cref="SentryJsonWriter"/>.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="logger"></param>
    public SentryJsonWriter(Stream stream, IDiagnosticLogger? logger = null)
        : base(COPY(new StreamWriter(stream), out var nWriter))
    {
        _logger = logger;
        _textWriter = nWriter;
        _writer = new JsonTextWriter(_textWriter);
    }

    /// <summary>
    /// Writes a value
    /// </summary>
    /// <param name="value"></param>
    public void WriteValue(ISentryJsonSerializable value)
    {
        value.WriteTo(this, _logger);
    }

    /// <summary>
    /// Copy
    /// </summary>
    /// <param name="inv"></param>
    /// <param name="outv"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T COPY<T>(T inv, out T outv)
    {
        outv = inv;
        return inv;
    }
}
