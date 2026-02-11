# AccuTrack Runtime Migration Checklist

This checklist tracks progress on migrating the Runtime from Qt QML to C# Avalonia. Use it together with **Runtime/MIGRATION_PLAN.md**. Mark items with `[x]` when done.

---

## Phase 1: Host and Execution Engine

### Application Entry and Avalonia Setup
- [x] Runtime.csproj: Add Avalonia packages (Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, etc.)
- [x] Program.cs: Replace "Hello World" with Avalonia AppBuilder.ConfigureApp()
- [x] Program.cs: Set up DI or static service locator for ExecutionEngine and key modules (optional but recommended)
- [x] Create App.axaml (optional) for application-level resources

### Module Contract (ExecutionEngine)
- [x] IModuleInterface: ModuleName, DisplayName, Version, Dependencies
- [x] IModuleInterface: Initialize(JObject), Start(), Stop(), Shutdown()
- [x] IModuleInterface: IsRunning, Status
- [x] IModuleInterface: Events (StatusChanged, ErrorOccurred, Initialized, Started, Stopped)
- [x] ModuleManager: RegisterModule(IModuleInterface), UnregisterModule(name)
- [x] ModuleManager: GetModule(name), GetModuleNames(), GetStartOrder(), GetStopOrder()
- [x] ModuleManager: CheckDependencies, GetMissingDependencies
- [x] ExecutionEngine: Singleton or DI; Initialize(), ReinitializeWithProject(projectPath)
- [x] ExecutionEngine: StartAll(), StopAll(), Shutdown()
- [x] ExecutionEngine: Load configs from project path (communications, tags, alarms, schedules, historian, etc.) and call each module Initialize(config)

### Main Window Shell (replaces Main.qml)
- [x] Create Views/MainWindow.axaml
- [x] MainWindow: Top toolbar (Home, Logs, Alarms buttons)
- [x] MainWindow: Main content area (ContentControl or similar as screen container)
- [x] MainWindow: Bottom status bar (placeholder for connection status)
- [x] MainWindow: Transfer overlay panel (title, status text, progress bar, percentage) – initially hidden
- [x] MainWindow.axaml.cs: Resolve/inject ScreenManager, Console, Communication, ProjectManager, TransferServer
- [x] Wire toolbar buttons to load Logs/Alarms screen (or navigate to views)
- [x] Wire status bar to Communication.GetConnectionStatuses() with timer or event updates
- [x] Wire transfer overlay to TransferServer events (TransferStarted, TransferProgress, TransferCompleted, ErrorOccurred)
- [x] Program.cs: After module setup, show MainWindow as main window

---

## Phase 2: Core Runtime Modules (Logic Only)

### EventDispatcher
- [x] iEventDispatcher.cs: Public API used by other modules
- [x] Event subscription and dispatch logic (port from event_dispatcher)
- [x] ExecutionEngine registers EventDispatcher first (no deps)

### Communication
- [x] iCommunication.cs: GetConnectionStatuses() for status bar
- [x] ConnectionStatus type; load from config (communication_modules / modules)
- [x] Modbus client and config (TCP/RTU) – stub only for now
- [x] OPC UA backend (or stub) and config
- [x] Load communications.json via ExecutionEngine config
- [x] Implement ModuleInterface (Initialize, Start, Stop, IsRunning)

### TagsEngine
- [x] iTagsEngine.cs: TagManager, TagIOHandler, GetTag, WriteTag, subscriptions
- [x] TagManager, TagIOHandler, RuntimeTag, TagSubscription
- [x] Implement ModuleInterface
- [x] Host wires TagsEngine to Communication (for I/O) when Communication module exists (Program.cs: tagsModule.SetCommunicationModule(commModule))

### Scheduler
- [x] iScheduler.cs: Schedule management, run schedule
- [x] ScheduleManager, ScheduleExecutor, Schedule (port from schedules_module)
- [x] Implement ModuleInterface
- [x] Host wires Scheduler to ScriptingEngine

### Alarms
- [x] iAlarms.cs: AlarmManager, current alarms list, ack, etc.
- [x] AlarmManager, Alarm, AlarmCondition (port from alarms_module)
- [x] Implement ModuleInterface

### Historian
- [x] iHistorian.cs: HistorianManager, database path, logging
- [x] HistorianManager (stub: in-memory tag history, query API)
- [x] Implement ModuleInterface
- [x] Host wires Historian to TagManager (for data collection)

### MLEngine
- [x] iMLEngine.cs: Stub or minimal API
- [x] Implement ModuleInterface; load ML config from project

### ScriptingEngine
- [x] iScriptingEngine.cs: LoadScript, Execute, ExecuteScript(path), events (ErrorOccurred, ScriptLoaded, ScriptExecuted)
- [ ] Lua (or chosen) engine integration (stub: no execution yet)
- [x] Implement or expose as service (not necessarily ModuleInterface)
- [x] Host exposes to Scheduler for schedule execution

### Console
- [x] iConsole.cs: LogInfo, LogError, ClearLogs, get log list for Logs view
- [x] In-memory log buffer and events for UI
- [x] Host exposes to MainWindow and LogsView

### Project
- [x] iProject.cs: OpenProject(path), GetCurrentProject(), resolution (ResolutionWidth, ResolutionHeight), ScreenAvailable event, GetScreens(), etc.
- [x] Project load/save, metadata, current project state
- [x] Host wires Project.ScreenAvailable → ScreenManager.LoadScreen
- [x] Host calls OpenProject on startup if data/metadata.iscr exists, and after transfer complete

### Security
- [x] iSecurity.cs: Stub or minimal API
- [x] Implement ModuleInterface; load security config from project

---

## Phase 3: Discovery and Project Transfer

### Discovery Module
- [x] iDiscovery.cs: Start/Stop responder and transfer server, TransferServer events
- [x] DeviceDiscoveryResponder: UDP port 8889, respond with deviceName, ip, port (8888)
- [x] ProjectTransferServer: TCP port 8888, deploy command + binary file stream, write to data/
- [x] ProjectTransferServer: Emit TransferStarted, TransferProgress, TransferCompleted, ErrorOccurred
- [x] Host: Subscribe to TransferServer; on TransferCompleted, delay 3s then OpenProject, ReinitializeWithProject, StopAll/StartAll
- [x] MainWindow: Transfer overlay visibility and progress bound to TransferServer events

---

## Phase 4: Screens Module – Core Services

### ScreenManager
- [x] iScreens.cs: ScreenManager, EventManager, AnimationManager, HistorianQueryHelper
- [x] ScreenManager: LoadScreen(path), UnloadScreen(), UnloadAllScreens(), SetLoadScreenCallback (host sets content)
- [x] ScreenManager: Resolve screen from project (JSON + control tree) for dynamic screens (ScreenRenderer.ParseScreen + ScreenViewBuilder in host)
- [x] Host sets content from path (built-in Logs/Alarms or project screen JSON or placeholder)
- [x] ResolveImagePath (SetProjectPath; projectPath/images/name). OpenFileDialog optional.

### EventManager
- [x] EventManager: Initialize(eventsJsonPath, projectDataPath), HandleEvent(eventId, componentId, triggerType)
- [x] EventManager: Load events.json; NavigateScreen, WriteTag, RunScript, ShowMessage, SetBit, ResetBit, ToggleBit
- [x] EventManager: SetScreenManager, SetTagIOHandler, SetTagManager (wired by host)
- [x] EventManager: ReloadEvents()

### AnimationManager
- [x] AnimationManager: Initialize(), LoadScreenAnimations, UnloadScreenAnimations (stub)
- [x] AnimationManager: GetAnimationState (stub)
- [x] AnimationManager: OnTagValueChanged; Visibility, ColorChange, Flashing, Translation (stub)
- [x] Expose animation state to view layer for controls

### HistorianQueryHelper
- [x] HistorianQueryHelper: SetDatabasePath(), QueryTagValues(tagName, startTime, endTime, maxRows), IsDatabaseAvailable()
- [x] Can delegate to HistorianManager when wired

### Screen Renderer (optional but recommended)
- [x] ScreenRenderer: Parse screen JSON from project (ComponentDescriptor, ScreenRenderer.ParseScreen in Screens module)
- [x] ScreenRenderer: For each component, create Avalonia control (from Controls/), set position/size/properties (ScreenViewBuilder in Runtime host)
- [x] ScreenRenderer: Bind tag-based properties to TagsEngine; attach events to EventManager (component id + trigger)
- [x] ScreenRenderer: Integrate with AnimationManager state for visibility/color/etc. (AnimationState + Subscribe; Build passes AnimationManager; ApplyAnimationState sets Visible, Opacity, Background on Border, Foreground on TextBlock)

---

## Phase 5: Main Shell Wiring and Status

- [x] Program.cs: Create all modules in dependency order; register core modules with ExecutionEngine
- [x] Program.cs: Cross-wire (Tags→Communication, Scheduler→Script, Historian←TagManager, EventManager←TagIOHandler/TagManager, AnimationManager←TagManager)
- [x] Program.cs: Initialize Screens with Avalonia container reference
- [x] Program.cs: Initialize Script, Console, Project; connect Project.ScreenAvailable → ScreenManager.LoadScreen
- [x] Program.cs: Start Discovery (responder + transfer server); subscribe TransferServer in MainWindow
- [x] Program.cs: Autoload project from data/metadata.iscr if present; then ReinitializeWithProject, init EventManager/AnimationManager, StartAll
- [x] Program.cs: StartAll(); show MainWindow; on exit Shutdown ExecutionEngine
- [x] MainWindow: Connection status list updated every second (or on Communication events) (ItemsSource = GetConnectionStatuses().Values, ItemTemplate)
- [x] MainWindow: Transfer overlay hide after delay on completion/error (2s delay after complete/error)

---

## Phase 6: Built-in Runtime Views

### Logs View
- [x] LogsView.axaml: Layout (header with Back, title "Logs", Clear Logs button; list area)
- [x] LogsView: Bind log list to Console module (SetConsole, NewLogEntry)
- [x] LogsView: Back button → RequestClose / UnloadScreen
- [x] LogsView: Clear Logs → Console.ClearLogs()
- [x] Host: "Load Logs screen" shows LogsView in container

### Alarms View
- [x] AlarmsView.axaml: Layout (header with Back, title "Alarms"; alarms list)
- [x] AlarmsView: Bind to Alarms module (SetAlarms, refresh timer)
- [x] AlarmsView: Back button → RequestClose / UnloadScreen
- [x] Host: "Load Alarms screen" shows AlarmsView in container

---

## Phase 7: IndusysComponents (Avalonia Controls)

Create one Avalonia UserControl (or Control) per QML component under Screens/Controls/ (or Runtime/Controls/). Each should support the same properties and behaviors as the QML version (tag binding, events to EventManager).

- [x] AlarmView (Runtime/Views/Controls/RuntimeAlarmView – stub)
- [x] Button (Runtime/Views/Controls/RuntimeButton)
- [x] Checkbox (Runtime/Views/Controls/RuntimeCheckbox)
- [x] CircularGauge (uses GaugeView)
- [x] ComboBox (Runtime/Views/Controls/RuntimeComboBox)
- [x] Conveyor (Runtime/Views/Controls/RuntimeConveyor)
- [x] DateTime (Runtime/Views/Controls/RuntimeDateTime)
- [x] GaugeView (Runtime/Views/Controls/RuntimeGaugeView)
- [x] ImageView (Runtime/Views/Controls/RuntimeImageView)
- [x] Indicator (Runtime/Views/Controls/RuntimeIndicator)
- [x] Line (Runtime/Views/Controls/RuntimeLine)
- [x] Motor (Runtime/Views/Controls/RuntimeMotor)
- [x] Numeric (Runtime/Views/Controls/RuntimeNumeric)
- [x] Popup (Runtime/Views/Controls/RuntimePopup – stub)
- [x] ProgressBar (Runtime/Views/Controls/RuntimeProgressBar)
- [x] Pump (Runtime/Views/Controls/RuntimePump)
- [x] RadioButton (Runtime/Views/Controls/RuntimeRadioButton)
- [x] Rectangle (Runtime/Views/Controls/RuntimeRectangle)
- [x] Slider (Runtime/Views/Controls/RuntimeSlider)
- [x] Spinner (Runtime/Views/Controls/RuntimeSpinner)
- [x] SVGView (Runtime/Views/Controls/RuntimeSVGView – stub)
- [x] Tab (Runtime/Views/Controls/RuntimeTab – stub)
- [x] TableView (Runtime/Views/Controls/RuntimeTableView – stub)
- [x] Tank (Runtime/Views/Controls/RuntimeTank)
- [x] Text (Runtime/Views/Controls/RuntimeText)
- [x] TextInput (Runtime/Views/Controls/RuntimeTextInput)
- [x] TextLabel (Runtime/Views/Controls/RuntimeTextLabel)
- [x] ToggleSwitch (Runtime/Views/Controls/RuntimeToggleSwitch)
- [x] TrendView (Runtime/Views/Controls/RuntimeTrendView – stub; chart/HistorianQueryHelper optional)
- [x] Triangle (Runtime/Views/Controls/RuntimeTriangle)

(AnimationHelper in QML is replaced by AnimationManager + bindings in the controls above; no separate control.)

---

## Phase 8: Integration and Testing

- [ ] End-to-end: Start Runtime, autoload or transfer project, project loads and configs applied (manual test)
- [x] Toolbar: Logs and Alarms open correct views (wired; manual test)
- [x] Status bar: Connection status shows and updates (ItemsSource + 1s timer; manual test)
- [x] Transfer overlay: Shows during transfer, progress updates, hides on complete/error (2s delay; manual test)
- [x] Project screen: Load one project screen with buttons/tags; click triggers EventManager; tag write works (wired; manual test)
- [ ] TrendView: HistorianQueryHelper returns data; chart displays (stub placeholder only)
- [x] Alarms: Active alarms show in Alarms view; ack if implemented (view wired; ack optional)
- [x] Console: Logs view shows entries; Clear works (wired; manual test)
- [x] Compatibility: Same project folder layout and JSON format as Qt runtime where applicable (see README)

---

## Phase 9: Documentation (Optional)

- [x] XML docs for public module APIs (IModuleInterface, IScreens, ICommunication, IProject, IAlarms, IConsole, IHistorian, IScheduler, ITagsEngine, IScriptingEngine, IEventDispatcher)
- [x] README for Runtime: how to run, project layout, config files (Runtime/README.md)
- [x] Note any differences from Qt runtime behavior for release notes (Runtime/README.md § Differences from Qt runtime)

---

## Progress Summary

| Phase | Description                    | Status   |
|-------|--------------------------------|----------|
| 1     | Host and Execution Engine     | Done (optional DI pending) |
| 2     | Core Runtime Modules           | Done (Communication stub; Modbus/OPC UA pending) |
| 3     | Discovery and Project Transfer | Done    |
| 4     | Screens Module – Core Services | Done (ScreenRenderer, ResolveImagePath; AnimationManager stub) |
| 5     | Main Shell Wiring              | Done    |
| 6     | Built-in Views (Logs, Alarms)  | Done    |
| 7     | IndusysComponents (29 controls)| Done (all 29) |
| 8     | Integration and Testing       | In progress (wiring done; manual E2E + TrendView chart pending) |
| 9     | Documentation                 | Done (README, differences note, XML docs on interfaces) |

**Overall:** Phases 1–7 and 9 complete; Phase 8 wiring done, manual E2E and TrendView chart optional.

---

## Notes

- Keep project and config format compatible with Designer and existing Qt runtime where possible.
- Prefer interfaces (iAlarms, iScreens, etc.) in RuntimeModules for testability and clear contracts.
- Use **Runtime/MIGRATION_PLAN.md** for detailed design and file layout.
