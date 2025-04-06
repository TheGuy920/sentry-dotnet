using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Graphics device unit.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/#gpu-context"/>
public sealed class Gpu : ISentryJsonSerializable, ICloneable<Gpu>, IUpdatable<Gpu>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "gpu";

    /// <summary>
    /// The name of the graphics device.
    /// </summary>
    /// <example>
    /// iPod touch: Apple A8 GPU
    /// Samsung S7: Mali-T880
    /// </example>
    public string? Name { get; set; }

    /// <summary>
    /// The PCI Id of the graphics device.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="VendorId"/> uniquely identifies the GPU.
    /// </remarks>
    public int? Id { get; set; }

    /// <summary>
    /// The PCI vendor Id of the graphics device.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="Id"/> uniquely identifies the GPU.
    /// </remarks>
    /// <seealso href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/identifiers-for-pci-devices"/>
    /// <seealso href="http://pci-ids.ucw.cz/read/PC/"/>
    public string? VendorId { get; set; }

    /// <summary>
    /// The vendor name reported by the graphic device.
    /// </summary>
    /// <example>
    /// Apple, ARM, WebKit
    /// </example>
    public string? VendorName { get; set; }

    /// <summary>
    /// Total GPU memory available in mega-bytes.
    /// </summary>
    public int? MemorySize { get; set; }

    /// <summary>
    /// Device type.
    /// </summary>
    /// <remarks>The low level API used.</remarks>
    /// <example>Metal, Direct3D11, OpenGLES3, PlayStation4, XboxOne</example>
    public string? ApiType { get; set; }

    /// <summary>
    /// Whether the GPU is multi-threaded rendering or not.
    /// </summary>
    public bool? MultiThreadedRendering { get; set; }

    /// <summary>
    /// The Version of the API of the graphics device.
    /// </summary>
    /// <example>
    /// iPod touch: Metal
    /// Android: OpenGL ES 3.2 v1.r22p0-01rel0.f294e54ceb2cb2d81039204fa4b0402e
    /// WebGL Windows: OpenGL ES 3.0 (WebGL 2.0 (OpenGL ES 3.0 Chromium))
    /// OpenGL 2.0, Direct3D 9.0c
    /// </example>
    public string? Version { get; set; }

    /// <summary>
    /// The Non-Power-Of-Two support level.
    /// </summary>
    /// <example>
    /// Full
    /// </example>
    public string? NpotSupport { get; set; }

    /// <summary>
    /// Largest size of a texture that is supported by the graphics hardware.
    /// </summary>
    public int? MaxTextureSize { get; set; }

    /// <summary>
    /// Approximate "shader capability" level of the graphics device.
    /// </summary>
    /// <example>
    /// Shader Model 2.0, OpenGL ES 3.0, Metal / OpenGL ES 3.1, 27 (unknown)
    /// </example>
    public string? GraphicsShaderLevel { get; set; }

    /// <summary>
    /// Is GPU draw call instancing supported?
    /// </summary>
    public bool? SupportsDrawCallInstancing { get; set; }

    /// <summary>
    /// Is ray tracing available on the device?
    /// </summary>
    public bool? SupportsRayTracing { get; set; }

    /// <summary>
    /// Are compute shaders available on the device?
    /// </summary>
    public bool? SupportsComputeShaders { get; set; }

    /// <summary>
    /// Are geometry shaders available on the device?
    /// </summary>
    public bool? SupportsGeometryShaders { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal Gpu Clone() => ((ICloneable<Gpu>)this).Clone();

    Gpu ICloneable<Gpu>.Clone()
        => new()
        {
            Name = Name,
            Id = Id,
            VendorId = VendorId,
            VendorName = VendorName,
            MemorySize = MemorySize,
            ApiType = ApiType,
            MultiThreadedRendering = MultiThreadedRendering,
            Version = Version,
            NpotSupport = NpotSupport,
            MaxTextureSize = MaxTextureSize,
            GraphicsShaderLevel = GraphicsShaderLevel,
            SupportsDrawCallInstancing = SupportsDrawCallInstancing,
            SupportsRayTracing = SupportsRayTracing,
            SupportsComputeShaders = SupportsComputeShaders,
            SupportsGeometryShaders = SupportsGeometryShaders
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Gpu source) => ((IUpdatable<Gpu>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Gpu gpu)
        {
            ((IUpdatable<Gpu>)this).UpdateFrom(gpu);
        }
    }

    void IUpdatable<Gpu>.UpdateFrom(Gpu source)
    {
        Name ??= source.Name;
        Id ??= source.Id;
        VendorId ??= source.VendorId;
        VendorName ??= source.VendorName;
        MemorySize ??= source.MemorySize;
        ApiType ??= source.ApiType;
        MultiThreadedRendering ??= source.MultiThreadedRendering;
        Version ??= source.Version;
        NpotSupport ??= source.NpotSupport;
        MaxTextureSize ??= source.MaxTextureSize;
        GraphicsShaderLevel ??= source.GraphicsShaderLevel;
        SupportsDrawCallInstancing ??= source.SupportsDrawCallInstancing;
        SupportsRayTracing ??= source.SupportsRayTracing;
        SupportsComputeShaders ??= source.SupportsComputeShaders;
        SupportsGeometryShaders ??= source.SupportsGeometryShaders;
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

        if (Id.HasValue)
        {
            writer.WritePropertyName("id");
            writer.WriteValue(Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(VendorId))
        {
            writer.WritePropertyName("vendor_id");
            writer.WriteValue(VendorId);
        }

        if (!string.IsNullOrWhiteSpace(VendorName))
        {
            writer.WritePropertyName("vendor_name");
            writer.WriteValue(VendorName);
        }

        if (MemorySize.HasValue)
        {
            writer.WritePropertyName("memory_size");
            writer.WriteValue(MemorySize.Value);
        }

        if (!string.IsNullOrWhiteSpace(ApiType))
        {
            writer.WritePropertyName("api_type");
            writer.WriteValue(ApiType);
        }

        if (MultiThreadedRendering.HasValue)
        {
            writer.WritePropertyName("multi_threaded_rendering");
            writer.WriteValue(MultiThreadedRendering.Value);
        }

        if (!string.IsNullOrWhiteSpace(Version))
        {
            writer.WritePropertyName("version");
            writer.WriteValue(Version);
        }

        if (!string.IsNullOrWhiteSpace(NpotSupport))
        {
            writer.WritePropertyName("npot_support");
            writer.WriteValue(NpotSupport);
        }

        if (MaxTextureSize.HasValue)
        {
            writer.WritePropertyName("max_texture_size");
            writer.WriteValue(MaxTextureSize.Value);
        }

        if (!string.IsNullOrWhiteSpace(GraphicsShaderLevel))
        {
            writer.WritePropertyName("graphics_shader_level");
            writer.WriteValue(GraphicsShaderLevel);
        }

        if (SupportsDrawCallInstancing.HasValue)
        {
            writer.WritePropertyName("supports_draw_call_instancing");
            writer.WriteValue(SupportsDrawCallInstancing.Value);
        }

        if (SupportsRayTracing.HasValue)
        {
            writer.WritePropertyName("supports_ray_tracing");
            writer.WriteValue(SupportsRayTracing.Value);
        }

        if (SupportsComputeShaders.HasValue)
        {
            writer.WritePropertyName("supports_compute_shaders");
            writer.WriteValue(SupportsComputeShaders.Value);
        }

        if (SupportsGeometryShaders.HasValue)
        {
            writer.WritePropertyName("supports_geometry_shaders");
            writer.WriteValue(SupportsGeometryShaders.Value);
        }

        writer.WriteEndObject();
    }
    /// <summary>
    /// Parses from JSON.
    /// </summary>
public static Gpu FromJson(Newtonsoft.Json.Linq.JToken json)
    {
    var name = json["name"]?.Value<string>();
    var id = json["id"]?.Value<int?>();
    var vendorId = json["vendor_id"]?.Value<string>();
    var vendorName = json["vendor_name"]?.Value<string>();
    var memorySize = json["memory_size"]?.Value<int?>();
    var apiType = json["api_type"]?.Value<string>();
    var multiThreadedRendering = json["multi_threaded_rendering"]?.Value<bool?>();
    var version = json["version"]?.Value<string>();
    var npotSupport = json["npot_support"]?.Value<string>();
    var maxTextureSize = json["max_texture_size"]?.Value<int?>();
    var graphicsShaderLevel = json["graphics_shader_level"]?.Value<string>();
    var supportsDrawCallInstancing = json["supports_draw_call_instancing"]?.Value<bool?>();
    var supportsRayTracing = json["supports_ray_tracing"]?.Value<bool?>();
    var supportsComputeShaders = json["supports_compute_shaders"]?.Value<bool?>();
    var supportsGeometryShaders = json["supports_geometry_shaders"]?.Value<bool?>();

        return new Gpu
        {
            Name = name,
            Id = id,
            VendorId = vendorId,
            VendorName = vendorName,
            MemorySize = memorySize,
            ApiType = apiType,
            MultiThreadedRendering = multiThreadedRendering,
            Version = version,
            NpotSupport = npotSupport,
            MaxTextureSize = maxTextureSize,
            GraphicsShaderLevel = graphicsShaderLevel,
            SupportsDrawCallInstancing = supportsDrawCallInstancing,
            SupportsRayTracing = supportsRayTracing,
            SupportsComputeShaders = supportsComputeShaders,
            SupportsGeometryShaders = supportsGeometryShaders
        };
    }
}
