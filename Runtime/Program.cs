using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    /// <summary>Set up TagProvider function for CommunicationModule to get tags from TagManager.</summary>
    private static void SetupTagProvider(CommunicationModule commModule, TagsModule tagsModule)
    {
        commModule.SetTagProvider(() =>
        {
            var tags = new List<(string tagName, string address)>();
            foreach (var tagName in tagsModule.TagManager.GetTagNames())
            {
                var tag = tagsModule.TagManager.GetTag(tagName);
                if (tag != null)
                    tags.Add((tagName, tag.Address ?? ""));
            }
            System.Diagnostics.Trace.WriteLine($"[Runtime] TagProvider called: Returning {tags.Count} tags from TagManager (Total tags: {tagsModule.TagManager.TagCount})");
            return tags;
        });
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

        // Route Trace output to the console (visible on the Logs screen). In .NET Core, Debug.Listeners
        // does not exist; the codebase uses Trace.WriteLine so all such messages appear on the Logs screen.
        var consoleListener = new ConsoleTraceListener(console, "Runtime");
        System.Diagnostics.Trace.Listeners.Add(consoleListener);

        engine.ModuleManager.RegisterModule(new SecurityModule());

        var commModule = new CommunicationModule();
        engine.ModuleManager.RegisterModule(commModule);
        Services.Register<ICommunication>(commModule);

        var tagsModule = new TagsModule();
        engine.ModuleManager.RegisterModule(tagsModule);

        // ML Engine and Scripting Engine both use the same TagManager (read/write tags).
        var mlEngineModule = new MLEngineModule();
        engine.ModuleManager.RegisterModule(mlEngineModule);
        mlEngineModule.SetTagManager(tagsModule.TagManager);

        tagsModule.SetCommunicationModule(commModule);
        
        // Set up tag update callback that handles both tag name and address resolution
        // Since tag name == tag address, either can work for resolution
        commModule.SetTagUpdateCallback((tagNameOrAddress, value) =>
        {
            System.Diagnostics.Trace.WriteLine($"[Runtime] Tag update callback received - TagName/Address: '{tagNameOrAddress}', Value: '{value}' (Type: {value?.GetType().Name ?? "null"})");
            
            // Try updating by tag name first (tag name == tag address, so this should work)
            if (tagsModule.TagManager.UpdateTagValue(tagNameOrAddress, value, TagQuality.Good))
            {
                System.Diagnostics.Trace.WriteLine($"[Runtime] Successfully updated tag '{tagNameOrAddress}' by name");
                return;
            }
            
            // If tag name update failed, try updating by address (tag name == tag address)
            if (tagsModule.TagManager.UpdateTagValueByAddress(tagNameOrAddress, value, TagQuality.Good))
            {
                System.Diagnostics.Trace.WriteLine($"[Runtime] Successfully updated tag '{tagNameOrAddress}' by address");
                return;
            }
            
            // Try to find tag by checking if tagNameOrAddress matches any tag's address
            var tag = tagsModule.TagManager.GetTag(tagNameOrAddress);
            if (tag != null)
            {
                // Found by name, try updating again (should have worked above, but try once more)
                if (tagsModule.TagManager.UpdateTagValue(tagNameOrAddress, value, TagQuality.Good))
                {
                    System.Diagnostics.Trace.WriteLine($"[Runtime] Successfully updated tag '{tagNameOrAddress}' after finding by name");
                    return;
                }
            }
            
            // Try finding by address
            tag = tagsModule.TagManager.GetTagByAddress(tagNameOrAddress);
            if (tag != null)
            {
                if (tagsModule.TagManager.UpdateTagValue(tag.Name, value, TagQuality.Good))
                {
                    System.Diagnostics.Trace.WriteLine($"[Runtime] Successfully updated tag '{tag.Name}' after finding by address '{tagNameOrAddress}'");
                    return;
                }
            }
            
            // If all attempts failed, log warning
            System.Diagnostics.Trace.WriteLine($"[Runtime] WARNING - Failed to update tag '{tagNameOrAddress}' - tag not found in TagManager (Total tags: {tagsModule.TagManager.TagCount})");
            
            // Log available tag names for debugging
            var availableTags = tagsModule.TagManager.GetTagNames();
            if (availableTags.Count > 0)
            {
                System.Diagnostics.Trace.WriteLine($"[Runtime] Available tags: {string.Join(", ", availableTags.Take(10))}");
            }
        });
        
        SetupTagProvider(commModule, tagsModule);

        engine.ModuleManager.RegisterModule(new AlarmsModule());

        var scriptingEngine = new ScriptingEngineModule();
        scriptingEngine.Initialize();
        if (engine.ModuleManager.GetModule("ConsoleModule") is IConsole consoleForLua)
            scriptingEngine.SetPrintCallback(msg => consoleForLua.LogDebug(msg, "Lua"));
        // Scripting engine: Lua read_tag(name) / write_tag(name, value) use TagManager.
        scriptingEngine.SetTagAccess(
            name => tagsModule.TagManager.GetTagValue(name),
            (name, value) => tagsModule.TagManager.UpdateTagValue(name, value, TagQuality.Good));

        // Scheduler: runs schedules from json/schedules.json (scripts + ML models on interval/time).
        var schedulerModule = new SchedulerModule();
        engine.ModuleManager.RegisterModule(schedulerModule);
        schedulerModule.SetScriptEngine(scriptingEngine);
        schedulerModule.SetMLRunAction(mlEngineModule.RunModelOnce);

        engine.ModuleManager.RegisterModule(new HistorianModule());

        var project = new ProjectModule();
        Services.Register<IProject>(project);
        schedulerModule.SetScriptPathResolver(scriptName =>
        {
            var p = Services.Get<IProject>();
            var script = p?.CurrentProject?.Script?.FirstOrDefault(s =>
                s.Enabled && string.Equals(s.Name, scriptName, StringComparison.Ordinal));
            if (script == null || string.IsNullOrEmpty(script.Path)) return null;
            
            // Script.Path is relative to data folder (e.g., "scripts/{id}.lua")
            // Resolve to full path: {projectPath}/data/scripts/{id}.lua
            var projectPath = ExecutionEngine.Instance.ProjectPath;
            if (string.IsNullOrEmpty(projectPath))
            {
                // Fallback: use data folder relative to executable
                var appDir = AppContext.BaseDirectory;
                return Path.Combine(appDir, "data", script.Path);
            }
            return Path.Combine(projectPath, "data", script.Path);
        });

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
            System.Diagnostics.Trace.WriteLine("Transfer server failed to start");
        if (!discovery.DiscoveryResponder.Start())
            System.Diagnostics.Trace.WriteLine("Discovery responder failed to start");

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
                    // Re-set TagProvider after reinitialization since connections may have been recreated
                    SetupTagProvider(commModule, tagsModule);
                    engine.StartAll();
                }
            });
        };

        // Load existing project data automatically on startup
        var appDir = AppContext.BaseDirectory;
        var dataPath = Path.Combine(appDir, "data");
        var metadataPath = Path.Combine(dataPath, "metadata.iscr");
        bool projectLoaded = false;
        
        System.Diagnostics.Trace.WriteLine($"[Runtime] Checking for project data in: {dataPath}");
        System.Diagnostics.Trace.WriteLine($"[Runtime] App directory: {appDir}");
        System.Diagnostics.Trace.WriteLine($"[Runtime] Data directory exists: {Directory.Exists(dataPath)}");
        System.Diagnostics.Trace.WriteLine($"[Runtime] Metadata file exists: {File.Exists(metadataPath)}");
        
        if (File.Exists(metadataPath))
        {
            System.Diagnostics.Trace.WriteLine($"[Runtime] Attempting to load project from: {metadataPath}");
            if (project.OpenProject(metadataPath))
            {
                engine.ReinitializeWithProject(dataPath);
                screensModule.InitializeWithProject(dataPath);
                WireAnimationManagerToTagManager(screensModule, tagsModule);
                // Re-set TagProvider after reinitialization since connections may have been recreated
                SetupTagProvider(commModule, tagsModule);
                projectLoaded = true;
                var projectName = project.CurrentProject?.Name ?? "Unknown";
                var screenCount = project.CurrentProject?.Screen.Count ?? 0;
                System.Diagnostics.Trace.WriteLine($"[Runtime] Successfully loaded project: {projectName} with {screenCount} screen(s)");
                System.Diagnostics.Trace.WriteLine($"[Runtime] Tags loaded: {tagsModule.TagManager.TagCount} tags");
                if (screenCount > 0)
                {
                    var firstScreen = project.GetFirstScreenPath();
                    System.Diagnostics.Trace.WriteLine($"[Runtime] First screen path: {firstScreen}");
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[Runtime] Failed to load existing project data (OpenProject returned false).");
            }
        }
        else
        {
            System.Diagnostics.Trace.WriteLine($"[Runtime] No existing project data found at {metadataPath}. Runtime will start without a project.");
            if (Directory.Exists(dataPath))
            {
                var files = Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories);
                System.Diagnostics.Trace.WriteLine($"[Runtime] Found {files.Length} file(s) in data directory:");
                foreach (var file in files.Take(10)) // Log first 10 files
                {
                    System.Diagnostics.Trace.WriteLine($"  - {file}");
                }
            }
        }

        // Start all modules (they can run with or without a project)
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
