using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConcretePackAssets
{
    public const string TexturesDir = "Assets/_Project/Packs/ConcreteTextures/Textures";
    public const string DiffusePath03 = TexturesDir + "/pattern03_diffuse.png";
    public const string NormalPath03 = TexturesDir + "/pattern03_normal.png";
    public const string DiffusePath07 = TexturesDir + "/pattern07_diffuse.png";
    public const string NormalPath07 = TexturesDir + "/pattern07_normal.png";
    public const string LotMaterialPath = "Assets/_Project/Materials/Ground/ParkingLotGround.mat";
    public const string ApronMaterialPath = "Assets/_Project/Materials/Ground/ParkingLotApron.mat";
    public const string GroundPrefabPath = "Assets/_Project/Prefabs/ParkingLotGround.prefab";
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string LotChildName = "LotFloor";
    public const string ApronChildName = "ExitApron";

    public const float LotMin = 0f;
    public const float LotMax = 12f;
    public const float LotSize = LotMax - LotMin;
    public const float ApronWidth = 4f;
    public const float GroundDepth = -0.02f;

    private const int MaxTextureSize = 2048;
    private const float MaterialSmoothness = 0.35f;

    public static readonly string[] DiffusePaths = { DiffusePath03, DiffusePath07 };
    public static readonly string[] NormalPaths = { NormalPath03, NormalPath07 };
    public static readonly string[] AllTexturePaths = { DiffusePath03, NormalPath03, DiffusePath07, NormalPath07 };

    private static readonly Quaternion FacingCamera = Quaternion.Euler(0f, 180f, 0f);
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

    private static void EnsureAssetsInternal()
    {
        EnsureImportSettings();
        EnsureMaterials();
        EnsureGroundPrefab();
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    private static void EnsureImportSettings()
    {
        foreach (var path in AllTexturePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            var isNormal = NormalPaths.Contains(path);

            if (isNormal && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }
            if (!isNormal && importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (importer.maxTextureSize != MaxTextureSize)
            {
                importer.maxTextureSize = MaxTextureSize;
                changed = true;
            }

            var settings = importer.GetPlatformTextureSettings("Standalone");
            if (!settings.overridden || settings.maxTextureSize != MaxTextureSize || settings.format != TextureImporterFormat.BC7)
            {
                settings.overridden = true;
                settings.maxTextureSize = MaxTextureSize;
                settings.format = TextureImporterFormat.BC7;
                importer.SetPlatformTextureSettings(settings);
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }
    }

    private static void EnsureMaterials()
    {
        EnsureFolder("Assets/_Project/Materials");
        EnsureFolder("Assets/_Project/Materials/Ground");

        EnsureMaterial(LotMaterialPath, DiffusePath03, NormalPath03, new Vector2(LotSize, LotSize));
        EnsureMaterial(ApronMaterialPath, DiffusePath07, NormalPath07, new Vector2(ApronWidth, LotSize));
    }

    private static void EnsureMaterial(string path, string diffusePath, string normalPath, Vector2 tiling)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.shader.name == "Universal Render Pipeline/Lit"
                && existing.mainTexture != null
                && existing.mainTextureScale == tiling
                && existing.GetTexture("_BumpMap") != null)
            {
                return;
            }
            AssetDatabase.DeleteAsset(path);
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var material = new Material(shader);
        material.SetTexture("_BaseMap", diffuse);
        material.mainTexture = diffuse;
        material.SetTexture("_BumpMap", normal);
        material.mainTextureScale = tiling;
        material.SetFloat("_Smoothness", MaterialSmoothness);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureGroundPrefab()
    {
        EnsureFolder("Assets/_Project/Prefabs");
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);
        if (existing != null && IsGroundComplete(existing))
        {
            if (AppliesPackMaterials(existing)) return;
            AssetDatabase.DeleteAsset(GroundPrefabPath);
        }

        var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        if (quad == null) return;

        var lotMaterial = AssetDatabase.LoadAssetAtPath<Material>(LotMaterialPath);
        var apronMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApronMaterialPath);
        if (lotMaterial == null || apronMaterial == null) return;

        var root = new GameObject("ParkingLotGround");
        AddQuad(root, LotChildName, new Vector3(LotSize * 0.5f + LotMin, LotSize * 0.5f + LotMin, GroundDepth),
            new Vector3(LotSize, LotSize, 1f), quad, lotMaterial);
        AddQuad(root, ApronChildName, new Vector3(LotMax + ApronWidth * 0.5f, LotSize * 0.5f + LotMin, GroundDepth),
            new Vector3(ApronWidth, LotSize, 1f), quad, apronMaterial);

        PrefabUtility.SaveAsPrefabAsset(root, GroundPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void AddQuad(GameObject parent, string name, Vector3 position, Vector3 scale,
        Mesh quad, Material material)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.position = position;
        child.transform.localScale = scale;
        child.transform.rotation = FacingCamera;

        var filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = quad;
        var renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private static bool IsGroundComplete(GameObject prefab)
    {
        if (prefab.transform.childCount != 2) return false;

        var lot = prefab.transform.Find(LotChildName);
        var apron = prefab.transform.Find(ApronChildName);
        if (lot == null || apron == null) return false;

        if (!IsQuadComplete(lot, new Vector3(LotSize * 0.5f + LotMin, LotSize * 0.5f + LotMin, GroundDepth),
                new Vector3(LotSize, LotSize, 1f))) return false;
        if (!IsQuadComplete(apron, new Vector3(LotMax + ApronWidth * 0.5f, LotSize * 0.5f + LotMin, GroundDepth),
                new Vector3(ApronWidth, LotSize, 1f))) return false;

        return true;
    }

    private static bool IsQuadComplete(Transform child, Vector3 position, Vector3 scale)
    {
        if (child.transform.position != position) return false;
        if (child.transform.localScale != scale) return false;
        if (Quaternion.Angle(child.transform.rotation, FacingCamera) > 0.01f) return false;

        var filter = child.GetComponent<MeshFilter>();
        var renderer = child.GetComponent<MeshRenderer>();
        if (filter == null || filter.sharedMesh == null) return false;
        if (renderer == null || renderer.sharedMaterial == null) return false;

        var bounds = filter.sharedMesh.bounds;
        return Mathf.Abs(bounds.size.x - 1f) < 0.01f
            && Mathf.Abs(bounds.size.y - 1f) < 0.01f;
    }

    private static bool AppliesPackMaterials(GameObject prefab)
    {
        var lotMaterial = AssetDatabase.LoadAssetAtPath<Material>(LotMaterialPath);
        var apronMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApronMaterialPath);
        var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length != 2) return false;

        var lot = renderers.FirstOrDefault(r => r.gameObject.name == LotChildName);
        var apron = renderers.FirstOrDefault(r => r.gameObject.name == ApronChildName);
        return lot != null && lot.sharedMaterial == lotMaterial
            && apron != null && apron.sharedMaterial == apronMaterial;
    }

    private static void EnsureMainScene()
    {
        if (!File.Exists(MainScenePath)) return;

        var guid = AssetDatabase.AssetPathToGUID(GroundPrefabPath);
        if (string.IsNullOrEmpty(guid)) return;
        if (File.ReadAllText(MainScenePath).Contains(guid)) return;

        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var ground = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);
        if (ground != null)
            PrefabUtility.InstantiatePrefab(ground, scene);

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
        var separator = path.LastIndexOf('/');
        var parent = path.Substring(0, separator);
        var leaf = path.Substring(separator + 1);
        EnsureFolder(parent);
        if (Directory.Exists(path)) return;
        AssetDatabase.CreateFolder(parent, leaf);
    }
}

public class ConcretePackPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var affected = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths);
        if (affected.Any(path => path.StartsWith(ConcretePackAssets.TexturesDir)))
            ConcretePackAssets.EnsureAssets();
    }
}