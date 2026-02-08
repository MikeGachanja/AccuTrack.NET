namespace Designer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        menuStrip1 = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        newToolStripMenuItem = new ToolStripMenuItem();
        openToolStripMenuItem = new ToolStripMenuItem();
        saveToolStripMenuItem = new ToolStripMenuItem();
        saveAsToolStripMenuItem = new ToolStripMenuItem();
        closeToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        buildToolStripMenuItem = new ToolStripMenuItem();
        buildToolStripMenuItemBuild = new ToolStripMenuItem();
        cleanToolStripMenuItem = new ToolStripMenuItem();
        deployToolStripMenuItem = new ToolStripMenuItem();
        deployToolStripMenuItemDeploy = new ToolStripMenuItem();
        uploadToolStripMenuItem = new ToolStripMenuItem();
        simulationToolStripMenuItem = new ToolStripMenuItem();
        startSimulationToolStripMenuItem = new ToolStripMenuItem();
        pauseSimulationToolStripMenuItem = new ToolStripMenuItem();
        stopSimulationToolStripMenuItem = new ToolStripMenuItem();
        toolsToolStripMenuItem = new ToolStripMenuItem();
        optionsToolStripMenuItem = new ToolStripMenuItem();
        customizeToolStripMenuItem = new ToolStripMenuItem();
        externalToolsToolStripMenuItem = new ToolStripMenuItem();
        packageManagerToolStripMenuItem = new ToolStripMenuItem();
        helpToolStripMenuItem = new ToolStripMenuItem();
        documentationToolStripMenuItem = new ToolStripMenuItem();
        aboutToolStripMenuItem = new ToolStripMenuItem();
        statusStrip1 = new StatusStrip();
        toolStripStatusLabel1 = new ToolStripStatusLabel();

        menuStrip1.SuspendLayout();
        statusStrip1.SuspendLayout();
        SuspendLayout();

        // menuStrip1
        menuStrip1.Items.AddRange(new ToolStripItem[] {
            fileToolStripMenuItem, buildToolStripMenuItem, deployToolStripMenuItem,
            simulationToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(1008, 24);
        menuStrip1.TabIndex = 0;
        menuStrip1.Text = "menuStrip1";

        // fileToolStripMenuItem
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            newToolStripMenuItem, openToolStripMenuItem, saveToolStripMenuItem,
            saveAsToolStripMenuItem, closeToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Text = "&File";

        newToolStripMenuItem.Name = "newToolStripMenuItem";
        newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        newToolStripMenuItem.Text = "&New Project...";
        openToolStripMenuItem.Name = "openToolStripMenuItem";
        openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        openToolStripMenuItem.Text = "&Open Project...";
        saveToolStripMenuItem.Name = "saveToolStripMenuItem";
        saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        saveToolStripMenuItem.Text = "&Save";
        saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
        saveAsToolStripMenuItem.Text = "Save &As...";
        closeToolStripMenuItem.Name = "closeToolStripMenuItem";
        closeToolStripMenuItem.Text = "&Close Project";
        toolStripSeparator1.Name = "toolStripSeparator1";
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Text = "E&xit";

        // buildToolStripMenuItem
        buildToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { buildToolStripMenuItemBuild, cleanToolStripMenuItem });
        buildToolStripMenuItem.Name = "buildToolStripMenuItem";
        buildToolStripMenuItem.Text = "&Build";
        buildToolStripMenuItemBuild.Name = "buildToolStripMenuItemBuild";
        buildToolStripMenuItemBuild.ShortcutKeys = Keys.F7;
        buildToolStripMenuItemBuild.Text = "&Build";
        cleanToolStripMenuItem.Name = "cleanToolStripMenuItem";
        cleanToolStripMenuItem.Text = "&Clean";

        // deployToolStripMenuItem
        deployToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { deployToolStripMenuItemDeploy, uploadToolStripMenuItem });
        deployToolStripMenuItem.Name = "deployToolStripMenuItem";
        deployToolStripMenuItem.Text = "&Deploy";
        deployToolStripMenuItemDeploy.Name = "deployToolStripMenuItemDeploy";
        deployToolStripMenuItemDeploy.Text = "&Deploy to Device...";
        uploadToolStripMenuItem.Name = "uploadToolStripMenuItem";
        uploadToolStripMenuItem.Text = "&Upload from Device...";

        // simulationToolStripMenuItem
        simulationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            startSimulationToolStripMenuItem, pauseSimulationToolStripMenuItem, stopSimulationToolStripMenuItem });
        simulationToolStripMenuItem.Name = "simulationToolStripMenuItem";
        simulationToolStripMenuItem.Text = "Si&mulation";
        startSimulationToolStripMenuItem.Name = "startSimulationToolStripMenuItem";
        startSimulationToolStripMenuItem.Text = "&Start";
        pauseSimulationToolStripMenuItem.Name = "pauseSimulationToolStripMenuItem";
        pauseSimulationToolStripMenuItem.Text = "&Pause";
        stopSimulationToolStripMenuItem.Name = "stopSimulationToolStripMenuItem";
        stopSimulationToolStripMenuItem.Text = "S&top";

        // toolsToolStripMenuItem
        toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            optionsToolStripMenuItem, customizeToolStripMenuItem, externalToolsToolStripMenuItem, packageManagerToolStripMenuItem });
        toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
        toolsToolStripMenuItem.Text = "&Tools";
        optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
        optionsToolStripMenuItem.Text = "&Options...";
        customizeToolStripMenuItem.Name = "customizeToolStripMenuItem";
        customizeToolStripMenuItem.Text = "&Customize...";
        externalToolsToolStripMenuItem.Name = "externalToolsToolStripMenuItem";
        externalToolsToolStripMenuItem.Text = "&External Tools...";
        packageManagerToolStripMenuItem.Name = "packageManagerToolStripMenuItem";
        packageManagerToolStripMenuItem.Text = "&Package Manager...";

        // helpToolStripMenuItem
        helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { documentationToolStripMenuItem, aboutToolStripMenuItem });
        helpToolStripMenuItem.Name = "helpToolStripMenuItem";
        helpToolStripMenuItem.Text = "&Help";
        documentationToolStripMenuItem.Name = "documentationToolStripMenuItem";
        documentationToolStripMenuItem.Text = "&Documentation";
        aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
        aboutToolStripMenuItem.Text = "&About AccuTrack Designer";

        // statusStrip1
        statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
        statusStrip1.Location = new Point(0, 428);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(1008, 22);
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        toolStripStatusLabel1.Name = "toolStripStatusLabel1";
        toolStripStatusLabel1.Text = "Ready";

        // MainForm: single menu bar, then content, then status bar
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1008, 450);
        Controls.Add(statusStrip1);
        Controls.Add(menuStrip1);
        MainMenuStrip = menuStrip1;
        Name = "MainForm";
        Text = "AccuTrack Designer";
        WindowState = FormWindowState.Maximized;

        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip1;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem newToolStripMenuItem;
    private ToolStripMenuItem openToolStripMenuItem;
    private ToolStripMenuItem saveToolStripMenuItem;
    private ToolStripMenuItem saveAsToolStripMenuItem;
    private ToolStripMenuItem closeToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem exitToolStripMenuItem;
    private ToolStripMenuItem buildToolStripMenuItem;
    private ToolStripMenuItem buildToolStripMenuItemBuild;
    private ToolStripMenuItem cleanToolStripMenuItem;
    private ToolStripMenuItem deployToolStripMenuItem;
    private ToolStripMenuItem deployToolStripMenuItemDeploy;
    private ToolStripMenuItem uploadToolStripMenuItem;
    private ToolStripMenuItem simulationToolStripMenuItem;
    private ToolStripMenuItem startSimulationToolStripMenuItem;
    private ToolStripMenuItem pauseSimulationToolStripMenuItem;
    private ToolStripMenuItem stopSimulationToolStripMenuItem;
    private ToolStripMenuItem toolsToolStripMenuItem;
    private ToolStripMenuItem optionsToolStripMenuItem;
    private ToolStripMenuItem customizeToolStripMenuItem;
    private ToolStripMenuItem externalToolsToolStripMenuItem;
    private ToolStripMenuItem packageManagerToolStripMenuItem;
    private ToolStripMenuItem helpToolStripMenuItem;
    private ToolStripMenuItem documentationToolStripMenuItem;
    private ToolStripMenuItem aboutToolStripMenuItem;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel toolStripStatusLabel1;
}
