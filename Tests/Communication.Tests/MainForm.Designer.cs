namespace Communication.Tests
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtEndpointUrl;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblConnected;
        private System.Windows.Forms.Label lblRunning;
        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.GroupBox grpNodeOperations;
        private System.Windows.Forms.TextBox txtNodeId;
        private System.Windows.Forms.Button btnReadNode;
        private System.Windows.Forms.TextBox txtWriteNodeId;
        private System.Windows.Forms.TextBox txtWriteValue;
        private System.Windows.Forms.Button btnWriteNode;
        private System.Windows.Forms.ComboBox cmbDataType;
        private System.Windows.Forms.Label lblNodeId;
        private System.Windows.Forms.Label lblWriteNodeId;
        private System.Windows.Forms.Label lblWriteValue;
        private System.Windows.Forms.Label lblDataType;
        private System.Windows.Forms.GroupBox grpConsole;
        private System.Windows.Forms.Button btnClearConsole;
        private System.Windows.Forms.ListBox lstNodeValues;
        private System.Windows.Forms.GroupBox grpNodeValues;
        private System.Windows.Forms.GroupBox grpTagMappings;
        private System.Windows.Forms.TextBox txtTagMappings;
        private System.Windows.Forms.Label lblTagMappings;
        private System.Windows.Forms.GroupBox grpNodeBrowser;
        private System.Windows.Forms.ListBox lstBrowsedNodes;
        private System.Windows.Forms.TextBox txtNodeFilter;
        private System.Windows.Forms.Button btnBrowseNodes;
        private System.Windows.Forms.Label lblNodeFilter;
        private System.Windows.Forms.Label lblNodeCount;
        private System.Windows.Forms.TextBox txtBrowseStartNode;
        private System.Windows.Forms.Label lblBrowseStartNode;
        private System.Windows.Forms.RadioButton rdoClientMode;
        private System.Windows.Forms.RadioButton rdoServerMode;
        private System.Windows.Forms.GroupBox grpServerNodes;
        private System.Windows.Forms.ListBox lstServerNodes;
        private System.Windows.Forms.TextBox txtServerNodeId;
        private System.Windows.Forms.TextBox txtServerNodeValue;
        private System.Windows.Forms.Button btnAddServerNode;
        private System.Windows.Forms.Label lblServerNodeId;
        private System.Windows.Forms.Label lblServerNodeValue;
        private System.Windows.Forms.GroupBox grpSubscriptions;
        private System.Windows.Forms.TextBox txtSubscribeNodeId;
        private System.Windows.Forms.Button btnSubscribeNode;
        private System.Windows.Forms.Button btnUnsubscribeNode;
        private System.Windows.Forms.ListBox lstSubscribedNodes;
        private System.Windows.Forms.Label lblSubscribedCount;
        private System.Windows.Forms.Label lblSubscribeNodeId;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtEndpointUrl = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblConnected = new System.Windows.Forms.Label();
            this.lblRunning = new System.Windows.Forms.Label();
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.grpTagMappings = new System.Windows.Forms.GroupBox();
            this.txtTagMappings = new System.Windows.Forms.TextBox();
            this.lblTagMappings = new System.Windows.Forms.Label();
            this.grpNodeOperations = new System.Windows.Forms.GroupBox();
            this.cmbDataType = new System.Windows.Forms.ComboBox();
            this.lblDataType = new System.Windows.Forms.Label();
            this.btnWriteNode = new System.Windows.Forms.Button();
            this.txtWriteValue = new System.Windows.Forms.TextBox();
            this.lblWriteValue = new System.Windows.Forms.Label();
            this.txtWriteNodeId = new System.Windows.Forms.TextBox();
            this.lblWriteNodeId = new System.Windows.Forms.Label();
            this.btnReadNode = new System.Windows.Forms.Button();
            this.txtNodeId = new System.Windows.Forms.TextBox();
            this.lblNodeId = new System.Windows.Forms.Label();
            this.grpConsole = new System.Windows.Forms.GroupBox();
            this.btnClearConsole = new System.Windows.Forms.Button();
            this.grpNodeValues = new System.Windows.Forms.GroupBox();
            this.lstNodeValues = new System.Windows.Forms.ListBox();
            this.grpNodeBrowser = new System.Windows.Forms.GroupBox();
            this.lstBrowsedNodes = new System.Windows.Forms.ListBox();
            this.txtNodeFilter = new System.Windows.Forms.TextBox();
            this.btnBrowseNodes = new System.Windows.Forms.Button();
            this.lblNodeFilter = new System.Windows.Forms.Label();
            this.lblNodeCount = new System.Windows.Forms.Label();
            this.txtBrowseStartNode = new System.Windows.Forms.TextBox();
            this.lblBrowseStartNode = new System.Windows.Forms.Label();
            this.rdoClientMode = new System.Windows.Forms.RadioButton();
            this.rdoServerMode = new System.Windows.Forms.RadioButton();
            this.grpServerNodes = new System.Windows.Forms.GroupBox();
            this.lstServerNodes = new System.Windows.Forms.ListBox();
            this.txtServerNodeId = new System.Windows.Forms.TextBox();
            this.txtServerNodeValue = new System.Windows.Forms.TextBox();
            this.btnAddServerNode = new System.Windows.Forms.Button();
            this.lblServerNodeId = new System.Windows.Forms.Label();
            this.lblServerNodeValue = new System.Windows.Forms.Label();
            this.grpSubscriptions = new System.Windows.Forms.GroupBox();
            this.txtSubscribeNodeId = new System.Windows.Forms.TextBox();
            this.btnSubscribeNode = new System.Windows.Forms.Button();
            this.btnUnsubscribeNode = new System.Windows.Forms.Button();
            this.lstSubscribedNodes = new System.Windows.Forms.ListBox();
            this.lblSubscribedCount = new System.Windows.Forms.Label();
            this.lblSubscribeNodeId = new System.Windows.Forms.Label();
            this.grpConnection.SuspendLayout();
            this.grpTagMappings.SuspendLayout();
            this.grpNodeOperations.SuspendLayout();
            this.grpConsole.SuspendLayout();
            this.grpNodeValues.SuspendLayout();
            this.grpNodeBrowser.SuspendLayout();
            this.grpServerNodes.SuspendLayout();
            this.grpSubscriptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtEndpointUrl
            // 
            this.txtEndpointUrl.Location = new System.Drawing.Point(12, 19);
            this.txtEndpointUrl.Name = "txtEndpointUrl";
            this.txtEndpointUrl.Size = new System.Drawing.Size(400, 23);
            this.txtEndpointUrl.TabIndex = 0;
            this.txtEndpointUrl.Text = "opc.tcp://localhost:4840";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(418, 18);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(100, 25);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(524, 18);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(100, 25);
            this.btnDisconnect.TabIndex = 2;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // txtConsole
            // 
            this.txtConsole.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConsole.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtConsole.Location = new System.Drawing.Point(6, 22);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.ReadOnly = true;
            this.txtConsole.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtConsole.Size = new System.Drawing.Size(618, 200);
            this.txtConsole.TabIndex = 3;
            this.txtConsole.WordWrap = false;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 48);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(45, 15);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Status: -";
            // 
            // lblConnected
            // 
            this.lblConnected.AutoSize = true;
            this.lblConnected.Location = new System.Drawing.Point(12, 68);
            this.lblConnected.Name = "lblConnected";
            this.lblConnected.Size = new System.Drawing.Size(66, 15);
            this.lblConnected.TabIndex = 5;
            this.lblConnected.Text = "Connected: -";
            // 
            // lblRunning
            // 
            this.lblRunning.AutoSize = true;
            this.lblRunning.Location = new System.Drawing.Point(12, 88);
            this.lblRunning.Name = "lblRunning";
            this.lblRunning.Size = new System.Drawing.Size(60, 15);
            this.lblRunning.TabIndex = 6;
            this.lblRunning.Text = "Running: -";
            // 
            // grpConnection
            // 
            this.grpConnection.Controls.Add(this.rdoServerMode);
            this.grpConnection.Controls.Add(this.rdoClientMode);
            this.grpConnection.Controls.Add(this.grpTagMappings);
            this.grpConnection.Controls.Add(this.lblRunning);
            this.grpConnection.Controls.Add(this.lblConnected);
            this.grpConnection.Controls.Add(this.lblStatus);
            this.grpConnection.Controls.Add(this.btnDisconnect);
            this.grpConnection.Controls.Add(this.btnConnect);
            this.grpConnection.Controls.Add(this.txtEndpointUrl);
            this.grpConnection.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpConnection.Location = new System.Drawing.Point(0, 0);
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.Size = new System.Drawing.Size(630, 230);
            this.grpConnection.TabIndex = 7;
            this.grpConnection.TabStop = false;
            this.grpConnection.Text = "OPC UA Connection";
            // 
            // rdoClientMode
            // 
            this.rdoClientMode.AutoSize = true;
            this.rdoClientMode.Checked = true;
            this.rdoClientMode.Location = new System.Drawing.Point(12, 110);
            this.rdoClientMode.Name = "rdoClientMode";
            this.rdoClientMode.Size = new System.Drawing.Size(55, 19);
            this.rdoClientMode.TabIndex = 8;
            this.rdoClientMode.TabStop = true;
            this.rdoClientMode.Text = "Client";
            this.rdoClientMode.UseVisualStyleBackColor = true;
            this.rdoClientMode.CheckedChanged += new System.EventHandler(this.rdoClientMode_CheckedChanged);
            // 
            // rdoServerMode
            // 
            this.rdoServerMode.AutoSize = true;
            this.rdoServerMode.Location = new System.Drawing.Point(73, 110);
            this.rdoServerMode.Name = "rdoServerMode";
            this.rdoServerMode.Size = new System.Drawing.Size(56, 19);
            this.rdoServerMode.TabIndex = 9;
            this.rdoServerMode.Text = "Server";
            this.rdoServerMode.UseVisualStyleBackColor = true;
            this.rdoServerMode.CheckedChanged += new System.EventHandler(this.rdoServerMode_CheckedChanged);
            // 
            // grpTagMappings
            // 
            this.grpTagMappings.Controls.Add(this.txtTagMappings);
            this.grpTagMappings.Controls.Add(this.lblTagMappings);
            this.grpTagMappings.Location = new System.Drawing.Point(12, 135);
            this.grpTagMappings.Name = "grpTagMappings";
            this.grpTagMappings.Size = new System.Drawing.Size(612, 84);
            this.grpTagMappings.TabIndex = 7;
            this.grpTagMappings.TabStop = false;
            this.grpTagMappings.Text = "Tag Mappings (tagName=nodeId, one per line) - Client Mode Only";
            // 
            // txtTagMappings
            // 
            this.txtTagMappings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTagMappings.Font = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtTagMappings.Location = new System.Drawing.Point(6, 22);
            this.txtTagMappings.Multiline = true;
            this.txtTagMappings.Name = "txtTagMappings";
            this.txtTagMappings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTagMappings.Size = new System.Drawing.Size(600, 56);
            this.txtTagMappings.TabIndex = 1;
            this.txtTagMappings.Text = "Temperature=ns=2;s=Temperature\r\nPressure=ns=2;s=Pressure";
            // 
            // lblTagMappings
            // 
            this.lblTagMappings.AutoSize = true;
            this.lblTagMappings.Location = new System.Drawing.Point(6, 19);
            this.lblTagMappings.Name = "lblTagMappings";
            this.lblTagMappings.Size = new System.Drawing.Size(0, 15);
            this.lblTagMappings.TabIndex = 0;
            // 
            // grpNodeOperations
            // 
            this.grpNodeOperations.Controls.Add(this.cmbDataType);
            this.grpNodeOperations.Controls.Add(this.lblDataType);
            this.grpNodeOperations.Controls.Add(this.btnWriteNode);
            this.grpNodeOperations.Controls.Add(this.txtWriteValue);
            this.grpNodeOperations.Controls.Add(this.lblWriteValue);
            this.grpNodeOperations.Controls.Add(this.txtWriteNodeId);
            this.grpNodeOperations.Controls.Add(this.lblWriteNodeId);
            this.grpNodeOperations.Controls.Add(this.btnReadNode);
            this.grpNodeOperations.Controls.Add(this.txtNodeId);
            this.grpNodeOperations.Controls.Add(this.lblNodeId);
            this.grpNodeOperations.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpNodeOperations.Location = new System.Drawing.Point(0, 230);
            this.grpNodeOperations.Name = "grpNodeOperations";
            this.grpNodeOperations.Size = new System.Drawing.Size(630, 120);
            this.grpNodeOperations.TabIndex = 8;
            this.grpNodeOperations.TabStop = false;
            this.grpNodeOperations.Text = "Node Operations (Client Mode)";
            // 
            // cmbDataType
            // 
            this.cmbDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDataType.FormattingEnabled = true;
            this.cmbDataType.Items.AddRange(new object[] {
            "Auto",
            "Boolean",
            "Int32",
            "Double",
            "String"});
            this.cmbDataType.Location = new System.Drawing.Point(418, 80);
            this.cmbDataType.Name = "cmbDataType";
            this.cmbDataType.Size = new System.Drawing.Size(100, 23);
            this.cmbDataType.TabIndex = 9;
            // 
            // lblDataType
            // 
            this.lblDataType.AutoSize = true;
            this.lblDataType.Location = new System.Drawing.Point(350, 83);
            this.lblDataType.Name = "lblDataType";
            this.lblDataType.Size = new System.Drawing.Size(62, 15);
            this.lblDataType.TabIndex = 8;
            this.lblDataType.Text = "Data Type:";
            // 
            // btnWriteNode
            // 
            this.btnWriteNode.Location = new System.Drawing.Point(524, 79);
            this.btnWriteNode.Name = "btnWriteNode";
            this.btnWriteNode.Size = new System.Drawing.Size(100, 25);
            this.btnWriteNode.TabIndex = 7;
            this.btnWriteNode.Text = "Write";
            this.btnWriteNode.UseVisualStyleBackColor = true;
            this.btnWriteNode.Click += new System.EventHandler(this.btnWriteNode_Click);
            // 
            // txtWriteValue
            // 
            this.txtWriteValue.Location = new System.Drawing.Point(80, 80);
            this.txtWriteValue.Name = "txtWriteValue";
            this.txtWriteValue.Size = new System.Drawing.Size(264, 23);
            this.txtWriteValue.TabIndex = 6;
            // 
            // lblWriteValue
            // 
            this.lblWriteValue.AutoSize = true;
            this.lblWriteValue.Location = new System.Drawing.Point(12, 83);
            this.lblWriteValue.Name = "lblWriteValue";
            this.lblWriteValue.Size = new System.Drawing.Size(38, 15);
            this.lblWriteValue.TabIndex = 5;
            this.lblWriteValue.Text = "Value:";
            // 
            // txtWriteNodeId
            // 
            this.txtWriteNodeId.Location = new System.Drawing.Point(80, 51);
            this.txtWriteNodeId.Name = "txtWriteNodeId";
            this.txtWriteNodeId.Size = new System.Drawing.Size(544, 23);
            this.txtWriteNodeId.TabIndex = 4;
            this.txtWriteNodeId.Text = "ns=2;s=MyVariable";
            // 
            // lblWriteNodeId
            // 
            this.lblWriteNodeId.AutoSize = true;
            this.lblWriteNodeId.Location = new System.Drawing.Point(12, 54);
            this.lblWriteNodeId.Name = "lblWriteNodeId";
            this.lblWriteNodeId.Size = new System.Drawing.Size(55, 15);
            this.lblWriteNodeId.TabIndex = 3;
            this.lblWriteNodeId.Text = "Write Node:";
            // 
            // btnReadNode
            // 
            this.btnReadNode.Location = new System.Drawing.Point(524, 22);
            this.btnReadNode.Name = "btnReadNode";
            this.btnReadNode.Size = new System.Drawing.Size(100, 25);
            this.btnReadNode.TabIndex = 2;
            this.btnReadNode.Text = "Read";
            this.btnReadNode.UseVisualStyleBackColor = true;
            this.btnReadNode.Click += new System.EventHandler(this.btnReadNode_Click);
            // 
            // txtNodeId
            // 
            this.txtNodeId.Location = new System.Drawing.Point(80, 22);
            this.txtNodeId.Name = "txtNodeId";
            this.txtNodeId.Size = new System.Drawing.Size(438, 23);
            this.txtNodeId.TabIndex = 1;
            this.txtNodeId.Text = "ns=0;i=2253";
            // 
            // lblNodeId
            // 
            this.lblNodeId.AutoSize = true;
            this.lblNodeId.Location = new System.Drawing.Point(12, 25);
            this.lblNodeId.Name = "lblNodeId";
            this.lblNodeId.Size = new System.Drawing.Size(54, 15);
            this.lblNodeId.TabIndex = 0;
            this.lblNodeId.Text = "Read Node:";
            // 
            // grpConsole
            // 
            this.grpConsole.Controls.Add(this.btnClearConsole);
            this.grpConsole.Controls.Add(this.txtConsole);
            this.grpConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConsole.Location = new System.Drawing.Point(0, 670);
            this.grpConsole.Name = "grpConsole";
            this.grpConsole.Size = new System.Drawing.Size(630, 170);
            this.grpConsole.TabIndex = 9;
            this.grpConsole.TabStop = false;
            this.grpConsole.Text = "Debug Console";
            // 
            // btnClearConsole
            // 
            this.btnClearConsole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearConsole.Location = new System.Drawing.Point(549, 0);
            this.btnClearConsole.Name = "btnClearConsole";
            this.btnClearConsole.Size = new System.Drawing.Size(75, 23);
            this.btnClearConsole.TabIndex = 4;
            this.btnClearConsole.Text = "Clear";
            this.btnClearConsole.UseVisualStyleBackColor = true;
            this.btnClearConsole.Click += new System.EventHandler(this.btnClearConsole_Click);
            // 
            // grpNodeValues
            // 
            this.grpNodeValues.Controls.Add(this.lstNodeValues);
            this.grpNodeValues.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpNodeValues.Location = new System.Drawing.Point(0, 350);
            this.grpNodeValues.Name = "grpNodeValues";
            this.grpNodeValues.Size = new System.Drawing.Size(630, 120);
            this.grpNodeValues.TabIndex = 10;
            this.grpNodeValues.TabStop = false;
            this.grpNodeValues.Text = "Node Values (Client Mode)";
            // 
            // lstNodeValues
            // 
            this.lstNodeValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstNodeValues.FormattingEnabled = true;
            this.lstNodeValues.ItemHeight = 15;
            this.lstNodeValues.Location = new System.Drawing.Point(3, 19);
            this.lstNodeValues.Name = "lstNodeValues";
            this.lstNodeValues.Size = new System.Drawing.Size(624, 98);
            this.lstNodeValues.TabIndex = 0;
            // 
            // grpNodeBrowser
            // 
            this.grpNodeBrowser.Controls.Add(this.lstBrowsedNodes);
            this.grpNodeBrowser.Controls.Add(this.lblNodeCount);
            this.grpNodeBrowser.Controls.Add(this.txtNodeFilter);
            this.grpNodeBrowser.Controls.Add(this.lblNodeFilter);
            this.grpNodeBrowser.Controls.Add(this.btnBrowseNodes);
            this.grpNodeBrowser.Controls.Add(this.txtBrowseStartNode);
            this.grpNodeBrowser.Controls.Add(this.lblBrowseStartNode);
            this.grpNodeBrowser.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpNodeBrowser.Location = new System.Drawing.Point(0, 470);
            this.grpNodeBrowser.Name = "grpNodeBrowser";
            this.grpNodeBrowser.Size = new System.Drawing.Size(630, 200);
            this.grpNodeBrowser.TabIndex = 11;
            this.grpNodeBrowser.TabStop = false;
            this.grpNodeBrowser.Text = "Node Browser (Client Mode)";
            // 
            // grpServerNodes
            // 
            this.grpServerNodes.Controls.Add(this.btnAddServerNode);
            this.grpServerNodes.Controls.Add(this.txtServerNodeValue);
            this.grpServerNodes.Controls.Add(this.lblServerNodeValue);
            this.grpServerNodes.Controls.Add(this.txtServerNodeId);
            this.grpServerNodes.Controls.Add(this.lblServerNodeId);
            this.grpServerNodes.Controls.Add(this.lstServerNodes);
            this.grpServerNodes.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpServerNodes.Location = new System.Drawing.Point(0, 350);
            this.grpServerNodes.Name = "grpServerNodes";
            this.grpServerNodes.Size = new System.Drawing.Size(630, 120);
            this.grpServerNodes.TabIndex = 12;
            this.grpServerNodes.TabStop = false;
            this.grpServerNodes.Text = "Server Nodes (Server Mode)";
            this.grpServerNodes.Visible = false;
            // 
            // lstServerNodes
            // 
            this.lstServerNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstServerNodes.FormattingEnabled = true;
            this.lstServerNodes.ItemHeight = 15;
            this.lstServerNodes.Location = new System.Drawing.Point(6, 48);
            this.lstServerNodes.Name = "lstServerNodes";
            this.lstServerNodes.Size = new System.Drawing.Size(618, 64);
            this.lstServerNodes.TabIndex = 0;
            // 
            // txtServerNodeId
            // 
            this.txtServerNodeId.Location = new System.Drawing.Point(80, 19);
            this.txtServerNodeId.Name = "txtServerNodeId";
            this.txtServerNodeId.Size = new System.Drawing.Size(300, 23);
            this.txtServerNodeId.TabIndex = 1;
            this.txtServerNodeId.Text = "ns=2;s=MyVariable";
            // 
            // lblServerNodeId
            // 
            this.lblServerNodeId.AutoSize = true;
            this.lblServerNodeId.Location = new System.Drawing.Point(6, 22);
            this.lblServerNodeId.Name = "lblServerNodeId";
            this.lblServerNodeId.Size = new System.Drawing.Size(54, 15);
            this.lblServerNodeId.TabIndex = 2;
            this.lblServerNodeId.Text = "Node ID:";
            // 
            // txtServerNodeValue
            // 
            this.txtServerNodeValue.Location = new System.Drawing.Point(450, 19);
            this.txtServerNodeValue.Name = "txtServerNodeValue";
            this.txtServerNodeValue.Size = new System.Drawing.Size(100, 23);
            this.txtServerNodeValue.TabIndex = 3;
            this.txtServerNodeValue.Text = "0";
            // 
            // lblServerNodeValue
            // 
            this.lblServerNodeValue.AutoSize = true;
            this.lblServerNodeValue.Location = new System.Drawing.Point(386, 22);
            this.lblServerNodeValue.Name = "lblServerNodeValue";
            this.lblServerNodeValue.Size = new System.Drawing.Size(38, 15);
            this.lblServerNodeValue.TabIndex = 4;
            this.lblServerNodeValue.Text = "Value:";
            // 
            // btnAddServerNode
            // 
            this.btnAddServerNode.Location = new System.Drawing.Point(556, 18);
            this.btnAddServerNode.Name = "btnAddServerNode";
            this.btnAddServerNode.Size = new System.Drawing.Size(68, 25);
            this.btnAddServerNode.TabIndex = 5;
            this.btnAddServerNode.Text = "Add/Update";
            this.btnAddServerNode.UseVisualStyleBackColor = true;
            this.btnAddServerNode.Click += new System.EventHandler(this.btnAddServerNode_Click);
            // 
            // lstBrowsedNodes
            // 
            this.lstBrowsedNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstBrowsedNodes.FormattingEnabled = true;
            this.lstBrowsedNodes.ItemHeight = 15;
            this.lstBrowsedNodes.Location = new System.Drawing.Point(6, 75);
            this.lstBrowsedNodes.Name = "lstBrowsedNodes";
            this.lstBrowsedNodes.Size = new System.Drawing.Size(618, 119);
            this.lstBrowsedNodes.TabIndex = 6;
            this.lstBrowsedNodes.DoubleClick += new System.EventHandler(this.lstBrowsedNodes_DoubleClick);
            // 
            // txtNodeFilter
            // 
            this.txtNodeFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNodeFilter.Location = new System.Drawing.Point(80, 46);
            this.txtNodeFilter.Name = "txtNodeFilter";
            this.txtNodeFilter.Size = new System.Drawing.Size(544, 23);
            this.txtNodeFilter.TabIndex = 5;
            this.txtNodeFilter.TextChanged += new System.EventHandler(this.txtNodeFilter_TextChanged);
            // 
            // btnBrowseNodes
            // 
            this.btnBrowseNodes.Location = new System.Drawing.Point(524, 17);
            this.btnBrowseNodes.Name = "btnBrowseNodes";
            this.btnBrowseNodes.Size = new System.Drawing.Size(100, 25);
            this.btnBrowseNodes.TabIndex = 4;
            this.btnBrowseNodes.Text = "Browse All Nodes";
            this.btnBrowseNodes.UseVisualStyleBackColor = true;
            this.btnBrowseNodes.Click += new System.EventHandler(this.btnBrowseNodes_Click);
            // 
            // lblNodeFilter
            // 
            this.lblNodeFilter.AutoSize = true;
            this.lblNodeFilter.Location = new System.Drawing.Point(6, 49);
            this.lblNodeFilter.Name = "lblNodeFilter";
            this.lblNodeFilter.Size = new System.Drawing.Size(38, 15);
            this.lblNodeFilter.TabIndex = 3;
            this.lblNodeFilter.Text = "Filter:";
            // 
            // lblNodeCount
            // 
            this.lblNodeCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblNodeCount.AutoSize = true;
            this.lblNodeCount.Location = new System.Drawing.Point(6, 197);
            this.lblNodeCount.Name = "lblNodeCount";
            this.lblNodeCount.Size = new System.Drawing.Size(60, 15);
            this.lblNodeCount.TabIndex = 7;
            this.lblNodeCount.Text = "Nodes: 0 / 0";
            // 
            // txtBrowseStartNode
            // 
            this.txtBrowseStartNode.Location = new System.Drawing.Point(80, 19);
            this.txtBrowseStartNode.Name = "txtBrowseStartNode";
            this.txtBrowseStartNode.Size = new System.Drawing.Size(438, 23);
            this.txtBrowseStartNode.TabIndex = 1;
            this.txtBrowseStartNode.Text = "ns=0;i=85";
            // 
            // lblBrowseStartNode
            // 
            this.lblBrowseStartNode.AutoSize = true;
            this.lblBrowseStartNode.Location = new System.Drawing.Point(6, 22);
            this.lblBrowseStartNode.Name = "lblBrowseStartNode";
            this.lblBrowseStartNode.Size = new System.Drawing.Size(68, 15);
            this.lblBrowseStartNode.TabIndex = 0;
            this.lblBrowseStartNode.Text = "Start Node:";
            // 
            // grpSubscriptions
            // 
            this.grpSubscriptions.Controls.Add(this.lstSubscribedNodes);
            this.grpSubscriptions.Controls.Add(this.lblSubscribedCount);
            this.grpSubscriptions.Controls.Add(this.btnUnsubscribeNode);
            this.grpSubscriptions.Controls.Add(this.btnSubscribeNode);
            this.grpSubscriptions.Controls.Add(this.txtSubscribeNodeId);
            this.grpSubscriptions.Controls.Add(this.lblSubscribeNodeId);
            this.grpSubscriptions.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSubscriptions.Location = new System.Drawing.Point(0, 670);
            this.grpSubscriptions.Name = "grpSubscriptions";
            this.grpSubscriptions.Size = new System.Drawing.Size(630, 150);
            this.grpSubscriptions.TabIndex = 13;
            this.grpSubscriptions.TabStop = false;
            this.grpSubscriptions.Text = "Node Subscriptions (Client Mode)";
            this.grpSubscriptions.Visible = false;
            // 
            // lblSubscribeNodeId
            // 
            this.lblSubscribeNodeId.AutoSize = true;
            this.lblSubscribeNodeId.Location = new System.Drawing.Point(6, 22);
            this.lblSubscribeNodeId.Name = "lblSubscribeNodeId";
            this.lblSubscribeNodeId.Size = new System.Drawing.Size(55, 15);
            this.lblSubscribeNodeId.TabIndex = 0;
            this.lblSubscribeNodeId.Text = "Node ID:";
            // 
            // txtSubscribeNodeId
            // 
            this.txtSubscribeNodeId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSubscribeNodeId.Location = new System.Drawing.Point(67, 19);
            this.txtSubscribeNodeId.Name = "txtSubscribeNodeId";
            this.txtSubscribeNodeId.Size = new System.Drawing.Size(400, 23);
            this.txtSubscribeNodeId.TabIndex = 1;
            // 
            // btnSubscribeNode
            // 
            this.btnSubscribeNode.Location = new System.Drawing.Point(473, 18);
            this.btnSubscribeNode.Name = "btnSubscribeNode";
            this.btnSubscribeNode.Size = new System.Drawing.Size(75, 25);
            this.btnSubscribeNode.TabIndex = 2;
            this.btnSubscribeNode.Text = "Subscribe";
            this.btnSubscribeNode.UseVisualStyleBackColor = true;
            this.btnSubscribeNode.Click += new System.EventHandler(this.btnSubscribeNode_Click);
            // 
            // btnUnsubscribeNode
            // 
            this.btnUnsubscribeNode.Location = new System.Drawing.Point(554, 18);
            this.btnUnsubscribeNode.Name = "btnUnsubscribeNode";
            this.btnUnsubscribeNode.Size = new System.Drawing.Size(70, 25);
            this.btnUnsubscribeNode.TabIndex = 3;
            this.btnUnsubscribeNode.Text = "Unsubscribe";
            this.btnUnsubscribeNode.UseVisualStyleBackColor = true;
            this.btnUnsubscribeNode.Click += new System.EventHandler(this.btnUnsubscribeNode_Click);
            // 
            // lstSubscribedNodes
            // 
            this.lstSubscribedNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstSubscribedNodes.FormattingEnabled = true;
            this.lstSubscribedNodes.ItemHeight = 15;
            this.lstSubscribedNodes.Location = new System.Drawing.Point(6, 48);
            this.lstSubscribedNodes.Name = "lstSubscribedNodes";
            this.lstSubscribedNodes.Size = new System.Drawing.Size(618, 94);
            this.lstSubscribedNodes.TabIndex = 4;
            this.lstSubscribedNodes.DoubleClick += new System.EventHandler(this.lstSubscribedNodes_DoubleClick);
            // 
            // lblSubscribedCount
            // 
            this.lblSubscribedCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSubscribedCount.AutoSize = true;
            this.lblSubscribedCount.Location = new System.Drawing.Point(6, 145);
            this.lblSubscribedCount.Name = "lblSubscribedCount";
            this.lblSubscribedCount.Size = new System.Drawing.Size(75, 15);
            this.lblSubscribedCount.TabIndex = 5;
            this.lblSubscribedCount.Text = "Subscribed: 0";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(630, 990);
            this.Controls.Add(this.grpConsole);
            this.Controls.Add(this.grpSubscriptions);
            this.Controls.Add(this.grpNodeBrowser);
            this.Controls.Add(this.grpServerNodes);
            this.Controls.Add(this.grpNodeValues);
            this.Controls.Add(this.grpNodeOperations);
            this.Controls.Add(this.grpConnection);
            this.Name = "MainForm";
            this.Text = "OPC UA Communication Test";
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.grpTagMappings.ResumeLayout(false);
            this.grpTagMappings.PerformLayout();
            this.grpNodeOperations.ResumeLayout(false);
            this.grpNodeOperations.PerformLayout();
            this.grpConsole.ResumeLayout(false);
            this.grpConsole.PerformLayout();
            this.grpNodeValues.ResumeLayout(false);
            this.grpNodeBrowser.ResumeLayout(false);
            this.grpNodeBrowser.PerformLayout();
            this.grpServerNodes.ResumeLayout(false);
            this.grpServerNodes.PerformLayout();
            this.grpSubscriptions.ResumeLayout(false);
            this.grpSubscriptions.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
