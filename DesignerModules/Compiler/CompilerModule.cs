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

            CopyMachineLearningConfig(jsonPath, buildPath);

            // Copy SVG files used in screens to runtime build output
            CopySvgFiles(buildPath);

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

    /// <summary>
    /// Copies SVG files referenced in screens to the runtime build output.
    /// </summary>
    private void CopySvgFiles(string buildPath)
    {
        if (_currentProject == null) return;

        try
        {
            // Get source SVG directory (executable directory/svg/)
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            string sourceSvgPath = Path.Combine(exeDir, "svg");
            
            // Create destination SVG directory in build output
            string destSvgPath = Path.Combine(buildPath, "svg");
            if (!Directory.Exists(destSvgPath))
            {
                Directory.CreateDirectory(destSvgPath);
            }

            // Collect all SVG paths from screen JSON files
            var svgPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string screensSrcPath = _currentProject.Paths.ScreensPath;
            
            if (Directory.Exists(screensSrcPath))
            {
                foreach (var file in Directory.GetFiles(screensSrcPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var screenJson = JObject.Parse(File.ReadAllText(file));
                        
                        // Extract SVG paths from components
                        if (screenJson["components"] is JArray components)
                        {
                            foreach (var comp in components)
                            {
                                if (comp is JObject compObj && compObj["componentType"]?.ToString() == "SVGView")
                                {
                                    string? svgPath = compObj["svgPath"]?.ToString();
                                    if (!string.IsNullOrEmpty(svgPath))
                                    {
                                        svgPaths.Add(svgPath);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        EmitWarning($"Error reading screen file {Path.GetFileName(file)} for SVG paths: {ex.Message}");
                    }
                }
            }

            // Copy each referenced SVG file
            int copiedCount = 0;
            foreach (var svgPath in svgPaths)
            {
                try
                {
                    // Get full source path
                    string fullSourcePath = Path.IsPathRooted(svgPath) 
                        ? svgPath 
                        : Path.Combine(sourceSvgPath, svgPath);
                    
                    if (File.Exists(fullSourcePath))
                    {
                        // Determine destination path (preserve relative directory structure)
                        string destFilePath;
                        if (Path.IsPathRooted(svgPath))
                        {
                            // If absolute, try to extract relative part or use filename
                            string fileName = Path.GetFileName(svgPath);
                            destFilePath = Path.Combine(destSvgPath, fileName);
                        }
                        else
                        {
                            // Preserve relative directory structure
                            destFilePath = Path.Combine(destSvgPath, svgPath);
                        }
                        
                        // Ensure destination directory exists
                        string? destDir = Path.GetDirectoryName(destFilePath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        
                        // Copy file
                        File.Copy(fullSourcePath, destFilePath, true);
                        copiedCount++;
                    }
                    else
                    {
                        EmitWarning($"SVG file not found: {fullSourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    EmitWarning($"Failed to copy SVG file '{svgPath}': {ex.Message}");
                }
            }

            if (copiedCount > 0)
            {
                EmitMessage($"Copied {copiedCount} SVG file(s) to runtime build output");
            }
        }
        catch (Exception ex)
        {
            EmitWarning($"Error copying SVG files: {ex.Message}");
        }
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
                        ["enabled"] = script.Enabled,
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

        // Construct source json path from RootPath
        var sourceJsonPath = Path.Combine(_currentProject.Paths.RootPath, "json");
        
        // Copy or create alarms.json
        var sourceAlarmsPath = Path.Combine(sourceJsonPath, "alarms.json");
        var buildAlarmsPath = Path.Combine(jsonPath, "alarms.json");
        if (File.Exists(sourceAlarmsPath))
        {
            try
            {
                File.Copy(sourceAlarmsPath, buildAlarmsPath, true);
                EmitMessage("Copied json/alarms.json from source project");
            }
            catch (Exception ex)
            {
                EmitWarning($"Failed to copy alarms.json: {ex.Message}");
                File.WriteAllText(buildAlarmsPath, new JObject { ["alarms"] = new JArray() }.ToString());
            }
        }
        else if (!File.Exists(buildAlarmsPath))
        {
            File.WriteAllText(buildAlarmsPath, new JObject { ["alarms"] = new JArray() }.ToString());
        }

        // Copy or create schedules.json
        var sourceSchedulesPath = Path.Combine(sourceJsonPath, "schedules.json");
        var buildSchedulesPath = Path.Combine(jsonPath, "schedules.json");
        if (File.Exists(sourceSchedulesPath))
        {
            try
            {
                File.Copy(sourceSchedulesPath, buildSchedulesPath, true);
                EmitMessage("Copied json/schedules.json from source project");
            }
            catch (Exception ex)
            {
                EmitWarning($"Failed to copy schedules.json: {ex.Message}");
                File.WriteAllText(buildSchedulesPath, new JObject { ["schedules"] = new JArray() }.ToString());
            }
        }
        else if (!File.Exists(buildSchedulesPath))
        {
            File.WriteAllText(buildSchedulesPath, new JObject { ["schedules"] = new JArray() }.ToString());
        }

        // Copy and transform historian.json
        var sourceHistorianPath = Path.Combine(sourceJsonPath, "historian.json");
        var buildHistorianPath = Path.Combine(jsonPath, "historian.json");
        if (File.Exists(sourceHistorianPath))
        {
            try
            {
                // Read and transform historian config
                var historianJson = JObject.Parse(File.ReadAllText(sourceHistorianPath));
                var transformedHistorian = TransformHistorianConfig(historianJson);
                
                File.WriteAllText(buildHistorianPath, transformedHistorian.ToString(Newtonsoft.Json.Formatting.Indented));
                EmitMessage("Processed json/historian.json from source project");
            }
            catch (Exception ex)
            {
                EmitWarning($"Failed to process historian.json: {ex.Message}");
                // Create default historian.json as fallback
                File.WriteAllText(buildHistorianPath, new JObject 
                { 
                    ["tags"] = new JArray(), 
                    ["loggingIntervalSeconds"] = 60, 
                    ["databaseType"] = "SQLite", 
                    ["maxStorageSizeMB"] = 1000,
                    ["retentionPolicy"] = "Days",
                    ["retentionDays"] = 30,
                    ["enabled"] = true 
                }.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
        else if (!File.Exists(buildHistorianPath))
        {
            // Create empty historian.json with proper structure
            File.WriteAllText(buildHistorianPath, new JObject 
            { 
                ["tags"] = new JArray(), 
                ["loggingIntervalSeconds"] = 60, 
                ["databaseType"] = "SQLite", 
                ["maxStorageSizeMB"] = 1000,
                ["retentionPolicy"] = "Days",
                ["retentionDays"] = 30,
                ["enabled"] = true 
            }.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        // Copy or create security.json
        var sourceSecurityPath = Path.Combine(sourceJsonPath, "security.json");
        var buildSecurityPath = Path.Combine(jsonPath, "security.json");
        if (File.Exists(sourceSecurityPath))
        {
            try
            {
                File.Copy(sourceSecurityPath, buildSecurityPath, true);
                EmitMessage("Copied json/security.json from source project");
            }
            catch (Exception ex)
            {
                EmitWarning($"Failed to copy security.json: {ex.Message}");
                File.WriteAllText(buildSecurityPath, new JObject { ["users"] = new JArray(), ["groups"] = new JArray(), ["roles"] = new JArray() }.ToString());
            }
        }
        else if (!File.Exists(buildSecurityPath))
        {
            File.WriteAllText(buildSecurityPath, new JObject { ["users"] = new JArray(), ["groups"] = new JArray(), ["roles"] = new JArray() }.ToString());
        }

        // Copy events.json from source project if it exists, otherwise create empty one
        var sourceEventsPath = Path.Combine(sourceJsonPath, "events.json");
        var buildEventsPath = Path.Combine(jsonPath, "events.json");
        
        if (File.Exists(sourceEventsPath))
        {
            try
            {
                File.Copy(sourceEventsPath, buildEventsPath, true);
                EmitMessage("Copied json/events.json from source project");
            }
            catch (Exception ex)
            {
                EmitWarning($"Failed to copy events.json: {ex.Message}");
                // Create empty events.json as fallback
                File.WriteAllText(buildEventsPath, new JObject { ["events"] = new JArray() }.ToString());
            }
        }
        else if (!File.Exists(buildEventsPath))
        {
            // Create empty events.json if source doesn't exist
            File.WriteAllText(buildEventsPath, new JObject { ["events"] = new JArray() }.ToString());
        }
    }

    private void CopyMachineLearningConfig(string jsonPath, string buildPath)
    {
        if (_currentProject == null) return;

        var mlConfigPath = Path.Combine(_currentProject.Paths.MachineLearningPath, "machine_learning.json");
        var buildMlJsonPath = Path.Combine(jsonPath, "machine_learning.json");
        var mlDataBuildPath = Path.Combine(buildPath, "ml_data");

        if (!File.Exists(mlConfigPath))
        {
            var minimal = new JObject { ["enabled"] = false, ["models"] = new JArray() };
            if (!string.IsNullOrEmpty(_currentProject.Paths.MachineLearningPath))
                minimal["trainingDataPath"] = string.Empty;
            try
            {
                File.WriteAllText(buildMlJsonPath, minimal.ToString());
                EmitMessage("Generated json/machine_learning.json (minimal, no source)");
            }
            catch (Exception ex)
            {
                EmitWarning($"Could not write machine_learning.json: {ex.Message}");
            }
            return;
        }

        try
        {
            var json = JObject.Parse(File.ReadAllText(mlConfigPath));
            var modelsArray = json["models"] as JArray;
            if (modelsArray != null && modelsArray.Count > 0)
            {
                if (!Directory.Exists(mlDataBuildPath))
                    Directory.CreateDirectory(mlDataBuildPath);

                var rootPath = _currentProject.Paths.RootPath;
                for (int i = 0; i < modelsArray.Count; i++)
                {
                    var modelObj = modelsArray[i] as JObject;
                    if (modelObj == null) continue;

                    var trainedDataPath = modelObj["trainedDataPath"]?.ToString();
                    if (string.IsNullOrWhiteSpace(trainedDataPath)) continue;

                    var fullPath = Path.Combine(rootPath, trainedDataPath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fullPath))
                    {
                        EmitWarning($"ML model trained data not found: {trainedDataPath}");
                        continue;
                    }

                    var modelId = modelObj["id"]?.ToString();
                    var ext = Path.GetExtension(fullPath);
                    if (string.IsNullOrEmpty(ext)) ext = ".zip";
                    var stableName = !string.IsNullOrEmpty(modelId)
                        ? $"{modelId}{ext}"
                        : $"model_{i}{ext}";
                    var destPath = Path.Combine(mlDataBuildPath, stableName);
                    File.Copy(fullPath, destPath, true);
                    modelObj["trainedDataPath"] = $"ml_data/{stableName}";
                }
                EmitMessage("Copied ML config and trained data to build");
            }
            else
            {
                EmitMessage("Copied json/machine_learning.json");
            }

            File.WriteAllText(buildMlJsonPath, json.ToString());
        }
        catch (Exception ex)
        {
            EmitWarning($"Failed to copy machine learning config: {ex.Message}");
            try
            {
                var fallback = new JObject { ["enabled"] = false, ["models"] = new JArray() };
                File.WriteAllText(buildMlJsonPath, fallback.ToString());
            }
            catch { }
        }
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
                ["events"] = "json/events.json",
                ["machine_learning"] = "json/machine_learning.json"
            }
        };
        
        // Add startup screen if set
        if (!string.IsNullOrEmpty(_currentProject.StartupScreen))
        {
            root["startupScreen"] = _currentProject.StartupScreen;
        }

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

    /// <summary>
    /// Transforms historian configuration to ensure runtime compatibility.
    /// Migrates old format (storageType/storagePath) to new format (databaseType).
    /// </summary>
    private JObject TransformHistorianConfig(JObject source)
    {
        var result = new JObject();
        
        // Copy tags array
        if (source["tags"] is JArray tags)
        {
            result["tags"] = tags;
        }
        else
        {
            result["tags"] = new JArray();
        }
        
        // Copy logging interval
        result["loggingIntervalSeconds"] = source["loggingIntervalSeconds"]?.ToObject<int>() ?? 60;
        
        // Handle database type migration
        string databaseType = "SQLite";
        if (source["databaseType"] != null)
        {
            databaseType = source["databaseType"].ToString();
        }
        else if (source["storageType"] != null)
        {
            // Migrate from old format
            var oldStorageType = source["storageType"].ToString();
            if (oldStorageType == "Database" || oldStorageType == "File")
            {
                databaseType = "SQLite";
            }
            // Note: Cloud storage type would need different handling
        }
        result["databaseType"] = databaseType;
        
        // Copy other settings
        result["maxStorageSizeMB"] = source["maxStorageSizeMB"]?.ToObject<int>() ?? 1000;
        result["retentionPolicy"] = source["retentionPolicy"]?.ToString() ?? "Days";
        result["retentionDays"] = source["retentionDays"]?.ToObject<int>() ?? 30;
        result["enabled"] = source["enabled"]?.ToObject<bool>() ?? true;
        
        // Handle PostgreSQL settings if databaseType is PostgreSQL
        if (databaseType == "PostgreSQL")
        {
            if (source["postgresHost"] != null)
                result["postgresHost"] = source["postgresHost"];
            else
                result["postgresHost"] = "localhost";
                
            result["postgresPort"] = source["postgresPort"]?.ToObject<int>() ?? 5432;
            
            if (source["postgresDatabase"] != null)
                result["postgresDatabase"] = source["postgresDatabase"];
            else
                result["postgresDatabase"] = "historian";
                
            if (source["postgresUsername"] != null)
                result["postgresUsername"] = source["postgresUsername"];
                
            if (source["postgresPassword"] != null)
                result["postgresPassword"] = source["postgresPassword"];
        }
        
        // Remove old fields that shouldn't be in runtime config
        // (storagePath is set automatically by HistorianModule at runtime)
        // We don't include storagePath in the build output since it's runtime-specific
        
        return result;
    }
}
