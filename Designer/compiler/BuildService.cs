using System.Text.Json;
using AccuTrack.Project;

namespace AccuTrack.Compiler;

/// <summary>
/// Build orchestration: collect screens, tags, scripts, communication, alarms, etc.;
/// validate; run JSON generation and Avalonia (AXAML) generation.
/// </summary>
public class BuildService
{
    public string OutputPath { get; set; } = "bin";

    public bool Build(ScadaProject project, Action<string>? log = null)
    {
        log?.Invoke("Collecting project assets...");
        try
        {
            GenerateJson(project, log);
            GenerateAvaloniaScreens(project, log);
            log?.Invoke("Build finished.");
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Build failed: {ex.Message}");
            return false;
        }
    }

    private void GenerateJson(ScadaProject project, Action<string>? log)
    {
        log?.Invoke("Generating JSON (tags, communication, alarms, schedules, historian, events)...");
        var dir = Path.Combine(project.ProjectPath, OutputPath);
        Directory.CreateDirectory(dir);
        // Stub: emit empty/minimal JSON so Runtime can load same format
        var tagsPath = Path.Combine(dir, "tags.json");
        File.WriteAllText(tagsPath, JsonSerializer.Serialize(new { tags = Array.Empty<object>() }));
        var commPath = Path.Combine(dir, "communication.json");
        File.WriteAllText(commPath, JsonSerializer.Serialize(new { modules = Array.Empty<object>() }));
        log?.Invoke($"JSON written to {dir}");
    }

    private void GenerateAvaloniaScreens(ScadaProject project, Action<string>? log)
    {
        log?.Invoke("Generating Avalonia (AXAML) screens...");
        var dir = Path.Combine(project.ProjectPath, OutputPath, "Screens");
        Directory.CreateDirectory(dir);
        foreach (var screen in project.Screens)
        {
            var axaml = AvaloniaScreenGenerator.GenerateScreen(screen.Name, 800, 600, Array.Empty<object>());
            var path = Path.Combine(dir, $"{screen.Name}.axaml");
            File.WriteAllText(path, axaml);
        }
        log?.Invoke($"AXAML written to {dir}");
    }
}
