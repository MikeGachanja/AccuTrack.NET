using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Represents machine learning configuration for a SCADA project.
/// </summary>
public class MachineLearning
{
    public List<MLModel> Models { get; set; } = new List<MLModel>();
    public bool Enabled { get; set; } = false;
    public string TrainingDataPath { get; set; } = string.Empty;
    public int TrainingIntervalHours { get; set; } = 24;
    /// <summary>Interval in seconds for time-series model runs when using historian (default 60).</summary>
    public int HistorianTimeSeriesIntervalSeconds { get; set; } = 60;

    public JObject ToJson()
    {
        var obj = new JObject();
        var modelsArray = new JArray();

        foreach (var model in Models)
        {
            modelsArray.Add(model.ToJson());
        }

        obj["models"] = modelsArray;
        obj["enabled"] = Enabled;
        obj["trainingDataPath"] = TrainingDataPath;
        obj["trainingIntervalHours"] = TrainingIntervalHours;
        if (HistorianTimeSeriesIntervalSeconds != 60)
            obj["historianTimeSeriesIntervalSeconds"] = HistorianTimeSeriesIntervalSeconds;

        return obj;
    }

    public static MachineLearning FromJson(JObject json)
    {
        var ml = new MachineLearning
        {
            Enabled = json["enabled"]?.ToObject<bool>() ?? false,
            TrainingDataPath = json["trainingDataPath"]?.ToString() ?? string.Empty,
            TrainingIntervalHours = json["trainingIntervalHours"]?.ToObject<int>() ?? 24,
            HistorianTimeSeriesIntervalSeconds = json["historianTimeSeriesIntervalSeconds"]?.ToObject<int>() ?? 60
        };

        var modelsArray = json["models"] as JArray;
        if (modelsArray != null)
        {
            foreach (var item in modelsArray)
            {
                if (item is JObject modelObj)
                {
                    ml.Models.Add(MLModel.FromJson(modelObj));
                }
            }
        }

        return ml;
    }
}

/// <summary>
/// Represents an ML model configuration.
/// </summary>
public class MLModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    /// <summary>Standard model kind id from catalog (e.g. FastForestRegression). Preferred over ModelType.</summary>
    public string ModelKindId { get; set; } = string.Empty;
    /// <summary>Project-relative path to trained model file (e.g. machine_learning/data/QA_Model.zip).</summary>
    public string TrainedDataPath { get; set; } = string.Empty;
    /// <summary>Legacy: Regression, Classification, AnomalyDetection. Used when ModelKindId is empty.</summary>
    public string ModelType { get; set; } = "Regression";
    public List<string> InputTags { get; set; } = new List<string>();
    public string OutputTag { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public bool Enabled { get; set; } = true;
    /// <summary>When true, run this model from historian time-series data on a timer (in addition to or instead of live tags).</summary>
    public bool UseHistorianTimeSeries { get; set; }
    /// <summary>Time range in minutes for historian queries when using time-series mode (e.g. 60 = last hour).</summary>
    public int HistorianTimeRangeMinutes { get; set; } = 60;
    /// <summary>Max rows per tag when querying historian (e.g. 500).</summary>
    public int HistorianMaxRowsPerTag { get; set; } = 500;
    /// <summary>When true, write prediction results to ml.db.</summary>
    public bool SaveResultsToDb { get; set; }

    public JObject ToJson()
    {
        var parametersObj = new JObject();
        foreach (var param in Parameters)
        {
            parametersObj[param.Key] = JToken.FromObject(param.Value);
        }

        var inputTagsArray = new JArray();
        foreach (var tag in InputTags)
        {
            inputTagsArray.Add(tag);
        }

        var obj = new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["modelType"] = ModelType,
            ["inputTags"] = inputTagsArray,
            ["outputTag"] = OutputTag,
            ["parameters"] = parametersObj,
            ["enabled"] = Enabled
        };
        if (!string.IsNullOrEmpty(ModelKindId))
            obj["modelKindId"] = ModelKindId;
        if (!string.IsNullOrEmpty(TrainedDataPath))
            obj["trainedDataPath"] = TrainedDataPath;
        if (UseHistorianTimeSeries)
            obj["useHistorianTimeSeries"] = true;
        if (HistorianTimeRangeMinutes != 60)
            obj["historianTimeRangeMinutes"] = HistorianTimeRangeMinutes;
        if (HistorianMaxRowsPerTag != 500)
            obj["historianMaxRowsPerTag"] = HistorianMaxRowsPerTag;
        if (SaveResultsToDb)
            obj["saveResultsToDb"] = true;
        return obj;
    }

    public static MLModel FromJson(JObject json)
    {
        var model = new MLModel
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            ModelType = json["modelType"]?.ToString() ?? "Regression",
            ModelKindId = json["modelKindId"]?.ToString() ?? string.Empty,
            TrainedDataPath = json["trainedDataPath"]?.ToString() ?? string.Empty,
            OutputTag = json["outputTag"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            UseHistorianTimeSeries = json["useHistorianTimeSeries"]?.ToObject<bool>() ?? false,
            HistorianTimeRangeMinutes = json["historianTimeRangeMinutes"]?.ToObject<int>() ?? 60,
            HistorianMaxRowsPerTag = json["historianMaxRowsPerTag"]?.ToObject<int>() ?? 500,
            SaveResultsToDb = json["saveResultsToDb"]?.ToObject<bool>() ?? false
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            model.Id = id;
        }

        var inputTagsArray = json["inputTags"] as JArray;
        if (inputTagsArray != null)
        {
            foreach (var item in inputTagsArray)
            {
                model.InputTags.Add(item.ToString());
            }
        }

        var parametersObj = json["parameters"] as JObject;
        if (parametersObj != null)
        {
            foreach (var prop in parametersObj.Properties())
            {
                model.Parameters[prop.Name] = prop.Value.ToObject<object>();
            }
        }

        return model;
    }
}
