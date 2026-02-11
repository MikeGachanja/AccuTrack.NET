using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Designer.Modules.Discovery;

/// <summary>
/// Collects all files from a build directory for project transfer.
/// </summary>
public class ProjectPackager
{
    public string ProjectName { get; private set; } = "";
    public string BuildDirectory { get; private set; } = "";
    public IReadOnlyList<ProjectFile> ProjectFiles => _files;
    public long TotalSize => _totalSize;

    private readonly List<ProjectFile> _files = new();
    private long _totalSize;

    public event EventHandler<string>? PackagingProgress;

    public bool PackageProject(string buildDirectory)
    {
        _files.Clear();
        _totalSize = 0;
        ProjectName = "";
        BuildDirectory = buildDirectory ?? "";

        if (string.IsNullOrWhiteSpace(buildDirectory))
        {
            PackagingProgress?.Invoke(this, "Error: Build directory path is empty");
            return false;
        }

        if (!Directory.Exists(buildDirectory))
        {
            PackagingProgress?.Invoke(this, $"Error: Build directory does not exist: {buildDirectory}");
            return false;
        }

        string metadataPath = Path.Combine(buildDirectory, "metadata.iscr");
        if (File.Exists(metadataPath))
        {
            try
            {
                var json = File.ReadAllText(metadataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("name", out var nameEl))
                    ProjectName = nameEl.GetString() ?? "";
            }
            catch (Exception ex)
            {
                PackagingProgress?.Invoke(this, $"Warning: Could not read metadata.iscr: {ex.Message}");
            }
        }
        else
        {
            PackagingProgress?.Invoke(this, $"Warning: metadata.iscr not found at {metadataPath}");
        }

        if (string.IsNullOrEmpty(ProjectName))
            ProjectName = Path.GetFileName(buildDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? "Project";

        PackagingProgress?.Invoke(this, $"Packaging project: {ProjectName} from {buildDirectory}");

        CollectFiles(buildDirectory, buildDirectory);

        if (_files.Count == 0)
        {
            PackagingProgress?.Invoke(this, $"Error: No files found in build directory: {buildDirectory}");
            // List what files actually exist for debugging
            try
            {
                var existingFiles = Directory.GetFiles(buildDirectory, "*", SearchOption.AllDirectories);
                PackagingProgress?.Invoke(this, $"Found {existingFiles.Length} files in directory (may be filtered)");
            }
            catch { }
            return false;
        }

        PackagingProgress?.Invoke(this, $"Packaged {_files.Count} files ({_totalSize} bytes)");
        return true;
    }

    private void CollectFiles(string directory, string basePath)
    {
        foreach (var filePath in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (!IsValidProjectFile(filePath))
                continue;

            string relativePath = Path.GetRelativePath(basePath, filePath);
            relativePath = relativePath.Replace('\\', '/');
            var fi = new FileInfo(filePath);
            _files.Add(new ProjectFile { AbsolutePath = filePath, RelativePath = relativePath, Size = fi.Length });
            _totalSize += fi.Length;
        }
    }

    private static bool IsValidProjectFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileName)) return false;
        if (fileName.StartsWith(".")) return false;
        if (fileName == "thumbs.db") return false;
        if (fileName.EndsWith(".tmp")) return false;
        return true;
    }
}

public class ProjectFile
{
    public string AbsolutePath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public long Size { get; set; }
}
