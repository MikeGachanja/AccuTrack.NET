using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Handles component serialization and deserialization.
/// </summary>
public static class ComponentSerializer
{
    private const int CURRENT_COMPONENT_VERSION = 1;

    /// <summary>
    /// Serializes a component to JSON.
    /// </summary>
    public static JObject SerializeComponent(object component)
    {
        // Use dynamic to call ToJson() method if available
        try
        {
            dynamic comp = component;
            var json = comp.ToJson();
            
            // Add version information
            json["componentVersion"] = CURRENT_COMPONENT_VERSION;
            json["serializedDate"] = DateTime.Now.ToString("O");

            return json;
        }
        catch
        {
            // Fallback: create basic JSON structure
            return new JObject
            {
                ["componentVersion"] = CURRENT_COMPONENT_VERSION,
                ["type"] = component.GetType().Name,
                ["serializedDate"] = DateTime.Now.ToString("O")
            };
        }
    }

    /// <summary>
    /// Deserializes a component from JSON.
    /// </summary>
    public static T? DeserializeComponent<T>(JObject json) where T : class
    {
        try
        {
            // Check version and migrate if needed
            int version = json["componentVersion"]?.ToObject<int>() ?? 0;
            if (version < CURRENT_COMPONENT_VERSION)
            {
                json = MigrateComponent(json, version, CURRENT_COMPONENT_VERSION);
            }

            // Try to use FromJson static method if available
            var type = typeof(T);
            var fromJsonMethod = type.GetMethod("FromJson", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(JObject) },
                null);

            if (fromJsonMethod != null)
            {
                return fromJsonMethod.Invoke(null, new object[] { json }) as T;
            }

            // Fallback: create instance and populate properties
            var instance = Activator.CreateInstance<T>();
            PopulateComponentFromJson(instance, json);
            return instance;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Migrates component from an older version to a newer version.
    /// </summary>
    private static JObject MigrateComponent(JObject json, int fromVersion, int toVersion)
    {
        // Version 0 -> Version 1: Add version field
        if (fromVersion == 0 && toVersion >= 1)
        {
            if (json["componentVersion"] == null)
            {
                json["componentVersion"] = 1;
            }
        }

        // Future migrations can be added here

        return json;
    }

    /// <summary>
    /// Populates component properties from JSON (fallback method).
    /// </summary>
    private static void PopulateComponentFromJson(object component, JObject json)
    {
        var type = component.GetType();
        foreach (var prop in type.GetProperties())
        {
            if (json[prop.Name] != null && prop.CanWrite)
            {
                try
                {
                    var value = json[prop.Name].ToObject(prop.PropertyType);
                    prop.SetValue(component, value);
                }
                catch
                {
                    // Skip properties that can't be set
                }
            }
        }
    }

    /// <summary>
    /// Serializes a list of components.
    /// </summary>
    public static JArray SerializeComponents(IEnumerable<object> components)
    {
        var array = new JArray();
        foreach (var component in components)
        {
            array.Add(SerializeComponent(component));
        }
        return array;
    }

    /// <summary>
    /// Deserializes a list of components.
    /// </summary>
    public static List<T> DeserializeComponents<T>(JArray jsonArray) where T : class
    {
        var components = new List<T>();
        foreach (var item in jsonArray)
        {
            if (item is JObject componentObj)
            {
                var component = DeserializeComponent<T>(componentObj);
                if (component != null)
                {
                    components.Add(component);
                }
            }
        }
        return components;
    }
}
