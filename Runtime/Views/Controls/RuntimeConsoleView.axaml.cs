using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Console;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeConsoleView : UserControl
{
    private readonly ObservableCollection<string> _logLines = new();
    private IConsole? _console;

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
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        if (TheBorder != null)
        {
            if (GetProperty(d, "backColor", "#1E1E1E") is string backHex)
                TheBorder.Background = ParseBrush(backHex);
            if (GetProperty(d, "borderColor", "#808080") is string borderHex)
                TheBorder.BorderBrush = ParseBrush(borderHex);
        }
        // ForeColor/font can be applied to item template via style if needed; for now default is used
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
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        if (hex.StartsWith("#") && hex.Length >= 7)
        {
            var r = System.Convert.ToByte(hex.Substring(1, 2), 16);
            var g = System.Convert.ToByte(hex.Substring(3, 2), 16);
            var b = System.Convert.ToByte(hex.Substring(5, 2), 16);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
        return new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
    }
}
