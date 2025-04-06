using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sentry.Internal.Extensions;

internal static class HttpClientExtensions
{
    public static async Task<JToken> ReadAsJsonAsync(
        this HttpContent content,
        CancellationToken cancellationToken = default)
    {
        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = JsonSerializer.CreateDefault();
            return await JToken.LoadAsync(jsonReader, cancellationToken).ConfigureAwait(false);
        }
    }

    public static JToken ReadAsJson(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        var serializer = JsonSerializer.CreateDefault();
        return JToken.Load(jsonReader);
    }

    public static string ReadAsString(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
