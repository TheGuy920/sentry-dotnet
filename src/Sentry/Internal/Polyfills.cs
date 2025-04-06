// Polyfills to bridge the missing APIs in older targets.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace System.Net.Http
{
    internal abstract class SerializableHttpContent : HttpContent
    {
        protected virtual void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
        }

        internal Stream ReadAsStream(CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            SerializeToStream(stream, null, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}

internal static partial class PolyfillExtensions
{
    public static Stream ReadAsStream(this HttpContent content, CancellationToken cancellationToken = default) =>
        content is SerializableHttpContent serializableContent
            ? serializableContent.ReadAsStream(cancellationToken)
            : content.ReadAsStreamAsync(cancellationToken).Result;
}

internal static partial class PolyfillExtensions
{
    public static void WriteRawValue(this JsonTextWriter writer, byte[] utf8Json)
    {
        using var stringReader = new StringReader(Encoding.UTF8.GetString(utf8Json));
        using var jsonReader = new JsonTextReader(stringReader);
        var token = JToken.ReadFrom(jsonReader);
        token.WriteTo(writer);
    }
}
