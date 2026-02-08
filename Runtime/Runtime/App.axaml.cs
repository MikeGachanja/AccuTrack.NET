using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AccuTrack.Runtime;

public partial class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Create MainWindow here so IWindowingPlatform is already registered (lifetime callback runs too early)
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = StartupContext.MainWindowViewModel;
            desktop.MainWindow = new Views.MainWindow { DataContext = vm };
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow.Closed += (_, _) => StartupContext.OnMainWindowClosed?.Invoke();
            StartupContext.OnMainWindowCreated?.Invoke();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
