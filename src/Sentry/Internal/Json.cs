using Newtonsoft.Json.Linq;

namespace Sentry.Internal;

internal static class Json
{
    public static T Parse<T>(byte[] json, Func<JToken, T> factory)
    {
        var token = JToken.Parse(Encoding.UTF8.GetString(json));
        return factory.Invoke(token);
    }

    public static T Parse<T>(string json, Func<JToken, T> factory)
    {
        var token = JToken.Parse(json);
        return factory.Invoke(token);
    }

    public static T Load<T>(IFileSystem fileSystem, string filePath, Func<JToken, T> factory)
    {
        using var file = fileSystem.OpenFileForReading(filePath);
        using var reader = new StreamReader(file);
        var token = JToken.Parse(reader.ReadToEnd());
        return factory.Invoke(token);
    }
}
