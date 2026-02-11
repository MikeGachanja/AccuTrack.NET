using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Reconstructs project structure from disk files.
/// </summary>
public static class ProjectReconstructor
{
    /// <summary>
    /// Reconstructs a project from disk files.
    /// </summary>
    public static Project? ReconstructProject(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            return null;

        try
        {
            // Try to find project file
            var projectFile = Directory.GetFiles(projectPath, "*.isc", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFile != null && File.Exists(projectFile))
            {
                // Try to load existing project file
                return ProjectSerializer.DeserializeProject(projectFile);
            }

            // Reconstruct from directory structure
            var projectName = Path.GetFileName(projectPath);
            var project = new Project
            {
                Name = projectName,
                Path = Path.Combine(projectPath, $"{projectName}.isc"),
                Settings = new ProjectSettings
                {
                    ProjectName = projectName,
                    CreatedDate = Directory.GetCreationTime(projectPath),
                    ModifiedDate = Directory.GetLastWriteTime(projectPath)
                }
            };

            // Find SCADA project directories
            var scadaDirs = Directory.GetDirectories(projectPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var scadaDir in scadaDirs)
            {
                var scadaName = Path.GetFileName(scadaDir);
                var scadaProject = ReconstructScadaProject(scadaDir, scadaName);
                if (scadaProject != null)
                {
                    project.ScadaProjects.Add(scadaProject);
                }
            }

            return project;
        }
        catch
        {
            return null;
        }
    }

    private static ScadaProject? ReconstructScadaProject(string scadaPath, string scadaName)
    {
        try
        {
            var scadaProject = new ScadaProject
            {
                Name = scadaName,
                Path = scadaPath,
                Type = ScadaType.HMI, // Default type
                Resolution = new System.Drawing.Size(1920, 1080)
            };

            scadaProject.Paths.InitializeFromRoot(scadaPath, scadaName);

            // Try to load metadata if exists
            var metadataFile = Path.Combine(scadaPath, $"{scadaName}_metadata.json");
            if (File.Exists(metadataFile))
            {
                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(metadataFile));
                    scadaProject.Version = json["version"]?.ToString() ?? "1.0.0";
                    var resolutionObj = json["resolution"] as Newtonsoft.Json.Linq.JObject;
                    if (resolutionObj != null)
                    {
                        scadaProject.Resolution = new System.Drawing.Size(
                            resolutionObj["width"]?.ToObject<int>() ?? 1920,
                            resolutionObj["height"]?.ToObject<int>() ?? 1080
                        );
                    }
                }
                catch
                {
                    // Use defaults if metadata can't be loaded
                }
            }

            return scadaProject;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reconstructs project from corrupted or incomplete project file.
    /// </summary>
    public static Project? ReconstructFromCorruptedFile(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
            return null;

        try
        {
            var projectDirectory = Path.GetDirectoryName(projectFilePath);
            if (string.IsNullOrEmpty(projectDirectory))
                return null;

            // Try to reconstruct from directory
            return ReconstructProject(projectDirectory);
        }
        catch
        {
            return null;
        }
    }
}
