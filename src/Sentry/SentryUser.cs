using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// An interface which describes the authenticated User for a request.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/user/"/>
public sealed class SentryUser : ISentryJsonSerializable
{
    internal Action<SentryUser>? PropertyChanged { get; set; }

    private string? _id;
    private string? _username;
    private string? _email;
    private string? _ipAddress;
    private IDictionary<string, string>? _other;

    /// <summary>
    /// The unique ID of the user.
    /// </summary>
    public string? Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The username of the user.
    /// </summary>
    public string? Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string? Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The IP address of the user.
    /// </summary>
    public string? IpAddress
    {
        get => _ipAddress;
        set
        {
            if (_ipAddress != value)
            {
                _ipAddress = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Additional information about the user.
    /// </summary>
    public IDictionary<string, string> Other
    {
        get => _other ??= new Dictionary<string, string>();
        set
        {
            if (_other != value)
            {
                _other = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Clones the current <see cref="SentryUser"/> instance.
    /// </summary>
    /// <returns>The cloned user.</returns>
    public SentryUser Clone()
    {
        var user = new SentryUser();
        CopyTo(user);
        return user;
    }

    internal void CopyTo(SentryUser? user)
    {
        if (user == null)
        {
            return;
        }

        user.Id ??= Id;
        user.Username ??= Username;
        user.Email ??= Email;
        user.IpAddress ??= IpAddress;

        user._other ??= _other?.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);
    }

    internal bool HasAnyData() =>
        Id is not null ||
        Username is not null ||
        Email is not null ||
        IpAddress is not null ||
        _other?.Count > 0;

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(Id))
    {
            writer.WritePropertyName("id");
            writer.WriteValue(Id);
        }

        if (!string.IsNullOrWhiteSpace(Username))
        {
            writer.WritePropertyName("username");
            writer.WriteValue(Username);
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            writer.WritePropertyName("email");
            writer.WriteValue(Email);
        }

        if (!string.IsNullOrWhiteSpace(IpAddress))
        {
            writer.WritePropertyName("ip_address");
            writer.WriteValue(IpAddress);
        }

        if (_other != null && _other.Count > 0)
        {
            writer.WritePropertyName("other");
        writer.WriteStartObject();
            foreach (var item in _other)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteValue(item.Value);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryUser FromJson(JToken json)
    {
        var id = json["id"]?.Value<string>();
        var username = json["username"]?.Value<string>();
        var email = json["email"]?.Value<string>();
        var ip = json["ip_address"]?.Value<string>();
        var segment = json["segment"]?.Value<string>();
        var other = json["other"]?.ToObject<Dictionary<string, string?>>();

        return new SentryUser
        {
            Id = id,
            Username = username,
            Email = email,
            IpAddress = ip,
            _other = other?.WhereNotNullValue().ToDict()
        };
    }
}
