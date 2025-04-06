using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Represents Sentry's context for OS.
/// </summary>
/// <remarks>
/// Defines the operating system that caused the event. In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/#os-context"/>
public sealed class OperatingSystem : ISentryJsonSerializable, ICloneable<OperatingSystem>, IUpdatable<OperatingSystem>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "os";

    /// <summary>
    /// The name of the operating system.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The version of the operating system.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// An optional raw description that Sentry can use in an attempt to normalize OS info.
    /// </summary>
    /// <remarks>
    /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
    /// this field can be used to provide a raw system info (e.g: uname)
    /// </remarks>
    public string? RawDescription { get; set; }

    /// <summary>
    /// The internal build revision of the operating system.
    /// </summary>
    public string? Build { get; set; }

    /// <summary>
    /// If known, this can be an independent kernel version string. Typically
    /// this is something like the entire output of the 'uname' tool.
    /// </summary>
    public string? KernelVersion { get; set; }

    /// <summary>
    /// An optional boolean that defines if the OS has been jailbroken or rooted.
    /// </summary>
    public bool? Rooted { get; set; }

    /// <summary>
    /// Clones this instance
    /// </summary>
    internal OperatingSystem Clone() => ((ICloneable<OperatingSystem>)this).Clone();

    OperatingSystem ICloneable<OperatingSystem>.Clone()
        => new()
        {
            Name = Name,
            Version = Version,
            RawDescription = RawDescription,
            Build = Build,
            KernelVersion = KernelVersion,
            Rooted = Rooted
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(OperatingSystem source) => ((IUpdatable<OperatingSystem>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is OperatingSystem os)
        {
            ((IUpdatable<OperatingSystem>)this).UpdateFrom(os);
        }
    }

    void IUpdatable<OperatingSystem>.UpdateFrom(OperatingSystem source)
    {
        Name ??= source.Name;
        Version ??= source.Version;
        RawDescription ??= source.RawDescription;
        Build ??= source.Build;
        KernelVersion ??= source.KernelVersion;
        Rooted ??= source.Rooted;
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteValue(Type);

        if (!string.IsNullOrWhiteSpace(Name))
        {
            writer.WritePropertyName("name");
            writer.WriteValue(Name);
        }

        if (!string.IsNullOrWhiteSpace(Version))
        {
            writer.WritePropertyName("version");
            writer.WriteValue(Version);
        }

        if (!string.IsNullOrWhiteSpace(RawDescription))
        {
            writer.WritePropertyName("raw_description");
            writer.WriteValue(RawDescription);
        }

        if (!string.IsNullOrWhiteSpace(Build))
        {
            writer.WritePropertyName("build");
            writer.WriteValue(Build);
        }

        if (!string.IsNullOrWhiteSpace(KernelVersion))
        {
            writer.WritePropertyName("kernel_version");
            writer.WriteValue(KernelVersion);
        }

        if (Rooted.HasValue)
        {
            writer.WritePropertyName("rooted");
            writer.WriteValue(Rooted.Value);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static OperatingSystem FromJson(Newtonsoft.Json.Linq.JToken json)
    {
        var name = json["name"]?.Value<string>();
        var version = json["version"]?.Value<string>();
        var rawDescription = json["raw_description"]?.Value<string>();
        var build = json["build"]?.Value<string>();
        var kernelVersion = json["kernel_version"]?.Value<string>();
        var rooted = json["rooted"]?.Value<bool?>();

        return new OperatingSystem
        {
            Name = name,
            Version = version,
            RawDescription = rawDescription,
            Build = build,
            KernelVersion = kernelVersion,
            Rooted = rooted
        };
    }
}
