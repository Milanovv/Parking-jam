using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class CameraRigTests : PlayModeTestBase
{
    private const float PitchDegrees = 40f;

    [UnityTest]
    public IEnumerator ThreeD_Surfaces_RenderLit()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("No graphics device in batch mode; the opaque-pass config contract is covered by RenderPipelineTests.");
            yield break;
        }

        string litShader = Shader.Find("Universal Render Pipeline/Lit") != null
            ? "Universal Render Pipeline/Lit"
            : null;
        Assert.IsNotNull(litShader, "URP Lit shader is available");

        var cubeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeGo.transform.position = Vector3.zero;
        var material = new Material(Shader.Find(litShader));
        material.color = Color.red;
        cubeGo.GetComponent<Renderer>().sharedMaterial = material;

        var lightGo = new GameObject("Sun");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

        var camGo = new GameObject("LitProbe");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 60f;
        cam.transform.position = new Vector3(0f, 0f, -3f);
        cam.transform.rotation = Quaternion.identity;
        var rt = new RenderTexture(64, 64, 24);
        cam.targetTexture = rt;
        cam.Render();

        var tex = new Texture2D(64, 64, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        RenderTexture.active = null;
        Color center = tex.GetPixel(32, 32);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);

        Assert.That(center.r, Is.GreaterThan(0.5f), "Opaque 3D surfaces render lit through the pipeline");
        Assert.That(center.g, Is.LessThan(0.4f), "The lit surface keeps its albedo color");
        yield break;
    }

    [Test]
    public void CameraRig_FramesLotCentered_AtFortyDegreesPitch_NoYaw()
    {
        var gridGo = new GameObject("RigGrid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        var gridController = gridGo.AddComponent<GridController>();
        gridController.SetGridSize(8, 8);

        var camGo = new GameObject("RigCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        var rig = camGo.AddComponent<GameCameraRig>();

        rig.Frame(gridController);

        Quaternion expected = Quaternion.Euler(PitchDegrees, 0f, 0f);
        Assert.That(Quaternion.Angle(rig.transform.rotation, expected), Is.LessThan(0.5f), "Camera pitches down at ~40 degrees");
        Assert.That(Mathf.Abs(rig.transform.eulerAngles.y), Is.LessThan(0.1f), "No yaw");
        Assert.That(Mathf.Abs(rig.transform.eulerAngles.z), Is.LessThan(0.1f), "No roll");

        Vector3 lotCenter = gridController.CellToWorld(new Vector3Int(4, 4, 0));
        Assert.That(Mathf.Abs(rig.transform.position.x - lotCenter.x), Is.LessThan(0.01f), "Rig stays centred on the lot laterally");
        Assert.That(rig.transform.position.z, Is.LessThan(lotCenter.z), "Rig stands behind the lot on the -Z side");
        Assert.That(rig.transform.position.y, Is.GreaterThan(lotCenter.y), "Rig looks down onto the lot");

        Vector3 screenCenter = cam.WorldToScreenPoint(lotCenter);
        float tolerance = 3f;
        Assert.That(Mathf.Abs(screenCenter.x - Screen.width / 2f), Is.LessThan(tolerance), "The lot centre lands mid-screen horizontally");
        Assert.That(Mathf.Abs(screenCenter.y - Screen.height / 2f), Is.LessThan(tolerance), "The lot centre lands mid-screen vertically");

        Assert.IsFalse(cam.orthographic, "The rig drives a perspective camera");
    }
}