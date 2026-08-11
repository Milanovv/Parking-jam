using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BarrierAssetsTests
{
    private const float ScaleTolerance = 0.1f;

    [Test]
    public void Ensure_ComposesBarrier_FromPalmovFences_RedCrossbar_AndGateView()
    {
        BarrierAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierAssets.BarrierPrefabPath);
        Assert.IsNotNull(prefab, "Barrier prefab is authored");

        var gate = prefab.GetComponent<BarrierGate>();
        Assert.IsNotNull(gate, "The prefab carries the gate view");
        Assert.IsNotNull(gate.CrossbarPivot, "The gate view is wired to the crossbar pivot");

        var fenceLeft = prefab.transform.Find(BarrierAssets.FenceLeftChildName);
        var fenceRight = prefab.transform.Find(BarrierAssets.FenceRightChildName);
        var crossbar = prefab.transform.Find(BarrierAssets.CrossbarChildName);
        Assert.IsNotNull(fenceLeft, "The left fence segment is composed");
        Assert.IsNotNull(fenceRight, "The right fence segment is composed");
        Assert.IsNotNull(crossbar, "The crossbar is composed");

        var palmovMaterial = AssetDatabase.LoadAssetAtPath<Material>(PalmovPackAssets.MaterialPath);
        var redPaint = CarPackAssets.PaintMaterial("Red");
        Assert.IsNotNull(palmovMaterial, "Palmov shared material exists");
        Assert.IsNotNull(redPaint, "The Red paint material exists");

        var leftMesh = fenceLeft.GetComponentInChildren<MeshFilter>(true);
        var rightMesh = fenceRight.GetComponentInChildren<MeshFilter>(true);
        var crossbarMesh = crossbar.GetComponentInChildren<MeshFilter>(true);
        Assert.IsNotNull(leftMesh?.sharedMesh, "The left fence carries Palmov geometry");
        Assert.IsNotNull(rightMesh?.sharedMesh, "The right fence carries Palmov geometry");
        Assert.IsNotNull(crossbarMesh?.sharedMesh, "The crossbar is a primitive");

        Assert.That(AssetDatabase.GetAssetPath(leftMesh.sharedMesh),
            Does.Contain("/Environment/fence white left.fbx"),
            "The left fence comes from the Palmov pack");
        Assert.That(AssetDatabase.GetAssetPath(rightMesh.sharedMesh),
            Does.Contain("/Environment/fence white right.fbx"),
            "The right fence comes from the Palmov pack");

        Assert.AreEqual(palmovMaterial, fenceLeft.GetComponentInChildren<MeshRenderer>(true).sharedMaterial,
            "The fence segments share the Palmov material");
        Assert.AreEqual(palmovMaterial, fenceRight.GetComponentInChildren<MeshRenderer>(true).sharedMaterial);
        Assert.AreEqual(redPaint, crossbar.GetComponentInChildren<MeshRenderer>(true).sharedMaterial,
            "The crossbar is palette-matched to the paint workflow");
    }

    [Test]
    public void Barrier_Crossbar_SpansOneTileGap_AboveGround()
    {
        BarrierAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierAssets.BarrierPrefabPath);
        Assert.IsNotNull(prefab, "Barrier prefab is authored");

        var instance = Object.Instantiate(prefab);
        try
        {
            var crossbar = instance.transform.Find(BarrierAssets.CrossbarChildName).GetComponentInChildren<MeshRenderer>(true);
            var left = instance.transform.Find(BarrierAssets.FenceLeftChildName).GetComponentInChildren<MeshRenderer>(true);
            var right = instance.transform.Find(BarrierAssets.FenceRightChildName).GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(crossbar, "The crossbar renders");
            Assert.IsNotNull(left, "The left fence renders");
            Assert.IsNotNull(right, "The right fence renders");

            float crossbarSpan = Mathf.Max(crossbar.bounds.size.x, crossbar.bounds.size.z);
            float fenceSpan = Mathf.Max(left.bounds.size.x, left.bounds.size.z);
            Assert.AreEqual(fenceSpan, crossbarSpan, ScaleTolerance,
                "The crossbar spans the same width as one fence segment");

            float gap = right.bounds.min.y - left.bounds.max.y;
            Assert.AreEqual(1f, gap, ScaleTolerance, "The gate opening is one tile wide");

            Assert.Greater(crossbar.bounds.min.y, left.bounds.max.y - ScaleTolerance,
                "The crossbar sits above the fence segments");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Barrier_UsesNoRig_NoAnimation_AndNoPhysics()
    {
        BarrierAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierAssets.BarrierPrefabPath);
        Assert.IsNotNull(prefab, "Barrier prefab is authored");

        Assert.IsNull(prefab.GetComponentInChildren<Animator>(true), "The gate carries no Animator");
        Assert.IsNull(prefab.GetComponentInChildren<Collider>(true), "The gate has no physics collider");
        Assert.IsNull(prefab.GetComponentInChildren<Rigidbody>(true), "The gate has no physics body");

        var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
        Assert.AreEqual(1, behaviours.Length, "The only script on the gate is the gate view");
        Assert.IsInstanceOf<BarrierGate>(behaviours[0]);
    }

    [Test]
    public void MainScene_ContainsBarrierInstance_AndKeepsCameraTag()
    {
        BarrierAssets.Ensure();

        var guid = AssetDatabase.AssetPathToGUID(BarrierAssets.BarrierPrefabPath);
        Assert.IsNotNull(guid, "Barrier prefab has a GUID");
        Assert.That(System.IO.File.ReadAllText(BarrierAssets.MainScenePath),
            Does.Contain(guid), "The main scene holds a Barrier prefab instance");

        var scene = EditorSceneManager.OpenScene(BarrierAssets.MainScenePath, OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var barrier = roots.FirstOrDefault(r =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(r))
                    == BarrierAssets.BarrierPrefabPath);
            Assert.IsNotNull(barrier, "The Barrier prefab instance is in the main scene");
            Assert.AreEqual(BarrierAssets.MainScenePosition, barrier.transform.position,
                "The gate stands at the inner-grid boundary, at the lane's mid-row");

            var camera = roots.FirstOrDefault(r => r.name == "MainCamera");
            Assert.IsNotNull(camera, "The main scene still has its camera");
            Assert.AreEqual("MainCamera", camera.tag, "Scene surgery did not reset the camera tag");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [Test]
    public void Ensure_IsIdempotent_SecondPassLeavesPrefabComplete()
    {
        BarrierAssets.Ensure();
        BarrierAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierAssets.BarrierPrefabPath);
        Assert.IsNotNull(prefab, "Barrier prefab survives a second Ensure");
        Assert.IsNotNull(prefab.GetComponent<BarrierGate>()?.CrossbarPivot, "The gate view stays wired");
    }
}
