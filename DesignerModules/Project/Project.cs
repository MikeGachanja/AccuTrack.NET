using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Represents a project with its settings and SCADA projects.
/// </summary>
public class Project
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ProjectSettings Settings { get; set; } = new();
    public List<ScadaProject> ScadaProjects { get; set; } = new();
    public DeviceNetwork? DeviceNetwork { get; set; }
    public bool Modified { get; set; }

    /// <summary>
    /// Converts the project to a JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject
        {
            ["name"] = Name,
            ["path"] = Path,
            ["settings"] = Settings.ToJson(),
            ["modified"] = Modified
        };

        var scadaArray = new JArray();
        foreach (var scada in ScadaProjects)
        {
            scadaArray.Add(scada.ToJson());
        }
        obj["scadaProjects"] = scadaArray;

        if (DeviceNetwork != null)
        {
            obj["deviceNetwork"] = DeviceNetwork.ToJson();
        }

        return obj;
    }

    /// <summary>
    /// Creates a Project from a JSON object.
    /// </summary>
    public static Project FromJson(JObject json)
    {
        var project = new Project
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Path = json["path"]?.ToString() ?? string.Empty,
            Settings = ProjectSettings.FromJson(json["settings"] as JObject ?? new JObject()),
            Modified = json["modified"]?.ToObject<bool>() ?? false
        };

        var scadaArray = json["scadaProjects"] as JArray;
        if (scadaArray != null)
        {
            foreach (var item in scadaArray)
            {
                if (item is JObject scadaObj)
                {
                    project.ScadaProjects.Add(ScadaProject.FromJson(scadaObj));
                }
            }
        }

        var deviceNetworkObj = json["deviceNetwork"] as JObject;
        if (deviceNetworkObj != null)
        {
            project.DeviceNetwork = DeviceNetwork.FromJson(deviceNetworkObj);
        }

        return project;
    }
}

/// <summary>
/// Project settings including metadata.
/// </summary>
public class ProjectSettings
{
    public string ProjectName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Converts settings to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["projectName"] = ProjectName,
            ["description"] = Description,
            ["author"] = Author,
            ["company"] = Company,
            ["version"] = Version,
            ["createdDate"] = CreatedDate.ToString("O"),
            ["modifiedDate"] = ModifiedDate.ToString("O")
        };
    }

    /// <summary>
    /// Creates settings from JSON object.
    /// </summary>
    public static ProjectSettings FromJson(JObject json)
    {
        var settings = new ProjectSettings
        {
            ProjectName = json["projectName"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty,
            Author = json["author"]?.ToString() ?? string.Empty,
            Company = json["company"]?.ToString() ?? string.Empty,
            Version = json["version"]?.ToString() ?? "1.0.0"
        };

        if (DateTime.TryParse(json["createdDate"]?.ToString(), out DateTime createdDate))
        {
            settings.CreatedDate = createdDate;
        }

        if (DateTime.TryParse(json["modifiedDate"]?.ToString(), out DateTime modifiedDate))
        {
            settings.ModifiedDate = modifiedDate;
        }

        return settings;
    }
}
