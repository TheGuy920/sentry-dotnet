using Newtonsoft.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// The Check-In Status
/// </summary>
public enum CheckInStatus
{
    /// <summary>
    /// The Checkin is in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// The Checkin is Ok
    /// </summary>
    Ok,

    /// <summary>
    /// The Checkin errored
    /// </summary>
    Error
}

/// <summary>
/// Sentry Check-In
/// </summary>
// https://develop.sentry.dev/sdk/check-ins/
public class SentryCheckIn : ISentryJsonSerializable
{
    /// <summary>
    /// Check-In ID
    /// </summary>
    public SentryId Id { get; }

    /// <summary>
    /// The distinct slug of the monitor.
    /// </summary>
    public string MonitorSlug { get; }


    /// <summary>
    /// The status of the Check-In
    /// </summary>
    public CheckInStatus Status { get; }

    /// <summary>
    /// The duration of the Check-In in seconds. Will only take effect if the status is ok or error.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// The release.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// The environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// The trace ID
    /// </summary>
    internal SentryId? TraceId { get; set; }

    /// <summary>
    /// The Monitor Config
    /// </summary>
    internal SentryMonitorOptions? MonitorOptions { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SentryCheckIn"/>.
    /// </summary>
    /// <param name="monitorSlug"></param>
    /// <param name="status"></param>
    /// <param name="sentryId"></param>
    public SentryCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null)
    {
        MonitorSlug = monitorSlug;
        Status = status;
        Id = sentryId ?? SentryId.Create();
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("check_in_id");
        writer.WriteValue(Id);
        writer.WritePropertyName("monitor_slug");
        writer.WriteValue(MonitorSlug);
        writer.WritePropertyName("status");
        writer.WriteValue(ToSnakeCase(Status));

        if (Duration.HasValue)
        {
            writer.WritePropertyName("duration");
            writer.WriteValue(Duration.Value.TotalSeconds);
        }

        if (!string.IsNullOrWhiteSpace(Release))
        {
            writer.WritePropertyName("release");
            writer.WriteValue(Release);
        }

        if (!string.IsNullOrWhiteSpace(Environment))
        {
            writer.WritePropertyName("environment");
            writer.WriteValue(Environment);
        }

        if (TraceId is not null)
        {
            writer.WritePropertyName("contexts");
            writer.WriteStartObject();
            writer.WritePropertyName("trace");
            writer.WriteStartObject();

            writer.WritePropertyName("trace_id");
            writer.WriteValue(TraceId.ToString());

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        MonitorOptions?.WriteTo(writer, logger);

        writer.WriteEndObject();
    }

    private static string ToSnakeCase(CheckInStatus status)
    {
        return status switch
        {
            CheckInStatus.InProgress => "in_progress",
            CheckInStatus.Ok => "ok",
            CheckInStatus.Error => "error",
            _ => throw new ArgumentException($"Unsupported CheckInStatus: '{status}'.")
        };
    }
}
