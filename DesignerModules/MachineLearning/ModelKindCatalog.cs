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
                Category = "General",
                InputSchema = -1,
                OutputSchema = "SingleNumeric",
                TrainedDataFormat = "ML.NET.zip",
                Source = "BuiltIn",
                ParameterSchema = GetDefaultParameterSchema()
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

    private static List<ModelKindParameterDef> GetDefaultParameterSchema()
    {
        return new List<ModelKindParameterDef>
        {
            new ModelKindParameterDef
            {
                Key = "dataCleaningMethod",
                DisplayName = "Data cleaning method",
                Type = "choice",
                Options = new List<string> { "None", "MovingAverage", "SoftMax" },
                Default = "None"
            },
            new ModelKindParameterDef
            {
                Key = "movingAverageWindow",
                DisplayName = "Moving average window size",
                Type = "integer",
                Default = 5,
                Min = 2,
                Max = 100
            }
        };
    }

    private static bool TryParseKind(JObject obj, out ModelKind kind)
    {
        kind = new ModelKind();
        var id = obj["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return false;
        kind.Id = id.Trim();
        kind.DisplayName = obj["displayName"]?.ToString()?.Trim() ?? kind.Id;
        kind.Description = obj["description"]?.ToString()?.Trim() ?? "";
        kind.Category = obj["category"]?.ToString()?.Trim() ?? "General";
        kind.InputSchema = obj["inputSchema"]?.ToObject<int>() ?? -1;
        kind.OutputSchema = obj["outputSchema"]?.ToString()?.Trim() ?? "SingleNumeric";
        kind.TrainedDataFormat = obj["trainedDataFormat"]?.ToString()?.Trim() ?? "ML.NET.zip";
        kind.Source = "Installed";
        kind.ParameterSchema = ParseParameterSchema(obj["parameterSchema"] as JArray);
        return true;
    }

    private static List<ModelKindParameterDef> ParseParameterSchema(JArray? arr)
    {
        var list = new List<ModelKindParameterDef>();
        if (arr == null) return list;
        foreach (var item in arr)
        {
            if (item is not JObject o) continue;
            var def = new ModelKindParameterDef
            {
                Key = o["key"]?.ToString()?.Trim() ?? "",
                DisplayName = o["displayName"]?.ToString()?.Trim() ?? o["key"]?.ToString() ?? "",
                Type = o["type"]?.ToString()?.Trim() ?? "string",
                Default = o["default"]?.ToObject<object>()
            };
            if (o["options"] is JArray optArr)
            {
                foreach (var opt in optArr)
                    if (opt?.ToString() is { } s) def.Options.Add(s);
            }
            if (o["min"] != null) def.Min = o["min"]?.ToObject<double?>();
            if (o["max"] != null) def.Max = o["max"]?.ToObject<double?>();
            if (!string.IsNullOrEmpty(def.Key))
                list.Add(def);
        }
        return list;
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

    /// <summary>Get all distinct category display names (for filter dropdown). "All" is not included; add it in UI.</summary>
    public static IReadOnlyList<string> GetCategories()
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var k in GetAllKinds())
        {
            var cat = string.IsNullOrEmpty(k.Category) ? "General" : k.Category;
            set.Add(ToCategoryDisplayName(cat));
        }
        var list = new List<string>(set);
        list.Sort(System.StringComparer.OrdinalIgnoreCase);
        return list;
    }

    /// <summary>Convert category id to display name (e.g. PredictiveMaintenance -> Predictive Maintenance).</summary>
    public static string ToCategoryDisplayName(string category)
    {
        if (string.IsNullOrEmpty(category)) return "General";
        return System.Text.RegularExpressions.Regex.Replace(category.Trim(), "([a-z])([A-Z])", "$1 $2");
    }

    /// <summary>Get kinds filtered by category. Pass null or "All" or empty to get all kinds.</summary>
    public static IReadOnlyList<ModelKind> GetKindsByCategory(string? categoryFilter)
    {
        var all = GetAllKinds();
        if (string.IsNullOrWhiteSpace(categoryFilter) || string.Equals(categoryFilter, "All", System.StringComparison.OrdinalIgnoreCase))
            return all;
        var displayName = ToCategoryDisplayName(categoryFilter);
        var list = new List<ModelKind>();
        foreach (var k in all)
        {
            var cat = string.IsNullOrEmpty(k.Category) ? "General" : k.Category;
            if (string.Equals(ToCategoryDisplayName(cat), displayName, System.StringComparison.OrdinalIgnoreCase))
                list.Add(k);
        }
        return list;
    }
}
