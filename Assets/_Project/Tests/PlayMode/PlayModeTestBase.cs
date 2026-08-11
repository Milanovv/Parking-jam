using NUnit.Framework;
using UnityEngine;

public class PlayModeTestBase
{
    [TearDown]
    public void Teardown()
    {
        var objects = Object.FindObjectsByType<GameObject>();
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            if (obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad")
                Object.DestroyImmediate(obj);
        }
    }
}