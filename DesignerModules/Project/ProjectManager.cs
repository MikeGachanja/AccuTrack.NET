using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Designer.Modules.TagEngine;
using Designer.Modules.ScriptEditor;
// Removed using Designer.Modules.ScreenEditor; to break circular dependency
// ScreenTemplate will be loaded via reflection/dynamic

namespace Designer.Modules.Project;

/// <summary>
/// Manages project lifecycle, loading, saving, and SCADA project operations.
/// </summary>
public class ProjectManager
{
    private Project? _currentProject;
    private ScadaProject? _currentScadaProject;

    public ProjectManager()
    {
        // Ensure the default projects directory exists
        ProjectDirectory.EnsureDefaultDirectory();
    }

    /// <summary>
    /// Gets the current project.
    /// </summary>
    public Project? GetCurrentProject() => _currentProject;

    /// <summary>
    /// Sets the current project.
    /// </summary>
    public void SetCurrentProject(Project? project) => _currentProject = project;

    /// <summary>
    /// Gets the current SCADA project.
    /// </summary>
    public ScadaProject? GetCurrentScadaProject() => _currentScadaProject;

    /// <summary>
    /// Sets the current SCADA project.
    /// </summary>
    public void SetCurrentScadaProject(ScadaProject? scadaProject) => _currentScadaProject = scadaProject;

    /// <summary>
    /// Gets the current SCADA project name.
    /// </summary>
    public string GetCurrentScadaName()
    {
        return _currentScadaProject?.Name ?? string.Empty;
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    public bool CreateProject(string name)
    {
        // Create project directory
        string projectPath = ProjectDirectory.GetProjectPath(name);
        if (!Directory.Exists(projectPath))
        {
            if (!ProjectDirectory.CreateProjectDirectory(name))
            {
                return false;
            }
        }

        // Create project file path (.isc file)
        string projectFilePath = ProjectDirectory.GetProjectFilePath(name);

        // Close any open project
        CloseProject();

        // Create new project structure
        _currentProject = new Project
        {
            Name = name,
            Path = projectFilePath,
            Settings = CreateDefaultSettings(name),
            DeviceNetwork = new DeviceNetwork(),
            Modified = true
        };

        // Save project file
        bool success = SaveProject();
        if (success)
        {
            // Save project directory path (not the .isc file) for recent projects
            ProjectDirectory.SetLastOpenedProject(projectPath);
        }
        else
        {
            _currentProject = null;
        }

        return success;
    }

    /// <summary>
    /// Opens a project from the specified path.
    /// </summary>
    public bool OpenProject(string path)
    {
        // Resolve project file path
        string projectFilePath = ResolveProjectFilePath(path);
        if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
        {
            return false;
        }

        try
        {
            // Close any open project
            CloseProject();

            // Deserialize project using ProjectSerializer (handles versioning and migration)
            _currentProject = ProjectSerializer.DeserializeProject(projectFilePath);
            if (_currentProject == null)
                return false;

            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? path;
            if (Directory.Exists(path))
            {
                projectDirectory = path;
            }

            // Load SCADA projects data
            foreach (var scada in _currentProject.ScadaProjects)
            {
                LoadScadaProjectData(scada);
            }

            // Update recent projects
            ProjectDirectory.SetLastOpenedProject(projectDirectory);
            _currentProject.Modified = false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the current project.
    /// </summary>
    public bool SaveProject()
    {
        if (_currentProject == null)
            return false;

        try
        {
            // Update modified date
            _currentProject.Settings.ModifiedDate = DateTime.Now;

            // Save project file using ProjectSerializer
            string projectDirectory = Path.GetDirectoryName(_currentProject.Path) ?? string.Empty;
            var projectJson = ProjectSerializer.SerializeProject(_currentProject, projectDirectory);

            // Ensure directory exists
            if (!string.IsNullOrEmpty(projectDirectory) && !Directory.Exists(projectDirectory))
            {
                Directory.CreateDirectory(projectDirectory);
            }

            File.WriteAllText(_currentProject.Path, projectJson.ToString());

            // Save SCADA projects
            foreach (var scada in _currentProject.ScadaProjects)
            {
                SaveScadaProject(scada);
            }

            _currentProject.Modified = false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Closes the current project.
    /// </summary>
    public void CloseProject()
    {
        _currentProject = null;
        _currentScadaProject = null;
    }

    /// <summary>
    /// Gets the list of recent projects.
    /// </summary>
    public List<string> GetRecentProjects()
    {
        return ProjectDirectory.GetRecentProjects();
    }

    /// <summary>
    /// Finds a SCADA project by name.
    /// </summary>
    public ScadaProject? FindScadaProject(string name)
    {
        return _currentProject?.ScadaProjects.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// Gets all SCADA projects.
    /// </summary>
    public List<ScadaProject> GetScadaProjects()
    {
        return _currentProject?.ScadaProjects ?? new List<ScadaProject>();
    }

    /// <summary>
    /// Adds a SCADA project to the current project.
    /// </summary>
    public bool AddScadaProject(string name, string path, ScadaType type, Size resolution = default)
    {
        if (_currentProject == null)
            return false;

        if (resolution == default)
        {
            resolution = new Size(1920, 1080);
        }

        // Determine project directory (where the .isc file lives)
        var projectDir = System.IO.Directory.Exists(path)
            ? path
            : System.IO.Path.GetDirectoryName(_currentProject.Path) ?? _currentProject.Path;

        // SCADA root path under the project directory
        var scadaRootPath = ProjectDirectory.GetScadaPath(projectDir, name);

        var scada = new ScadaProject
        {
            Name = name,
            Path = scadaRootPath,
            Type = type,
            Resolution = resolution,
            Version = "1.0.0"
        };

        scada.Paths.InitializeFromRoot(scadaRootPath, name);

        // Create SCADA directory structure
        if (!ProjectDirectory.CreateScadaDirectory(projectDir, name))
        {
            return false;
        }

        _currentProject.ScadaProjects.Add(scada);
        _currentProject.Modified = true;

        return true;
    }

    /// <summary>
    /// Increments the version of a SCADA project.
    /// </summary>
    public bool IncrementScadaProjectVersion(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null)
            return false;

        scada.Version = IncrementVersion(scada.Version);
        if (_currentProject != null)
        {
            _currentProject.Modified = true;
        }

        return true;
    }

    /// <summary>
    /// Increments a version string (e.g., "1.0.0" -> "1.0.1").
    /// </summary>
    public static string IncrementVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length >= 3 && int.TryParse(parts[2], out int patch))
        {
            patch++;
            return $"{parts[0]}.{parts[1]}.{patch}";
        }
        return version;
    }

    /// <summary>
    /// Creates default project settings.
    /// </summary>
    private ProjectSettings CreateDefaultSettings(string projectName)
    {
        return new ProjectSettings
        {
            ProjectName = projectName,
            Description = "SCADA Project",
            Author = Environment.UserName,
            Company = string.Empty,
            Version = "1.0.0",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };
    }

    /// <summary>
    /// Resolves project file path from directory or file path.
    /// </summary>
    private string ResolveProjectFilePath(string path)
    {
        if (Directory.Exists(path))
        {
            var iscFiles = Directory.GetFiles(path, "*.isc", SearchOption.TopDirectoryOnly);
            if (iscFiles.Length > 0)
            {
                return iscFiles[0];
            }
        }
        else if (File.Exists(path) && Path.GetExtension(path).Equals(".isc", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return string.Empty;
    }

    /// <summary>
    /// Converts absolute path to relative path.
    /// </summary>
    private string ToRelativePath(string absolutePath, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return absolutePath;

        var absoluteUri = new Uri(absolutePath);
        var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
        var relativeUri = baseUri.MakeRelativeUri(absoluteUri);
        return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// Converts relative path to absolute path.
    /// </summary>
    private string ToAbsolutePath(string relativePath, string basePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        return Path.GetFullPath(Path.Combine(basePath, relativePath));
    }

    /// <summary>
    /// Saves SCADA project data.
    /// </summary>
    private void SaveScadaProject(ScadaProject scada)
    {
        // Save metadata file
        UpdateScadaMetadata(scada);
    }

    /// <summary>
    /// Updates SCADA metadata file.
    /// </summary>
    private bool UpdateScadaMetadata(ScadaProject scada)
    {
        try
        {
            var metadata = new JObject
            {
                ["name"] = scada.Name,
                ["type"] = (int)scada.Type,
                ["version"] = scada.Version,
                ["resolution"] = new JObject
                {
                    ["width"] = scada.Resolution.Width,
                    ["height"] = scada.Resolution.Height
                }
            };

            string metadataPath = scada.Paths.MetadataPath;
            string directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(metadataPath, metadata.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads SCADA project data from disk.
    /// </summary>
    private void LoadScadaProjectData(ScadaProject scada)
    {
        // Load metadata if exists
        if (File.Exists(scada.Paths.MetadataPath))
        {
            try
            {
                string json = File.ReadAllText(scada.Paths.MetadataPath);
                var metadata = JObject.Parse(json);
                
                if (metadata["version"] != null)
                {
                    scada.Version = metadata["version"]!.ToString();
                }

                var resolutionObj = metadata["resolution"] as JObject;
                if (resolutionObj != null)
                {
                    scada.Resolution = new Size(
                        resolutionObj["width"]?.ToObject<int>() ?? 1920,
                        resolutionObj["height"]?.ToObject<int>() ?? 1080
                    );
                }
            }
            catch
            {
                // Ignore errors, use defaults
            }
        }
    }

    // Tag table management - basic implementation
    public List<object> GetTagTables(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null)
            return new List<object>();

        // Load tag tables from disk
        var tagTables = new List<object>();
        try
        {
            if (Directory.Exists(scada.Paths.TagsPath))
            {
                var tagFiles = Directory.GetFiles(scada.Paths.TagsPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in tagFiles)
                {
                    try
                    {
                        var table = new TagEngine.TagTable(Path.GetFileNameWithoutExtension(file));
                        if (table.LoadFromFile(file))
                        {
                            tagTables.Add(table);
                        }
                    }
                    catch
                    {
                        // Skip invalid tag table files
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return tagTables;
    }

    public object? GetTagTable(string scadaName, int id)
    {
        var tables = GetTagTables(scadaName);
        // TODO: Implement tag table retrieval by ID (need to add ID to TagTable)
        return tables.Count > 0 ? tables[0] : null;
    }
    /// <summary>
    /// Gets all screens for a SCADA project.
    /// </summary>
    public List<object> GetScreens(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return new List<object>();

        var screens = new List<object>();
        try
        {
            if (Directory.Exists(scada.Paths.ScreensPath))
            {
                var screenFiles = Directory.GetFiles(scada.Paths.ScreensPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in screenFiles)
                {
                    try
                    {
                        var json = JObject.Parse(File.ReadAllText(file));
                        // Load ScreenTemplate using reflection to avoid circular dependency
                        var screenTemplateType = Type.GetType("Designer.Modules.ScreenEditor.ScreenTemplate, ScreenEditor");
                        if (screenTemplateType != null)
                        {
                            var fromJsonMethod = screenTemplateType.GetMethod("FromJson", new[] { typeof(JObject) });
                            if (fromJsonMethod != null)
                            {
                                var screen = fromJsonMethod.Invoke(null, new object[] { json });
                                if (screen != null)
                                {
                                    screenTemplateType.GetProperty("FilePath")?.SetValue(screen, file);
                                    screens.Add(screen);
                                }
                            }
                        }
                        else
                        {
                            // Fallback: store as JObject
                            screens.Add(json);
                        }
                    }
                    catch { /* Skip invalid screen files */ }
                }
            }
        }
        catch { /* Return empty list on error */ }
        return screens;
    }
    
    /// <summary>
    /// Gets all scripts for a SCADA project.
    /// </summary>
    public List<object> GetScripts(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return new List<object>();

        var scripts = new List<object>();
        try
        {
            if (Directory.Exists(scada.Paths.ScriptsPath))
            {
                // Load scripts from JSON metadata files
                var scriptJsonFiles = Directory.GetFiles(scada.Paths.ScriptsPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var jsonFile in scriptJsonFiles)
                {
                    try
                    {
                        var json = JObject.Parse(File.ReadAllText(jsonFile));
                        var script = LuaScript.FromJson(json);
                        // Set the JSON file path, but also check for .lua file
                        script.FilePath = json["filePath"]?.ToString() ?? jsonFile.Replace(".json", ".lua");
                        // Load script code if .lua file exists
                        if (File.Exists(script.FilePath))
                        {
                            script.LoadCode();
                        }
                        scripts.Add(script);
                    }
                    catch { /* Skip invalid script files */ }
                }
            }
        }
        catch { /* Return empty list on error */ }
        return scripts;
    }
    
    /// <summary>
    /// Gets communication modules for a SCADA project.
    /// Returns JSON string or null.
    /// </summary>
    public List<object> GetCommunicationModules(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return new List<object>();

        try
        {
            var commFile = Path.Combine(scada.Paths.CommunicationsPath, "communication_modules.json");
            if (File.Exists(commFile))
            {
                var json = JObject.Parse(File.ReadAllText(commFile));
                // Return as list for compatibility
                return new List<object> { File.ReadAllText(commFile) };
            }
        }
        catch { /* Return empty list on error */ }
        
        return new List<object>();
    }
    
    /// <summary>
    /// Updates communication modules for a SCADA project.
    /// </summary>
    public bool UpdateCommunicationModules(string scadaName, List<object> modules)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null || modules == null || modules.Count == 0) return false;

        try
        {
            if (!Directory.Exists(scada.Paths.CommunicationsPath))
            {
                Directory.CreateDirectory(scada.Paths.CommunicationsPath);
            }

            // Get the CommunicationModules object from the list (use dynamic to avoid circular dependency)
            object? commModulesObj = null;
            foreach (var item in modules)
            {
                // Check if item has ToJson method (indicating it's a CommunicationModules instance)
                try
                {
                    dynamic testItem = item;
                    var testJson = testItem.ToJson();
                    commModulesObj = item;
                    break;
                }
                catch
                {
                    // Not a CommunicationModules instance, continue
                }
            }

            if (commModulesObj == null)
                return false;

            dynamic commModulesDynamic = commModulesObj;
            var json = commModulesDynamic.ToJson();
            var commFile = Path.Combine(scada.Paths.CommunicationsPath, "communication_modules.json");
            File.WriteAllText(commFile, json.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
    public object? GetAlarms(string scadaName) => null;
    
    /// <summary>
    /// Gets schedules for a SCADA project.
    /// Returns null if schedules file doesn't exist or can't be loaded.
    /// Caller should create a new Schedules instance if null is returned.
    /// </summary>
    public object? GetSchedules(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return null;

        try
        {
            var schedulesFile = Path.Combine(scada.Paths.SchedulesPath, "schedules.json");
            if (File.Exists(schedulesFile))
            {
                // Return the file path and let the caller (MainForm) load it using Schedules.FromJson
                // This avoids circular dependency
                return File.ReadAllText(schedulesFile);
            }
        }
        catch { /* Return null on error */ }
        
        return null;
    }
    
    /// <summary>
    /// Saves schedules for a SCADA project.
    /// </summary>
    public bool SaveSchedules(string scadaName, object? schedules)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null || schedules == null) return false;

        try
        {
            if (!Directory.Exists(scada.Paths.SchedulesPath))
            {
                Directory.CreateDirectory(scada.Paths.SchedulesPath);
            }

            // Use dynamic to call ToJson() without direct type reference
            dynamic schedulesObj = schedules;
            var json = schedulesObj.ToJson();
            var schedulesFile = Path.Combine(scada.Paths.SchedulesPath, "schedules.json");
            File.WriteAllText(schedulesFile, json.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// Gets historian for a SCADA project.
    /// Returns JSON string or null.
    /// </summary>
    public object? GetHistorian(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return null;

        try
        {
            var historianFile = Path.Combine(scada.Paths.HistorianPath, "historian.json");
            if (File.Exists(historianFile))
            {
                return File.ReadAllText(historianFile);
            }
        }
        catch { /* Return null on error */ }
        
        return null;
    }
    
    /// <summary>
    /// Saves historian for a SCADA project.
    /// </summary>
    public bool SaveHistorian(string scadaName, object? historian)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null || historian == null) return false;

        try
        {
            if (!Directory.Exists(scada.Paths.HistorianPath))
            {
                Directory.CreateDirectory(scada.Paths.HistorianPath);
            }

            // Use dynamic to call ToJson() without direct type reference
            dynamic historianObj = historian;
            var json = historianObj.ToJson();
            var historianFile = Path.Combine(scada.Paths.HistorianPath, "historian.json");
            File.WriteAllText(historianFile, json.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// Gets security for a SCADA project.
    /// Returns JSON string or null.
    /// </summary>
    public object? GetSecurity(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return null;

        try
        {
            var securityFile = Path.Combine(scada.Paths.SecurityPath, "security.json");
            if (File.Exists(securityFile))
            {
                return File.ReadAllText(securityFile);
            }
        }
        catch { /* Return null on error */ }
        
        return null;
    }
    
    /// <summary>
    /// Saves security for a SCADA project.
    /// </summary>
    public bool SaveSecurity(string scadaName, object? security)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null || security == null) return false;

        try
        {
            if (!Directory.Exists(scada.Paths.SecurityPath))
            {
                Directory.CreateDirectory(scada.Paths.SecurityPath);
            }

            dynamic securityObj = security;
            var json = securityObj.ToJson();
            var securityFile = Path.Combine(scada.Paths.SecurityPath, "security.json");
            File.WriteAllText(securityFile, json.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// Gets machine learning for a SCADA project.
    /// Returns JSON string or null.
    /// </summary>
    public object? GetMachineLearning(string scadaName)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null) return null;

        try
        {
            var mlFile = Path.Combine(scada.Paths.MachineLearningPath, "machine_learning.json");
            if (File.Exists(mlFile))
            {
                return File.ReadAllText(mlFile);
            }
        }
        catch { /* Return null on error */ }
        
        return null;
    }
    
    /// <summary>
    /// Saves machine learning for a SCADA project.
    /// </summary>
    public bool SaveMachineLearning(string scadaName, object? ml)
    {
        var scada = FindScadaProject(scadaName);
        if (scada == null || ml == null) return false;

        try
        {
            if (!Directory.Exists(scada.Paths.MachineLearningPath))
            {
                Directory.CreateDirectory(scada.Paths.MachineLearningPath);
            }

            dynamic mlObj = ml;
            var json = mlObj.ToJson();
            var mlFile = Path.Combine(scada.Paths.MachineLearningPath, "machine_learning.json");
            File.WriteAllText(mlFile, json.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public bool AddMachineLearning(string scadaName) => false;
    public bool AddHistorian(string scadaName) => false;
    public DeviceNetwork? GetDeviceNetwork() => _currentProject?.DeviceNetwork;
}
