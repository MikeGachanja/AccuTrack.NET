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

        return obj;
    }

    public static MachineLearning FromJson(JObject json)
    {
        var ml = new MachineLearning
        {
            Enabled = json["enabled"]?.ToObject<bool>() ?? false,
            TrainingDataPath = json["trainingDataPath"]?.ToString() ?? string.Empty,
            TrainingIntervalHours = json["trainingIntervalHours"]?.ToObject<int>() ?? 24
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
    public string ModelType { get; set; } = "Regression"; // Regression, Classification, AnomalyDetection
    public List<string> InputTags { get; set; } = new List<string>();
    public string OutputTag { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public bool Enabled { get; set; } = true;

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

        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["modelType"] = ModelType,
            ["inputTags"] = inputTagsArray,
            ["outputTag"] = OutputTag,
            ["parameters"] = parametersObj,
            ["enabled"] = Enabled
        };
    }

    public static MLModel FromJson(JObject json)
    {
        var model = new MLModel
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            ModelType = json["modelType"]?.ToString() ?? "Regression",
            OutputTag = json["outputTag"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true
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
