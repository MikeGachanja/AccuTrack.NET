using System.Collections.Generic;
using System.Linq;

namespace Runtime.Modules.ExecutionEngine;

/// <summary>
/// Manages module registration and dependency-ordered start/stop.
/// </summary>
public sealed class ModuleManager
{
    private readonly Dictionary<string, IModuleInterface> _modules = new();
    private readonly object _lock = new();

    public bool RegisterModule(IModuleInterface module)
    {
        if (module == null)
            return false;

        lock (_lock)
        {
            var name = module.ModuleName;
            if (string.IsNullOrEmpty(name) || _modules.ContainsKey(name))
                return false;

            var missing = GetMissingDependencies(name, module.Dependencies);
            if (missing.Count > 0)
                return false;

            _modules[name] = module;
            return true;
        }
    }

    public bool UnregisterModule(string moduleName)
    {
        lock (_lock)
            return _modules.Remove(moduleName ?? "");
    }

    public IModuleInterface? GetModule(string moduleName)
    {
        lock (_lock)
            return _modules.TryGetValue(moduleName ?? "", out var m) ? m : null;
    }

    public T? GetModule<T>(string moduleName) where T : class, IModuleInterface
    {
        return GetModule(moduleName) as T;
    }

    public IReadOnlyList<string> GetModuleNames()
    {
        lock (_lock)
            return _modules.Keys.ToList();
    }

    /// <summary>Module names in order they should be started (dependencies first).</summary>
    public IReadOnlyList<string> GetStartOrder()
    {
        lock (_lock)
            return TopologicalSort(_modules, reverse: false);
    }

    /// <summary>Module names in order they should be stopped (dependents first).</summary>
    public IReadOnlyList<string> GetStopOrder()
    {
        lock (_lock)
            return TopologicalSort(_modules, reverse: true);
    }

    public bool CheckDependencies(string moduleName, IReadOnlyList<string> dependencies)
    {
        if (dependencies == null || dependencies.Count == 0)
            return true;
        lock (_lock)
            return GetMissingDependenciesLocked(moduleName, dependencies).Count == 0;
    }

    public IReadOnlyList<string> GetMissingDependencies(string moduleName, IReadOnlyList<string> dependencies)
    {
        if (dependencies == null || dependencies.Count == 0)
            return Array.Empty<string>();
        lock (_lock)
            return GetMissingDependenciesLocked(moduleName, dependencies);
    }

    private List<string> GetMissingDependenciesLocked(string moduleName, IReadOnlyList<string> dependencies)
    {
        var missing = new List<string>();
        foreach (var dep in dependencies)
        {
            if (string.IsNullOrEmpty(dep)) continue;
            if (dep == moduleName) continue; // self
            if (!_modules.ContainsKey(dep))
                missing.Add(dep);
        }
        return missing;
    }

    private static List<string> TopologicalSort(Dictionary<string, IModuleInterface> modules, bool reverse)
    {
        var order = new List<string>();
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();

        void Visit(string name)
        {
            if (visited.Contains(name)) return;
            if (stack.Contains(name))
                return; // cycle; skip
            if (!modules.TryGetValue(name, out var mod))
                return;

            stack.Add(name);
            var deps = mod.Dependencies ?? Array.Empty<string>();
            foreach (var d in deps)
            {
                if (!string.IsNullOrEmpty(d))
                    Visit(d);
            }
            stack.Remove(name);
            visited.Add(name);
            order.Add(name);
        }

        foreach (var name in modules.Keys.ToList())
            Visit(name);

        if (reverse)
            order.Reverse();
        return order;
    }
}
