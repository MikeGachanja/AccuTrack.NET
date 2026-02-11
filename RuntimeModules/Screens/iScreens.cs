namespace Runtime.Modules.Screens;

/// <summary>Screens module interface. Provides screen loading, events, animations, and historian queries for the runtime UI.</summary>
public interface IScreens
{
    /// <summary>Manages current screen path and load/unload; host sets content via callback.</summary>
    ScreenManager ScreenManager { get; }
    /// <summary>Loads events.json and executes actions (NavigateScreen, WriteTag, RunScript, etc.).</summary>
    EventManager EventManager { get; }
    /// <summary>Screen animations (stub).</summary>
    AnimationManager AnimationManager { get; }
    /// <summary>Queries tag history for TrendView and other consumers.</summary>
    HistorianQueryHelper HistorianQueryHelper { get; }
}
