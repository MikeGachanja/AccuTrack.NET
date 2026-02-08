using System.Text.Json;

namespace AccuTrack.Project;

public class ProjectManager
{
    public ScadaProject? CurrentProject { get; private set; }
    /// <summary>Full path to the .isc project file (when project is open).</summary>
    public string? ProjectFilePath { get; private set; }
    public IReadOnlyList<string> RecentProjects { get; private set; } = Array.Empty<string>();
    private const int MaxRecent = 10;
    private List<string> _recentList = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public event EventHandler<ScadaProject?>? ProjectOpened;
    public event EventHandler? ProjectClosed;

    /// <summary>
    /// Create a new project (matching Qt createProject): create project directory and .isc file.
    /// </summary>
    /// <param name="projectDir">Project directory (e.g. .../Accutrack/MyProject).</param>
    /// <param name="name">Project name (used for .isc filename and folder).</param>
    /// <returns>True if created and saved successfully.</returns>
    public bool New(string projectDir, string name)
    {
        Close();
        try
        {
            Directory.CreateDirectory(projectDir);
            string projectFilePath = ProjectFile.GetProjectFilePath(projectDir, name);
            CurrentProject = new ScadaProject
            {
                ProjectPath = projectDir,
                Name = name,
                Screens = new List<ScreenNode> { new() { Name = "Main" } },
                TagTables = new List<TagTableNode> { new() { Name = "Default" } },
                Scripts = new List<ScriptNode>(),
                CommunicationModules = new List<CommunicationModuleNode>(),
                Alarms = new AlarmsNode { Name = "Alarms" },
                Schedules = new SchedulesNode { Name = "Schedules" },
                Historian = new HistorianNode { Name = "Historian" },
                Security = new SecurityNode { Name = "Security" },
                MachineLearning = new MachineLearningNode { Name = "Machine Learning" },
                DeviceNetwork = new DeviceNetworkNode { Name = "Device Network" }
            };
            ProjectFilePath = projectFilePath;
            if (!Save())
                return false;
            AddRecent(projectDir);
            ProjectOpened?.Invoke(this, CurrentProject);
            return true;
        }
        catch
        {
            CurrentProject = null;
            ProjectFilePath = null;
            return false;
        }
    }

    /// <summary>
    /// Open project from directory or .isc file path (matching Qt openProject).
    /// Finds *.isc in directory and loads it.
    /// </summary>
    public bool Open(string path)
    {
        Close();
        try
        {
            string projectDir;
            string projectFilePath;
            if (path.EndsWith(ProjectFile.Extension, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                projectFilePath = path;
                projectDir = Path.GetDirectoryName(path) ?? path;
            }
            else
            {
                projectDir = path;
                var iscFiles = Directory.Exists(projectDir)
                    ? Directory.GetFiles(projectDir, "*" + ProjectFile.Extension)
                    : Array.Empty<string>();
                if (iscFiles.Length == 0)
                    return false;
                projectFilePath = iscFiles[0];
            }

            string json = File.ReadAllText(projectFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string name = root.GetProperty("name").GetString() ?? Path.GetFileNameWithoutExtension(projectFilePath);
            var scadaArray = root.GetProperty("scadaProjects");
            if (scadaArray.GetArrayLength() == 0)
            {
                CurrentProject = new ScadaProject { ProjectPath = projectDir, Name = name };
            }
            else
            {
                var first = scadaArray[0];
                string scadaName = first.GetProperty("name").GetString() ?? name;
                string scadaPath = first.TryGetProperty("path", out var p) ? Path.Combine(projectDir, p.GetString() ?? scadaName) : Path.Combine(projectDir, scadaName);
                CurrentProject = new ScadaProject
                {
                    ProjectPath = projectDir,
                    Name = scadaName,
                    Screens = new List<ScreenNode> { new() { Name = "Main" } },
                    TagTables = new List<TagTableNode> { new() { Name = "Default" } },
                    Scripts = new List<ScriptNode>(),
                    CommunicationModules = new List<CommunicationModuleNode>(),
                    Alarms = new AlarmsNode { Name = "Alarms" },
                    Schedules = new SchedulesNode { Name = "Schedules" },
                    Historian = new HistorianNode { Name = "Historian" },
                    Security = new SecurityNode { Name = "Security" },
                    MachineLearning = new MachineLearningNode { Name = "Machine Learning" },
                    DeviceNetwork = new DeviceNetworkNode { Name = "Device Network" }
                };
                // TODO: load screens/tags/scripts from _metadata.json and other JSON files
            }

            ProjectFilePath = projectFilePath;
            AddRecent(projectDir);
            ProjectOpened?.Invoke(this, CurrentProject);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Save()
    {
        if (CurrentProject == null) return false;
        try
        {
            var projectFilePath = ProjectFile.GetProjectFilePath(CurrentProject.ProjectPath, CurrentProject.Name);
            ProjectFilePath = projectFilePath;
            var projectDir = CurrentProject.ProjectPath;
            var obj = new Dictionary<string, object?>
            {
                ["name"] = CurrentProject.Name,
                ["path"] = ".",
                ["settings"] = new Dictionary<string, object>(),
                ["scadaProjects"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["name"] = CurrentProject.Name,
                        ["path"] = CurrentProject.Name,
                        ["type"] = 0,
                        ["metadataFile"] = CurrentProject.Name + ProjectFile.MetadataSuffix
                    }
                },
                ["deviceNetwork"] = new Dictionary<string, object>()
            };
            string json = JsonSerializer.Serialize(obj, JsonOptions);
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(projectFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Close()
    {
        CurrentProject = null;
        ProjectFilePath = null;
        ProjectClosed?.Invoke(this, EventArgs.Empty);
    }

    private void AddRecent(string path)
    {
        _recentList.Remove(path);
        _recentList.Insert(0, path);
        if (_recentList.Count > MaxRecent) _recentList.RemoveAt(_recentList.Count - 1);
        RecentProjects = _recentList.ToList();
    }
}
