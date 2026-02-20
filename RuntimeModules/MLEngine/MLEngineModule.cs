using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.Historian;
using Runtime.Modules.Models;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.MLEngine;

/// <summary>ML engine module: loads ML config and trained models, runs inference on tag changes and optionally on historian time-series.</summary>
public sealed class MLEngineModule : ModuleBase, IMLEngine
{
    public const string FastForestRegressionId = "FastForestRegression";
    private const int DefaultHistorianTimeRangeMinutes = 60;
    private const int DefaultHistorianMaxRowsPerTag = 500;
    private const int DefaultTimeSeriesIntervalSeconds = 60;

    public bool IsInitialized => Status == "Initialized" || IsRunning;

    public override string ModuleName => "MLEngine";
    public override string DisplayName => "ML Engine";
    public override IReadOnlyList<string> Dependencies => new[] { "TagsModule" };

    /// <summary>Optional: set TagManager explicitly (e.g. from Program). If not set, resolved from TagsModule in Start().</summary>
    public void SetTagManager(TagManager? tagManager) => _tagManager = tagManager;

    private readonly List<ModelInstanceConfig> _modelConfigs = new();
    private readonly List<IMLModelRunner> _runners = new();
    private readonly List<TagSubscription> _subscriptions = new();
    private TagManager? _tagManager;
    private string? _projectPath;
    private HistorianManager? _historianManager;
    private readonly MLResultStore _mlResultStore = new();
    private Timer? _timeSeriesTimer;
    private int _timeSeriesIntervalSeconds = DefaultTimeSeriesIntervalSeconds;

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

        _timeSeriesIntervalSeconds = config["historianTimeSeriesIntervalSeconds"]?.GetValue<int>() ?? DefaultTimeSeriesIntervalSeconds;
        if (_timeSeriesIntervalSeconds < 10) _timeSeriesIntervalSeconds = 10;

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

            var useHistorianTimeSeries = m["useHistorianTimeSeries"]?.GetValue<bool>() ?? false;
            var historianTimeRangeMinutes = m["historianTimeRangeMinutes"]?.GetValue<int>() ?? DefaultHistorianTimeRangeMinutes;
            var historianMaxRowsPerTag = m["historianMaxRowsPerTag"]?.GetValue<int>() ?? DefaultHistorianMaxRowsPerTag;
            var saveResultsToDb = m["saveResultsToDb"]?.GetValue<bool>() ?? false;

            _modelConfigs.Add(new ModelInstanceConfig
            {
                Id = id,
                Name = name,
                ModelKindId = modelKindId,
                TrainedDataPath = trainedDataPath,
                InputTagNames = inputTags,
                OutputTagName = outputTag,
                UseHistorianTimeSeries = useHistorianTimeSeries,
                HistorianTimeRangeMinutes = historianTimeRangeMinutes,
                HistorianMaxRowsPerTag = historianMaxRowsPerTag,
                SaveResultsToDb = saveResultsToDb
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
        if (_tagManager == null)
        {
            var tagsModule = engine.ModuleManager.GetModule("TagsModule") as TagsModule;
            _tagManager = tagsModule?.TagManager;
        }
        if (_tagManager == null || string.IsNullOrEmpty(_projectPath))
        {
            SetRunning(true);
            return true;
        }

        var historian = engine.ModuleManager.GetModule("HistorianModule") as IHistorian;
        _historianManager = historian?.HistorianManager;

        _mlResultStore.SetDatabasePath(_projectPath);
        _mlResultStore.EnsureInitialized();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var runnerTypesByKind = ModelLoader.LoadRunnerTypes(_projectPath, baseDir);

        foreach (var cfg in _modelConfigs)
        {
            var dataPath = Path.Combine(_projectPath, cfg.TrainedDataPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(dataPath))
            {
                System.Diagnostics.Debug.WriteLine($"[MLEngine] Model data not found: {dataPath}");
                continue;
            }

            IMLModelRunner? runner = TryCreateRunner(runnerTypesByKind, cfg.ModelKindId, dataPath, cfg.Id, cfg.OutputTagName, cfg.InputTagNames);
            if (runner != null)
                _runners.Add(runner);
        }

        var allInputTags = _runners.SelectMany(r => r.InputTagNames).Distinct().ToList();
        foreach (var tagName in allInputTags)
        {
            var sub = _tagManager.Subscribe(tagName, OnTagChanged);
            _subscriptions.Add(sub);
        }

        var anyTimeSeries = _modelConfigs.Any(c => c.UseHistorianTimeSeries) && _historianManager != null;
        if (anyTimeSeries)
        {
            _timeSeriesTimer = new Timer(_ => RunTimeSeriesModels(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(_timeSeriesIntervalSeconds));
        }

        SetRunning(true);
        return true;
    }

    private static IMLModelRunner? TryCreateRunner(
        Dictionary<string, Type> runnerTypesByKind,
        string modelKindId,
        string dataPath,
        string modelId,
        string outputTagName,
        IReadOnlyList<string> inputTagNames)
    {
        if (!runnerTypesByKind.TryGetValue(modelKindId, out var type))
            return null;
        var method = type.GetMethod("TryLoad", BindingFlags.Public | BindingFlags.Static);
        if (method == null) return null;
        try
        {
            var result = method.Invoke(null, new object[] { dataPath, modelId, outputTagName, inputTagNames });
            return result as IMLModelRunner;
        }
        catch
        {
            return null;
        }
    }

    public override void Stop()
    {
        _timeSeriesTimer?.Dispose();
        _timeSeriesTimer = null;
        _historianManager = null;
        foreach (var sub in _subscriptions)
        {
            try { _tagManager?.Unsubscribe(sub); } catch { }
        }
        _subscriptions.Clear();
        _runners.Clear();
        SetRunning(false);
    }

    private void RunTimeSeriesModels()
    {
        if (_historianManager == null || _tagManager == null) return;
        var endMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var cfg in _modelConfigs)
        {
            if (!cfg.UseHistorianTimeSeries) continue;
            var runner = _runners.FirstOrDefault(r => string.Equals(r.ModelId, cfg.Id, StringComparison.OrdinalIgnoreCase));
            if (runner == null) continue;
            var startMs = endMs - (cfg.HistorianTimeRangeMinutes * 60L * 1000);
            var maxRows = cfg.HistorianMaxRowsPerTag > 0 ? cfg.HistorianMaxRowsPerTag : DefaultHistorianMaxRowsPerTag;
            var inputValues = new List<float>();
            foreach (var tagName in cfg.InputTagNames)
            {
                var rows = _historianManager.QueryTagValues(tagName, startMs, endMs, maxRows);
                if (rows.Count == 0) { inputValues.Clear(); break; }
                var last = rows[rows.Count - 1];
                if (!TryToFloat(last.Value, out var f)) { inputValues.Clear(); break; }
                inputValues.Add(f);
            }
            if (inputValues.Count != cfg.InputTagNames.Count) continue;
            try
            {
                var output = runner.Predict(inputValues);
                _tagManager.UpdateTagValue(runner.OutputTagName, output, TagQuality.Good);
                if (cfg.SaveResultsToDb)
                    _mlResultStore.RecordResult(cfg.Id, runner.OutputTagName, output, (int)TagQuality.Good);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MLEngine] Time-series Predict failed for {cfg.Id}: {ex.Message}");
            }
        }
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
            var cfg = _modelConfigs.FirstOrDefault(c => string.Equals(c.Id, modelId, StringComparison.OrdinalIgnoreCase));
            if (cfg?.SaveResultsToDb == true)
                _mlResultStore.RecordResult(runner.ModelId, runner.OutputTagName, output, (int)TagQuality.Good);
            System.Diagnostics.Trace.WriteLine($"[MLEngine] RunModelOnce OK: {modelId} -> {runner.OutputTagName}={output}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[MLEngine] RunModelOnce failed for {modelId}: {ex.Message}");
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
                var cfg = _modelConfigs.FirstOrDefault(c => string.Equals(c.Id, runner.ModelId, StringComparison.OrdinalIgnoreCase));
                if (cfg?.SaveResultsToDb == true)
                    _mlResultStore.RecordResult(runner.ModelId, runner.OutputTagName, output, (int)TagQuality.Good);
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
        public bool UseHistorianTimeSeries { get; set; }
        public int HistorianTimeRangeMinutes { get; set; } = DefaultHistorianTimeRangeMinutes;
        public int HistorianMaxRowsPerTag { get; set; } = DefaultHistorianMaxRowsPerTag;
        public bool SaveResultsToDb { get; set; }
    }
}
