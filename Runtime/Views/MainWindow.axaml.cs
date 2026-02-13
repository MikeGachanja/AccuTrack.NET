using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Runtime.Modules.Communication;
using Runtime.Modules.Discovery;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Project;
using Runtime.Modules.Screens;
using Runtime.Modules.TagsEngine;

namespace Runtime.Views;

public partial class MainWindow : Window
{
    private IProject? _project;
    private ProjectTransferServer? _transferServer;
    private ICommunication? _communication;
    private IScreens? _screensModule;
    private System.Timers.Timer? _statusTimer;
    private ContentControl? _screenContainer;

    public MainWindow()
    {
        InitializeComponent();
        
        _screenContainer = this.FindControl<ContentControl>("ScreenContainer");
    }

    internal void SetProject(IProject? project)
    {
        // Unsubscribe from previous project if any
        if (_project != null)
        {
            _project.ResolutionChanged -= OnProjectResolutionChanged;
        }
        
        _project = project;
        
        // Subscribe to resolution changes
        if (_project != null)
        {
            _project.ResolutionChanged += OnProjectResolutionChanged;
            // Apply resolution immediately if project is already loaded
            if (_project.CurrentProject != null)
            {
                ResizeWindowToProjectResolution();
            }
        }
    }

    private void OnProjectResolutionChanged(object? sender, EventArgs e)
    {
        // Resize window when project resolution changes (e.g., after upload)
        Avalonia.Threading.Dispatcher.UIThread.Post(ResizeWindowToProjectResolution);
    }

    private void ResizeWindowToProjectResolution()
    {
        if (_project == null) return;
        
        var width = _project.ResolutionWidth;
        var height = _project.ResolutionHeight;
        
        // Only resize if we have valid dimensions
        if (width > 0 && height > 0)
        {
            // Ensure minimum size constraints are respected
            var minWidth = Math.Max(640, width);
            var minHeight = Math.Max(480, height);
            
            Width = width;
            Height = height;
            MinWidth = minWidth;
            MinHeight = minHeight;
            
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Resized to match project resolution: {width}x{height}");
        }
    }

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
        
        // Only load project screens (JSON files)
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
        _screenContainer.Content = new TextBlock { Text = "Screen: " + path, Foreground = Avalonia.Media.Brushes.Black };
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
