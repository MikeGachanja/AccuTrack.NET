using System.IO;
using System.Text.Json;

namespace Runtime.Modules.Project;

/// <summary>
/// Project module: loads project from metadata.iscr and json files, exposes project data and screen list.
/// </summary>
public sealed class ProjectModule : IProject
{
    private readonly ProjectData _project = new();
    private string _projectDir = "";

    public int ResolutionWidth => _project.ResolutionWidth;
    public int ResolutionHeight => _project.ResolutionHeight;
    public ProjectData? CurrentProject => _project.Screen.Count > 0 ? _project : null;

    public event EventHandler<string>? ProjectLoaded;
    public event EventHandler<string>? ScreenAvailable;
    public event EventHandler? ResolutionChanged;

    public bool OpenProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            return false;

        // If path is a file (e.g. metadata.iscr), use its directory
        _projectDir = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath;
        if (string.IsNullOrEmpty(_projectDir))
            return false;

        if (!LoadProject(_projectDir))
            return false;

        ProjectLoaded?.Invoke(this, _project.Name);
        ResolutionChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string GetFirstScreenPath()
    {
        var screens = _project.Screen;
        if (screens.Count == 0) return "";
        return screens[0].Address;
    }

    public IReadOnlyList<ScreenInfo> GetScreens() => _project.Screen;

    private bool LoadProject(string projectDir)
    {
        var metadataPath = Path.Combine(projectDir, "metadata.iscr");
        if (!File.Exists(metadataPath))
            return false;

        if (!LoadProjectMetadata(metadataPath))
            return false;

        var jsonPath = Path.Combine(projectDir, "json");
        LoadCommunicationInfo(Path.Combine(jsonPath, "communications.json"));
        LoadScreensInfo(Path.Combine(jsonPath, "screens.json"));
        LoadScriptsInfo(Path.Combine(jsonPath, "scripts.json"));
        LoadTagsInfo(Path.Combine(jsonPath, "tags.json"));
        LoadAlarmsInfo(Path.Combine(jsonPath, "alarms.json"));
        LoadHistorianInfo(Path.Combine(jsonPath, "historian.json"));
        LoadSecurityInfo(Path.Combine(jsonPath, "security.json"));

        return true;
    }

    private bool LoadProjectMetadata(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _project.Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            _project.Type = root.TryGetProperty("type", out var t) ? t.GetInt32() : 0;
            _project.Version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
            _project.ResolutionWidth = 1024;
            _project.ResolutionHeight = 768;
            if (root.TryGetProperty("resolution", out var res))
            {
                _project.ResolutionWidth = res.TryGetProperty("width", out var w) ? w.GetInt32() : 1024;
                _project.ResolutionHeight = res.TryGetProperty("height", out var h) ? h.GetInt32() : 768;
            }
            if (root.TryGetProperty("configFiles", out var cf))
            {
                _project.ConfigPaths.Alarms = cf.TryGetProperty("alarms", out var a) ? a.GetString() ?? "" : "";
                _project.ConfigPaths.Communication = cf.TryGetProperty("communication", out var c) ? c.GetString() ?? "" : "";
                _project.ConfigPaths.Historian = cf.TryGetProperty("historian", out var hi) ? hi.GetString() ?? "" : "";
                _project.ConfigPaths.Screens = cf.TryGetProperty("screens", out var sc) ? sc.GetString() ?? "" : "";
                _project.ConfigPaths.Scripts = cf.TryGetProperty("scripts", out var si) ? si.GetString() ?? "" : "";
                _project.ConfigPaths.Security = cf.TryGetProperty("security", out var se) ? se.GetString() ?? "" : "";
                _project.ConfigPaths.Tags = cf.TryGetProperty("tags", out var tg) ? tg.GetString() ?? "" : "";
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void LoadCommunicationInfo(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var array = root.TryGetProperty("communication_modules", out var cm) ? cm : root.TryGetProperty("modules", out var m) ? m : default;
            if (array.ValueKind != JsonValueKind.Array) return;
            foreach (var item in array.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                var address = item.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
                if (item.TryGetProperty("config", out var config))
                {
                    name = config.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : name;
                    if (type == "Modbus")
                    {
                        var host = config.TryGetProperty("host", out var h) ? h.GetString() ?? "" : "";
                        var port = config.TryGetProperty("port", out var p) ? p.GetInt32() : 502;
                        address = $"{host}:{port}";
                    }
                    else if (type == "OPC")
                        address = config.TryGetProperty("endpoint", out var e) ? e.GetString() ?? "" : address;
                }
                _project.Communication.Add(new CommunicationInfo { Name = name, Type = type, Address = address });
            }
        }
        catch { }
    }

    private void LoadScreensInfo(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("screens", out var screens) || screens.ValueKind != JsonValueKind.Array)
                return;

            var jsonDir = Path.GetDirectoryName(path) ?? "";
            var projectDir = Path.GetDirectoryName(jsonDir) ?? _projectDir;
            var screensDir = Path.Combine(projectDir, "screens");

            foreach (var item in screens.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var id = item.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? name : name;
                if (string.IsNullOrWhiteSpace(id)) id = name;
                var jsonFile = Path.Combine(screensDir, id + ".json");
                var address = File.Exists(jsonFile) ? Path.GetFullPath(jsonFile) : jsonFile;
                _project.Screen.Add(new ScreenInfo { Name = name, Type = "screen", Address = address });
            }

            if (_project.Screen.Count > 0)
                ScreenAvailable?.Invoke(this, _project.Screen[0].Address);
        }
        catch { }
    }

    private void LoadScriptsInfo(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("scripts", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var item in arr.EnumerateArray())
                    _project.Script.Add(new ScriptInfo
                    {
                        Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Path = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : ""
                    });
        }
        catch { }
    }

    private void LoadTagsInfo(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("tags", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var item in arr.EnumerateArray())
                    _project.Tags.Add(new TagInfo
                    {
                        Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Address = item.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "",
                        Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : ""
                    });
        }
        catch { }
    }

    private void LoadAlarmsInfo(string path) { if (File.Exists(path)) { } }
    private void LoadHistorianInfo(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            _project.Historian.Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            _project.Historian.Type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
        }
        catch { }
    }

    private void LoadSecurityInfo(string path) { if (File.Exists(path)) { } }
}
