using Newtonsoft.Json.Linq;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

internal class SentryGraphQLHttpFailedRequestHandler : SentryFailedRequestHandler
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    internal const string MechanismType = "GraphqlInstrumentation";
    private readonly SentryHttpFailedRequestHandler _httpFailedRequestHandler;

    internal SentryGraphQLHttpFailedRequestHandler(IHub hub, SentryOptions options)
        : base(hub, options)
    {
        _hub = hub;
        _options = options;
        _httpFailedRequestHandler = new SentryHttpFailedRequestHandler(hub, options);
    }

    protected internal override void DoEnsureSuccessfulResponse([NotNull] HttpRequestMessage request, [NotNull] HttpResponseMessage response)
    {
        JToken? json = null;
        try
        {
            json = GraphQLContentExtractor.ExtractResponseContentAsync(response, _options).Result;
            if (json != null)
            {
                if (json["errors"] != null)
                {
                    // We just show the first error... maybe there's a better way to do this when multiple errors exist.
                    // We should check what the Java code is doing.
                    var errorMessage = json["errors"]![0]!["message"]?.Value<string>() ?? "GraphQL Error";
                    throw new GraphQLHttpRequestException(errorMessage);
                }
            }
            // No GraphQL errors, but we still might have an HTTP error status
            _httpFailedRequestHandler.DoEnsureSuccessfulResponse(request, response);
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType, "GraphQL Failed Request Handler", false);

            var @event = new SentryEvent(exception);
            var hint = new SentryHint(_hub.Sdk ?? null!, HintTypes.HttpResponseMessage, response);

            var sentryRequest = new SentryRequest
            {
                QueryString = request.RequestUri?.Query,
                Method = request.Method.Method.ToUpperInvariant(),
                ApiTarget = "graphql"
            };

            var responseContext = new Response
            {
                StatusCode = (short)response.StatusCode,
#if NET5_0_OR_GREATER
                // Starting with .NET 5, the content and headers are guaranteed to not be null.
                BodySize = response.Content?.Headers.ContentLength,
#else
                BodySize = response.Content?.Headers?.ContentLength,
#endif
            };

            var requestContent = request.GetFused<GraphQLRequestContent>();
            if (!_options.SendDefaultPii)
            {
                sentryRequest.Url = request.RequestUri?.HttpRequestUrl();
            }
            else
            {
                sentryRequest.Cookies = request.Headers.GetCookies();
                sentryRequest.Data = requestContent?.RequestContent;
                sentryRequest.Url = request.RequestUri?.AbsoluteUri;
                sentryRequest.AddHeaders(request.Headers);
                responseContext.Cookies = response.Headers.GetCookies();
                responseContext.Data = json;
                responseContext.AddHeaders(response.Headers);
            }

            @event.Request = sentryRequest;
            @event.Contexts[Response.Type] = responseContext;
            if (requestContent is not null)
            {
                @event.Fingerprint = new[]
                {
                    requestContent.OperationNameOrFallback(),
                    requestContent.OperationTypeOrFallback(),
                    ((int)response.StatusCode).ToString()
                };
            }
            _hub.CaptureEvent(@event, hint: hint);
        }
    }
}
