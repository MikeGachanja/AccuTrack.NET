using System;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.TagEngine;

/// <summary>
/// Represents a tag in a tag table.
/// </summary>
public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "Float";
    public string Address { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public bool ReadOnly { get; set; }
    public string? CommunicationModule { get; set; }

    /// <summary>
    /// Converts tag to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["dataType"] = DataType,
            ["address"] = Address,
            ["value"] = Value != null ? JToken.FromObject(Value) : null,
            ["description"] = Description,
            ["unit"] = Unit,
            ["minValue"] = MinValue,
            ["maxValue"] = MaxValue,
            ["readOnly"] = ReadOnly,
            ["communicationModule"] = CommunicationModule
        };
    }

    /// <summary>
    /// Creates tag from JSON object.
    /// </summary>
    public static Tag FromJson(JObject json)
    {
        var tag = new Tag
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            DataType = json["dataType"]?.ToString() ?? "Float",
            Address = json["address"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty,
            Unit = json["unit"]?.ToString() ?? string.Empty,
            MinValue = json["minValue"]?.ToObject<double>() ?? 0,
            MaxValue = json["maxValue"]?.ToObject<double>() ?? 0,
            ReadOnly = json["readOnly"]?.ToObject<bool>() ?? false,
            CommunicationModule = json["communicationModule"]?.ToString()
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            tag.Id = id;
        }

        if (json["value"] != null)
        {
            tag.Value = json["value"].ToObject<object>();
        }

        return tag;
    }
}
