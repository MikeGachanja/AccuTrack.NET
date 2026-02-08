# Designer Gap Analysis vs Plan 1 (Updated)

This document compares the current Designer implementation to the updated **Plan 1: AccuTrack Designer – Qt to C# WinForms Migration** and lists what needs to be updated or added.

---

## 1. Solution & build order (§1.2, §16.1)

| Requirement | Current | Action |
|-------------|---------|--------|
| **Canonical order in Designer.slnx** | Order matches plan except **security** | ✅ Order is correct (SDK, alarms, communication, compiler, components, console, Designer, discovery, events, historian, machinelearning, project, scheduler, screeneditor, scripter, simulator, tagengine, tools, workspace) |
| **Security** | No `security` project | **Add** either a `security` project (after scheduler) **or** document that security/roles editor is implemented under `tools`; add to solution if new project |
| **Designer references SDK** | SDK reference was removed (SDK had build errors) | **Re-add** `ProjectReference` to `../SDK/SDK/SDK.csproj` once SDK builds; plan §2.3 and §16.2 require Designer to reference SDK for communication/scripting types |

---

## 2. Main shell (§2)

| Requirement | Current | Action |
|-------------|---------|--------|
| MainForm with menu, toolbar, project tree, tab control, property panel, console | ✅ Done | — |
| Recent projects | In-memory list only | **Persist** recent projects in user settings (§2.2); wire "Recent" menu items from `ProjectManager.RecentProjects` |
| Tab handlers for all node types | Screen + Tag table open real editors; others show placeholder | **Wire** dedicated editors for: script, communication module, alarms, schedules, historian, **security**, ML, device network, settings (or document placeholders until those modules implement controls) |
| Deploy / Upload / Simulation menu items | Present but no handlers | **Implement** or stub: Deploy → deploy dialog; Upload → upload dialog; Simulation Start/Pause/Stop → simulator |

---

## 3. Project module (§3)

| Requirement | Current | Action |
|-------------|---------|--------|
| ProjectManager open/save/close, metadata.iscr | Save/Open are stubs (no file I/O) | **Implement** read/write of `metadata.iscr` and project JSON layout expected by Runtime (Plan 2 §11) |
| ProjectView tree, double-click → editor tab | ✅ Done | — |
| Project model (screens, tag tables, scripts, comm, alarms, …) | ✅ Done | — |
| **Dialogs** | New/Open use `FolderBrowserDialog` / `OpenFileDialog` | **Add**: New project wizard/dialog; Project settings dialog; Validation dialog; Device network/IP dialog where applicable (§3.2) |

---

## 4. Screens module (§4)

| Requirement | Current | Action |
|-------------|---------|--------|
| Canvas with double-buffering | Basic `ScreenEditorControl` | **Enhance**: drag/drop, move components, zoom, coordinate system similar to Qt graphics scene |
| ScreenTemplate model | ✅ Done | — |
| **Component palette** | Missing | **Add** ComponentsView: list/toolbar of types (Button, Tank, Gauge, TrendView, AlarmView, etc.) aligned with Runtime/Plan 2; drag onto canvas to create instance |
| Property editor when component selected | PropertyGrid shows project node | **Wire** screen editor selection so PropertyEditor shows selected **canvas component** (and updates model) |
| Serialization | Not implemented | **Save/load** screen to/from JSON in format Runtime can load (§4.2, §17) |

---

## 5. Tags module (§5)

| Requirement | Current | Action |
|-------------|---------|--------|
| Tag table model and editor control | ✅ Done | — |
| Address format compatible with SDK | Basic fields (Name, Address, DataType, Scale, Offset) | **Align** with Plan 3 (SDK) address formats (e.g. Modbus, OPC NodeId) |
| Save to project JSON (tags.json) | Stub in compiler only | **Full** tags serialization in compiler and load/save from project |

---

## 6. Script module (§6)

| Requirement | Current | Action |
|-------------|---------|--------|
| Script list per project | ScriptNode in tree only | **Add** script list model; open "Script" node → tab with script editor |
| Script editor | Not implemented | **Add** text editor (e.g. Scintilla or TextBox) with Lua syntax highlighting; save under project `scripts/` or equivalent (§6.2) |

---

## 7. Communication module (§7)

| Requirement | Current | Action |
|-------------|---------|--------|
| Communication modules list UI | Not implemented | **Add** list/model and tab editor for connections (name, type: Modbus/OPC/S7) |
| Modbus settings dialog | Not implemented | **Add** dialog (host, port, slave ID, TCP/RTU, timeouts) mirroring Qt `modbussettingsdialog` |
| OPC settings dialog | Not implemented | **Add** dialog (endpoint, security) mirroring Qt `opcuaconnectiondialog` |
| S7 dialog (if used) | Not implemented | **Add** if required |
| Persistence (communication.json) | Stub in compiler | **Full** generation in compiler; structure matching Runtime/SDK |

---

## 8. Alarms, Schedules, Historian, Security, ML (§8)

| Requirement | Current | Action |
|-------------|---------|--------|
| Alarms editor | Placeholder tab | **Add** editor control (tag, condition, priority, message); save to `alarms.json` |
| Schedules editor | Placeholder tab | **Add** editor (cron-like or time-based); save to `schedules.json` |
| Historian editor | Placeholder tab | **Add** editor (tags to log, interval, retention); save to `historian.json` |
| **Security** | No project; placeholder in tree | **Add** `security` project **or** implement roles/permissions editor under `tools`; save to `security.json` |
| Machine learning editor | Placeholder tab | **Add** editor (ML model/config, tag selection); save to `machinelearning.json` |

---

## 9. Components module (§9)

| Requirement | Current | Action |
|-------------|---------|--------|
| PropertyEditor (MainForm) | ✅ PropertyGrid bound to selected node | Wire to **screen component** selection when a component on canvas is selected |
| **ComponentsView (palette)** | Not implemented | **Add** palette of component types with icons; drag onto screen canvas to create instance |
| Animations | Not implemented | **Add** model/UI for animation definitions if edited in designer; align with Runtime AnimationManager (Plan 2) |

---

## 10. Events module (§10)

| Requirement | Current | Action |
|-------------|---------|--------|
| Event list (per screen or global) | Not implemented | **Add** event list/editor (event type, target, action); save to `events.json` |
| Tab for "Events" | Placeholder | Wire to events editor when Events node opened |

---

## 11. Compiler (§11.2, §17)

| Requirement | Current | Action |
|-------------|---------|--------|
| Build orchestration | ✅ BuildService collects project and runs generation | Extend to **validate** and include all asset types |
| Avalonia (AXAML) generation | ✅ Stub per screen | **Expand** to map all Qt component types to Avalonia controls (Button, CheckBox, Slider, Tank, Gauge, TrendView, AlarmView, etc.) per Plan 2 |
| **JSON generation (full set)** | Only `tags.json` and `communication.json` stubs | **Port full set** from Qt `generatejson`: `generateCommunicationConfig`, `generateHistorianConfig`, `generateAlarmsConfig`, `generateSchedulesConfig`, `generateScreensConfig`, `generateScriptsConfig`, `generateTagsConfig`, `generateSecurityConfig`, `generateEventsConfig`, `generateAnimationsConfig`, `generateMachineLearningConfig`, `generateProjectMetadata`. Output files and schema must match Runtime (Plan 2 §11). |
| Output layout | Writes to project `bin/` | Confirm layout matches Runtime expectations (Plan 2 §11) |

---

## 12. Device Discovery / Deployment (§12)

| Requirement | Current | Action |
|-------------|---------|--------|
| Deploy dialog | Menu item only | **Add** dialog: select device (IP/host), push built project (JSON + AXAML + assets) via protocol matching Runtime `ProjectTransferServer` |
| Upload dialog | Menu item only | **Add** dialog: pull project from device to designer |
| Device manager | Not implemented | **Add** list of known devices; discovery if applicable (match Runtime discovery responder) |

---

## 13. Simulation (§13)

| Requirement | Current | Action |
|-------------|---------|--------|
| Simulation Start/Pause/Stop | Menu items only | **Implement**: start Avalonia Runtime process with built project path; Pause/Stop control |

---

## 14. Tools (§14)

| Requirement | Current | Action |
|-------------|---------|--------|
| Options | Menu item only | **Add** application settings (paths, themes, editor defaults); persist in user config |
| Customize | Menu item only | Toolbar/menu customization if required |
| External tools | Menu item only | Optional list of external executables/scripts |
| Package manager | Menu item only | Stub or NuGet/internal package UI |

---

## 15. Workspace & Console (§15)

| Requirement | Current | Action |
|-------------|---------|--------|
| Workspace layout persistence | Not implemented | **Save/restore** form layout (panel positions, tab order) via WinForms or config file |
| Console | TextBox with WriteConsole | **Subscribe** to compiler and script runner output (already partially wired via BuildService) |

---

## 16. Acceptance criteria (§19)

| Criterion | Status |
|-----------|--------|
| MainForm opens with menu, toolbar, project tree, editor tab control, property panel, console | ✅ |
| New/Open/Save/Save As/Close project work; recent projects persist | ⚠️ New/Open/Save/Close work; recent not persisted |
| Double-click in project tree opens correct editor tab for all node types | ⚠️ Screen + Tag table; others placeholder |
| Build produces **all** JSON configs and Avalonia output in layout expected by Runtime | ❌ Only minimal JSON stubs; AXAML stub |
| Deploy pushes built project to device (same protocol as Runtime) | ❌ |
| Simulation starts Runtime with built project path | ❌ |
| All module projects in Designer.slnx in canonical order; security as project or under tools | ⚠️ Order correct; security missing |

---

## Suggested implementation order (from plan §18)

1. **SDK reference** – Re-add when SDK builds.
2. **Security** – Add `security` project (or document under tools).
3. **Recent projects** – Persist in settings; wire Recent menu.
4. **Compiler JSON** – Implement full set of JSON generators to match Plan 2 §11.
5. **Dialogs** – New project wizard, project settings, validation, device network.
6. **Project persistence** – metadata.iscr and project folder layout.
7. **Component palette + screen canvas** – ComponentsView, drag onto canvas, PropertyEditor for selected component.
8. **Communication / Alarms / Schedules / Historian / Security / ML** – One-by-one editors and tab wiring.
9. **Script editor** – Script list + editor, save under project.
10. **Events editor** – Event list, save to `events.json`.
11. **Deploy / Upload / Device manager** – Discovery module.
12. **Simulation** – Start Runtime with project path.
13. **Workspace** – Layout save/restore.
14. **Tools** – Options, Customize, External tools, Package manager.

Use this document to track progress and to assign work (e.g. to parallel agents).
