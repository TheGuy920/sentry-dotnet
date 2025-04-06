using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry HTTP interface.
/// </summary>
/// <example>
/// "request": {
///     "url": "http://absolute.uri/foo",
///     "method": "POST",
///     "api_target": "apiType",
///     "data": {
///         "foo": "bar"
///     },
///     "query_string": "hello=world",
///     "cookies": "foo=bar",
///     "headers": {
///         "Content-Type": "text/html"
///     },
///     "env": {
///         "REMOTE_ADDR": "192.168.0.1"
///     }
/// }
/// </example>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/request/"/>
public sealed class SentryRequest : ISentryJsonSerializable
{
    internal Dictionary<string, string>? InternalEnv { get; private set; }

    internal Dictionary<string, string>? InternalOther { get; private set; }

    internal Dictionary<string, string>? InternalHeaders { get; private set; }

    /// <summary>
    /// Gets or sets the full request URL, if available.
    /// </summary>
    /// <value>The request URL.</value>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the method of the request.
    /// </summary>
    /// <value>The HTTP method.</value>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the API target for the request (e.g. "graphql")
    /// </summary>
    /// <value>The API Target.</value>
    public string? ApiTarget { get; set; }

    // byte[] or Memory<T>?
    // TODO: serializable object or string?
    /// <summary>
    /// Submitted data in whatever format makes most sense.
    /// </summary>
    /// <remarks>
    /// This data should not be provided by default as it can get quite large.
    /// </remarks>
    /// <value>The request payload.</value>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the unparsed query string.
    /// </summary>
    /// <value>The query string.</value>
    public string? QueryString { get; set; }

    /// <summary>
    /// Gets or sets the cookies.
    /// </summary>
    /// <value>The cookies.</value>
    public string? Cookies { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    /// <remarks>
    /// If a header appears multiple times it needs to be merged according to the HTTP standard for header merging.
    /// </remarks>
    /// <value>The headers.</value>
    public IDictionary<string, string> Headers => InternalHeaders ??= new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the optional environment data.
    /// </summary>
    /// <remarks>
    /// This is where information such as IIS/CGI keys go that are not HTTP headers.
    /// </remarks>
    /// <value>The env.</value>
    public IDictionary<string, string> Env => InternalEnv ??= new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets some optional other data.
    /// </summary>
    /// <value>The other.</value>
    public IDictionary<string, string> Other => InternalOther ??= new Dictionary<string, string>();

    internal void AddHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        foreach (var header in headers)
        {
            Headers.Add(header.Key, string.Join("; ", header.Value));
        }
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <remarks>
    /// This is a shallow copy.
    /// References like <see cref="Data"/> could hold a mutable, non-thread-safe object.
    /// </remarks>
    public SentryRequest Clone()
    {
        var request = new SentryRequest();

        CopyTo(request);

        return request;
    }

    internal void CopyTo(SentryRequest? request)
    {
        if (request == null)
        {
            return;
        }

        request.ApiTarget ??= ApiTarget;
        request.Url ??= Url;
        request.Method ??= Method;
        request.Data ??= Data;
        request.QueryString ??= QueryString;
        request.Cookies ??= Cookies;

        InternalEnv?.TryCopyTo(request.Env);
        InternalOther?.TryCopyTo(request.Other);
        InternalHeaders?.TryCopyTo(request.Headers);
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (InternalEnv != null && InternalEnv.Count > 0)
        {
            writer.WritePropertyName("env");
            JsonSerializer.Create().Serialize(writer, InternalEnv);
        }

        if (InternalOther != null && InternalOther.Count > 0)
        {
            writer.WritePropertyName("other");
            JsonSerializer.Create().Serialize(writer, InternalOther);
        }

        if (InternalHeaders != null && InternalHeaders.Count > 0)
        {
            writer.WritePropertyName("headers");
            JsonSerializer.Create().Serialize(writer, InternalHeaders);
        }

        if (!string.IsNullOrWhiteSpace(Url))
        {
            writer.WritePropertyName("url");
            writer.WriteValue(Url);
        }

        if (!string.IsNullOrWhiteSpace(Method))
        {
            writer.WritePropertyName("method");
            writer.WriteValue(Method);
        }

        if (Data != null)
        {
            writer.WritePropertyName("data");
            JsonSerializer.Create().Serialize(writer, Data);
        }

        if (!string.IsNullOrWhiteSpace(QueryString))
        {
            writer.WritePropertyName("query_string");
            writer.WriteValue(QueryString);
        }

        if (!string.IsNullOrWhiteSpace(Cookies))
        {
            writer.WritePropertyName("cookies");
            writer.WriteValue(Cookies);
        }

        if (!string.IsNullOrWhiteSpace(ApiTarget))
        {
            writer.WritePropertyName("api_target");
            writer.WriteValue(ApiTarget);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryRequest FromJson(JToken json)
    {
        var env = json["env"]?.ToObject<Dictionary<string, string?>>();
        var other = json["other"]?.ToObject<Dictionary<string, string?>>();
        var headers = json["headers"]?.ToObject<Dictionary<string, string?>>();
        var url = json["url"]?.Value<string>();
        var method = json["method"]?.Value<string>();
        var apiTarget = json["api_target"]?.Value<string>();
        var data = json["data"]?.ToObject<object>();
        var query = json["query_string"]?.Value<string>();
        var cookies = json["cookies"]?.Value<string>();

        return new SentryRequest
        {
            InternalEnv = env?.WhereNotNullValue().ToDict(),
            InternalOther = other?.WhereNotNullValue().ToDict(),
            InternalHeaders = headers?.WhereNotNullValue().ToDict(),
            Url = url,
            Method = method,
            ApiTarget = apiTarget,
            Data = data,
            QueryString = query,
            Cookies = cookies
        };
    }
}
