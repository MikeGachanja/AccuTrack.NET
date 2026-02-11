using System.IO;
using Designer.Modules.Discovery;
using Xunit;

namespace Designer.Tests;

public class ProjectPackagerTests
{
    [Fact]
    public void PackageProject_CollectsFilesAndReadsProjectName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DarkStar_Packager_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var jsonPath = Path.Combine(tempDir, "json");
            Directory.CreateDirectory(jsonPath);
            File.WriteAllText(Path.Combine(tempDir, "metadata.iscr"), "{\"name\":\"PackagerTest\",\"type\":0,\"version\":\"1.0.0\"}");
            File.WriteAllText(Path.Combine(jsonPath, "screens.json"), "{\"screens\":[]}");

            var packager = new ProjectPackager();
            bool ok = packager.PackageProject(tempDir);

            Assert.True(ok);
            Assert.Equal("PackagerTest", packager.ProjectName);
            Assert.True(packager.ProjectFiles.Count >= 2);
            Assert.True(packager.TotalSize > 0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void PackageProject_EmptyDirectory_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DarkStar_PackagerEmpty_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);

            var packager = new ProjectPackager();
            bool ok = packager.PackageProject(tempDir);

            Assert.False(ok);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
