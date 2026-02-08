# Plan 1: AccuTrack Designer – Qt to C# WinForms Migration

This plan guides the migration of the AccuTrack Designer from Qt (C++) to C# WinForms. It references the original Qt implementation at **`d:\Dev\AccuTrackQt\Designer`** and the target C# solution at **`d:\Dev\AccuTrack\Designer`**.

---

## Agent instructions (parallel work)

- **Scope**: Designer only. Do not modify Runtime or SDK code; consume SDK (Plan 3) as a referenced project/package.
- **Inputs from other plans**: Use **Plan 3 (SDK)** for `ICommunicationModule`, address formats, and shared DTOs so communication dialogs and tag engine stay compatible. Align JSON schemas with what **Plan 2 (Runtime)** expects (see Plan 2 §11).
- **Outputs for other plans**: Compiler output (Avalonia views + JSON) and project format must be loadable by Runtime (Plan 2). Keep JSON file names and structure documented in §17 and in Plan 2.
- **Build precedence**: Use the **canonical module order** in §1.3 (from `Designer.slnx`); add new projects in that order so build and dependency resolution stay consistent.
- **When running as one of three agents**: Assume Plan 2 and Plan 3 are being implemented in parallel; agree on shared contract (SDK interfaces, JSON schema) early; do not block on Runtime/Designer UI details.

### Desired conditions (this plan must satisfy)

1. **Designer** is implemented in **C# WinForms** (not Qt).
2. **References** the initial Qt Designer at **`d:\Dev\AccuTrackQt\Designer`** for structure, modules, and behavior (see §1 and per-module Qt paths).
3. **Build precedence** follows the C# Designer solution order (**`Designer.slnx`**) as the canonical module order (§1.2).
4. **Compiler** produces **Avalonia** (AXAML/C#) output instead of QML, so Runtime (Plan 2) can load screens.
5. **JSON and project format** are compatible with Runtime (Plan 2) and SDK (Plan 3); see §17.
6. Plan is **extensive** enough to drive implementation and to be used by one of three parallel agents.

---

## 1. Reference: Qt Designer Structure

### 1.1 Root and Build

| Qt Path | Purpose |
|--------|---------|
| `d:\Dev\AccuTrackQt\Designer\CMakeLists.txt` | Qt module order: SDK first, then tags → screens → script → communication → alarms → schedules → historian → security → machine_learning → project → components → events → workspace → console → compiler → device_discovery → simulation → tools. |
| `d:\Dev\AccuTrackQt\Designer\main.cpp` | Entry point; creates `MainWindow`. |
| `d:\Dev\AccuTrackQt\Designer\mainwindow.cpp` / `.h` / `.ui` | Main window: menu, toolbar, `ProjectView`, `QTabWidget` (editorTabs), `PropertyEditor`, `ConsoleModule`, `Compiler_module`, `SimulationModule`. |

### 1.2 Canonical build precedence (C# Designer)

The **authoritative** build and reference order for the C# Designer is defined by **`d:\Dev\AccuTrack\Designer\Designer.slnx`**. New projects must be inserted in this order so that dependencies resolve correctly:

1. **SDK** (referenced from `../SDK/SDK/SDK.csproj`)  
2. alarms  
3. communication  
4. compiler  
5. components  
6. console  
7. **Designer** (main executable)  
8. discovery  
9. events  
10. historian  
11. machinelearning  
12. project  
13. scheduler  
14. screeneditor  
15. scripter  
16. simulator  
17. tagengine  
18. tools  
19. workspace  

**Missing from current solution**: **security**. Either add a `security` project (e.g. after scheduler) or implement the security/roles editor inside the **tools** module; document the choice in the solution README or Plan 1.

### 1.3 Module-to-Module Mapping (Qt → C# Designer)

| Qt Designer Module | Path (Qt) | C# Designer Project | Notes |
|--------------------|-----------|----------------------|--------|
| **tags_module** | `Designer\tags_module\` | `Designer\tagengine\` | Tag tables, tag editor UI. |
| **screens_module** | `Designer\screens_module\` | `Designer\screeneditor\` | Screen canvas, templates, component placement. |
| **script_module** | `Designer\script_module\` | `Designer\scripter\` | Script editor, Lua integration. |
| **communication_module** | `Designer\communication_module\` | `Designer\communication\` | Comm modules list, Modbus/OPC dialogs. |
| **alarms_module** | `Designer\alarms_module\` | `Designer\alarms\` | Alarms config, conditions, editor. |
| **schedules_module** | `Designer\schedules_module\` | `Designer\scheduler\` | Schedules editor. |
| **historian_module** | `Designer\historian_module\` | `Designer\historian\` | Historian config, editor. |
| **security_module** | `Designer\security_module\` | **Add** `security` project to solution (see §1.2) or implement under `tools` | Security/roles; JSON: `security.json`. |
| **machine_learning** | `Designer\machine_learning\` | `Designer\machinelearning\` | ML config, editor. |
| **project_module** | `Designer\project_module\` | `Designer\project\` | Project tree, SCADA nodes, ProjectManager, ProjectView, dialogs. |
| **components_module** | `Designer\components_module\` | `Designer\components\` | Component palette, PropertyEditor, animations. |
| **events_module** | `Designer\events_module\` | `Designer\events\` | Event definitions. |
| **workspace_module** | `Designer\workspace_module\` | `Designer\workspace\` | Workspace layout. |
| **console_module** | `Designer\console_module\` | `Designer\console\` | Output/console. |
| **compiler_module** | `Designer\compiler_module\` | `Designer\compiler\` | Build, JSON, **QML generation** → replace with **Avalonia/AXAML generation**. |
| **device_discovery** | `Designer\device_discovery\` | `Designer\discovery\` | Deploy/upload, device manager, project transfer client. |
| **simulation_module** | `Designer\simulation_module\` | `Designer\simulator\` | Simulation run/control. |
| **tools_module** | `Designer\tools_module\` | `Designer\tools\` | Options, customize, external tools, package manager. |

---

## 2. Main Shell (WinForms)

### 2.1 Reference: Qt Main Window

- **`mainwindow.h`** / **`mainwindow.cpp`**: `QMainWindow`, `Ui::MainWindow`, `ProjectView`, `QTabWidget` (editorTabs), `PropertyEditor`, `ProjectManager`, `Compiler_module`, `SimulationModule`, `ConsoleModule`.
- **Slots**: New/Open/Save/Save As/Close project, Build/Clean, Deploy/Upload, Simulation (Start/Pause/Stop), Options/Customize/External Tools/Package Manager, Documentation/About.
- **Tab open handlers**: `onScreenOpen`, `onTagTableOpen`, `onAllTagTablesOpen`, `onScriptOpen`, `onCommunicationModuleOpen`, `onAlarmsOpen`, `onSchedulesOpen`, `onHistorianOpen`, `onSecurityOpen`, `onMachineLearningOpen`, `onDeviceNetworkOpen`, `onSettingsOpen`, `onTabCloseRequested`, `onAutoSaveRequested`, `onComponentSelected`.

### 2.2 WinForms Equivalents

| Qt | WinForms |
|----|----------|
| `QMainWindow` | `Form` (e.g. `MainForm`) with `IsMdiContainer` or single client area |
| `QTabWidget` (editorTabs) | `TabControl` (e.g. `EditorTabControl`) |
| `ProjectView` (tree) | `TreeView` + `ProjectView`-style presenter/service |
| `PropertyEditor` | Property grid (`PropertyGrid`) or custom panel |
| Menu/toolbar | `MenuStrip`, `ToolStrip` |
| Recent projects | `ToolStripMenuItem` list, persist in settings |

### 2.3 Tasks

1. **MainForm**: Create `MainForm` (or rename existing `MainWindow`) with menu, toolbar, status bar.
2. **Project tree**: Host project tree control; wire to `ProjectManager` (or equivalent C# service).
3. **Editor tabs**: Single `TabControl`; each opened entity (screen, tag table, script, etc.) opens as a new tab with the corresponding user control.
4. **Property editor**: Panel that shows properties of selected item (e.g. screen component); wire to selection from screen editor.
5. **Console**: Dedicated panel or dock for build/output/console; wire to compiler and scripts.
6. **References**: Add project references from main Designer app to all module projects (tagengine, screeneditor, scripter, communication, alarms, scheduler, historian, machinelearning, project, components, events, workspace, console, compiler, discovery, simulator, tools) and to **SDK** for communication/scripting types.

---

## 3. Project Module (Core)

### 3.1 Qt Reference

- **`project_module\project_module.h/.cpp`**: Thin module class.
- **`project_module\projectmanager.cpp/.h`**: Project lifecycle, SCADA project type, open/save, recent list.
- **`project_module\projectview.cpp/.h`**, **`projectview.ui`**: Tree view of project/screens/tags/scripts/comm/alarms/schedules/historian/security/ML/device network.
- **`project_module\projectdirectory.cpp/.h`**, **projecttemplates**, **projectvalidator**, **projectreconstructor**, **projectsettings**, **projectvalidator**.
- **`project_module\devicenetwork*.cpp`**, **scadaprojectdialog**, **settingseditor**, **validationdialog**, **ipaddressdialog**.

### 3.2 C# Designer Tasks

1. **ProjectManager**: Port project open/save/close, path to metadata (e.g. `metadata.iscr`), SCADA project type, recent projects list.
2. **ProjectView**: Tree control bound to project model (screens, tag tables, scripts, communication modules, alarms, schedules, historian, security, ML, device network). Double-click → raise event so MainForm can open the correct editor tab.
3. **Project model**: Classes for SCADA project, folders, screens, tag tables, scripts, communication modules, alarms, schedules, historian config, security, ML config, device network (mirror Qt data structures used in `mainwindow` slots).
4. **Dialogs**: New project (wizard/dialog), project settings, validation, device network/IP (where applicable).
5. **References**: Project module should not depend on other editor modules; MainForm wires tree events to editor opening.

---

## 4. Screens Module (Screen Editor)

### 4.1 Qt Reference

- **`screens_module\`**: Screen editor canvas, `ScreenTemplate`, component instances, save/load.
- **`components_module\propertyeditor`**: Property editing for selected component.
- **`components_module\componentsview`**: Palette of components (Button, Tank, Gauge, etc.).

### 4.2 C# Designer Tasks

1. **Canvas**: WinForms `Panel` with double-buffering (or use a control that supports drag/drop and drawing) for placing and moving components; coordinate system and zoom similar to Qt graphics scene.
2. **ScreenTemplate**: Model for one screen: name, size, list of component instances (type, position, size, properties).
3. **Component palette**: List or toolbar of component types (Button, Indicator, Tank, Motor, Pump, Gauge, TrendView, AlarmView, etc.) — align with Runtime Avalonia component list from Plan 2.
4. **Property editor**: When a component is selected on canvas, PropertyEditor shows its properties; changes update the model and optionally the canvas.
5. **Serialization**: Save/load screen to/from JSON (or same format as Qt `json/` screens) so Runtime can load the same format.
6. **Compiler**: Screens are input to the compiler; compiler output is Avalonia (AXAML + code-behind or pure C#) instead of QML — see Compiler section.

---

## 5. Tags Module (Tag Engine)

### 5.1 Qt Reference

- **`tags_module\`**: Tag table model, tag editor UI (e.g. table with name, address, type, etc.).

### 5.2 C# Designer Tasks

1. **Tag table model**: List of tags (name, address, data type, scaling, etc.); per SCADA or per tag table.
2. **Tag editor control**: Grid or list view to add/edit/delete tags; address format compatible with SDK (e.g. Modbus, OPC).
3. **Integration**: Project tree opens “Tag table” → add tab with tag editor; save to project JSON (e.g. `tags.json` or per-table files) so Runtime tags module can load same format.

---

## 6. Script Module (Scripter)

### 6.1 Qt Reference

- **`script_module\`**: Lua script editor, script list per SCADA, run/debug (if any).

### 6.2 C# Designer Tasks

1. **Script list**: Per SCADA project; list of script files/names.
2. **Script editor**: Text editor (e.g. Scintilla or simple `TextBox`) with syntax highlighting for Lua (or chosen scripting language).
3. **Save**: Scripts saved under project (e.g. `scripts/` folder or embedded in project JSON); format compatible with Runtime script module (see Plan 2).

---

## 7. Communication Module

### 7.1 Qt Reference

- **`communication_module\communicationmoduleeditor.cpp/.h`**, **communicationmodules.cpp/.h**: List of communication modules (Modbus, OPC, etc.), add/edit/delete.
- **`modbussettingsdialog`**, **`opcuaconnectiondialog`**: Protocol-specific configuration dialogs.

### 7.2 C# Designer Tasks

1. **Communication modules list**: Model and UI for multiple connections (name, type: Modbus/OPC/S7).
2. **Modbus settings**: Dialog/panel for host, port, slave ID, TCP/RTU, timeouts (mirror `d:\Dev\AccuTrackQt\Designer\communication_module\modbussettingsdialog`).
3. **OPC settings**: Dialog for endpoint, security (mirror `opcuaconnectiondialog`).
4. **S7**: If used, dialog for PLC address, rack, slot, etc.
5. **Persistence**: Save to project JSON (e.g. `communication.json`) so Runtime communication module (and SDK drivers) can use same config.

---

## 8. Alarms, Schedules, Historian, Security, Machine Learning

### 8.1 Qt Reference

- **`alarms_module\`**: `alarms.cpp/.h`, `alarmseditor.cpp/.h`, `alarmseditor.ui` — alarm conditions, priorities, editor UI.
- **`schedules_module\`**: Schedules list and editor UI.
- **`historian_module\`**: `historian.cpp/.h`, `historianconfig`, `historianeditor` — tags to log, retention, editor.
- **`security_module\`**: Roles/users (if present).
- **`machine_learning\`**: `machinelearning.cpp/.h`, `machinelearningconfig`, `machinelearningeditor` — ML config and editor.

### 8.2 C# Designer Tasks

1. **Alarms**: Editor control for alarm definitions (tag, condition, priority, message); save to `alarms.json` (or equivalent).
2. **Schedules**: Editor for schedule entries (cron-like or time-based); save to `schedules.json`.
3. **Historian**: Editor for which tags to log, interval, retention; save to `historian.json`.
4. **Security**: If applicable, roles/permissions editor; save to `security.json`.
5. **Machine learning**: Editor for ML model/config and tag selection; save to `machinelearning.json`.

Each opens in a tab from MainForm when user selects the node in the project tree.

---

## 9. Components Module

### 9.1 Qt Reference

- **`components_module\`**: `propertyeditor`, `componentsview`, `animations`; component definitions and icons (SVG/PNG) for palette.

### 9.2 C# Designer Tasks

1. **PropertyEditor**: Shared control used by MainForm; bound to “selected object” (e.g. screen component or project node). Use WinForms `PropertyGrid` or custom panel.
2. **ComponentsView**: Palette of component types with icons; drag onto screen canvas to create instance.
3. **Animations**: If animations are edited in designer, model and UI for animation definitions; align with Runtime AnimationManager (Plan 2).

---

## 10. Events Module

### 10.1 Qt Reference

- **`events_module\`**: Event definitions (e.g. OnClick, OnValueChange) linked to scripts or actions.

### 10.2 C# Designer Tasks

1. **Event list**: Per screen or global: event type, target (component/tag), action (script, navigation, etc.).
2. **Editor**: Simple list/grid to add/edit/delete events; save to `events.json` for Runtime EventManager.

---

## 11. Compiler Module (Critical for Avalonia)

### 11.1 Qt Reference

- **`compiler_module\compiler_module.cpp/.h`**: Build orchestration.
- **`compiler_module\generateqml.cpp/.h`**: Converts screen templates to QML: `GenerateScreens`, `GenerateScreen`, `generateComponentQml` and per-component helpers (Button, Indicator, Table, TrendView, AlarmView, Tank, Motor, Pump, Conveyor, DateTime, TextLabel, Checkbox, GaugeView, ImageView, Line, Numeric, RadioButton, Rectangle, Triangle, Svg, Popup, ProgressBar, Slider, Spinner, Tab, TextInput, ToggleSwitch, ComboBox, AnimationHelper).

### 11.2 C# Designer Tasks

1. **Build orchestration**: “Build project” triggers: collect screens, tags, scripts, communication, alarms, schedules, historian, events, animations from project; validate; then run code generation and copy assets.
2. **Generate Avalonia instead of QML**: New class (e.g. `AvaloniaScreenGenerator` or `GenerateAXAML`):
   - Input: Same screen/model data that Qt `GenerateQML` uses (list of screens, component list per screen).
   - Output: Per-screen AXAML (+ code-behind if needed) or C# view classes that mirror `Runtime\...\IndusysComponents` (see Plan 2). Map each Qt component type to an Avalonia control (Button, CheckBox, Slider, ProgressBar, TextBlock, etc., and custom controls for Tank, Gauge, TrendView, AlarmView, etc.).
3. **JSON generation**: Port the full set of generators from **`d:\Dev\AccuTrackQt\Designer\compiler_module\genenratejson.h`** (note Qt typo "GenenrateJSON"). Generate all of: `generateCommunicationConfig`, `generateHistorianConfig`, `generateAlarmsConfig`, `generateSchedulesConfig`, `generateScreensConfig`, `generateScriptsConfig`, `generateTagsConfig`, `generateSecurityConfig`, `generateEventsConfig`, `generateAnimationsConfig`, `generateMachineLearningConfig`, `generateProjectMetadata`. Output files and schema must match what Runtime loads (see Plan 2 §11).
4. **Output layout**: Emit to project build output (e.g. `bin/` or a “Runtime payload” folder) so the same project can be loaded by the Avalonia Runtime.

---

## 12. Device Discovery / Deployment

### 12.1 Qt Reference

- **`device_discovery\deploydialog`**, **uploaddialog**, **devicemanagerwidget**: Deploy project to device, upload from device.
- **`project_transfer_client.cpp/.h`**, **project_upload_server** (if designer hosts upload): Transfer client to push project to Runtime device.

### 12.2 C# Designer Tasks

1. **Deploy dialog**: Select device (IP/host), push built project (JSON + generated Avalonia + assets) to device (e.g. HTTP or custom protocol matching Runtime `ProjectTransferServer`).
2. **Upload dialog**: Pull project from device back to designer.
3. **Device manager**: List of known devices, discovery if applicable (match Runtime discovery responder).

---

## 13. Simulation Module

### 13.1 Qt Reference

- **`simulation_module\`**: Start/pause/stop simulation; may launch Runtime in “simulation” mode or drive a local run.

### 13.2 C# Designer Tasks

1. **Simulation control**: Buttons/commands to start/pause/stop; either start the Avalonia Runtime process with a flag and point it at the built project, or embed a lightweight runner. Prefer reusing the same Runtime executable with project path.

---

## 14. Tools Module

### 14.1 Qt Reference

- **`tools_module\optionsdialog`**, **customizedialog**, **externaltoolsdialog**, **packagemanagerdialog**: Options, toolbar customization, external tools, package manager.

### 14.2 C# Designer Tasks

1. **Options**: Application settings (paths, themes, editor defaults); persist in user config.
2. **Customize**: Toolbar/menu customization if required.
3. **External tools**: Optional list of external executables/scripts.
4. **Package manager**: If the stack uses NuGet or internal packages, UI to manage them; otherwise can be stubbed.

---

## 15. Workspace & Console

### 15.1 Qt Reference

- **`workspace_module\`**: Layout persistence (docking).
- **`console_module\`**: Output window for build and script logs.

### 15.2 C# Designer Tasks

1. **Workspace**: Save/restore form layout (panel positions, tab order); use WinForms persistence or a simple config file.
2. **Console**: Dedicated output control (e.g. `ListBox` or `TextBox` with append); subscribe to compiler and script runner output.

---

## 16. Solution and Project References

### 16.1 Current C# Solution (`Designer.slnx`)

Projects (in solution order): **SDK** (referenced from `../SDK/SDK/SDK.csproj`), alarms, communication, compiler, components, console, Designer, discovery, events, historian, machinelearning, project, scheduler, screeneditor, scripter, simulator, tagengine, tools, workspace. No **security** project yet — add one or implement under tools (§1.2).

### 16.2 Dependency Order (for build and reference)

1. **SDK** (shared): Referenced by Designer and by modules that need communication/types.
2. **project**: No dependency on other editor modules; defines ProjectManager and tree model.
3. **tagengine**, **scheduler**, **historian**, **alarms**, **machinelearning**, **communication**, **events**: Depend on project/SDK as needed; expose editor controls.
4. **components**: PropertyEditor and palette; may depend on project/screeneditor for types.
5. **screeneditor**: Depends on components and project; produces screen model for compiler.
6. **scripter**: Depends on project.
7. **compiler**: Depends on project, screeneditor (for screen list), and optionally others for JSON; references SDK for nothing or minimal.
8. **console**, **workspace**, **tools**: Minimal dependencies; used by main shell.
9. **discovery**, **simulator**: Depend on project/compiler output; discovery may reference SDK for transfer protocol.

Main **Designer** executable references all of the above and wires them in MainForm.

---

## 17. File and Format Compatibility

- **Project metadata**: Keep `metadata.iscr` (or same name) and JSON schema so Runtime can open projects built by the new Designer. See **Plan 2 §11** for Runtime project path and config layout.
- **JSON files** (must match Runtime expectations — Plan 2):  
  `tags.json`, `communication.json`, `alarms.json`, `schedules.json`, `historian.json`, `security.json`, `events.json`, `animations.json`, `machinelearning.json`, plus screens/scripts metadata as generated by `generateProjectMetadata` / `generateScreensConfig` / `generateScriptsConfig`. Keep structure compatible with C# Runtime (Plan 2) and Qt Runtime.
- **Screens**: Designer produces screen model (e.g. JSON); compiler produces Avalonia views (AXAML/C#); Runtime (Plan 2) loads project and instantiates those views. No QML in output.
- **SDK alignment**: Communication config and tag address formats must align with **Plan 3 (SDK)** so Runtime TagIOHandler and SDK drivers work without change.

---

## 18. Implementation Order Suggestion

1. **SDK** (Plan 3) and **shared contracts**: So Designer and Runtime can share project format and communication types.
2. **Project module**: ProjectManager, ProjectView, project model, open/save.
3. **MainForm**: Shell with menu, toolbar, project tree, tab control, property panel, console.
4. **Tag engine**: Tag table model and editor; wire into project tree and tabs.
5. **Communication**: Modules list and Modbus/OPC (and S7) dialogs; wire into project and tabs.
6. **Alarms, Schedules, Historian, Security, ML**: One by one; editors and tab wiring.
7. **Components**: Palette and PropertyEditor; wire selection from screen editor.
8. **Screen editor**: Canvas, ScreenTemplate, component placement; wire to PropertyEditor and project.
9. **Events**: Event list editor; save to `events.json`.
10. **Compiler**: JSON generation first, then Avalonia screen generator (replacing QML).
11. **Scripter**: Script list and editor; save under project.
12. **Console, workspace, tools**: Polish and layout.
13. **Discovery**: Deploy/upload and device manager.
14. **Simulator**: Start Runtime with built project.

This order keeps the shell and project core first, then data editors, then screen editor and compiler, then deployment and simulation.

---

## 19. Acceptance criteria (Designer)

- [ ] MainForm opens with menu, toolbar, project tree, editor tab control, property panel, console.
- [ ] New/Open/Save/Save As/Close project work; recent projects persist.
- [ ] Double-click in project tree opens correct editor tab (screen, tag table, script, communication, alarms, schedules, historian, security, ML, device network, settings).
- [ ] Build produces all JSON configs (see §11.2) and Avalonia screen output in the layout expected by Runtime (Plan 2).
- [ ] Deploy pushes built project to device using same protocol as Runtime ProjectTransferServer (Plan 2).
- [ ] Simulation starts Runtime (Plan 2) with built project path.
- [ ] All module projects are in `Designer.slnx` in the canonical order (§1.2); security is either a dedicated project or under tools.
