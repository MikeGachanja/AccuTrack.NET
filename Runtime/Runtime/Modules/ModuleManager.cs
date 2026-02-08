using System.Collections.Concurrent;

namespace AccuTrack.Runtime.Modules;

/// <summary>
/// Registry of modules; dependency list per module; topological sort for start/stop order.
/// </summary>
public sealed class ModuleManager
{
    private readonly ConcurrentDictionary<string, IModuleInterface> _modules = new();

    public void RegisterModule(IModuleInterface module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        _modules[module.ModuleName] = module;
    }

    public void UnregisterModule(string moduleName)
    {
        _modules.TryRemove(moduleName, out _);
    }

    public IModuleInterface? GetModule(string moduleName) =>
        _modules.TryGetValue(moduleName, out var m) ? m : null;

    public IReadOnlyList<IModuleInterface> GetAllModules() => _modules.Values.ToList();

    /// <summary>
    /// Topological order for start (dependencies first).
    /// </summary>
    public IReadOnlyList<string> GetStartOrder()
    {
        var order = new List<string>();
        var visited = new HashSet<string>();
        var temp = new HashSet<string>();

        void Visit(string name)
        {
            if (temp.Contains(name)) return; // cycle: ignore
            if (visited.Contains(name)) return;
            temp.Add(name);
            var mod = GetModule(name);
            if (mod != null)
            {
                foreach (var dep in mod.Dependencies)
                    Visit(dep);
            }
            temp.Remove(name);
            visited.Add(name);
            order.Add(name);
        }

        foreach (var name in _modules.Keys.ToList())
            Visit(name);
        return order;
    }

    /// <summary>
    /// Reverse of start order for stop (dependents first).
    /// </summary>
    public IReadOnlyList<string> GetStopOrder()
    {
        var start = GetStartOrder();
        var list = start.ToList();
        list.Reverse();
        return list;
    }

    public bool CheckDependencies(out IReadOnlyList<string> missing)
    {
        var registered = new HashSet<string>(_modules.Keys, StringComparer.OrdinalIgnoreCase);
        var missingList = new List<string>();
        foreach (var mod in _modules.Values)
        {
            foreach (var dep in mod.Dependencies)
            {
                if (!registered.Contains(dep))
                    missingList.Add($"{mod.ModuleName} requires {dep}");
            }
        }
        missing = missingList;
        return missingList.Count == 0;
    }
}
