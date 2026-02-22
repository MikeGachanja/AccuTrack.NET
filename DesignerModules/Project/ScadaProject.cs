using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    public string? StartupScreen { get; set; } // Screen ID or name to load on startup

    /// <summary>Target Runtime device for deploy/upload. When set, this IP is probed first when scanning.</summary>
    public string? TargetDeviceHost { get; set; }
    /// <summary>TCP port for project transfer (default 8888). Discovery uses UDP 8889.</summary>
    public int TargetDevicePort { get; set; } = 8888;
    
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
        var json = new JObject
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
        
        if (!string.IsNullOrEmpty(StartupScreen))
            json["startupScreen"] = StartupScreen;
        if (!string.IsNullOrEmpty(TargetDeviceHost))
            json["targetDeviceHost"] = TargetDeviceHost;
        if (TargetDevicePort != 8888)
            json["targetDevicePort"] = TargetDevicePort;
        
        return json;
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
            Version = json["version"]?.ToString() ?? "1.0.0",
            StartupScreen = json["startupScreen"]?.ToString(),
            TargetDeviceHost = json["targetDeviceHost"]?.ToString(),
            TargetDevicePort = json["targetDevicePort"]?.ToObject<int>() ?? 8888
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
/// A node in the device network representing a configured SCADA project / runtime device.
/// </summary>
public class DeviceNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public ScadaType Type { get; set; } = ScadaType.HMI;
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 8888;
    public string? Description { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public List<string> ConnectedDeviceIds { get; set; } = new();

    public string TypeString => Type switch
    {
        ScadaType.HMI => "HMI",
        ScadaType.PC_STATION => "PC Station",
        ScadaType.BMS => "BMS",
        _ => "Other"
    };

    public JObject ToJson()
    {
        var obj = new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["type"] = (int)Type,
            ["port"] = Port,
            ["x"] = X,
            ["y"] = Y
        };
        if (!string.IsNullOrEmpty(IpAddress)) obj["ipAddress"] = IpAddress;
        if (!string.IsNullOrEmpty(Description)) obj["description"] = Description;
        if (ConnectedDeviceIds.Count > 0) obj["connections"] = new JArray(ConnectedDeviceIds.Cast<object>().ToArray());
        return obj;
    }

    public static DeviceNode FromJson(JObject json)
    {
        var node = new DeviceNode
        {
            Id = json["id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
            Name = json["name"]?.ToString() ?? "",
            Type = (ScadaType)(json["type"]?.ToObject<int>() ?? 0),
            IpAddress = json["ipAddress"]?.ToString(),
            Port = json["port"]?.ToObject<int>() ?? 8888,
            Description = json["description"]?.ToString(),
            X = json["x"]?.ToObject<double>() ?? 0,
            Y = json["y"]?.ToObject<double>() ?? 0
        };
        var conn = json["connections"] as JArray;
        if (conn != null)
            foreach (var c in conn)
                if (c?.ToString() is string id && !string.IsNullOrEmpty(id))
                    node.ConnectedDeviceIds.Add(id);
        return node;
    }
}

/// <summary>
/// Device network: links configured SCADA projects (runtime devices). Synced from project's SCADA list; each node shows name, type, IP, port.
/// </summary>
public class DeviceNetwork
{
    public List<DeviceNode> Devices { get; set; } = new();

    /// <summary>
    /// Syncs devices from the current SCADA projects. Adds a node for each SCADA; updates IP/port from each project's TargetDeviceHost/Port; removes nodes for deleted SCADA.
    /// </summary>
    public void SyncFromScadaProjects(IEnumerable<ScadaProject> scadaProjects)
    {
        var scadaList = scadaProjects?.ToList() ?? new List<ScadaProject>();
        var byName = Devices.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        // Remove nodes whose SCADA no longer exists
        var toRemove = Devices.Where(d => !scadaList.Any(s => string.Equals(s.Name, d.Name, StringComparison.OrdinalIgnoreCase))).ToList();
        foreach (var d in toRemove)
            Devices.Remove(d);

        // Add or update node for each SCADA project
        foreach (var scada in scadaList)
        {
            if (!byName.TryGetValue(scada.Name, out var node))
            {
                node = new DeviceNode
                {
                    Name = scada.Name,
                    Type = scada.Type,
                    Description = $"SCADA Project: {scada.Name}"
                };
                Devices.Add(node);
            }
            else
            {
                node.Type = scada.Type;
            }
            node.IpAddress = string.IsNullOrWhiteSpace(scada.TargetDeviceHost) ? null : scada.TargetDeviceHost.Trim();
            node.Port = scada.TargetDevicePort;
        }
    }

    public IReadOnlyList<DeviceNode> GetAllDevices() => Devices;

    public JObject ToJson()
    {
        var arr = new JArray();
        foreach (var d in Devices)
            arr.Add(d.ToJson());
        return new JObject { ["devices"] = arr };
    }

    public static DeviceNetwork FromJson(JObject json)
    {
        var network = new DeviceNetwork();
        var arr = json["devices"] as JArray;
        if (arr != null)
            foreach (var item in arr)
                if (item is JObject obj)
                    network.Devices.Add(DeviceNode.FromJson(obj));
        return network;
    }
}
