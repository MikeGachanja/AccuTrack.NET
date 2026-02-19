using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Alarms;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeAlarmView : UserControl
{
    private readonly ObservableCollection<AlarmRow> _alarms = new();
    private System.Timers.Timer? _refreshTimer;
    private IAlarms? _alarmsModule;

    public RuntimeAlarmView()
    {
        InitializeComponent();
        if (AlarmsList != null)
            AlarmsList.ItemsSource = _alarms;
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        TitleText.Text = GetProperty(d, "title", "Alarms");
        
        // Font from designer (headerFont, headerFontSize, headerFontStyle for title)
        var fontName = GetProperty(d, "headerFont", "Arial");
        var fontSize = GetPropDouble(d, "headerFontSize", 12);
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "headerFontStyle", "Bold");
        TitleText.FontFamily = new FontFamily(fontName);
        TitleText.FontSize = fontSize;
        TitleText.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.SemiBold;
        TitleText.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        
        // Apply styling
        var backColor = GetColorProperty(d, "backColor", Colors.White);
        MainBorder.Background = new SolidColorBrush(backColor);
        if (AlarmsContainer != null)
            AlarmsContainer.Background = new SolidColorBrush(backColor);
        
        var headerBackColor = GetColorProperty(d, "headerBackColor", Colors.LightGray);
        var headerForeColor = GetColorProperty(d, "headerForeColor", Colors.Black);
        
        // Configure columns if specified
        var columns = new List<string> { "Time", "Tag", "Message", "Status" };
        if (d.Properties.TryGetValue("columns", out var colsObj) && colsObj is List<string> cols && cols.Count > 0)
        {
            columns = cols;
        }
        
        var showHeader = GetProperty(d, "showHeader", "true").Equals("true", StringComparison.OrdinalIgnoreCase);
        if (HeaderGrid != null)
        {
            HeaderGrid.Children.Clear();
            HeaderGrid.ColumnDefinitions.Clear();
            
            if (showHeader)
            {
                // Create header row
                for (int i = 0; i < columns.Count; i++)
                {
                    HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == columns.Count - 1 ? GridLength.Star : GridLength.Auto });
                    var headerText = new TextBlock
                    {
                        Text = columns[i],
                        FontWeight = FontWeight.Bold,
                        Padding = new Avalonia.Thickness(4),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(headerText, i);
                    HeaderGrid.Children.Add(headerText);
                }
            }
            else
            {
                // Hide header border if header is not shown
                var headerBorder = this.FindControl<Border>("HeaderBorder");
                if (headerBorder != null)
                    headerBorder.IsVisible = false;
            }
        }
        
        // Update alarm row template based on columns
        UpdateAlarmRowTemplate(columns);
        
        var maxRows = GetPropInt(d, "maxRows", 20);
        // Note: MaxRows would need to be implemented via filtering if needed
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        // Connect to AlarmsModule
        ConnectToAlarmsModule();
    }

    private void ConnectToAlarmsModule()
    {
        try
        {
            var engine = ExecutionEngine.Instance;
            _alarmsModule = engine.ModuleManager.GetModule("AlarmsModule") as IAlarms;
            
            if (_alarmsModule != null)
            {
                RefreshAlarms();
                _refreshTimer?.Stop();
                _refreshTimer = new System.Timers.Timer(1000) { AutoReset = true };
                _refreshTimer.Elapsed += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(RefreshAlarms);
                _refreshTimer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RuntimeAlarmView] Error connecting to AlarmsModule: {ex.Message}");
        }
    }

    private void RefreshAlarms()
    {
        _alarms.Clear();
        if (_alarmsModule == null) return;
        
        var manager = _alarmsModule.AlarmManager;
        foreach (var alarm in manager.GetActiveAlarmObjects())
        {
            _alarms.Add(new AlarmRow(alarm));
        }
    }

    private void UpdateAlarmRowTemplate(List<string> columns)
    {
        // The template is defined in XAML, but we can dynamically adjust it if needed
        // For now, the AlarmRow class provides all the properties needed
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static int GetPropInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return (int)dbl;
        if (v is float f) return (int)f;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static Color GetColorProperty(ComponentDescriptor d, string key, Color fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        var colorStr = v?.ToString() ?? "";
        if (string.IsNullOrEmpty(colorStr)) return fallback;
        
        // Try to parse color (supports named colors and hex)
        try
        {
            if (colorStr.StartsWith("#"))
            {
                return Color.Parse(colorStr);
            }
            // Try named color
            var colorProp = typeof(Colors).GetProperty(colorStr, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (colorProp != null && colorProp.GetValue(null) is Color namedColor)
                return namedColor;
        }
        catch { }
        
        return fallback;
    }

    private sealed class AlarmRow
    {
        public string Time { get; }
        public string TagName { get; }
        public string Message { get; }
        public string Status { get; }

        public AlarmRow(Alarm alarm)
        {
            Time = alarm.ActivationTime.Ticks > 0 ? alarm.ActivationTime.ToString("HH:mm:ss") : "";
            TagName = alarm.TagName ?? "";
            Message = alarm.Description ?? "";
            Status = alarm.State.ToString();
        }
    }
}
