namespace AccuTrack.Compiler;

/// <summary>
/// Generates Avalonia AXAML (and code-behind if needed) from screen model.
/// Replaces Qt GenerateQML; output is AXAML for Runtime (Plan 2).
/// </summary>
public static class AvaloniaScreenGenerator
{
    public static string GenerateScreen(string screenName, int width, int height, object[] components)
    {
        return $@"<UserControl xmlns=""https://github.com/avaloniaui""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             x:Class=""AccuTrack.Runtime.Screens.{screenName}View""
             Width=""{width}"" Height=""{height}"">
  <TextBlock Text=""{screenName}"" FontSize=""24"" Margin=""20""/>
</UserControl>";
    }
}
