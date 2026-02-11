using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.ScriptEditor;

/// <summary>
/// Represents a Lua script.
/// </summary>
public class LuaScript
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    public LuaScript(string name, string description = "", string filePath = "")
    {
        Name = name;
        Description = description;
        FilePath = filePath;
    }

    /// <summary>
    /// Loads script code from file.
    /// </summary>
    public bool LoadCode()
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            return false;

        try
        {
            Code = File.ReadAllText(FilePath);
            ModifiedDate = File.GetLastWriteTime(FilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves script code to file.
    /// </summary>
    public bool SaveCode()
    {
        if (string.IsNullOrEmpty(FilePath))
            return false;

        try
        {
            string directory = Path.GetDirectoryName(FilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(FilePath, Code);
            ModifiedDate = DateTime.Now;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts script to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["description"] = Description,
            ["code"] = Code,
            ["filePath"] = FilePath,
            ["createdDate"] = CreatedDate.ToString("O"),
            ["modifiedDate"] = ModifiedDate.ToString("O")
        };
    }

    /// <summary>
    /// Creates script from JSON object.
    /// </summary>
    public static LuaScript FromJson(JObject json)
    {
        var script = new LuaScript(
            json["name"]?.ToString() ?? string.Empty,
            json["description"]?.ToString() ?? string.Empty,
            json["filePath"]?.ToString() ?? string.Empty
        );

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            script.Id = id;
        }

        script.Code = json["code"]?.ToString() ?? string.Empty;

        if (DateTime.TryParse(json["createdDate"]?.ToString(), out DateTime createdDate))
        {
            script.CreatedDate = createdDate;
        }

        if (DateTime.TryParse(json["modifiedDate"]?.ToString(), out DateTime modifiedDate))
        {
            script.ModifiedDate = modifiedDate;
        }

        return script;
    }
}
