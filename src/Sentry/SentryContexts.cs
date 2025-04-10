using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Trace = Sentry.Protocol.Trace;

namespace Sentry;

/// <summary>
/// Represents Sentry's structured Context.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/" />
public sealed class SentryContexts : IDictionary<string, object>, ISentryJsonSerializable
{
    private readonly ConcurrentDictionary<string, object> _innerDictionary = new(StringComparer.Ordinal);

    /// <summary>
    /// Describes the application.
    /// </summary>
    public App App => _innerDictionary.GetOrCreate<App>(App.Type);

    /// <summary>
    /// Describes the browser.
    /// </summary>
    public Browser Browser => _innerDictionary.GetOrCreate<Browser>(Browser.Type);

    /// <summary>
    /// Describes the device.
    /// </summary>
    public Device Device => _innerDictionary.GetOrCreate<Device>(Device.Type);

    /// <summary>
    /// Holds user feedback.
    /// </summary>
    public SentryFeedback? Feedback
    {
        get => _innerDictionary.TryGetValue<SentryFeedback>(SentryFeedback.Type);
        set
        {
            if (value is null)
            {
                _innerDictionary.TryRemove(SentryFeedback.Type, out _);
                return;
            }
            _innerDictionary[SentryFeedback.Type] = value;
        }
    }

    /// <summary>
    /// Defines the operating system.
    /// </summary>
    /// <remarks>
    /// In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
    /// </remarks>
    public OperatingSystem OperatingSystem => _innerDictionary.GetOrCreate<OperatingSystem>(OperatingSystem.Type);

    /// <summary>
    /// Response interface that contains information on any HTTP response related to the event.
    /// </summary>
    public Response Response => _innerDictionary.GetOrCreate<Response>(Response.Type);

    /// <summary>
    /// This describes a runtime in more detail.
    /// </summary>
    public Runtime Runtime => _innerDictionary.GetOrCreate<Runtime>(Runtime.Type);

    /// <summary>
    /// This describes a GPU of the device.
    /// </summary>
    public Gpu Gpu => _innerDictionary.GetOrCreate<Gpu>(Gpu.Type);

    /// <summary>
    /// This describes trace information.
    /// </summary>
    public Trace Trace => _innerDictionary.GetOrCreate<Trace>(Trace.Type);

    /// <summary>
    /// Initializes an instance of <see cref="SentryContexts"/>.
    /// </summary>
    public SentryContexts() { }

    /// <summary>
    /// Creates a deep clone of this context.
    /// </summary>
    internal SentryContexts Clone()
    {
        var context = new SentryContexts();

        CopyTo(context);

        return context;
    }

    /// <summary>
    /// Copies the items of the context while cloning the known types.
    /// </summary>
    internal void CopyTo(SentryContexts to)
    {
        foreach (var kv in this)
        {
            to._innerDictionary.AddOrUpdate(kv.Key,

                addValueFactory: _ =>
                    kv.Value is ICloneable<object> cloneable
                        ? cloneable.Clone()
                        : kv.Value,

                updateValueFactory: (_, existing) =>
                {
                    if (existing is IUpdatable updatable)
                    {
                        updatable.UpdateFrom(kv.Value);
                    }
                    else if (kv.Value is IDictionary<string, object?> source &&
                             existing is IDictionary<string, object?> target)
                    {
                        foreach (var item in source)
                        {
                            if (!target.TryGetValue(item.Key, out var value))
                            {
                                target.Add(item);
                            }
                            else if (value is null)
                            {
                                target[item.Key] = item.Value;
                            }
                        }
                    }

                    return existing;
                });
        }
    }

    /// <inheritdoc />
    public void WriteTo(SentryJsonWriter writer, IDiagnosticLogger? logger)
    {
        var contexts = this.OrderBy(x => x.Key, StringComparer.Ordinal);
        writer.WriteStartObject();
        foreach (var context in contexts)
        {
            writer.WritePropertyName(context.Key);
            JsonSerializer.CreateDefault().Serialize(writer, context.Value);
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryContexts FromJson(JToken json)
    {
        var result = new SentryContexts();

        foreach (var property in ((JObject)json).Properties())
        {
            var name = property.Name;
            var value = property.Value;
            var type = value["type"]?.Value<string>() ?? name;

            // Handle known context types
            if (string.Equals(type, App.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<App>()!;
            }
            else if (string.Equals(type, Browser.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Browser>()!;
            }
            else if (string.Equals(type, Device.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Device>()!;
            }
            else if (string.Equals(type, SentryFeedback.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<SentryFeedback>()!;
            }
            else if (string.Equals(type, OperatingSystem.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<OperatingSystem>()!;
            }
            else if (string.Equals(type, Response.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Response>()!;
            }
            else if (string.Equals(type, Runtime.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Runtime>()!;
            }
            else if (string.Equals(type, Gpu.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Gpu>()!;
            }
            else if (string.Equals(type, Trace.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = value.ToObject<Trace>()!;
            }
            else
            {
                // Unknown context - parse as dictionary
                var dynamicContext = value.ToObject<Dictionary<string, object>>();
                if (dynamicContext is not null)
                {
                    result[name] = dynamicContext;
                }
            }
        }

        return result;
    }

    internal void ReplaceWith(SentryContexts? contexts)
    {
        Clear();

        if (contexts == null)
        {
            return;
        }

        foreach (var context in contexts)
        {
            this[context.Key] = context.Value;
        }
    }

    internal SentryContexts? NullIfEmpty() => _innerDictionary.IsEmpty ? null : this;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _innerDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_innerDictionary).GetEnumerator();

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, object> item)
        => ((ICollection<KeyValuePair<string, object>>)_innerDictionary).Add(item);

    /// <inheritdoc/>
    public void Clear() => _innerDictionary.Clear();

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, object> item) => _innerDictionary.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<string, object>>)_innerDictionary).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, object> item)
        => ((ICollection<KeyValuePair<string, object>>)_innerDictionary).Remove(item);

    /// <inheritdoc/>
    public int Count => _innerDictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => ((ICollection<KeyValuePair<string, object>>)_innerDictionary).IsReadOnly;

    /// <inheritdoc/>
    public void Add(string key, object value) => _innerDictionary.Add(key, value);

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _innerDictionary.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(string key) => ((IDictionary<string, object>)_innerDictionary).Remove(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, out object value)
    {
        if (_innerDictionary.TryGetValue(key, out var innerValue))
        {
            value = innerValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc/>
    public object this[string key]
    {
        get => _innerDictionary[key];
        set => _innerDictionary[key] = value;
    }

    /// <inheritdoc/>
    public ICollection<string> Keys => _innerDictionary.Keys;

    /// <inheritdoc/>
    public ICollection<object> Values => _innerDictionary.Values;
}
