using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.EventDispatch;
using AccuTrack.Runtime.Modules;

namespace AccuTrack.Runtime.ExecutionEngine;

/// <summary>
/// Singleton; initialize with optional project path; register modules; load configs from project/json; StartAll/StopAll.
/// </summary>
public sealed class ExecutionEngine
{
    private static ExecutionEngine? _instance;
    private static readonly object StaticLock = new();

    public static ExecutionEngine Instance
    {
        get
        {
            if (_instance == null)
                lock (StaticLock)
                    _instance ??= new ExecutionEngine();
            return _instance;
        }
    }

    public static void SetInstance(ExecutionEngine? instance)
    {
        lock (StaticLock)
            _instance = instance;
    }

    private readonly ModuleManager _moduleManager = new();
    private string? _projectPath;
    private readonly ConcurrentDictionary<string, JsonElement?> _configCache = new();
    private bool _initialized;

    public ModuleManager ModuleManager => _moduleManager;
    public string? ProjectPath => _projectPath;
    public bool IsInitialized => _initialized;

    public void Initialize(string? projectPath = null)
    {
        EventDispatcher.Instance.Initialize();
        _projectPath = projectPath;
        if (!string.IsNullOrEmpty(projectPath))
            LoadAllConfigurations(projectPath);
        _initialized = true;
    }

    public void Shutdown()
    {
        StopAll();
        foreach (var name in _moduleManager.GetStopOrder())
        {
            var mod = _moduleManager.GetModule(name);
            mod?.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        EventDispatcher.Instance.Shutdown();
        _initialized = false;
        _projectPath = null;
        _configCache.Clear();
    }

    public void RegisterModule(IModuleInterface module) => _moduleManager.RegisterModule(module);

    public void UnregisterModule(string moduleName) => _moduleManager.UnregisterModule(moduleName);

    public IModuleInterface? GetModule(string moduleName) => _moduleManager.GetModule(moduleName);

    public void LoadModuleConfiguration(string moduleName, string projectPath)
    {
        var configPath = Path.Combine(projectPath, "json", $"{moduleName.ToLowerInvariant()}.json");
        if (!File.Exists(configPath))
        {
            _configCache[moduleName] = null;
            return;
        }
        var json = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(json);
        _configCache[moduleName] = doc.RootElement.Clone();
    }

    public void LoadAllConfigurations(string projectPath)
    {
        var jsonDir = Path.Combine(projectPath, "json");
        if (!Directory.Exists(jsonDir))
            return;
        foreach (var file in Directory.EnumerateFiles(jsonDir, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var key = MapConfigFileToModuleName(name);
            var json = File.ReadAllText(file);
            try
            {
                using var doc = JsonDocument.Parse(json);
                _configCache[key] = doc.RootElement.Clone();
            }
            catch
            {
                _configCache[key] = null;
            }
        }
    }

    private static string MapConfigFileToModuleName(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            "tags" => "Tags",
            "communication" => "Communication",
            "alarms" => "Alarms",
            "historian" => "Historian",
            "schedules" => "Schedules",
            "security" => "Security",
            "machinelearning" => "MachineLearning",
            "events" => "Events",
            "animations" => "Animations",
            _ => char.ToUpperInvariant(fileName[0]) + fileName[1..].ToLowerInvariant()
        };
    }

    /// <summary>
    /// Get loaded config by key (e.g. "Events", "Animations") for non-module consumers (EventManager, AnimationManager).
    /// </summary>
    public System.Text.Json.JsonElement? GetConfig(string key)
    {
        return _configCache.TryGetValue(key, out var c) ? c : null;
    }

    public void ReinitializeWithProject(string projectPath)
    {
        StopAll();
        _projectPath = projectPath;
        LoadAllConfigurations(projectPath);
        foreach (var name in _moduleManager.GetStartOrder())
        {
            var mod = _moduleManager.GetModule(name);
            if (mod == null) continue;
            _configCache.TryGetValue(name, out var config);
            mod.InitializeAsync(config ?? default).GetAwaiter().GetResult();
        }
        StartAll();
    }

    public void StartAll()
    {
        foreach (var name in _moduleManager.GetStartOrder())
            StartModule(name);
    }

    public void StopAll()
    {
        foreach (var name in _moduleManager.GetStopOrder())
            StopModule(name);
    }

    public void StartModule(string moduleName)
    {
        var mod = _moduleManager.GetModule(moduleName);
        if (mod != null && !mod.IsRunning)
            mod.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public void StopModule(string moduleName)
    {
        var mod = _moduleManager.GetModule(moduleName);
        if (mod != null && mod.IsRunning)
            mod.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
