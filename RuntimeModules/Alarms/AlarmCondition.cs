namespace Runtime.Modules.Alarms;

/// <summary>Evaluates alarm conditions (digital/analog/derived) and clear-with-hysteresis.</summary>
public static class AlarmCondition
{
    public static bool Evaluate(Alarm alarm, object? tagValue)
    {
        if (alarm == null || !alarm.Enabled) return false;
        return alarm.Type switch
        {
            AlarmType.Digital => EvaluateDigital(alarm, tagValue),
            AlarmType.Analog => EvaluateAnalog(alarm, tagValue),
            AlarmType.Derived => EvaluateDerived(alarm, tagValue),
            _ => false
        };
    }

    public static bool ShouldClear(Alarm alarm, object? tagValue)
    {
        if (alarm == null) return true;
        if (alarm.Type == AlarmType.Digital)
            return !EvaluateDigital(alarm, tagValue);
        if (alarm.Type == AlarmType.Analog)
            return !EvaluateAnalog(alarm, tagValue);
        return !EvaluateDerived(alarm, tagValue);
    }

    private static bool EvaluateDigital(Alarm alarm, object? tagValue)
    {
        var b = tagValue is bool vb ? vb : (tagValue is int vi && vi != 0) || (tagValue is double vd && vd != 0);
        return b;
    }

    private static bool EvaluateAnalog(Alarm alarm, object? tagValue)
    {
        if (tagValue == null || alarm.Threshold == null) return false;
        var v = ToDouble(tagValue);
        var t = ToDouble(alarm.Threshold);
        return CompareValues(v, t, alarm.Condition);
    }

    private static bool EvaluateDerived(Alarm alarm, object? tagValue)
    {
        return EvaluateAnalog(alarm, tagValue);
    }

    private static bool CompareValues(double value1, double value2, string condition)
    {
        return condition switch
        {
            ">" => value1 > value2,
            ">=" => value1 >= value2,
            "<" => value1 < value2,
            "<=" => value1 <= value2,
            "==" => Math.Abs(value1 - value2) < 1e-9,
            "!=" => Math.Abs(value1 - value2) >= 1e-9,
            _ => false
        };
    }

    private static double ToDouble(object o)
    {
        if (o is double d) return d;
        if (o is int i) return i;
        if (o is float f) return f;
        if (o is long l) return l;
        if (o is decimal dec) return (double)dec;
        if (o != null && double.TryParse(o.ToString(), out var parsed)) return parsed;
        return 0;
    }
}
