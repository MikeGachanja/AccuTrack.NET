using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.MLEngine;

/// <summary>ML engine module: loads ML config and trained models, runs inference on tag changes.</summary>
public sealed class MLEngineModule : ModuleBase, IMLEngine
{
    public const string FastForestRegressionId = "FastForestRegression";

    public bool IsInitialized => Status == "Initialized" || IsRunning;

    public override string ModuleName => "MLEngine";
    public override string DisplayName => "ML Engine";
    public override IReadOnlyList<string> Dependencies => new[] { "TagsModule" };

    private readonly List<ModelInstanceConfig> _modelConfigs = new();
    private readonly List<IMLModelRunner> _runners = new();
    private readonly List<TagSubscription> _subscriptions = new();
    private TagManager? _tagManager;
    private string? _projectPath;

    public override bool Initialize(JsonObject? config = null)
    {
        _modelConfigs.Clear();
        if (config == null)
        {
            SetStatus("Initialized (no config)");
            RaiseInitialized();
            return true;
        }

        var enabled = config["enabled"]?.GetValue<bool>() ?? false;
        if (!enabled)
        {
            SetStatus("Initialized (ML disabled)");
            RaiseInitialized();
            return true;
        }

        var models = config["models"] as JsonArray;
        if (models == null)
        {
            SetStatus("Initialized (no models)");
            RaiseInitialized();
            return true;
        }

        foreach (var node in models)
        {
            if (node is not JsonObject m) continue;
            var modelEnabled = m["enabled"]?.GetValue<bool>() ?? true;
            if (!modelEnabled) continue;

            var id = m["id"]?.GetValue<string>() ?? "";
            var name = m["name"]?.GetValue<string>() ?? "";
            var modelKindId = m["modelKindId"]?.GetValue<string>() ?? "";
            if (string.IsNullOrEmpty(modelKindId) && string.Equals(m["modelType"]?.GetValue<string>(), "Regression", StringComparison.OrdinalIgnoreCase))
                modelKindId = FastForestRegressionId;
            var trainedDataPath = m["trainedDataPath"]?.GetValue<string>() ?? "";
            var outputTag = m["outputTag"]?.GetValue<string>() ?? "";
            var inputTags = new List<string>();
            if (m["inputTags"] is JsonArray arr)
            {
                foreach (var t in arr)
                    if (t != null && t.GetValue<string>() is { } s && !string.IsNullOrEmpty(s))
                        inputTags.Add(s);
            }

            if (string.IsNullOrEmpty(trainedDataPath) || string.IsNullOrEmpty(outputTag) || inputTags.Count == 0)
                continue;

            _modelConfigs.Add(new ModelInstanceConfig
            {
                Id = id,
                Name = name,
                ModelKindId = modelKindId,
                TrainedDataPath = trainedDataPath,
                InputTagNames = inputTags,
                OutputTagName = outputTag
            });
        }

        SetStatus($"Initialized ({_modelConfigs.Count} model(s))");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        _runners.Clear();
        foreach (var sub in _subscriptions)
        {
            try { _tagManager?.Unsubscribe(sub); } catch { }
        }
        _subscriptions.Clear();

        var engine = global::Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance;
        _projectPath = engine.ProjectPath;
        var tagsModule = engine.ModuleManager.GetModule("TagsModule") as TagsModule;
        _tagManager = tagsModule?.TagManager;
        if (_tagManager == null || string.IsNullOrEmpty(_projectPath))
        {
            SetRunning(true);
            return true;
        }

        foreach (var cfg in _modelConfigs)
        {
            var dataPath = Path.Combine(_projectPath, cfg.TrainedDataPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(dataPath))
            {
                System.Diagnostics.Debug.WriteLine($"[MLEngine] Model data not found: {dataPath}");
                continue;
            }

            IMLModelRunner? runner = null;
            if (string.Equals(cfg.ModelKindId, FastForestRegressionId, StringComparison.OrdinalIgnoreCase))
            {
                runner = FastForestRegressionRunner.TryLoad(dataPath, cfg.Id, cfg.OutputTagName, cfg.InputTagNames);
            }

            if (runner != null)
                _runners.Add(runner);
        }

        var allInputTags = _runners.SelectMany(r => r.InputTagNames).Distinct().ToList();
        foreach (var tagName in allInputTags)
        {
            var sub = _tagManager.Subscribe(tagName, OnTagChanged);
            _subscriptions.Add(sub);
        }

        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        foreach (var sub in _subscriptions)
        {
            try { _tagManager?.Unsubscribe(sub); } catch { }
        }
        _subscriptions.Clear();
        _runners.Clear();
        SetRunning(false);
    }

    public void RunModelOnce(string modelId)
    {
        if (_tagManager == null || string.IsNullOrEmpty(modelId)) return;
        var runner = _runners.FirstOrDefault(r => string.Equals(r.ModelId, modelId, StringComparison.OrdinalIgnoreCase));
        if (runner == null) return;
        var inputValues = new List<float>();
        foreach (var inputTag in runner.InputTagNames)
        {
            var v = _tagManager.GetTagValue(inputTag);
            if (v == null) return;
            if (!TryToFloat(v, out var f)) return;
            inputValues.Add(f);
        }
        if (inputValues.Count != runner.InputTagNames.Count) return;
        try
        {
            var output = runner.Predict(inputValues);
            _tagManager.UpdateTagValue(runner.OutputTagName, output, TagQuality.Good);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MLEngine] RunModelOnce failed for {modelId}: {ex.Message}");
        }
    }

    private void OnTagChanged(string tagName, object? value, TagQuality quality)
    {
        if (_tagManager == null) return;
        foreach (var runner in _runners)
        {
            if (!runner.InputTagNames.Contains(tagName, StringComparer.OrdinalIgnoreCase)) continue;
            var inputValues = new List<float>();
            foreach (var inputTag in runner.InputTagNames)
            {
                var v = _tagManager.GetTagValue(inputTag);
                if (v == null) { inputValues.Clear(); break; }
                if (TryToFloat(v, out var f))
                    inputValues.Add(f);
                else
                {
                    inputValues.Clear();
                    break;
                }
            }
            if (inputValues.Count != runner.InputTagNames.Count) continue;
            try
            {
                var output = runner.Predict(inputValues);
                _tagManager.UpdateTagValue(runner.OutputTagName, output, TagQuality.Good);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MLEngine] Predict failed for {runner.ModelId}: {ex.Message}");
            }
        }
    }

    private static bool TryToFloat(object? value, out float result)
    {
        result = 0f;
        if (value == null) return false;
        if (value is float f) { result = f; return true; }
        if (value is double d) { result = (float)d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is decimal dec) { result = (float)dec; return true; }
        if (float.TryParse(value.ToString(), out result)) return true;
        return false;
    }

    private sealed class ModelInstanceConfig
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string ModelKindId { get; set; } = "";
        public string TrainedDataPath { get; set; } = "";
        public List<string> InputTagNames { get; set; } = new();
        public string OutputTagName { get; set; } = "";
    }
}
