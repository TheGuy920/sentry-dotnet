using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

internal class LazyHttpTransport : ITransport
{
    private readonly Lazy<HttpTransport> _httpTransport;

    public LazyHttpTransport(SentrySdk sdk, SentryOptions options)
    {
        _httpTransport = new Lazy<HttpTransport>(() => new HttpTransport(sdk, options, options.GetHttpClient()));
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        return _httpTransport.Value.SendEnvelopeAsync(envelope, cancellationToken);
    }
}
