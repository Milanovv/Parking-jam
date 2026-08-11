using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BarrierAssets
{
    public const string BarrierPrefabPath = "Assets/_Project/Prefabs/Barrier.prefab";
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string FenceLeftChildName = "Fence Left";
    public const string FenceRightChildName = "Fence Right";
    public const string CrossbarChildName = "Crossbar";
    public static readonly Vector3 MainScenePosition = new Vector3(12f, 6f, 0f);

    private const string FenceLeftModel = "fence white left";
    private const string FenceRightModel = "fence white right";
    private const float OpeningWidth = 1f;
    private const float CrossbarThickness = 0.4f;

    private static bool _ensuring;

    public static void Ensure()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsureAssetsInternal();
            EnsureMainScene();
        }
        finally
        {
            _ensuring = false;
        }
    }

    public static void EnsureAssets()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsureAssetsInternal();
        }
        finally
        {
            _ensuring = false;
        }
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    private static void EnsureAssetsInternal()
    {
        PalmovPackAssets.EnsureAssets();
        CarPackAssets.Ensure();
        EnsureBarrierPrefab();
    }

    private static void EnsureBarrierPrefab()
    {
        EnsureFolder("Assets/_Project/Prefabs");
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPrefabPath);
        if (existing != null && IsBarrierComplete(existing)) return;

        var palmovMaterial = AssetDatabase.LoadAssetAtPath<Material>(PalmovPackAssets.MaterialPath);
        var redPaint = CarPackAssets.PaintMaterial("Red");
        var leftMesh = LoadFirstMesh(PalmovPackAssets.ModelPath(FenceLeftModel));
        var rightMesh = LoadFirstMesh(PalmovPackAssets.ModelPath(FenceRightModel));
        if (leftMesh == null || rightMesh == null || palmovMaterial == null || redPaint == null) return;

        var root = new GameObject("Barrier");

        var fenceLeft = BuildFence(FenceLeftChildName, leftMesh, palmovMaterial,
            -OpeningWidth * 0.5f, flushInnerEdgeAtMax: true);
        fenceLeft.transform.SetParent(root.transform, false);

        var fenceRight = BuildFence(FenceRightChildName, rightMesh, palmovMaterial,
            OpeningWidth * 0.5f, flushInnerEdgeAtMax: false);
        fenceRight.transform.SetParent(root.transform, false);

        float fenceSpan = Mathf.Max(leftMesh.bounds.size.x, leftMesh.bounds.size.z);
        float fenceHeight = Mathf.Max(leftMesh.bounds.size.z, rightMesh.bounds.size.z);

        var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossbar.name = CrossbarChildName;
        Object.DestroyImmediate(crossbar.GetComponent<Collider>());
        crossbar.transform.SetParent(root.transform, false);
        crossbar.transform.localPosition = new Vector3(0f, 0f, fenceHeight);
        crossbar.transform.localScale = new Vector3(fenceSpan, CrossbarThickness, CrossbarThickness);
        crossbar.GetComponent<MeshRenderer>().sharedMaterial = redPaint;

        var gate = root.AddComponent<BarrierGate>();
        gate.CrossbarPivot = crossbar.transform;

        PrefabUtility.SaveAsPrefabAsset(root, BarrierPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject BuildFence(string childName, Mesh mesh, Material material,
        float innerEdgeY, bool flushInnerEdgeAtMax)
    {
        var bounds = mesh.bounds;
        float y = flushInnerEdgeAtMax ? innerEdgeY - bounds.max.y : innerEdgeY - bounds.min.y;

        var fence = new GameObject(childName);
        fence.AddComponent<MeshFilter>().sharedMesh = mesh;
        fence.AddComponent<MeshRenderer>().sharedMaterial = material;
        fence.transform.localPosition = new Vector3(-bounds.center.x, y, -bounds.min.z);
        return fence;
    }

    private static bool IsBarrierComplete(GameObject prefab)
    {
        var gate = prefab.GetComponent<BarrierGate>();
        if (gate == null || gate.CrossbarPivot == null) return false;

        var fenceLeft = prefab.transform.Find(FenceLeftChildName);
        var fenceRight = prefab.transform.Find(FenceRightChildName);
        var crossbar = prefab.transform.Find(CrossbarChildName);
        if (fenceLeft == null || fenceRight == null || crossbar == null) return false;

        if (!HasMeshAtPath(fenceLeft, PalmovPackAssets.ModelPath(FenceLeftModel))) return false;
        if (!HasMeshAtPath(fenceRight, PalmovPackAssets.ModelPath(FenceRightModel))) return false;
        if (crossbar.GetComponentInChildren<MeshFilter>(true)?.sharedMesh == null) return false;

        var palmovMaterial = AssetDatabase.LoadAssetAtPath<Material>(PalmovPackAssets.MaterialPath);
        var redPaint = CarPackAssets.PaintMaterial("Red");
        if (fenceLeft.GetComponentInChildren<MeshRenderer>(true)?.sharedMaterial != palmovMaterial) return false;
        if (fenceRight.GetComponentInChildren<MeshRenderer>(true)?.sharedMaterial != palmovMaterial) return false;
        if (crossbar.GetComponentInChildren<MeshRenderer>(true)?.sharedMaterial != redPaint) return false;

        if (prefab.GetComponentInChildren<Collider>(true) != null) return false;
        if (prefab.GetComponentInChildren<Animator>(true) != null) return false;
        return true;
    }

    private static bool HasMeshAtPath(Transform child, string expectedPath)
    {
        var mesh = child.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
        return mesh != null && AssetDatabase.GetAssetPath(mesh) == expectedPath;
    }

    private static Mesh LoadFirstMesh(string modelPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        return prefab == null ? null : prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
    }

    private static void EnsureMainScene()
    {
        if (!File.Exists(MainScenePath)) return;

        var guid = AssetDatabase.AssetPathToGUID(BarrierPrefabPath);
        if (string.IsNullOrEmpty(guid)) return;
        if (File.ReadAllText(MainScenePath).Contains(guid)) return;

        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var barrier = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPrefabPath);
        if (barrier != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(barrier, scene) as GameObject;
            if (instance != null)
                instance.transform.position = MainScenePosition;
        }

        EditorSceneManager.SaveScene(scene);
        VerifyMainCameraTag(scene);

        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    private static void VerifyMainCameraTag(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "MainCamera") continue;
            if (root.tag == "MainCamera") return;
            root.tag = "MainCamera";
            EditorSceneManager.SaveScene(scene);
            return;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
