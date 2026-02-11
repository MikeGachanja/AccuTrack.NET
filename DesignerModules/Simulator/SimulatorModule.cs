using System;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Simulator;

/// <summary>
/// Module for simulating SCADA projects.
/// </summary>
public class SimulatorModule
{
    private bool _isRunning = false;
    private bool _isPaused = false;
    private bool _autoBuildBeforeSimulate = true;
    private ProjectManager? _projectManager;

    public event EventHandler<string>? SimulationStarted;
    public event EventHandler<string>? SimulationStopped;
    public event EventHandler<string>? SimulationPaused;
    public event EventHandler<string>? SimulationResumed;

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public bool GetAutoBuildBeforeSimulate() => _autoBuildBeforeSimulate;

    public void SetProjectManager(ProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    /// <summary>
    /// Starts simulation of a SCADA project.
    /// </summary>
    public bool StartSimulation(string scadaName)
    {
        if (_isRunning)
        {
            MessageBox.Show("Simulation is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_projectManager == null)
        {
            MessageBox.Show("Project manager not set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        var scadaProject = _projectManager.FindScadaProject(scadaName);
        if (scadaProject == null)
        {
            MessageBox.Show($"SCADA project '{scadaName}' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        // Show project selection dialog if needed
        if (string.IsNullOrEmpty(scadaName))
        {
            scadaName = ShowProjectSelectionDialog();
            if (string.IsNullOrEmpty(scadaName))
                return false;
        }

        _isRunning = true;
        _isPaused = false;
        SimulationStarted?.Invoke(this, scadaName);
        return true;
    }

    /// <summary>
    /// Stops the simulation.
    /// </summary>
    public void StopSimulation()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _isPaused = false;
        SimulationStopped?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Pauses the simulation.
    /// </summary>
    public void PauseSimulation()
    {
        if (!_isRunning || _isPaused)
            return;

        _isPaused = true;
        SimulationPaused?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Resumes the simulation.
    /// </summary>
    public void ResumeSimulation()
    {
        if (!_isRunning || !_isPaused)
            return;

        _isPaused = false;
        SimulationResumed?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Shows project selection dialog.
    /// </summary>
    public string ShowProjectSelectionDialog()
    {
        if (_projectManager == null)
            return string.Empty;

        using (var dialog = new Form
        {
            Text = "Select SCADA Project",
            Size = new System.Drawing.Size(400, 200),
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

            layout.Controls.Add(new Label { Text = "Select SCADA Project:", AutoSize = true }, 0, 0);

            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var projects = _projectManager.GetScadaProjects();
            foreach (var project in projects)
            {
                comboBox.Items.Add(project.Name);
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }

            layout.Controls.Add(comboBox, 0, 1);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 2);

            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog() == DialogResult.OK && comboBox.SelectedItem != null)
            {
                return comboBox.SelectedItem.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Shows simulator settings dialog.
    /// </summary>
    public void ShowSettingsDialog()
    {
        using (var dialog = new Form
        {
            Text = "Simulator Settings",
            Size = new System.Drawing.Size(400, 200),
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

            var autoBuildCheckBox = new CheckBox
            {
                Text = "Build project before simulating",
                AutoSize = true,
                Checked = _autoBuildBeforeSimulate
            };

            layout.Controls.Add(autoBuildCheckBox, 0, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
            okButton.Click += (s, e) =>
            {
                _autoBuildBeforeSimulate = autoBuildCheckBox.Checked;
                dialog.DialogResult = DialogResult.OK;
            };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 1);

            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            dialog.ShowDialog();
        }
    }
}
