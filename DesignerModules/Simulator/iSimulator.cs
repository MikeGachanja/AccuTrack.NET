using System.Windows.Forms;

namespace Designer.Modules.Simulator;

/// <summary>
/// Simulation control surface aligned with AccuTrackQt SimulationModule.
/// </summary>
public interface ISimulator
{
    bool IsRunning { get; }
    bool IsPaused { get; }

    bool GetAutoBuildBeforeSimulate();
    void SetAutoBuildBeforeSimulate(bool enabled);
    string GetRuntimePath();
    void SetRuntimePath(string path);

    bool StartSimulation(string projectPath, string? runtimePath = null);
    bool StopSimulation();
    void PauseSimulation();
    void ResumeSimulation();

    string ShowProjectSelectionDialog(IWin32Window? parent = null);
    void ShowSettingsDialog(IWin32Window? parent = null);

    string FindRuntimeExecutable();
    bool CopyProjectToRuntimeData(string buildPath, string runtimeDir);

    event System.EventHandler<string>? SimulationStarted;
    event System.EventHandler<string>? SimulationStopped;
    event System.EventHandler<string>? SimulationPaused;
    event System.EventHandler<string>? SimulationResumed;
    event System.EventHandler<string>? SimulationError;
}
