using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Alarms;

namespace Runtime.Views;

public partial class AlarmsView : UserControl
{
    private readonly ObservableCollection<AlarmRow> _alarms = new();
    private IAlarms? _alarmsModule;
    private System.Timers.Timer? _refreshTimer;

    public AlarmsView()
    {
        InitializeComponent();
        AlarmsList = this.FindControl<ItemsControl>("AlarmsList");
        BackButton = this.FindControl<Button>("BackButton");
        if (AlarmsList != null) AlarmsList.ItemsSource = _alarms;
        if (BackButton != null) BackButton.Click += (_, _) => RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public void SetAlarms(IAlarms? alarmsModule)
    {
        _alarmsModule = alarmsModule;
        RefreshAlarms();
        _refreshTimer?.Stop();
        _refreshTimer = new System.Timers.Timer(1000) { AutoReset = true };
        _refreshTimer.Elapsed += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(RefreshAlarms);
        _refreshTimer.Start();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void RefreshAlarms()
    {
        _alarms.Clear();
        if (_alarmsModule == null) return;
        var manager = _alarmsModule.AlarmManager;
        foreach (var alarm in manager.GetActiveAlarmObjects())
            _alarms.Add(new AlarmRow(alarm));
    }

    public event EventHandler? RequestClose;

    private sealed class AlarmRow
    {
        public string Name { get; }
        public string State { get; }
        public string Time { get; }
        public AlarmRow(Alarm a)
        {
            Name = a.Name;
            State = a.State.ToString();
            Time = a.ActivationTime.Ticks > 0 ? a.ActivationTime.ToString("HH:mm:ss") : "";
        }
    }
}
