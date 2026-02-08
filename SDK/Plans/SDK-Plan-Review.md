# SDK Plan 3 Review – What Needs Updating

This document compares the current AccuTrack SDK implementation against the updated **Plan 3 (SDK)**, **Plan 1 (Designer)**, and **Plan 2 (Runtime)** to identify required or recommended changes.

---

## 1. Plan 3 (SDK) Alignment

### 1.1 Already Aligned

| Plan 3 requirement | Current SDK | Status |
|--------------------|-------------|--------|
| **ICommunicationModule** + **CommunicationModuleBase** | `Communication/ICommunicationModule.cs`, `CommunicationModuleBase.cs` | OK |
| Config via **System.Text.Json** | All modules use `JsonElement` for Configure/GetConfiguration | OK |
| Events **StatusChanged**, **ErrorOccurred** | Both on interface and base | OK |
| **ModbusModule** | Configure from JSON (TCP/RTU), Start/Stop, ModbusConnection, ModbusAddressParser | OK |
| **OPCModule** | Configure from JSON, OPC UA client, ReadNode/WriteNode, address format | OK |
| **S7Module** | Configure from JSON (host, rack, slot, port), S7 client, Read/Write, S7AddressParser | OK |
| **IHistorian** / **Historian** / **HistorianConfig** | Historian folder with interface, placeholder, DTO | OK |
| **IScriptingEngine** / **IScriptApi** / **Scripting** | Scripting folder with interfaces and placeholder | OK |
| Assembly **AccuTrack.SDK**, target **net8.0** | SDK.csproj: net8.0, RootNamespace AccuTrack.SDK | OK |
| **ICommunicationDriver** + **ConnectionStatus** | Present for Runtime TagIOHandler (tag-level ReadAsync/WriteAsync) | OK |

### 1.2 Updates Needed

#### A. GetConfiguration() must include `"type"` (Plan 3 §9)

- **Plan 3 §9**: Config shape must include *name, **type** "Modbus"/"OPC"/"S7"*, etc. Designer writes and Runtime reads this; SDK must read/write the same structure.
- **Current**: We store whatever is passed to `Configure()`. If the caller omits `"type"`, `GetConfiguration()` does not add it, so round-trip save/load can lose the module type.
- **Change**: Ensure `GetConfiguration()` always returns JSON that includes `"type": ModuleType` so Designer/Runtime can persist and reload without adding it themselves. **Implemented in CommunicationModuleBase.**

#### B. GetConfiguration() return type (Plan 3 §2.2)

- **Plan 3 §2.2**: Specifies `JsonObject GetConfiguration()`.
- **Current**: Interface returns `JsonElement` (read-only snapshot).
- **Assessment**: `JsonElement` is sufficient for “same JSON shape” and is easy to serialize/deserialize. Designer/Runtime can add `"type"` when building the config object if they need to mutate. No code change required; optionally add a note in Plan 3 or SDK docs that the contract is “same structure as JsonObject” and that `GetConfiguration()` now ensures `"type"` is present.

#### C. Address format documentation (Plan 3 §9)

- **Plan 3 §9**: “Document in SDK and align with ModbusAddressParser and OPC/S7 address handling.”
- **Current**: XML comments on parsers and modules; no single place that lists all address formats for Designer and Runtime.
- **Recommendation**: Add a short **Address formats** section (e.g. in this file or in `SDK/README.md`) that documents:
  - **Modbus**: e.g. `40001`, `MW100`, `I0.0`, `Q1.5`, `holding:100`, etc. (per ModbusAddressParser).
  - **OPC**: e.g. `ns=2;s=MyVariable`, `2:MyVar` (per OPCAddressFormat / OPCModule).
  - **S7**: e.g. `DB1.DBD0`, `DB2.DBW10`, `DB1,0,Real` (per S7AddressParser).

---

## 2. Plan 1 (Designer) and Plan 2 (Runtime) Alignment

### 2.1 JSON and contract

- **Plan 1 §17 / Plan 2 §11**: `communication.json` and other configs; same schema and file names. SDK must not change config shape without aligning with both plans.
- **Current**: Module configs use `name`, `host`, `port`, `slaveId`, `protocol`, etc. for Modbus; `endpoint`, `securityMode`, `securityPolicy`, `type` (UA/DA) for OPC; `host`, `rack`, `slot`, `port` for S7. With **GetConfiguration() including `"type"` (module type)**, the shape matches Plan 3 §9 and supports Designer output and Runtime load.

### 2.2 ICommunicationDriver vs ICommunicationModule

- **Plan 2 §4.1**: TagIOHandler does read/write via Communication module. Plan 2 §4.2: Communication module holds `ICommunicationModule` instances and exposes connection status.
- **Current**: SDK has both:
  - **ICommunicationModule**: lifecycle (Configure, Start, Stop) and status; protocol-specific read/write (ModbusConnection, OPC ReadNode/WriteNode, S7 Read/Write).
  - **ICommunicationDriver**: tag-level `ReadAsync(address)`, `WriteAsync(address, value)` for Runtime TagIOHandler.
- **Assessment**: Keeping **ICommunicationDriver** is useful so Runtime can wrap each `ICommunicationModule` in a driver that implements `ReadAsync`/`WriteAsync` using the module’s connection and address parsers. No SDK change required; Runtime implements the wrapper.

---

## 3. Acceptance Criteria (Plan 3 §11) – Checklist

- [x] **ICommunicationModule** and **CommunicationModuleBase** implemented; config via System.Text.Json; events StatusChanged, ErrorOccurred.
- [x] **ModbusModule**: Configure from JSON (TCP/RTU); Start/Stop; ModbusConnection with Read/Write; ModbusAddressParser for tag address strings. Compatible with Runtime TagIOHandler (Plan 2).
- [x] **OPCModule**: Configure from JSON; OPC UA client (OPC Foundation .NET); read/write by NodeId; tag address format documented in code (and optionally in SDK-Plan-Review or README).
- [x] **S7Module**: Configure from JSON (host, rack, slot, port); S7 client; read/write DBs; address format documented in code (and optionally in SDK-Plan-Review or README).
- [x] JSON config shape for communication modules matches what Designer (Plan 1) generates and Runtime (Plan 2) loads; **with GetConfiguration() including `"type"`**, round-trip is robust.
- [x] Assembly targets net8.0; referenced by Designer (Plan 1) and Runtime (Plan 2) without conflict.

---

## 4. Optional / Future (renumbered in doc)

- **netstandard2.0**: Plan 3 allows netstandard2.0 for broader compatibility. Current target is net8.0. If Designer or Runtime need netstandard2.0, consider multi-targeting (e.g. net8.0 + netstandard2.0) or a separate abstractions assembly.
- **Formal JSON schema**: Plans refer to “same schema”; no formal JSON Schema file exists in the repo. Consider adding `communication.json` schema (and others) under a `schemas/` or similar for Designer/Runtime validation.
- **OPC auto-discover**: Plan 3 §4.2 mentions optional auto-discover (discover servers, discover endpoints). OPCModule does not implement this yet; add when Designer or Runtime need it.

---

## 5. Address formats (Plan 3 §9 – Designer tag editor & Runtime TagIOHandler)

- **Modbus** (ModbusAddressParser): `40001`, `30001`, `10001`, `00001` (1-based); `MW100`, `MW100:8`, `MD100`; `I0.0`, `Q1.5`; `DB100.DBD10`; quantity with `:n` or `[n]`.
- **OPC** (OPCAddressFormat / OPCModule): Full NodeId `ns=2;s=MyVariable`, `ns=2;i=1234`; short form `2:MyVar`.
- **S7** (S7AddressParser): `DB1.DBD0`, `DB2.DBW10`, `DB1.DBX0.0`; or `DB1,0,Real`, `DB2,10,Int`.

---

## 6. Summary of Code Changes Made

1. **CommunicationModuleBase.GetConfiguration()**  
   Builds and returns a JSON object that always includes `"type": ModuleType` and copies all other properties from the stored config, so Designer and Runtime get a round-trip safe shape for `communication.json`.

2. **SDK-Plan-Review.md** (this file)  
   Documents alignment with Plans 1–3, required updates, and optional improvements.

3. **Address formats** (see §6 below); optional: add the same to `SDK/README.md` or a dedicated `AddressFormats.md`.
