namespace AccuTrack.Project;

/// <summary>
/// SCADA project model: screens, tag tables, scripts, communication, alarms, schedules, historian, etc.
/// </summary>
public class ScadaProject
{
    public string ProjectPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ScreenNode> Screens { get; set; } = new();
    public List<TagTableNode> TagTables { get; set; } = new();
    public List<ScriptNode> Scripts { get; set; } = new();
    public List<CommunicationModuleNode> CommunicationModules { get; set; } = new();
    public AlarmsNode? Alarms { get; set; }
    public SchedulesNode? Schedules { get; set; }
    public HistorianNode? Historian { get; set; }
    public SecurityNode? Security { get; set; }
    public MachineLearningNode? MachineLearning { get; set; }
    public DeviceNetworkNode? DeviceNetwork { get; set; }
}

public abstract class ProjectNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
}

public class ScreenNode : ProjectNode { }
public class TagTableNode : ProjectNode { }
public class ScriptNode : ProjectNode { }
public class CommunicationModuleNode : ProjectNode { }
public class AlarmsNode : ProjectNode { }
public class SchedulesNode : ProjectNode { }
public class HistorianNode : ProjectNode { }
public class SecurityNode : ProjectNode { }
public class MachineLearningNode : ProjectNode { }
public class DeviceNetworkNode : ProjectNode { }
