using AccuTrack.Runtime.Abstractions;

namespace AccuTrack.Runtime.Tags;

/// <summary>
/// Read/write via Communication module (SDK drivers). Delegates to drivers by address/name.
/// </summary>
public sealed class TagIOHandler
{
    private readonly TagManager _tagManager;
    private Func<IReadOnlyList<ICommunicationDriver>>? _driverProvider;

    public TagIOHandler(TagManager tagManager)
    {
        _tagManager = tagManager;
    }

    public void SetDriverProvider(Func<IReadOnlyList<ICommunicationDriver>>? driverProvider)
    {
        _driverProvider = driverProvider;
    }

    public object? Read(string tagNameOrAddress)
    {
        var tag = _tagManager.GetTag(tagNameOrAddress);
        if (tag != null)
            return tag.Value;
        if (_driverProvider != null)
        {
            foreach (var driver in _driverProvider())
            {
                try
                {
                    var v = driver.ReadAsync(tagNameOrAddress).GetAwaiter().GetResult();
                    if (v != null) return v;
                }
                catch { /* try next */ }
            }
        }
        return null;
    }

    public async Task<object?> ReadAsync(string tagNameOrAddress, CancellationToken cancellationToken = default)
    {
        var tag = _tagManager.GetTag(tagNameOrAddress);
        if (tag != null)
            return tag.Value;
        if (_driverProvider != null)
        {
            foreach (var driver in _driverProvider())
            {
                try
                {
                    var v = await driver.ReadAsync(tagNameOrAddress, cancellationToken).ConfigureAwait(false);
                    if (v != null) return v;
                }
                catch { /* try next */ }
            }
        }
        return null;
    }

    public void Write(string tagNameOrAddress, object value)
    {
        var tag = _tagManager.GetTag(tagNameOrAddress);
        if (tag != null)
        {
            _tagManager.SetValue(tagNameOrAddress, value);
            return;
        }
        if (_driverProvider != null)
        {
            foreach (var driver in _driverProvider())
            {
                try
                {
                    driver.WriteAsync(tagNameOrAddress, value).GetAwaiter().GetResult();
                    return;
                }
                catch { /* try next */ }
            }
        }
    }

    public async Task WriteAsync(string tagNameOrAddress, object value, CancellationToken cancellationToken = default)
    {
        var tag = _tagManager.GetTag(tagNameOrAddress);
        if (tag != null)
        {
            _tagManager.SetValue(tagNameOrAddress, value);
            return;
        }
        if (_driverProvider != null)
        {
            foreach (var driver in _driverProvider())
            {
                try
                {
                    await driver.WriteAsync(tagNameOrAddress, value, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch { /* try next */ }
            }
        }
    }
}
