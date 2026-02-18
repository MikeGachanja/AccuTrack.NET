using Designer.Modules.Project;
using Designer.Modules.TagEngine;
using Designer.Modules.ScriptEditor;
using Designer.Modules.ScreenEditor;
using Designer.Modules.Console;
using Designer.Modules.Components;
using Designer.Modules.Compiler;
using Designer.Modules.Alarms;
using Designer.Modules.Scheduler;
using Designer.Modules.Historian;
using Designer.Modules.Security;
using Designer.Modules.MachineLearning;
using Designer.Modules.Communication;
using Designer.Modules.Discovery;
using Designer.Modules.Simulator;
using Designer.Modules.Tools;
using Designer.Modules.Project;
using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer
{
    public partial class MainForm : Form
    {
        private ProjectManager? _projectManager;
        private ConsoleModule? _consoleModule;
        private CompilerModule? _compilerModule;
        private ProjectView? _projectView;
        private ComponentsView? _componentsView;
        private PropertyEditor? _propertyEditor;
        private ComponentsModule? _componentsModule;
        private SimulatorModule? _simulatorModule;
        private string? _activeScadaProjectName;
        private ToolStripStatusLabel? _activeProjectLabel;

        public MainForm()
        {
            InitializeComponent();
            _projectManager = new ProjectManager();
            _consoleModule = new ConsoleModule();
            _compilerModule = new CompilerModule();
            _compilerModule.SetConsoleModule(_consoleModule);
            _componentsModule = new ComponentsModule();
            _simulatorModule = new SimulatorModule();
            _simulatorModule.SetProjectManager(_projectManager);
            _simulatorModule.SimulationError += (s, err) =>
            {
                if (InvokeRequired) BeginInvoke(() => MessageBox.Show(this, err ?? "Simulation error.", "Simulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
                else MessageBox.Show(this, err ?? "Simulation error.", "Simulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            _simulatorModule.SimulationStopped += (s, _) =>
            {
                if (InvokeRequired) BeginInvoke(UpdateSimulationStoppedUI);
                else UpdateSimulationStoppedUI();
            };
            ConnectMenuActions();
            InitializeProjectView();
            InitializeConsoles();
            InitializeComponentsView();
            InitializePropertyEditor();
            InitializeTabClosing();
            InitializeActiveProjectDisplay();
        }
        
        private void InitializeActiveProjectDisplay()
        {
            _activeProjectLabel = new ToolStripStatusLabel
            {
                Text = "Active Project: None",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(_activeProjectLabel);
            UpdateActiveProjectDisplay();
        }
        
        private void UpdateActiveProjectDisplay()
        {
            if (_activeProjectLabel != null)
            {
                if (string.IsNullOrEmpty(_activeScadaProjectName))
                {
                    _activeProjectLabel.Text = "Active Project: None";
                    _activeProjectLabel.ForeColor = SystemColors.GrayText;
                }
                else
                {
                    _activeProjectLabel.Text = $"Active Project: {_activeScadaProjectName}";
                    _activeProjectLabel.ForeColor = SystemColors.ControlText;
                }
            }
        }
        
        private void SetActiveScadaProject(string? projectName)
        {
            _activeScadaProjectName = projectName;
            if (_projectManager != null && !string.IsNullOrEmpty(projectName))
            {
                var scadaProject = _projectManager.FindScadaProject(projectName);
                _projectManager.SetCurrentScadaProject(scadaProject);
            }
            else
            {
                _projectManager?.SetCurrentScadaProject(null);
            }
            UpdateActiveProjectDisplay();
        }
        
        private string? GetActiveScadaProjectName()
        {
            // If no active project is set, try to get from ProjectManager
            if (string.IsNullOrEmpty(_activeScadaProjectName))
            {
                _activeScadaProjectName = _projectManager?.GetCurrentScadaName();
            }
            return _activeScadaProjectName;
        }
        
        private string? SelectScadaProject(string title, string message)
        {
            if (_projectManager == null)
                return null;
                
            var scadaProjects = _projectManager.GetScadaProjects();
            if (scadaProjects.Count == 0)
            {
                MessageBox.Show("No SCADA projects found in the current project.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            
            if (scadaProjects.Count == 1)
            {
                // Only one project, use it automatically
                return scadaProjects[0].Name;
            }
            
            // Show dialog to select SCADA project
            using var dialog = new Form
            {
                Text = title,
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent
            };
            
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            
            layout.Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, AutoSize = true }, 0, 0);
            
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBox.Items.AddRange(scadaProjects.Select(s => s.Name).ToArray());
            
            // Pre-select active project if set
            if (!string.IsNullOrEmpty(_activeScadaProjectName))
            {
                var activeIndex = comboBox.Items.IndexOf(_activeScadaProjectName);
                if (activeIndex >= 0)
                    comboBox.SelectedIndex = activeIndex;
                else
                    comboBox.SelectedIndex = 0;
            }
            else
            {
                comboBox.SelectedIndex = 0;
            }
            
            layout.Controls.Add(comboBox, 0, 1);
            
            var setActiveCheckBox = new CheckBox
            {
                Text = "Set as active project for future operations",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Checked = true
            };
            layout.Controls.Add(setActiveCheckBox, 0, 2);
            
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 25) };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 25) };
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 3);
            
            dialog.Controls.Add(layout);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;
            
            if (dialog.ShowDialog() != DialogResult.OK || comboBox.SelectedItem == null)
                return null;
            
            string selectedScadaName = comboBox.SelectedItem.ToString() ?? string.Empty;
            
            // Set as active project if checkbox is checked
            if (setActiveCheckBox.Checked)
            {
                SetActiveScadaProject(selectedScadaName);
            }
            
            return selectedScadaName;
        }

        private int _rightClickedTabIndex = -1;

        private void InitializeTabClosing()
        {
            // Handle middle mouse button to close tabs
            editorTabs.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Middle)
                {
                    for (int i = 0; i < editorTabs.TabCount; i++)
                    {
                        var rect = editorTabs.GetTabRect(i);
                        if (rect.Contains(e.Location))
                        {
                            CloseTab(i);
                            break;
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    _rightClickedTabIndex = -1;
                    for (int i = 0; i < editorTabs.TabCount; i++)
                    {
                        var rect = editorTabs.GetTabRect(i);
                        if (rect.Contains(e.Location))
                        {
                            _rightClickedTabIndex = i;
                            break;
                        }
                    }
                }
            };

            // Context menu for editor tabs
            contextMenuEditorTabs = new ContextMenuStrip();
            var closeTabItem = new ToolStripMenuItem("Close", null, (o, _) => CloseEditorTabFromContext());
            var closeAllItem = new ToolStripMenuItem("Close All Tabs", null, (o, _) => CloseAllEditorTabs());
            contextMenuEditorTabs.Items.Add(closeTabItem);
            contextMenuEditorTabs.Items.Add(closeAllItem);
            contextMenuEditorTabs.Opening += (s, e) =>
            {
                bool hasTabs = editorTabs.TabCount > 0;
                closeTabItem.Enabled = hasTabs;
                closeAllItem.Enabled = hasTabs;
                e.Cancel = !hasTabs;
            };
            editorTabs.ContextMenuStrip = contextMenuEditorTabs;
            UpdateEditorCloseMenuState();
        }

        private void InitializeConsoles()
        {
            // Add console modules to bottom tabs
            if (_consoleModule != null && bottomTabWidget.TabPages.Count >= 2)
            {
                // Output tab
                var outputTab = bottomTabWidget.TabPages[0];
                var outputConsole = _consoleModule.GetOutputConsole();
                outputTab.Controls.Add(outputConsole);
                outputConsole.Dock = DockStyle.Fill;

                // Debug tab
                var debugTab = bottomTabWidget.TabPages[1];
                var debugConsole = _consoleModule.GetDebugConsole();
                debugTab.Controls.Add(debugConsole);
                debugConsole.Dock = DockStyle.Fill;
            }
        }

        private void InitializeComponentsView()
        {
            _componentsView = new ComponentsView();
            _componentsView.Dock = DockStyle.Fill;
            if (_componentsModule != null)
            {
                _componentsView.SetComponentsModule(_componentsModule);
            }
            componentsWidget.Controls.Add(_componentsView);
        }

        private void InitializePropertyEditor()
        {
            _propertyEditor = new PropertyEditor();
            _propertyEditor.Dock = DockStyle.Fill;
            
            // Wire up auto-save request
            _propertyEditor.RequestAutoSave += (s, e) =>
            {
                // Mark current screen editor as modified if it exists
                if (editorTabs.SelectedTab != null)
                {
                    var widget = editorTabs.SelectedTab.Controls.Count > 0 ? editorTabs.SelectedTab.Controls[0] : null;
                    if (widget is ScreenEditor screenEditor)
                    {
                        // ScreenEditor doesn't have a public SetModified, so we'll trigger it through component changes
                        // The screen will be marked modified when SaveScreen is called
                    }
                }
            };
            
            if (bottomTabWidget.TabPages.Count >= 3)
            {
                var propertiesTab = bottomTabWidget.TabPages[2];
                propertiesTab.Controls.Add(_propertyEditor);
            }
        }

        private void InitializeProjectView()
        {
            _projectView = new ProjectView();
            _projectView.Dock = DockStyle.Fill;
            _projectView.SetProjectManager(_projectManager!);
            
            // Connect project view events
            _projectView.DeviceNetworkOpenRequested += (s, e) => OnDeviceNetworkOpen();
            _projectView.SettingsOpenRequested += (s, e) => OnSettingsOpen();
            _projectView.TagTableOpenRequested += (s, e) => OnTagTableOpen(e.TagTable, e.ScadaName);
            _projectView.ScriptOpenRequested += (s, e) => OnScriptOpen(e.Script, e.ScadaName);
            _projectView.ScreenOpenRequested += (s, e) => OnScreenOpen(e.Screen, e.ScadaName);
            _projectView.CommunicationModuleOpenRequested += (s, e) => OnCommunicationModuleOpen(e.ScadaName, e.CommunicationModule);
            _projectView.AlarmsOpenRequested += (s, e) => OnAlarmsOpen(e.ScadaName, e.Alarms);
            _projectView.SchedulesOpenRequested += (s, e) => OnSchedulesOpen(e.ScadaName, e.Schedules);
            _projectView.HistorianOpenRequested += (s, e) => OnHistorianOpen(e.ScadaName, e.Historian);
            _projectView.SecurityOpenRequested += (s, e) => OnSecurityOpen(e.ScadaName, e.Security);
            _projectView.MachineLearningOpenRequested += (s, e) => OnMachineLearningOpen(e.ScadaName, e.MachineLearning);
            _projectView.AllTagTablesOpenRequested += (s, e) => OnAllTagTablesOpen(e.ScadaName, e.TagTables);
            _projectView.ScreenCreated += (s, e) => OnScreenCreated(e.Screen, e.ScadaName);
            _projectView.TagTableCreated += (s, e) => OnTagTableCreated(e.TagTable, e.ScadaName);
            _projectView.ScriptCreated += (s, e) => OnScriptCreated(e.Script, e.ScadaName);

            projectWidget.Controls.Add(_projectView);
        }

        /// <summary>
        /// Creates a new project.
        /// </summary>
        public void NewProject()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Create New Project",
                Filter = "SCADA Projects (*.isc)|*.isc",
                InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string projectName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (_projectManager != null && _projectManager.CreateProject(projectName))
                {
                    _projectView?.RefreshView();
                    UpdateWindowTitle();
                }
                else
                {
                    MessageBox.Show("Failed to create project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Opens a project from the specified path.
        /// </summary>
        public void OpenProjectPath(string path)
        {
            if (_projectManager != null && _projectManager.OpenProject(path))
            {
                _projectView?.RefreshView();
                UpdateWindowTitle();
            }
            else
            {
                MessageBox.Show("Failed to open project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Connects menu actions to event handlers.
        /// </summary>
        private void ConnectMenuActions()
        {
            // File menu
            actionNewProject.Click += (s, e) => NewProject();
            actionOpenProject.Click += (s, e) => OpenProject();
            actionCloseProject.Click += (s, e) => CloseProject();
            actionSave.Click += (s, e) => SaveProject();
            actionSaveAs.Click += (s, e) => SaveProjectAs();
            actionSaveAll.Click += (s, e) => SaveAll();
            actionCloseEditor.Click += (s, e) => CloseCurrentEditorTab();
            actionCloseAllEditors.Click += (s, e) => CloseAllEditorTabs();
            menuFile.DropDownOpening += (s, e) => UpdateEditorCloseMenuState();
            actionRename.Click += (s, e) => RenameProject();
            actionImport.Click += (s, e) => ImportProject();
            actionExport.Click += (s, e) => ExportProject();
            actionExit.Click += (s, e) => Close();

            // Edit menu
            actionUndo.Click += (s, e) => Undo();
            actionRedo.Click += (s, e) => Redo();
            actionCut.Click += (s, e) => Cut();
            actionCopy.Click += (s, e) => Copy();
            actionPaste.Click += (s, e) => Paste();
            actionDelete.Click += (s, e) => Delete();

            // Build menu
            actionBuildProject.Click += (s, e) => BuildProject();
            actionRebuildProject.Click += (s, e) => RebuildProject();
            actionCleanProject.Click += (s, e) => CleanProject();
            actionSelectActiveProject.Click += (s, e) => SelectActiveProject();
            actionBuildSettings.Click += (s, e) => ShowBuildSettings();

            // Download menu
            actionDownloadToDevice.Click += (s, e) => DeployToDevice();

            // Upload menu
            actionUploadFromDevice.Click += (s, e) => UploadFromDevice();

            // Simulate menu
            actionStartSimulation.Click += (s, e) => StartSimulation();
            actionPauseSimulation.Click += (s, e) => PauseSimulation();
            actionStopSimulation.Click += (s, e) => StopSimulation();
            actionSimulationSettings.Click += (s, e) => ShowSimulationSettings();

            // Tools menu
            actionOptions.Click += (s, e) => ShowOptions();
            actionCustomize.Click += (s, e) => ShowCustomize();
            actionExternalTools.Click += (s, e) => ShowExternalTools();
            actionPackageManager.Click += (s, e) => ShowPackageManager();

            // Help menu
            actionDocumentation.Click += (s, e) => OpenDocumentation();
            actionAbout.Click += (s, e) => OpenAbout();

            // View menu
            actionProjectExplorer.CheckedChanged += (s, e) => ToggleProjectExplorer();
            actionProperties.CheckedChanged += (s, e) => ToggleProperties();
            actionOutput.CheckedChanged += (s, e) => ToggleOutput();
            actionComponentPalette.CheckedChanged += (s, e) => ToggleComponentPalette();
            actionZoomIn.Click += (s, e) => ZoomIn();
            actionZoomOut.Click += (s, e) => ZoomOut();
            actionResetZoom.Click += (s, e) => ResetZoom();
        }

        private void OpenProject()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Open Project",
                Filter = "SCADA Projects (*.isc)|*.isc",
                InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string projectPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
                OpenProjectPath(projectPath);
            }
        }

        private void CloseProject()
        {
            if (_projectManager == null) return;
            CloseAllEditorTabs();
            _projectManager.CloseProject();
            SetActiveScadaProject(null); // Clear active project when closing
            _projectView?.RefreshView();
            UpdateWindowTitle();
        }

        private void CloseAllEditorTabs()
        {
            while (editorTabs.TabPages.Count > 0)
            {
                CloseTab(0);
            }
        }

        private void SaveProject()
        {
            if (_projectManager != null)
            {
                if (_projectManager.SaveProject())
                {
                    statusStrip.Items.Clear();
                    statusStrip.Items.Add("Project saved successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to save project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveProjectAs()
        {
            if (_projectManager?.GetCurrentProject() == null)
            {
                MessageBox.Show("No project is currently open.", "Save As", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var currentProject = _projectManager.GetCurrentProject();
            var currentProjectPath = currentProject.Path;
            var currentProjectDir = Path.GetDirectoryName(currentProjectPath) ?? string.Empty;

            using var dialog = new SaveFileDialog
            {
                Title = "Save Project As",
                Filter = "SCADA Projects (*.isc)|*.isc",
                FileName = currentProject.Name + ".isc",
                InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string newProjectFilePath = dialog.FileName;
                    string newProjectDir = Path.GetDirectoryName(newProjectFilePath) ?? string.Empty;
                    string newProjectName = Path.GetFileNameWithoutExtension(newProjectFilePath);

                    // If saving to a different directory, copy the entire project directory
                    if (!string.Equals(currentProjectDir, newProjectDir, StringComparison.OrdinalIgnoreCase))
                    {
                        // Create new project directory if it doesn't exist
                        if (!Directory.Exists(newProjectDir))
                        {
                            Directory.CreateDirectory(newProjectDir);
                        }

                        // Copy all files and subdirectories from current project to new location
                        if (Directory.Exists(currentProjectDir))
                        {
                            CopyDirectory(currentProjectDir, newProjectDir, true);
                        }
                    }

                    // Update project name and path
                    currentProject.Name = newProjectName;
                    currentProject.Path = newProjectFilePath;

                    // Save the project with new path
                    if (_projectManager.SaveProject())
                    {
                        // Update recent projects
                        ProjectDirectory.SetLastOpenedProject(newProjectDir);
                        statusStrip.Items.Clear();
                        statusStrip.Items.Add($"Project saved as '{newProjectName}'.");
                        UpdateWindowTitle();
                    }
                    else
                    {
                        MessageBox.Show("Failed to save project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Recursively copies a directory and its contents.
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy files
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy subdirectories
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDir, subdir.Name);
                    CopyDirectory(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private void RenameProject()
        {
            if (_projectManager?.GetCurrentProject() == null)
                return;

            var project = _projectManager.GetCurrentProject();
            // TODO: Show rename dialog
            MessageBox.Show($"Rename project '{project.Name}' - TODO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportProject()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Project",
                Filter = "SCADA Projects (*.isc)|*.isc|All Files (*.*)|*.*",
                InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // TODO: Implement import logic
                MessageBox.Show($"Import project from {dialog.FileName} - TODO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportProject()
        {
            if (_projectManager?.GetCurrentProject() == null)
            {
                MessageBox.Show("No project is currently open.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export Project",
                Filter = "SCADA Projects (*.isc)|*.isc|Archive (*.zip)|*.zip",
                FileName = _projectManager.GetCurrentProject().Name,
                InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // TODO: Implement export logic
                MessageBox.Show($"Export project to {dialog.FileName} - TODO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Undo()
        {
            // TODO: Implement undo for active editor
            var activeTab = editorTabs.SelectedTab;
            if (activeTab != null)
            {
                // Check if editor supports undo
            }
        }

        private void Redo()
        {
            // TODO: Implement redo for active editor
        }

        private void Cut()
        {
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0)
            {
                if (activeTab.Controls[0] is ScreenEditor screenEditor)
                {
                    screenEditor.CutSelectedComponents();
                }
                else
                {
                    var focusedControl = ActiveControl;
                    if (focusedControl is TextBox textBox)
                    {
                        textBox.Cut();
                    }
                }
            }
        }

        private void Copy()
        {
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0)
            {
                if (activeTab.Controls[0] is ScreenEditor screenEditor)
                {
                    screenEditor.CopySelectedComponents();
                }
                else
                {
                    var focusedControl = ActiveControl;
                    if (focusedControl is TextBox textBox)
                    {
                        textBox.Copy();
                    }
                }
            }
        }

        private void Paste()
        {
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0)
            {
                if (activeTab.Controls[0] is ScreenEditor screenEditor)
                {
                    screenEditor.PasteComponents();
                }
                else
                {
                    var focusedControl = ActiveControl;
                    if (focusedControl is TextBox textBox)
                    {
                        textBox.Paste();
                    }
                }
            }
        }

        private void Delete()
        {
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0)
            {
                if (activeTab.Controls[0] is ScreenEditor screenEditor)
                {
                    screenEditor.DeleteSelectedComponents();
                }
                else
                {
                    var focusedControl = ActiveControl;
                    if (focusedControl is TextBox textBox && textBox.SelectionLength > 0)
                    {
                        textBox.SelectedText = "";
                    }
                }
            }
        }

        private void ZoomIn()
        {
            // Zoom in active screen editor
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0 && activeTab.Controls[0] is ScreenEditor screenEditor)
            {
                screenEditor.ZoomIn();
            }
        }

        private void ZoomOut()
        {
            // Zoom out active screen editor
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0 && activeTab.Controls[0] is ScreenEditor screenEditor)
            {
                screenEditor.ZoomOut();
            }
        }

        private void ResetZoom()
        {
            // Reset zoom in active screen editor
            var activeTab = editorTabs.SelectedTab;
            if (activeTab?.Controls.Count > 0 && activeTab.Controls[0] is ScreenEditor screenEditor)
            {
                screenEditor.ResetZoom();
            }
        }

        private void SelectActiveProject()
        {
            var selected = SelectScadaProject("Select Active Project", "Choose SCADA project to set as active:");
            if (!string.IsNullOrEmpty(selected))
            {
                SetActiveScadaProject(selected);
                MessageBox.Show($"Active project set to: {selected}\n\nThis project will be used automatically for Build, Rebuild, Clean, and Deploy operations.",
                    "Active Project Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void RebuildProject()
        {
            // Use active project if set, otherwise ask once
            string? selectedScadaName = GetActiveScadaProjectName();
            
            if (string.IsNullOrEmpty(selectedScadaName))
            {
                selectedScadaName = SelectScadaProject("Rebuild Project", "Choose SCADA project to rebuild:");
                if (string.IsNullOrEmpty(selectedScadaName))
                    return;
            }
            
            // Perform clean and build without asking again
            if (PerformClean(selectedScadaName) && PerformBuild(selectedScadaName))
            {
                MessageBox.Show($"SCADA project '{selectedScadaName}' rebuilt successfully.",
                    "Rebuild Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowBuildSettings()
        {
            MessageBox.Show("Build settings dialog - TODO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveAll()
        {
            SaveProject();
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Controls.Count == 0) continue;
                var widget = tab.Controls[0];
                if (widget is Designer.Modules.ScriptEditor.ScriptEditor scriptEditor && scriptEditor.IsModified)
                    scriptEditor.SaveScript();
                else if (widget is Designer.Modules.ScreenEditor.ScreenEditor screenEditor && screenEditor.IsModified)
                    screenEditor.SaveScreen();
                else if (widget is Designer.Modules.Communication.CommunicationModuleEditor commEditor && commEditor.IsModified)
                    SaveCommunicationModuleFromEditor(commEditor, tab);
                else if (widget is Designer.Modules.TagEngine.TagTableEditor tagEditor && tagEditor.IsModified)
                    SaveTagTableFromEditor(tagEditor, tab);
                else if (widget is Designer.Modules.Historian.HistorianEditor historianEditor && historianEditor.IsModified)
                    SaveHistorianFromEditor(historianEditor, tab);
                else if (widget is Designer.Modules.Alarms.AlarmsEditor alarmsEditor && alarmsEditor.IsModified)
                    SaveAlarmsFromEditor(alarmsEditor, tab);
                else if (widget is Designer.Modules.Scheduler.ScheduleEditor scheduleEditor && scheduleEditor.IsModified)
                    SaveSchedulesFromEditor(scheduleEditor, tab);
                else if (widget is Designer.Modules.Security.SecurityEditor securityEditor && securityEditor.IsModified)
                    SaveSecurityFromEditor(securityEditor, tab);
                else if (widget is Designer.Modules.MachineLearning.MachineLearningEditor mlEditor && mlEditor.IsModified)
                    SaveMachineLearningFromEditor(mlEditor, tab);
            }
        }

        private void SaveCommunicationModuleFromEditor(Designer.Modules.Communication.CommunicationModuleEditor editor, TabPage tab)
        {
            var modules = editor.GetCommunicationModules();
            if (modules == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Communication - ", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
                _projectManager.UpdateCommunicationModules(scadaName, new List<object> { modules });
        }

        private void SaveHistorianFromEditor(Designer.Modules.Historian.HistorianEditor editor, TabPage tab)
        {
            var historian = editor.GetHistorian();
            if (historian == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Historian - ", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
            {
                if (_projectManager.SaveHistorian(scadaName, historian))
                {
                    editor.ResetModified();
                }
            }
        }

        private void SaveAlarmsFromEditor(Designer.Modules.Alarms.AlarmsEditor editor, TabPage tab)
        {
            var alarms = editor.GetAlarms();
            if (alarms == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Alarms (", "").Replace(")", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
            {
                if (_projectManager.SaveAlarms(scadaName, alarms))
                {
                    editor.ResetModified();
                }
            }
        }

        private void SaveSchedulesFromEditor(Designer.Modules.Scheduler.ScheduleEditor editor, TabPage tab)
        {
            var schedules = editor.GetSchedules();
            if (schedules == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Schedules - ", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
            {
                if (_projectManager.SaveSchedules(scadaName, schedules))
                {
                    editor.ResetModified();
                }
            }
        }

        private void SaveSecurityFromEditor(Designer.Modules.Security.SecurityEditor editor, TabPage tab)
        {
            var security = editor.GetSecurity();
            if (security == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Security - ", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
            {
                if (_projectManager.SaveSecurity(scadaName, security))
                {
                    editor.ResetModified();
                }
            }
        }

        private void SaveMachineLearningFromEditor(Designer.Modules.MachineLearning.MachineLearningEditor editor, TabPage tab)
        {
            var ml = editor.GetMachineLearning();
            if (ml == null || _projectManager == null) return;
            var scadaName = tab.Text.Replace("Machine Learning - ", "").Trim();
            if (!string.IsNullOrEmpty(scadaName))
            {
                if (_projectManager.SaveMachineLearning(scadaName, ml))
                {
                    editor.ResetModified();
                }
            }
        }

        private void SaveTagTableFromEditor(Designer.Modules.TagEngine.TagTableEditor tagEditor, TabPage tab)
        {
            // Use the SaveAll method from TagTableEditor which handles all tables
            bool saved = tagEditor.SaveAll();
            
            if (!saved)
            {
                // If SaveAll failed, try to save individual tables with file path resolution
                var tables = tagEditor.GetTagTables();
                foreach (var table in tables)
                {
                    if (string.IsNullOrEmpty(table.FilePath))
                    {
                        // Try to determine file path from SCADA project
                        string tabText = tab.Text;
                        string scadaName = "";
                        
                        if (tabText.Contains(" - "))
                        {
                            scadaName = tabText.Split(new[] { " - " }, StringSplitOptions.None)[0];
                        }
                        else if (tabText.Contains("All Tags ("))
                        {
                            scadaName = tabText.Replace("All Tags (", "").Replace(")", "");
                        }
                        
                        if (!string.IsNullOrEmpty(scadaName) && _projectManager != null)
                        {
                            var scada = _projectManager.FindScadaProject(scadaName);
                            if (scada != null)
                            {
                                string tagsDir = scada.Paths.TagsPath;
                                if (!System.IO.Directory.Exists(tagsDir))
                                {
                                    System.IO.Directory.CreateDirectory(tagsDir);
                                }
                                
                                string filePath = System.IO.Path.Combine(tagsDir, table.Name + ".json");
                                table.FilePath = filePath;
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(table.FilePath))
                    {
                        bool tableSaved = table.SaveToFile(table.FilePath);
                        if (!tableSaved)
                        {
                            MessageBox.Show($"Failed to save tag table '{table.Name}' to {table.FilePath}", 
                                "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void BuildProject()
        {
            // Use active project if set, otherwise ask
            string? selectedScadaName = GetActiveScadaProjectName();
            
            if (string.IsNullOrEmpty(selectedScadaName))
            {
                selectedScadaName = SelectScadaProject("Build Project", "Choose SCADA project to build:");
                if (string.IsNullOrEmpty(selectedScadaName))
                    return;
            }
            
            PerformBuild(selectedScadaName);
        }
        
        private bool PerformBuild(string selectedScadaName)
        {
            if (_projectManager?.GetCurrentProject() == null)
            {
                MessageBox.Show("No project is currently open.", "Build Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var scadaProject = _projectManager.FindScadaProject(selectedScadaName);
            if (scadaProject == null)
            {
                MessageBox.Show($"SCADA project '{selectedScadaName}' not found.", "Build Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Save project before building
            SaveProject();

            // Increment version
            _projectManager.IncrementScadaProjectVersion(selectedScadaName);
            _projectManager.SaveProject();

            // Refresh to get updated version
            scadaProject = _projectManager.FindScadaProject(selectedScadaName);

            // Switch to Output tab
            bottomTabWidget.SelectedIndex = 0;

            // Set project and compile
            _compilerModule?.SetProject(scadaProject!);
            if (_compilerModule?.CompileProject() == true)
            {
                _projectView?.RefreshView();
                MessageBox.Show($"SCADA project '{scadaProject!.Name}' built successfully.\nOutput file: {scadaProject.Name}.iscr",
                    "Build Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            else
            {
                MessageBox.Show($"Failed to build SCADA project '{scadaProject!.Name}'.", "Build Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void CleanProject()
        {
            // Use active project if set, otherwise ask
            string? selectedScadaName = GetActiveScadaProjectName();
            
            if (string.IsNullOrEmpty(selectedScadaName))
            {
                selectedScadaName = SelectScadaProject("Clean Project", "Choose SCADA project to clean:");
                if (string.IsNullOrEmpty(selectedScadaName))
                    return;
            }
            
            PerformClean(selectedScadaName);
        }
        
        private bool PerformClean(string selectedScadaName)
        {
            if (_projectManager?.GetCurrentProject() == null)
            {
                MessageBox.Show("No project is currently open.", "Clean Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var scadaProject = _projectManager.FindScadaProject(selectedScadaName);
            if (scadaProject == null)
            {
                MessageBox.Show($"SCADA project '{selectedScadaName}' not found.", "Clean Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            bottomTabWidget.SelectedIndex = 0;
            _compilerModule?.SetProject(scadaProject);
            if (_compilerModule?.CleanProject() == true)
            {
                MessageBox.Show($"SCADA project '{scadaProject.Name}' cleaned successfully.", "Clean Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            else
            {
                MessageBox.Show($"Failed to clean SCADA project '{scadaProject.Name}'.", "Clean Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void DeployToDevice()
        {
            if (_projectManager == null)
            {
                MessageBox.Show("No project is currently open.", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Pass active project name to DeployDialog
            string? activeProjectName = GetActiveScadaProjectName();
            using (var dialog = new DeployDialog(_projectManager, _compilerModule, activeProjectName))
            {
                dialog.ShowDialog();
                // Update active project if user selected one in the dialog
                if (!string.IsNullOrEmpty(dialog.SelectedScadaProjectName))
                {
                    SetActiveScadaProject(dialog.SelectedScadaProjectName);
                }
            }
        }

        private void UploadFromDevice()
        {
            using (var dialog = new UploadDialog())
            {
                dialog.UploadRequested += (s, e) =>
                {
                    // Handle upload request
                    MessageBox.Show($"Upload from {e.SourceDevice} to {e.TargetDevice} completed.", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                dialog.ShowDialog();
            }
        }

        private void StartSimulation()
        {
            if (_simulatorModule == null || _projectManager == null)
                return;

            if (_projectManager.GetCurrentProject() == null)
            {
                MessageBox.Show(this, "No project is currently open.", "Simulation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string result = _simulatorModule.ShowProjectSelectionDialog(this);
            if (string.IsNullOrEmpty(result))
                return;

            string projectPath = result;
            if (result.StartsWith("BUILD_REQUIRED:", StringComparison.OrdinalIgnoreCase))
            {
                string rest = result.Substring(14);
                int pipe = rest.IndexOf('|');
                string scadaName = pipe > 0 ? rest.Substring(0, pipe) : rest;
                projectPath = pipe > 0 ? rest.Substring(pipe + 1) : rest;

                bool doBuild = _simulatorModule.GetAutoBuildBeforeSimulate();
                if (!doBuild)
                {
                    var choice = MessageBox.Show(this,
                        "The selected project has not been built yet. Do you want to build it now?",
                        "Project Not Built",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    if (choice == DialogResult.Cancel)
                        return;
                    doBuild = (choice == DialogResult.Yes);
                }

                if (doBuild && !PerformBuild(scadaName))
                {
                    MessageBox.Show(this, "Failed to build project. Cannot start simulation.", "Simulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!doBuild)
                    return;
            }

            if (_simulatorModule.StartSimulation(projectPath))
            {
                actionStartSimulation.Enabled = false;
                actionPauseSimulation.Enabled = true;
                actionStopSimulation.Enabled = true;
                statusStrip.Items.Clear();
                statusStrip.Items.Add($"Simulation running: {Path.GetFileName(Path.GetDirectoryName(projectPath)) ?? projectPath}");
            }
        }

        private void UpdateSimulationStoppedUI()
        {
            actionStartSimulation.Enabled = true;
            actionPauseSimulation.Enabled = false;
            actionPauseSimulation.Text = "Pause Simulation";
            actionStopSimulation.Enabled = false;
            statusStrip.Items.Clear();
            statusStrip.Items.Add("Simulation stopped");
        }

        private void PauseSimulation()
        {
            if (_simulatorModule == null)
                return;

            if (_simulatorModule.IsPaused)
            {
                _simulatorModule.ResumeSimulation();
                actionPauseSimulation.Text = "Pause Simulation";
            }
            else
            {
                _simulatorModule.PauseSimulation();
                actionPauseSimulation.Text = "Resume Simulation";
            }
        }

        private void StopSimulation()
        {
            if (_simulatorModule == null)
                return;

            _simulatorModule.StopSimulation();
            UpdateSimulationStoppedUI();
        }

        private void ShowSimulationSettings()
        {
            _simulatorModule?.ShowSettingsDialog(this);
        }

        private void ShowOptions()
        {
            using (var dialog = new OptionsDialog())
            {
                dialog.ThemeChanged += (s, theme) =>
                {
                    // Apply theme (can be implemented later)
                    statusStrip.Items.Clear();
                    statusStrip.Items.Add($"Theme changed to: {theme}");
                };
                dialog.ShowDialog();
            }
        }

        private void ShowCustomize()
        {
            using (var dialog = new Designer.Modules.Tools.CustomizeDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void ShowExternalTools()
        {
            using (var dialog = new Designer.Modules.Tools.ExternalToolsDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void ShowPackageManager()
        {
            using (var dialog = new Designer.Modules.Tools.PackageManagerDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void OpenDocumentation()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.indusysltd.com/accutrack",
                UseShellExecute = true
            });
        }

        private void OpenAbout()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.indusysltd.com/accutrack",
                UseShellExecute = true
            });
        }

        private void ToggleProjectExplorer()
        {
            projectWidget.Visible = actionProjectExplorer.Checked;
        }

        private void ToggleProperties()
        {
            bottomTabWidget.TabPages[2].Visible = actionProperties.Checked;
        }

        private void ToggleOutput()
        {
            bottomWidget.Visible = actionOutput.Checked;
        }

        private void ToggleComponentPalette()
        {
            componentsPanel.Visible = actionComponentPalette.Checked;
        }

        private void UpdateWindowTitle()
        {
            if (_projectManager?.GetCurrentProject() != null)
            {
                Text = $"AccuTrack Designer - {_projectManager.GetCurrentProject().Name}";
            }
            else
            {
                Text = "AccuTrack Designer";
            }
        }

        /// <summary>
        /// Closes the currently selected editor tab (File > Close or Ctrl+F4).
        /// </summary>
        private void CloseCurrentEditorTab()
        {
            if (editorTabs.TabCount == 0)
                return;
            int index = editorTabs.SelectedIndex;
            if (index >= 0)
                CloseTab(index);
        }

        /// <summary>
        /// Closes the tab that was right-clicked, or the selected tab if none was right-clicked.
        /// </summary>
        private void CloseEditorTabFromContext()
        {
            int index = _rightClickedTabIndex >= 0 && _rightClickedTabIndex < editorTabs.TabCount
                ? _rightClickedTabIndex
                : editorTabs.SelectedIndex;
            if (index >= 0)
                CloseTab(index);
        }

        /// <summary>
        /// Updates enabled state of Close / Close All editor menu items.
        /// </summary>
        private void UpdateEditorCloseMenuState()
        {
            bool hasTabs = editorTabs.TabCount > 0;
            actionCloseEditor.Enabled = hasTabs;
            actionCloseAllEditors.Enabled = hasTabs;
        }

        /// <summary>
        /// Closes a tab with save prompt if modified.
        /// </summary>
        private void CloseTab(int index)
        {
            if (index < 0 || index >= editorTabs.TabPages.Count)
                return;

            var tab = editorTabs.TabPages[index];
            var widget = tab.Controls.Count > 0 ? tab.Controls[0] : null;

            // Check if editor is modified and prompt to save
            bool shouldClose = true;

            if (widget is TagTableEditor tagEditor && tagEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveTagTableFromEditor(tagEditor, tab);
                    shouldClose = true;
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is ScriptEditor scriptEditor && scriptEditor.IsModified)
            {
                if (!scriptEditor.MaybeSave())
                {
                    shouldClose = false;
                }
            }
            else if (widget is ScreenEditor screenEditor && screenEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    screenEditor.SaveScreen();
                    shouldClose = true;
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is ScheduleEditor scheduleEditor && scheduleEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var schedules = scheduleEditor.GetSchedules();
                    if (schedules != null && _projectManager != null)
                    {
                        // Extract SCADA name from tab text (format: "Schedules - SCADAName")
                        var tabText = tab.Text;
                        var scadaName = tabText.Replace("Schedules - ", "").Trim();
                        _projectManager.SaveSchedules(scadaName, schedules);
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is Designer.Modules.Alarms.AlarmsEditor alarmsEditor && alarmsEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveAlarmsFromEditor(alarmsEditor, tab);
                    shouldClose = true;
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is HistorianEditor historianEditor && historianEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var historian = historianEditor.GetHistorian();
                    if (historian != null && _projectManager != null)
                    {
                        var tabText = tab.Text;
                        var scadaName = tabText.Replace("Historian - ", "").Trim();
                        _projectManager.SaveHistorian(scadaName, historian);
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is SecurityEditor securityEditor && securityEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var security = securityEditor.GetSecurity();
                    if (security != null && _projectManager != null)
                    {
                        var tabText = tab.Text;
                        var scadaName = tabText.Replace("Security - ", "").Trim();
                        _projectManager.SaveSecurity(scadaName, security);
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is MachineLearningEditor mlEditor && mlEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var ml = mlEditor.GetMachineLearning();
                    if (ml != null && _projectManager != null)
                    {
                        var tabText = tab.Text;
                        var scadaName = tabText.Replace("Machine Learning - ", "").Trim();
                        _projectManager.SaveMachineLearning(scadaName, ml);
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }
            else if (widget is CommunicationModuleEditor commEditor && commEditor.IsModified)
            {
                var result = MessageBox.Show(
                    $"Save changes to '{tab.Text}'?",
                    "Save Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var modules = commEditor.GetCommunicationModules();
                    if (modules != null && _projectManager != null)
                    {
                        var tabText = tab.Text;
                        var scadaName = tabText.Replace("Communication - ", "").Trim();
                        _projectManager.UpdateCommunicationModules(scadaName, new List<object> { modules });
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    shouldClose = false;
                }
            }

            if (shouldClose)
            {
                editorTabs.TabPages.RemoveAt(index);
                UpdateEditorCloseMenuState();
            }
        }

        // Event handlers for project view
        private void OnDeviceNetworkOpen()
        {
            if (_projectManager?.GetCurrentProject() == null)
                return;

            var deviceNetwork = _projectManager.GetDeviceNetwork();
            if (deviceNetwork == null)
                return;

            // Check if device network editor is already open
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Controls[0] is DeviceNetworkEditor existingEditor)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create new device network editor
            var editor = new DeviceNetworkEditor();
            editor.SetDeviceNetwork(deviceNetwork);
            editor.SetProjectManager(_projectManager);

            var tabPage = new TabPage("Device Network");
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnSettingsOpen()
        {
            if (_projectManager?.GetCurrentProject() == null)
                return;

            // Check if settings editor is already open
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Controls[0] is SettingsEditor existingEditor)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create new settings editor
            var editor = new SettingsEditor();
            editor.SetProjectManager(_projectManager);

            var tabPage = new TabPage("Project Settings");
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnCommunicationModuleOpen(string scadaName, object? module)
        {
            string tabName = $"Communication - {scadaName}";

            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            var editor = new CommunicationModuleEditor();
            
            CommunicationModules? modulesObj = null;
            if (module is CommunicationModules commModules)
            {
                modulesObj = commModules;
            }
            else if (module is string modulesJson)
            {
                try
                {
                    var json = JObject.Parse(modulesJson);
                    modulesObj = CommunicationModules.FromJson(json);
                }
                catch
                {
                    modulesObj = null;
                }
            }
            
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (modulesObj == null && scadaProject != null)
            {
                var commFile = System.IO.Path.Combine(scadaProject.Paths.CommunicationsPath, "communication_modules.json");
                if (System.IO.File.Exists(commFile))
                {
                    try
                    {
                        var json = JObject.Parse(System.IO.File.ReadAllText(commFile));
                        modulesObj = CommunicationModules.FromJson(json);
                    }
                    catch
                    {
                        modulesObj = null;
                    }
                }
            }
            
            editor.SetCommunicationModules(modulesObj ?? new CommunicationModules());
            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }
            if (_projectManager != null)
            {
                editor.SetProjectManager(_projectManager);
            }
            editor.SetScadaName(scadaName);

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnAlarmsOpen(string scadaName, object? alarms)
        {
            // Check if alarms editor is already open
            string tabName = $"Alarms ({scadaName})";
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Get all tags for this SCADA project
            var tagTables = _projectManager?.GetTagTables(scadaName).OfType<TagTable>().ToList() ?? new List<TagTable>();
            var allTags = new List<Tag>();
            foreach (var table in tagTables)
            {
                allTags.AddRange(table.GetTags());
            }

            // Create alarms editor
            var editor = new AlarmsEditor(allTags);
            
            if (alarms is Alarms alarmsObj)
            {
                editor.SetAlarms(alarmsObj);
            }
            else
            {
                editor.SetAlarms(new Alarms());
            }

            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnSchedulesOpen(string scadaName, object? schedules)
        {
            string tabName = $"Schedules - {scadaName}";

            // Check if tab is already open
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Get all scripts for this SCADA project
            var scripts = _projectManager?.GetScripts(scadaName).OfType<LuaScript>().ToList() ?? new List<LuaScript>();
            var scriptNames = scripts.Select(s => s.Name).ToList();

            // Create schedules editor
            var editor = new ScheduleEditor(scriptNames);
            
            Schedules? schedulesObj = null;
            if (schedules is Schedules sched)
            {
                schedulesObj = sched;
            }
            else if (schedules is string schedulesJson)
            {
                // Load from JSON string if ProjectManager returned file contents
                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(schedulesJson);
                    schedulesObj = Schedules.FromJson(json);
                }
                catch
                {
                    schedulesObj = null;
                }
            }
            
            // Get SCADA project for loading schedules and setting editor
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            
            // If still null, try loading from file directly
            if (schedulesObj == null && scadaProject != null)
            {
                var schedulesFile = System.IO.Path.Combine(scadaProject.Paths.SchedulesPath, "schedules.json");
                if (System.IO.File.Exists(schedulesFile))
                {
                    try
                    {
                        var json = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(schedulesFile));
                        schedulesObj = Schedules.FromJson(json);
                    }
                    catch
                    {
                        schedulesObj = null;
                    }
                }
            }
            
            editor.SetSchedules(schedulesObj ?? new Schedules());

            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnHistorianOpen(string scadaName, object? historian)
        {
            string tabName = $"Historian - {scadaName}";

            // Check if tab is already open
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Get all tags for this SCADA project
            var tagTables = _projectManager?.GetTagTables(scadaName).OfType<TagTable>().ToList() ?? new List<TagTable>();
            var allTags = new List<Tag>();
            foreach (var table in tagTables)
            {
                allTags.AddRange(table.GetTags());
            }

            // Create historian editor
            var editor = new HistorianEditor(allTags);
            
            Historian? historianObj = null;
            if (historian is Historian hist)
            {
                historianObj = hist;
            }
            else if (historian is string historianJson)
            {
                try
                {
                    var json = JObject.Parse(historianJson);
                    historianObj = Historian.FromJson(json);
                }
                catch
                {
                    historianObj = null;
                }
            }
            
            // If still null, try loading from file directly
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (historianObj == null && scadaProject != null)
            {
                // Try json folder first (for runtime compatibility)
                var jsonPath = System.IO.Path.Combine(scadaProject.Path, "json");
                var historianFile = System.IO.Path.Combine(jsonPath, "historian.json");
                
                if (!System.IO.File.Exists(historianFile))
                {
                    // Fallback to historian folder (legacy)
                    historianFile = System.IO.Path.Combine(scadaProject.Paths.HistorianPath, "historian.json");
                }
                
                if (System.IO.File.Exists(historianFile))
                {
                    try
                    {
                        var json = JObject.Parse(System.IO.File.ReadAllText(historianFile));
                        historianObj = Historian.FromJson(json);
                    }
                    catch
                    {
                        historianObj = null;
                    }
                }
            }
            
            editor.SetHistorian(historianObj ?? new Historian());
            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }
            
            // Set SCADA name and ProjectManager for tag table access
            editor.SetScadaName(scadaName);
            if (_projectManager != null)
            {
                editor.SetProjectManager(_projectManager);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnSecurityOpen(string scadaName, object? security)
        {
            string tabName = $"Security - {scadaName}";

            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            var editor = new SecurityEditor();
            
            Security? securityObj = null;
            if (security is Security sec)
            {
                securityObj = sec;
            }
            else if (security is string securityJson)
            {
                try
                {
                    var json = JObject.Parse(securityJson);
                    securityObj = Security.FromJson(json);
                }
                catch
                {
                    securityObj = null;
                }
            }
            
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (securityObj == null && scadaProject != null)
            {
                var securityFile = System.IO.Path.Combine(scadaProject.Paths.SecurityPath, "security.json");
                if (System.IO.File.Exists(securityFile))
                {
                    try
                    {
                        var json = JObject.Parse(System.IO.File.ReadAllText(securityFile));
                        securityObj = Security.FromJson(json);
                    }
                    catch
                    {
                        securityObj = null;
                    }
                }
            }
            
            editor.SetSecurity(securityObj ?? new Security());
            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnMachineLearningOpen(string scadaName, object? ml)
        {
            string tabName = $"Machine Learning - {scadaName}";

            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Get available tags
            var tagTables = _projectManager?.GetTagTables(scadaName).OfType<TagTable>().ToList() ?? new List<TagTable>();
            var tagNames = new List<string>();
            foreach (var table in tagTables)
            {
                tagNames.AddRange(table.GetTags().Select(t => t.Name));
            }

            var editor = new MachineLearningEditor(tagNames);
            
            MachineLearning? mlObj = null;
            if (ml is MachineLearning mlInst)
            {
                mlObj = mlInst;
            }
            else if (ml is string mlJson)
            {
                try
                {
                    var json = JObject.Parse(mlJson);
                    mlObj = MachineLearning.FromJson(json);
                }
                catch
                {
                    mlObj = null;
                }
            }
            
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (mlObj == null && scadaProject != null)
            {
                var mlFile = System.IO.Path.Combine(scadaProject.Paths.MachineLearningPath, "machine_learning.json");
                if (System.IO.File.Exists(mlFile))
                {
                    try
                    {
                        var json = JObject.Parse(System.IO.File.ReadAllText(mlFile));
                        mlObj = MachineLearning.FromJson(json);
                    }
                    catch
                    {
                        mlObj = null;
                    }
                }
            }
            
            editor.SetMachineLearning(mlObj ?? new MachineLearning());
            if (scadaProject != null)
            {
                editor.SetScadaProject(scadaProject);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnTagTableOpen(object tagTable, string scadaName)
        {
            if (tagTable is not TagTable table)
                return;

            // Check if tag table editor is already open
            string tabName = $"{scadaName} - {table.Name}";
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create tag table editor
            var editor = new TagTableEditor();
            editor.SetTagTable(table);

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnScreenOpen(object screen, string scadaName)
        {
            // Set active project when a screen is opened
            if (!string.IsNullOrEmpty(scadaName))
            {
                SetActiveScadaProject(scadaName);
            }
            
            // Use dynamic to handle ScreenTemplate without direct dependency
            if (screen == null)
                return;
            
            try
            {
                dynamic screenObj = screen;
                string screenName = screenObj.Name?.ToString() ?? "Unknown";
                var template = screen; // Pass as object, ScreenEditor will handle it

            // Check if screen editor is already open
            string tabName = $"{screenName} ({scadaName})";
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create screen editor
            var editor = new ScreenEditor();
            editor.SetScadaProjectName(scadaName);
            
            // Get SCADA project to retrieve resolution and set for event creation
            var scadaProject = _projectManager?.FindScadaProject(scadaName);
            if (scadaProject != null)
            {
                editor.SetScadaResolution(scadaProject.Resolution);
                editor.SetScadaProject(scadaProject); // Set project for event creation
            }
            
            // SetTemplate now accepts object
            editor.SetTemplate(template);
            
            // Connect selection changes to property editor
            if (_propertyEditor != null && _projectManager != null)
            {
                // Get current SCADA project's tag tables and screens
                var tagTables = _projectManager.GetTagTables(scadaName).OfType<TagTable>().ToList();
                var screens = _projectManager.GetScreens(scadaName);
                
                _propertyEditor.SetAvailableTagTables(tagTables);
                _propertyEditor.SetAvailableScreens(screens);
                
                _propertyEditor.SetScadaProject(scadaProject);
            }
            
            // Connect screen editor selection events to property editor (always connect, even if property editor setup failed)
            editor.SelectionChanged += (s, selectedComponent) =>
            {
                if (_propertyEditor != null)
                {
                    _propertyEditor.UpdateEditor(selectedComponent);
                }
            };
            
            // Connect double-click to open Properties tab
            editor.ComponentDoubleClicked += (s, component) =>
            {
                if (_propertyEditor != null && bottomTabWidget != null && bottomTabWidget.TabPages.Count >= 3)
                {
                    // Select the Properties tab
                    bottomTabWidget.SelectedTab = bottomTabWidget.TabPages[2]; // Properties tab is at index 2
                }
            };

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
            }
            catch
            {
                return; // Failed to process screen
            }
        }

        private void OnScreenCreated(object screen, string scadaName)
        {
            // Open the newly created screen in editor
            OnScreenOpen(screen, scadaName);
        }

        private void OnTagTableCreated(TagTable tagTable, string scadaName)
        {
            // Open the newly created tag table in editor
            OnTagTableOpen(tagTable, scadaName);
        }

        private void OnScriptCreated(LuaScript script, string scadaName)
        {
            // Open the newly created script in editor
            OnScriptOpen(script, scadaName);
        }

        private void OnScriptOpen(object script, string scadaName)
        {
            if (script is not LuaScript luaScript)
                return;

            // Check if script editor is already open
            string tabName = $"{luaScript.Name} ({scadaName})";
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create script editor
            var editor = new ScriptEditor();
            editor.SetScript(luaScript);

            // Update available tags
            if (_projectManager != null)
            {
                var tagTables = _projectManager.GetTagTables(scadaName);
                var tagNames = new List<string>();
                foreach (var table in tagTables.OfType<TagTable>())
                {
                    foreach (var tag in table.GetTags())
                    {
                        tagNames.Add(tag.Name);
                    }
                }
                editor.UpdateAvailableTags(tagNames);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }

        private void OnAllTagTablesOpen(string scadaName, List<object> tagTables)
        {
            // Check if tag table editor is already open
            string tabName = $"All Tags ({scadaName})";
            foreach (TabPage tab in editorTabs.TabPages)
            {
                if (tab.Text == tabName)
                {
                    editorTabs.SelectedTab = tab;
                    return;
                }
            }

            // Create tag table editor
            var editor = new TagTableEditor();
            
            // Convert object list to TagTable list
            var tagTableList = tagTables.OfType<TagTable>().ToList();
            if (tagTableList.Count == 0 && _projectManager != null)
            {
                // Try to get tag tables from project manager
                var tables = _projectManager.GetTagTables(scadaName);
                tagTableList = tables.OfType<TagTable>().ToList();
            }

            if (tagTableList.Count > 0)
            {
                editor.SetTagTables(tagTableList);
            }

            var tabPage = new TabPage(tabName);
            tabPage.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editorTabs.TabPages.Add(tabPage);
            editorTabs.SelectedTab = tabPage;
        }
    }
}

