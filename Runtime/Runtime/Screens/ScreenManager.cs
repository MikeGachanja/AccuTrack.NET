using Avalonia.Controls;

namespace AccuTrack.Runtime.Screens;

/// <summary>
/// Holds current screen content; LoadScreen/UnloadScreen; Container for main window to bind.
/// </summary>
public sealed class ScreenManager
{
    private Control? _currentContent;
    private readonly object _lock = new();

    public Control? Container
    {
        get { lock (_lock) return _currentContent; }
        set { lock (_lock) _currentContent = value; }
    }

    public Control? CurrentView => Container;

    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? ScreenLoaded;
    public event EventHandler<string>? ScreenUnloaded;

    public void LoadScreen(string pathOrName)
    {
        lock (_lock)
        {
            // Resolve pathOrName to Avalonia view type (from Designer compiler); for now set placeholder or null
            // When Designer generates views: resolve type by name, activate, set as Container
            ScreenLoaded?.Invoke(this, pathOrName);
        }
    }

    public void UnloadScreen(string pathOrName)
    {
        lock (_lock)
        {
            _currentContent = null;
            ScreenUnloaded?.Invoke(this, pathOrName);
        }
    }

    public event EventHandler<Control?>? ContentChanged;

    public void SetContent(Control? control)
    {
        lock (_lock)
        {
            _currentContent = control;
            ContentChanged?.Invoke(this, control);
        }
    }
}
