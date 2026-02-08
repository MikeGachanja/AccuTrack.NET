namespace AccuTrack.Runtime.EventDispatch;

/// <summary>
/// Well-known event types for runtime publish/subscribe.
/// </summary>
public static class EventTypes
{
    public const string TagValueChanged = "TagValueChanged";
    public const string AlarmRaised = "AlarmRaised";
    public const string AlarmCleared = "AlarmCleared";
    public const string ScreenLoaded = "ScreenLoaded";
    public const string ScreenUnloaded = "ScreenUnloaded";
    public const string ProjectLoaded = "ProjectLoaded";
    public const string TransferStarted = "TransferStarted";
    public const string TransferProgress = "TransferProgress";
    public const string TransferCompleted = "TransferCompleted";
    public const string ModuleStatusChanged = "ModuleStatusChanged";
    public const string Error = "Error";
}
