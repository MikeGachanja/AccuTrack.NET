using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Console;

namespace Runtime.Views;

public partial class LogsView : UserControl
{
    private readonly ObservableCollection<LogLine> _logLines = new();
    private IConsole? _console;

    public LogsView()
    {
        InitializeComponent();
        LogList = this.FindControl<ItemsControl>("LogList");
        BackButton = this.FindControl<Button>("BackButton");
        ClearButton = this.FindControl<Button>("ClearButton");
        if (LogList != null) LogList.ItemsSource = _logLines;
        if (BackButton != null) BackButton.Click += OnBackClicked;
        if (ClearButton != null) ClearButton.Click += OnClearClicked;
    }

    public void SetConsole(IConsole? console)
    {
        if (_console != null)
            _console.NewLogEntry -= OnNewLogEntry;
        _console = console;
        if (_console != null)
        {
            _console.NewLogEntry += OnNewLogEntry;
            foreach (var entry in _console.GetLogEntries())
                _logLines.Add(new LogLine(entry));
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnNewLogEntry(object? sender, LogEntry entry)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logLines.Add(new LogLine(entry));
        });
    }

    private void OnBackClicked(object? sender, RoutedEventArgs e) => RequestClose?.Invoke(this, EventArgs.Empty);

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        _console?.ClearLogs();
        _logLines.Clear();
    }

    public event EventHandler? RequestClose;

    private sealed class LogLine
    {
        public string Line { get; }
        public LogLine(LogEntry e) => Line = $"[{e.Timestamp:HH:mm:ss}] [{e.Level}] {e.Source}: {e.Message}";
    }
}
