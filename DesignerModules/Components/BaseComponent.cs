using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Base class for all screen components.
/// </summary>
public abstract class BaseComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Point Location { get; set; } = Point.Empty;
    public Size Size { get; set; } = new Size(100, 30);
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int ZOrder { get; set; } = 0;
    public string TagName { get; set; } = string.Empty; // Associated tag for data binding
    public List<string> EventIds { get; set; } = new List<string>(); // Associated event IDs
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Component type name (e.g., "Button", "TextLabel", etc.).
    /// </summary>
    public abstract string ComponentType { get; }

    /// <summary>
    /// Gets the bounding rectangle of the component.
    /// </summary>
    public Rectangle Bounds => new Rectangle(Location, Size);

    /// <summary>
    /// Checks if a point is within the component bounds.
    /// </summary>
    public bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    /// <summary>
    /// Moves the component to a new location.
    /// </summary>
    public virtual void Move(Point newLocation)
    {
        Location = newLocation;
    }

    /// <summary>
    /// Resizes the component.
    /// </summary>
    public virtual void Resize(Size newSize)
    {
        Size = newSize;
    }

    /// <summary>
    /// Serializes the component to JSON.
    /// </summary>
    public virtual JObject ToJson()
    {
        var propsObj = new JObject();
        foreach (var prop in Properties)
        {
            propsObj[prop.Key] = JToken.FromObject(prop.Value);
        }

        var json = new JObject
        {
            ["id"] = Id.ToString(),
            ["componentType"] = ComponentType,
            ["name"] = Name,
            ["location"] = new JObject
            {
                ["x"] = Location.X,
                ["y"] = Location.Y
            },
            ["size"] = new JObject
            {
                ["width"] = Size.Width,
                ["height"] = Size.Height
            },
            ["visible"] = Visible,
            ["enabled"] = Enabled,
            ["zOrder"] = ZOrder,
            ["tagName"] = TagName,
            ["properties"] = propsObj
        };
        
        // Add event IDs if any
        if (EventIds != null && EventIds.Count > 0)
        {
            var eventIdsArray = new JArray();
            foreach (var eventId in EventIds)
            {
                eventIdsArray.Add(eventId);
            }
            json["eventIds"] = eventIdsArray;
        }
        
        return json;
    }

    /// <summary>
    /// Deserializes the component from JSON.
    /// </summary>
    public virtual void FromJson(JObject json)
    {
        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            Id = id;
        }

        Name = json["name"]?.ToString() ?? string.Empty;
        TagName = json["tagName"]?.ToString() ?? string.Empty;
        Visible = json["visible"]?.ToObject<bool>() ?? true;
        Enabled = json["enabled"]?.ToObject<bool>() ?? true;
        ZOrder = json["zOrder"]?.ToObject<int>() ?? 0;
        
        // Load event IDs
        EventIds.Clear();
        if (json["eventIds"] is JArray eventIdsArray)
        {
            foreach (var item in eventIdsArray)
            {
                var eventId = item?.ToString();
                if (!string.IsNullOrEmpty(eventId))
                {
                    EventIds.Add(eventId);
                }
            }
        }

        var locationObj = json["location"] as JObject;
        if (locationObj != null)
        {
            Location = new Point(
                locationObj["x"]?.ToObject<int>() ?? 0,
                locationObj["y"]?.ToObject<int>() ?? 0
            );
        }

        var sizeObj = json["size"] as JObject;
        if (sizeObj != null)
        {
            Size = new Size(
                sizeObj["width"]?.ToObject<int>() ?? 100,
                sizeObj["height"]?.ToObject<int>() ?? 30
            );
        }

        var propsObj = json["properties"] as JObject;
        if (propsObj != null)
        {
            Properties.Clear();
            foreach (var prop in propsObj.Properties())
            {
                Properties[prop.Name] = prop.Value.ToObject<object>();
            }
        }
    }

    /// <summary>
    /// Draws the component on the graphics surface.
    /// </summary>
    public abstract void Draw(Graphics g, bool isSelected = false);

    /// <summary>
    /// Creates a copy of this component.
    /// </summary>
    public abstract BaseComponent Clone();
}
