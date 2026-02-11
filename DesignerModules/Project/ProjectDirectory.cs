using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Manages project directory structure and recent projects.
/// </summary>
public static class ProjectDirectory
{
    private const string DefaultFolder = "Accutrack";
    private const int MaxRecentProjects = 10;

    /// <summary>
    /// Gets the default projects path (Documents/Accutrack).
    /// </summary>
    public static string GetDefaultProjectsPath()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, DefaultFolder);
    }

    /// <summary>
    /// Gets the full path to a project directory.
    /// </summary>
    public static string GetProjectPath(string projectName)
    {
        return Path.Combine(GetDefaultProjectsPath(), projectName);
    }

    /// <summary>
    /// Gets the full path to a project file (.isc).
    /// </summary>
    public static string GetProjectFilePath(string projectName)
    {
        return Path.Combine(GetProjectPath(projectName), projectName + ".isc");
    }

    /// <summary>
    /// Gets the path to a SCADA project directory.
    /// </summary>
    public static string GetScadaPath(string projectPath, string scadaName)
    {
        return Path.Combine(projectPath, scadaName);
    }

    /// <summary>
    /// Gets the path to a SCADA metadata file.
    /// </summary>
    public static string GetMetadataPath(string scadaPath, string scadaName)
    {
        return Path.Combine(scadaPath, scadaName + "_metadata.json");
    }

    /// <summary>
    /// Creates a project directory.
    /// </summary>
    public static bool CreateProjectDirectory(string projectName)
    {
        string path = GetProjectPath(projectName);
        return CreateDirectory(path, "project directory");
    }

    /// <summary>
    /// Creates a SCADA directory with standard subdirectories.
    /// </summary>
    public static bool CreateScadaDirectory(string projectPath, string scadaName)
    {
        string path = GetScadaPath(projectPath, scadaName);
        if (!CreateDirectory(path, "SCADA directory"))
            return false;

        // Create standard subdirectories
        string[] subdirs = { "screens", "scripts", "tags", "communications" };
        foreach (string subdir in subdirs)
        {
            string subdirPath = Path.Combine(path, subdir);
            if (!CreateDirectory(subdirPath, $"{subdir} directory"))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Ensures the default projects directory exists.
    /// </summary>
    public static void EnsureDefaultDirectory()
    {
        CreateDirectory(GetDefaultProjectsPath(), "default projects directory");
    }

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    private static string GetSettingsPath()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Indusys", "AccutrackDesigner");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        return Path.Combine(appDataPath, "settings.json");
    }

    /// <summary>
    /// Sets the last opened project and updates recent projects list.
    /// </summary>
    public static void SetLastOpenedProject(string path)
    {
        string settingsPath = GetSettingsPath();
        var settings = LoadSettings();
        
        // Get recent projects list
        var recentProjects = settings.GetValue("recentProjects")?.ToObject<List<string>>() ?? new List<string>();
        
        // Remove if already exists
        recentProjects.Remove(path);
        
        // Add to beginning
        recentProjects.Insert(0, path);
        
        // Keep only last 10
        while (recentProjects.Count > MaxRecentProjects)
        {
            recentProjects.RemoveAt(recentProjects.Count - 1);
        }
        
        settings["recentProjects"] = JToken.FromObject(recentProjects);
        SaveSettings(settings);
    }

    /// <summary>
    /// Gets the list of recent projects.
    /// </summary>
    public static List<string> GetRecentProjects()
    {
        var settings = LoadSettings();
        var recentProjects = settings.GetValue("recentProjects")?.ToObject<List<string>>() ?? new List<string>();
        
        // Filter out non-existent projects
        var existingProjects = new List<string>();
        foreach (string path in recentProjects)
        {
            if (Directory.Exists(path))
            {
                // Check if directory contains a .isc file
                var iscFiles = Directory.GetFiles(path, "*.isc", SearchOption.TopDirectoryOnly);
                if (iscFiles.Length > 0)
                {
                    existingProjects.Add(path);
                }
            }
            else if (File.Exists(path) && Path.GetExtension(path).Equals(".isc", StringComparison.OrdinalIgnoreCase))
            {
                existingProjects.Add(path);
            }
        }
        
        return existingProjects;
    }

    /// <summary>
    /// Removes a project from the recent projects list.
    /// </summary>
    public static void RemoveRecentProject(string path)
    {
        var settings = LoadSettings();
        var recentProjects = settings.GetValue("recentProjects")?.ToObject<List<string>>() ?? new List<string>();
        
        recentProjects.Remove(path);
        
        settings["recentProjects"] = JToken.FromObject(recentProjects);
        SaveSettings(settings);
    }

    /// <summary>
    /// Gets the project author from the project file.
    /// </summary>
    public static string GetProjectAuthor(string projectPath)
    {
        string projectFilePath = ResolveProjectFilePath(projectPath);
        if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
            return string.Empty;

        try
        {
            string json = File.ReadAllText(projectFilePath);
            var projectObj = JObject.Parse(json);
            
            var settingsObj = projectObj["settings"] as JObject;
            if (settingsObj == null)
                return string.Empty;

            return settingsObj["author"]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the project version from the project file.
    /// </summary>
    public static string GetProjectVersion(string projectPath)
    {
        string projectFilePath = ResolveProjectFilePath(projectPath);
        if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
            return string.Empty;

        try
        {
            string json = File.ReadAllText(projectFilePath);
            var projectObj = JObject.Parse(json);
            
            var settingsObj = projectObj["settings"] as JObject;
            if (settingsObj == null)
                return string.Empty;

            return settingsObj["version"]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves the project file path from either a directory or file path.
    /// </summary>
    private static string ResolveProjectFilePath(string projectPath)
    {
        if (Directory.Exists(projectPath))
        {
            // It's a directory, find the .isc file
            var iscFiles = Directory.GetFiles(projectPath, "*.isc", SearchOption.TopDirectoryOnly);
            if (iscFiles.Length == 0)
                return string.Empty;
            return iscFiles[0];
        }
        else if (File.Exists(projectPath) && Path.GetExtension(projectPath).Equals(".isc", StringComparison.OrdinalIgnoreCase))
        {
            return projectPath;
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    private static bool CreateDirectory(string path, string description)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Loads settings from file.
    /// </summary>
    private static JObject LoadSettings()
    {
        string settingsPath = GetSettingsPath();
        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                return JObject.Parse(json);
            }
            catch
            {
                // Return empty object on error
            }
        }
        return new JObject();
    }

    /// <summary>
    /// Saves settings to file.
    /// </summary>
    private static void SaveSettings(JObject settings)
    {
        string settingsPath = GetSettingsPath();
        string directory = Path.GetDirectoryName(settingsPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(settingsPath, settings.ToString());
    }
}
