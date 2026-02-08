using System.Text.Json;
using AccuTrack.Runtime.EventDispatch;
using AccuTrack.Runtime.Modules;

namespace AccuTrack.Runtime.Tags;

/// <summary>
/// TagsModule implementing IModuleInterface; TagManager, TagIOHandler; config tags.json.
/// </summary>
public sealed class TagsModule : IModuleInterface
{
    private readonly TagManager _tagManager = new();
    private readonly EventDispatcher _dispatcher = EventDispatcher.Instance;
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";

    public TagManager TagManager => _tagManager;
    public TagIOHandler TagIOHandler { get; }

    public TagsModule()
    {
        TagIOHandler = new TagIOHandler(_tagManager);
    }

    public string ModuleName => "Tags";
    public string DisplayName => "Tags Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => new[] { "Communication" };
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _tagManager.LoadFromJson(config);
        SetStatus("Initialized");
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _running = true;
        SetStatus("Running");
        Started?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _running = false;
        SetStatus("Stopped");
        Stopped?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _tagManager.Clear();
        return Task.CompletedTask;
    }

    public JsonElement? GetConfiguration() => _config;
    public bool ValidateConfiguration(JsonElement? config) => true;

    public void SetDriverProvider(Func<IReadOnlyList<AccuTrack.Runtime.Abstractions.ICommunicationDriver>>? provider)
    {
        TagIOHandler.SetDriverProvider(provider);
    }

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
