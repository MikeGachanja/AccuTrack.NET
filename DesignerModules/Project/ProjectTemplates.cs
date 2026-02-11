using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Manages project templates.
/// </summary>
public static class ProjectTemplates
{
    private static readonly string TemplatesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AccuTrackDesigner",
        "Templates");

    /// <summary>
    /// Gets all available project templates.
    /// </summary>
    public static List<ProjectTemplate> GetTemplates()
    {
        var templates = new List<ProjectTemplate>();

        try
        {
            if (!Directory.Exists(TemplatesPath))
            {
                Directory.CreateDirectory(TemplatesPath);
                CreateDefaultTemplates();
            }

            var templateFiles = Directory.GetFiles(TemplatesPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in templateFiles)
            {
                try
                {
                    var json = JObject.Parse(File.ReadAllText(file));
                    var template = ProjectTemplate.FromJson(json);
                    template.FilePath = file;
                    templates.Add(template);
                }
                catch
                {
                    // Skip invalid template files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return templates;
    }

    /// <summary>
    /// Creates a project from a template.
    /// </summary>
    public static Project? CreateProjectFromTemplate(ProjectTemplate template, string projectName, string projectPath)
    {
        if (template == null || string.IsNullOrEmpty(projectName))
            return null;

        try
        {
            var project = new Project
            {
                Name = projectName,
                Path = Path.Combine(projectPath, $"{projectName}.isc"),
                Settings = new ProjectSettings
                {
                    ProjectName = projectName,
                    Author = template.Author ?? string.Empty,
                    Description = template.Description ?? string.Empty,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                }
            };

            // Create SCADA projects from template
            foreach (var scadaTemplate in template.ScadaTemplates)
            {
                var scadaPath = Path.Combine(projectPath, scadaTemplate.Name);
                var scadaProject = new ScadaProject
                {
                    Name = scadaTemplate.Name,
                    Path = scadaPath,
                    Type = scadaTemplate.Type,
                    Resolution = scadaTemplate.Resolution,
                    Version = "1.0.0"
                };

                scadaProject.Paths.InitializeFromRoot(scadaPath, scadaTemplate.Name);
                project.ScadaProjects.Add(scadaProject);
            }

            return project;
        }
        catch
        {
            return null;
        }
    }

    private static void CreateDefaultTemplates()
    {
        // Create default HMI template
        var hmiTemplate = new ProjectTemplate
        {
            Name = "Basic HMI",
            Description = "Basic HMI project template",
            Author = "AccuTrack Designer",
            ScadaTemplates = new List<ScadaTemplate>
            {
                new ScadaTemplate
                {
                    Name = "HMI",
                    Type = ScadaType.HMI,
                    Resolution = new System.Drawing.Size(1920, 1080)
                }
            }
        };

        SaveTemplate(hmiTemplate);
    }

    private static void SaveTemplate(ProjectTemplate template)
    {
        try
        {
            var filePath = Path.Combine(TemplatesPath, $"{template.Name}.json");
            File.WriteAllText(filePath, template.ToJson().ToString());
        }
        catch
        {
            // Silently fail
        }
    }
}

/// <summary>
/// Represents a project template.
/// </summary>
public class ProjectTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<ScadaTemplate> ScadaTemplates { get; set; } = new List<ScadaTemplate>();
    public string FilePath { get; set; } = string.Empty;

    public JObject ToJson()
    {
        var scadaArray = new JArray();
        foreach (var scada in ScadaTemplates)
        {
            scadaArray.Add(new JObject
            {
                ["name"] = scada.Name,
                ["type"] = (int)scada.Type,
                ["resolution"] = new JObject
                {
                    ["width"] = scada.Resolution.Width,
                    ["height"] = scada.Resolution.Height
                }
            });
        }

        return new JObject
        {
            ["name"] = Name,
            ["description"] = Description,
            ["author"] = Author,
            ["scadaTemplates"] = scadaArray
        };
    }

    public static ProjectTemplate FromJson(JObject json)
    {
        var template = new ProjectTemplate
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty,
            Author = json["author"]?.ToString() ?? string.Empty
        };

        var scadaArray = json["scadaTemplates"] as JArray;
        if (scadaArray != null)
        {
            foreach (var item in scadaArray)
            {
                if (item is JObject scadaObj)
                {
                    var scadaTemplate = new ScadaTemplate
                    {
                        Name = scadaObj["name"]?.ToString() ?? string.Empty,
                        Type = (ScadaType)(scadaObj["type"]?.ToObject<int>() ?? 0)
                    };

                    var resolutionObj = scadaObj["resolution"] as JObject;
                    if (resolutionObj != null)
                    {
                        scadaTemplate.Resolution = new System.Drawing.Size(
                            resolutionObj["width"]?.ToObject<int>() ?? 1920,
                            resolutionObj["height"]?.ToObject<int>() ?? 1080
                        );
                    }

                    template.ScadaTemplates.Add(scadaTemplate);
                }
            }
        }

        return template;
    }
}

/// <summary>
/// Represents a SCADA project template.
/// </summary>
public class ScadaTemplate
{
    public string Name { get; set; } = string.Empty;
    public ScadaType Type { get; set; } = ScadaType.HMI;
    public System.Drawing.Size Resolution { get; set; } = new System.Drawing.Size(1920, 1080);
}
