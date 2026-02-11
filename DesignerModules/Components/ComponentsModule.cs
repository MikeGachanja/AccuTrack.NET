using System;
using System.Collections.Generic;
using System.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Components module managing available components.
/// </summary>
public class ComponentsModule
{
    private Dictionary<string, Type> _componentTypes = new Dictionary<string, Type>();

    public ComponentsModule()
    {
        RegisterDefaultComponents();
    }

    /// <summary>
    /// Registers default component types.
    /// </summary>
    private void RegisterDefaultComponents()
    {
        RegisterComponent("Button", typeof(ButtonComponent));
        RegisterComponent("TextLabel", typeof(TextLabelComponent));
        RegisterComponent("TextInput", typeof(TextInputComponent));
        RegisterComponent("Checkbox", typeof(CheckboxComponent));
        RegisterComponent("RadioButton", typeof(RadioButtonComponent));
        RegisterComponent("ProgressBar", typeof(ProgressBarComponent));
        RegisterComponent("Slider", typeof(SliderComponent));
        RegisterComponent("GaugeView", typeof(GaugeViewComponent));
        RegisterComponent("TrendView", typeof(TrendViewComponent));
        RegisterComponent("Indicator", typeof(IndicatorComponent));
        RegisterComponent("ImageView", typeof(ImageViewComponent));
        RegisterComponent("DateTime", typeof(DateTimeComponent));
        RegisterComponent("AlarmView", typeof(AlarmViewComponent));
        RegisterComponent("Table", typeof(TableComponent));
        RegisterComponent("Rectangle", typeof(RectangleComponent));
        RegisterComponent("Line", typeof(LineComponent));
        RegisterComponent("Triangle", typeof(TriangleComponent));
        RegisterComponent("Spinner", typeof(SpinnerComponent));
        RegisterComponent("ToggleSwitch", typeof(ToggleSwitchComponent));
        RegisterComponent("Motor", typeof(MotorComponent));
        RegisterComponent("Pump", typeof(PumpComponent));
        RegisterComponent("Tank", typeof(TankComponent));
        RegisterComponent("Conveyor", typeof(ConveyorComponent));
        RegisterComponent("Tab", typeof(TabComponent));
        RegisterComponent("Popup", typeof(PopupComponent));
        
        // Additional components can be registered here as they are implemented
    }

    /// <summary>
    /// Registers a component type.
    /// </summary>
    public void RegisterComponent(string name, Type componentType)
    {
        if (typeof(BaseComponent).IsAssignableFrom(componentType))
        {
            _componentTypes[name] = componentType;
        }
    }

    /// <summary>
    /// Gets available component types.
    /// </summary>
    public List<object> GetAvailableComponents()
    {
        return _componentTypes.Keys.Cast<object>().ToList();
    }

    /// <summary>
    /// Creates a component instance by name.
    /// </summary>
    public BaseComponent? CreateComponent(string componentName)
    {
        if (_componentTypes.TryGetValue(componentName, out var componentType))
        {
            try
            {
                return Activator.CreateInstance(componentType) as BaseComponent;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets component type by name.
    /// </summary>
    public Type? GetComponentType(string componentName)
    {
        return _componentTypes.TryGetValue(componentName, out var type) ? type : null;
    }
}
