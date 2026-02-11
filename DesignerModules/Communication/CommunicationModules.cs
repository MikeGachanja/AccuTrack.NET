using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Communication;

/// <summary>
/// Represents communication modules configuration for a SCADA project.
/// </summary>
public class CommunicationModules
{
    public List<CommunicationModule> Modules { get; set; } = new List<CommunicationModule>();

    public JObject ToJson()
    {
        var obj = new JObject();
        var modulesArray = new JArray();

        foreach (var module in Modules)
        {
            modulesArray.Add(module.ToJson());
        }

        obj["modules"] = modulesArray;
        return obj;
    }

    public static CommunicationModules FromJson(JObject json)
    {
        var commModules = new CommunicationModules();

        var modulesArray = json["modules"] as JArray;
        if (modulesArray != null)
        {
            foreach (var item in modulesArray)
            {
                if (item is JObject moduleObj)
                {
                    commModules.Modules.Add(CommunicationModule.FromJson(moduleObj));
                }
            }
        }

        return commModules;
    }
}

/// <summary>
/// Represents a communication module (Modbus, OPC UA, etc.).
/// </summary>
public class CommunicationModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "ModbusTCP"; // ModbusTCP, ModbusRTU, OPCUA, EthernetIP, etc.
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;

    public JObject ToJson()
    {
        var settingsObj = new JObject();
        foreach (var setting in Settings)
        {
            settingsObj[setting.Key] = JToken.FromObject(setting.Value);
        }

        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["type"] = Type,
            ["settings"] = settingsObj,
            ["enabled"] = Enabled,
            ["description"] = Description
        };
    }

    public static CommunicationModule FromJson(JObject json)
    {
        var module = new CommunicationModule
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Type = json["type"]?.ToString() ?? "ModbusTCP",
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            Description = json["description"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            module.Id = id;
        }

        var settingsObj = json["settings"] as JObject;
        if (settingsObj != null)
        {
            foreach (var prop in settingsObj.Properties())
            {
                module.Settings[prop.Name] = prop.Value.ToObject<object>();
            }
        }

        return module;
    }
}
