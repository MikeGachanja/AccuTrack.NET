# AccuTrack Designer Qt to C# Migration Plan

## Overview
This document outlines the comprehensive plan to migrate the AccuTrack Designer from Qt (C++) to C# Windows Forms. The migration will preserve all functionality while adapting to C#/.NET patterns and Windows Forms UI framework.

---

## 1. Main Application Structure

### 1.1 Application Entry Point
**Qt Implementation:** `main.cpp`
**C# Implementation:** `Program.cs` (already exists, needs enhancement)

**Tasks:**
- [ ] Enhance `Program.cs` to handle application configuration
- [ ] Add application icon support
- [ ] Implement theme/options loading on startup
- [ ] Integrate startup page before main window
- [ ] Handle command-line arguments

**Files:**
- `Designer/Program.cs` (modify)

---

### 1.2 Startup Page
**Qt Implementation:** `startuppage.h`, `startuppage.cpp`
**C# Implementation:** New form

**Tasks:**
- [ ] Create `StartupPage.cs` Windows Form
- [ ] Create `StartupPage.Designer.cs` with UI layout
- [ ] Implement recent projects table (DataGridView)
- [ ] Add buttons: New Project, Open Project, Remove, Open, Cancel
- [ ] Load recent projects from ProjectDirectory
- [ ] Display project metadata (Name, Author, Version, Path)
- [ ] Handle double-click to open project
- [ ] Implement project removal from recent list
- [ ] Return selected project path or new project flag

**Files:**
- `Designer/StartupPage.cs` (new)
- `Designer/StartupPage.Designer.cs` (new)
- `Designer/StartupPage.resx` (new)

**Dependencies:**
- Project module (ProjectDirectory)

---

### 1.3 Main Window
**Qt Implementation:** `mainwindow.h`, `mainwindow.cpp`, `mainwindow.ui`
**C# Implementation:** `MainForm.cs` (exists, needs complete rebuild)

**Layout Structure:**
```
MainWindow
├── MenuBar (File, Edit, View, Build, Download, Upload, Simulate, Tools, Help)
├── StatusBar
└── CentralWidget (SplitContainer)
    ├── Left Panel (Project Explorer) - Fixed width 160-250px
    ├── Center Area (SplitContainer)
    │   ├── Top (Editor Tabs) - TabControl with closable tabs
    │   └── Bottom (Bottom Tabs) - TabControl
    │       ├── Output Tab (Console Output)
    │       ├── Debug Tab (Console Debug)
    │       └── Properties Tab (Property Editor)
    └── Right Panel (Components View) - Fixed width 200-300px
```

**Tasks:**
- [ ] Redesign `MainForm.Designer.cs` with proper layout
- [ ] Implement MenuStrip with all menu items
- [ ] Implement StatusStrip
- [ ] Create SplitContainer hierarchy for layout
- [ ] Add TabControl for editor tabs (closable)
- [ ] Add TabControl for bottom tabs (Output, Debug, Properties)
- [ ] Integrate ProjectView in left panel
- [ ] Integrate ComponentsView in right panel
- [ ] Integrate ConsoleModule in bottom tabs
- [ ] Integrate PropertyEditor in bottom tabs
- [ ] Handle tab closing with save prompts
- [ ] Implement menu actions and shortcuts
- [ ] Connect all module signals/events

**Files:**
- `Designer/MainForm.cs` (modify)
- `Designer/MainForm.Designer.cs` (modify)
- `Designer/MainForm.resx` (modify)

**Dependencies:**
- All DesignerModules

---

## 2. Core Modules

### 2.1 Project Module
**Qt Implementation:** `project_module/`
**C# Implementation:** `DesignerModules/Project/`

**Components:**

#### 2.1.1 ProjectDirectory
**Qt Files:** `projectdirectory.h`, `projectdirectory.cpp`
**Tasks:**
- [ ] Create `ProjectDirectory.cs` class
- [ ] Implement `GetDefaultProjectsPath()` static method
- [ ] Implement `GetRecentProjects()` - returns list of project paths
- [ ] Implement `AddRecentProject(string path)`
- [ ] Implement `RemoveRecentProject(string path)`
- [ ] Implement `GetProjectAuthor(string path)` - read from metadata
- [ ] Implement `GetProjectVersion(string path)` - read from metadata
- [ ] Handle project metadata file reading

**Files:**
- `DesignerModules/Project/ProjectDirectory.cs` (new)

---

#### 2.1.2 ProjectManager
**Qt Files:** `projectmanager.h`, `projectmanager.cpp`
**Tasks:**
- [ ] Create `ProjectManager.cs` class
- [ ] Implement project structure (Project, ScadaProject classes)
- [ ] Implement `CreateProject(string name)` - creates new project structure
- [ ] Implement `OpenProject(string path)` - loads project from disk
- [ ] Implement `SaveProject()` - saves project metadata and structure
- [ ] Implement `CloseProject()` - closes current project
- [ ] Implement `GetCurrentProject()` - returns active project
- [ ] Implement `GetCurrentScadaName()` - returns active SCADA name
- [ ] Implement `FindScadaProject(string name)` - finds SCADA project
- [ ] Implement `GetTagTables(string scadaName)` - returns tag tables
- [ ] Implement `GetTagTable(string scadaName, int id)` - returns specific tag table
- [ ] Implement `GetScreens(string scadaName)` - returns screens
- [ ] Implement `GetScripts(string scadaName)` - returns scripts
- [ ] Implement `GetCommunicationModules(string scadaName)` - returns comm modules
- [ ] Implement `UpdateCommunicationModules(string scadaName, List<CommunicationModules> modules)`
- [ ] Implement `GetAlarms(string scadaName)` - returns alarms
- [ ] Implement `GetSchedules(string scadaName)` - returns schedules
- [ ] Implement `GetHistorian(string scadaName)` - returns historian
- [ ] Implement `GetSecurity(string scadaName)` - returns security
- [ ] Implement `GetMachineLearning(string scadaName)` - returns ML config
- [ ] Implement `AddMachineLearning(string scadaName)` - creates new ML config
- [ ] Implement `AddHistorian(string scadaName)` - creates new historian
- [ ] Implement `GetDeviceNetwork()` - returns device network
- [ ] Implement `IncrementScadaProjectVersion(string scadaName)` - increments version
- [ ] Implement `GetRecentProjects()` - returns recent projects list
- [ ] Handle project file structure (.isc files)
- [ ] Handle SCADA project paths (buildPath, sourcePath, etc.)

**Files:**
- `DesignerModules/Project/ProjectManager.cs` (new)
- `DesignerModules/Project/Project.cs` (new) - Project data class
- `DesignerModules/Project/ScadaProject.cs` (new) - SCADA project data class

---

#### 2.1.3 ProjectView
**Qt Files:** `projectview.h`, `projectview.cpp`, `projectview.ui`
**Tasks:**
- [ ] Create `ProjectView.cs` UserControl
- [ ] Create `ProjectView.Designer.cs` with TreeView
- [ ] Implement tree structure showing:
  - Project root
    - SCADA Projects
      - SCADA Project 1
        - Screens folder
          - Screen items
        - Scripts folder
          - Script items
        - Tags folder
          - Tag table items
        - Communication Modules
        - Alarms
        - Schedules
        - Historian
        - Security
        - Machine Learning
    - Device Network
    - Project Settings
- [ ] Implement context menus for each item type
- [ ] Implement double-click handlers to open editors
- [ ] Implement `SetProjectManager(ProjectManager manager)`
- [ ] Implement `RefreshView()` - updates tree view
- [ ] Handle item selection and opening
- [ ] Emit events for opening screens, scripts, tag tables, etc.
- [ ] Add icons for different item types

**Files:**
- `DesignerModules/Project/ProjectView.cs` (new)
- `DesignerModules/Project/ProjectView.Designer.cs` (new)
- `DesignerModules/Project/ProjectView.resx` (new)

**Events:**
- `ScreenOpenRequested(ScreenTemplate screen, string scadaName)`
- `TagTableOpenRequested(TagTable tagTable, string scadaName)`
- `AllTagTablesOpenRequested(List<TagTable> tagTables, string scadaName)`
- `ScriptOpenRequested(LuaScript script, string scadaName)`
- `CommunicationModuleOpenRequested(CommunicationModules module, string scadaName)`
- `AlarmsOpenRequested(Alarms alarms, string scadaName)`
- `SchedulesOpenRequested(Schedules schedules, string scadaName)`
- `HistorianOpenRequested(Historian historian, string scadaName)`
- `SecurityOpenRequested(Security security, string scadaName)`
- `MachineLearningOpenRequested(MachineLearning ml, string scadaName)`
- `DeviceNetworkOpenRequested()`
- `SettingsOpenRequested()`

---

#### 2.1.4 SettingsEditor
**Qt Files:** `settingseditor.h`, `settingseditor.cpp`, `settingseditor.ui`
**Tasks:**
- [ ] Create `SettingsEditor.cs` UserControl
- [ ] Create `SettingsEditor.Designer.cs` with form controls
- [ ] Implement project settings editing (name, author, version, etc.)
- [ ] Implement `SetProjectManager(ProjectManager manager)`
- [ ] Load and save project settings
- [ ] Handle validation

**Files:**
- `DesignerModules/Project/SettingsEditor.cs` (new)
- `DesignerModules/Project/SettingsEditor.Designer.cs` (new)
- `DesignerModules/Project/SettingsEditor.resx` (new)

---

#### 2.1.5 DeviceNetworkEditor
**Qt Files:** `devicenetworkeditor.h`, `devicenetworkeditor.cpp`, `devicenetworkeditor.ui`
**Tasks:**
- [ ] Create `DeviceNetworkEditor.cs` UserControl
- [ ] Create `DeviceNetworkEditor.Designer.cs` with network visualization
- [ ] Implement device network tree/grid view
- [ ] Implement device addition/removal
- [ ] Implement device configuration (IP addresses, ports, etc.)
- [ ] Implement `SetDeviceNetwork(DeviceNetwork network)`
- [ ] Implement `SetProjectManager(ProjectManager manager)`
- [ ] Handle network changes and save

**Files:**
- `DesignerModules/Project/DeviceNetworkEditor.cs` (new)
- `DesignerModules/Project/DeviceNetworkEditor.Designer.cs` (new)
- `DesignerModules/Project/DeviceNetworkEditor.resx` (new)

**Dependencies:**
- DeviceNetwork model class

---

#### 2.1.6 ProjectValidator
**Qt Files:** `projectvalidator.h`, `projectvalidator.cpp`
**Tasks:**
- [ ] Create `ProjectValidator.cs` class
- [ ] Implement project validation logic
- [ ] Validate project structure
- [ ] Validate references (tags, screens, etc.)
- [ ] Return validation results with errors/warnings

**Files:**
- `DesignerModules/Project/ProjectValidator.cs` (new)

---

#### 2.1.7 ProjectReconstructor
**Qt Files:** `projectreconstructor.h`, `projectreconstructor.cpp`
**Tasks:**
- [ ] Create `ProjectReconstructor.cs` class
- [ ] Implement project reconstruction from runtime files
- [ ] Used for upload from device functionality
- [ ] Reconstruct project structure from compiled files

**Files:**
- `DesignerModules/Project/ProjectReconstructor.cs` (new)

---

#### 2.1.8 ProjectTemplates
**Qt Files:** `projecttemplates.h`, `projecttemplates.cpp`
**Tasks:**
- [ ] Create `ProjectTemplates.cs` class
- [ ] Implement project template system
- [ ] Provide default project templates
- [ ] Handle template creation and loading

**Files:**
- `DesignerModules/Project/ProjectTemplates.cs` (new)

---

#### 2.1.9 ScadaProjectDialog
**Qt Files:** `scadaprojectdialog.h`, `scadaprojectdialog.cpp`, `scadaprojectdialog.ui`
**Tasks:**
- [ ] Create `ScadaProjectDialog.cs` Form
- [ ] Create `ScadaProjectDialog.Designer.cs` with dialog controls
- [ ] Implement SCADA project creation dialog
- [ ] Collect SCADA project name, type, and settings
- [ ] Return created SCADA project configuration

**Files:**
- `DesignerModules/Project/ScadaProjectDialog.cs` (new)
- `DesignerModules/Project/ScadaProjectDialog.Designer.cs` (new)
- `DesignerModules/Project/ScadaProjectDialog.resx` (new)

---

#### 2.1.10 IPAddressDialog
**Qt Files:** `ipaddressdialog.h`, `ipaddressdialog.cpp`
**Tasks:**
- [ ] Create `IPAddressDialog.cs` Form
- [ ] Create `IPAddressDialog.Designer.cs` with IP input controls
- [ ] Implement IP address input and validation
- [ ] Return validated IP address

**Files:**
- `DesignerModules/Project/IPAddressDialog.cs` (new)
- `DesignerModules/Project/IPAddressDialog.Designer.cs` (new)
- `DesignerModules/Project/IPAddressDialog.resx` (new)

---

#### 2.1.11 ValidationDialog
**Qt Files:** `validationdialog.h`, `validationdialog.cpp`
**Tasks:**
- [ ] Create `ValidationDialog.cs` Form
- [ ] Create `ValidationDialog.Designer.cs` with results display
- [ ] Display validation results (errors, warnings)
- [ ] Allow navigation to problematic items

**Files:**
- `DesignerModules/Project/ValidationDialog.cs` (new)
- `DesignerModules/Project/ValidationDialog.Designer.cs` (new)
- `DesignerModules/Project/ValidationDialog.resx` (new)

---

### 2.2 Screen Editor Module
**Qt Implementation:** `screens_module/`
**C# Implementation:** `DesignerModules/ScreenEditor/`

**Components:**

#### 2.2.1 ScreenEditor
**Qt Files:** `screeneditor.h`, `screeneditor.cpp`, `screeneditor.ui`
**Tasks:**
- [ ] Create `ScreenEditor.cs` UserControl
- [ ] Create `ScreenEditor.Designer.cs` with graphics canvas
- [ ] Implement graphics scene using GDI+ or similar
- [ ] Implement component placement and manipulation
- [ ] Implement selection, move, resize operations
- [ ] Implement grid and snap-to-grid
- [ ] Implement zoom controls
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Implement `SetTemplate(ScreenTemplate template)`
- [ ] Implement `SaveScreen()` - saves screen to file
- [ ] Implement `IsModified()` - tracks modification state
- [ ] Implement `GetScene()` - returns graphics scene for property editor
- [ ] Handle component selection changes
- [ ] Connect to property editor for component editing
- [ ] Support undo/redo operations

**Files:**
- `DesignerModules/ScreenEditor/ScreenEditor.cs` (new)
- `DesignerModules/ScreenEditor/ScreenEditor.Designer.cs` (new)
- `DesignerModules/ScreenEditor/ScreenEditor.resx` (new)

**Dependencies:**
- Components module (for component types)
- TagEngine (for tag binding)

---

#### 2.2.2 ScreenTemplate
**Qt Files:** `screentemplate.h`, `screentemplate.cpp`
**Tasks:**
- [ ] Create `ScreenTemplate.cs` class
- [ ] Implement screen template data structure
- [ ] Store screen properties (name, size, components, etc.)
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/ScreenEditor/ScreenTemplate.cs` (new)

---

### 2.3 Script Editor Module
**Qt Implementation:** `script_module/`
**C# Implementation:** `DesignerModules/ScriptEditor/`

**Components:**

#### 2.3.1 ScriptEditor
**Qt Files:** `scripteditor.h`, `scripteditor.cpp`, `scripteditor.ui`
**Tasks:**
- [ ] Create `ScriptEditor.cs` UserControl
- [ ] Create `ScriptEditor.Designer.cs` with RichTextBox or ScintillaNET
- [ ] Implement Lua script editing
- [ ] Implement syntax highlighting (Lua)
- [ ] Implement code completion/intellisense
- [ ] Implement `SetScript(LuaScript script)`
- [ ] Implement `IsModified()` - tracks modification state
- [ ] Implement `MaybeSave()` - prompts to save if modified
- [ ] Implement `UpdateAvailableTags(List<string> tagNames)` - updates autocomplete
- [ ] Handle script saving
- [ ] Connect to tag table changes for tag updates

**Files:**
- `DesignerModules/ScriptEditor/ScriptEditor.cs` (new)
- `DesignerModules/ScriptEditor/ScriptEditor.Designer.cs` (new)
- `DesignerModules/ScriptEditor/ScriptEditor.resx` (new)

**Dependencies:**
- Lua scripting engine (consider using NLua or similar)
- TagEngine (for tag references)

---

#### 2.3.2 LuaScript
**Qt Files:** `luascript.h`, `luascript.cpp`
**Tasks:**
- [ ] Create `LuaScript.cs` class
- [ ] Implement Lua script data structure
- [ ] Store script properties (name, content, etc.)
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/ScriptEditor/LuaScript.cs` (new)

---

### 2.4 Components Module
**Qt Implementation:** `components_module/`
**C# Implementation:** `DesignerModules/Components/`

**Components:**

#### 2.4.1 ComponentsView
**Qt Files:** `componentsview.h`, `componentsview.cpp`, `componentsview.ui`
**Tasks:**
- [ ] Create `ComponentsView.cs` UserControl
- [ ] Create `ComponentsView.Designer.cs` with component palette
- [ ] Implement component library display (ListBox or TreeView)
- [ ] Show available components (Button, TextLabel, Gauge, etc.)
- [ ] Implement drag-and-drop to screen editor
- [ ] Implement `SetComponentsModule(ComponentsModule module)`
- [ ] Display component icons and names
- [ ] Handle component selection

**Files:**
- `DesignerModules/Components/ComponentsView.cs` (new)
- `DesignerModules/Components/ComponentsView.Designer.cs` (new)
- `DesignerModules/Components/ComponentsView.resx` (new)

---

#### 2.4.2 PropertyEditor
**Qt Files:** `propertyeditor.h`, `propertyeditor.cpp`, `propertyeditor.ui`
**Tasks:**
- [ ] Create `PropertyEditor.cs` UserControl
- [ ] Create `PropertyEditor.Designer.cs` with PropertyGrid
- [ ] Implement property editing for selected components
- [ ] Implement `UpdateEditor(object item)` - updates properties for selected item
- [ ] Implement `SetAvailableTagTables(List<TagTable> tables)` - for tag binding
- [ ] Implement `SetAvailableScreens(List<ScreenTemplate> screens)` - for screen navigation
- [ ] Implement `SetScadaProject(ScadaProject project)` - for context
- [ ] Handle property changes and update components
- [ ] Emit `RequestAutoSave` event when properties change
- [ ] Support different property types (text, number, color, tag binding, etc.)

**Files:**
- `DesignerModules/Components/PropertyEditor.cs` (new)
- `DesignerModules/Components/PropertyEditor.Designer.cs` (new)
- `DesignerModules/Components/PropertyEditor.resx` (new)

**Dependencies:**
- TagEngine (for tag property editing)
- ScreenEditor (for screen navigation properties)

---

#### 2.4.3 ComponentsModule
**Qt Files:** `components_module.h`, `components_module.cpp`
**Tasks:**
- [ ] Create `ComponentsModule.cs` class
- [ ] Implement component registration system
- [ ] Register all available components
- [ ] Provide component creation methods
- [ ] Manage component metadata

**Files:**
- `DesignerModules/Components/ComponentsModule.cs` (new)

---

#### 2.4.4 Component Types
**Qt Implementation:** QML components in `components/` folder
**Tasks:**
- [ ] Create C# classes for each component type:
  - [ ] ButtonComponent
  - [ ] TextLabelComponent
  - [ ] TextInputComponent
  - [ ] CheckboxComponent
  - [ ] RadioButtonComponent
  - [ ] SliderComponent
  - [ ] ProgressBarComponent
  - [ ] GaugeViewComponent
  - [ ] TrendViewComponent
  - [ ] AlarmViewComponent
  - [ ] TableComponent
  - [ ] ImageViewComponent
  - [ ] DateTimeComponent
  - [ ] IndicatorComponent
  - [ ] MotorComponent
  - [ ] PumpComponent
  - [ ] TankComponent
  - [ ] ConveyorComponent
  - [ ] LineComponent
  - [ ] RectangleComponent
  - [ ] TriangleComponent
  - [ ] PopupComponent
  - [ ] TabComponent
  - [ ] SpinnerComponent
  - [ ] ToggleSwitchComponent
- [ ] Each component should:
  - Inherit from base component class
  - Implement rendering logic
  - Implement property system
  - Support tag binding
  - Support events/actions

**Files:**
- `DesignerModules/Components/Components/BaseComponent.cs` (new)
- `DesignerModules/Components/Components/ButtonComponent.cs` (new)
- `DesignerModules/Components/Components/TextLabelComponent.cs` (new)
- ... (one file per component type)

---

### 2.5 Console Module
**Qt Implementation:** `console_module/`
**C# Implementation:** `DesignerModules/Console/`

**Components:**

#### 2.5.1 ConsoleModule
**Qt Files:** `console_module.h`, `console_module.cpp`
**Tasks:**
- [ ] Create `ConsoleModule.cs` class
- [ ] Implement `GetOutputConsole()` - returns output console control
- [ ] Implement `GetDebugConsole()` - returns debug console control
- [ ] Implement `HandleCompilerMessage(string message)` - displays compiler output
- [ ] Implement `HandleCompilerError(string error)` - displays compiler errors
- [ ] Implement `HandleCompilerWarning(string warning)` - displays compiler warnings
- [ ] Provide text output controls for both consoles
- [ ] Support colored output (errors in red, warnings in yellow, etc.)
- [ ] Support clear functionality

**Files:**
- `DesignerModules/Console/ConsoleModule.cs` (new)

**Dependencies:**
- Compiler module (for compiler messages)

---

### 2.6 Compiler Module
**Qt Implementation:** `compiler_module/`
**C# Implementation:** `DesignerModules/Compiler/`

**Components:**

#### 2.6.1 CompilerModule
**Qt Files:** `compiler_module.h`, `compiler_module.cpp`
**Tasks:**
- [ ] Create `CompilerModule.cs` class
- [ ] Implement `SetProject(ScadaProject project)` - sets project to compile
- [ ] Implement `CompileProject()` - compiles SCADA project
- [ ] Implement `CleanProject()` - cleans build output
- [ ] Emit compilation messages, errors, warnings events
- [ ] Handle compilation process
- [ ] Generate compiled output files (.iscr)
- [ ] Increment version during build
- [ ] Validate project before compilation

**Files:**
- `DesignerModules/Compiler/CompilerModule.cs` (new)

**Events:**
- `Message(string message)`
- `Error(string error)`
- `Warning(string warning)`

**Dependencies:**
- Project module (for project structure)
- Console module (for output)

---

### 2.7 Communication Module
**Qt Implementation:** `communication_module/`
**C# Implementation:** `DesignerModules/Communication/`

**Components:**

#### 2.7.1 CommunicationModuleEditor
**Qt Files:** `communicationmoduleeditor.h`, `communicationmoduleeditor.cpp`, `communicationmoduleeditor.ui`
**Tasks:**
- [ ] Create `CommunicationModuleEditor.cs` UserControl
- [ ] Create `CommunicationModuleEditor.Designer.cs` with module list and configuration
- [ ] Implement communication module list display
- [ ] Implement module addition/removal
- [ ] Implement module configuration (Modbus, OPC UA, etc.)
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Implement `SetProjectManager(ProjectManager manager)`
- [ ] Implement `SetScadaName(string scadaName)`
- [ ] Implement `SetCommunicationModules(List<CommunicationModules> modules)`
- [ ] Implement `GetCommunicationModules()` - returns current modules
- [ ] Implement `SaveCommunicationConfig()` - saves configuration
- [ ] Handle module modifications
- [ ] Emit module change events

**Files:**
- `DesignerModules/Communication/CommunicationModuleEditor.cs` (new)
- `DesignerModules/Communication/CommunicationModuleEditor.Designer.cs` (new)
- `DesignerModules/Communication/CommunicationModuleEditor.resx` (new)

**Events:**
- `ModuleModified()`
- `ModulesChanged()`

---

#### 2.7.2 ModbusSettingsDialog
**Qt Files:** `modbussettingsdialog.h`, `modbussettingsdialog.cpp`, `modbussettingsdialog.ui`
**Tasks:**
- [ ] Create `ModbusSettingsDialog.cs` Form
- [ ] Create `ModbusSettingsDialog.Designer.cs` with Modbus configuration
- [ ] Implement Modbus connection settings (IP, port, slave ID, etc.)
- [ ] Validate Modbus settings
- [ ] Return configured Modbus settings

**Files:**
- `DesignerModules/Communication/ModbusSettingsDialog.cs` (new)
- `DesignerModules/Communication/ModbusSettingsDialog.Designer.cs` (new)
- `DesignerModules/Communication/ModbusSettingsDialog.resx` (new)

---

#### 2.7.3 OPCUAConnectionDialog
**Qt Files:** `opcuaconnectiondialog.h`, `opcuaconnectiondialog.cpp`
**Tasks:**
- [ ] Create `OPCUAConnectionDialog.cs` Form
- [ ] Create `OPCUAConnectionDialog.Designer.cs` with OPC UA configuration
- [ ] Implement OPC UA connection settings (endpoint URL, security, etc.)
- [ ] Validate OPC UA settings
- [ ] Return configured OPC UA settings

**Files:**
- `DesignerModules/Communication/OPCUAConnectionDialog.cs` (new)
- `DesignerModules/Communication/OPCUAConnectionDialog.Designer.cs` (new)
- `DesignerModules/Communication/OPCUAConnectionDialog.resx` (new)

---

#### 2.7.4 CommunicationModules
**Qt Files:** `communicationmodules.h`, `communicationmodules.cpp`
**Tasks:**
- [ ] Create `CommunicationModules.cs` class
- [ ] Implement communication module data structure
- [ ] Store module type, settings, connections
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/Communication/CommunicationModules.cs` (new)

---

### 2.8 Alarms Module
**Qt Implementation:** `alarms_module/`
**C# Implementation:** `DesignerModules/Alarms/`

**Components:**

#### 2.8.1 AlarmsEditor
**Qt Files:** `alarmseditor.h`, `alarmseditor.cpp`, `alarmseditor.ui`
**Tasks:**
- [ ] Create `AlarmsEditor.cs` UserControl
- [ ] Create `AlarmsEditor.Designer.cs` with alarm configuration grid
- [ ] Implement alarm list display (DataGridView)
- [ ] Implement alarm addition/removal
- [ ] Implement alarm configuration (tag, condition, priority, message, etc.)
- [ ] Implement `SetAlarms(Alarms alarms)`
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Handle alarm modifications
- [ ] Save alarm configuration

**Files:**
- `DesignerModules/Alarms/AlarmsEditor.cs` (new)
- `DesignerModules/Alarms/AlarmsEditor.Designer.cs` (new)
- `DesignerModules/Alarms/AlarmsEditor.resx` (new)

**Dependencies:**
- TagEngine (for tag selection)

---

#### 2.8.2 Alarms
**Qt Files:** `alarms.h`, `alarms.cpp`
**Tasks:**
- [ ] Create `Alarms.cs` class
- [ ] Implement alarms data structure
- [ ] Store alarm definitions
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/Alarms/Alarms.cs` (new)

---

### 2.9 Scheduler Module
**Qt Implementation:** `schedules_module/`
**C# Implementation:** `DesignerModules/Scheduler/`

**Components:**

#### 2.9.1 ScheduleEditor
**Qt Files:** `scheduleeditor.h`, `scheduleeditor.cpp`, `scheduleeditor.ui`
**Tasks:**
- [ ] Create `ScheduleEditor.cs` UserControl
- [ ] Create `ScheduleEditor.Designer.cs` with schedule configuration
- [ ] Implement schedule list display
- [ ] Implement schedule addition/removal
- [ ] Implement schedule configuration (time, script, recurrence, etc.)
- [ ] Implement `SetSchedules(Schedules schedules)`
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Handle schedule modifications
- [ ] Save schedule configuration

**Files:**
- `DesignerModules/Scheduler/ScheduleEditor.cs` (new)
- `DesignerModules/Scheduler/ScheduleEditor.Designer.cs` (new)
- `DesignerModules/Scheduler/ScheduleEditor.resx` (new)

---

#### 2.9.2 ScriptSelectionDialog
**Qt Files:** `scriptselectiondialog.h`, `scriptselectiondialog.cpp`, `scriptselectiondialog.ui`
**Tasks:**
- [ ] Create `ScriptSelectionDialog.cs` Form
- [ ] Create `ScriptSelectionDialog.Designer.cs` with script list
- [ ] Display available scripts for selection
- [ ] Return selected script

**Files:**
- `DesignerModules/Scheduler/ScriptSelectionDialog.cs` (new)
- `DesignerModules/Scheduler/ScriptSelectionDialog.Designer.cs` (new)
- `DesignerModules/Scheduler/ScriptSelectionDialog.resx` (new)

---

#### 2.9.3 Schedules
**Qt Files:** `schedules.h`, `schedules.cpp`
**Tasks:**
- [ ] Create `Schedules.cs` class
- [ ] Implement schedules data structure
- [ ] Store schedule definitions
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/Scheduler/Schedules.cs` (new)

---

### 2.10 Historian Module
**Qt Implementation:** `historian_module/`
**C# Implementation:** `DesignerModules/Historian/`

**Components:**

#### 2.10.1 HistorianEditor
**Qt Files:** `historianeditor.h`, `historianeditor.cpp`, `historianeditor.ui`
**Tasks:**
- [ ] Create `HistorianEditor.cs` UserControl
- [ ] Create `HistorianEditor.Designer.cs` with historian configuration
- [ ] Implement tag selection for historical logging
- [ ] Implement logging interval configuration
- [ ] Implement storage settings
- [ ] Implement `SetHistorian(Historian historian)`
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Handle historian modifications
- [ ] Save historian configuration

**Files:**
- `DesignerModules/Historian/HistorianEditor.cs` (new)
- `DesignerModules/Historian/HistorianEditor.Designer.cs` (new)
- `DesignerModules/Historian/HistorianEditor.resx` (new)

**Dependencies:**
- TagEngine (for tag selection)

---

#### 2.10.2 Historian
**Qt Files:** `historian.h`, `historian.cpp`
**Tasks:**
- [ ] Create `Historian.cs` class
- [ ] Implement historian data structure
- [ ] Store historian configuration
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/Historian/Historian.cs` (new)

---

### 2.11 Security Module
**Qt Implementation:** `security_module/`
**C# Implementation:** `DesignerModules/Security/`

**Components:**

#### 2.11.1 SecurityEditor
**Qt Files:** `securityeditor.h`, `securityeditor.cpp`, `securityeditor.ui`
**Tasks:**
- [ ] Create `SecurityEditor.cs` UserControl
- [ ] Create `SecurityEditor.Designer.cs` with security configuration
- [ ] Implement user management (add, remove, edit users)
- [ ] Implement role management
- [ ] Implement permission configuration
- [ ] Implement `SetSecurity(Security security)`
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Handle security modifications
- [ ] Save security configuration

**Files:**
- `DesignerModules/Security/SecurityEditor.cs` (new)
- `DesignerModules/Security/SecurityEditor.Designer.cs` (new)
- `DesignerModules/Security/SecurityEditor.resx` (new)

---

#### 2.11.2 Security
**Qt Files:** `security.h`, `security.cpp`
**Tasks:**
- [ ] Create `Security.cs` class
- [ ] Implement security data structure
- [ ] Store users, roles, permissions
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/Security/Security.cs` (new)

---

### 2.12 Machine Learning Module
**Qt Implementation:** `machine_learning/`
**C# Implementation:** `DesignerModules/MachineLearning/`

**Components:**

#### 2.12.1 MachineLearningEditor
**Qt Files:** `machinelearningeditor.h`, `machinelearningeditor.cpp`, `machinelearningeditor.ui`
**Tasks:**
- [ ] Create `MachineLearningEditor.cs` UserControl
- [ ] Create `MachineLearningEditor.Designer.cs` with ML configuration
- [ ] Implement ML model configuration
- [ ] Implement training data selection
- [ ] Implement model parameters
- [ ] Implement `SetMachineLearning(MachineLearning ml)`
- [ ] Implement `SetScadaProject(ScadaProject project)`
- [ ] Handle ML modifications
- [ ] Save ML configuration

**Files:**
- `DesignerModules/MachineLearning/MachineLearningEditor.cs` (new)
- `DesignerModules/MachineLearning/MachineLearningEditor.Designer.cs` (new)
- `DesignerModules/MachineLearning/MachineLearningEditor.resx` (new)

**Dependencies:**
- TagEngine (for tag selection for training data)

---

#### 2.12.2 MachineLearning
**Qt Files:** `machinelearning.h`, `machinelearning.cpp`
**Tasks:**
- [ ] Create `MachineLearning.cs` class
- [ ] Implement ML data structure
- [ ] Store ML configuration
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/MachineLearning/MachineLearning.cs` (new)

---

### 2.13 Tag Engine Module
**Qt Implementation:** `tags_module/`
**C# Implementation:** `DesignerModules/TagEngine/`

**Components:**

#### 2.13.1 TagTableEditor
**Qt Implementation:** Referenced in mainwindow.cpp
**Tasks:**
- [ ] Create `TagTableEditor.cs` UserControl
- [ ] Create `TagTableEditor.Designer.cs` with tag table grid
- [ ] Implement tag table display (DataGridView)
- [ ] Implement tag addition/removal
- [ ] Implement tag editing (name, type, address, etc.)
- [ ] Implement `SetTagTable(TagTable table)` - single table mode
- [ ] Implement `SetTagTables(List<TagTable> tables)` - multiple tables mode
- [ ] Implement `IsModified()` - tracks modification state
- [ ] Handle tag modifications
- [ ] Support tag validation

**Files:**
- `DesignerModules/TagEngine/TagTableEditor.cs` (new)
- `DesignerModules/TagEngine/TagTableEditor.Designer.cs` (new)
- `DesignerModules/TagEngine/TagTableEditor.resx` (new)

---

#### 2.13.2 TagTable
**Tasks:**
- [ ] Create `TagTable.cs` class
- [ ] Implement tag table data structure
- [ ] Store tags collection
- [ ] Implement tag access methods
- [ ] Implement serialization/deserialization
- [ ] Emit modification events

**Files:**
- `DesignerModules/TagEngine/TagTable.cs` (new)

**Events:**
- `Modified()`

---

#### 2.13.3 Tag
**Tasks:**
- [ ] Create `Tag.cs` class
- [ ] Implement tag data structure
- [ ] Store tag properties (name, type, address, value, etc.)
- [ ] Implement serialization/deserialization

**Files:**
- `DesignerModules/TagEngine/Tag.cs` (new)

---

### 2.14 Simulator Module
**Qt Implementation:** `simulation_module/`
**C# Implementation:** `DesignerModules/Simulator/`

**Components:**

#### 2.14.1 SimulatorModule
**Qt Files:** `simulation_module.h`, `simulation_module.cpp`
**Tasks:**
- [ ] Create `SimulatorModule.cs` class
- [ ] Implement `StartSimulation(string projectPath, string options)` - starts simulation
- [ ] Implement `StopSimulation()` - stops simulation
- [ ] Implement `PauseSimulation()` - pauses simulation
- [ ] Implement `ResumeSimulation()` - resumes simulation
- [ ] Implement `IsPaused()` - checks if paused
- [ ] Implement `GetAutoBuildBeforeSimulate()` - gets auto-build setting
- [ ] Implement `ShowProjectSelectionDialog(Form parent, ProjectManager manager)` - shows project selection
- [ ] Implement `ShowSettingsDialog(Form parent)` - shows simulation settings
- [ ] Emit simulation state events
- [ ] Handle simulation process

**Files:**
- `DesignerModules/Simulator/SimulatorModule.cs` (new)

**Events:**
- `SimulationStarted()`
- `SimulationStopped()`
- `SimulationPaused()`
- `SimulationResumed()`
- `SimulationError(string error)`

**Dependencies:**
- Project module (for project selection)
- Compiler module (for auto-build)

---

### 2.15 Discovery Module
**Qt Implementation:** `device_discovery/`
**C# Implementation:** `DesignerModules/Discovery/`

**Components:**

#### 2.15.1 DeployDialog
**Qt Files:** `deploydialog.h`, `deploydialog.cpp`, `deploydialog.ui`
**Tasks:**
- [ ] Create `DeployDialog.cs` Form
- [ ] Create `DeployDialog.Designer.cs` with deployment configuration
- [ ] Implement device selection for deployment
- [ ] Implement deployment progress display
- [ ] Handle deployment process
- [ ] Show deployment results

**Files:**
- `DesignerModules/Discovery/DeployDialog.cs` (new)
- `DesignerModules/Discovery/DeployDialog.Designer.cs` (new)
- `DesignerModules/Discovery/DeployDialog.resx` (new)

**Dependencies:**
- Project module (for project manager)

---

#### 2.15.2 UploadDialog
**Qt Files:** `uploaddialog.h`, `uploaddialog.cpp`, `uploaddialog.ui`
**Tasks:**
- [ ] Create `UploadDialog.cs` Form
- [ ] Create `UploadDialog.Designer.cs` with upload configuration
- [ ] Implement device connection for upload
- [ ] Implement upload progress display
- [ ] Handle upload process
- [ ] Return uploaded project path
- [ ] Emit upload requested event

**Files:**
- `DesignerModules/Discovery/UploadDialog.cs` (new)
- `DesignerModules/Discovery/UploadDialog.Designer.cs` (new)
- `DesignerModules/Discovery/UploadDialog.resx` (new)

**Events:**
- `UploadRequested(string deviceIp, int devicePort, string saveDirectory)`

---

### 2.16 Tools Module
**Qt Implementation:** `tools_module/`
**C# Implementation:** `DesignerModules/Tools/`

**Components:**

#### 2.16.1 OptionsDialog
**Qt Files:** `optionsdialog.h`, `optionsdialog.cpp`, `optionsdialog.ui`
**Tasks:**
- [ ] Create `OptionsDialog.cs` Form
- [ ] Create `OptionsDialog.Designer.cs` with options tabs
- [ ] Implement theme selection (Light, Dark, System)
- [ ] Implement `GetCurrentTheme()` - gets saved theme
- [ ] Implement `ApplyTheme(Theme theme, Application app)` - applies theme
- [ ] Implement other application options
- [ ] Save options to settings

**Files:**
- `DesignerModules/Tools/OptionsDialog.cs` (new)
- `DesignerModules/Tools/OptionsDialog.Designer.cs` (new)
- `DesignerModules/Tools/OptionsDialog.resx` (new)

**Events:**
- `ThemeChanged(Theme theme)`

---

#### 2.16.2 CustomizeDialog
**Qt Files:** `customizedialog.h`, `customizedialog.cpp`
**Tasks:**
- [ ] Create `CustomizeDialog.cs` Form
- [ ] Create `CustomizeDialog.Designer.cs` with customization options
- [ ] Implement UI customization (toolbars, menus, shortcuts)
- [ ] Save customization settings

**Files:**
- `DesignerModules/Tools/CustomizeDialog.cs` (new)
- `DesignerModules/Tools/CustomizeDialog.Designer.cs` (new)
- `DesignerModules/Tools/CustomizeDialog.resx` (new)

---

#### 2.16.3 ExternalToolsDialog
**Qt Files:** `externaltoolsdialog.h`, `externaltoolsdialog.cpp`
**Tasks:**
- [ ] Create `ExternalToolsDialog.cs` Form
- [ ] Create `ExternalToolsDialog.Designer.cs` with external tools list
- [ ] Implement external tool management (add, remove, edit)
- [ ] Configure tool paths and arguments
- [ ] Save external tools configuration

**Files:**
- `DesignerModules/Tools/ExternalToolsDialog.cs` (new)
- `DesignerModules/Tools/ExternalToolsDialog.Designer.cs` (new)
- `DesignerModules/Tools/ExternalToolsDialog.resx` (new)

---

#### 2.16.4 PackageManagerDialog
**Qt Files:** `packagemanagerdialog.h`, `packagemanagerdialog.cpp`
**Tasks:**
- [ ] Create `PackageManagerDialog.cs` Form
- [ ] Create `PackageManagerDialog.Designer.cs` with package list
- [ ] Implement package browsing and installation
- [ ] Implement package management (install, uninstall, update)
- [ ] Display package information

**Files:**
- `DesignerModules/Tools/PackageManagerDialog.cs` (new)
- `DesignerModules/Tools/PackageManagerDialog.Designer.cs` (new)
- `DesignerModules/Tools/PackageManagerDialog.resx` (new)

---

### 2.17 Workspace Module
**Qt Implementation:** `workspace_module/`
**C# Implementation:** `DesignerModules/Workspace/`

**Components:**

#### 2.17.1 WorkspaceModule
**Tasks:**
- [ ] Create `WorkspaceModule.cs` class
- [ ] Implement workspace state management
- [ ] Save/restore window layouts
- [ ] Save/restore dock panel states
- [ ] Handle workspace persistence

**Files:**
- `DesignerModules/Workspace/WorkspaceModule.cs` (new)

---

### 2.18 Events Bus Module
**Qt Implementation:** `events_module/`
**C# Implementation:** `DesignerModules/EventsBus/`

**Components:**

#### 2.18.1 EventsBus
**Tasks:**
- [ ] Create `EventsBus.cs` class
- [ ] Implement event publishing/subscribing system
- [ ] Support cross-module communication
- [ ] Handle event routing

**Files:**
- `DesignerModules/EventsBus/EventsBus.cs` (new)

---

## 3. Main Window Menu Implementation

### 3.1 File Menu
**Tasks:**
- [ ] New Project (Ctrl+N) - calls `newProject()`
- [ ] Open Project (Ctrl+O) - calls `openProject()`
- [ ] Close Project (Ctrl+W) - calls `closeProject()`
- [ ] Save (Ctrl+S) - calls `saveProject()`
- [ ] Save As (Ctrl+Shift+S) - calls `saveProjectAs()`
- [ ] Save All - saves all open editors
- [ ] Rename (F2) - renames project/item
- [ ] Import - imports project/data
- [ ] Export - exports project/data
- [ ] Archive Project - archives project
- [ ] Restore Project - restores archived project
- [ ] Recent Projects - submenu with recent projects
- [ ] Exit (Alt+F4) - exits application

---

### 3.2 Edit Menu
**Tasks:**
- [ ] Undo (Ctrl+Z) - undo last action
- [ ] Redo (Ctrl+Y) - redo last action
- [ ] Cut (Ctrl+X) - cut selection
- [ ] Copy (Ctrl+C) - copy selection
- [ ] Paste (Ctrl+V) - paste selection
- [ ] Delete (Del) - delete selection

---

### 3.3 View Menu
**Tasks:**
- [ ] Project Explorer (toggle) - shows/hides project view
- [ ] Properties (toggle) - shows/hides property editor
- [ ] Output (toggle) - shows/hides output console
- [ ] Zoom In (Ctrl++) - zoom in active editor
- [ ] Zoom Out (Ctrl+-) - zoom out active editor
- [ ] Reset Zoom (Ctrl+0) - reset zoom to 100%

---

### 3.4 Build Menu
**Tasks:**
- [ ] Build Project (F7) - calls `buildProject()`
- [ ] Rebuild Project (Ctrl+F7) - rebuilds project
- [ ] Clean Project - calls `cleanProject()`
- [ ] Build Settings - opens build configuration

---

### 3.5 Download Menu
**Tasks:**
- [ ] Download to Device (F5) - calls `deployToDevice()`
- [ ] Download Settings - opens download configuration

---

### 3.6 Upload Menu
**Tasks:**
- [ ] Upload from Device (F6) - calls `uploadFromDevice()`
- [ ] Upload Settings - opens upload configuration

---

### 3.7 Simulate Menu
**Tasks:**
- [ ] Start Simulation (F9) - calls `startSimulation()`
- [ ] Pause Simulation (F10) - calls `pauseSimulation()`
- [ ] Stop Simulation (Shift+F9) - calls `stopSimulation()`
- [ ] Simulation Settings - calls `showSimulationSettings()`

---

### 3.8 Tools Menu
**Tasks:**
- [ ] Options - calls `showOptions()`
- [ ] Customize - calls `showCustomize()`
- [ ] External Tools - calls `showExternalTools()`
- [ ] Package Manager - calls `showPackageManager()`

---

### 3.9 Help Menu
**Tasks:**
- [ ] Documentation (F1) - opens documentation
- [ ] About - shows about dialog

---

## 4. Integration Points

### 4.1 Main Window Integration
**Tasks:**
- [ ] Integrate ProjectView in left panel
- [ ] Integrate ComponentsView in right panel
- [ ] Integrate ConsoleModule in bottom tabs
- [ ] Integrate PropertyEditor in bottom tabs
- [ ] Handle editor tab management
- [ ] Connect ProjectView events to editor opening
- [ ] Connect ScreenEditor selection to PropertyEditor
- [ ] Connect CompilerModule events to ConsoleModule
- [ ] Handle tab closing with save prompts
- [ ] Manage editor lifecycle (create, open, close, save)

---

### 4.2 Module Communication
**Tasks:**
- [ ] Implement event system for module communication
- [ ] Connect tag table changes to script editor tag updates
- [ ] Connect project changes to all editors
- [ ] Handle cross-module dependencies

---

## 5. Data Models and Serialization

### 5.1 Project File Format
**Tasks:**
- [ ] Define project file structure (.isc files)
- [ ] Implement project serialization (JSON or XML)
- [ ] Implement project deserialization
- [ ] Handle project versioning
- [ ] Handle project migration/upgrades

---

### 5.2 Component Serialization
**Tasks:**
- [ ] Implement component serialization for screens
- [ ] Implement component deserialization
- [ ] Handle component versioning

---

## 6. UI/UX Enhancements

### 6.1 Icons and Resources
**Tasks:**
- [ ] Convert Qt icons to Windows Forms compatible format
- [ ] Add icons for all menu items
- [ ] Add icons for project tree items
- [ ] Add icons for component palette
- [ ] Create application icon

---

### 6.2 Themes and Styling
**Tasks:**
- [ ] Implement light theme
- [ ] Implement dark theme
- [ ] Implement system theme detection
- [ ] Apply themes consistently across all forms
- [ ] Handle high DPI scaling

---

### 6.3 Layout Persistence
**Tasks:**
- [ ] Save window layout on close
- [ ] Restore window layout on open
- [ ] Save splitter positions
- [ ] Save dock panel states
- [ ] Handle multiple monitor scenarios

---

## 7. Testing and Validation

### 7.1 Unit Tests
**Tasks:**
- [ ] Create unit tests for ProjectManager
- [ ] Create unit tests for CompilerModule
- [ ] Create unit tests for data serialization
- [ ] Create unit tests for validation logic

---

### 7.2 Integration Tests
**Tasks:**
- [ ] Test project creation workflow
- [ ] Test project opening workflow
- [ ] Test editor opening and closing
- [ ] Test compilation workflow
- [ ] Test deployment workflow

---

### 7.3 UI Tests
**Tasks:**
- [ ] Test all menu actions
- [ ] Test all dialogs
- [ ] Test editor interactions
- [ ] Test drag-and-drop operations

---

## 8. Documentation

### 8.1 Code Documentation
**Tasks:**
- [ ] Add XML documentation comments to all public APIs
- [ ] Document module interfaces
- [ ] Document data structures
- [ ] Document event systems

---

### 8.2 User Documentation
**Tasks:**
- [ ] Update user manual for C# version
- [ ] Document new features
- [ ] Document migration from Qt version

---

## 9. Migration Priority

### Phase 1: Core Infrastructure (Weeks 1-2)
1. Main Window layout and structure
2. Project Module (ProjectManager, ProjectDirectory, ProjectView)
3. Startup Page
4. Basic menu system

### Phase 2: Editors (Weeks 3-4)
1. Tag Engine (TagTableEditor)
2. Script Editor
3. Screen Editor (basic)
4. Property Editor

### Phase 3: Components (Weeks 5-6)
1. Components Module
2. Components View
3. Basic component types
4. Component integration with Screen Editor

### Phase 4: Configuration Modules (Weeks 7-8)
1. Communication Module Editor
2. Alarms Editor
3. Schedules Editor
4. Historian Editor
5. Security Editor
6. Machine Learning Editor

### Phase 5: Build and Deploy (Weeks 9-10)
1. Compiler Module
2. Console Module
3. Discovery Module (Deploy/Upload)
4. Simulator Module

### Phase 6: Tools and Polish (Weeks 11-12)
1. Tools Module (Options, Customize, External Tools, Package Manager)
2. Workspace Module
3. Events Bus
4. Theme system
5. Icon resources
6. Testing and bug fixes

---

## 10. Technical Considerations

### 10.1 Graphics Rendering
- **Decision:** Use GDI+ or SkiaSharp for screen editor graphics
- **Consideration:** Performance for complex screens
- **Alternative:** Consider WPF for graphics-heavy components

### 10.2 Script Editing
- **Decision:** Use ScintillaNET or similar for syntax highlighting
- **Consideration:** Lua syntax support
- **Alternative:** Use AvalonEdit if available for .NET

### 10.3 Property Editing
- **Decision:** Use PropertyGrid control or custom implementation
- **Consideration:** Complex property types (tag binding, screen navigation)
- **Alternative:** Custom property editor for better control

### 10.4 Serialization
- **Decision:** Use JSON.NET or System.Text.Json
- **Consideration:** Performance and compatibility
- **Alternative:** XML for better readability

### 10.5 Theme System
- **Decision:** Use Windows Forms theming or custom implementation
- **Consideration:** Consistent theming across all controls
- **Alternative:** Consider third-party theme libraries

---

## 11. Dependencies

### 11.1 External Libraries
- [ ] JSON.NET or System.Text.Json (for serialization)
- [ ] ScintillaNET or AvalonEdit (for script editing)
- [ ] NLua or similar (for Lua scripting support)
- [ ] SkiaSharp or GDI+ (for graphics rendering)
- [ ] Optional: Third-party theme library

### 11.2 .NET Framework
- Target: .NET 8.0 Windows
- UI Framework: Windows Forms
- Language: C#

---

## 12. File Structure Summary

```
Designer/
├── Program.cs
├── StartupPage.cs
├── StartupPage.Designer.cs
├── StartupPage.resx
├── MainForm.cs
├── MainForm.Designer.cs
├── MainForm.resx
└── Designer.csproj

DesignerModules/
├── Project/
│   ├── ProjectDirectory.cs
│   ├── ProjectManager.cs
│   ├── Project.cs
│   ├── ScadaProject.cs
│   ├── ProjectView.cs
│   ├── ProjectView.Designer.cs
│   ├── SettingsEditor.cs
│   ├── DeviceNetworkEditor.cs
│   ├── ProjectValidator.cs
│   ├── ProjectReconstructor.cs
│   ├── ProjectTemplates.cs
│   ├── ScadaProjectDialog.cs
│   ├── IPAddressDialog.cs
│   └── ValidationDialog.cs
├── ScreenEditor/
│   ├── ScreenEditor.cs
│   ├── ScreenEditor.Designer.cs
│   └── ScreenTemplate.cs
├── ScriptEditor/
│   ├── ScriptEditor.cs
│   ├── ScriptEditor.Designer.cs
│   └── LuaScript.cs
├── Components/
│   ├── ComponentsModule.cs
│   ├── ComponentsView.cs
│   ├── PropertyEditor.cs
│   └── Components/ (various component types)
├── Console/
│   └── ConsoleModule.cs
├── Compiler/
│   └── CompilerModule.cs
├── Communication/
│   ├── CommunicationModuleEditor.cs
│   ├── CommunicationModules.cs
│   ├── ModbusSettingsDialog.cs
│   └── OPCUAConnectionDialog.cs
├── Alarms/
│   ├── AlarmsEditor.cs
│   └── Alarms.cs
├── Scheduler/
│   ├── ScheduleEditor.cs
│   ├── Schedules.cs
│   └── ScriptSelectionDialog.cs
├── Historian/
│   ├── HistorianEditor.cs
│   └── Historian.cs
├── Security/
│   ├── SecurityEditor.cs
│   └── Security.cs
├── MachineLearning/
│   ├── MachineLearningEditor.cs
│   └── MachineLearning.cs
├── TagEngine/
│   ├── TagTableEditor.cs
│   ├── TagTable.cs
│   └── Tag.cs
├── Simulator/
│   └── SimulatorModule.cs
├── Discovery/
│   ├── DeployDialog.cs
│   └── UploadDialog.cs
├── Tools/
│   ├── OptionsDialog.cs
│   ├── CustomizeDialog.cs
│   ├── ExternalToolsDialog.cs
│   └── PackageManagerDialog.cs
├── Workspace/
│   └── WorkspaceModule.cs
└── EventsBus/
    └── EventsBus.cs
```

---

## Conclusion

This migration plan provides a comprehensive roadmap for converting the AccuTrack Designer from Qt to C# Windows Forms. The plan is organized by modules and includes detailed tasks for each component. Following this plan systematically will ensure a complete and functional migration while preserving all features from the original Qt implementation.

**Estimated Timeline:** 12 weeks (3 months) for complete migration
**Team Size:** 2-3 developers recommended
**Priority:** Follow phases sequentially, but some tasks can be parallelized within phases
