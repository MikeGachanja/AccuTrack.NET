using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Alarms;
using Runtime.Modules.Communication;
using Runtime.Modules.Console;
using Runtime.Modules.Discovery;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Project;
using Runtime.Modules.Screens;
using Runtime.Modules.TagsEngine;

namespace Runtime.Views;

public partial class MainWindow : Window
{
    private IConsole? _console;
    private IProject? _project;
    private ProjectTransferServer? _transferServer;
    private ICommunication? _communication;
    private IScreens? _screensModule;
    private System.Timers.Timer? _statusTimer;
    private ContentControl? _screenContainer;

    public MainWindow()
    {
        InitializeComponent();
        
        // Set window to full screen mode
        WindowState = WindowState.FullScreen;
        
        _screenContainer = this.FindControl<ContentControl>("ScreenContainer");
        HomeButton.Click += OnHomeClicked;
        LogsButton.Click += OnLogsClicked;
        AlarmsButton.Click += OnAlarmsClicked;
    }

    internal void SetConsole(IConsole console)
    {
        _console = console;
        if (_console != null)
            _console.NewLogEntry += OnNewLogEntry;
    }

    internal void SetProject(IProject? project) => _project = project;

    internal void SetScreensModule(IScreens? screensModule)
    {
        _screensModule = screensModule;
        if (_screensModule == null) return;
        _screensModule.ScreenManager.SetLoadScreenCallback(path =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => SetScreenContent(path));
        });
    }

    private void DisposeScreenSubscriptions()
    {
        if (_screenContainer?.Content is Control c && c.Tag is List<IDisposable> subs)
        {
            foreach (var s in subs)
                s.Dispose();
        }
    }

    private void SetScreenContent(string path)
    {
        if (_screenContainer == null) return;
        if (string.IsNullOrEmpty(path))
        {
            DisposeScreenSubscriptions();
            _screenContainer.Content = null;
            return;
        }
        if (path.IndexOf("Logs", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var logsView = new LogsView();
            logsView.SetConsole(_console);
            logsView.RequestClose += (_, _) => { _screensModule?.ScreenManager.UnloadScreen(path); SetScreenContent(""); };
            _screenContainer.Content = logsView;
            return;
        }
        if (path.IndexOf("Alarms", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var alarmsView = new AlarmsView();
            var alarms = ExecutionEngine.Instance.ModuleManager.GetModule("AlarmsModule") as IAlarms;
            alarmsView.SetAlarms(alarms);
            alarmsView.RequestClose += (_, _) => { _screensModule?.ScreenManager.UnloadScreen(path); SetScreenContent(""); };
            _screenContainer.Content = alarmsView;
            return;
        }
        // Project screen: path may be full path to screen JSON
        var jsonPath = path;
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            jsonPath = path + ".json";
        if (File.Exists(jsonPath))
        {
            DisposeScreenSubscriptions();
            var screenDesc = ScreenRenderer.ParseScreen(jsonPath);
            if (screenDesc != null && _screensModule != null)
            {
                var screenId = screenDesc.Id ?? screenDesc.Name ?? path;
                _screensModule.AnimationManager.UnloadScreenAnimations(screenId);
                _screensModule.AnimationManager.AddImplicitVisibilityRules(screenId, screenDesc.Components.Select(c => (c.Id, (string?)c.TagName)));
            }
            var tagsModule = ExecutionEngine.Instance.ModuleManager.GetModule("TagsModule") as ITagsEngine;
            var tagManager = tagsModule?.TagManager;
            var tagIOHandler = tagsModule?.TagIOHandler;
            var eventManager = _screensModule?.EventManager;
            var view = ScreenViewBuilder.Build(screenDesc, tagManager, eventManager, tagIOHandler, name => _screensModule?.ScreenManager.ResolveImagePath(name), _screensModule?.AnimationManager);
            if (view != null)
            {
                _screenContainer.Content = view;
                return;
            }
        }
        _screenContainer.Content = new TextBlock { Text = "Screen: " + path, Foreground = Avalonia.Media.Brushes.White };
    }

    internal void SetTransferServer(ProjectTransferServer server)
    {
        _transferServer = server;
        _transferServer.TransferStarted += (_, name) => Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowTransferOverlay(name));
        _transferServer.TransferProgress += (_, pct) => Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateTransferProgress(pct));
        _transferServer.TransferCompleted += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() => OnTransferCompleted());
        _transferServer.ErrorOccurred += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() => OnTransferError());
    }

    internal void SetCommunication(ICommunication communication)
    {
        _communication = communication;
        UpdateConnectionStatuses();
        _statusTimer = new System.Timers.Timer(1000) { AutoReset = true };
        _statusTimer.Elapsed += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateConnectionStatuses);
        _statusTimer.Start();
    }

    private void OnNewLogEntry(object? sender, LogEntry entry)
    {
        // TODO: Update Logs view when it's implemented
    }

    private void OnHomeClicked(object? sender, RoutedEventArgs e)
    {
        var firstScreen = _project?.GetFirstScreenPath();
        if (!string.IsNullOrEmpty(firstScreen))
            _screensModule?.ScreenManager.LoadScreen(firstScreen);
        else
            SetScreenContent("");
    }

    private void OnLogsClicked(object? sender, RoutedEventArgs e)
    {
        _screensModule?.ScreenManager.LoadScreen("Logs");
    }

    private void OnAlarmsClicked(object? sender, RoutedEventArgs e)
    {
        _screensModule?.ScreenManager.LoadScreen("Alarms");
    }

    internal void UpdateConnectionStatuses()
    {
        if (_communication == null || ConnectionStatusList == null) return;
        var statuses = _communication.GetConnectionStatuses();
        ConnectionStatusList.ItemsSource = statuses != null ? statuses.Values : Array.Empty<ConnectionStatus>();
    }

    internal void ShowTransferOverlay(string projectName)
    {
        if (TransferOverlay != null) TransferOverlay.IsVisible = true;
        if (TransferTitle != null) TransferTitle.Text = "Project Transfer: " + projectName;
        if (TransferStatus != null) TransferStatus.Text = "Receiving files...";
        if (TransferProgressBar != null) TransferProgressBar.Value = 0;
        if (TransferProgressText != null) TransferProgressText.Text = "0%";
    }

    internal void UpdateTransferProgress(int percentage)
    {
        if (TransferProgressBar != null) TransferProgressBar.Value = percentage;
        if (TransferProgressText != null) TransferProgressText.Text = percentage + "%";
        if (TransferStatus != null && percentage < 100) TransferStatus.Text = "Receiving files... " + percentage + "%";
    }

    private async void OnTransferCompleted()
    {
        if (TransferProgressBar != null) TransferProgressBar.Value = 100;
        if (TransferProgressText != null) TransferProgressText.Text = "100%";
        if (TransferStatus != null) TransferStatus.Text = "Transfer complete.";
        await Task.Delay(2000);
        Avalonia.Threading.Dispatcher.UIThread.Post(HideTransferOverlay);
    }

    private async void OnTransferError()
    {
        if (TransferStatus != null) TransferStatus.Text = "Transfer failed.";
        await Task.Delay(2000);
        Avalonia.Threading.Dispatcher.UIThread.Post(HideTransferOverlay);
    }

    internal void HideTransferOverlay()
    {
        if (TransferOverlay != null) TransferOverlay.IsVisible = false;
    }
}
