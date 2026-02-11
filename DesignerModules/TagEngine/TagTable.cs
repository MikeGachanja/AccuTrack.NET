using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.TagEngine;

/// <summary>
/// Represents a table of tags.
/// </summary>
public class TagTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    private List<Tag> _tags = new List<Tag>();

    public event EventHandler? Modified;

    public TagTable(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the number of tags in the table.
    /// </summary>
    public int TagCount => _tags.Count;

    /// <summary>
    /// Gets a tag at the specified index.
    /// </summary>
    public Tag? TagAt(int index)
    {
        if (index >= 0 && index < _tags.Count)
            return _tags[index];
        return null;
    }

    /// <summary>
    /// Gets all tags.
    /// </summary>
    public List<Tag> GetTags() => new List<Tag>(_tags);

    /// <summary>
    /// Adds a tag to the table.
    /// </summary>
    public void AddTag(Tag tag)
    {
        _tags.Add(tag);
        OnModified();
    }

    /// <summary>
    /// Removes a tag from the table.
    /// </summary>
    public bool RemoveTag(Tag tag)
    {
        if (_tags.Remove(tag))
        {
            OnModified();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes a tag at the specified index.
    /// </summary>
    public bool RemoveTagAt(int index)
    {
        if (index >= 0 && index < _tags.Count)
        {
            _tags.RemoveAt(index);
            OnModified();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Finds a tag by name.
    /// </summary>
    public Tag? FindTag(string name)
    {
        return _tags.FirstOrDefault(t => t.Name == name);
    }

    /// <summary>
    /// Finds a tag by ID.
    /// </summary>
    public Tag? FindTagById(Guid id)
    {
        return _tags.FirstOrDefault(t => t.Id == id);
    }

    /// <summary>
    /// Clears all tags.
    /// </summary>
    public void Clear()
    {
        _tags.Clear();
        OnModified();
    }

    /// <summary>
    /// Saves the tag table to a file.
    /// </summary>
    public bool SaveToFile(string filePath)
    {
        try
        {
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = ToJson();
            File.WriteAllText(filePath, json.ToString(Newtonsoft.Json.Formatting.Indented));
            FilePath = filePath;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving tag table to {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads the tag table from a file.
    /// </summary>
    public bool LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            string json = File.ReadAllText(filePath);
            var jsonObj = JObject.Parse(json);
            FromJson(jsonObj);
            FilePath = filePath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts tag table to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name
        };

        var tagsArray = new JArray();
        foreach (var tag in _tags)
        {
            tagsArray.Add(tag.ToJson());
        }
        obj["tags"] = tagsArray;
        
        // Include filePath in JSON for reference
        if (!string.IsNullOrEmpty(FilePath))
        {
            obj["filePath"] = FilePath;
        }

        return obj;
    }

    /// <summary>
    /// Creates tag table from JSON object.
    /// </summary>
    public void FromJson(JObject json)
    {
        Name = json["name"]?.ToString() ?? string.Empty;

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            Id = id;
        }
        
        // Restore filePath if present
        FilePath = json["filePath"]?.ToString() ?? string.Empty;

        _tags.Clear();
        var tagsArray = json["tags"] as JArray;
        if (tagsArray != null)
        {
            foreach (var item in tagsArray)
            {
                if (item is JObject tagObj)
                {
                    _tags.Add(Tag.FromJson(tagObj));
                }
            }
        }
    }

    /// <summary>
    /// Raises the Modified event.
    /// </summary>
    protected virtual void OnModified()
    {
        Modified?.Invoke(this, EventArgs.Empty);
    }
}
