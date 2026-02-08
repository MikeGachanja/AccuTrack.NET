using System.Text.Json;

namespace AccuTrack.Runtime.Project;

/// <summary>
/// Load project from path; read metadata; resolution (width/height); notify ScreenManager of available screens.
/// </summary>
public sealed class ProjectModule
{
    private string? _projectPath;
    private int _resolutionWidth = 1920;
    private int _resolutionHeight = 1080;
    private string _projectName = "";
    private IReadOnlyList<string> _availableScreens = Array.Empty<string>();

    public string? ProjectPath => _projectPath;
    public int ResolutionWidth => _resolutionWidth;
    public int ResolutionHeight => _resolutionHeight;
    public string ProjectName => _projectName;
    public IReadOnlyList<string> AvailableScreens => _availableScreens;

    public event EventHandler? ProjectLoaded;

    public void OpenProject(string path)
    {
        _projectPath = path;
        var metaPath = Path.Combine(path, "metadata.iscr");
        if (File.Exists(metaPath))
        {
            var json = File.ReadAllText(metaPath);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                _projectName = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : root.TryGetProperty("projectName", out var n2) ? n2.GetString() ?? "" : "";
                if (root.TryGetProperty("resolution", out var res))
                {
                    _resolutionWidth = res.TryGetProperty("width", out var w) ? w.GetInt32() : 1920;
                    _resolutionHeight = res.TryGetProperty("height", out var h) ? h.GetInt32() : 1080;
                }
                if (root.TryGetProperty("screens", out var screens) && screens.ValueKind == JsonValueKind.Array)
                    _availableScreens = screens.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
            catch { /* use defaults */ }
        }
        if (_availableScreens.Count == 0)
        {
            var screensDir = Path.Combine(path, "screens");
            if (Directory.Exists(screensDir))
                _availableScreens = Directory.GetFiles(screensDir, "*.axaml").Select(Path.GetFileNameWithoutExtension).Where(s => !string.IsNullOrEmpty(s)).Cast<string>().ToList();
        }
        ProjectLoaded?.Invoke(this, EventArgs.Empty);
    }

    public void CloseProject()
    {
        _projectPath = null;
        _projectName = "";
        _availableScreens = Array.Empty<string>();
    }
}
