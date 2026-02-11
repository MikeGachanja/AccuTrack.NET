# DarkStar Runtime

C# Avalonia host for running deployed SCADA projects. Replaces the Qt/QML AccuTrack Runtime.

## How to run

1. **Build:** From the solution root or this folder:
   ```bash
   dotnet build Runtime/Runtime.csproj
   ```
   Or open the solution in Visual Studio and build the **Runtime** project.

2. **Run:** Execute the built executable (e.g. `bin/Debug/Runtime/Runtime.exe` on Windows), or:
   ```bash
   dotnet run --project Runtime/Runtime.csproj
   ```

3. **With a project:** Place a deployed project under a `data` folder next to the executable:
   - `data/metadata.iscr` – project metadata (required for autoload)
   - `data/json/` – config files (screens.json, tags.json, communications.json, events.json, etc.)
   - `data/screens/*.json` – screen definitions
   - `data/images/` – images referenced by ImageView (resolved via `ResolveImagePath`)

   On startup, if `data/metadata.iscr` exists, the Runtime opens the project and loads the first screen.

## Project layout (deployed project)

Expected layout under the project root (e.g. `data/` after transfer or copy):

| Path | Description |
|------|-------------|
| `metadata.iscr` | Project metadata (name, version, resolution, config paths) |
| `json/screens.json` | List of screens (id, name) |
| `json/tags.json` | Tag definitions |
| `json/communications.json` | Communication modules (Modbus, OPC UA, etc.) |
| `json/events.json` | Event rules (trigger: componentId + type; actions: NavigateScreen, WriteTag, RunScript, etc.) |
| `json/scripts.json` | Script paths |
| `json/alarms.json` | Alarm configuration |
| `json/historian.json` | Historian configuration |
| `screens/<id>.json` | Screen JSON (components with type, location, size, tagName, properties) |
| `images/<name>` | Image files for ImageView (source/imageName resolved here) |

Screen JSON format matches the Designer export: `id`, `name`, `size`, `backgroundColor`, `components[]` with `componentType`, `location`, `size`, `tagName`, `properties`, etc.

## Toolbar and built-in views

- **Home** – Loads the first project screen (or clears if no project).
- **Logs** – Built-in log view (Console module).
- **Alarms** – Built-in alarms view (Alarms module).

Status bar shows connection status from the Communication module. Transfer overlay appears during project transfer (Discovery) and hides a few seconds after completion or error.

## Configuration and transfer

- **Autoload:** If `data/metadata.iscr` exists at startup, the project is opened and the first screen is loaded.
- **Transfer:** The Discovery module listens for project transfer (TCP 8888) and UDP discovery (8889). After a successful transfer, the project is opened from the received `data/` folder.

## Differences from Qt runtime

- **UI:** Qt/QML replaced by C# Avalonia; same project folder layout and JSON screen format where applicable.
- **Screens:** Screen source is `screens/<id>.json` (not `qml/*.qml`). EventManager `NavigateToScreen` uses `projectPath/screens/<screenId>.json`.
- **Images:** Resolved from `projectPath/images/<name>` via `ScreenManager.ResolveImagePath` (no QML resource paths).
- **Components:** All 29 IndusysComponents have Avalonia UserControls under `Runtime/Views/Controls/`. Some (Tab, TableView, SVGView, Popup, AlarmView, TrendView) are stubs with placeholder content; others are fully wired (tag binding, events).
- **Scripting:** ScriptingEngine is a stub (no Lua execution yet). Scheduler and event-triggered scripts do not run until an engine is integrated.
- **Communication:** Modbus and OPC UA are stubs; connection status list is populated from config but no real I/O.
- **Historian:** In-memory stub; TrendView shows a placeholder (no chart). HistorianQueryHelper API is available for future integration.

See **MIGRATION_PLAN.md** and **MIGRATION_CHECKLIST.md** for architecture and migration status.
