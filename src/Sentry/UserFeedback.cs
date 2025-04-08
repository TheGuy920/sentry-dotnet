using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry User Feedback.
/// </summary>
[Obsolete("Use SentryFeedback instead.")]
public sealed class UserFeedback : ISentryJsonSerializable
{
    /// <summary>
    /// The eventId of the event to which the user feedback is associated.
    /// </summary>
    public SentryId EventId { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Comments of the user about what happened.
    /// </summary>
    public string? Comments { get; }

    /// <summary>
    /// Initializes an instance of <see cref="UserFeedback"/>.
    /// </summary>
    public UserFeedback(SentryId eventId, string? name, string? email, string? comments)
    {
        EventId = eventId;
        Name = name;
        Email = email;
        Comments = comments;
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("event_id");
        writer.WriteValue(EventId.ToString());

        if (!string.IsNullOrWhiteSpace(Name))
        {
            writer.WritePropertyName("name");
            writer.WriteValue(Name);
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            writer.WritePropertyName("email");
            writer.WriteValue(Email);
        }

        if (!string.IsNullOrWhiteSpace(Comments))
        {
            writer.WritePropertyName("comments");
            writer.WriteValue(Comments);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static UserFeedback FromJson(JToken json)
    {
        var eventId = json["event_id"] != null ? SentryId.FromJson(json["event_id"]!) : SentryId.Empty;
        var name = json["name"]?.Value<string>();
        var email = json["email"]?.Value<string>();
        var comments = json["comments"]?.Value<string>();

        return new UserFeedback(eventId, name, email, comments);
    }
}
