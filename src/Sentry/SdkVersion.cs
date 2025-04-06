using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Reflection;

namespace Sentry;

/// <summary>
/// Information about the SDK to be sent with the SentryEvent.
/// </summary>
/// <remarks>Requires Sentry version 8.4 or higher.</remarks>
public sealed class SdkVersion : ISentryJsonSerializable
{
    private static readonly Lazy<SdkVersion> InstanceLazy = new(
        () => new SdkVersion
        {
            Name = "sentry.dotnet",
            Version = typeof(ISentryClient).Assembly.GetVersion()
        });

    internal static SdkVersion Instance => InstanceLazy.Value;

    internal ConcurrentBag<SentryPackage> InternalPackages { get; set; } = new();
    internal ConcurrentBag<string> Integrations { get; set; } = new();

    /// <summary>
    /// SDK packages.
    /// </summary>
    /// <remarks>This property is not required.</remarks>
    public IEnumerable<SentryPackage> Packages => InternalPackages;

    /// <summary>
    /// SDK name.
    /// </summary>
    public string? Name
    {
        get;
        // For integrations to set their name
        [EditorBrowsable(EditorBrowsableState.Never)]
        set;
    }

    /// <summary>
    /// SDK Version.
    /// </summary>
    public string? Version
    {
        get;
        // For integrations to set their version
        [EditorBrowsable(EditorBrowsableState.Never)]
        set;
    }

    /// <summary>
    /// Add a package used to compose the SDK.
    /// </summary>
    /// <param name="name">The package name.</param>
    /// <param name="version">The package version.</param>
    public void AddPackage(string name, string version)
        => AddPackage(new SentryPackage(name, version));

    internal void AddPackage(SentryPackage package)
        => InternalPackages.Add(package);

    /// <summary>
    /// Add an integration used in the SDK.
    /// </summary>
    /// <param name="integration">The integrations name.</param>
    public void AddIntegration(string integration)
        => Integrations.Add(integration);

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        if (InternalPackages.Any())
        {
            writer.WritePropertyName("packages");
            writer.WriteStartArray();
            foreach (var package in InternalPackages.Distinct())
            {
                writer.WriteValue(package);
            }
            writer.WriteEndArray();
        }

        if (Integrations.Any())
        {
            writer.WritePropertyName("integrations");
            writer.WriteStartArray();
            foreach (var integration in Integrations.Distinct())
            {
                writer.WriteValue(integration);
            }
            writer.WriteEndArray();
        }

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

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SdkVersion FromJson(JToken json)
    {
        // Packages
        var packages =
            json["packages"]?.ToObject<JArray>()?.Select(SentryPackage.FromJson).ToArray()
            ?? Array.Empty<SentryPackage>();

        // Integrations
        var integrations =
            json["integrations"]?.ToObject<JArray>()?.Select(element => element.ToString() ?? "").ToArray()
            ?? Array.Empty<string>();

        // Name
        var name = (json["name"]?.ToString()) ?? "dotnet.unknown";

        // Version
        var version = (json["version"]?.ToString()) ?? "0.0.0";

        return new SdkVersion
        {
            InternalPackages = new ConcurrentBag<SentryPackage>(packages),
            Integrations = new ConcurrentBag<string>(integrations),
            Name = name,
            Version = version
        };
    }
}
