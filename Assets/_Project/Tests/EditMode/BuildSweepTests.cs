using System.IO;
using System.Linq;
using NUnit.Framework;

public class BuildSweepTests
{
    private static string[] ReportLines()
    {
        return File.ReadAllLines(BuildSweep.ReportPath);
    }

    private static string ReportText()
    {
        return string.Join("\n", ReportLines());
    }

    [Test]
    [Timeout(1800000)]
    public void Build_ProducesTheSweepReport()
    {
        bool fresh = File.Exists(BuildSweep.ReportPath)
            && File.Exists(BuildSweep.PlayerPath)
            && File.ReadAllText(BuildSweep.ReportPath).Contains(BuildSweep.SucceededMarker);
        if (!fresh) BuildSweep.Build();

        Assert.IsTrue(File.Exists(BuildSweep.ReportPath),
            "The sweep build lands its report; rebuild by deleting " + BuildSweep.PlayerPath + " or running BuildSweep.Build interactively");
    }

    [Test]
    public void Report_Exists_FromTheLastSweepBuild()
    {
        Assert.IsTrue(File.Exists(BuildSweep.ReportPath),
            "Run the sweep build (BuildSweep.Build) once so its report lands on disk");
    }

    [Test]
    public void Report_ShowsABuildThatSucceeded()
    {
        Assert.That(ReportText(), Does.Contain(BuildSweep.SucceededMarker),
            "The report records a successful player build");
    }

    [Test]
    public void Report_ListsMainPlusAllNineMiniGameScenes()
    {
        var scenes = ReportLines().Where(line => line.StartsWith(BuildSweep.SceneLinePrefix)).ToArray();
        Assert.AreEqual(1, scenes.Length, "Exactly one active scene goes into the build");
        Assert.That(scenes[0], Does.EndWith("Assets/Scenes/Main.unity"),
            "The reference-repo scene never slips into the build");

        var miniGamesLine = ReportLines().First(line => line.StartsWith(BuildSweep.MiniGamesLinePrefix));
        foreach (var type in new[] { MiniGameType.Pipes, MiniGameType.Pattern, MiniGameType.Memory })
        {
            foreach (var difficulty in new[] { MiniGameDifficulty.Easy, MiniGameDifficulty.Medium, MiniGameDifficulty.Hard })
            {
                Assert.That(miniGamesLine, Does.Contain(MiniGameCatalog.SceneName(type, difficulty)),
                    MiniGameCatalog.SceneName(type, difficulty) + " ships in the build as an additive scene");
            }
        }
    }

    [Test]
    public void Report_ContentStaysWithinTheSourcedAllowlist()
    {
        var content = ReportLines().Where(line => line.StartsWith(BuildSweep.ContentLinePrefix)).ToArray();
        Assert.That(content.Length, Is.GreaterThan(0), "The report enumerates the included assets");

        foreach (var line in content)
        {
            string path = line.Substring(BuildSweep.ContentLinePrefix.Length).Trim();
            var allowed = BuildSweep.ContentPathPrefixes.Any(path.StartsWith);
            Assert.IsTrue(allowed, path + " is first-party or sourced third-party content");
        }
    }

    [Test]
    public void Report_MarksEveryTexture_Bc7AtMax2048()
    {
        var textures = ReportLines().Where(line => line.StartsWith(BuildSweep.TextureLinePrefix)).ToArray();
        Assert.That(textures.Length, Is.GreaterThan(0), "The report pins the baked-in textures");

        foreach (var line in textures)
            Assert.That(line, Does.Contain(" BC7 max-2048"), line + " was compressed BC7 at 2048");
    }

    [Test]
    public void Report_ShowsVehicleFootprints_AtD8Scales()
    {
        foreach (var expected in BuildSweep.FootprintMarkers)
        {
            Assert.That(ReportText(), Does.Contain(expected),
                expected + " - vehicles keep their D8 grid-aligned footprints in the sweep");
        }
    }

    [Test]
    public void Report_MarksEverySweptPrefab_AsUncollided()
    {
        foreach (var expected in BuildSweep.NoColliderMarkers)
        {
            Assert.That(ReportText(), Does.Contain(expected),
                expected + " - generated prefabs carry no physics, collision stays grid-space");
        }
    }
}