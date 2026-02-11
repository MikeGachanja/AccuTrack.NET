using System.Drawing;
using System.IO;
using Designer.Modules.Compiler;
using Designer.Modules.Project;
using Xunit;

namespace Designer.Tests;

public class CompilerTests
{
    [Fact]
    public void CompileProject_ProducesMetadataAndJsonStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DarkStar_CompilerTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var buildPath = Path.Combine(tempDir, "build");
            var jsonPath = Path.Combine(buildPath, "json");
            var screensPath = Path.Combine(tempDir, "screens");
            var tagsPath = Path.Combine(tempDir, "tags");
            var scriptsPath = Path.Combine(tempDir, "scripts");
            var commPath = Path.Combine(tempDir, "communications");
            Directory.CreateDirectory(screensPath);
            Directory.CreateDirectory(tagsPath);
            Directory.CreateDirectory(scriptsPath);
            Directory.CreateDirectory(commPath);

            var project = new ScadaProject
            {
                Name = "TestProject",
                Path = tempDir,
                Type = ScadaType.HMI,
                Resolution = new Size(1920, 1080),
                Version = "1.0.0"
            };
            project.Paths.InitializeFromRoot(tempDir, project.Name);

            var screenJson = Path.Combine(screensPath, "main.json");
            File.WriteAllText(screenJson, @"{
  ""id"": ""main"",
  ""name"": ""Main"",
  ""size"": { ""width"": 1920, ""height"": 1080 },
  ""backgroundColor"": ""#FFFFFF"",
  ""components"": []
}");
            var compiler = new CompilerModule();
            compiler.SetProject(project);

            bool result = compiler.CompileProject();

            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(buildPath, "metadata.iscr")));
            Assert.True(Directory.Exists(jsonPath));
            Assert.True(File.Exists(Path.Combine(jsonPath, "communications.json")));
            Assert.True(File.Exists(Path.Combine(jsonPath, "screens.json")));
            Assert.True(File.Exists(Path.Combine(jsonPath, "tag_tables.json")));
            Assert.True(File.Exists(Path.Combine(jsonPath, "scripts.json")));
            Assert.True(File.Exists(Path.Combine(buildPath, "screens", "main.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CleanProject_RemovesBuildDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DarkStar_CompilerClean_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var project = new ScadaProject
            {
                Name = "CleanTest",
                Path = tempDir,
                Type = ScadaType.HMI
            };
            project.Paths.InitializeFromRoot(tempDir, project.Name);
            Directory.CreateDirectory(project.Paths.BuildPath);
            File.WriteAllText(Path.Combine(project.Paths.BuildPath, "dummy.txt"), "x");

            var compiler = new CompilerModule();
            compiler.SetProject(project);
            bool cleaned = compiler.CleanProject();

            Assert.True(cleaned);
            Assert.True(Directory.Exists(project.Paths.BuildPath));
            Assert.False(File.Exists(Path.Combine(project.Paths.BuildPath, "dummy.txt")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
