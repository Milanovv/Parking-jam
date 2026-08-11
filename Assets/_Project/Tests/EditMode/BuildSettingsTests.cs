using System.Linq;
using NUnit.Framework;
using UnityEditor;

public class BuildSettingsTests
{
    [Test]
    public void BuildSettings_CarryMainActive_PlusAllNineMiniGameScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotNull(scenes, "Build settings carry a scene list");
        Assert.AreEqual(10, scenes.Length, "Main plus the nine mini-game scenes go into the build");

        Assert.IsTrue(scenes[0].enabled, "Main is the active scene");
        StringAssert.EndsWith("Assets/Scenes/Main.unity", scenes[0].path, "The Main showcase scene is the active build scene");

        var miniGames = scenes.Skip(1).ToArray();
        Assert.IsTrue(miniGames.All(scene => !scene.enabled), "Mini-game scenes are additive, never active");

        var expected = new[]
        {
            MiniGameCatalog.ScenePath(MiniGameType.Pipes, MiniGameDifficulty.Easy),
            MiniGameCatalog.ScenePath(MiniGameType.Pipes, MiniGameDifficulty.Medium),
            MiniGameCatalog.ScenePath(MiniGameType.Pipes, MiniGameDifficulty.Hard),
            MiniGameCatalog.ScenePath(MiniGameType.Pattern, MiniGameDifficulty.Easy),
            MiniGameCatalog.ScenePath(MiniGameType.Pattern, MiniGameDifficulty.Medium),
            MiniGameCatalog.ScenePath(MiniGameType.Pattern, MiniGameDifficulty.Hard),
            MiniGameCatalog.ScenePath(MiniGameType.Memory, MiniGameDifficulty.Easy),
            MiniGameCatalog.ScenePath(MiniGameType.Memory, MiniGameDifficulty.Medium),
            MiniGameCatalog.ScenePath(MiniGameType.Memory, MiniGameDifficulty.Hard)
        };
        foreach (var path in expected)
            Assert.IsTrue(scenes.Any(scene => scene.path == path), path + " is registered in the build");
    }
}