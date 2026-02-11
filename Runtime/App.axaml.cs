using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
            if (Services.Get<IScreens>() is { } screensModule)
                mainWindow.SetScreensModule(screensModule);
            if (Services.Get<IProject>() is { } project)
                mainWindow.SetProject(project);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
