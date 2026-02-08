using System.Reflection;
using AccuTrack.Runtime.Alarms;
using AccuTrack.Runtime.Communication;
using AccuTrack.Runtime.Console;
using AccuTrack.Runtime.DeviceDiscovery;
using AccuTrack.Runtime.EventDispatch;
using Engine = AccuTrack.Runtime.ExecutionEngine.ExecutionEngine;
using AccuTrack.Runtime.Historian;
using AccuTrack.Runtime.MachineLearning;
using AccuTrack.Runtime.Modules;
using AccuTrack.Runtime.Project;
using AccuTrack.Runtime.Schedules;
using AccuTrack.Runtime.Screens;
using AccuTrack.Runtime.Security;
using AccuTrack.Runtime.Script;
using AccuTrack.Runtime.Tags;
using AccuTrack.Runtime.ViewModels;
using AccuTrack.Runtime.Views;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace AccuTrack.Runtime;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataPath);

        var engine = Engine.Instance;
        engine.Initialize(dataPath);

        // Register modules in dependency order (Communication before Tags, etc.)
        var commModule = new CommunicationModule();
        var tagsModule = new TagsModule();
        var alarmsModule = new AlarmsModule();
        var historianModule = new HistorianModule();
        var schedulesModule = new SchedulesModule();
        var securityModule = new SecurityModule();
        var mlModule = new MachineLearningModule();

        engine.ModuleManager.RegisterModule(commModule);
        engine.ModuleManager.RegisterModule(tagsModule);
        engine.ModuleManager.RegisterModule(alarmsModule);
        engine.ModuleManager.RegisterModule(historianModule);
        engine.ModuleManager.RegisterModule(schedulesModule);
        engine.ModuleManager.RegisterModule(securityModule);
        engine.ModuleManager.RegisterModule(mlModule);

        // Wire Tags <-> Communication
        tagsModule.SetDriverProvider(() => commModule.Drivers);

        // Non-IModuleInterface: Script, Console, Project, Screens
        var scriptModule = new ScriptModule();
        var consoleModule = new ConsoleModule();
        var projectModule = new ProjectModule();
        var screenManager = new ScreenManager();
        var eventManager = new EventManager();
        var animationManager = new AnimationManager();
        var historianQueryHelper = new HistorianQueryHelper();

        eventManager.SetTagManager(tagsModule.TagManager);
        eventManager.SetTagIOHandler(tagsModule.TagIOHandler);
        eventManager.SetScriptRunner((name, a) => scriptModule.RunScriptAsync(name, a));
        animationManager.SetTagManager(tagsModule.TagManager);
        alarmsModule.SetTagManager(tagsModule.TagManager);
        alarmsModule.SubscribeToTagEvents(EventDispatcher.Instance);
        historianModule.SetTagManager(tagsModule.TagManager);
        historianQueryHelper.SetQueryProvider((tag, start, end) => historianModule.Manager.QueryAsync(tag, start, end));
        schedulesModule.SetScriptRunner((name, a) => scriptModule.RunScriptAsync(name, a));

        var transferServer = new ProjectTransferServer(dataPath);
        var discoveryResponder = new DeviceDiscoveryResponder();

        var services = new ServiceCollection()
            .AddSingleton(engine)
            .AddSingleton(commModule)
            .AddSingleton(tagsModule)
            .AddSingleton(tagsModule.TagManager)
            .AddSingleton(screenManager)
            .AddSingleton(eventManager)
            .AddSingleton(animationManager)
            .AddSingleton(historianQueryHelper)
            .AddSingleton(scriptModule)
            .AddSingleton(consoleModule)
            .AddSingleton(projectModule)
            .AddSingleton(transferServer)
            .AddTransient<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();

        var vm = provider.GetRequiredService<MainWindowViewModel>();
        projectModule.ProjectLoaded += (_, _) =>
        {
            vm.ResolutionWidth = projectModule.ResolutionWidth;
            vm.ResolutionHeight = projectModule.ResolutionHeight;
        };
        transferServer.TransferStarted += (_, _) => vm.ShowTransfer(0, "Transferring...");
        transferServer.TransferProgress += (_, p) => vm.ShowTransfer(p.percent, p.status);
        // Plan 2 §13: After delay (e.g. 3s as in Qt main.cpp), OpenProject, ReinitializeWithProject, initialize EventManager/AnimationManager with events.json/animations.json
        transferServer.TransferCompleted += (_, path) =>
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000).ConfigureAwait(false);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    vm.HideTransfer();
                    projectModule.OpenProject(path);
                    engine.ReinitializeWithProject(path);
                    eventManager.LoadFromConfig(engine.GetConfig("Events"));
                    animationManager.LoadFromConfig(engine.GetConfig("Animations"));
                });
            });
        };
        transferServer.ErrorOccurred += (_, msg) => consoleModule.LogError(msg, "Transfer");
        screenManager.ContentChanged += (_, control) => vm.Content = control;
        eventManager.NavigateRequested += (_, screen) => screenManager.LoadScreen(screen);

        // Plan 2 §13: Preloaded project — open and ReinitializeWithProject then StartAll; load events/animations
        if (Directory.Exists(dataPath) && File.Exists(Path.Combine(dataPath, "metadata.iscr")))
        {
            projectModule.OpenProject(dataPath);
            engine.ReinitializeWithProject(dataPath);
            eventManager.LoadFromConfig(engine.GetConfig("Events"));
            animationManager.LoadFromConfig(engine.GetConfig("Animations"));
        }
        else
        {
            engine.StartAll();
        }
        transferServer.Start();
        discoveryResponder.Start();

        // Pass context to App so MainWindow is created in OnFrameworkInitializationCompleted (after IWindowingPlatform is ready)
        StartupContext.MainWindowViewModel = vm;
        StartupContext.OnMainWindowCreated = () =>
        {
            var statusTimer = new System.Timers.Timer(1000);
            statusTimer.Elapsed += (_, _) =>
            {
                var statuses = commModule.GetConnectionStatuses();
                Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.UpdateConnectionStatuses(statuses));
            };
            statusTimer.Start();
        };
        StartupContext.OnMainWindowClosed = () =>
        {
            transferServer.Stop();
            discoveryResponder.Stop();
            engine.Shutdown();
        };

        var app = BuildAvaloniaApp();
        return app.StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
