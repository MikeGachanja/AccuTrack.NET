using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runtime.Modules.ExecutionEngine;

/// <summary>
/// Central runtime engine: holds module manager, loads project configs, and drives lifecycle (initialize, start, stop, shutdown).
/// </summary>
public sealed class ExecutionEngine
{
    private readonly ModuleManager _moduleManager = new();
    private string? _projectPath;
    private bool _initialized;
    private static ExecutionEngine? _instance;
    private static readonly object _instanceLock = new();

    public static ExecutionEngine Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                    _instance ??= new ExecutionEngine();
            }
            return _instance;
        }
    }

    public ModuleManager ModuleManager => _moduleManager;
    public string? ProjectPath => _projectPath;
    public bool IsInitialized => _initialized;

    /// <summary>Initialize without a project (e.g. at startup before any project is loaded).</summary>
    public void Initialize()
    {
        _initialized = true;
    }

    /// <summary>Re-initialize with project path: load configs and call Initialize(config) on each registered module.</summary>
    public void ReinitializeWithProject(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            return;

        _projectPath = projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var jsonPath = Path.Combine(_projectPath, "json");
        if (!Directory.Exists(jsonPath))
            jsonPath = _projectPath;

        var startOrder = _moduleManager.GetStartOrder();
        foreach (var name in startOrder)
        {
            var module = _moduleManager.GetModule(name);
            if (module == null) continue;

            var config = LoadModuleConfig(name, jsonPath);
            try
            {
                module.Initialize(config);
            }
            catch (Exception ex)
            {
                // Log and continue; ErrorOccurred can be raised by module
                System.Diagnostics.Debug.WriteLine($"[ExecutionEngine] Initialize {name}: {ex.Message}");
            }
        }
    }

    /// <summary>Start all modules in dependency order.</summary>
    public bool StartAll()
    {
        var startOrder = _moduleManager.GetStartOrder();
        foreach (var name in startOrder)
        {
            var module = _moduleManager.GetModule(name);
            if (module == null) continue;
            try
            {
                if (!module.Start())
                    System.Diagnostics.Debug.WriteLine($"[ExecutionEngine] Start failed: {name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecutionEngine] Start {name}: {ex.Message}");
            }
        }
        return true;
    }

    /// <summary>Stop all modules in reverse dependency order.</summary>
    public void StopAll()
    {
        var stopOrder = _moduleManager.GetStopOrder();
        foreach (var name in stopOrder)
        {
            var module = _moduleManager.GetModule(name);
            if (module == null) continue;
            try
            {
                module.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecutionEngine] Stop {name}: {ex.Message}");
            }
        }
    }

    /// <summary>Shutdown: stop all then call Shutdown on each module.</summary>
    public void Shutdown()
    {
        StopAll();
        var stopOrder = _moduleManager.GetStopOrder();
        foreach (var name in stopOrder)
        {
            var module = _moduleManager.GetModule(name);
            if (module == null) continue;
            try
            {
                module.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecutionEngine] Shutdown {name}: {ex.Message}");
            }
        }
        _projectPath = null;
    }

    private JsonObject? LoadModuleConfig(string moduleName, string jsonDirectory)
    {
        // Map module names to config file names (convention from Qt runtime)
        var fileName = moduleName switch
        {
            "CommunicationModule" => "communications.json",
            "TagsEngine" or "TagsModule" => "tags.json", // Changed from tag_tables.json to tags.json
            "AlarmsModule" or "Alarms" => "alarms.json",
            "SchedulesModule" or "Scheduler" => "schedules.json",
            "HistorianModule" or "Historian" => "historian.json",
            "MLEngine" or "MachineLearningModule" => "machine_learning.json",
            "SecurityModule" or "Security" => "security.json",
            _ => null
        };

        if (string.IsNullOrEmpty(fileName))
            return null;

        var path = Path.Combine(jsonDirectory, fileName);
        if (!File.Exists(path))
        {
            // For TagsModule, also try tag_tables.json as fallback for backward compatibility
            if ((moduleName == "TagsEngine" || moduleName == "TagsModule") && File.Exists(Path.Combine(jsonDirectory, "tag_tables.json")))
            {
                path = Path.Combine(jsonDirectory, "tag_tables.json");
            }
            else
            {
                return null;
            }
        }

        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
                return JsonObject.Create(root) ?? new JsonObject();
        }
        catch
        {
            // Ignore parse errors
        }

        return null;
    }
}
