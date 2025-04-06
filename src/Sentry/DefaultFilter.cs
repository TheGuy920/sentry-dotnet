using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// Default filter.
/// </summary>
public class DefaultFilter(IEnumerable<string> namespaces) : ISentryEventProcessor
{
    /// <summary>
    /// Process
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public SentryEvent? Process(SentryEvent @event)
    {
        var hasCore = @event.SentryExceptions!
            .SelectMany(ex => ex.Stacktrace?.Frames ?? Enumerable.Empty<SentryStackFrame>())
            .Any(f => namespaces.Any(ns => f.Module?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) == true));

        return hasCore ? @event : null;
    }
}
