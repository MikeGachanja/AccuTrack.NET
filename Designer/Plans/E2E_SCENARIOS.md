# End-to-End Scenarios (DarkStar Migration)

Use these scenarios to validate the full build/deploy/run flow after the migration.

## Scenario A: Build and deploy to local Runtime

1. **Designer:** Create or open a project with at least one SCADA (e.g. HMI).
2. **Designer:** Add one screen (e.g. Main), one tag table with a few tags, and optionally one OPC UA communication module (endpoint e.g. `opc.tcp://localhost:4840`).
3. **Designer:** Build: Menu Build → Build Project, select the SCADA project, confirm compilation succeeds and Output shows "Compilation completed successfully."
4. **Designer:** Verify `<project root>/build/` contains `metadata.iscr`, `json/*.json`, and `screens/<id>.json`.
5. **Runtime:** Start the Runtime (e.g. from Visual Studio or `dotnet run` for the Runtime project). Ensure it is listening on port 8888 for project transfer.
6. **Designer:** Deploy: Menu Build → Deploy, select the same SCADA project, leave Target IP as 127.0.0.1 and Port 8888, check "Build project before deploying", click Deploy.
7. **Runtime:** After transfer completes, wait a few seconds; the Runtime should reload the project (stop modules, load new project, start modules) and show the first screen.
8. **Verify:** The screen renders with Avalonia controls; if OPC UA client is configured and a server is running at the endpoint, tag values can update from the server.

## Scenario B: Project replace (unload previous, load new)

1. Deploy project "A" from Designer to Runtime as in Scenario A; confirm the Runtime shows project A’s screen.
2. In Designer, open or create a different project "B", build it.
3. Deploy project "B" to the same Runtime (same IP/port).
4. **Verify:** Runtime clears previous project data, receives project B, and after reload shows project B’s screen (not A). No leftover files or state from A.

## Scenario C: OPC UA client tag updates

1. Start an OPC UA server (e.g. Reference Server from UA-.NETStandard or another simulator) on `opc.tcp://localhost:4840`.
2. In Designer, add an OPC UA communication module with endpoint `opc.tcp://localhost:4840` and tag mappings (tag name → OPC UA node id, e.g. `ns=2;s=MyVariable`).
3. Build and deploy to Runtime.
4. **Verify:** Runtime Communication module connects; connection status shows "Connected"; tag values in the Runtime UI update from the OPC UA server (if the server exposes those nodes).

## Automated tests

- **Designer.Tests:** Compiler and ProjectPackager tests. Run with `dotnet test Tests/Designer.Tests/Designer.Tests.csproj`.
- Compiler tests assert that `CompileProject` produces `metadata.iscr`, `json/` files, and `screens/*.json` for a minimal project.
- ProjectPackager tests assert that `PackageProject` collects files and reads project name from `metadata.iscr`.
