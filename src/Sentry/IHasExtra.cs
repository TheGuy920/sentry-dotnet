namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of untyped extra data.
/// </summary>
public interface IHasExtra
{
    /// <summary>
    /// An arbitrary mapping of additional metadata to store with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Extra { get; }

    /// <summary>
    /// Sets an extra.
    /// </summary>
    public void SetExtra(string key, object? value);
}

/// <summary>
/// Extensions for <see cref="IHasExtra"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HasExtraExtensions
{
    /// <summary>
    /// Sets the extra key-value pairs to the object.
    /// </summary>
    public static void SetExtras(this IHasExtra hasExtra, IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var (key, value) in values)
        {
            hasExtra.SetExtra(key, value);
        }
    }
}
