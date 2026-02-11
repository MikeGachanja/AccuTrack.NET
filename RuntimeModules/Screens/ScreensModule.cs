using System.IO;

namespace Runtime.Modules.Screens;

/// <summary>Screens module: ScreenManager, EventManager, AnimationManager, HistorianQueryHelper. Not IModuleInterface.</summary>
public sealed class ScreensModule : IScreens
{
    public ScreenManager ScreenManager { get; } = new();
    public EventManager EventManager { get; } = new();
    public AnimationManager AnimationManager { get; } = new();
    public HistorianQueryHelper HistorianQueryHelper { get; } = new();

    public void Initialize()
    {
        EventManager.SetScreenManager(ScreenManager);
    }

    /// <summary>Initialize EventManager with project path (events.json and screen resolution). Call after project load.</summary>
    public void InitializeWithProject(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath)) return;
        var eventsPath = Path.Combine(projectPath, "json", "events.json");
        EventManager.Initialize(eventsPath, projectPath);
        ScreenManager.SetProjectPath(projectPath);
    }

    public void WireTagIOHandler(object? tagIOHandler)
    {
        EventManager.SetTagIOHandler(tagIOHandler);
    }

    public void WireTagManager(object? tagManager)
    {
        EventManager.SetTagManager(tagManager);
    }

    public void WireScriptManager(object? scriptManager)
    {
        EventManager.SetScriptManager(scriptManager);
    }

    public void WireHistorianManager(object? historianManager)
    {
        HistorianQueryHelper.SetHistorianManager(historianManager);
    }
}
