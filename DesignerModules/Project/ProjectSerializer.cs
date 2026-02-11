using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Handles project serialization with versioning and migration support.
/// </summary>
public static class ProjectSerializer
{
    private const int CURRENT_PROJECT_VERSION = 1;
    private const string PROJECT_FILE_EXTENSION = ".isc";

    /// <summary>
    /// Serializes a project to JSON with version information.
    /// </summary>
    public static JObject SerializeProject(Project project, string projectDirectory)
    {
        var json = project.ToJson();
        
        // Add version information
        json["version"] = CURRENT_PROJECT_VERSION;
        json["fileFormat"] = "AccuTrack Designer Project";
        json["createdWith"] = "AccuTrack Designer 1.0";
        json["serializedDate"] = DateTime.Now.ToString("O");

        // Convert absolute paths to relative
        var scadaArray = json["scadaProjects"] as JArray;
        if (scadaArray != null)
        {
            foreach (JObject scadaObj in scadaArray.Cast<JObject>())
            {
                if (scadaObj["path"] != null)
                {
                    string absolutePath = scadaObj["path"]!.ToString();
                    scadaObj["path"] = ToRelativePath(absolutePath, projectDirectory);
                }
            }
        }

        return json;
    }

    /// <summary>
    /// Deserializes a project from JSON with version migration support.
    /// </summary>
    public static Project? DeserializeProject(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
            return null;

        try
        {
            string jsonContent = File.ReadAllText(projectFilePath);
            var json = JObject.Parse(jsonContent);

            // Check version and migrate if needed
            int version = json["version"]?.ToObject<int>() ?? 0;
            if (version < CURRENT_PROJECT_VERSION)
            {
                json = MigrateProject(json, version, CURRENT_PROJECT_VERSION);
            }

            // Deserialize project
            var project = Project.FromJson(json);
            project.Path = projectFilePath;

            // Convert relative paths to absolute
            string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            foreach (var scada in project.ScadaProjects)
            {
                scada.Path = ToAbsolutePath(scada.Path, projectDirectory);
                scada.Paths.InitializeFromRoot(scada.Path, scada.Name);
            }

            return project;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Migrates project from an older version to a newer version.
    /// </summary>
    private static JObject MigrateProject(JObject json, int fromVersion, int toVersion)
    {
        // Version 0 -> Version 1: Add version field and ensure all required fields exist
        if (fromVersion == 0 && toVersion >= 1)
        {
            if (json["version"] == null)
            {
                json["version"] = 1;
            }

            // Ensure settings exist
            if (json["settings"] == null)
            {
                json["settings"] = new JObject();
            }

            // Ensure scadaProjects array exists
            if (json["scadaProjects"] == null)
            {
                json["scadaProjects"] = new JArray();
            }
        }

        // Future migrations can be added here
        // if (fromVersion == 1 && toVersion >= 2) { ... }

        return json;
    }

    /// <summary>
    /// Validates project file format.
    /// </summary>
    public static bool ValidateProjectFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        if (!filePath.EndsWith(PROJECT_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            var json = JObject.Parse(jsonContent);

            // Check required fields
            if (json["name"] == null || json["settings"] == null)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToRelativePath(string absolutePath, string basePath)
    {
        if (string.IsNullOrEmpty(basePath) || !Path.IsPathRooted(absolutePath))
            return absolutePath;

        try
        {
            var absoluteUri = new Uri(absolutePath);
            var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            var relativeUri = baseUri.MakeRelativeUri(absoluteUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            return absolutePath;
        }
    }

    private static string ToAbsolutePath(string relativePath, string basePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        if (string.IsNullOrEmpty(basePath))
            return relativePath;

        try
        {
            return Path.GetFullPath(Path.Combine(basePath, relativePath));
        }
        catch
        {
            return relativePath;
        }
    }
}
