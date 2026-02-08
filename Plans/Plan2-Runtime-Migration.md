# Plan 2: AccuTrack Runtime – Qt/QML to C# Avalonia Migration

This plan guides the migration of the AccuTrack Runtime from Qt (C++/QML) to C# Avalonia. It references the original Qt implementation at **`d:\Dev\AccuTrackQt\Runtime`** and the target C# solution at **`d:\Dev\AccuTrack\Runtime`**.

---

## Agent instructions (parallel work)

- **Scope**: Runtime only. Do not modify Designer or SDK source; reference SDK (Plan 3) as a package and consume project/compiler output from Designer (Plan 1).
- **Inputs from other plans**: Use **Plan 3 (SDK)** for `ICommunicationModule` and protocol drivers (Modbus, OPC, S7); Runtime Communication and Tags modules depend on SDK. Use **Plan 1** for project layout (metadata, `json/` folder), JSON file names/schemas, and Avalonia screen output format so Runtime can load projects built by the Designer.
- **Outputs for other plans**: None (Runtime is consumer). Ensure config file names and project path layout match what Plan 1 Designer compiler produces (§11).
- **Parity with Qt Runtime**: Implement the same module set, same lifecycle (register → initialize → start/stop), same cross-module wiring as **`d:\Dev\AccuTrackQt\Runtime\main.cpp`**; see §13 Parity checklist.
- **When running as one of three agents**: Assume Plan 1 and Plan 3 are parallel; rely on agreed SDK interfaces (Plan 3) and project/JSON contract (Plan 1 §17); do not change JSON schema or project layout without aligning with Plan 1.

### Desired conditions (this plan must satisfy)

1. **Runtime** is implemented in **C# Avalonia** (replacing QML).
2. **References** the initial Qt Runtime at **`d:\Dev\AccuTrackQt\Runtime`** for structure, modules, and behavior (see §1 and per-module Qt paths).
3. **Modules** are created **similarly to the previous Qt implementation**: same module set, same lifecycle (register → initialize → start/stop), same cross-module wiring; see §13 Parity checklist.
4. **Execution engine** and **module interface** mirror Qt `execution_engine` and `module_interface.h`.
5. **Project and config** layout match Designer output (Plan 1 §17); Runtime loads projects built by Designer.
6. Plan is **extensive** enough to drive implementation and to be used by one of three parallel agents.

---

## 1. Reference: Qt Runtime Structure

### 1.1 Entry and Shell

| Qt Path | Purpose |
|--------|---------|
| `d:\Dev\AccuTrackQt\Runtime\main.cpp` | Creates `QApplication`, `QQmlApplicationEngine`; builds ExecutionEngine, registers modules, sets up QML context properties, loads `Main.qml`. |
| `d:\Dev\AccuTrackQt\Runtime\Main.qml` | Main window: resolution from `projectManager`, toolbar (Logs, Alarms), screen container (`screenManager.container`), status bar (connection status), transfer overlay, Connections to screenManager/scriptManager/consoleManager/projectManager/transferServer/communicationModule. |

### 1.2 Module Order (from `CMakeLists.txt`)

SDK first, then:

1. **event_dispatcher** (before execution_engine)
2. **execution_engine**
3. alarms_module  
4. communication_module  
5. historian_module  
6. script_module  
7. tags_module  
8. screens_module  
9. security_module  
10. console_module  
11. schedules_module  
12. project_module  
13. device_discovery  
14. machine_learning  

### 1.3 Module-to-Module Mapping (Qt Runtime → C# Runtime)

| Qt Runtime Module | Path (Qt) | C# Runtime (to create) | Notes |
|-------------------|-----------|------------------------|--------|
| **event_dispatcher** | `Runtime\event_dispatcher\` | `Runtime\EventDispatcher\` or shared core | Singleton; event queue, subscriptions, priority. |
| **execution_engine** | `Runtime\execution_engine\` | `Runtime\ExecutionEngine\` | Singleton; ModuleManager, lifecycle, config load. |
| **alarms_module** | `Runtime\alarms_module\` | `Runtime\Alarms\` | Implements ModuleInterface; alarm conditions, manager. |
| **communication_module** | `Runtime\communication_module\` | `Runtime\Communication\` | Implements ModuleInterface; wraps SDK drivers. |
| **historian_module** | `Runtime\historian_module\` | `Runtime\Historian\` | Implements ModuleInterface; data collection, retention. |
| **script_module** | `Runtime\script_module\` | `Runtime\Script\` | Script execution (Lua or other); not ModuleInterface in Qt. |
| **tags_module** | `Runtime\tags_module\` | `Runtime\Tags\` | Implements ModuleInterface; TagManager, TagIOHandler. |
| **screens_module** | `Runtime\screens_module\` | `Runtime\Screens\` | ScreenManager, EventManager, AnimationManager, QML load → Avalonia views. |
| **security_module** | `Runtime\security_module\` | `Runtime\Security\` | Implements ModuleInterface if used. |
| **console_module** | `Runtime\console_module\` | `Runtime\Console\` | Logging; not ModuleInterface. |
| **schedules_module** | `Runtime\schedules_module\` | `Runtime\Schedules\` | Implements ModuleInterface. |
| **project_module** | `Runtime\project_module\` | `Runtime\Project\` | Project load, storage; not ModuleInterface. |
| **device_discovery** | `Runtime\device_discovery\` | `Runtime\DeviceDiscovery\` | Discovery responder, project transfer server. |
| **machine_learning** | `Runtime\machine_learning\` | `Runtime\MachineLearning\` | Implements ModuleInterface. |

---

## 2. Execution Engine and Module Interface

### 2.1 Qt Reference

- **`execution_engine\module_interface.h`**: Base interface for runtime modules.
  - `moduleName()`, `displayName()`, `version()`, `dependencies()`.
  - `initialize(config)`, `start()`, `stop()`, `shutdown()`, `isRunning()`, `status()`, `getConfiguration()`, `validateConfiguration(config)`.
  - Signals: `statusChanged`, `errorOccurred`, `initialized`, `started`, `stopped`.
- **`execution_engine\module_manager.h`**: Register/unregister modules, `getModule(name)`, `getStartOrder()` (topological), `getStopOrder()`, `checkDependencies`, `getMissingDependencies`.
- **`execution_engine\execution_engine.h/.cpp`**: Singleton; `initialize(projectPath)`, `shutdown()`, `registerModule()`, `loadModuleConfiguration()`, `loadAllConfigurations()`, `reinitializeWithProject()`, `startAll()`, `stopAll()`, `startModule()`, `stopModule()`, `getModule()`. Uses EventDispatcher; loads JSON configs from project path.

### 2.2 C# Runtime Tasks

1. **IModuleInterface** (or abstract `ModuleBase`): Same contract as Qt `ModuleInterface`; use `System.Text.Json` or equivalent for config (replace `QJsonObject`).
2. **ModuleManager**: Registry of modules; dependency list per module; topological sort for start/stop order.
3. **ExecutionEngine**: Singleton; initialize with optional project path; register modules; load configs from project directory (`json/*.json`); `ReinitializeWithProject(projectPath)`; `StartAll()` / `StopAll()`; on shutdown call `StopAll()` and cleanup.
4. **Config loading**: Map Qt `loadAllConfigurations(projectPath)` to reading e.g. `projectPath/json/tags.json`, `communication.json`, `alarms.json`, etc., and passing to each module’s `Initialize(config)`. File names and schema must match Designer output (Plan 1 §17).

---

## 3. Event Dispatcher

### 3.1 Qt Reference

- **`event_dispatcher\event_dispatcher.h`**: Singleton; `initialize()`, `shutdown()`; `publish(event)`, `publishSync(event)`; `subscribe(eventType, receiver, sourceFilter)` / `subscribe(eventType, callback, sourceFilter)`; `unsubscribe(subscription)`; `pendingEventCount()`, `subscriptionCount()`; thread-safe queue and delivery; `Event`, `EventSubscription`, `EventType`.

### 3.2 C# Runtime Tasks

1. **Event**: Class with type (enum or string), source, payload (e.g. `Dictionary<string, object>` or JSON).
2. **EventSubscription**: Token/callback for unsubscribe.
3. **EventDispatcher**: Singleton; queue (e.g. `Channel` or lock + queue); subscribe by type (and optional source filter); dispatch on dedicated thread or sync; initialize/shutdown.

---

## 4. Core Modules (Implement IModuleInterface)

### 4.1 Tags Module

**Qt**: `tags_module\tags_module.h/.cpp`, `tag_manager.h/.cpp`, `tag_io_handler.h/.cpp`, `tag_address_parser`, `runtime_tag`, `global_tag_storage`, `tag_subscription`.

- **C#**: `TagsModule` implementing `IModuleInterface`; `TagManager` (load from JSON, in-memory tag list, get/set value); `TagIOHandler` (read/write via Communication module); address parsing for Modbus/OPC (can delegate to SDK). Dependencies: EventDispatcher, CommunicationModule. Config: `tags.json`.

### 4.2 Communication Module

**Qt**: `communication_module\` — wraps SDK CommunicationModule instances (Modbus, OPC, S7); manages connections; exposes status to QML.

- **C#**: `CommunicationModule` implementing `IModuleInterface`; holds list of SDK driver instances (`ICommunicationModule` from Plan 3); load from `communication.json`; start/stop each driver; expose connection status list for UI (status bar). Config: `communication.json`.

### 4.3 Alarms Module

**Qt**: `alarms_module\alarm_manager.cpp`, `alarm_condition.cpp`, `alarm.cpp`.

- **C#**: `AlarmsModule` implementing `IModuleInterface`; `AlarmManager` with conditions from config; subscribe to tag changes (via Tags or EventDispatcher); evaluate conditions; raise/clear alarms; expose list for AlarmView. Config: `alarms.json`.

### 4.4 Historian Module

**Qt**: `historian_module\` — data_collector, historian_database, data_retention, historian_manager; uses TagManager for tag values.

- **C#**: `HistorianModule` implementing `IModuleInterface`; sample configured tags at interval; store in DB (SQLite or same as Qt); retention policy; HistorianQueryHelper for trend screens. Config: `historian.json`. Dependency: TagManager.

### 4.5 Schedules Module

**Qt**: `schedules_module\` — schedule execution (cron-like or time-based); may trigger scripts.

- **C#**: `SchedulesModule` implementing `IModuleInterface`; parse schedule config; timer-based execution; call Script module. Config: `schedules.json`. Dependency: Script module.

### 4.6 Security Module

**Qt**: `security_module\` — minimal in Qt.

- **C#**: Stub or simple role check if required; implement `IModuleInterface` for consistency.

### 4.7 Machine Learning Module

**Qt**: `machine_learning\` — ML module implementing ModuleInterface.

- **C#**: `MachineLearningModule` implementing `IModuleInterface`; load model/config; run inference on tag inputs if used; otherwise minimal stub. Config: `machinelearning.json`.

---

## 5. Project Module

### 5.1 Qt Reference

- **`project_module\project_module.cpp/.h`**, **project_storage_manager**: Open project (e.g. `metadata.iscr`), resolution, project name; signal `screenAvailable` for ScreenManager.

### 5.2 C# Runtime Tasks

1. **ProjectModule** (or ProjectManager): Load project from path (e.g. `data/` or path from transfer); read metadata; set resolution (width/height); expose to UI.
2. **Screen loading**: When project is loaded, notify ScreenManager of available screens (from built project JSON/Avalonia); ScreenManager loads views (Avalonia instead of QML).

---

## 6. Screens Module (Avalonia replaces QML)

### 6.1 Qt Reference

- **`screens_module\screenmanager.cpp/.h`**: Load/unload screens; `container` set from QML; loads QML from path (e.g. `qrc:/screens_module/qml/Logs.qml`, `Alarms.qml`) or generated QML.
- **`screens_module\eventmanager.cpp/.h`**: Events from UI (e.g. button click → script or WriteTag); `setTagIOHandler`, `setTagManager`.
- **`screens_module\animationmanager.cpp/.h`**: Animations bound to tag values; `setTagManager`.
- **`screens_module\historianqueryhelper.cpp/.h`**: Queries for trend views.
- **`screens_module\qml\`**: IndusysComponents (AlarmView, Button, Checkbox, CircularGauge, ComboBox, Conveyor, DateTime, GaugeView, ImageView, Indicator, Line, Motor, Numeric, Popup, ProgressBar, Pump, etc.) and screens (Alarms.qml, Logs.qml).

### 6.2 C# Runtime Tasks

1. **ScreenManager**: 
   - Holds current “screen” content; instead of QML component, host an Avalonia `Control` or `UserControl` (generated by Designer compiler — Plan 1).
   - Load screen by name/path: resolve to Avalonia view type (e.g. from assembly or dynamic load); set as content of main screen container.
   - Unload: clear container. API similar to Qt: `LoadScreen(pathOrName)`, `UnloadScreen(pathOrName)`, `Container` property for main window to bind content to.

2. **EventManager**: 
   - Register events from screen model (e.g. OnClick → script, Navigate, WriteTag); when user clicks a control, EventManager runs the action (call Script module, or TagIOHandler.Write).
   - Set TagIOHandler and TagManager from Tags module.

3. **AnimationManager**: 
   - Bind tag values to visual properties (e.g. gauge value, tank level); use TagManager to subscribe to tag updates; apply to Avalonia control properties. Initialize from `animations.json`.

4. **HistorianQueryHelper**: 
   - Query historian DB for trend data; expose to Avalonia views (e.g. TrendView control) via interface or view model.

5. **Avalonia “IndusysComponents”**: 
   - One Avalonia control per Qt QML component: Button, CheckBox, Slider, ProgressBar, TextBlock, Numeric, ComboBox, ImageView, Line, Rectangle, Triangle, SVGView, Tank, Motor, Pump, Conveyor, GaugeView, CircularGauge, TrendView, AlarmView, DateTime, Popup, Tab, TableView, TextInput, ToggleSwitch, Spinner, etc. Each control:
     - Binds to TagManager (for read/write tags where applicable).
     - Raises events to EventManager (e.g. Click, ValueChanged).
     - Supports bindings for AnimationManager (e.g. value property).
   - Place these in e.g. `Runtime\Screens\Controls\` or a shared library so Designer’s compiler can reference the same control types when generating AXAML.

6. **Main window content**: 
   - Single content area (e.g. `ContentControl` or panel) that ScreenManager sets as the current view; toolbar above (Logs, Alarms buttons) and status bar below as in Main.qml.

---

## 7. Script Module

### 7.1 Qt Reference

- **`script_module\`**: Lua execution; scriptManager exposed to QML; run script by name/path; errors to console.

### 7.2 C# Runtime Tasks

1. **ScriptModule**: Load scripts from project; execute by name; use Lua (e.g. NLua, LuaInterface) or another engine; expose to EventManager and Schedules.
2. **API for scripts**: If Lua, expose TagManager read/write, navigation (screen change), and log to Console; mirror Qt script API where applicable.

---

## 8. Console Module

### 8.1 Qt Reference

- **`console_module\`**: Logging; exposed as `consoleManager` to QML; `logError`, `logInfo`, etc.

### 8.2 C# Runtime Tasks

1. **ConsoleModule**: In-memory log list or file; methods `LogError`, `LogInfo`, etc.; expose to Avalonia Logs screen and status. No ModuleInterface.

---

## 9. Device Discovery and Project Transfer

### 9.1 Qt Reference

- **`device_discovery\device_discovery_responder.cpp/.h`**: Responds to discovery requests (e.g. UDP broadcast).
- **`device_discovery\project_transfer_server.cpp/.h`**: Receives project upload from Designer; saves to disk; signals `transferStarted`, `transferProgress`, `transferCompleted`; then main.cpp loads project and reinitializes engine.
- **`project_upload_client`**: Runtime as client (e.g. upload from device back to designer) if used.

### 9.2 C# Runtime Tasks

1. **DeviceDiscoveryResponder**: Same protocol as Qt (UDP or HTTP); respond with device name/IP so Designer can list devices.
2. **ProjectTransferServer**: HTTP or custom TCP server; receive uploaded project (zip or files); save to e.g. `data/`; raise events for progress and completion; on completion, call `ProjectModule.OpenProject(path)` and `ExecutionEngine.ReinitializeWithProject(projectPath)`, then restart modules and load screens (mirror main.cpp transfer completion logic).
3. **Main window**: Transfer overlay (progress bar, status text) bound to transfer server events; hide when transfer complete and project loaded.

---

## 10. Main Application and Avalonia Shell

### 10.1 Qt Reference (Main.qml)

- Window size from `projectManager.resolutionWidth/Height`.
- Toolbar: Logs button (load Logs screen), Alarms button (load Alarms screen).
- Main content: Rectangle with `Item` (screenParent) where `screenManager.container = screenParent`.
- Status bar: Connection status per communication module (Repeater over connectionStatusModel); update from `communicationModule.getConnectionStatuses()` and timer.
- Transfer overlay: Visible during transfer; progress and status from `transferServer`.
- Connections: screenManager (onErrorOccurred, onScreenLoaded, onScreenUnloaded); scriptManager (onErrorOccurred, etc.); consoleManager; projectManager (onProjectLoaded); transferServer (onTransferStarted, onTransferProgress, onTransferCompleted, onErrorOccurred); communicationModule (onModuleStatusChanged, onModuleAdded, onModuleRemoved, onCommunicationError).

### 10.2 C# Avalonia Tasks

1. **MainWindow.axaml**: 
   - Top: toolbar with “Logs” and “Alarms” buttons; click → ScreenManager.LoadScreen(Logs) / LoadScreen(Alarms).
   - Center: ContentControl (or similar) bound to ScreenManager’s current view (or direct assignment when screen loads).
   - Bottom: Status bar with list of connection statuses (bound to CommunicationModule status list); update every second (timer or binding).
   - Overlay: Panel for transfer progress (visibility bound to transfer state); progress bar and text bound to ProjectTransferServer.

2. **MainWindowViewModel** (optional): 
   - Resolution (Width, Height) from ProjectManager; ConnectionStatusList; TransferVisible, TransferProgress, TransferStatus; Commands for Logs/Alarms.

3. **App.axaml / Program.cs**: 
   - Create ExecutionEngine singleton and initialize.
   - Register all modules (Tags, Communication, Alarms, Historian, Schedules, Security, MachineLearning with engine; Script, Console, Project, Screens without ModuleInterface).
   - Wire cross-module: Tags ↔ Communication; Schedules ↔ Script; Historian ↔ TagManager; Screens (EventManager, AnimationManager) ↔ TagManager, TagIOHandler; Project ↔ ScreenManager (screen available).
   - Initialize ScreenManager with main content target; initialize Script, Console, Project; expose to DI or static access for views.
   - Start DeviceDiscoveryResponder and ProjectTransferServer.
   - If preloaded project in `data/`, open it and call ReinitializeWithProject; then StartAll.
   - Load main window (Avalonia); on exit, ExecutionEngine.Shutdown().

4. **Dependency injection**: Prefer a small DI container (e.g. Microsoft.Extensions.DependencyInjection) to register ExecutionEngine, ModuleManager, TagManager, CommunicationModule, ScreenManager, EventManager, AnimationManager, ScriptModule, ConsoleModule, ProjectManager, ProjectTransferServer, and inject into MainWindowViewModel and views where needed.

---

## 11. Configuration and Project Paths

- **Project path**: Same as Qt — e.g. `applicationDir/data` or path received from transfer; metadata at `metadata.iscr` (or equivalent). Must match what Designer (Plan 1) produces and what ProjectTransferServer saves.
- **JSON configs**: Under `projectPath/json/` — tags.json, communication.json, alarms.json, schedules.json, historian.json, security.json, events.json, animations.json, machinelearning.json, plus screens/scripts metadata. **Same schema and file names as Designer output (Plan 1 §17)**; do not add or rename files without updating Plan 1.

---

## 12. Implementation Order Suggestion

1. **SDK** (Plan 3): ICommunicationModule, Modbus/OPC (and S7 if used), so Runtime can drive I/O.
2. **EventDispatcher**: Singleton, publish/subscribe, thread-safe.
3. **ExecutionEngine + ModuleManager + IModuleInterface**: Register and start/stop modules; load configs from project path.
4. **TagsModule**: TagManager, TagIOHandler; load tags.json; integrate with Communication (SDK) for read/write.
5. **CommunicationModule**: Wrap SDK drivers; load communication.json; expose connection status.
6. **ProjectModule**: Load project, resolution; signal screen list to ScreenManager.
7. **ScreenManager**: Placeholder that can set a single “current view”; implement LoadScreen/UnloadScreen with stub views first.
8. **Avalonia shell**: MainWindow with toolbar, content area, status bar; bind content to ScreenManager; no real screens yet.
9. **ScriptModule**: Load and run scripts; wire to EventManager later.
10. **ConsoleModule**: Log list; wire to Script and ScreenManager errors.
11. **EventManager**: Wire to ScreenManager and TagIOHandler; implement OnClick/WriteTag/Navigate.
12. **AnimationManager**: Subscribe to TagManager; bind to control properties (start with one control type).
13. **AlarmsModule, HistorianModule, SchedulesModule, SecurityModule, MachineLearningModule**: One by one; Historian depends on TagManager.
14. **IndusysComponents**: Implement Avalonia controls (Button, Numeric, Tank, Gauge, TrendView, AlarmView, etc.) and register with EventManager/AnimationManager.
15. **Screen loading from project**: Resolve screen name to generated Avalonia view (from Designer compiler); instantiate and set as content.
16. **DeviceDiscoveryResponder + ProjectTransferServer**: Discovery and transfer; on transfer complete, load project and reinitialize engine.
17. **Transfer overlay and connection status**: Bind UI to transfer and communication status.

This order gives a runnable runtime with tags and communication first, then UI shell, then screens and events/animations, then deployment.

---

## 13. Parity checklist (Qt Runtime)

Implement the following so the C# Runtime behaves like the Qt Runtime:

- [ ] **Module set**: event_dispatcher, execution_engine, alarms, communication, historian, script, tags, screens, security, console, schedules, project, device_discovery, machine_learning (see §1.2).
- [ ] **Start order**: EventDispatcher initialized first; then ExecutionEngine; then core modules registered and started in dependency order (Tags depends on Communication; Historian on TagManager; Schedules on Script; EventManager/AnimationManager on TagManager/TagIOHandler). Mirror **`d:\Dev\AccuTrackQt\Runtime\main.cpp`** registration and wiring.
- [ ] **Config loading**: Load from `projectPath/json/` the same files Designer produces (Plan 1 §17): tags.json, communication.json, alarms.json, schedules.json, historian.json, security.json, events.json, animations.json, machinelearning.json; pass to each module’s Initialize.
- [ ] **Context properties → DI**: Qt exposes screenManager, scriptManager, consoleManager, projectManager, transferServer, communicationModule, TagManager, EventManager, AnimationManager, HistorianQueryHelper to QML. In C#, expose the same services via DI or a service locator so MainWindow and screens can access them.
- [ ] **Transfer flow**: On ProjectTransferServer.TransferCompleted, after delay (e.g. 3s as in Qt main.cpp), call ProjectModule.OpenProject(path), ExecutionEngine.ReinitializeWithProject(projectPath), initialize EventManager with events.json and AnimationManager with animations.json, then StopAll/StartAll. See **`d:\Dev\AccuTrackQt\Runtime\main.cpp`** (transferCompleted handler).
- [ ] **Preloaded project**: If `data/metadata.iscr` exists at startup, open project and ReinitializeWithProject(dataFolderPath) then StartAll; same as Qt main.cpp.

---

## 14. Acceptance criteria (Runtime)

- [ ] ExecutionEngine singleton; initialize with optional project path; register modules; load configs from project `json/`; StartAll/StopAll; ReinitializeWithProject works.
- [ ] All IModuleInterface modules (Tags, Communication, Alarms, Historian, Schedules, Security, MachineLearning) implement initialize/start/stop and load from their JSON config.
- [ ] MainWindow (Avalonia): toolbar (Logs, Alarms), content area bound to ScreenManager, status bar (connection status), transfer overlay; resolution from ProjectManager.
- [ ] ScreenManager loads Avalonia views (generated by Designer — Plan 1); EventManager and AnimationManager wired to TagManager/TagIOHandler.
- [ ] DeviceDiscoveryResponder and ProjectTransferServer run; on transfer complete, project loads and engine reinitializes as in §13.
- [ ] Project built by Designer (Plan 1) runs in this Runtime without schema or path changes.
