using System.Collections.Concurrent;
using System.IO;

namespace Runtime.Modules.Screens;

/// <summary>Manages current screen path and notifies host to set content (host owns the actual view).</summary>
public sealed class ScreenManager
{
    private readonly ConcurrentDictionary<string, object> _loadedScreens = new();
    private Action<string>? _onLoadScreen;
    private string _currentScreenPath = "";
    private string _projectPath = "";

    public string CurrentScreenPath => _currentScreenPath;

    /// <summary>Set callback when LoadScreen is called (host sets container content).</summary>
    /// <remarks>If a screen is already loaded, the callback will be invoked immediately with the current screen path.</remarks>
    public void SetLoadScreenCallback(Action<string>? callback)
    {
        _onLoadScreen = callback;
        // If a screen was already loaded before the callback was set, trigger it now
        if (callback != null && !string.IsNullOrEmpty(_currentScreenPath))
        {
            callback(_currentScreenPath);
        }
    }

    /// <summary>Load a screen by path; invokes callback so host can set view.</summary>
    public bool LoadScreen(string screenPath)
    {
        if (string.IsNullOrEmpty(screenPath)) return false;
        _currentScreenPath = screenPath;
        _loadedScreens[screenPath] = true;
        _onLoadScreen?.Invoke(screenPath);
        return true;
    }

    public void UnloadScreen(string screenPath)
    {
        _loadedScreens.TryRemove(screenPath, out _);
        if (_currentScreenPath == screenPath)
        {
            _currentScreenPath = "";
            _onLoadScreen?.Invoke(""); // Clear content
        }
    }

    public void UnloadAllScreens()
    {
        _loadedScreens.Clear();
        _currentScreenPath = "";
        _onLoadScreen?.Invoke("");
    }

    /// <summary>Set project root path so ResolveImagePath can resolve image paths. Call when project loads.</summary>
    public void SetProjectPath(string? projectPath) => _projectPath = projectPath ?? "";

    /// <summary>Resolve image name to full path under project (e.g. projectPath/images/imageName). Returns null if not found or no project.</summary>
    public string? ResolveImagePath(string imageName)
    {
        if (string.IsNullOrEmpty(_projectPath) || string.IsNullOrWhiteSpace(imageName)) return null;
        var path = Path.Combine(_projectPath, "images", imageName.Trim());
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    /// <summary>Resolve SVG path (relative to svg/ directory) to full path under project (e.g. projectPath/svg/svgPath). Returns null if not found or no project.</summary>
    public string? ResolveSvgPath(string svgPath)
    {
        if (string.IsNullOrEmpty(_projectPath) || string.IsNullOrWhiteSpace(svgPath)) return null;
        
        // If it's already an absolute path, check if it exists
        if (Path.IsPathRooted(svgPath))
        {
            return File.Exists(svgPath) ? Path.GetFullPath(svgPath) : null;
        }
        
        // Otherwise, resolve relative to project's svg/ directory
        var path = Path.Combine(_projectPath, "svg", svgPath.Trim());
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }
}
