# Plan 3: AccuTrack SDK – Qt (C++) to C# Class Library Migration

This plan guides the migration of the AccuTrack SDK from Qt (C++) to a C# class library. It references the original Qt implementation at **`d:\Dev\AccuTrackQt\SDK`** and the target C# solution at **`d:\Dev\AccuTrack\SDK`**.

---

## Agent instructions (parallel work)

- **Scope**: SDK only. Do not modify Designer or Runtime source; produce a class library consumed by both.
- **Inputs from other plans**: None (SDK is the shared contract). JSON config shape for communication modules must match what Designer (Plan 1) writes and Runtime (Plan 2) reads; see §9.
- **Outputs for other plans**: **Plan 1 (Designer)** uses SDK for communication types and address formats in communication dialogs and tag editor. **Plan 2 (Runtime)** uses SDK for `ICommunicationModule`, Modbus/OPC/S7 drivers, and TagIOHandler integration. Do not change `ICommunicationModule` or config JSON shape without aligning with Plan 1 and Plan 2.
- **When running as one of three agents**: Assume Plan 1 and Plan 2 are parallel; keep interfaces and config schema stable; document any breaking change in this plan and in Plan 1 §17 / Plan 2 §11.

### Desired conditions (this plan must satisfy)

1. **SDK** is implemented as a **C# class library** (no UI); consumed by Designer (Plan 1) and Runtime (Plan 2).
2. **References** the initial Qt SDK at **`d:\Dev\AccuTrackQt\SDK`** for structure and key files (see §1 and explicit Qt paths in §1.2).
3. **CAL** provides `ICommunicationModule` (and base) so Modbus, OPC, and S7 drivers implement the same interface as Qt.
4. **Config JSON** shape for communication modules is compatible with Designer output and Runtime load (Plan 1 §17, Plan 2 §11).
5. Plan is **extensive** enough to drive implementation and to be used by one of three parallel agents.

---

## 1. Reference: Qt SDK Structure

### 1.1 Root and Build

| Qt Path | Purpose |
|--------|---------|
| `d:\Dev\AccuTrackQt\SDK\CMakeLists.txt` | Builds shared library SDK; subdirs: scipting, CAL, Modbus, OPC, S7, historian. |
| `d:\Dev\AccuTrackQt\SDK\SDK_global.h` | Export macro (e.g. SDK_EXPORT). |
| `d:\Dev\AccuTrackQt\SDK\sdk.h` / `sdk.cpp` | Thin SDK class (placeholder). |

### 1.2 Subdirectories and key files

| Qt Path | Purpose |
|--------|---------|
| **CAL** | `d:\Dev\AccuTrackQt\SDK\CAL\CommunicationModule.h`, `CommunicationModule.cpp` — base interface for all protocol drivers. |
| **Modbus** | `d:\Dev\AccuTrackQt\SDK\Modbus\ModbusModule.h/.cpp`, `ModbusConnection.h/.cpp`, `ModbusAddressParser.h/.cpp` — Modbus TCP/RTU; address parser. |
| **OPC** | `d:\Dev\AccuTrackQt\SDK\OPC\OPCModule.h/.cpp`; `OPC\src\` (open62541-based stack) — OPC UA (and possibly DA). |
| **S7** | `d:\Dev\AccuTrackQt\SDK\S7\S7Module.h/.cpp` — Siemens S7 driver. |
| **historian** | `d:\Dev\AccuTrackQt\SDK\historian\historian.h/.cpp` — thin placeholder. |
| **scipting** | `d:\Dev\AccuTrackQt\SDK\scipting\scipting.h/.cpp` — scripting placeholder (typo: “scipting”). |

---

## 2. CAL (Communication Abstraction Layer)

### 2.1 Qt Reference

- **`CAL\CommunicationModule.h`**: Abstract base class (QObject).
  - `moduleType()` (pure), `configure(config)`, `getConfiguration()`, `validateConfiguration(config)`, `start()`, `stop()`, `isRunning()`.
  - `name()`, `setName()`; signals: `statusChanged`, `errorOccurred`, `dataReceived`.
  - Protected: `m_name`, `m_running`, `m_config` (QJsonObject).
- **`CAL\CommunicationModule.cpp`**: Constructor/destructor and base implementation.

### 2.2 C# SDK Tasks

1. **ICommunicationModule** (interface):  
   - `string ModuleType { get; }`  
   - `bool Configure(JsonElement config)` or `bool Configure(JsonObject config)`  
   - `JsonObject GetConfiguration()`  
   - `bool ValidateConfiguration(JsonElement config)`  
   - `bool Start()`, `void Stop()`, `bool IsRunning`  
   - `string Name { get; set; }`  
   - Events: `StatusChanged`, `ErrorOccurred`, `DataReceived` (optional).

2. **CommunicationModuleBase** (abstract):  
   - Implements `ICommunicationModule`; holds name, running flag, config (e.g. `JsonDocument` or `JsonObject`); default `Stop()` sets running = false.  
   - Use `System.Text.Json` for config (replace QJsonObject).

3. **Assembly**: Place in e.g. `AccuTrack.SDK` or `AccuTrack.SDK.Abstractions` so Designer and Runtime can reference it.

---

## 3. Modbus Module

### 3.1 Qt Reference

- **`Modbus\ModbusModule.h/.cpp`**: Extends CommunicationModule; `moduleType() => "Modbus"`; configure from JSON (host, port, slaveId, protocol TCP/RTU, serial settings, timeout, retries, addressOffset); `connection()` returns ModbusConnection; auto-discovery methods.
- **`Modbus\ModbusConnection.h/.cpp`**:  
  - TCP: host, port, slaveId; RTU: portName, baudRate, dataBits, parity, stopBits, slaveId.  
  - `configureTcp()`, `configureRtu()`; connect/disconnect; read/write coils, discrete inputs, holding registers, input registers.  
  - Uses Qt SerialBus (QModbusTcpClient, QModbusRtuSerialClient) or raw socket/serial.
- **`Modbus\ModbusAddressParser.cpp/.h`**: Parse tag address string (e.g. "40001", "0x0001", "holding:100") to function code and address.

### 3.2 C# SDK Tasks

1. **ModbusModule**: Class extending `CommunicationModuleBase` (or implementing `ICommunicationModule`); `ModuleType => "Modbus"`; configure from JSON (host, port, slaveId, protocol, serial port name, baud rate, etc.); create and hold a `ModbusConnection` (or equivalent client wrapper).

2. **ModbusConnection** (or ModbusClient):  
   - TCP: use TcpClient or a Modbus TCP library (e.g. NModbus4, EasyModbus, or similar).  
   - RTU: use SerialPort + Modbus RTU protocol (same library or custom frame encode/decode).  
   - Methods: Connect, Disconnect, ReadCoils, ReadDiscreteInputs, ReadHoldingRegisters, ReadInputRegisters, WriteSingleCoil, WriteSingleRegister, WriteMultipleRegisters, etc.  
   - Thread-safe if shared; expose connection state (e.g. enum: Disconnected, Connecting, Connected, Error) and events.

3. **ModbusAddressParser**:  
   - Parse address strings (e.g. "40001", "0x0001", "holding:100") to:  
     - Function type (coil, discrete input, holding register, input register)  
     - Offset/address  
   - Return a small DTO or struct used by Runtime TagIOHandler to decide read/write calls.

4. **NuGet**: Add a Modbus TCP/RTU library (e.g. NModbus, EasyModbus) if not implementing protocol from scratch.

---

## 4. OPC Module

### 4.1 Qt Reference

- **`OPC\OPCModule.h/.cpp`**: Extends CommunicationModule; `moduleType() => "OPC"`; configure endpoint, type (UA/DA), security; auto-discover servers/endpoints; start/stop.
- **`OPC\src\`**: Large stack — open62541-based (C) with Qt wrappers (opcua plugins, open62541 backend); declarative_opcua; opcua (core types, client, subscription, etc.).

### 4.2 C# SDK Tasks

1. **OPCModule**: Class implementing `ICommunicationModule`; `ModuleType => "OPC"`; configure from JSON (endpoint URL, security mode/policy, type UA/DA); start/stop; optional auto-discover (discover servers, discover endpoints).

2. **OPC UA client**:  
   - Use **OPC Foundation** stack: NuGet package `OPCFoundation.NetStandard.Opc.Ua` (or official OPC UA .NET stack).  
   - Connect to endpoint; read/write nodes by NodeId; subscribe to data changes; map to “tag” abstraction (node id ↔ tag address).  
   - No need to port open62541 C code; use the .NET OPC UA client API.

3. **Address format**: Define tag address format for OPC (e.g. namespace index + identifier string, or full NodeId string); OPCAddressParser or parse inside OPCModule when reading/writing.

4. **OPC DA** (if used): Legacy OPC DA has different APIs; consider a separate small wrapper or omit if only UA is required.

5. **NuGet**: Add OPC Foundation OPC UA .NET client package(s) as per official documentation.

---

## 5. S7 Module

### 5.1 Qt Reference

- **`S7\S7Module.h/.cpp`**: Extends CommunicationModule; `moduleType() => "S7"`; config: host, rack, slot, port; configure/start/stop; read/write PLC DBs.

### 5.2 C# SDK Tasks

1. **S7Module**: Class implementing `ICommunicationModule`; `ModuleType => "S7"`; configure from JSON (host, rack, slot, port); start/stop; connect to PLC via S7 protocol.

2. **S7 protocol**:  
   - Use a .NET S7 client library (e.g. S7net, or similar open-source S7 client) to read/write DB blocks.  
   - Map tag address to DB number + offset + type (e.g. "DB1.DBD0", "DB2.DBW10").  
   - Define address parser for tag strings (e.g. "DB1,0,Real" or "DB2,10,Int").

3. **NuGet**: Add S7 client library if available; otherwise implement minimal S7 protocol (connect, read, write) from spec.

---

## 6. Historian (SDK Part)

### 6.1 Qt Reference

- **`historian\historian.h/.cpp`**: Thin class; constructor only. Real logic is in Runtime historian_module (data_collector, historian_database, data_retention, historian_manager).

### 6.2 C# SDK Tasks

1. **IHistorian** (optional): If Designer or Runtime need an abstraction for “historian service,” define a minimal interface (e.g. WriteSample(tagId, value, timestamp), Query(…) ).  
2. **Historian** (class): Can remain a simple placeholder or namespace/assembly name for historian-related types used by Runtime (e.g. sample DTOs, retention policy enums).  
3. **Storage**: Actual DB and retention logic stay in Runtime (Plan 2); SDK only needs shared types/contracts if any (e.g. HistorianConfig DTO for JSON).

---

## 7. Scripting (SDK Part)

### 7.1 Qt Reference

- **`scipting\scipting.h/.cpp`**: Thin class (typo “Scipting”); constructor only. Real scripting is in Runtime script_module (Lua).

### 7.2 C# SDK Tasks

1. **IScriptingEngine** (optional): Interface for “run script by name,” “evaluate expression,” “register API (e.g. ReadTag, WriteTag, Log)” so Runtime can plug Lua or another engine.  
2. **Scripting** (class): Placeholder or facade; no need to port Qt scripting C++ code — use NLua, LuaInterface, or IronPython etc. in Runtime.  
3. **Shared API contract**: Define the “script API” (method names and signatures) that Designer and Runtime agree on (e.g. ReadTag(name), WriteTag(name, value), Navigate(screen), Log(message)); SDK can hold this contract in an interface or documentation.

---

## 8. SDK Assembly Layout (C#)

### 8.1 Suggested Projects

- **AccuTrack.SDK** (or **SDK**):  
  - Core: `ICommunicationModule`, `CommunicationModuleBase`.  
  - Modbus: `ModbusModule`, `ModbusConnection`/ModbusClient, `ModbusAddressParser`.  
  - OPC: `OPCModule`, OPC UA client wrapper, OPC address format.  
  - S7: `S7Module`, S7 client wrapper, S7 address parser.  
  - Optional: `IHistorian`, `IScriptingEngine`, shared DTOs (e.g. HistorianConfig, ScriptApi).

### 8.2 Target Framework

- **netstandard2.0** or **net8.0** (or net10.0 to match current SDK.csproj):  
  - netstandard2.0 maximizes compatibility with Designer (WinForms) and Runtime (Avalonia).  
  - If using latest OPC UA or S7 packages that require net8+, use net8.0.

### 8.3 Dependencies

- **System.Text.Json**: For config (replace QJsonObject).  
- **OPC Foundation** OPC UA .NET (NuGet).  
- **Modbus** library (NuGet).  
- **S7** library (NuGet or custom).  
- No UI; SDK is a class library only.

---

## 9. Configuration JSON Compatibility

- **Communication modules**: Same JSON shape as Qt (e.g. name, type "Modbus"/"OPC"/"S7", host, port, slaveId for Modbus; endpoint, security for OPC; host, rack, slot, port for S7). **Plan 1 (Designer)** writes these; **Plan 2 (Runtime)** reads them; SDK must read/write the same structure. Do not change property names or nesting without updating Plan 1 §17 and Plan 2 §11.
- **Address strings**: Tag address format (Modbus, OPC, S7) must match what Runtime TagIOHandler (Plan 2) and Designer tag editor (Plan 1) expect; document in SDK and align with ModbusAddressParser and OPC/S7 address handling.

---

## 10. Implementation Order Suggestion

1. **AccuTrack.SDK** solution/project; target framework; add System.Text.Json.
2. **ICommunicationModule** and **CommunicationModuleBase** (CAL).
3. **ModbusAddressParser**: Parse address strings; no I/O yet.
4. **ModbusConnection/ModbusClient**: TCP (and RTU if required); integrate NuGet Modbus library.
5. **ModbusModule**: Wire config to ModbusConnection; implement Start/Stop/Configure; expose to Runtime.
6. **OPCModule**: Integrate OPC UA .NET client; configure from JSON; read/write by NodeId; define tag address format.
7. **S7Module**: Integrate S7 library; configure from JSON; read/write DBs; define tag address format.
8. **IHistorian / Historian** (optional): Minimal interface or placeholder.
9. **IScriptingEngine / Script API** (optional): Contract only; implementation in Runtime.
10. **Shared DTOs**: Any config or sample types shared between Designer and Runtime (e.g. in SDK or a separate “Contracts” project).

This order gives Designer and Runtime a single SDK with CAL and all three protocol drivers (Modbus, OPC, S7), then optional historian/scripting contracts.

---

## 11. Acceptance criteria (SDK)

- [ ] **ICommunicationModule** and **CommunicationModuleBase** implemented; config via `System.Text.Json`; events for StatusChanged, ErrorOccurred.
- [ ] **ModbusModule**: Configure from JSON (TCP/RTU); Start/Stop; ModbusConnection with Read/Write methods; ModbusAddressParser for tag address strings. Compatible with Runtime TagIOHandler (Plan 2).
- [ ] **OPCModule**: Configure from JSON; OPC UA client (e.g. OPC Foundation .NET); read/write by NodeId; tag address format documented.
- [ ] **S7Module**: Configure from JSON (host, rack, slot, port); S7 client; read/write DBs; address format documented.
- [ ] JSON config shape for communication modules matches what Designer (Plan 1) generates and Runtime (Plan 2) loads; no breaking changes without plan updates.
- [ ] Assembly targets netstandard2.0 or net8.0; referenced by Designer (Plan 1) and Runtime (Plan 2) without conflict.
