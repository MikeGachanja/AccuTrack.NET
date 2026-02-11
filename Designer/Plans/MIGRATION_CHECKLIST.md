# AccuTrack Designer Migration Checklist

This checklist tracks progress on migrating from Qt to C# Windows Forms.

## Phase 1: Core Infrastructure

### Main Application
- [x] Enhance Program.cs with startup configuration
- [x] Create StartupPage form
- [x] Implement StartupPage UI (Designer)
- [x] Implement recent projects loading
- [x] Implement project selection logic

### Main Window
- [x] Redesign MainForm.Designer.cs layout
- [x] Implement MenuStrip with all menus
- [x] Implement StatusStrip
- [x] Create SplitContainer hierarchy
- [x] Add TabControl for editor tabs
- [x] Add TabControl for bottom tabs
- [x] Connect menu actions

### Project Module - Core
- [x] ProjectDirectory.cs - GetDefaultProjectsPath
- [x] ProjectDirectory.cs - GetRecentProjects
- [x] ProjectDirectory.cs - SetLastOpenedProject (AddRecentProject equivalent)
- [x] ProjectDirectory.cs - RemoveRecentProject
- [x] ProjectDirectory.cs - GetProjectAuthor
- [x] ProjectDirectory.cs - GetProjectVersion

### Project Module - ProjectManager
- [x] ProjectManager.cs - CreateProject
- [x] ProjectManager.cs - OpenProject
- [x] ProjectManager.cs - SaveProject
- [x] ProjectManager.cs - CloseProject
- [x] ProjectManager.cs - GetCurrentProject
- [x] ProjectManager.cs - GetCurrentScadaName
- [x] ProjectManager.cs - FindScadaProject
- [x] ProjectManager.cs - GetTagTables (placeholder)
- [x] ProjectManager.cs - GetTagTable (placeholder)
- [x] ProjectManager.cs - GetScreens (placeholder)
- [x] ProjectManager.cs - GetScripts (placeholder)
- [x] ProjectManager.cs - GetCommunicationModules (placeholder)
- [x] ProjectManager.cs - UpdateCommunicationModules (placeholder)
- [x] ProjectManager.cs - GetAlarms (placeholder)
- [x] ProjectManager.cs - GetSchedules (placeholder)
- [x] ProjectManager.cs - GetHistorian (placeholder)
- [x] ProjectManager.cs - GetSecurity (placeholder)
- [x] ProjectManager.cs - GetMachineLearning (placeholder)
- [x] ProjectManager.cs - AddMachineLearning (placeholder)
- [x] ProjectManager.cs - AddHistorian (placeholder)
- [x] ProjectManager.cs - GetDeviceNetwork
- [x] ProjectManager.cs - IncrementScadaProjectVersion
- [x] Project.cs - Project data class
- [x] ScadaProject.cs - ScadaProject data class

### Project Module - ProjectView
- [x] ProjectView.cs - UserControl creation
- [x] ProjectView.cs - TreeView layout (integrated in code)
- [x] ProjectView.cs - Tree structure implementation
- [x] ProjectView.cs - Context menus
- [x] ProjectView.cs - Double-click handlers
- [x] ProjectView.cs - SetProjectManager
- [x] ProjectView.cs - RefreshView
- [x] ProjectView.cs - Event emissions

### Project Module - Dialogs
- [x] SettingsEditor.cs - Form creation
- [x] SettingsEditor.cs - UI layout (integrated in code)
- [x] SettingsEditor.cs - Settings editing logic
- [x] DeviceNetworkEditor.cs - Form creation
- [x] DeviceNetworkEditor.cs - UI layout (integrated in code)
- [x] DeviceNetworkEditor.cs - Network editing logic (basic)
- [x] ProjectValidator.cs - Validation logic
- [x] ProjectReconstructor.cs - Reconstruction logic
- [x] ProjectTemplates.cs - Template system
- [x] ScadaProjectDialog.cs - Dialog creation
- [x] IPAddressDialog.cs - Dialog creation
- [x] ValidationDialog.cs - Dialog creation

---

## Phase 2: Editors

**Review (incomplete):** Only Script Editor syntax highlighting is optional; all other Phase 2 items are complete.

### Tag Engine
- [x] Tag.cs - Tag data class
- [x] TagTable.cs - TagTable data class
- [x] TagTableEditor.cs - UserControl creation
- [x] TagTableEditor.cs - DataGridView layout (integrated in code)
- [x] TagTableEditor.cs - Tag editing logic
- [x] TagTableEditor.cs - SetTagTable
- [x] TagTableEditor.cs - SetTagTables
- [x] TagTableEditor.cs - IsModified

### Script Editor
- [x] LuaScript.cs - Script data class
- [x] ScriptEditor.cs - UserControl creation
- [x] ScriptEditor.cs - TextBox layout (basic, can upgrade to ScintillaNET)
- [ ] ScriptEditor.cs - Syntax highlighting (optional: ScintillaNET or simple keyword highlighting)
- [x] ScriptEditor.cs - SetScript
- [x] ScriptEditor.cs - IsModified
- [x] ScriptEditor.cs - MaybeSave
- [x] ScriptEditor.cs - UpdateAvailableTags

### Screen Editor
- [x] ScreenTemplate.cs - Template data class
- [x] ScreenEditor.cs - UserControl creation
- [x] ScreenEditor.cs - Graphics canvas (Panel-based)
- [x] ScreenEditor.cs - Component placement (drag-and-drop implemented)
- [x] ScreenEditor.cs - Selection/move/resize (implemented)
- [x] ScreenEditor.cs - Grid and snap (grid drawing implemented)
- [x] ScreenEditor.cs - Zoom controls
- [x] ScreenEditor.cs - SetScadaProject
- [x] ScreenEditor.cs - SetTemplate
- [x] ScreenEditor.cs - SaveScreen
- [x] ScreenEditor.cs - IsModified
- [x] ScreenEditor.cs - GetScene

### Property Editor
- [x] PropertyEditor.cs - UserControl creation
- [x] PropertyEditor.cs - PropertyGrid layout (integrated in code)
- [x] PropertyEditor.cs - UpdateEditor
- [x] PropertyEditor.cs - SetAvailableTagTables
- [x] PropertyEditor.cs - SetAvailableScreens
- [x] PropertyEditor.cs - SetScadaProject
- [x] PropertyEditor.cs - Property change handling (basic)
- [x] PropertyEditor.cs - RequestAutoSave event

---

## Phase 3: Components

### Components Module
- [x] ComponentsModule.cs - Component registration (complete)
- [x] ComponentsView.cs - UserControl creation
- [x] ComponentsView.cs - Component palette (ListBox-based)
- [x] ComponentsView.cs - Drag-and-drop (implemented)
- [x] ComponentsView.cs - SetComponentsModule

### Component Types
- [x] BaseComponent.cs - Base class
- [x] ButtonComponent.cs
- [x] TextLabelComponent.cs
- [x] TextInputComponent.cs
- [x] CheckboxComponent.cs
- [x] ProgressBarComponent.cs
- [x] SliderComponent.cs
- [x] RadioButtonComponent.cs
- [x] GaugeViewComponent.cs
- [x] TrendViewComponent.cs
- [x] IndicatorComponent.cs
- [x] ImageViewComponent.cs
- [x] DateTimeComponent.cs
- [x] AlarmViewComponent.cs
- [x] TableComponent.cs
- [x] RectangleComponent.cs
- [x] LineComponent.cs
- [x] TriangleComponent.cs
- [x] SpinnerComponent.cs
- [x] ToggleSwitchComponent.cs
- [x] MotorComponent.cs
- [x] PumpComponent.cs
- [x] TankComponent.cs
- [x] ConveyorComponent.cs
- [x] PopupComponent.cs
- [x] TabComponent.cs

---

## Phase 4: Configuration Modules

### Communication Module
- [x] CommunicationModules.cs - Data class
- [x] CommunicationModuleEditor.cs - UserControl creation
- [x] CommunicationModuleEditor.Designer.cs - UI layout
- [x] CommunicationModuleEditor.cs - Module management
- [x] CommunicationModuleEditor.cs - SetScadaProject
- [x] CommunicationModuleEditor.cs - SetProjectManager
- [x] CommunicationModuleEditor.cs - SetScadaName
- [x] CommunicationModuleEditor.cs - SetCommunicationModules
- [x] CommunicationModuleEditor.cs - GetCommunicationModules
- [x] CommunicationModuleEditor.cs - SaveCommunicationConfig
- [x] ModbusSettingsDialog.cs - Dialog creation
- [x] OPCUAConnectionDialog.cs - Dialog creation

### Alarms Module
- [x] Alarms.cs - Data class
- [x] AlarmsEditor.cs - UserControl creation
- [x] AlarmsEditor.Designer.cs - DataGridView layout
- [x] AlarmsEditor.cs - Alarm management
- [x] AlarmsEditor.cs - SetAlarms
- [x] AlarmsEditor.cs - SetScadaProject

### Scheduler Module
- [x] Schedules.cs - Data class
- [x] ScheduleEditor.cs - UserControl creation
- [x] ScheduleEditor.Designer.cs - UI layout
- [x] ScheduleEditor.cs - Schedule management
- [x] ScheduleEditor.cs - SetSchedules
- [x] ScheduleEditor.cs - SetScadaProject
- [x] ScriptSelectionDialog.cs - Dialog creation

### Historian Module
- [x] Historian.cs - Data class
- [x] HistorianEditor.cs - UserControl creation
- [x] HistorianEditor.Designer.cs - UI layout
- [x] HistorianEditor.cs - Historian configuration
- [x] HistorianEditor.cs - SetHistorian
- [x] HistorianEditor.cs - SetScadaProject

### Security Module
- [x] Security.cs - Data class
- [x] SecurityEditor.cs - UserControl creation
- [x] SecurityEditor.Designer.cs - UI layout
- [x] SecurityEditor.cs - User management
- [x] SecurityEditor.cs - Role management
- [x] SecurityEditor.cs - SetSecurity
- [x] SecurityEditor.cs - SetScadaProject

### Machine Learning Module
- [x] MachineLearning.cs - Data class
- [x] MachineLearningEditor.cs - UserControl creation
- [x] MachineLearningEditor.Designer.cs - UI layout
- [x] MachineLearningEditor.cs - ML configuration
- [x] MachineLearningEditor.cs - SetMachineLearning
- [x] MachineLearningEditor.cs - SetScadaProject

---

## Phase 5: Build and Deploy

### Console Module
- [x] ConsoleModule.cs - Class creation
- [x] ConsoleModule.cs - GetOutputConsole
- [x] ConsoleModule.cs - GetDebugConsole
- [x] ConsoleModule.cs - HandleCompilerMessage
- [x] ConsoleModule.cs - HandleCompilerError
- [x] ConsoleModule.cs - HandleCompilerWarning

### Compiler Module
- [x] CompilerModule.cs - Class creation
- [x] CompilerModule.cs - SetProject
- [x] CompilerModule.cs - CompileProject (basic implementation)
- [x] CompilerModule.cs - CleanProject
- [x] CompilerModule.cs - Message event
- [x] CompilerModule.cs - Error event
- [x] CompilerModule.cs - Warning event

### Discovery Module
- [x] DeployDialog.cs - Form creation
- [x] DeployDialog.Designer.cs - UI layout
- [x] DeployDialog.cs - Deployment logic
- [x] UploadDialog.cs - Form creation
- [x] UploadDialog.Designer.cs - UI layout
- [x] UploadDialog.cs - Upload logic
- [x] UploadDialog.cs - UploadRequested event

### Simulator Module
- [x] SimulatorModule.cs - Class creation
- [x] SimulatorModule.cs - StartSimulation
- [x] SimulatorModule.cs - StopSimulation
- [x] SimulatorModule.cs - PauseSimulation
- [x] SimulatorModule.cs - ResumeSimulation
- [x] SimulatorModule.cs - IsPaused
- [x] SimulatorModule.cs - GetAutoBuildBeforeSimulate
- [x] SimulatorModule.cs - ShowProjectSelectionDialog
- [x] SimulatorModule.cs - ShowSettingsDialog
- [x] SimulatorModule.cs - Simulation events

---

## Phase 6: Tools and Polish

**Review (incomplete):** OptionsDialog, WorkspaceModule, and EventsBus are done. Missing: Customize/ExternalTools/PackageManager dialogs (stub messages only) and all UI/UX (icons, themes, DPI, layout persistence wired to MainForm).

### Tools Module
- [x] OptionsDialog.cs - Form creation
- [x] OptionsDialog.Designer.cs - UI layout
- [x] OptionsDialog.cs - GetCurrentTheme
- [x] OptionsDialog.cs - ApplyTheme
- [x] OptionsDialog.cs - ThemeChanged event
- [ ] CustomizeDialog.cs - Form creation (stub: MessageBox only)
- [ ] CustomizeDialog.Designer.cs - UI layout
- [ ] CustomizeDialog.cs - Customization logic
- [ ] ExternalToolsDialog.cs - Form creation (stub: MessageBox only)
- [ ] ExternalToolsDialog.Designer.cs - UI layout
- [ ] ExternalToolsDialog.cs - Tool management
- [ ] PackageManagerDialog.cs - Form creation (stub: MessageBox only)
- [ ] PackageManagerDialog.Designer.cs - UI layout
- [ ] PackageManagerDialog.cs - Package management

### Workspace Module
- [x] WorkspaceModule.cs - Class creation
- [x] WorkspaceModule.cs - Save layout
- [x] WorkspaceModule.cs - Restore layout
- [x] WorkspaceModule.cs - Dock panel states
- [ ] MainForm - Call WorkspaceModule SaveLayout on FormClosing
- [ ] MainForm - Call WorkspaceModule RestoreLayout on Load

### Events Bus Module
- [x] EventsBus.cs - Class creation
- [x] EventsBus.cs - Publish/Subscribe
- [x] EventsBus.cs - Event routing

### UI/UX
- [ ] Convert Qt icons to Windows Forms format
- [ ] Add menu icons
- [ ] Add project tree icons
- [ ] Add component palette icons
- [ ] Create application icon
- [ ] Implement light theme
- [ ] Implement dark theme
- [ ] Implement system theme detection
- [ ] Apply themes consistently
- [ ] Handle high DPI scaling
- [ ] Save window layout (WorkspaceModule exists; not wired to MainForm)
- [ ] Restore window layout
- [ ] Save splitter positions
- [ ] Save dock panel states

---

## Phase 7: Integration

**Review (incomplete):** Core integration is done. Missing: View menu toggle for Component Palette; layout save/restore not wired (WorkspaceModule exists but MainForm does not call it).

### Main Window Integration
- [x] Integrate ProjectView in left panel
- [x] Integrate ComponentsView in right panel
- [x] Integrate ConsoleModule in bottom tabs
- [x] Integrate PropertyEditor in bottom tabs
- [x] Handle editor tab management
- [x] Connect ProjectView events
- [x] Connect ScreenEditor selection to PropertyEditor (basic - can be enhanced)
- [x] Connect CompilerModule to ConsoleModule
- [x] Handle tab closing with save prompts
- [x] Manage editor lifecycle
- [x] View menu - Toggle Component Palette (panel visibility)

### Module Communication
- [x] Implement event system (EventsBus module created)
- [x] Connect tag table changes to script editor (basic)
- [x] Connect project changes to editors
- [x] Handle cross-module dependencies

### Menu Implementation
- [x] File menu - All actions (New, Open, Close, Save, Save As, Save All, Rename, Import, Export, Exit)
- [x] Edit menu - All actions (Undo, Redo, Cut, Copy, Paste, Delete)
- [x] View menu - Project Explorer, Properties, Output, Component Palette, Zoom controls
- [x] Build menu - All actions (Build, Rebuild, Clean, Settings)
- [x] Download menu - All actions (Deploy to Device)
- [x] Upload menu - All actions (Upload from Device)
- [x] Simulate menu - All actions (Start, Pause, Stop, Settings)
- [x] Tools menu - All actions (Options, Customize, External Tools, Package Manager)
- [x] Help menu - All actions (Documentation, About)

---

## Phase 8: Data and Serialization

### Project File Format
- [x] Define .isc file structure
- [x] Implement project serialization
- [x] Implement project deserialization
- [x] Handle project versioning
- [x] Handle project migration

### Component Serialization
- [x] Implement component serialization
- [x] Implement component deserialization
- [x] Handle component versioning

---

## Phase 9: Testing

### Unit Tests
- [ ] ProjectManager tests
- [ ] CompilerModule tests
- [ ] Serialization tests
- [ ] Validation tests

### Integration Tests
- [ ] Project creation workflow
- [ ] Project opening workflow
- [ ] Editor opening/closing
- [ ] Compilation workflow
- [ ] Deployment workflow

### UI Tests
- [ ] Menu actions
- [ ] Dialogs
- [ ] Editor interactions
- [ ] Drag-and-drop

---

## Phase 10: Documentation

### Code Documentation
- [ ] XML documentation for public APIs
- [ ] Module interface documentation
- [ ] Data structure documentation
- [ ] Event system documentation

### User Documentation
- [ ] Update user manual
- [ ] Document new features
- [ ] Migration guide

---

## Progress Tracking

**Phase 1:** 100% Complete (All core infrastructure completed)
**Phase 2:** ~95% Complete (One optional item: Script Editor syntax highlighting)
**Phase 3:** 100% Complete (BaseComponent and 25 component types completed, drag-and-drop implemented)
**Phase 4:** 100% Complete (All configuration modules completed, including Communication Module)
**Phase 5:** 100% Complete (Discovery and Simulator modules completed)
**Phase 6:** ~50% Complete (OptionsDialog/Workspace/EventsBus done; Customize/ExternalTools/PackageManager stubs; UI/UX not started)
**Phase 7:** ~92% Complete (Integration done; Component Palette toggle added; layout save/restore not yet wired to MainForm)
**Phase 8:** 100% Complete (Project and Component serialization completed)
**Phase 9:** 0% Complete
**Phase 10:** 0% Complete

**Overall Progress:** ~75% Complete (Core functionality implemented, testing and documentation remaining)

**Component Library:** 25 component types available:
- Basic UI: Button, TextLabel, TextInput, Checkbox, RadioButton, ToggleSwitch
- Data Display: ProgressBar, Slider, GaugeView, TrendView, Indicator, DateTime, AlarmView, Table, ImageView
- Shapes: Rectangle, Line, Triangle
- Industrial: Motor, Pump, Tank, Conveyor
- Containers: Tab, Popup
- Utilities: Spinner

---

## Notes

- Update this checklist as tasks are completed
- Mark items with [x] when done
- Add notes for any blockers or issues
- Track time spent on each phase
