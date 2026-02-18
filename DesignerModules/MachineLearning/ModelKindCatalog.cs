using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Catalog of available ML model kinds (built-in + installed). Single source of truth for the ML editor.
/// </summary>
public static class ModelKindCatalog
{
    public const string FastForestRegressionId = "FastForestRegression";

    /// <summary>Folder where installed ML model kind manifests are stored (e.g. from Package Manager).</summary>
    public static string InstalledKindsFolder =>
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "DarkStar", "MLModelKinds");

    /// <summary>Folder where imported trained model data (.zip etc.) is stored so it is always available for future use.</summary>
    public static string SystemTrainedModelsFolder =>
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "DarkStar", "MLModels");

    private static List<ModelKind>? _builtInKinds;

    /// <summary>Built-in model kinds shipped with the app.</summary>
    public static IReadOnlyList<ModelKind> GetBuiltInKinds()
    {
        if (_builtInKinds != null)
            return _builtInKinds;

        _builtInKinds = new List<ModelKind>
        {
            new ModelKind
            {
                Id = FastForestRegressionId,
                DisplayName = "Fast Forest (Regression)",
                Description = "Regression using Fast Forest (ML.NET); N numeric inputs, 1 numeric output.",
                InputSchema = -1,
                OutputSchema = "SingleNumeric",
                TrainedDataFormat = "ML.NET.zip",
                Source = "BuiltIn"
            }
        };
        return _builtInKinds;
    }

    /// <summary>Model kinds installed from packages or "Add from folder". Reads from InstalledKindsFolder.</summary>
    public static IReadOnlyList<ModelKind> GetInstalledKinds()
    {
        var list = new List<ModelKind>();
        var dir = InstalledKindsFolder;
        if (!Directory.Exists(dir)) return list;
        try
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var json = JToken.Parse(File.ReadAllText(path));
                    if (json is JArray arr)
                    {
                        foreach (var item in arr)
                            if (item is JObject obj && TryParseKind(obj, out var kind))
                                list.Add(kind);
                    }
                    else if (json is JObject obj && TryParseKind(obj, out var kind))
                        list.Add(kind);
                }
                catch
                {
                    // Skip invalid manifest files
                }
            }
        }
        catch
        {
            // Ignore IO errors
        }
        return list;
    }

    private static bool TryParseKind(JObject obj, out ModelKind kind)
    {
        kind = new ModelKind();
        var id = obj["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return false;
        kind.Id = id.Trim();
        kind.DisplayName = obj["displayName"]?.ToString()?.Trim() ?? kind.Id;
        kind.Description = obj["description"]?.ToString()?.Trim() ?? "";
        kind.InputSchema = obj["inputSchema"]?.ToObject<int>() ?? -1;
        kind.OutputSchema = obj["outputSchema"]?.ToString()?.Trim() ?? "SingleNumeric";
        kind.TrainedDataFormat = obj["trainedDataFormat"]?.ToString()?.Trim() ?? "ML.NET.zip";
        kind.Source = "Installed";
        return true;
    }

    /// <summary>All available kinds (built-in + installed).</summary>
    public static IReadOnlyList<ModelKind> GetAllKinds()
    {
        var list = new List<ModelKind>(GetBuiltInKinds());
        list.AddRange(GetInstalledKinds());
        return list;
    }

    /// <summary>Get a kind by id, or null.</summary>
    public static ModelKind? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var k in GetAllKinds())
        {
            if (string.Equals(k.Id, id, System.StringComparison.OrdinalIgnoreCase))
                return k;
        }
        return null;
    }
}
