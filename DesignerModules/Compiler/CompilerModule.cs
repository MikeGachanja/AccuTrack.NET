using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Designer.Modules.Project;
using Designer.Modules.Console;
using Designer.Modules.TagEngine;
using Designer.Modules.ScriptEditor;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Compiler;

/// <summary>
/// Compiler module for building SCADA projects.
/// Produces JSON configs and metadata.iscr for the Avalonia Runtime (no QML).
/// </summary>
public class CompilerModule
{
    private ScadaProject? _currentProject;
    private ConsoleModule? _consoleModule;

    public event EventHandler<string>? Message;
    public event EventHandler<string>? Error;
    public event EventHandler<string>? Warning;

    public void SetConsoleModule(ConsoleModule consoleModule)
    {
        _consoleModule = consoleModule;
    }

    public void SetProject(ScadaProject project)
    {
        _currentProject = project;
    }

    public bool CompileProject()
    {
        if (_currentProject == null)
        {
            EmitError("No project set for compilation.");
            return false;
        }

        EmitMessage($"Starting compilation of SCADA project: {_currentProject.Name}");

        try
        {
            if (!ValidateProject())
                return false;

            string buildPath = _currentProject.Paths.BuildPath;
            string jsonPath = Path.Combine(buildPath, "json");
            string screensOutPath = Path.Combine(buildPath, "screens");

            if (Directory.Exists(buildPath))
            {
                try { Directory.Delete(buildPath, true); } catch { }
            }

            Directory.CreateDirectory(buildPath);
            Directory.CreateDirectory(jsonPath);
            Directory.CreateDirectory(screensOutPath);
            EmitMessage("Build directory created.");

            if (!GenerateCommunicationsConfig(jsonPath))
            {
                EmitWarning("Communications config skipped or failed.");
            }
            else
            {
                EmitMessage("Generated json/communications.json");
            }

            if (!GenerateScreensConfig(jsonPath, screensOutPath))
            {
                EmitWarning("Screens config skipped or failed.");
            }
            else
            {
                EmitMessage("Generated json/screens.json and screens/*.json");
            }

            if (!GenerateTagsConfig(jsonPath))
            {
                EmitWarning("Tags config skipped or failed.");
            }
            else
            {
                EmitMessage("Generated json/tag_tables.json and json/tags.json");
            }

            if (!GenerateScriptsConfig(jsonPath))
            {
                EmitWarning("Scripts config skipped or failed.");
            }
            else
            {
                EmitMessage("Generated json/scripts.json");
            }

            GenerateMinimalConfigs(jsonPath);

            if (!GenerateProjectMetadata(buildPath))
            {
                EmitError("Failed to generate metadata.iscr");
                return false;
            }
            EmitMessage("Generated metadata.iscr");

            EmitMessage("Compilation completed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            EmitError($"Compilation failed: {ex.Message}");
            return false;
        }
    }

    public bool CleanProject()
    {
        if (_currentProject == null)
        {
            EmitError("No project set for cleaning.");
            return false;
        }

        try
        {
            string buildPath = _currentProject.Paths.BuildPath;
            if (Directory.Exists(buildPath))
            {
                Directory.Delete(buildPath, true);
                Directory.CreateDirectory(buildPath);
                EmitMessage("Build directory cleaned.");
                return true;
            }

            EmitMessage("No build directory to clean.");
            return true;
        }
        catch (Exception ex)
        {
            EmitError($"Clean failed: {ex.Message}");
            return false;
        }
    }

    private bool ValidateProject()
    {
        if (_currentProject == null) return false;

        if (!Directory.Exists(_currentProject.Path))
        {
            EmitError($"Project directory does not exist: {_currentProject.Path}");
            return false;
        }

        if (!Directory.Exists(_currentProject.Paths.ScreensPath))
        {
            EmitWarning("Screens directory missing; creating empty screens config.");
        }

        return true;
    }

    private bool GenerateCommunicationsConfig(string jsonPath)
    {
        if (_currentProject == null) return false;

        string commFile = Path.Combine(_currentProject.Paths.CommunicationsPath, "communication_modules.json");
        if (!File.Exists(commFile))
        {
            var empty = new JObject { ["communication_modules"] = new JArray() };
            File.WriteAllText(Path.Combine(jsonPath, "communications.json"), empty.ToString());
            return true;
        }

        try
        {
            var content = JObject.Parse(File.ReadAllText(commFile));
            var root = new JObject();
            if (content["modules"] is JArray modules)
                root["communication_modules"] = modules;
            else if (content["communication_modules"] is JArray cm)
                root["communication_modules"] = cm;
            else
                root["communication_modules"] = new JArray();

            File.WriteAllText(Path.Combine(jsonPath, "communications.json"), root.ToString());
            return true;
        }
        catch (Exception ex)
        {
            EmitError($"Communications config: {ex.Message}");
            return false;
        }
    }

    private bool GenerateScreensConfig(string jsonPath, string screensOutPath)
    {
        if (_currentProject == null) return false;

        string screensSrcPath = _currentProject.Paths.ScreensPath;
        if (!Directory.Exists(screensSrcPath))
        {
            var empty = new JObject { ["screens"] = new JArray() };
            File.WriteAllText(Path.Combine(jsonPath, "screens.json"), empty.ToString());
            return true;
        }

        var screensArray = new JArray();
        foreach (var file in Directory.GetFiles(screensSrcPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var screenJson = JObject.Parse(File.ReadAllText(file));
                string id = screenJson["id"]?.ToString() ?? Path.GetFileNameWithoutExtension(file);
                string name = screenJson["name"]?.ToString() ?? id;

                screensArray.Add(new JObject
                {
                    ["id"] = id,
                    ["name"] = name
                });

                string destPath = Path.Combine(screensOutPath, id + ".json");
                File.WriteAllText(destPath, screenJson.ToString());
            }
            catch (Exception ex)
            {
                EmitWarning($"Screen file {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var root = new JObject { ["screens"] = screensArray };
        File.WriteAllText(Path.Combine(jsonPath, "screens.json"), root.ToString());
        return true;
    }

    private bool GenerateTagsConfig(string jsonPath)
    {
        if (_currentProject == null) return false;

        string tagsPath = _currentProject.Paths.TagsPath;
        var allTags = new JArray();

        if (Directory.Exists(tagsPath))
        {
            foreach (var file in Directory.GetFiles(tagsPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var table = new TagTable(Path.GetFileNameWithoutExtension(file));
                    if (table.LoadFromFile(file))
                    {
                        foreach (var tag in table.GetTags())
                        {
                            allTags.Add(new JObject
                            {
                                ["name"] = tag.Name,
                                ["address"] = tag.Address ?? "",
                                ["description"] = tag.Description ?? "",
                                ["dataType"] = tag.DataType ?? "Float",
                                ["unit"] = tag.Unit ?? ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    EmitWarning($"Tag table {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        var tagTablesRoot = new JObject { ["tags"] = allTags };
        File.WriteAllText(Path.Combine(jsonPath, "tag_tables.json"), tagTablesRoot.ToString());
        File.WriteAllText(Path.Combine(jsonPath, "tags.json"), tagTablesRoot.ToString());
        return true;
    }

    private bool GenerateScriptsConfig(string jsonPath)
    {
        if (_currentProject == null) return false;

        string scriptsPath = _currentProject.Paths.ScriptsPath;
        var scriptsArray = new JArray();

        if (Directory.Exists(scriptsPath))
        {
            foreach (var file in Directory.GetFiles(scriptsPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var json = JObject.Parse(File.ReadAllText(file));
                    var script = LuaScript.FromJson(json);
                    script.FilePath = json["filePath"]?.ToString() ?? file.Replace(".json", ".lua");
                    if (File.Exists(script.FilePath))
                        script.LoadCode();

                    scriptsArray.Add(new JObject
                    {
                        ["id"] = script.Id.ToString(),
                        ["name"] = script.Name ?? "",
                        ["description"] = script.Description ?? "",
                        ["enabled"] = true,
                        ["code"] = script.Code ?? "",
                        ["path"] = script.FilePath ?? ""
                    });
                }
                catch (Exception ex)
                {
                    EmitWarning($"Script {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        var root = new JObject { ["scripts"] = scriptsArray };
        File.WriteAllText(Path.Combine(jsonPath, "scripts.json"), root.ToString());
        return true;
    }

    private void GenerateMinimalConfigs(string jsonPath)
    {
        if (_currentProject == null) return;

        var alarmsPath = Path.Combine(jsonPath, "alarms.json");
        if (!File.Exists(alarmsPath))
            File.WriteAllText(alarmsPath, new JObject { ["alarms"] = new JArray() }.ToString());

        var schedulesPath = Path.Combine(jsonPath, "schedules.json");
        if (!File.Exists(schedulesPath))
            File.WriteAllText(schedulesPath, new JObject { ["schedules"] = new JArray() }.ToString());

        var historianPath = Path.Combine(jsonPath, "historian.json");
        if (!File.Exists(historianPath))
            File.WriteAllText(historianPath, new JObject { ["name"] = "", ["type"] = "" }.ToString());

        var securityPath = Path.Combine(jsonPath, "security.json");
        if (!File.Exists(securityPath))
            File.WriteAllText(securityPath, new JObject { ["users"] = new JArray(), ["groups"] = new JArray(), ["roles"] = new JArray() }.ToString());

        var eventsPath = Path.Combine(jsonPath, "events.json");
        if (!File.Exists(eventsPath))
            File.WriteAllText(eventsPath, new JObject { ["events"] = new JArray() }.ToString());
    }

    private bool GenerateProjectMetadata(string buildPath)
    {
        if (_currentProject == null) return false;

        var root = new JObject
        {
            ["name"] = _currentProject.Name,
            ["type"] = (int)_currentProject.Type,
            ["version"] = _currentProject.Version ?? "1.0.0",
            ["resolution"] = new JObject
            {
                ["width"] = _currentProject.Resolution.Width,
                ["height"] = _currentProject.Resolution.Height
            },
            ["configFiles"] = new JObject
            {
                ["communication"] = "json/communications.json",
                ["historian"] = "json/historian.json",
                ["alarms"] = "json/alarms.json",
                ["schedules"] = "json/schedules.json",
                ["screens"] = "json/screens.json",
                ["scripts"] = "json/scripts.json",
                ["security"] = "json/security.json",
                ["tags"] = "json/tag_tables.json",
                ["events"] = "json/events.json"
            }
        };

        string metadataPath = Path.Combine(buildPath, "metadata.iscr");
        File.WriteAllText(metadataPath, root.ToString());
        return true;
    }

    private void EmitMessage(string message)
    {
        Message?.Invoke(this, message);
        _consoleModule?.HandleCompilerMessage(message);
    }

    private void EmitError(string error)
    {
        Error?.Invoke(this, error);
        _consoleModule?.HandleCompilerError(error);
    }

    private void EmitWarning(string warning)
    {
        Warning?.Invoke(this, warning);
        _consoleModule?.HandleCompilerWarning(warning);
    }
}
