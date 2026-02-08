namespace AccuTrack.Project;

/// <summary>
/// Project file paths and constants matching Qt Designer (project_module).
/// Project file is {projectDir}/{name}.isc; optional SCADA metadata is {scadaPath}/{name}_metadata.json.
/// </summary>
public static class ProjectFile
{
    public const string Extension = ".isc";
    public const string MetadataSuffix = "_metadata.json";

    /// <summary>Default projects root (e.g. Documents/Accutrack).</summary>
    public static string GetDefaultProjectsPath()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "Accutrack");
    }

    /// <summary>Project directory for a given name (e.g. .../Accutrack/MyProject).</summary>
    public static string GetProjectPath(string projectName)
    {
        return Path.Combine(GetDefaultProjectsPath(), projectName);
    }

    /// <summary>Full path to the .isc file (e.g. .../Accutrack/MyProject/MyProject.isc).</summary>
    public static string GetProjectFilePath(string projectDir, string projectName)
    {
        return Path.Combine(projectDir, projectName + Extension);
    }

    /// <summary>SCADA folder path inside project (e.g. .../MyProject/ScadaName).</summary>
    public static string GetScadaPath(string projectDir, string scadaName)
    {
        return Path.Combine(projectDir, scadaName);
    }

    /// <summary>SCADA metadata file path (e.g. .../ScadaName/ScadaName_metadata.json).</summary>
    public static string GetMetadataPath(string scadaPath, string scadaName)
    {
        return Path.Combine(scadaPath, scadaName + MetadataSuffix);
    }
}
