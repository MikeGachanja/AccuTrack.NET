using System.Collections.Concurrent;
using Runtime.Modules.Communication;
using Runtime.Modules.Console;
using Runtime.Modules.Discovery;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Project;
using Runtime.Modules.Screens;

namespace Runtime;

/// <summary>Static service locator for ExecutionEngine and key runtime modules. Set in Program.Main; resolve from UI or other code.</summary>
public static class Services
{
    private static readonly ConcurrentDictionary<Type, object> _registry = new();

    /// <summary>Execution engine singleton. Use for ModuleManager, ReinitializeWithProject, StartAll, StopAll.</summary>
    public static ExecutionEngine Engine => ExecutionEngine.Instance;

    /// <summary>Register a service by its interface or concrete type. Called from Program.Main.</summary>
    public static void Register<T>(T instance) where T : class
    {
        if (instance == null) return;
        _registry[typeof(T)] = instance;
    }

    /// <summary>Register a service under an interface type (e.g. Register&lt;ICommunication&gt;(commModule)).</summary>
    public static void Register<TInterface>(object instance) where TInterface : class
    {
        if (instance == null) return;
        _registry[typeof(TInterface)] = instance;
    }

    /// <summary>Resolve a service by type. Returns null if not registered.</summary>
    public static T? Get<T>() where T : class
    {
        return _registry.TryGetValue(typeof(T), out var o) ? o as T : null;
    }

    /// <summary>Resolve a module by name via ExecutionEngine.ModuleManager. Convenience for UI.</summary>
    public static T? GetModule<T>(string moduleName) where T : class
    {
        return Engine.ModuleManager.GetModule(moduleName) as T;
    }
}
