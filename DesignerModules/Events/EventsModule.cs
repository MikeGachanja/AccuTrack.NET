using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Events;

/// <summary>
/// Module for managing SCADA events.
/// </summary>
public class EventsModule
{
    private string? _eventsJsonPath;
    private object? _scadaProject; // ScadaProject object for context
    
    /// <summary>
    /// Sets the SCADA project for context.
    /// </summary>
    public void SetScadaProject(object? scadaProject)
    {
        _scadaProject = scadaProject;
        
        // Determine events.json path from project
        if (scadaProject != null)
        {
            try
            {
                dynamic project = scadaProject;
                var paths = project.Paths;
                if (paths != null)
                {
                    // ScadaPaths has RootPath, construct json path from it
                    var rootPath = paths.RootPath?.ToString();
                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        _eventsJsonPath = Path.Combine(rootPath, "json", "events.json");
                    }
                }
            }
            catch
            {
                // Fallback: try to get path from project directory
                try
                {
                    dynamic project = scadaProject;
                    var path = project.Path?.ToString();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var projectDir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(projectDir))
                        {
                            _eventsJsonPath = Path.Combine(projectDir, "json", "events.json");
                        }
                    }
                }
                catch
                {
                    // Could not determine path
                }
            }
        }
    }
    
    /// <summary>
    /// Sets the events.json file path directly.
    /// </summary>
    public void SetEventsJsonPath(string? path)
    {
        _eventsJsonPath = path;
    }
    
    /// <summary>
    /// Gets the events.json file path.
    /// </summary>
    public string? GetEventsJsonPath() => _eventsJsonPath;
    
    /// <summary>
    /// Loads events from events.json file.
    /// </summary>
    public List<ScadaEvent> LoadEventsFromJson(string? path = null)
    {
        var filePath = path ?? _eventsJsonPath;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new List<ScadaEvent>();
        }
        
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JObject.Parse(json);
            
            var events = new List<ScadaEvent>();
            
            if (jsonObj["events"] is JArray eventsArray)
            {
                foreach (var item in eventsArray)
                {
                    if (item is JObject eventObj)
                    {
                        events.Add(ScadaEvent.FromJson(eventObj));
                    }
                }
            }
            
            return events;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading events from {filePath}: {ex.Message}");
            return new List<ScadaEvent>();
        }
    }
    
    /// <summary>
    /// Saves events to events.json file.
    /// </summary>
    public bool SaveEventsToJson(List<ScadaEvent> events, string? path = null)
    {
        var filePath = path ?? _eventsJsonPath;
        if (string.IsNullOrEmpty(filePath))
        {
            System.Diagnostics.Debug.WriteLine("EventsModule: No file path specified for saving events");
            return false;
        }
        
        try
        {
            var eventsArray = new JArray();
            foreach (var evt in events)
            {
                eventsArray.Add(evt.ToJson());
            }
            
            var json = new JObject
            {
                ["events"] = eventsArray
            };
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json.ToString(Newtonsoft.Json.Formatting.Indented));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving events to {filePath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets events for a specific component.
    /// </summary>
    public List<ScadaEvent> GetEventsForComponent(string componentId, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        return allEvents.Where(e => e.Trigger.ComponentId == componentId).ToList();
    }
    
    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    public ScadaEvent? GetEventById(string eventId, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        return allEvents.FirstOrDefault(e => e.Id == eventId);
    }
    
    /// <summary>
    /// Adds a new event.
    /// </summary>
    public bool AddEvent(ScadaEvent evt, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        
        // Check if event with same ID already exists
        if (allEvents.Any(e => e.Id == evt.Id))
        {
            System.Diagnostics.Debug.WriteLine($"Event with ID {evt.Id} already exists");
            return false;
        }
        
        allEvents.Add(evt);
        return SaveEventsToJson(allEvents, path);
    }
    
    /// <summary>
    /// Updates an existing event.
    /// </summary>
    public bool UpdateEvent(string eventId, ScadaEvent evt, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        var index = allEvents.FindIndex(e => e.Id == eventId);
        
        if (index < 0)
        {
            System.Diagnostics.Debug.WriteLine($"Event with ID {eventId} not found");
            return false;
        }
        
        // Update metadata
        evt.Metadata.ModifiedBy = "User";
        evt.Metadata.ModifiedAt = DateTime.Now.ToString("O");
        
        allEvents[index] = evt;
        return SaveEventsToJson(allEvents, path);
    }
    
    /// <summary>
    /// Removes an event.
    /// </summary>
    public bool RemoveEvent(string eventId, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        var removed = allEvents.RemoveAll(e => e.Id == eventId);
        
        if (removed == 0)
        {
            System.Diagnostics.Debug.WriteLine($"Event with ID {eventId} not found");
            return false;
        }
        
        return SaveEventsToJson(allEvents, path);
    }
    
    /// <summary>
    /// Removes all events for a component.
    /// </summary>
    public bool RemoveEventsForComponent(string componentId, string? path = null)
    {
        var allEvents = LoadEventsFromJson(path);
        var removed = allEvents.RemoveAll(e => e.Trigger.ComponentId == componentId);
        
        if (removed == 0)
        {
            return true; // No events to remove is not an error
        }
        
        return SaveEventsToJson(allEvents, path);
    }
}
