namespace Runtime.Modules.Project;

/// <summary>
/// Project module interface: load project, expose current project data and resolution, emit screen available.
/// </summary>
public interface IProject
{
    /// <summary>Open project from metadata path (e.g. projectDir/metadata.iscr).</summary>
    bool OpenProject(string path);

    /// <summary>Path to first screen (for initial load).</summary>
    string GetFirstScreenPath();

    /// <summary>All screens from current project.</summary>
    IReadOnlyList<ScreenInfo> GetScreens();

    /// <summary>Current project resolution width.</summary>
    int ResolutionWidth { get; }

    /// <summary>Current project resolution height.</summary>
    int ResolutionHeight { get; }

    /// <summary>Current project data, or null if none loaded.</summary>
    ProjectData? CurrentProject { get; }

    /// <summary>Raised when a project is loaded.</summary>
    event EventHandler<string>? ProjectLoaded;

    /// <summary>Raised when a screen path is available (e.g. first screen after load).</summary>
    event EventHandler<string>? ScreenAvailable;

    /// <summary>Raised when resolution changes.</summary>
    event EventHandler? ResolutionChanged;
}
