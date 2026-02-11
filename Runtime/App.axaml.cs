using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Runtime.Modules.Communication;
using Runtime.Modules.Console;
using Runtime.Modules.Discovery;
using Runtime.Modules.Project;
using Runtime.Modules.Screens;

namespace Runtime;

public partial class App : Application
{
    public override void Initialize()
    {
        // Ensure light theme is applied
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new Views.MainWindow();
            desktop.MainWindow = mainWindow;

            var console = Services.GetModule<ConsoleModule>("Console");
            if (console != null)
                mainWindow.SetConsole(console);

            if (Services.Get<ProjectTransferServer>() is { } transferServer)
                mainWindow.SetTransferServer(transferServer);
            if (Services.Get<ICommunication>() is { } communication)
                mainWindow.SetCommunication(communication);
            
            IScreens? screensModule = null;
            if (Services.Get<IScreens>() is { } screens)
            {
                screensModule = screens;
                mainWindow.SetScreensModule(screens);
            }
            
            if (Services.Get<IProject>() is { } project)
            {
                mainWindow.SetProject(project);
                
                // Load the first screen if project is already loaded (startup scenario)
                // This ensures screens are displayed even if project loaded before UI initialization
                if (project.CurrentProject != null)
                {
                    var firstScreenPath = project.GetFirstScreenPath();
                    if (!string.IsNullOrEmpty(firstScreenPath) && screensModule != null)
                    {
                        // Use dispatcher to ensure UI is ready
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            screensModule.ScreenManager.LoadScreen(firstScreenPath);
                        });
                    }
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
