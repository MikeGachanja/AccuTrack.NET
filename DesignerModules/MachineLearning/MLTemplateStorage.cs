using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Stores and loads ML model configuration templates in the Designer data folder (%LocalAppData%\DarkStar\DesignerData\MLTemplates).
/// Templates are user/designer-wide (not per-project).
/// </summary>
public static class MLTemplateStorage
{
    private const string TemplatesFileName = "templates.json";

    /// <summary>Resolves the folder path for ML templates. Creates the folder on first use when saving/loading.</summary>
    public static string GetTemplatesPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DarkStar",
            "DesignerData",
            "MLTemplates");
    }

    /// <summary>Full path to templates.json.</summary>
    private static string GetTemplatesFilePath()
    {
        return Path.Combine(GetTemplatesPath(), TemplatesFileName);
    }

    /// <summary>Ensures the templates directory exists. Returns true if created or already exists.</summary>
    public static bool EnsureTemplatesFolder()
    {
        var path = GetTemplatesPath();
        if (Directory.Exists(path)) return true;
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Load all templates. Returns empty list if file missing or invalid.</summary>
    public static IReadOnlyList<MLTemplate> LoadTemplates()
    {
        var list = new List<MLTemplate>();
        var filePath = GetTemplatesFilePath();
        if (!File.Exists(filePath)) return list;
        try
        {
            var json = JToken.Parse(File.ReadAllText(filePath));
            if (json is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JObject obj && TryParseTemplate(obj, out var t))
                        list.Add(t);
                }
            }
        }
        catch
        {
            // Return empty on any error
        }
        return list;
    }

    /// <summary>Load a single template by id. Returns null if not found.</summary>
    public static MLTemplate? LoadTemplate(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (var t in LoadTemplates())
        {
            if (string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    /// <summary>Load templates that match the given model kind id (for dropdown in Tab 3).</summary>
    public static IReadOnlyList<MLTemplate> LoadTemplatesForKind(string? modelKindId)
    {
        if (string.IsNullOrWhiteSpace(modelKindId)) return Array.Empty<MLTemplate>();
        var list = new List<MLTemplate>();
        foreach (var t in LoadTemplates())
        {
            if (string.Equals(t.ModelKindId, modelKindId, StringComparison.OrdinalIgnoreCase))
                list.Add(t);
        }
        return list;
    }

    /// <summary>Save a new template or overwrite existing by id. Returns false on failure.</summary>
    public static bool SaveTemplate(MLTemplate template)
    {
        if (template == null || string.IsNullOrWhiteSpace(template.Id)) return false;
        if (!EnsureTemplatesFolder()) return false;
        var filePath = GetTemplatesFilePath();
        var list = new List<MLTemplate>(LoadTemplates());
        var idx = list.FindIndex(t => string.Equals(t.Id, template.Id, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            list[idx] = template;
        else
            list.Add(template);
        try
        {
            var arr = new JArray();
            foreach (var t in list)
                arr.Add(TemplateToJObject(t));
            File.WriteAllText(filePath, arr.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Save template from name, model kind id, and parameters. Generates a new id if not provided.</summary>
    public static bool SaveTemplate(string name, string modelKindId, Dictionary<string, object> parameters, string? id = null)
    {
        var t = new MLTemplate
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Name = name ?? "Unnamed",
            ModelKindId = modelKindId ?? "",
            Parameters = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>()
        };
        return SaveTemplate(t);
    }

    /// <summary>Delete a template by id. Returns true if removed or did not exist.</summary>
    public static bool DeleteTemplate(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return true;
        if (!File.Exists(GetTemplatesFilePath())) return true;
        var list = new List<MLTemplate>(LoadTemplates());
        var removed = list.RemoveAll(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed) return true;
        try
        {
            var arr = new JArray();
            foreach (var t in list)
                arr.Add(TemplateToJObject(t));
            File.WriteAllText(GetTemplatesFilePath(), arr.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseTemplate(JObject obj, out MLTemplate template)
    {
        template = new MLTemplate();
        var id = obj["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return false;
        template.Id = id.Trim();
        template.Name = obj["name"]?.ToString()?.Trim() ?? "Unnamed";
        template.ModelKindId = obj["modelKindId"]?.ToString()?.Trim() ?? "";
        var parametersObj = obj["parameters"] as JObject;
        if (parametersObj != null)
        {
            foreach (var prop in parametersObj.Properties())
                template.Parameters[prop.Name] = prop.Value.ToObject<object>()!;
        }
        return true;
    }

    private static JObject TemplateToJObject(MLTemplate t)
    {
        var parametersObj = new JObject();
        foreach (var p in t.Parameters)
            parametersObj[p.Key] = JToken.FromObject(p.Value);
        return new JObject
        {
            ["id"] = t.Id,
            ["name"] = t.Name,
            ["modelKindId"] = t.ModelKindId,
            ["parameters"] = parametersObj
        };
    }
}

/// <summary>Single ML configuration template (name, model kind, parameters snapshot).</summary>
public class MLTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModelKindId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}
