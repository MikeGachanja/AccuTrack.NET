using Runtime.Modules.Communication;

namespace Runtime.Modules.TagsEngine;

/// <summary>Handles tag I/O with communication layer. Reads/writes via CommunicationModule, updates TagManager.</summary>
public sealed class TagIOHandler
{
    private readonly TagManager _tagManager;
    private volatile bool _running;
    private CommunicationModule? _communicationModule;

    public TagIOHandler(TagManager tagManager)
    {
        _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
    }

    public void SetCommunicationModule(object? commModule)
    {
        _communicationModule = commModule as CommunicationModule;
    }

    public void Start() => _running = true;
    public void Stop() => _running = false;
    public bool IsRunning => _running;

    /// <summary>Read a tag value from the communication layer and update TagManager.</summary>
    public bool ReadTag(string tagName)
    {
        if (!_running) return false;
        
        var tag = _tagManager.GetTag(tagName);
        if (tag == null) return false;
        
        if (_communicationModule == null)
        {
            _tagManager.UpdateTagValue(tagName, tag.GetValue(), TagQuality.NotConnected);
            return false;
        }
        
        // Try reading by address first, then by tag name if address fails
        bool readSuccess = false;
        object? value = null;
        
        // First try using the address (if available)
        if (!string.IsNullOrEmpty(tag.Address))
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Attempting to read tag '{tagName}' by address '{tag.Address}'");
            if (_communicationModule.ReadTagByAddress(tag.Address, out value))
            {
                readSuccess = true;
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Successfully read tag '{tagName}' by address '{tag.Address}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Failed to read tag '{tagName}' by address '{tag.Address}', will try tag name");
            }
        }
        
        // If address read failed or no address, try using tag name
        if (!readSuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Attempting to read tag '{tagName}' by name");
            if (_communicationModule.ReadTagByAddress(tagName, out value))
            {
                readSuccess = true;
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Successfully read tag '{tagName}' by name");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ReadTag: Failed to read tag '{tagName}' by name");
            }
        }
        
        if (readSuccess && value != null)
        {
            _tagManager.UpdateTagValue(tagName, value, TagQuality.Good);
            return true;
        }
        
        // If both attempts failed, mark as not connected
        _tagManager.UpdateTagValue(tagName, tag.GetValue(), TagQuality.NotConnected);
        return false;
    }

    /// <summary>Read a tag value by address from the communication layer and update TagManager.</summary>
    public bool ReadTagByAddress(string address)
    {
        if (!_running) return false;
        
        var tag = _tagManager.GetTagByAddress(address);
        if (tag == null) return false;
        
        // Read from communication layer
        if (_communicationModule != null && _communicationModule.ReadTagByAddress(address, out var value))
        {
            _tagManager.UpdateTagValueByAddress(address, value, TagQuality.Good);
            return true;
        }
        
        // If communication module not available or read failed, mark as not connected
        _tagManager.UpdateTagValueByAddress(address, tag.GetValue(), TagQuality.NotConnected);
        return false;
    }

    /// <summary>Write a tag value to the communication layer and update TagManager.</summary>
    public bool WriteTag(string tagName, object? value)
    {
        if (!_running)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag failed: Handler is not running. TagName: {tagName}, Value: {value}");
            return false;
        }
        
        System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Starting write operation for tag '{tagName}', Value: '{value}' (Type: {value?.GetType().Name ?? "null"})");
        
        var tag = _tagManager.GetTag(tagName);
        if (tag == null)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag failed: Tag '{tagName}' not found in TagManager");
            return false;
        }
        
        System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Tag found - Name: '{tag.Name}', Address: '{tag.Address}', DataType: '{tag.DataType}'");
        
        if (_communicationModule == null)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: CommunicationModule is null - updating TagManager only");
            return _tagManager.UpdateTagValue(tagName, value, TagQuality.NotConnected);
        }
        
        // Convert value based on tag's data type
        object? convertedValue = ConvertValueForTag(value, tag.DataType);
        System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Converted value from '{value}' (Type: {value?.GetType().Name ?? "null"}) to '{convertedValue}' (Type: {convertedValue?.GetType().Name ?? "null"}) for DataType '{tag.DataType}'");
        
        // Try writing by address first, then by tag name if address fails
        bool writeSuccess = false;
        
        // First try using the address (if available)
        if (!string.IsNullOrEmpty(tag.Address))
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Attempting to write tag '{tagName}' by address '{tag.Address}'");
            writeSuccess = _communicationModule.WriteTagByAddress(tag.Address, convertedValue);
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Write by address '{tag.Address}' returned {writeSuccess}");
            
            if (writeSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Successfully wrote tag '{tagName}' by address '{tag.Address}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Failed to write tag '{tagName}' by address '{tag.Address}', will try tag name");
            }
        }
        
        // If address write failed or no address, try using tag name
        if (!writeSuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Attempting to write tag '{tagName}' by name");
            writeSuccess = _communicationModule.WriteTagByAddress(tagName, convertedValue);
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Write by name '{tagName}' returned {writeSuccess}");
            
            if (writeSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Successfully wrote tag '{tagName}' by name");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Failed to write tag '{tagName}' by name");
            }
        }
        
        // Update TagManager regardless (optimistic update)
        // Quality will reflect connection status
        var quality = writeSuccess ? TagQuality.Good : TagQuality.NotConnected;
        System.Diagnostics.Debug.WriteLine($"[TagIOHandler] WriteTag: Updating TagManager with quality {quality}");
        return _tagManager.UpdateTagValue(tagName, value, quality);
    }

    /// <summary>Write a tag value by address to the communication layer and update TagManager.</summary>
    public bool WriteTagByAddress(string address, object? value)
    {
        if (!_running) return false;
        
        var tag = _tagManager.GetTagByAddress(address);
        if (tag == null) return false;
        
        // Write to communication layer first
        bool writeSuccess = false;
        if (_communicationModule != null)
        {
            writeSuccess = _communicationModule.WriteTagByAddress(address, value);
        }
        
        // Update TagManager regardless (optimistic update)
        var quality = writeSuccess ? TagQuality.Good : TagQuality.NotConnected;
        return _tagManager.UpdateTagValueByAddress(address, value, quality);
    }

    /// <summary>Read all tags that have addresses from the communication layer.</summary>
    public void ReadAllTags()
    {
        foreach (var name in _tagManager.GetTagNames())
        {
            var tag = _tagManager.GetTag(name);
            if (tag != null && !string.IsNullOrEmpty(tag.Address))
            {
                ReadTag(name);
            }
        }
    }

    /// <summary>Convert a value to the appropriate type based on the tag's data type.</summary>
    private object? ConvertValueForTag(object? value, string dataType)
    {
        if (value == null) return null;
        
        if (string.IsNullOrWhiteSpace(dataType))
        {
            // No data type specified, return as-is
            return value;
        }
        
        var dataTypeLower = dataType.Trim().ToLowerInvariant();
        
        try
        {
            switch (dataTypeLower)
            {
                case "bit":
                case "bool":
                case "boolean":
                    // Convert to boolean
                    if (value is bool boolVal)
                        return boolVal;
                    if (value is int intVal)
                        return intVal != 0;
                    if (value is long longVal)
                        return longVal != 0;
                    if (value is string strVal)
                    {
                        if (bool.TryParse(strVal, out var parsedBool))
                            return parsedBool;
                        if (int.TryParse(strVal, out var parsedInt))
                            return parsedInt != 0;
                        return strVal.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                               strVal.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    // For other numeric types
                    if (value is IConvertible convertible)
                    {
                        var intResult = convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                        return intResult != 0;
                    }
                    return Convert.ToBoolean(value);
                    
                case "int":
                case "int16":
                case "int32":
                case "integer":
                    if (value is int) return value;
                    if (value is long longVal2 && longVal2 >= int.MinValue && longVal2 <= int.MaxValue)
                        return (int)longVal2;
                    if (value is IConvertible convertible2)
                        return convertible2.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    return Convert.ToInt32(value);
                    
                case "int64":
                case "long":
                    if (value is long) return value;
                    if (value is IConvertible convertible3)
                        return convertible3.ToInt64(System.Globalization.CultureInfo.InvariantCulture);
                    return Convert.ToInt64(value);
                    
                case "float":
                case "single":
                    if (value is float) return value;
                    if (value is IConvertible convertible4)
                        return convertible4.ToSingle(System.Globalization.CultureInfo.InvariantCulture);
                    return Convert.ToSingle(value);
                    
                case "double":
                case "real":
                    if (value is double) return value;
                    if (value is IConvertible convertible5)
                        return convertible5.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                    return Convert.ToDouble(value);
                    
                case "string":
                case "text":
                    return value.ToString();
                    
                default:
                    // Unknown data type, return as-is
                    System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ConvertValueForTag: Unknown data type '{dataType}', returning value as-is");
                    return value;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TagIOHandler] ConvertValueForTag: Error converting value '{value}' to type '{dataType}': {ex.Message}");
            return value; // Return original value if conversion fails
        }
    }
}
