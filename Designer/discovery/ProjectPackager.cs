using System.Text.Json;

namespace AccuTrack.Discovery;

/// <summary>
/// Collects files from the build directory (metadata.iscr, tags.json, Screens/*, etc.)
/// and creates a zip for transfer to the Runtime.
/// </summary>
public sealed class ProjectPackager
{
    private readonly List<ProjectFileEntry> _files = new();
    private long _totalSize;
    private string _projectName = "";
    private string _buildDirectory = "";

    public IReadOnlyList<ProjectFileEntry> Files => _files;
    public long TotalSize => _totalSize;
    public string ProjectName => _projectName;
    public string BuildDirectory => _buildDirectory;

    public bool Package(string buildDirectory)
    {
        _files.Clear();
        _totalSize = 0;
        _projectName = "";
        _buildDirectory = buildDirectory;

        if (!Directory.Exists(buildDirectory))
            return false;

        _buildDirectory = Path.GetFullPath(buildDirectory);

        // Try project name from metadata.iscr
        var metadataPath = Path.Combine(_buildDirectory, "metadata.iscr");
        if (File.Exists(metadataPath))
        {
            try
            {
                var json = File.ReadAllText(metadataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("name", out var n))
                    _projectName = n.GetString() ?? "";
                if (string.IsNullOrEmpty(_projectName) && root.TryGetProperty("projectName", out var n2))
                    _projectName = n2.GetString() ?? "";
            }
            catch { /* ignore */ }
        }
        if (string.IsNullOrEmpty(_projectName))
            _projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(_buildDirectory)) ?? "Project";

        foreach (var filePath in Directory.EnumerateFiles(_buildDirectory, "*", SearchOption.AllDirectories))
        {
            if (!IsValidProjectFile(filePath)) continue;
            var rel = Path.GetRelativePath(_buildDirectory, filePath);
            rel = rel.Replace('\\', '/');
            var fi = new FileInfo(filePath);
            _files.Add(new ProjectFileEntry(rel, filePath, fi.Length));
            _totalSize += fi.Length;
        }

        return _files.Count > 0;
    }

    private static bool IsValidProjectFile(string filePath)
    {
        var name = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(name)) return false;
        if (name.StartsWith('.') || string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}

public sealed record ProjectFileEntry(string RelativePath, string AbsolutePath, long Size);
