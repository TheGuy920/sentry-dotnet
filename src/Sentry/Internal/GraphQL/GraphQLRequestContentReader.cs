using Newtonsoft.Json;

namespace Sentry.Internal.GraphQL;

/// <summary>
/// Adapted from https://github.com/graphql-dotnet/graphql-dotnet/blob/42a299e77748ec588bf34c33334e985098563298/src/GraphQL.SystemTextJson/GraphQLRequestJsonConverter.cs#L64
/// </summary>
internal static class GraphQLRequestContentReader
{
    /// <summary>
    /// Name for the operation name parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string OperationNameKey = "operationName";

    /// <summary>
    /// Name for the query parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string QueryKey = "query";


    public static IReadOnlyDictionary<string, object> Read(string requestContent)
    {
        using var stringReader = new StringReader(requestContent);
        using var jsonReader = new JsonTextReader(stringReader);

        if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        var request = new Dictionary<string, object>();

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonToken.EndObject)
            {
                return request;
            }

            if (jsonReader.TokenType != JsonToken.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            var key = (string)jsonReader.Value!;

            if (!jsonReader.Read())
            {
                throw new JsonException("unexpected end of data");
            }

            switch (key)
            {
                case QueryKey:
                case OperationNameKey:
                    request[key] = (string)jsonReader.Value!;
                    break;
                default:
                    jsonReader.Skip();
                    break;
            }
        }

        throw new JsonException("unexpected end of data");
    }
}
