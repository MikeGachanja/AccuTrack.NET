using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Console;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeConsoleView : UserControl
{
    private readonly ObservableCollection<string> _logLines = new();
    private IConsole? _console;
    private ComponentDescriptor? _descriptor;
    private int _maxLines = 500;

    public RuntimeConsoleView()
    {
        InitializeComponent();
        if (LogList != null)
            LogList.ItemsSource = _logLines;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        _descriptor = d;
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        if (TheBorder != null)
        {
            // Background color (supports named colors like "Silver" and hex)
            var backColor = GetProperty(d, "backColor", "Silver");
            TheBorder.Background = ColorParser.ParseBrush(backColor);
            System.Diagnostics.Trace.WriteLine($"[RuntimeConsoleView] Applied backColor: '{backColor}'");
            
            // Border color
            var borderColor = GetProperty(d, "borderColor", "Gray");
            TheBorder.BorderBrush = ColorParser.ParseBrush(borderColor);
            System.Diagnostics.Trace.WriteLine($"[RuntimeConsoleView] Applied borderColor: '{borderColor}'");
            
            // Border width
            var borderWidth = GetPropertyInt(d, "borderWidth", 1);
            TheBorder.BorderThickness = new Thickness(borderWidth);
        }
        
        // Max lines property
        _maxLines = GetPropertyInt(d, "maxLines", 500);
        TrimLogs();
        
        // Font and text color properties will be applied via the ItemTemplate style
        // We store the descriptor so we can access these properties when formatting log entries
        UpdateItemTemplate();
    }
    
    private void UpdateItemTemplate()
    {
        if (LogList == null || _descriptor == null) return;
        
        // Get font properties from descriptor
        var fontName = GetProperty(_descriptor, "font", "Consolas");
        var fontSize = GetPropertyDouble(_descriptor, "fontSize", 12.0);
        var fontStyleStr = GetProperty(_descriptor, "fontStyle", "Regular");
        var foreColor = GetProperty(_descriptor, "foreColor", "Black");
        
        System.Diagnostics.Trace.WriteLine($"[RuntimeConsoleView] UpdateItemTemplate: font={fontName}, fontSize={fontSize}, fontStyle={fontStyleStr}, foreColor={foreColor}");
        
        // Parse font style
        var fontWeight = FontWeight.Normal;
        var fontStyle = FontStyle.Normal;
        if (fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            fontWeight = FontWeight.Bold;
        if (fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase))
            fontStyle = FontStyle.Italic;
        
        // Create a DataTemplate with TextBlock that has the properties applied
        var dataTemplate = new FuncDataTemplate<string>((item, _) =>
        {
            var textBlock = new TextBlock
            {
                Text = item ?? "",
                FontFamily = new FontFamily(fontName),
                FontSize = fontSize,
                FontWeight = fontWeight,
                FontStyle = fontStyle,
                Foreground = ColorParser.ParseBrush(foreColor),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1)
            };
            return textBlock;
        });
        
        LogList.ItemTemplate = dataTemplate;
        System.Diagnostics.Trace.WriteLine($"[RuntimeConsoleView] ItemTemplate updated with font properties");
    }
    
    private void TrimLogs()
    {
        while (_logLines.Count > _maxLines)
        {
            _logLines.RemoveAt(0);
        }
    }

    public void SetConsole(IConsole? console)
    {
        if (_console != null)
        {
            _console.NewLogEntry -= OnNewLogEntry;
            _console.LogsCleared -= OnLogsCleared;
        }
        _console = console;
        if (_console != null)
        {
            _console.NewLogEntry += OnNewLogEntry;
            _console.LogsCleared += OnLogsCleared;
            _logLines.Clear();
            foreach (var entry in _console.GetLogEntries())
                _logLines.Add(FormatLogEntry(entry));
        }
        else
        {
            _logLines.Clear();
        }
    }

    private void OnNewLogEntry(object? sender, LogEntry entry)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logLines.Add(FormatLogEntry(entry));
            TrimLogs();
        });
    }

    private void OnLogsCleared(object? sender, System.EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _logLines.Clear());
    }

    private static string FormatLogEntry(LogEntry e) =>
        $"[{e.Timestamp:HH:mm:ss}] [{e.Level}] {e.Source}: {e.Message}";

    private static string? GetProperty(ComponentDescriptor d, string key, string? fallback)
    {
        // Check Properties dictionary first (includes root-level properties parsed by ScreenRenderer)
        if (d.Properties.TryGetValue(key, out var v))
        {
            if (v is string s) return s;
            return v?.ToString() ?? fallback;
        }
        return fallback;
    }
    
    private static double GetPropertyDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
    
    private static int GetPropertyInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return (int)dbl;
        if (v is float f) return (int)f;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
}
