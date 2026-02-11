# AccuTrack Runtime: Qt QML to C# Avalonia Migration Plan

## Overview

This document outlines the plan to migrate the AccuTrack Runtime from **Qt (C++) + QML** to **C# + Avalonia UI**. The Runtime runs the deployed SCADA project: it loads project data, runs communication/tags/alarms/historian/scripts/schedules, and displays runtime screens. All modules will be **individually rebuilt** under `RuntimeModules/`, and the Qt/QML shell will be replaced by an **Avalonia** host application.

**Source:** `d:\Dev\AccuTrackQt\Runtime`  
**Target:** `d:\Dev\DarkStar\Runtime` (host) + `d:\Dev\DarkStar\RuntimeModules\` (modules)

**Goals:**
- Replace Qt application and QML UI with C# Avalonia.
- Rebuild each Qt Runtime module as a C# RuntimeModule with clear interfaces.
- Preserve behavior: project load, config load, module lifecycle, screen loading, events, animations, communication status, project transfer, discovery.

---

## 1. Architecture

### 1.1 Host Application (Runtime)

**Qt:** `main.cpp` + `Main.qml` (QQmlApplicationEngine, context properties, QQuickItem container).  
**C# Avalonia:** Single executable that:
- Builds an Avalonia `AppBuilder` and runs the main window.
- Creates the **ExecutionEngine** (singleton or DI), initializes and registers all modules.
- Opens the **main Avalonia window** (replacing Main.qml): toolbar, screen container, status bar, transfer overlay.
- Wires module APIs into the UI via services/data binding (no QML context properties).

**Files:**
- `Runtime/Program.cs` – Entry point, Avalonia app setup, ExecutionEngine init, module registration, window show.
- `Runtime/App.axaml` (optional) – Application-level resources.
- `Runtime/Views/MainWindow.axaml` + `MainWindow.axaml.cs` – Main shell (replaces Main.qml).
- `Runtime/Runtime.csproj` – Must reference Avalonia + all RuntimeModules.

### 1.2 Module Contract

**Qt:** `execution_engine/module_interface.h` – `ModuleInterface` with `initialize(config)`, `start()`, `stop()`, `shutdown()`, `isRunning()`, `status()`, `moduleName()`, `dependencies()`, signals.  
**C#:** Define a shared contract used by the host and all modules.

**Tasks:**
- Add `RuntimeModules/ExecutionEngine/IModuleInterface.cs` (or equivalent in a shared Core project) with:
  - `string ModuleName`, `string DisplayName`, `string Version`, `IReadOnlyList<string> Dependencies`
  - `bool Initialize(JObject config)`, `bool Start()`, `void Stop()`, `void Shutdown()`
  - `bool IsRunning`, `string Status`
  - Events: `StatusChanged`, `ErrorOccurred`, `Initialized`, `Started`, `Stopped`
- Add `ModuleManager` (or extend ExecutionEngine): `RegisterModule(IModuleInterface)`, `GetModule<T>(name)`, `GetStartOrder()`, `GetStopOrder()`, `StartAll()`, `StopAll()`, `ReinitializeWithProject(projectPath)`.

### 1.3 Dependency Order (from Qt main.cpp)

Suggested start order (dependencies first):
1. **EventDispatcher** (no deps)
2. **Communication**, **TagsEngine**, **Console**, **Project**, **Discovery** (as needed)
3. **Scheduler** (Script dependency), **Alarms**, **Historian**, **MLEngine**
4. **ScriptingEngine**, **Screens** (UI; may depend on Tags, EventManager, etc.)

Cross-wiring (from main.cpp):
- TagsModule → Communication (setCommunicationModule)
- Scheduler → Script (setScriptModule)
- Historian ← TagManager (setTagManager)
- Screens: EventManager ← TagIOHandler, TagManager; AnimationManager ← TagManager
- Project → ScreenManager (screenAvailable → loadScreen)

These wirings must be done in the host after modules are created and before/after project load.

---

## 2. Module-by-Module Rebuild Plan

Each row: Qt module → DarkStar RuntimeModule; key types to implement.

| Qt Module | RuntimeModule | Key Types / Notes |
|-----------|----------------|-------------------|
| **execution_engine** | **ExecutionEngine** | `IModuleInterface`, `ModuleManager`, `ExecutionEngine` (singleton), `Initialize()` / `ReinitializeWithProject()` / `StartAll()` / `StopAll()` / `Shutdown()` |
| **event_dispatcher** | **EventDispatcher** | Event subscription, dispatch; used by other modules |
| **communication_module** | **Communication** | Modbus + OPC UA backends, config load, tag mappings, `GetConnectionStatuses()` for status bar |
| **tags_module** | **TagsEngine** | TagManager, TagIOHandler, GlobalTagStorage, TagAddressParser, TagSubscription, RuntimeTag |
| **schedules_module** | **Scheduler** | ScheduleManager, ScheduleExecutor, Schedule; dependency on Script |
| **alarms_module** | **Alarms** | AlarmManager, Alarm, AlarmCondition |
| **historian_module** | **Historian** | HistorianManager, HistorianDatabase, DataCollector, DataRetention |
| **machine_learning** | **MLEngine** | ML module stub/config |
| **script_module** | **ScriptingEngine** | Lua (or other) scripting, script load/execute, used by Scheduler and events |
| **console_module** | **Console** | Log buffer, `logInfo`/`logError`/`clearLogs()` for Logs screen |
| **project_module** | **Project** | Project load/save, current project, resolution, screen list; `screenAvailable` → ScreenManager |
| **device_discovery** | **Discovery** | DeviceDiscoveryResponder, ProjectTransferServer, ProjectUploadClient |
| **screens_module** | **Screens** | ScreenManager, EventManager, AnimationManager, HistorianQueryHelper; **all QML replaced by Avalonia** (see Section 3) |
| **security_module** | **Security** | Security module stub/config |

For each module:
- **Interface:** Add or extend `RuntimeModules/<Module>/i<Module>.cs` (e.g. `IAlarms`, `IScreens`) with the public API used by the host or other modules.
- **Implementation:** Rebuild logic from Qt C++ into C# in the same RuntimeModule project; no Qt/QML.
- **Config:** Keep same config files and JSON shapes where possible (e.g. `communications.json`, `events.json`, `animations.json`) so project format stays compatible.

---

## 3. Replacing QML with Avalonia

### 3.1 Main Shell (replaces Main.qml)

**Qt:** `Main.qml` – Window, ColumnLayout, toolbar (Logs/Alarms buttons), main area (Item as screen container), status bar (connection status Repeater), transfer overlay, Connections to screenManager/scriptManager/projectManager/transferServer, Timer for status update, `updateConnectionStatuses()`.  
**Avalonia:** One main window with:
- **Toolbar:** Logs button, Alarms button (command or navigation to show Logs/Alarms view).
- **Main content:** A single `ContentControl` or similar that acts as the **screen container**; ScreenManager (or host) sets its `Content` to the currently loaded screen view (project screen or Logs/Alarms).
- **Status bar:** List of connection status items (data-bound from Communication module’s `GetConnectionStatuses()`), updated on a timer or on Communication events.
- **Transfer overlay:** Overlay panel with title, status text, progress bar, progress percentage; visibility and values bound to ProjectTransferServer (or equivalent) events (TransferStarted, TransferProgress, TransferCompleted, ErrorOccurred).
- **Wiring:** Replace QML `Connections` and context properties with C# events and service references (e.g. inject or resolve ScreenManager, Console, ScriptManager, ProjectManager, Communication, TransferServer in the main window and subscribe to their events).

**Files:**
- `Runtime/Views/MainWindow.axaml` + `MainWindow.axaml.cs`
- Optional: `Runtime/ViewModels/MainWindowViewModel.cs` for binding (connection status list, overlay visibility, etc.)

### 3.2 Built-in Runtime Screens (QML → Avalonia)

| QML Screen | Avalonia View | Purpose |
|------------|----------------|---------|
| `qml/Logs.qml` | e.g. `Runtime/Views/LogsView.axaml` (or under Screens module) | Back button, title "Logs", Clear Logs, ListView of console entries |
| `qml/Alarms.qml` | e.g. `Runtime/Views/AlarmsView.axaml` | Back button, title "Alarms", list/grid of alarms (from Alarms module) |

Data:
- Logs: bind to Console module (log list).
- Alarms: bind to Alarms module (current alarms list).

These can live in the Runtime host or in `RuntimeModules/Screens` and be referenced by ScreenManager so that "Load Logs screen" / "Load Alarms screen" show these views.

### 3.3 Project Screens (dynamic content)

**Qt:** ScreenManager loads QML from project (e.g. screen JSON + QML component tree); EventManager handles NavigateScreen/WriteTag/RunScript/etc.; AnimationManager drives visibility/color/flashing/translation from tag values; IndusysComponents are QML components.  
**C#:** 
- **ScreenManager:** Load screen from project (e.g. screen JSON); build or resolve an Avalonia control tree (see 3.4) and set it as the content of the main screen container. No QML engine.
- **EventManager:** Same logic as Qt: load `events.json`, handle triggers (OnClick, etc.) and actions (NavigateScreen, WriteTag, RunScript, ShowMessage, etc.); call TagIOHandler/TagManager/ScriptManager/ScreenManager as needed.
- **AnimationManager:** Same logic: load animations from project, subscribe to tag changes, update animation state (visibility, color, flashing, translation); expose state to the view layer so controls can bind.
- **HistorianQueryHelper:** Same API (e.g. set database path, query by tag and time range); TrendView and other controls call this for historical data.

### 3.4 IndusysComponents (QML → Avalonia UserControls)

Replace each QML component with an Avalonia UserControl (or Control) that matches the same properties and behavior. Prefer a single namespace/folder (e.g. `RuntimeModules/Screens/Controls/` or `Runtime/Controls/`) so the screen renderer can resolve by type name.

| QML Component | Avalonia Control | Notes |
|---------------|-------------------|--------|
| AlarmView.qml | AlarmView.axaml | Alarm list/row display |
| AnimationHelper.qml | Handled by AnimationManager + binding in controls | No separate control; logic in manager + view bindings |
| Button.qml | Button.axaml or standard Button + style | Click → EventManager by component id + trigger |
| Checkbox.qml | Checkbox.axaml | Bind to tag; write on change |
| CircularGauge.qml | CircularGauge.axaml | Gauge + tag binding |
| ComboBox.qml | ComboBox.axaml | Options + tag binding |
| Conveyor.qml | Conveyor.axaml | Industrial graphic |
| DateTime.qml | DateTime.axaml | Date/time display (and optional tag) |
| GaugeView.qml | GaugeView.axaml | Linear gauge, tag binding |
| ImageView.qml | ImageView.axaml | Image path from project |
| Indicator.qml | Indicator.axaml | On/off or state indicator |
| Line.qml | Line.axaml | Line shape |
| Motor.qml | Motor.axaml | Industrial graphic |
| Numeric.qml | Numeric.axaml | Numeric display/input, tag |
| Popup.qml | Popup.axaml | Popup container |
| ProgressBar.qml | ProgressBar.axaml | Min/max/value, tag |
| Pump.qml | Pump.axaml | Industrial graphic |
| RadioButton.qml | RadioButton.axaml | Group + tag |
| Rectangle.qml | Rectangle.axaml | Filled rectangle |
| Slider.qml | Slider.axaml | Min/max/value, tag |
| Spinner.qml | Spinner.axaml | Loading indicator |
| SVGView.qml | SVGView.axaml | SVG from project |
| Tab.qml | TabControl / TabItem.axaml | Tabs |
| TableView.qml | DataGrid.axaml | Table data + optional tag source |
| Tank.qml | Tank.axaml | Level, tag |
| Text.qml | TextBlock.axaml | Static text |
| TextInput.qml | TextBox.axaml | Text, optional tag |
| TextLabel.qml | TextBlock.axaml | Label |
| ToggleSwitch.qml | ToggleSwitch.axaml | On/off, tag |
| TrendView.qml | TrendView.axaml | HistorianQueryHelper + chart |
| Triangle.qml | Triangle.axaml | Triangle shape |

**Screen JSON → Avalonia:** The screen format (component type, id, position, size, properties) should stay the same or be adapted so that a **ScreenRenderer** (or equivalent) in the Screens module can:
- Parse screen JSON.
- For each component, instantiate the corresponding Avalonia control and set position/size/properties.
- Bind tag-based properties to TagsEngine (and AnimationManager where needed).
- Attach events (e.g. click) to EventManager with component id and trigger type.

---

## 4. Device Discovery and Project Transfer

**Qt:** `DeviceDiscoveryResponder`, `ProjectTransferServer`, `ProjectUploadClient` in `device_discovery/`.  
**C#:** Implement under `RuntimeModules/Discovery` (or keep naming consistent):
- **DeviceDiscoveryResponder:** Same protocol (e.g. UDP/broadcast or TCP) so Designer/Upload client can discover the device.
- **ProjectTransferServer:** TCP server; receive project upload (file list + file data), write to storage path, emit TransferStarted/TransferProgress/TransferCompleted/ErrorOccurred. Host (MainWindow) subscribes and shows overlay, then on completion delays and calls Project open + ExecutionEngine ReinitializeWithProject + EventManager/AnimationManager init + StartAll.
- **ProjectUploadClient:** Used by Designer to push project to device; can remain in Designer; Runtime only needs server + responder.

Expose transfer server (and optionally discovery) to the host so the main window can bind the overlay and any discovery-related UI.

---

## 5. Execution Engine and Config Load

**Qt:** `ExecutionEngine::initialize()`, `reinitializeWithProject(projectPath)` loads configs from project path (e.g. `communications.json`, tag tables, alarms, schedules, historian, etc.) and passes config to each module’s `initialize(config)`.  
**C#:**
- ExecutionEngine holds ModuleManager and project path.
- `Initialize()` – no project path; optional minimal setup (e.g. EventDispatcher).
- `ReinitializeWithProject(projectPath)` – read all JSON configs from project path, build per-module config (JObject), call each registered module’s `Initialize(config)`.
- After project load (or after transfer complete): also initialize EventManager with `events.json`, AnimationManager with `animations.json` (and project path if needed).
- `StartAll()` / `StopAll()` in dependency order; `Shutdown()` on exit.

Ensure project layout (e.g. `metadata.iscr`, `json/`, `qml/` or equivalent for screens) is defined and that the C# Project module and ExecutionEngine use the same layout.

---

## 6. Host Wiring Summary (replacing main.cpp + Main.qml)

1. **Create ExecutionEngine**, call `Initialize()`.
2. **Create and register core modules:** Communication, TagsEngine, Scheduler, Alarms, Historian, MLEngine (and optionally EventDispatcher first). Register with ExecutionEngine.
3. **Create UI/designer-facing modules:** ScriptingEngine, Screens, Console, Project. Set cross-links (Tags→Communication, Scheduler→Script, Historian←TagManager, EventManager←TagIOHandler/TagManager, AnimationManager←TagManager). Register if they implement IModuleInterface; otherwise hold references.
4. **Initialize Screens** (with Avalonia; e.g. pass main window or screen container reference to ScreenManager).
5. **Initialize Script, Console, Project.** Wire Project `screenAvailable` → ScreenManager `LoadScreen`.
6. **Start Discovery:** DeviceDiscoveryResponder, ProjectTransferServer. Subscribe TransferServer events in MainWindow for overlay; on TransferCompleted, delay then open project, ReinitializeWithProject, init EventManager/AnimationManager, StopAll/StartAll.
7. **Optional autoload:** If `data/metadata.iscr` exists, open project, then ReinitializeWithProject, init events/animations.
8. **StartAll()** on ExecutionEngine.
9. **Show MainWindow** (Avalonia). Toolbar and status bar and overlay are bound to the services above.
10. **On exit:** Shutdown ExecutionEngine.

---

## 7. File Layout (Suggested)

```
Runtime/
  Program.cs
  App.axaml (optional)
  Runtime.csproj
  MIGRATION_PLAN.md
  MIGRATION_CHECKLIST.md
  Views/
    MainWindow.axaml
    MainWindow.axaml.cs
    LogsView.axaml
    LogsView.axaml.cs
    AlarmsView.axaml
    AlarmsView.axaml.cs

RuntimeModules/
  ExecutionEngine/   – IModuleInterface, ModuleManager, ExecutionEngine
  EventDispatcher/
  Communication/
  TagsEngine/
  Scheduler/
  Alarms/
  Historian/
  MLEngine/
  ScriptingEngine/
  Console/
  Project/
  Discovery/         – DeviceDiscoveryResponder, ProjectTransferServer (ProjectUploadClient optional)
  Screens/           – ScreenManager, EventManager, AnimationManager, HistorianQueryHelper, ScreenRenderer, Controls/*.axaml
  Security/
```

Screens module may contain:
- `ScreenManager.cs`, `EventManager.cs`, `AnimationManager.cs`, `HistorianQueryHelper.cs`
- `Controls/` – one Avalonia control per IndusysComponent (AlarmView, Button, GaugeView, TrendView, etc.)
- Optional `ScreenRenderer.cs` that builds Avalonia tree from screen JSON.

---

## 8. Testing and Validation

- **Unit:** Per-module tests for config load, start/stop, and key APIs (e.g. Tags read/write, Alarms add/ack, Historian query, EventManager action execution).
- **Integration:** Run host with a test project: autoload or transfer, then open project; verify ExecutionEngine start order, connection status in status bar, Logs and Alarms screens, and one project screen with buttons/tags/trends.
- **Compatibility:** Use same project structure and JSON schemas as Qt runtime where possible so projects built in Designer (DarkStar or AccuTrack) can run unchanged.

---

## 9. References

- **Source Runtime:** `d:\Dev\AccuTrackQt\Runtime`
- **Designer migration (structure reference):** `Designer/MIGRATION_PLAN.md`, `Designer/MIGRATION_CHECKLIST.md`
- **Existing RuntimeModules:** `RuntimeModules/*/` (interfaces and stubs to be filled)

This plan should be used together with **Runtime/MIGRATION_CHECKLIST.md** for task-level tracking until the migration is complete.
