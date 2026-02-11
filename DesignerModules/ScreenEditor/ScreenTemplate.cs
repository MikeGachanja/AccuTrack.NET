using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;
using Designer.Modules.Components;

namespace Designer.Modules.ScreenEditor;

/// <summary>
/// Represents a screen template with components and layout.
/// </summary>
public class ScreenTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Size Size { get; set; } = new Size(1920, 1080);
    public Color BackgroundColor { get; set; } = Color.White;
    public List<object> Components { get; set; } = new List<object>(); // Will hold BaseComponent objects
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Converts screen template to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var componentsArray = new JArray();
        foreach (var comp in Components.OfType<BaseComponent>())
        {
            componentsArray.Add(comp.ToJson());
        }

        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["description"] = Description,
            ["size"] = new JObject
            {
                ["width"] = Size.Width,
                ["height"] = Size.Height
            },
            ["backgroundColor"] = ColorTranslator.ToHtml(BackgroundColor),
            ["components"] = componentsArray,
            ["createdDate"] = CreatedDate.ToString("O"),
            ["modifiedDate"] = ModifiedDate.ToString("O")
        };
    }

    /// <summary>
    /// Creates screen template from JSON object.
    /// </summary>
    public static ScreenTemplate FromJson(JObject json)
    {
        var screen = new ScreenTemplate
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty,
            FilePath = json["filePath"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            screen.Id = id;
        }

        var sizeObj = json["size"] as JObject;
        if (sizeObj != null)
        {
            screen.Size = new Size(
                sizeObj["width"]?.ToObject<int>() ?? 1920,
                sizeObj["height"]?.ToObject<int>() ?? 1080
            );
        }

        if (ColorTranslator.FromHtml(json["backgroundColor"]?.ToString() ?? "#FFFFFF") is Color bgColor)
        {
            screen.BackgroundColor = bgColor;
        }

        if (DateTime.TryParse(json["createdDate"]?.ToString(), out DateTime createdDate))
        {
            screen.CreatedDate = createdDate;
        }

        if (DateTime.TryParse(json["modifiedDate"]?.ToString(), out DateTime modifiedDate))
        {
            screen.ModifiedDate = modifiedDate;
        }

        // Load components
        var componentsArray = json["components"] as JArray;
        if (componentsArray != null)
        {
            foreach (var item in componentsArray)
            {
                if (item is JObject compObj)
                {
                    var component = DeserializeComponent(compObj);
                    if (component != null)
                    {
                        screen.Components.Add(component);
                    }
                }
            }
        }

        return screen;
    }

    /// <summary>
    /// Deserializes a component from JSON using ComponentSerializer or direct creation.
    /// </summary>
    private static BaseComponent? DeserializeComponent(JObject json)
    {
        try
        {
            string? componentType = json["componentType"]?.ToString();
            if (string.IsNullOrEmpty(componentType))
                return null;

            // Use ComponentSerializer for deserialization
            // We need to create the component instance first based on type
            BaseComponent? component = componentType switch
            {
                "Button" => new ButtonComponent(),
                "TextLabel" => new TextLabelComponent(),
                "TextInput" => new TextInputComponent(),
                "Checkbox" => new CheckboxComponent(),
                "RadioButton" => new RadioButtonComponent(),
                "ProgressBar" => new ProgressBarComponent(),
                "Slider" => new SliderComponent(),
                "GaugeView" => new GaugeViewComponent(),
                "TrendView" => new TrendViewComponent(),
                "Indicator" => new IndicatorComponent(),
                "ImageView" => new ImageViewComponent(),
                "DateTime" => new DateTimeComponent(),
                "AlarmView" => new AlarmViewComponent(),
                "Table" => new TableComponent(),
                "Rectangle" => new RectangleComponent(),
                "Line" => new LineComponent(),
                "Triangle" => new TriangleComponent(),
                "Spinner" => new SpinnerComponent(),
                "ToggleSwitch" => new ToggleSwitchComponent(),
                "Motor" => new MotorComponent(),
                "Pump" => new PumpComponent(),
                "Tank" => new TankComponent(),
                "Conveyor" => new ConveyorComponent(),
                "Tab" => new TabComponent(),
                "Popup" => new PopupComponent(),
                _ => null
            };

            if (component != null)
            {
                component.FromJson(json);
            }

            return component;
        }
        catch
        {
            return null;
        }
    }
}
