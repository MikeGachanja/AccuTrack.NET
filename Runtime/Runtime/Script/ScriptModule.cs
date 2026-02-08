namespace AccuTrack.Runtime.Script;

/// <summary>
/// Load and run scripts (Lua or other); expose to EventManager and Schedules. No IModuleInterface.
/// </summary>
public sealed class ScriptModule
{
    private readonly List<string> _loadedScripts = new();
    private Func<string, object?[]?, Task<object?>>? _runScriptImpl;

    public void SetRunner(Func<string, object?[]?, Task<object?>>? impl) => _runScriptImpl = impl;

    public async Task<object?> RunScriptAsync(string scriptNameOrPath, object?[]? args = null, CancellationToken cancellationToken = default)
    {
        if (_runScriptImpl != null)
            return await _runScriptImpl(scriptNameOrPath, args).ConfigureAwait(false);
        return null;
    }

    public void LoadScriptsFromProject(string projectPath)
    {
        var scriptsDir = Path.Combine(projectPath, "scripts");
        if (!Directory.Exists(scriptsDir)) return;
        _loadedScripts.Clear();
        _loadedScripts.AddRange(Directory.GetFiles(scriptsDir, "*.lua").Select(Path.GetFileName)!);
    }

    public IReadOnlyList<string> LoadedScripts => _loadedScripts;
}
