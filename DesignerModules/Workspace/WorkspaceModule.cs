using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Workspace;

/// <summary>
/// Module for managing workspace layout and state.
/// </summary>
public class WorkspaceModule
{
    private string _workspaceConfigPath;

    public WorkspaceModule()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AccuTrackDesigner");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        _workspaceConfigPath = Path.Combine(appDataPath, "workspace.json");
    }

    /// <summary>
    /// Saves the current layout state.
    /// </summary>
    public void SaveLayout(Form mainForm, SplitContainer[] splitContainers, DockPanelState[] dockPanels)
    {
        try
        {
            var layout = new JObject
            {
                ["mainForm"] = new JObject
                {
                    ["windowState"] = mainForm.WindowState.ToString(),
                    ["location"] = new JObject
                    {
                        ["x"] = mainForm.Location.X,
                        ["y"] = mainForm.Location.Y
                    },
                    ["size"] = new JObject
                    {
                        ["width"] = mainForm.Size.Width,
                        ["height"] = mainForm.Size.Height
                    }
                },
                ["splitContainers"] = new JArray(),
                ["dockPanels"] = new JArray()
            };

            var splitArray = layout["splitContainers"] as JArray;
            foreach (var split in splitContainers)
            {
                splitArray?.Add(new JObject
                {
                    ["name"] = split.Name,
                    ["splitterDistance"] = split.SplitterDistance
                });
            }

            var dockArray = layout["dockPanels"] as JArray;
            foreach (var dock in dockPanels)
            {
                dockArray?.Add(new JObject
                {
                    ["name"] = dock.Name,
                    ["state"] = dock.State
                });
            }

            File.WriteAllText(_workspaceConfigPath, layout.ToString());
        }
        catch
        {
            // Silently fail if layout can't be saved
        }
    }

    /// <summary>
    /// Restores the saved layout state.
    /// </summary>
    public void RestoreLayout(Form mainForm, SplitContainer[] splitContainers, DockPanelState[] dockPanels)
    {
        try
        {
            if (!File.Exists(_workspaceConfigPath))
                return;

            var json = JObject.Parse(File.ReadAllText(_workspaceConfigPath));
            var mainFormObj = json["mainForm"] as JObject;

            if (mainFormObj != null)
            {
                var locationObj = mainFormObj["location"] as JObject;
                var sizeObj = mainFormObj["size"] as JObject;

                if (locationObj != null)
                {
                    mainForm.Location = new System.Drawing.Point(
                        locationObj["x"]?.ToObject<int>() ?? mainForm.Location.X,
                        locationObj["y"]?.ToObject<int>() ?? mainForm.Location.Y
                    );
                }

                if (sizeObj != null)
                {
                    mainForm.Size = new System.Drawing.Size(
                        sizeObj["width"]?.ToObject<int>() ?? mainForm.Size.Width,
                        sizeObj["height"]?.ToObject<int>() ?? mainForm.Size.Height
                    );
                }

                if (Enum.TryParse<FormWindowState>(mainFormObj["windowState"]?.ToString(), out var windowState))
                {
                    mainForm.WindowState = windowState;
                }
            }

            var splitArray = json["splitContainers"] as JArray;
            if (splitArray != null)
            {
                foreach (var item in splitArray)
                {
                    if (item is JObject splitObj)
                    {
                        var name = splitObj["name"]?.ToString();
                        var distance = splitObj["splitterDistance"]?.ToObject<int>();
                        if (name != null && distance.HasValue)
                        {
                            foreach (var split in splitContainers)
                            {
                                if (split.Name == name)
                                {
                                    split.SplitterDistance = distance.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Silently fail if layout can't be restored
        }
    }
}

/// <summary>
/// Represents dock panel state.
/// </summary>
public class DockPanelState
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
