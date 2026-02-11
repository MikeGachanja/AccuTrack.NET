using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Project;

/// <summary>
/// Represents a SCADA project with its configuration and resources.
/// </summary>
public class ScadaProject
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ScadaType Type { get; set; } = ScadaType.HMI;
    public ScadaPaths Paths { get; set; } = new();
    public Size Resolution { get; set; } = new Size(1920, 1080);
    public string Version { get; set; } = "1.0.0";
    
    // These will be loaded from their respective modules
    public List<object> Screens { get; set; } = new();
    public List<object> TagTables { get; set; } = new();
    public List<object> Scripts { get; set; } = new();
    public List<object> CommunicationModules { get; set; } = new();
    public object? Alarms { get; set; }
    public object? Schedules { get; set; }
    public object? Historian { get; set; }
    public object? Security { get; set; }
    public object? MachineLearning { get; set; }

    /// <summary>
    /// Converts SCADA project to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["name"] = Name,
            ["path"] = Path,
            ["type"] = (int)Type,
            ["resolution"] = new JObject
            {
                ["width"] = Resolution.Width,
                ["height"] = Resolution.Height
            },
            ["version"] = Version
        };
    }

    /// <summary>
    /// Creates SCADA project from JSON object.
    /// </summary>
    public static ScadaProject FromJson(JObject json)
    {
        var scada = new ScadaProject
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Path = json["path"]?.ToString() ?? string.Empty,
            Type = (ScadaType)(json["type"]?.ToObject<int>() ?? 0),
            Version = json["version"]?.ToString() ?? "1.0.0"
        };

        var resolutionObj = json["resolution"] as JObject;
        if (resolutionObj != null)
        {
            scada.Resolution = new Size(
                resolutionObj["width"]?.ToObject<int>() ?? 1920,
                resolutionObj["height"]?.ToObject<int>() ?? 1080
            );
        }

        scada.Paths.InitializeFromRoot(scada.Path, scada.Name);
        return scada;
    }
}

/// <summary>
/// SCADA project type enumeration.
/// </summary>
public enum ScadaType
{
    HMI,
    PC_STATION,
    BMS
}

/// <summary>
/// Paths structure for SCADA project directories.
/// </summary>
public class ScadaPaths
{
    public string RootPath { get; set; } = string.Empty;
    public string BuildPath { get; set; } = string.Empty;
    public string ScreensPath { get; set; } = string.Empty;
    public string TagsPath { get; set; } = string.Empty;
    public string ScriptsPath { get; set; } = string.Empty;
    public string CommunicationsPath { get; set; } = string.Empty;
    public string AlarmsPath { get; set; } = string.Empty;
    public string SchedulesPath { get; set; } = string.Empty;
    public string HistorianPath { get; set; } = string.Empty;
    public string SecurityPath { get; set; } = string.Empty;
    public string EventsPath { get; set; } = string.Empty;
    public string MachineLearningPath { get; set; } = string.Empty;
    public string MetadataPath { get; set; } = string.Empty;

    /// <summary>
    /// Initializes all paths based on root path and SCADA name.
    /// </summary>
    public void InitializeFromRoot(string root, string scadaName)
    {
        RootPath = root;
        BuildPath = System.IO.Path.Combine(root, "build");
        ScreensPath = System.IO.Path.Combine(root, "screens");
        TagsPath = System.IO.Path.Combine(root, "tags");
        ScriptsPath = System.IO.Path.Combine(root, "scripts");
        CommunicationsPath = System.IO.Path.Combine(root, "communications");
        AlarmsPath = System.IO.Path.Combine(root, "alarms");
        SchedulesPath = System.IO.Path.Combine(root, "schedules");
        HistorianPath = System.IO.Path.Combine(root, "historian");
        SecurityPath = System.IO.Path.Combine(root, "security");
        EventsPath = System.IO.Path.Combine(root, "events");
        MachineLearningPath = System.IO.Path.Combine(root, "machine_learning");
        MetadataPath = System.IO.Path.Combine(root, $"{scadaName}_metadata.json");
    }
}

/// <summary>
/// Placeholder for DeviceNetwork - will be implemented later.
/// </summary>
public class DeviceNetwork
{
    public JObject ToJson()
    {
        return new JObject();
    }

    public static DeviceNetwork FromJson(JObject json)
    {
        return new DeviceNetwork();
    }
}
