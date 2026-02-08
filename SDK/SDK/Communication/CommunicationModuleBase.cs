using System.Text.Json;
using System.Text.Json.Nodes;

namespace AccuTrack.SDK.Communication;

/// <summary>
/// Abstract base for all communication modules. Implements ICommunicationModule with shared state
/// (name, running flag, config) and default Stop() behavior. Uses System.Text.Json for config.
/// </summary>
public abstract class CommunicationModuleBase : ICommunicationModule
{
    private string _name = string.Empty;
    private bool _running;
    private JsonDocument? _configDoc;
    private readonly object _configLock = new();

    /// <inheritdoc />
    public abstract string ModuleType { get; }

    /// <inheritdoc />
    public bool IsRunning => _running;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set => _name = value ?? string.Empty;
    }

    /// <inheritdoc />
    public event EventHandler<string>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler<string>? ErrorOccurred;

    /// <inheritdoc />
    public event EventHandler<JsonElement>? DataReceived;

    /// <inheritdoc />
    public abstract bool Configure(JsonElement config);

    /// <inheritdoc />
    /// <remarks>Returned JSON always includes "type" (ModuleType) so Designer/Runtime can round-trip communication.json (Plan 3 §9).</remarks>
    public JsonElement GetConfiguration()
    {
        lock (_configLock)
        {
            if (_configDoc == null)
                return default;
            var root = _configDoc.RootElement;
            var obj = new JsonObject();
            obj["type"] = ModuleType;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                    continue;
                obj[prop.Name] = JsonNode.Parse(prop.Value.GetRawText());
            }
            using var doc = JsonDocument.Parse(obj.ToJsonString());
            return doc.RootElement.Clone();
        }
    }

    /// <inheritdoc />
    public abstract bool ValidateConfiguration(JsonElement config);

    /// <inheritdoc />
    public abstract bool Start();

    /// <inheritdoc />
    public virtual void Stop()
    {
        _running = false;
    }

    /// <summary>Set the stored config from a JSON element (clone). Call from Configure in derived types.</summary>
    protected void SetConfig(JsonElement config)
    {
        lock (_configLock)
        {
            _configDoc?.Dispose();
            _configDoc = JsonDocument.Parse(config.GetRawText());
        }
    }

    /// <summary>Get a clone of the current config root, or default if none.</summary>
    protected JsonElement GetConfigRoot()
    {
        lock (_configLock)
        {
            if (_configDoc == null)
                return default;
            return _configDoc.RootElement.Clone();
        }
    }

    /// <summary>Set running flag (typically in Start()).</summary>
    protected void SetRunning(bool value)
    {
        _running = value;
    }

    /// <summary>Raise StatusChanged.</summary>
    protected void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    /// <summary>Raise ErrorOccurred.</summary>
    protected void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }

    /// <summary>Raise DataReceived.</summary>
    protected void OnDataReceived(JsonElement data)
    {
        DataReceived?.Invoke(this, data);
    }
}
