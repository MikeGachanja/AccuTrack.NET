using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Runtime.Modules.Alarms;
using Runtime.Modules.Communication;
using Runtime.Modules.Console;
using Runtime.Modules.Discovery;
using Runtime.Modules.EventDispatcher;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Historian;
using Runtime.Modules.MLEngine;
using Runtime.Modules.Project;
using Runtime.Modules.Scheduler;
using Runtime.Modules.ScriptingEngine;
using Runtime.Modules.Security;
using Runtime.Modules.Screens;
using Runtime.Modules.TagsEngine;

namespace Runtime;

internal static class Program
{
    private static readonly List<IDisposable> _animationTagSubscriptions = new();

    /// <summary>Subscribe AnimationManager to all tag value changes so it can drive visibility/color/flashing/translation.</summary>
    private static void WireAnimationManagerToTagManager(ScreensModule screensModule, TagsModule tagsModule)
    {
        foreach (var sub in _animationTagSubscriptions)
            sub.Dispose();
        _animationTagSubscriptions.Clear();
        var tagManager = tagsModule.TagManager;
        var animationManager = screensModule.AnimationManager;
        foreach (var tagName in tagManager.GetTagNames())
        {
            var sub = tagManager.Subscribe(tagName, (name, value, _) => animationManager.OnTagValueChanged(name, value));
            _animationTagSubscriptions.Add(sub);
        }
    }
    [STAThread]
    public static int Main(string[] args)
    {
        var engine = ExecutionEngine.Instance;
        engine.Initialize();

        var eventDispatcher = EventDispatcher.Instance;
        engine.ModuleManager.RegisterModule(eventDispatcher);

        var console = new ConsoleModule();
        engine.ModuleManager.RegisterModule(console);

        engine.ModuleManager.RegisterModule(new SecurityModule());
        engine.ModuleManager.RegisterModule(new MLEngineModule());

        var commModule = new CommunicationModule();
        engine.ModuleManager.RegisterModule(commModule);
        Services.Register<ICommunication>(commModule);

        var tagsModule = new TagsModule();
        engine.ModuleManager.RegisterModule(tagsModule);
        tagsModule.SetCommunicationModule(commModule);
        commModule.SetTagUpdateCallback((name, val) => tagsModule.TagManager.UpdateTagValue(name, val, TagQuality.Good));

        engine.ModuleManager.RegisterModule(new AlarmsModule());

        var scriptingEngine = new ScriptingEngineModule();
        scriptingEngine.Initialize();

        var schedulerModule = new SchedulerModule();
        engine.ModuleManager.RegisterModule(schedulerModule);
        schedulerModule.SetScriptEngine(scriptingEngine);

        engine.ModuleManager.RegisterModule(new HistorianModule());

        var project = new ProjectModule();
        Services.Register<IProject>(project);

        var screensModule = new ScreensModule();
        screensModule.Initialize();
        screensModule.WireTagIOHandler(tagsModule.TagIOHandler);
        screensModule.WireTagManager(tagsModule.TagManager);
        screensModule.WireScriptManager(scriptingEngine);
        if (engine.ModuleManager.GetModule("HistorianModule") is IHistorian historianModule)
            screensModule.WireHistorianManager(historianModule.HistorianManager);
        Services.Register<IScreens>(screensModule);

        project.ScreenAvailable += (_, path) =>
        {
            if (!string.IsNullOrEmpty(path))
                screensModule.ScreenManager.LoadScreen(path);
        };

        var discovery = new DiscoveryService();
        Services.Register(discovery.TransferServer);
        if (!discovery.TransferServer.Start(8888))
            System.Diagnostics.Debug.WriteLine("Transfer server failed to start");
        if (!discovery.DiscoveryResponder.Start())
            System.Diagnostics.Debug.WriteLine("Discovery responder failed to start");

        discovery.TransferServer.TransferCompleted += (_, projectPath) =>
        {
            Task.Delay(3000).ContinueWith(_ =>
            {
                // Unload previous project: stop all modules first so they release old state
                engine.StopAll();
                var metadataPath = Path.Combine(projectPath, "metadata.iscr");
                if (project.OpenProject(metadataPath))
                {
                    engine.ReinitializeWithProject(projectPath);
                    screensModule.InitializeWithProject(projectPath);
                    WireAnimationManagerToTagManager(screensModule, tagsModule);
                    engine.StartAll();
                }
            });
        };

        var appDir = AppContext.BaseDirectory;
        var dataPath = Path.Combine(appDir, "data");
        var metadataPath = Path.Combine(dataPath, "metadata.iscr");
        if (File.Exists(metadataPath))
        {
            if (project.OpenProject(metadataPath))
            {
                engine.ReinitializeWithProject(dataPath);
                screensModule.InitializeWithProject(dataPath);
                WireAnimationManagerToTagManager(screensModule, tagsModule);
            }
        }

        engine.StartAll();

        return BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
