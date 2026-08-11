using NUnit.Framework;
using UnityEditor;

public class BuildSettingsTests
{
    [Test]
    public void BuildSettings_ContainOnlyTheMainScene()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotNull(scenes, "Build settings carry a scene list");
        Assert.AreEqual(1, scenes.Length, "The build carries exactly one scene");
        Assert.IsTrue(scenes[0].enabled, "The scene is enabled");
        StringAssert.EndsWith("Assets/Scenes/Main.unity", scenes[0].path, "The Main showcase scene is the only build scene");
    }
}