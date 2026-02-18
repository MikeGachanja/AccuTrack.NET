using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Simulator;

/// <summary>
/// Module for simulating SCADA projects. Aligned with AccuTrackQt simulation_module:
/// runtime path, auto-build setting, copy project to runtime data, and process lifecycle.
/// </summary>
public class SimulatorModule : ISimulator
{
    private bool _isPaused;
    private string _runtimePath = string.Empty;
    private Process? _runtimeProcess;
    private ProjectManager? _projectManager;
    private readonly string _settingsPath;
    private const string SettingsFileName = "simulator_settings.txt";

    public event EventHandler<string>? SimulationStarted;
    public event EventHandler<string>? SimulationStopped;
    public event EventHandler<string>? SimulationPaused;
    public event EventHandler<string>? SimulationResumed;
    public event EventHandler<string>? SimulationError;

    public bool IsRunning => _runtimeProcess != null && !_runtimeProcess.HasExited;
    public bool IsPaused => _isPaused;

    public SimulatorModule()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "DarkStar", "Designer");
        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);
        _settingsPath = Path.Combine(configDir, SettingsFileName);
        LoadSettings();
    }

    public void SetProjectManager(ProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    /// <summary>
    /// Gets whether to auto-build the project before simulation (persisted).
    /// </summary>
    public bool GetAutoBuildBeforeSimulate()
    {
        return LoadBoolSetting("autoBuildBeforeSimulate", true);
    }

    /// <summary>
    /// Sets whether to auto-build the project before simulation (persisted).
    /// </summary>
    public void SetAutoBuildBeforeSimulate(bool enabled)
    {
        SaveSetting("autoBuildBeforeSimulate", enabled);
    }

    /// <summary>
    /// Gets the configured runtime executable path.
    /// </summary>
    public string GetRuntimePath() => _runtimePath;

    /// <summary>
    /// Sets the runtime executable path and persists it.
    /// </summary>
    public void SetRuntimePath(string path)
    {
        _runtimePath = path ?? string.Empty;
        SaveSetting("runtimePath", _runtimePath);
    }

    /// <summary>
    /// Starts simulation of a SCADA project. projectPath must be the build output directory
    /// (containing metadata.iscr). Optional runtimePath overrides saved setting.
    /// </summary>
    public bool StartSimulation(string projectPath, string? runtimePath = null)
    {
        if (IsRunning)
        {
            MessageBox.Show("Simulation is already running. Please stop it first.", "Simulation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        string actualRuntime = !string.IsNullOrEmpty(runtimePath) ? runtimePath : _runtimePath;
        if (string.IsNullOrEmpty(actualRuntime))
        {
            actualRuntime = FindRuntimeExecutable();
            if (string.IsNullOrEmpty(actualRuntime))
            {
                SimulationError?.Invoke(this, "Runtime executable not found. Please set the runtime path in Simulation Settings.");
                return false;
            }
            _runtimePath = actualRuntime;
            SaveSetting("runtimePath", _runtimePath);
        }

        var runtimeInfo = new FileInfo(actualRuntime);
        if (!runtimeInfo.Exists)
        {
            SimulationError?.Invoke(this, $"Runtime executable not found: {actualRuntime}");
            return false;
        }

        string runtimeDir = runtimeInfo.DirectoryName ?? Path.GetDirectoryName(actualRuntime) ?? string.Empty;
        if (string.IsNullOrEmpty(runtimeDir))
        {
            SimulationError?.Invoke(this, "Could not determine runtime directory.");
            return false;
        }

        if (!CopyProjectToRuntimeData(projectPath, runtimeDir))
        {
            SimulationError?.Invoke(this, "Failed to copy project to runtime data folder.");
            return false;
        }

        try
        {
            _runtimeProcess = new Process();
            _runtimeProcess.StartInfo.FileName = actualRuntime;
            _runtimeProcess.StartInfo.WorkingDirectory = runtimeDir;
            _runtimeProcess.StartInfo.UseShellExecute = false;
            _runtimeProcess.EnableRaisingEvents = true;
            _runtimeProcess.Exited += OnProcessExited;
            _runtimeProcess.Start();
        }
        catch (Exception ex)
        {
            SimulationError?.Invoke(this, $"Failed to start runtime process: {ex.Message}");
            return false;
        }

        _isPaused = false;
        SimulationStarted?.Invoke(this, projectPath);
        return true;
    }

    /// <summary>
    /// Stops the simulation (terminates the runtime process).
    /// </summary>
    public bool StopSimulation()
    {
        if (!IsRunning)
            return false;

        if (_runtimeProcess != null)
        {
            try
            {
                if (!_runtimeProcess.HasExited)
                {
                    _runtimeProcess.Exited -= OnProcessExited;
                    _runtimeProcess.Kill(entireProcessTree: true);
                    _runtimeProcess.WaitForExit(5000);
                }
            }
            catch { /* ignore */ }
            _runtimeProcess.Dispose();
            _runtimeProcess = null;
        }

        _isPaused = false;
        SimulationStopped?.Invoke(this, string.Empty);
        return true;
    }

    /// <summary>
    /// Pauses the simulation (state only; actual process suspend is platform-specific and not implemented).
    /// </summary>
    public void PauseSimulation()
    {
        if (!IsRunning || _isPaused)
            return;
        _isPaused = true;
        SimulationPaused?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Resumes the simulation.
    /// </summary>
    public void ResumeSimulation()
    {
        if (!IsRunning || !_isPaused)
            return;
        _isPaused = false;
        SimulationResumed?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Shows project selection dialog. Returns:
    /// - Build path if project is already built (metadata.iscr exists),
    /// - "BUILD_REQUIRED:scadaName|buildPath" if project is not built (so caller can build then start),
    /// - Empty string if cancelled or no projects.
    /// </summary>
    public string ShowProjectSelectionDialog(IWin32Window? parent = null)
    {
        if (_projectManager?.GetCurrentProject() == null)
        {
            MessageBox.Show(parent as Form, "No project is currently open.", "No Project",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return string.Empty;
        }

        var projects = _projectManager.GetScadaProjects();
        if (projects == null || projects.Count == 0)
        {
            MessageBox.Show(parent as Form, "No SCADA projects found in the current project.", "No Projects",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return string.Empty;
        }

        string? selectedScadaName = null;
        string? buildPath = null;

        using (var dialog = new Form
        {
            Text = "Select Project for Simulation",
            Size = new System.Drawing.Size(420, 180),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog
        })
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            layout.Controls.Add(new Label { Text = "Choose project to simulate:", AutoSize = true }, 0, 0);

            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var p in projects)
                comboBox.Items.Add(p.Name);
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
            layout.Controls.Add(comboBox, 0, 1);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 2);

            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (parent is Form owner)
                dialog.ShowDialog(owner);
            else
                dialog.ShowDialog();

            if (dialog.DialogResult != DialogResult.OK || comboBox.SelectedItem == null)
                return string.Empty;

            selectedScadaName = comboBox.SelectedItem.ToString();
            var scadaProject = _projectManager.FindScadaProject(selectedScadaName ?? string.Empty);
            if (scadaProject == null)
            {
                MessageBox.Show(parent as Form, "Failed to get selected SCADA project.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }

            buildPath = scadaProject.Paths.BuildPath;
            string metadataPath = Path.Combine(buildPath, "metadata.iscr");
            if (!File.Exists(metadataPath))
                return $"BUILD_REQUIRED:{selectedScadaName}|{buildPath}";
            return buildPath;
        }
    }

    /// <summary>
    /// Shows simulator settings dialog (runtime path + auto-build before simulate). Parent used for Browse dialog.
    /// </summary>
    public void ShowSettingsDialog(IWin32Window? parent = null)
    {
        using (var dialog = new Form
        {
            Text = "Simulation Settings",
            Size = new System.Drawing.Size(520, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimumSize = new System.Drawing.Size(500, 180)
        })
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            var runtimeLabel = new Label { Text = "Runtime Executable:", AutoSize = true };
            var runtimeEdit = new TextBox { Text = _runtimePath, Dock = DockStyle.Fill };
            var browseButton = new Button { Text = "Browse...", AutoSize = true };
            browseButton.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Select Runtime Executable",
                    Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
                    FileName = runtimeEdit.Text
                };
                if (ofd.ShowDialog(parent as Form) == DialogResult.OK)
                    runtimeEdit.Text = ofd.FileName;
            };

            var runtimeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            runtimeRow.Controls.Add(runtimeLabel);
            runtimeRow.Controls.Add(runtimeEdit);
            runtimeRow.Controls.Add(browseButton);
            layout.Controls.Add(runtimeRow, 0, 0);

            var autoBuildCheckBox = new CheckBox
            {
                Text = "Auto-build project before simulation",
                AutoSize = true,
                Checked = GetAutoBuildBeforeSimulate()
            };
            layout.Controls.Add(autoBuildCheckBox, 0, 1);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 2);

            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (parent is Form owner)
                dialog.ShowDialog(owner);
            else
                dialog.ShowDialog();

            if (dialog.DialogResult == DialogResult.OK)
            {
                _runtimePath = runtimeEdit.Text?.Trim() ?? string.Empty;
                SetAutoBuildBeforeSimulate(autoBuildCheckBox.Checked);
                SaveSetting("runtimePath", _runtimePath);
            }
        }
    }

    /// <summary>
    /// Tries to locate the Runtime executable relative to the Designer app (same dir, ../Runtime/bin, ../Runtime/build, etc.).
    /// </summary>
    public string FindRuntimeExecutable()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string baseDir = Path.GetDirectoryName(appDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? appDir;

        string[] candidates =
        {
            Path.Combine(appDir, "Runtime.exe"),
            Path.Combine(appDir, "Runtime"),
            Path.Combine(baseDir, "Runtime", "bin", "Runtime.exe"),
            Path.Combine(baseDir, "Runtime", "bin", "Runtime"),
            Path.Combine(baseDir, "Runtime", "bin", "Debug", "net8.0", "Runtime.exe"),
            Path.Combine(baseDir, "Runtime", "bin", "Release", "net8.0", "Runtime.exe")
        };

        foreach (string path in candidates)
        {
            if (string.IsNullOrEmpty(path)) continue;
            try
            {
                var info = new FileInfo(path);
                if (info.Exists)
                    return path;
            }
            catch { /* ignore */ }
        }

        return string.Empty;
    }

    /// <summary>
    /// Copies the built project (buildPath) into the runtime's data folder so Runtime can load it.
    /// </summary>
    public bool CopyProjectToRuntimeData(string buildPath, string runtimeDir)
    {
        if (string.IsNullOrEmpty(buildPath) || !Directory.Exists(buildPath))
            return false;

        string dataDir = Path.Combine(runtimeDir, "data");
        if (Directory.Exists(dataDir))
        {
            try
            {
                Directory.Delete(dataDir, true);
            }
            catch
            {
                return false;
            }
        }

        try
        {
            Directory.CreateDirectory(dataDir);
        }
        catch
        {
            return false;
        }

        try
        {
            foreach (string file in Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(buildPath, file);
                string dest = Path.Combine(dataDir, relative);
                string destDir = Path.GetDirectoryName(dest) ?? "";
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                File.Copy(file, dest, overwrite: true);
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_runtimeProcess != null)
        {
            _runtimeProcess.Exited -= OnProcessExited;
            _runtimeProcess.Dispose();
            _runtimeProcess = null;
        }
        _isPaused = false;
        SimulationStopped?.Invoke(this, string.Empty);
    }

    private void LoadSettings()
    {
        _runtimePath = LoadStringSetting("runtimePath", "");
    }

    private string LoadStringSetting(string key, string defaultValue)
    {
        try
        {
            if (!File.Exists(_settingsPath)) return defaultValue;
            var lines = File.ReadAllLines(_settingsPath);
            foreach (var line in lines)
            {
                if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return line.Substring(key.Length + 1).Trim();
            }
        }
        catch { /* ignore */ }
        return defaultValue;
    }

    private bool LoadBoolSetting(string key, bool defaultValue)
    {
        string v = LoadStringSetting(key, defaultValue ? "true" : "false");
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    private void SaveSetting(string key, object value)
    {
        try
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(_settingsPath))
            {
                foreach (var line in File.ReadAllLines(_settingsPath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0)
                        dict[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            dict[key] = value?.ToString() ?? "";
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(_settingsPath, dict.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        catch { /* ignore */ }
    }
}
