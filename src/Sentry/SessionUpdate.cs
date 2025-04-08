using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Session update.
/// </summary>
// https://develop.sentry.dev/sdk/sessions/#session-update-payload
public class SessionUpdate : ISentrySession, ISentryJsonSerializable
{
    /// <inheritdoc />
    public SentryId Id { get; }

    /// <inheritdoc />
    public string? DistinctId { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; }

    /// <inheritdoc />
    public string Release { get; }

    /// <inheritdoc />
    public string? Environment { get; }

    /// <inheritdoc />
    public string? IpAddress { get; }

    /// <inheritdoc />
    public string? UserAgent { get; }

    /// <inheritdoc />
    public int ErrorCount { get; }

    /// <summary>
    /// Whether this is the initial update.
    /// </summary>
    public bool IsInitial { get; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Sequence number.
    /// </summary>
    public int SequenceNumber { get; }

    /// <summary>
    /// Duration of time since the start of the session.
    /// </summary>
    public TimeSpan Duration => Timestamp - StartTimestamp;

    /// <summary>
    /// Status with which the session was ended.
    /// </summary>
    public SessionEndStatus? EndStatus { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(
        SentryId id,
        string? distinctId,
        DateTimeOffset startTimestamp,
        string release,
        string? environment,
        string? ipAddress,
        string? userAgent,
        int errorCount,
        bool isInitial,
        DateTimeOffset timestamp,
        int sequenceNumber,
        SessionEndStatus? endStatus)
    {
        Id = id;
        DistinctId = distinctId;
        StartTimestamp = startTimestamp;
        Release = release;
        Environment = environment;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ErrorCount = errorCount;
        IsInitial = isInitial;
        Timestamp = timestamp;
        SequenceNumber = sequenceNumber;
        EndStatus = endStatus;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(
        ISentrySession session,
        bool isInitial,
        DateTimeOffset timestamp,
        int sequenceNumber,
        SessionEndStatus? endStatus)
        : this(
            session.Id,
            session.DistinctId,
            session.StartTimestamp,
            session.Release,
            session.Environment,
            session.IpAddress,
            session.UserAgent,
            session.ErrorCount,
            isInitial,
            timestamp,
            sequenceNumber,
            endStatus)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(SessionUpdate sessionUpdate, bool isInitial, SessionEndStatus? endStatus)
        : this(
            sessionUpdate,
            isInitial,
            sessionUpdate.Timestamp,
            sessionUpdate.SequenceNumber,
            endStatus)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(SessionUpdate sessionUpdate, bool isInitial)
        : this(sessionUpdate, isInitial, sessionUpdate.EndStatus)
    {
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("sid");
        writer.WriteValue(Id.ToString());

        if (!string.IsNullOrWhiteSpace(DistinctId))
        {
            writer.WritePropertyName("did");
            writer.WriteValue(DistinctId);
        }

        writer.WritePropertyName("init");
        writer.WriteValue(IsInitial);

        writer.WritePropertyName("started");
        writer.WriteValue(StartTimestamp);

        writer.WritePropertyName("timestamp");
        writer.WriteValue(Timestamp);

        writer.WritePropertyName("seq");
        writer.WriteValue(SequenceNumber);

        writer.WritePropertyName("duration");
        writer.WriteValue((int)Duration.TotalSeconds);

        writer.WritePropertyName("errors");
        writer.WriteValue(ErrorCount);

        if (EndStatus.HasValue)
        {
            writer.WritePropertyName("status");
            writer.WriteValue(EndStatus.Value.ToString().ToSnakeCase());
        }

        writer.WritePropertyName("attrs");
        writer.WriteStartObject();

        writer.WritePropertyName("release");
        writer.WriteValue(Release);

        if (!string.IsNullOrWhiteSpace(Environment))
        {
            writer.WritePropertyName("environment");
            writer.WriteValue(Environment);
        }

        if (!string.IsNullOrWhiteSpace(IpAddress))
        {
            writer.WritePropertyName("ip_address");
            writer.WriteValue(IpAddress);
        }

        if (!string.IsNullOrWhiteSpace(UserAgent))
        {
            writer.WritePropertyName("user_agent");
            writer.WriteValue(UserAgent);
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses <see cref="SessionUpdate"/> from JSON.
    /// </summary>
    public static SessionUpdate FromJson(JToken json)
    {
        var id = SentryId.Parse(json["sid"]!.Value<string>()!);
        var distinctId = json["did"]?.Value<string>();
        var startTimestamp = json["started"]!.Value<DateTimeOffset>();
        var release = json["attrs"]!["release"]!.Value<string>()!;
        var environment = json["attrs"]!["environment"]?.Value<string>();
        var ipAddress = json["attrs"]!["ip_address"]?.Value<string>();
        var userAgent = json["attrs"]!["user_agent"]?.Value<string>();
        var errorCount = json["errors"]?.Value<int>() ?? 0;
        var isInitial = json["init"]?.Value<bool>() ?? false;
        var timestamp = json["timestamp"]!.Value<DateTimeOffset>();
        var sequenceNumber = json["seq"]!.Value<int>();
        var endStatus = json["status"]?.Value<string>()?.ParseEnum<SessionEndStatus>();

        return new SessionUpdate(
            id,
            distinctId,
            startTimestamp,
            release,
            environment,
            ipAddress,
            userAgent,
            errorCount,
            isInitial,
            timestamp,
            sequenceNumber,
            endStatus);
    }
}
