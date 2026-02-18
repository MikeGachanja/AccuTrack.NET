namespace Designer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private Panel projectWidget;
        private Panel editorWidget;
        private Panel componentsWidget;
        private SplitContainer mainSplitContainer;
        private SplitContainer verticalSplitContainer;
        private SplitContainer horizontalSplitContainer;

        // Menu items
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuEdit;
        private ToolStripMenuItem menuView;
        private ToolStripMenuItem menuBuild;
        private ToolStripMenuItem menuDownload;
        private ToolStripMenuItem menuUpload;
        private ToolStripMenuItem menuSimulate;
        private ToolStripMenuItem menuTools;
        private ToolStripMenuItem menuHelp;

        // File menu items
        private ToolStripMenuItem actionNewProject;
        private ToolStripMenuItem actionOpenProject;
        private ToolStripMenuItem actionCloseProject;
        private ToolStripSeparator separator1;
        private ToolStripMenuItem actionSave;
        private ToolStripMenuItem actionSaveAs;
        private ToolStripMenuItem actionSaveAll;
        private ToolStripMenuItem actionCloseEditor;
        private ToolStripMenuItem actionCloseAllEditors;
        private ToolStripSeparator separator2;
        private ToolStripMenuItem actionRename;
        private ToolStripSeparator separator3;
        private ToolStripMenuItem actionImport;
        private ToolStripMenuItem actionExport;
        private ToolStripSeparator separator4;
        private ToolStripMenuItem actionArchive;
        private ToolStripMenuItem actionRestore;
        private ToolStripSeparator separator5;
        private ToolStripMenuItem actionRecentProjects;
        private ToolStripSeparator separator6;
        private ToolStripMenuItem actionExit;

        // Edit menu items
        private ToolStripMenuItem actionUndo;
        private ToolStripMenuItem actionRedo;
        private ToolStripSeparator separator7;
        private ToolStripMenuItem actionCut;
        private ToolStripMenuItem actionCopy;
        private ToolStripMenuItem actionPaste;
        private ToolStripMenuItem actionDelete;

        // View menu items
        private ToolStripMenuItem actionProjectExplorer;
        private ToolStripMenuItem actionProperties;
        private ToolStripMenuItem actionOutput;
        private ToolStripMenuItem actionComponentPalette;
        private ToolStripSeparator separator8;
        private ToolStripMenuItem actionZoomIn;
        private ToolStripMenuItem actionZoomOut;
        private ToolStripMenuItem actionResetZoom;

        // Build menu items
        private ToolStripMenuItem actionBuildProject;
        private ToolStripMenuItem actionRebuildProject;
        private ToolStripMenuItem actionCleanProject;
        private ToolStripSeparator separator9;
        private ToolStripMenuItem actionSelectActiveProject;
        private ToolStripSeparator separator12;
        private ToolStripMenuItem actionBuildSettings;

        // Download menu items
        private ToolStripMenuItem actionDownloadToDevice;
        private ToolStripMenuItem actionDownloadSettings;

        // Upload menu items
        private ToolStripMenuItem actionUploadFromDevice;
        private ToolStripMenuItem actionUploadSettings;

        // Simulate menu items
        private ToolStripMenuItem actionStartSimulation;
        private ToolStripMenuItem actionPauseSimulation;
        private ToolStripMenuItem actionStopSimulation;
        private ToolStripSeparator separator10;
        private ToolStripMenuItem actionSimulationSettings;

        // Tools menu items
        private ToolStripMenuItem actionOptions;
        private ToolStripMenuItem actionCustomize;
        private ToolStripSeparator separator11;
        private ToolStripMenuItem actionExternalTools;
        private ToolStripMenuItem actionPackageManager;

        // Help menu items
        private ToolStripMenuItem actionDocumentation;
        private ToolStripMenuItem actionAbout;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip = new MenuStrip();
            statusStrip = new StatusStrip();
            projectWidget = new Panel();
            editorWidget = new Panel();
            bottomWidget = new Panel();
            bottomTabWidget = new TabControl();
            outputTab = new TabPage();
            debugTab = new TabPage();
            propertiesTab = new TabPage();
            editorTabs = new TabControl();
            componentsPanel = new Panel();
            componentsWidget = componentsPanel; // Alias for MainForm.cs compatibility - set after componentsPanel is initialized
            mainSplitContainer = new SplitContainer();
            verticalSplitContainer = new SplitContainer();
            horizontalSplitContainer = new SplitContainer();
            editorWidget.SuspendLayout();
            bottomWidget.SuspendLayout();
            bottomTabWidget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(mainSplitContainer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(verticalSplitContainer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(horizontalSplitContainer)).BeginInit();
            mainSplitContainer.SuspendLayout();
            verticalSplitContainer.SuspendLayout();
            horizontalSplitContainer.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1165, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip";
            // 
            // statusStrip
            // 
            statusStrip.Location = new Point(0, 715);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1165, 22);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip";
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.FixedPanel = FixedPanel.Panel2;
            mainSplitContainer.Location = new Point(0, 24);
            mainSplitContainer.Name = "mainSplitContainer";
            mainSplitContainer.Orientation = Orientation.Horizontal;
            mainSplitContainer.Size = new Size(1165, 691);
            mainSplitContainer.SplitterDistance = 534;
            mainSplitContainer.SplitterWidth = 4;
            mainSplitContainer.TabIndex = 0;
            // 
            // verticalSplitContainer
            // 
            verticalSplitContainer.Dock = DockStyle.Fill;
            verticalSplitContainer.FixedPanel = FixedPanel.Panel1;
            verticalSplitContainer.Location = new Point(0, 0);
            verticalSplitContainer.Name = "verticalSplitContainer";
            verticalSplitContainer.Orientation = Orientation.Vertical;
            verticalSplitContainer.Size = new Size(1165, 534);
            verticalSplitContainer.SplitterDistance = 200;
            verticalSplitContainer.SplitterWidth = 4;
            verticalSplitContainer.TabIndex = 0;
            // 
            // horizontalSplitContainer
            // 
            horizontalSplitContainer.Dock = DockStyle.Fill;
            horizontalSplitContainer.FixedPanel = FixedPanel.Panel2;
            horizontalSplitContainer.Location = new Point(204, 0);
            horizontalSplitContainer.Name = "horizontalSplitContainer";
            horizontalSplitContainer.Orientation = Orientation.Vertical;
            horizontalSplitContainer.Size = new Size(961, 534);
            horizontalSplitContainer.SplitterDistance = 757;
            horizontalSplitContainer.SplitterWidth = 4;
            horizontalSplitContainer.TabIndex = 0;
            // 
            // projectWidget
            // 
            projectWidget.Dock = DockStyle.Fill;
            projectWidget.Location = new Point(0, 0);
            projectWidget.MaximumSize = new Size(250, 0);
            projectWidget.MinimumSize = new Size(160, 0);
            projectWidget.Name = "projectWidget";
            projectWidget.Size = new Size(200, 534);
            projectWidget.TabIndex = 0;
            // 
            // editorWidget
            // 
            editorWidget.Controls.Add(editorTabs);
            editorWidget.Dock = DockStyle.Fill;
            editorWidget.Location = new Point(0, 0);
            editorWidget.Name = "editorWidget";
            editorWidget.Size = new Size(765, 534);
            editorWidget.TabIndex = 0;
            // 
            // bottomWidget
            // 
            bottomWidget.Controls.Add(bottomTabWidget);
            bottomWidget.Dock = DockStyle.Fill;
            bottomWidget.Location = new Point(0, 0);
            bottomWidget.MaximumSize = new Size(0, 400);
            bottomWidget.MinimumSize = new Size(0, 120);
            bottomWidget.Name = "bottomWidget";
            bottomWidget.Size = new Size(1165, 157);
            bottomWidget.TabIndex = 3;
            // 
            // bottomTabWidget
            // 
            bottomTabWidget.Controls.Add(outputTab);
            bottomTabWidget.Controls.Add(debugTab);
            bottomTabWidget.Controls.Add(propertiesTab);
            bottomTabWidget.Dock = DockStyle.Fill;
            bottomTabWidget.Location = new Point(0, 0);
            bottomTabWidget.Name = "bottomTabWidget";
            bottomTabWidget.SelectedIndex = 0;
            bottomTabWidget.Size = new Size(1165, 157);
            bottomTabWidget.TabIndex = 0;
            // 
            // outputTab
            // 
            outputTab.Location = new Point(4, 24);
            outputTab.Name = "outputTab";
            outputTab.Size = new Size(1157, 129);
            outputTab.TabIndex = 0;
            outputTab.Text = "Output";
            // 
            // debugTab
            // 
            debugTab.Location = new Point(4, 24);
            debugTab.Name = "debugTab";
            debugTab.Size = new Size(1157, 129);
            debugTab.TabIndex = 1;
            debugTab.Text = "Debug";
            // 
            // propertiesTab
            // 
            propertiesTab.Location = new Point(4, 24);
            propertiesTab.Name = "propertiesTab";
            propertiesTab.Size = new Size(1157, 129);
            propertiesTab.TabIndex = 2;
            propertiesTab.Text = "Properties";
            // 
            // editorTabs
            // 
            editorTabs.Dock = DockStyle.Fill;
            editorTabs.Location = new Point(0, 0);
            editorTabs.Name = "editorTabs";
            editorTabs.SelectedIndex = 0;
            editorTabs.ShowToolTips = true;
            editorTabs.Size = new Size(965, 534);
            editorTabs.TabIndex = 4;
            // 
            // componentsPanel
            // 
            componentsPanel.Dock = DockStyle.Fill;
            componentsPanel.Location = new Point(769, 0);
            componentsPanel.MaximumSize = new Size(300, 0);
            componentsPanel.MinimumSize = new Size(200, 0);
            componentsPanel.Name = "componentsPanel";
            componentsPanel.Size = new Size(192, 534);
            componentsPanel.TabIndex = 4;
            // Set componentsWidget alias after componentsPanel is initialized
            componentsWidget = componentsPanel;
            // 
            // Setup SplitContainer hierarchy
            // 
            horizontalSplitContainer.Panel1.Controls.Add(editorWidget);
            horizontalSplitContainer.Panel2.Controls.Add(componentsPanel);
            verticalSplitContainer.Panel1.Controls.Add(projectWidget);
            verticalSplitContainer.Panel2.Controls.Add(horizontalSplitContainer);
            mainSplitContainer.Panel1.Controls.Add(verticalSplitContainer);
            mainSplitContainer.Panel2.Controls.Add(bottomWidget);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1165, 737);
            Controls.Add(mainSplitContainer);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            Text = "AccuTrack Designer";
            InitializeMenus();
            editorWidget.ResumeLayout(false);
            bottomWidget.ResumeLayout(false);
            bottomTabWidget.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(mainSplitContainer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(verticalSplitContainer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(horizontalSplitContainer)).EndInit();
            mainSplitContainer.ResumeLayout(false);
            verticalSplitContainer.ResumeLayout(false);
            horizontalSplitContainer.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private void InitializeMenus()
        {
            // File Menu
            menuFile = new ToolStripMenuItem("File");
            actionNewProject = new ToolStripMenuItem("New Project...", null, null, Keys.Control | Keys.N);
            actionOpenProject = new ToolStripMenuItem("Open Project...", null, null, Keys.Control | Keys.O);
            actionCloseProject = new ToolStripMenuItem("Close Project", null, null, Keys.Control | Keys.W);
            separator1 = new ToolStripSeparator();
            actionSave = new ToolStripMenuItem("Save", null, null, Keys.Control | Keys.S);
            actionSaveAs = new ToolStripMenuItem("Save As...", null, null, Keys.Control | Keys.Shift | Keys.S);
            actionSaveAll = new ToolStripMenuItem("Save All", null, null, Keys.Control | Keys.Shift | Keys.S);
            actionCloseEditor = new ToolStripMenuItem("Close", null, null, Keys.Control | Keys.F4);
            actionCloseAllEditors = new ToolStripMenuItem("Close All");
            separator2 = new ToolStripSeparator();
            actionRename = new ToolStripMenuItem("Rename...", null, null, Keys.F2);
            separator3 = new ToolStripSeparator();
            actionImport = new ToolStripMenuItem("Import...");
            actionExport = new ToolStripMenuItem("Export...");
            separator4 = new ToolStripSeparator();
            actionArchive = new ToolStripMenuItem("Archive Project...");
            actionRestore = new ToolStripMenuItem("Restore Project...");
            separator5 = new ToolStripSeparator();
            actionRecentProjects = new ToolStripMenuItem("Recent Projects");
            separator6 = new ToolStripSeparator();
            actionExit = new ToolStripMenuItem("Exit", null, null, Keys.Alt | Keys.F4);

            menuFile.DropDownItems.AddRange(new ToolStripItem[] {
                actionNewProject,
                actionOpenProject,
                actionCloseProject,
                separator1,
                actionSave,
                actionSaveAs,
                actionSaveAll,
                actionCloseEditor,
                actionCloseAllEditors,
                separator2,
                actionRename,
                separator3,
                actionImport,
                actionExport,
                separator4,
                actionArchive,
                actionRestore,
                separator5,
                actionRecentProjects,
                separator6,
                actionExit
            });

            // Edit Menu
            menuEdit = new ToolStripMenuItem("Edit");
            actionUndo = new ToolStripMenuItem("Undo", null, null, Keys.Control | Keys.Z);
            actionRedo = new ToolStripMenuItem("Redo", null, null, Keys.Control | Keys.Y);
            separator7 = new ToolStripSeparator();
            actionCut = new ToolStripMenuItem("Cut", null, null, Keys.Control | Keys.X);
            actionCopy = new ToolStripMenuItem("Copy", null, null, Keys.Control | Keys.C);
            actionPaste = new ToolStripMenuItem("Paste", null, null, Keys.Control | Keys.V);
            actionDelete = new ToolStripMenuItem("Delete", null, null, Keys.Delete);

            menuEdit.DropDownItems.AddRange(new ToolStripItem[] {
                actionUndo,
                actionRedo,
                separator7,
                actionCut,
                actionCopy,
                actionPaste,
                actionDelete
            });

            // View Menu
            menuView = new ToolStripMenuItem("View");
            actionProjectExplorer = new ToolStripMenuItem("Project Explorer") { Checked = true, CheckOnClick = true };
            actionProperties = new ToolStripMenuItem("Properties") { Checked = true, CheckOnClick = true };
            actionOutput = new ToolStripMenuItem("Output") { Checked = true, CheckOnClick = true };
            actionComponentPalette = new ToolStripMenuItem("Component Palette") { Checked = true, CheckOnClick = true };
            separator8 = new ToolStripSeparator();
            actionZoomIn = new ToolStripMenuItem("Zoom In", null, null, Keys.Control | Keys.Add);
            actionZoomOut = new ToolStripMenuItem("Zoom Out", null, null, Keys.Control | Keys.Subtract);
            actionResetZoom = new ToolStripMenuItem("Reset Zoom", null, null, Keys.Control | Keys.D0);

            menuView.DropDownItems.AddRange(new ToolStripItem[] {
                actionProjectExplorer,
                actionProperties,
                actionOutput,
                actionComponentPalette,
                separator8,
                actionZoomIn,
                actionZoomOut,
                actionResetZoom
            });

            // Build Menu
            menuBuild = new ToolStripMenuItem("Build");
            actionBuildProject = new ToolStripMenuItem("Build Project", null, null, Keys.F7);
            actionRebuildProject = new ToolStripMenuItem("Rebuild Project", null, null, Keys.Control | Keys.F7);
            actionCleanProject = new ToolStripMenuItem("Clean Project");
            separator9 = new ToolStripSeparator();
            actionSelectActiveProject = new ToolStripMenuItem("Select Active Project...");
            separator12 = new ToolStripSeparator();
            actionBuildSettings = new ToolStripMenuItem("Build Settings...");

            menuBuild.DropDownItems.AddRange(new ToolStripItem[] {
                actionBuildProject,
                actionRebuildProject,
                actionCleanProject,
                separator9,
                actionSelectActiveProject,
                separator12,
                actionBuildSettings
            });

            // Download Menu
            menuDownload = new ToolStripMenuItem("Download");
            actionDownloadToDevice = new ToolStripMenuItem("Download to Device...", null, null, Keys.F5);
            actionDownloadSettings = new ToolStripMenuItem("Download Settings...");

            menuDownload.DropDownItems.AddRange(new ToolStripItem[] {
                actionDownloadToDevice,
                actionDownloadSettings
            });

            // Upload Menu
            menuUpload = new ToolStripMenuItem("Upload");
            actionUploadFromDevice = new ToolStripMenuItem("Upload from Device...", null, null, Keys.F6);
            actionUploadSettings = new ToolStripMenuItem("Upload Settings...");

            menuUpload.DropDownItems.AddRange(new ToolStripItem[] {
                actionUploadFromDevice,
                actionUploadSettings
            });

            // Simulate Menu
            menuSimulate = new ToolStripMenuItem("Simulate");
            actionStartSimulation = new ToolStripMenuItem("Start Simulation", null, null, Keys.F9);
            actionPauseSimulation = new ToolStripMenuItem("Pause Simulation", null, null, Keys.F10) { Enabled = false };
            actionStopSimulation = new ToolStripMenuItem("Stop Simulation", null, null, Keys.Shift | Keys.F9) { Enabled = false };
            separator10 = new ToolStripSeparator();
            actionSimulationSettings = new ToolStripMenuItem("Simulation Settings...");

            menuSimulate.DropDownItems.AddRange(new ToolStripItem[] {
                actionStartSimulation,
                actionPauseSimulation,
                actionStopSimulation,
                separator10,
                actionSimulationSettings
            });

            // Tools Menu
            menuTools = new ToolStripMenuItem("Tools");
            actionOptions = new ToolStripMenuItem("Options...");
            actionCustomize = new ToolStripMenuItem("Customize...");
            separator11 = new ToolStripSeparator();
            actionExternalTools = new ToolStripMenuItem("External Tools...");
            actionPackageManager = new ToolStripMenuItem("Package Manager...");

            menuTools.DropDownItems.AddRange(new ToolStripItem[] {
                actionOptions,
                actionCustomize,
                separator11,
                actionExternalTools,
                actionPackageManager
            });

            // Help Menu
            menuHelp = new ToolStripMenuItem("Help");
            actionDocumentation = new ToolStripMenuItem("Documentation", null, null, Keys.F1);
            actionAbout = new ToolStripMenuItem("About");

            menuHelp.DropDownItems.AddRange(new ToolStripItem[] {
                actionDocumentation,
                actionAbout
            });

            // Add menus to menu strip
            menuStrip.Items.AddRange(new ToolStripItem[] {
                menuFile,
                menuEdit,
                menuView,
                menuBuild,
                menuDownload,
                menuUpload,
                menuSimulate,
                menuTools,
                menuHelp
            });
        }

        #endregion

        private Panel bottomWidget;
        private TabControl bottomTabWidget;
        private TabPage outputTab;
        private TabPage debugTab;
        private TabPage propertiesTab;
        private TabControl editorTabs;
        private ContextMenuStrip contextMenuEditorTabs;
        private Panel componentsPanel;
    }
}
