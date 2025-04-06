using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Describes the device that caused the event. This is most appropriate for mobile applications.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
public sealed class Device : ISentryJsonSerializable, ICloneable<Device>, IUpdatable<Device>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "device";

    /// <summary>
    /// The timezone of the device.
    /// </summary>
    /// <example>
    /// Europe/Vienna
    /// </example>
    public TimeZoneInfo? Timezone { get; set; }

    /// <summary>
    /// The name of the device. This is typically a hostname.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The manufacturer of the device.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// The brand of the device.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// The family of the device.
    /// </summary>
    /// <remarks>
    /// This is normally the common part of model names across generations.
    /// </remarks>
    /// <example>
    /// iPhone, Samsung Galaxy
    /// </example>
    public string? Family { get; set; }

    /// <summary>
    /// The model name.
    /// </summary>
    /// <example>
    /// Samsung Galaxy S3
    /// </example>
    public string? Model { get; set; }

    /// <summary>
    /// An internal hardware revision to identify the device exactly.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// The CPU architecture.
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// If the device has a battery a number defining the battery level (in the range 0-100).
    /// </summary>
    public float? BatteryLevel { get; set; }

    /// <summary>
    /// True if the device is charging.
    /// </summary>
    public bool? IsCharging { get; set; }

    /// <summary>
    /// True if the device has a internet connection.
    /// </summary>
    public bool? IsOnline { get; set; }

    /// <summary>
    /// This can be a string portrait or landscape to define the orientation of a device.
    /// </summary>
    public DeviceOrientation? Orientation { get; set; }

    /// <summary>
    /// A boolean defining whether this device is a simulator or an actual device.
    /// </summary>
    public bool? Simulator { get; set; }

    /// <summary>
    /// Total system memory available in bytes.
    /// </summary>
    public long? MemorySize { get; set; }

    /// <summary>
    /// Free system memory in bytes.
    /// </summary>
    public long? FreeMemory { get; set; }

    /// <summary>
    /// Memory usable for the app in bytes.
    /// </summary>
    public long? UsableMemory { get; set; }

    /// <summary>
    /// True, if the device memory is low.
    /// </summary>
    public bool? LowMemory { get; set; }

    /// <summary>
    /// Total device storage in bytes.
    /// </summary>
    public long? StorageSize { get; set; }

    /// <summary>
    /// Free device storage in bytes.
    /// </summary>
    public long? FreeStorage { get; set; }

    /// <summary>
    /// Total size of an attached external storage in bytes (e.g.: android SDK card).
    /// </summary>
    public long? ExternalStorageSize { get; set; }

    /// <summary>
    /// Free size of an attached external storage in bytes (e.g.: android SDK card).
    /// </summary>
    public long? ExternalFreeStorage { get; set; }

    /// <summary>
    /// The resolution of the screen.
    /// </summary>
    /// <example>
    /// 800x600
    /// </example>
    public string? ScreenResolution { get; set; }

    /// <summary>
    /// The logical density of the display.
    /// </summary>
    public float? ScreenDensity { get; set; }

    /// <summary>
    /// The screen density as dots-per-inch.
    /// </summary>
    public int? ScreenDpi { get; set; }

    /// <summary>
    /// A formatted UTC timestamp when the system was booted.
    /// </summary>
    /// <example>
    /// 2018-02-08T12:52:12Z
    /// </example>
    public DateTimeOffset? BootTime { get; set; }

    /// <summary>
    /// Number of "logical processors".
    /// </summary>
    /// <example>
    /// 8
    /// </example>
    public int? ProcessorCount { get; set; }

    /// <summary>
    /// CPU description.
    /// </summary>
    /// <example>
    /// Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz
    /// </example>
    public string? CpuDescription { get; set; }

    /// <summary>
    /// Processor frequency in MHz. Note that the actual CPU frequency might vary depending on current load and power
    /// conditions, especially on low-powered devices like phones and laptops. On some platforms it's not possible
    /// to query the CPU frequency. Currently such platforms are iOS and WebGL.
    /// </summary>
    /// <example>
    /// 2500
    /// </example>
    public float? ProcessorFrequency { get; set; }

    /// <summary>
    /// Kind of device the application is running on.
    /// </summary>
    /// <example>
    /// Unknown, Handheld, Console, Desktop
    /// </example>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Status of the device's battery.
    /// </summary>
    /// <example>
    /// Unknown, Charging, Discharging, NotCharging, Full
    /// </example>
    public string? BatteryStatus { get; set; }

    /// <summary>
    /// Unique device identifier. Depends on the running platform.
    /// </summary>
    /// <example>
    /// iOS: UIDevice.identifierForVendor (UUID)
    /// Android: The generated Installation ID
    /// Windows Store Apps: AdvertisingManager::AdvertisingId (possible fallback to HardwareIdentification::GetPackageSpecificToken().Id)
    /// Windows Standalone: hash from the concatenation of strings taken from Computer System Hardware Classes
    /// </example>
    /// TODO: Investigate - Do ALL platforms now return a generated installation ID?
    ///       See https://github.com/getsentry/sentry-java/pull/1455
    public string? DeviceUniqueIdentifier { get; set; }

    /// <summary>
    /// Is vibration available on the device?
    /// </summary>
    public bool? SupportsVibration { get; set; }

    /// <summary>
    /// Is accelerometer available on the device?
    /// </summary>
    public bool? SupportsAccelerometer { get; set; }

    /// <summary>
    /// Is gyroscope available on the device?
    /// </summary>
    public bool? SupportsGyroscope { get; set; }

    /// <summary>
    /// Is audio available on the device?
    /// </summary>
    public bool? SupportsAudio { get; set; }

    /// <summary>
    /// Is the device capable of reporting its location?
    /// </summary>
    public bool? SupportsLocationService { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal Device Clone() => ((ICloneable<Device>)this).Clone();

    Device ICloneable<Device>.Clone()
        => new()
        {
            Name = Name,
            Manufacturer = Manufacturer,
            Brand = Brand,
            Architecture = Architecture,
            BatteryLevel = BatteryLevel,
            IsCharging = IsCharging,
            IsOnline = IsOnline,
            BootTime = BootTime,
            ExternalFreeStorage = ExternalFreeStorage,
            ExternalStorageSize = ExternalStorageSize,
            ScreenResolution = ScreenResolution,
            ScreenDensity = ScreenDensity,
            ScreenDpi = ScreenDpi,
            Family = Family,
            FreeMemory = FreeMemory,
            FreeStorage = FreeStorage,
            MemorySize = MemorySize,
            Model = Model,
            ModelId = ModelId,
            Orientation = Orientation,
            Simulator = Simulator,
            StorageSize = StorageSize,
            Timezone = Timezone,
            UsableMemory = UsableMemory,
            LowMemory = LowMemory,
            ProcessorCount = ProcessorCount,
            CpuDescription = CpuDescription,
            ProcessorFrequency = ProcessorFrequency,
            SupportsVibration = SupportsVibration,
            DeviceType = DeviceType,
            BatteryStatus = BatteryStatus,
            DeviceUniqueIdentifier = DeviceUniqueIdentifier,
            SupportsAccelerometer = SupportsAccelerometer,
            SupportsGyroscope = SupportsGyroscope,
            SupportsAudio = SupportsAudio,
            SupportsLocationService = SupportsLocationService
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Device source) => ((IUpdatable<Device>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Device device)
        {
            ((IUpdatable<Device>)this).UpdateFrom(device);
        }
    }

    void IUpdatable<Device>.UpdateFrom(Device source)
    {
        Name ??= source.Name;
        Manufacturer ??= source.Manufacturer;
        Brand ??= source.Brand;
        Architecture ??= source.Architecture;
        BatteryLevel ??= source.BatteryLevel;
        IsCharging ??= source.IsCharging;
        IsOnline ??= source.IsOnline;
        BootTime ??= source.BootTime;
        ExternalFreeStorage ??= source.ExternalFreeStorage;
        ExternalStorageSize ??= source.ExternalStorageSize;
        ScreenResolution ??= source.ScreenResolution;
        ScreenDensity ??= source.ScreenDensity;
        ScreenDpi ??= source.ScreenDpi;
        Family ??= source.Family;
        FreeMemory ??= source.FreeMemory;
        FreeStorage ??= source.FreeStorage;
        MemorySize ??= source.MemorySize;
        Model ??= source.Model;
        ModelId ??= source.ModelId;
        Orientation ??= source.Orientation;
        Simulator ??= source.Simulator;
        StorageSize ??= source.StorageSize;
        Timezone ??= source.Timezone;
        UsableMemory ??= source.UsableMemory;
        LowMemory ??= source.LowMemory;
        ProcessorCount ??= source.ProcessorCount;
        CpuDescription ??= source.CpuDescription;
        ProcessorFrequency ??= source.ProcessorFrequency;
        SupportsVibration ??= source.SupportsVibration;
        DeviceType ??= source.DeviceType;
        BatteryStatus ??= source.BatteryStatus;
        DeviceUniqueIdentifier ??= source.DeviceUniqueIdentifier;
        SupportsAccelerometer ??= source.SupportsAccelerometer;
        SupportsGyroscope ??= source.SupportsGyroscope;
        SupportsAudio ??= source.SupportsAudio;
        SupportsLocationService ??= source.SupportsLocationService;
    }

    /// <inheritdoc />
    public void WriteTo(JsonTextWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("timezone", Timezone?.Id);

        // Write display name, but only if it's different from the ID
        if (!string.Equals(Timezone?.Id, Timezone?.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteStringIfNotWhiteSpace("timezone_display_name", Timezone?.DisplayName);
        }

        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("manufacturer", Manufacturer);
        writer.WriteStringIfNotWhiteSpace("brand", Brand);
        writer.WriteStringIfNotWhiteSpace("family", Family);
        writer.WriteStringIfNotWhiteSpace("model", Model);
        writer.WriteStringIfNotWhiteSpace("model_id", ModelId);
        writer.WriteStringIfNotWhiteSpace("arch", Architecture);

        if (BatteryLevel.HasValue)
        {
            writer.WriteNumber("battery_level", BatteryLevel.Value);
        }

        if (IsCharging.HasValue)
        {
            writer.WritePropertyName("charging");
            writer.WriteValue(IsCharging.Value);
        }

        if (IsOnline.HasValue)
        {
            writer.WritePropertyName("online");
            writer.WriteValue(IsOnline.Value);
        }

        if (Orientation.HasValue)
        {
            writer.WriteStringIfNotWhiteSpace("orientation", Orientation.Value.ToString().ToLowerInvariant());
        }

        if (Simulator.HasValue)
        {
            writer.WritePropertyName("simulator");
            writer.WriteValue(Simulator.Value);
        }

        if (MemorySize.HasValue)
        {
            writer.WriteNumber("memory_size", MemorySize.Value);
        }

        if (FreeMemory.HasValue)
        {
            writer.WriteNumber("free_memory", FreeMemory.Value);
        }

        if (UsableMemory.HasValue)
        {
            writer.WriteNumber("usable_memory", UsableMemory.Value);
        }

        if (LowMemory.HasValue)
        {
            writer.WritePropertyName("low_memory");
            writer.WriteValue(LowMemory.Value);
        }

        if (StorageSize.HasValue)
        {
            writer.WriteNumber("storage_size", StorageSize.Value);
        }

        if (FreeStorage.HasValue)
        {
            writer.WriteNumber("free_storage", FreeStorage.Value);
        }

        if (ExternalStorageSize.HasValue)
        {
            writer.WriteNumber("external_storage_size", ExternalStorageSize.Value);
        }

        if (ExternalFreeStorage.HasValue)
        {
            writer.WriteNumber("external_free_storage", ExternalFreeStorage.Value);
        }

        writer.WriteStringIfNotWhiteSpace("screen_resolution", ScreenResolution);

        if (ScreenDensity.HasValue)
        {
            writer.WriteNumber("screen_density", ScreenDensity.Value);
        }

        if (ScreenDpi.HasValue)
        {
            writer.WriteNumber("screen_dpi", ScreenDpi.Value);
        }

        writer.WriteStringIfNotNull("boot_time", BootTime);

        if (ProcessorCount.HasValue)
        {
            writer.WriteNumber("processor_count", ProcessorCount.Value);
        }

        writer.WriteStringIfNotWhiteSpace("cpu_description", CpuDescription);

        if (ProcessorFrequency.HasValue)
        {
            writer.WriteNumber("processor_frequency", ProcessorFrequency.Value);
        }

        writer.WriteStringIfNotWhiteSpace("device_type", DeviceType);
        writer.WriteStringIfNotWhiteSpace("battery_status", BatteryStatus);
        writer.WriteStringIfNotWhiteSpace("device_unique_identifier", DeviceUniqueIdentifier);

        if (SupportsVibration.HasValue)
        {
            writer.WritePropertyName("supports_vibration");
            writer.WriteValue(SupportsVibration.Value);
        }

        if (SupportsAccelerometer.HasValue)
        {
            writer.WritePropertyName("supports_accelerometer");
            writer.WriteValue(SupportsAccelerometer.Value);
        }

        if (SupportsGyroscope.HasValue)
        {
            writer.WritePropertyName("supports_gyroscope");
            writer.WriteValue(SupportsGyroscope.Value);
        }

        if (SupportsAudio.HasValue)
        {
            writer.WritePropertyName("supports_audio");
            writer.WriteValue(SupportsAudio.Value);
        }

        if (SupportsLocationService.HasValue)
        {
            writer.WritePropertyName("supports_location_service");
            writer.WriteValue(SupportsLocationService.Value);
        }

        writer.WriteEndObject();
    }

    private static TimeZoneInfo? TryParseTimezone(JObject json)
    {
        var timezoneId = json["timezone"]?.Value<string>();
        var timezoneName = json["timezone_display_name"]?.Value<string>() ?? timezoneId;

        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(timezoneId, TimeSpan.Zero, timezoneName, timezoneName);
        }
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Device FromJson(JObject json)
    {
        var timezone = TryParseTimezone(json);
        var name = json["name"]?.Value<string>();
        var manufacturer = json["manufacturer"]?.Value<string>();
        var brand = json["brand"]?.Value<string>();
        var family = json["family"]?.Value<string>();
        var model = json["model"]?.Value<string>();
        var modelId = json["model_id"]?.Value<string>();
        var architecture = json["arch"]?.Value<string>();
        var batteryLevel = json["battery_level"]?.Value<float?>();
        var isCharging = json["charging"]?.Value<bool?>();
        var isOnline = json["online"]?.Value<bool?>();
        var orientation = json["orientation"]?.Value<string>()?.ParseEnum<DeviceOrientation>();
        var simulator = json["simulator"]?.Value<bool?>();
        var memorySize = json["memory_size"]?.Value<long?>();
        var freeMemory = json["free_memory"]?.Value<long?>();
        var usableMemory = json["usable_memory"]?.Value<long?>();
        var lowMemory = json["low_memory"]?.Value<bool?>();
        var storageSize = json["storage_size"]?.Value<long?>();
        var freeStorage = json["free_storage"]?.Value<long?>();
        var externalStorageSize = json["external_storage_size"]?.Value<long?>();
        var externalFreeStorage = json["external_free_storage"]?.Value<long?>();
        var screenResolution = json["screen_resolution"]?.Value<string>();
        var screenDensity = json["screen_density"]?.Value<float?>();
        var screenDpi = json["screen_dpi"]?.Value<int?>();
        var bootTime = json["boot_time"]?.Value<DateTimeOffset?>();
        var processorCount = json["processor_count"]?.Value<int?>();
        var cpuDescription = json["cpu_description"]?.Value<string>();
        var processorFrequency = json["processor_frequency"]?.Value<float?>();
        var deviceType = json["device_type"]?.Value<string>();
        var batteryStatus = json["battery_status"]?.Value<string>();
        var deviceUniqueIdentifier = json["device_unique_identifier"]?.Value<string>();
        var supportsVibration = json["supports_vibration"]?.Value<bool?>();
        var supportsAccelerometer = json["supports_accelerometer"]?.Value<bool?>();
        var supportsGyroscope = json["supports_gyroscope"]?.Value<bool?>();
        var supportsAudio = json["supports_audio"]?.Value<bool?>();
        var supportsLocationService = json["supports_location_service"]?.Value<bool?>();

        return new Device
        {
            Timezone = timezone,
            Name = name,
            Manufacturer = manufacturer,
            Brand = brand,
            Family = family,
            Model = model,
            ModelId = modelId,
            Architecture = architecture,
            BatteryLevel = batteryLevel,
            IsCharging = isCharging,
            IsOnline = isOnline,
            Orientation = orientation,
            Simulator = simulator,
            MemorySize = memorySize,
            FreeMemory = freeMemory,
            UsableMemory = usableMemory,
            LowMemory = lowMemory,
            StorageSize = storageSize,
            FreeStorage = freeStorage,
            ExternalStorageSize = externalStorageSize,
            ExternalFreeStorage = externalFreeStorage,
            ScreenResolution = screenResolution,
            ScreenDensity = screenDensity,
            ScreenDpi = screenDpi,
            BootTime = bootTime,
            ProcessorCount = processorCount,
            CpuDescription = cpuDescription,
            ProcessorFrequency = processorFrequency,
            DeviceType = deviceType,
            BatteryStatus = batteryStatus,
            DeviceUniqueIdentifier = deviceUniqueIdentifier,
            SupportsVibration = supportsVibration,
            SupportsAccelerometer = supportsAccelerometer,
            SupportsGyroscope = supportsGyroscope,
            SupportsAudio = supportsAudio,
            SupportsLocationService = supportsLocationService
        };
    }
}
