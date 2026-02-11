# DarkStar Designer/Runtime Baseline Analysis

This document maps key classes and flows in the DarkStar Designer and Runtime, and identifies extension points for the migration plan (compiler, project transfer, communications, screen rendering).

## 1. Designer Structure

### 1.1 Entry and shell
- **`Designer/Program.cs`**: Entry point; shows `StartupPage`, then `MainForm`. Passes new/open project choice to `MainForm.NewProject()` or `MainForm.OpenProjectPath(path)`.
- **`Designer/MainForm.cs`**: Main window. Holds references to `ProjectManager`, `ConsoleModule`, `CompilerModule`, `ProjectView`, `ComponentsView`, `PropertyEditor`, `ComponentsModule`, `SimulatorModule`. Connects menu actions (File, Build, Deploy, Upload, etc.) and wires project view events to open editors (screens, scripts, tag tables, communications, alarms, historian, schedules, security, machine learning).
- **Build/Deploy hooks**: `MainForm` calls `_compilerModule.CompileProject()` on Build; Deploy opens `DeployDialog` (simulated deploy only). No real project transfer client yet.

### 1.2 Compiler extension point
- **`DesignerModules/Compiler/CompilerModule.cs`**
  - `SetProject(ScadaProject)`, `SetConsoleModule(ConsoleModule)`.
  - `CompileProject()`: Validates project, creates build directory, then has TODO for actual compilation (screens, scripts, tag tables, communication modules, metadata, .iscr).
  - `CleanProject()`: Deletes and recreates build directory.
  - `ValidateProject()`: Checks project directory exists; TODOs for required files, tag/screen references, script syntax.
- **Gap**: No JSON config generation, no `metadata.iscr` generation, no screen/tag/script/comm output.

### 1.3 Project transfer (Designer side)
- **`DesignerModules/Discovery/DeployDialog.cs`**
  - UI: SCADA project combo, target device combo, target path, build-before-deploy, backup options, deployment log.
  - `OnDeployClick`: Simulates deployment with a timer; no TCP client, no file packaging, no call to Runtime.
- **Gap**: No `ProjectPackager`, no `ProjectTransferClient`; Designer never talks to Runtime's `ProjectTransferServer`.

### 1.4 Communications configuration editor
- **`DesignerModules/Communication/CommunicationModuleEditor.cs`**: DataGridView for modules; Add/Remove/Configure; types ModbusTCP, ModbusRTU, OPCUA, EthernetIP, BACnet, DNP3. `ConfigureSelectedModule()` opens Modbus or OPC UA dialog by type; EthernetIP/BACnet/DNP3 show "not yet implemented".
- **`DesignerModules/Communication/CommunicationModules.cs`**: `CommunicationModules` (list of `CommunicationModule`), `CommunicationModule` (Id, Name, Type, Settings, Enabled, Description); JSON serialization via `ToJson`/`FromJson`. Settings are `Dictionary<string, object>`.
- **`DesignerModules/Communication/OPCUAConnectionDialog.cs`**: Endpoint URL, Security Policy/Mode, auth (anonymous/username+password), session timeout; `GetSettings()` returns `Dictionary<string, object>`.
- **Gap**: OPC UA role (client/server) and server-specific settings not yet in model or dialog; Runtime uses stub OPC client only.

### 1.5 Project and paths
- **`DesignerModules/Project/ScadaProject.cs`**: Name, Path, Type, Paths, Resolution, Version; lists for Screens, TagTables, Scripts, CommunicationModules; Alarms, Schedules, Historian, Security, MachineLearning.
- **`ScadaPaths`**: RootPath, BuildPath, ScreensPath, TagsPath, ScriptsPath, CommunicationsPath, AlarmsPath, SchedulesPath, HistorianPath, SecurityPath, EventsPath, MachineLearningPath, MetadataPath. `InitializeFromRoot(root, scadaName)` sets BuildPath = root/build.

## 2. Runtime Structure

### 2.1 Entry and module registration
- **`Runtime/Program.cs`**: Builds `ExecutionEngine`, registers modules (EventDispatcher, Console, Security, MLEngine, Communication, Tags, Alarms, Scheduler, Historian), creates ProjectModule and ScreensModule, wires Project.ScreenAvailable → ScreenManager.LoadScreen, starts Discovery (TransferServer on 8888, DiscoveryResponder). On `TransferCompleted`: delay 3s, then `project.OpenProject(metadataPath)`, `engine.ReinitializeWithProject(projectPath)`, `screensModule.InitializeWithProject(projectPath)`, `WireAnimationManagerToTagManager`, `engine.StopAll()`, `engine.StartAll()`. If `data/metadata.iscr` exists at startup, opens project and reinitializes. Then `engine.StartAll()` and `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`.

### 2.2 Execution engine
- **`RuntimeModules/ExecutionEngine/ExecutionEngine.cs`**: Singleton. `ModuleManager`, `ProjectPath`, `Initialize()`, `ReinitializeWithProject(projectPath)` (loads configs from `projectPath/json`, calls `Initialize(config)` on each module in start order), `StartAll()`, `StopAll()`, `Shutdown()`. `LoadModuleConfig(moduleName, jsonDirectory)` maps module names to filenames (e.g. CommunicationModule → communications.json, TagsModule → tag_tables.json, AlarmsModule → alarms.json, etc.).

### 2.3 Project transfer (Runtime side)
- **`RuntimeModules/Discovery/ProjectTransferServer.cs`**: TCP listener on configurable port (default 8888). Protocol: receive JSON line with `command: "deploy"`, `projectName`, `fileCount`, `totalSize`; clears `GetStoragePath()` (data/), creates it, sends `{"status":"ready"}\n`. Then receives files as `[pathLen:4][path][size:4][bytes]`; pathLen==0 is end marker. Writes files under storage path, fires `TransferStarted`, `TransferProgress`, `TransferCompleted(projectPath)`.
- **Gap**: Designer has no client sending this protocol; unload is implicit (data/ cleared before receive). No explicit "unload previous project" step beyond clearing directory and reinitializing.

### 2.4 Project module
- **`RuntimeModules/Project/ProjectModule.cs`**: `OpenProject(path)` (path can be metadata.iscr or directory). Loads from `metadata.iscr` and `json/` (communications, screens, scripts, tags, alarms, historian, security). Expects `json/screens.json` with array of screens (id, name); resolves per-screen file as `projectDir/screens/{id}.json`. Fires `ScreenAvailable` with first screen path (full path to screen JSON). Also loads communications, scripts, tags for project metadata.

### 2.5 Screen rendering
- **`RuntimeModules/Screens/ScreenManager.cs`**: `SetLoadScreenCallback(callback)`; `LoadScreen(screenPath)` invokes callback so host sets content; `UnloadScreen`, `UnloadAllScreens`; `SetProjectPath` for `ResolveImagePath(imageName)` → `projectPath/images/{imageName}`.
- **`RuntimeModules/Screens/ScreenRenderer.cs`**: `ParseScreen(jsonPath)` reads JSON with `id`, `name`, `backgroundColor`, `size{width,height}`, `components[]`. Each component: `id`, `componentType`, `name`, `visible`, `enabled`, `zOrder`, `tagName`, `location{x,y}`, `size{width,height}`, `properties`. Returns `ScreenDescriptor` with `ComponentDescriptor` list.
- **`Runtime/ScreenViewBuilder.cs`**: `Build(screen, tagManager, eventManager, tagIOHandler, resolveImagePath, animationManager)` builds Avalonia control tree from `ScreenDescriptor`: Canvas with controls by `ComponentType` (Button, TextLabel, GaugeView, Checkbox, ProgressBar, Indicator, Slider, TextInput, Rectangle, Line, ToggleSwitch, Numeric, ImageView, Text, RadioButton, DateTime, TrendView, AlarmView, Tank, Motor, Pump, Triangle, Spinner, ComboBox, Tab, Conveyor, SVGView, TableView, Popup, etc.). Wires tag subscriptions and event triggers.
- **`Runtime/Views/MainWindow.axaml.cs`**: Resolves ScreensModule, TagManager, EventManager, TagIOHandler; sets `ScreenManager.SetLoadScreenCallback`. Callback: if path is Logs/Alarms shows built-in view; else `ScreenRenderer.ParseScreen(path)` and `ScreenViewBuilder.Build(...)` and sets content. Transfer overlay bound to TransferServer events.

### 2.6 Communications (Runtime)
- **RuntimeModules/Communication**: CommunicationModule loads config; OPC UA is stub (parses endpoint, no real connection). Need real OPC UA client and server using UA-.NETStandard.

## 3. Expected build output layout (for compiler)

So that Runtime can load a deployed project, the compiler must produce:

- `build/<projectName>/` (or same structure under `data/` when deployed)
  - `metadata.iscr` – JSON manifest: name, type, version, resolution, configFiles (paths to json files).
  - `json/communications.json` – format compatible with ProjectModule and CommunicationModule (e.g. modules or communication_modules array).
  - `json/screens.json` – array of { id, name } for each screen.
  - `json/tags.json` or `tag_tables.json` – tags array.
  - `json/scripts.json` – scripts array.
  - `json/alarms.json`, `json/historian.json`, `json/security.json`, etc. as needed by ExecutionEngine.
  - `screens/<id>.json` – per-screen JSON matching ScreenRenderer schema (id, name, backgroundColor, size, components with componentType, location, size, tagName, properties, etc.).

## 4. TODOs blocking end-to-end path

| Area | TODO / gap |
|------|-------------|
| Compiler | Implement full compilation: generate all json/*.json, metadata.iscr, screens/*.json; validate project. |
| Project transfer | Implement Designer ProjectPackager + ProjectTransferClient; wire DeployDialog to compile then transfer. |
| Runtime transfer | Already clears data/ and reinitializes; ensure all modules fully reinit (no stale refs). |
| Communications | Add OPC UA role (client/server) to Designer model and dialog; implement real OPC UA client and server in Runtime. |
| Screen JSON | Ensure Designer screen/component serialization matches Runtime ScreenRenderer + ScreenViewBuilder schema. |

## 5. Key file reference

| Purpose | Path |
|--------|------|
| Designer entry | Designer/Program.cs |
| Main form, build/deploy hooks | Designer/MainForm.cs |
| Compiler | DesignerModules/Compiler/CompilerModule.cs |
| Deploy dialog | DesignerModules/Discovery/DeployDialog.cs |
| Comms editor | DesignerModules/Communication/CommunicationModuleEditor.cs |
| Comms model | DesignerModules/Communication/CommunicationModules.cs |
| Project/paths | DesignerModules/Project/ScadaProject.cs, ScadaPaths |
| Runtime entry | Runtime/Program.cs |
| Execution engine | RuntimeModules/ExecutionEngine/ExecutionEngine.cs |
| Transfer server | RuntimeModules/Discovery/ProjectTransferServer.cs |
| Project load | RuntimeModules/Project/ProjectModule.cs |
| Screen manager/renderer | RuntimeModules/Screens/ScreenManager.cs, ScreenRenderer.cs |
| Avalonia view builder | Runtime/ScreenViewBuilder.cs |
| Main window | Runtime/Views/MainWindow.axaml.cs |
