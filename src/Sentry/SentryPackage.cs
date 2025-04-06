using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Represents a package used to compose the SDK.
/// </summary>
public sealed class SentryPackage : ISentryJsonSerializable
{
    /// <summary>
    /// The name of the package.
    /// </summary>
    /// <example>
    /// nuget:Sentry
    /// nuget:Sentry.AspNetCore
    /// </example>
    public string Name { get; }

    /// <summary>
    /// The version of the package.
    /// </summary>
    /// <example>
    /// 1.0.0-rc1
    /// </example>
    public string Version { get; }

    /// <summary>
    /// Creates a new instance of a <see cref="SentryPackage"/>.
    /// </summary>
    /// <param name="name">The package name.</param>
    /// <param name="version">The package version.</param>
    public SentryPackage(string name, string version)
    {
        Name = name;
        Version = version;
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("name");
        writer.WriteValue(Name);
        writer.WritePropertyName("version");
        writer.WriteValue(Version);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryPackage FromJson(JToken json)
    {
        var name = json["name"]?.ToString() ?? throw new JsonException("name is required");
        var version = json["version"]?.ToString() ?? throw new JsonException("version is required");

        return new SentryPackage(name, version);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ Version.GetHashCode();
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is SentryPackage package)
        {
            return Name == package.Name && Version == package.Version;
        }

        return false;
    }
}
