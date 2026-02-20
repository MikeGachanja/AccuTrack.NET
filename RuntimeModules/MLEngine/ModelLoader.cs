using System.Reflection;
using Runtime.Modules.Models;

namespace Runtime.Modules.MLEngine;

/// <summary>
/// Discovers and loads ML runner types from the contract assembly and from DLLs in configurable folders
/// (Exchequer StrategyLoader-style). Returns a registry of KindId -> Type for MLEngine to create runners via TryLoad.
/// </summary>
public static class ModelLoader
{
    /// <summary>Optional DLL name prefix so only intentional model assemblies are loaded (e.g. DarkStar.Models.FastForest.dll).</summary>
    public const string DllNamePrefix = "DarkStar.Models.";

    /// <summary>
    /// Load all runner types from the contract assembly (BaseModel) and from DLLs in the given folders.
    /// Folders scanned: baseDirectory/Models, baseDirectory/mlmodels/plugins (if baseDirectory set),
    /// projectPath/mlmodels/plugins (if projectPath set). Subfolders are included (SearchOption.AllDirectories).
    /// DLLs whose filename starts with <see cref="DllNamePrefix"/> are loaded from folder scan; others are skipped.
    /// </summary>
    /// <param name="projectPath">Optional project data path (e.g. engine.ProjectPath). Adds projectPath/mlmodels/plugins.</param>
    /// <param name="baseDirectory">Optional app base directory (e.g. AppDomain.CurrentDomain.BaseDirectory). Adds baseDir/Models and baseDir/mlmodels/plugins.</param>
    /// <returns>Dictionary of model KindId -> runner Type (case-insensitive keys). First occurrence wins for duplicate KindIds.</returns>
    public static Dictionary<string, Type> LoadRunnerTypes(string? projectPath, string? baseDirectory = null)
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var iface = typeof(IMLModelRunner);
        var assembliesToScan = new List<Assembly> { iface.Assembly };

        var foldersToScan = new List<string>();
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            foldersToScan.Add(Path.Combine(baseDirectory, "Models"));
            foldersToScan.Add(Path.Combine(baseDirectory, "mlmodels", "plugins"));
        }
        if (!string.IsNullOrEmpty(projectPath))
            foldersToScan.Add(Path.Combine(projectPath, "mlmodels", "plugins"));

        var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in foldersToScan)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;
            try
            {
                foreach (var dll in Directory.EnumerateFiles(folder, "*.dll", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(dll);
                    if (!fileName.StartsWith(DllNamePrefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (loadedPaths.Contains(dll)) continue;
                    try
                    {
                        var asm = Assembly.LoadFrom(dll);
                        assembliesToScan.Add(asm);
                        loadedPaths.Add(dll);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MLEngine] Could not load plugin {dll}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MLEngine] Plugin folder scan failed for {folder}: {ex.Message}");
            }
        }

        foreach (var asm in assembliesToScan)
        {
            try
            {
                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.IsAbstract || !iface.IsAssignableFrom(type)) continue;
                    var kindId = GetStaticKindId(type);
                    if (string.IsNullOrEmpty(kindId)) continue;
                    if (!HasTryLoad(type)) continue;
                    if (!map.ContainsKey(kindId))
                        map[kindId] = type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that fail to load types
            }
        }

        return map;
    }

    private static string? GetStaticKindId(Type type)
    {
        var field = type.GetField("KindId", BindingFlags.Public | BindingFlags.Static);
        if (field != null && field.FieldType == typeof(string))
            return field.GetValue(null) as string;
        var prop = type.GetProperty("KindId", BindingFlags.Public | BindingFlags.Static);
        if (prop != null && prop.PropertyType == typeof(string) && prop.CanRead)
            return prop.GetValue(null) as string;
        return null;
    }

    private static bool HasTryLoad(Type type)
    {
        var method = type.GetMethod("TryLoad", BindingFlags.Public | BindingFlags.Static);
        if (method == null) return false;
        var ps = method.GetParameters();
        if (ps.Length != 4) return false;
        if (ps[0].ParameterType != typeof(string) || ps[1].ParameterType != typeof(string) || ps[2].ParameterType != typeof(string))
            return false;
        if (ps[3].ParameterType != typeof(IReadOnlyList<string>)) return false;
        var ret = method.ReturnType;
        if (ret.IsValueType && ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(Nullable<>))
            ret = ret.GetGenericArguments()[0];
        return typeof(IMLModelRunner).IsAssignableFrom(ret);
    }
}
