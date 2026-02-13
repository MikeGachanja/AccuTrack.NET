namespace Runtime.Modules.Project;

public sealed class ConfigPaths
{
    public string Alarms { get; set; } = "";
    public string Communication { get; set; } = "";
    public string Historian { get; set; } = "";
    public string Screens { get; set; } = "";
    public string Scripts { get; set; } = "";
    public string Security { get; set; } = "";
    public string Tags { get; set; } = "";
}

public sealed class CommunicationInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
}

public sealed class ScreenInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "screen";
    public string Address { get; set; } = "";  // Path or URL to screen
    public string Id { get; set; } = "";  // Screen ID for matching startup screen
}

public sealed class ScriptInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public sealed class TagInfo
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Type { get; set; } = "";
}

public sealed class HistorianInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
}

public sealed class ProjectData
{
    public string Name { get; set; } = "";
    public int Type { get; set; }
    public string Version { get; set; } = "";
    public int ResolutionWidth { get; set; } = 1024;
    public int ResolutionHeight { get; set; } = 768;
    public ConfigPaths ConfigPaths { get; set; } = new();
    public List<CommunicationInfo> Communication { get; set; } = new();
    public List<ScreenInfo> Screen { get; set; } = new();
    public List<ScriptInfo> Script { get; set; } = new();
    public List<TagInfo> Tags { get; set; } = new();
    public HistorianInfo Historian { get; set; } = new();
}
