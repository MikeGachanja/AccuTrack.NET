namespace AccuTrack.Runtime;

/// <summary>
/// Holder for startup context passed from Program to App so MainWindow is created after IWindowingPlatform is ready.
/// </summary>
internal static class StartupContext
{
    public static object? MainWindowViewModel { get; set; }
    public static Action? OnMainWindowCreated { get; set; }
    public static Action? OnMainWindowClosed { get; set; }
}
