# JSON Schema Alignment: DarkStar vs Legacy AccuTrackQt

This document defines the JSON formats the DarkStar compiler must produce so the Runtime and legacy compatibility work correctly.

## 1. Build output layout

All paths relative to `build/<projectName>/` (or Runtime `data/` after deploy):

- `metadata.iscr` – project manifest (JSON)
- `json/communications.json`
- `json/screens.json`
- `json/tag_tables.json` – used by ExecutionEngine for TagsModule
- `json/tags.json` – used by ProjectModule for project metadata (optional; can share structure with tag_tables)
- `json/scripts.json`
- `json/alarms.json`
- `json/schedules.json`
- `json/historian.json`
- `json/security.json`
- `json/events.json`
- `json/animations.json` (optional)
- `json/machine_learning.json` (optional)
- `screens/<id>.json` – one file per screen (id = screen GUID or stable id)

## 2. metadata.iscr

Root object:

- `name` (string)
- `type` (int, 0=HMI, 1=PC_STATION, 2=BMS)
- `version` (string, e.g. "1.0.0")
- `resolution` (object): `width`, `height`
- `configFiles` (object): paths relative to project root, e.g. `"communication": "json/communications.json"`, `"screens": "json/screens.json"`, `"scripts": "json/scripts.json"`, `"tags": "json/tag_tables.json"`, `"alarms": "json/alarms.json"`, `"schedules": "json/schedules.json"`, `"historian": "json/historian.json"`, `"security": "json/security.json"`, `"events": "json/events.json"`

Runtime ProjectModule reads name, type, version, resolution, configFiles from this file.

## 3. json/communications.json

Root: `communication_modules` (array). Each element (legacy-compatible):

- `type` (string): "Modbus", "ModbusTCP", "ModbusRTU", "OPC", "OPCUA", etc.
- `id` (string, GUID)
- `config` (object): name, and type-specific settings (host, port, slaveId for Modbus; endpoint, securityMode, securityPolicy, username, password, sessionTimeout for OPC UA; etc.)
- `tagMappings` (array): `{ "tagName", "address", "deviceAddress", "ioType", "dataType", "scanRate", "enabled" }`

Designer CommunicationModules currently serialize with root `modules`; compiler will output root `communication_modules` for Runtime. For OPC UA client/server, extend config with `role` ("client" | "server") and server-specific fields when implemented.

Runtime ProjectModule and CommunicationModule expect either `communication_modules` or `modules` array.

## 4. json/screens.json

Root: `screens` (array). Each element:

- `id` (string) – used as filename for per-screen JSON, e.g. `screens/<id>.json`
- `name` (string)

Runtime ProjectModule resolves each screen to `projectDir/screens/<id>.json`.

## 5. Per-screen JSON (screens/<id>.json)

Root object (matches Designer ScreenTemplate.ToJson() and Runtime ScreenRenderer.ParseScreen):

- `id` (string)
- `name` (string)
- `size` (object): `width`, `height`
- `backgroundColor` (string, e.g. "#FFFFFF")
- `components` (array). Each component (matches BaseComponent.ToJson() and ScreenRenderer.ParseComponent):
  - `id` (string)
  - `componentType` (string): "Button", "TextLabel", "GaugeView", "Numeric", etc.
  - `name` (string)
  - `location` (object): `x`, `y`
  - `size` (object): `width`, `height`
  - `visible` (bool), `enabled` (bool), `zOrder` (int), `tagName` (string)
  - `properties` (object) – component-specific key/value

Designer already uses this shape; compiler copies or generates from screen templates.

## 6. json/tag_tables.json (TagsModule config)

Root: `tags` (array). Each element:

- `name` (string)
- `address` (string)
- `description` (string, optional)
- Legacy extended fields: type, ioType, dataType, minValue, maxValue, scale, decimals, format, unit, accessLevel, visible, enabled, sourceTable, deviceId, deviceName, isRemote, deviceAddress

Runtime TagsModule.Initialize(config) expects config["tags"] as array with at least name, address, description.

ExecutionEngine maps "TagsModule" / "TagsEngine" to filename "tag_tables.json".

## 7. json/scripts.json

Root: `scripts` (array). Each element:

- `id` (string, GUID)
- `name` (string)
- `description` (string), `enabled` (bool)
- `code` (string) – script body
- `path` (string, optional)

## 8. json/alarms.json

Root: `alarms` (array). Each element: name, description, type, severity, source, tag, timestamp, acknowledged, cleared, notes, isAcknowledged, isCleared, isActive, isSuppressed, isFlagged, isSilenced.

## 9. json/schedules.json

Root: `schedules` (array). Each element: id, name, description, type, startTime, endTime, enabled, scanCycleMs, maxExecutionTimeMs, retryCount, retryDelayMs, executeOnStartup, executeOnShutdown, priority, scriptInfo { scriptId, scriptName, description }.

## 10. json/historian.json, security.json, events.json

- historian.json: name, type, and historian-specific config.
- security.json: users, groups, roles arrays (legacy structure).
- events.json: events array (copy from source or generate empty).

## 11. Designer model vs output

- **CommunicationModules**: Designer uses `ToJson()` on each CommunicationModule with id, name, type, settings, enabled, description. Compiler must merge into one root object with `communication_modules` array; each item must include `config` (with name + settings) and `tagMappings` when we support them.
- **ScreenTemplate**: ToJson() already produces id, name, size, backgroundColor, components; component shape matches Runtime. Compiler writes screens.json (id, name per screen) and copies or serializes each screen to `screens/<id>.json`.
- **Tag tables**: Designer has TagTable with Tags; compiler flattens all tags into one `tags` array for tag_tables.json (and optionally tags.json for project metadata).

All config files use UTF-8 JSON with indentation for readability.
