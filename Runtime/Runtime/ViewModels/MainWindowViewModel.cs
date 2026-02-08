using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AccuTrack.Runtime.ViewModels;

/// <summary>
/// MainWindow VM: resolution, connection status list, transfer overlay, Logs/Alarms commands.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _resolutionWidth = 1920;
    private int _resolutionHeight = 1080;
    private bool _transferVisible;
    private int _transferProgress;
    private string _transferStatus = "";
    private object? _content;

    public MainWindowViewModel()
    {
        LogsCommand = new RelayCommand(_ => LoadLogs());
        AlarmsCommand = new RelayCommand(_ => LoadAlarms());
    }

    public ICommand LogsCommand { get; }
    public ICommand AlarmsCommand { get; }

    private void LoadLogs() => LoadScreenRequested?.Invoke(this, "Logs");
    private void LoadAlarms() => LoadScreenRequested?.Invoke(this, "Alarms");

    public event EventHandler<string>? LoadScreenRequested;

    public int ResolutionWidth
    {
        get => _resolutionWidth;
        set { _resolutionWidth = value; OnPropertyChanged(); }
    }

    public int ResolutionHeight
    {
        get => _resolutionHeight;
        set { _resolutionHeight = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> ConnectionStatusList { get; } = new();

    public bool TransferVisible
    {
        get => _transferVisible;
        set { _transferVisible = value; OnPropertyChanged(); }
    }

    public int TransferProgress
    {
        get => _transferProgress;
        set { _transferProgress = value; OnPropertyChanged(); }
    }

    public string TransferStatus
    {
        get => _transferStatus;
        set { _transferStatus = value ?? ""; OnPropertyChanged(); }
    }

    public object? Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public void UpdateConnectionStatuses(IEnumerable<AccuTrack.Runtime.Abstractions.ConnectionStatus> statuses)
    {
        ConnectionStatusList.Clear();
        foreach (var s in statuses)
            ConnectionStatusList.Add($"{s.Name} ({s.DriverType}): {(s.IsConnected ? "Connected" : "Disconnected")}");
    }

    public void ShowTransfer(int progress, string status)
    {
        TransferVisible = true;
        TransferProgress = progress;
        TransferStatus = status;
    }

    public void HideTransfer()
    {
        TransferVisible = false;
        TransferProgress = 0;
        TransferStatus = "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
