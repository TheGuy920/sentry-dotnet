using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry User Feedback.
/// </summary>
public sealed class SentryFeedback : ISentryJsonSerializable, ICloneable<SentryFeedback>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    internal const string Type = "feedback";

    /// <summary>
    /// Message containing the user's feedback.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The user's contact email address.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional ID of the Replay session associated with the feedback.
    /// </summary>
    public string? ReplayId { get; set; }

    /// <summary>
    /// Url that the feedback relates to
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional ID of the event that the user feedback is associated with.
    /// </summary>
    public SentryId? AssociatedEventId { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="SentryFeedback"/>.
    /// </summary>
    public SentryFeedback(string message, string? contactEmail = null, string? name = null, string? replayId = null, string? url = null, SentryId? associatedEventId = null)
    {
        Message = message;
        ContactEmail = contactEmail;
        Name = name;
        ReplayId = replayId;
        Url = url;
        AssociatedEventId = associatedEventId;
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        if (string.IsNullOrEmpty(Message))
        {
            logger?.LogWarning("Feedback message is empty - serializing as null");
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName("message");
        writer.WriteValue(Message);

        if (!string.IsNullOrWhiteSpace(ContactEmail))
        {
            writer.WritePropertyName("contact_email");
            writer.WriteValue(ContactEmail);
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            writer.WritePropertyName("name");
            writer.WriteValue(Name);
        }

        if (!string.IsNullOrWhiteSpace(ReplayId))
        {
            writer.WritePropertyName("replay_id");
            writer.WriteValue(ReplayId);
        }

        if (!string.IsNullOrWhiteSpace(Url))
        {
            writer.WritePropertyName("url");
            writer.WriteValue(Url);
        }

        if (AssociatedEventId != null)
        {
            writer.WritePropertyName("associated_event_id");
            // AssociatedEventId.WriteTo(writer, logger);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryFeedback FromJson(JObject json)
    {
        var message = json["message"]?.Value<string>() ?? "<empty>";
        var contactEmail = json["contact_email"]?.Value<string>();
        var name = json["name"]?.Value<string>();
        var replayId = json["replay_id"]?.Value<string>();
        var url = json["url"]?.Value<string>();
        var eventId = json["associated_event_id"] != null ? SentryId.FromJson(json["associated_event_id"]!) : default;

        return new SentryFeedback(message, contactEmail, name, replayId, url, eventId);
    }

    internal SentryFeedback Clone() => ((ICloneable<SentryFeedback>)this).Clone();

    SentryFeedback ICloneable<SentryFeedback>.Clone()
        => new(Message, ContactEmail, Name, ReplayId, Url, AssociatedEventId);
}
