using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Validates project structure and configuration.
/// </summary>
public static class ProjectValidator
{
    /// <summary>
    /// Validates a project and returns validation results.
    /// </summary>
    public static ValidationResult ValidateProject(Project project)
    {
        var result = new ValidationResult();

        if (project == null)
        {
            result.Errors.Add("Project is null");
            return result;
        }

        // Validate project name
        if (string.IsNullOrWhiteSpace(project.Name))
        {
            result.Errors.Add("Project name is empty");
        }

        // Validate project path
        if (string.IsNullOrWhiteSpace(project.Path))
        {
            result.Errors.Add("Project path is empty");
        }
        else if (!File.Exists(project.Path))
        {
            result.Warnings.Add($"Project file not found at: {project.Path}");
        }

        // Validate SCADA projects
        if (project.ScadaProjects == null || project.ScadaProjects.Count == 0)
        {
            result.Warnings.Add("No SCADA projects defined");
        }
        else
        {
            foreach (var scada in project.ScadaProjects)
            {
                ValidateScadaProject(scada, result);
            }
        }

        // Validate settings
        if (project.Settings == null)
        {
            result.Errors.Add("Project settings are missing");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(project.Settings.ProjectName))
            {
                result.Warnings.Add("Project name in settings is empty");
            }
        }

        return result;
    }

    private static void ValidateScadaProject(ScadaProject scada, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(scada.Name))
        {
            result.Errors.Add($"SCADA project has empty name");
        }

        if (string.IsNullOrWhiteSpace(scada.Path))
        {
            result.Errors.Add($"SCADA project '{scada.Name}' has empty path");
        }
        else if (!Directory.Exists(scada.Path))
        {
            result.Warnings.Add($"SCADA project '{scada.Name}' directory not found: {scada.Path}");
        }

        // Validate paths
        if (scada.Paths != null)
        {
            ValidatePath(scada.Paths.TagsPath, $"SCADA '{scada.Name}' Tags", result);
            ValidatePath(scada.Paths.ScriptsPath, $"SCADA '{scada.Name}' Scripts", result);
            ValidatePath(scada.Paths.ScreensPath, $"SCADA '{scada.Name}' Screens", result);
        }

        // Validate resolution
        if (scada.Resolution.Width <= 0 || scada.Resolution.Height <= 0)
        {
            result.Errors.Add($"SCADA project '{scada.Name}' has invalid resolution");
        }
    }

    private static void ValidatePath(string path, string description, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            result.Warnings.Add($"{description} path is empty");
        }
        else if (!Directory.Exists(path))
        {
            result.Warnings.Add($"{description} directory not found: {path}");
        }
    }

    /// <summary>
    /// Validates project file format.
    /// </summary>
    public static bool ValidateProjectFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            // Use ProjectSerializer validation
            return ProjectSerializer.ValidateProjectFile(filePath);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents validation results.
/// </summary>
public class ValidationResult
{
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();

    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
}
