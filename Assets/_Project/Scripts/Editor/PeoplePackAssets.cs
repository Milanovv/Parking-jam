using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PeoplePackAssets
{
    public const string ModelsDir = "Assets/_Project/Packs/People/Models";
    public const string TexturePath = "Assets/_Project/Packs/People/Textures/people_pal.png";
    public const string MaterialPath = "Assets/_Project/Materials/People/PeoplePalette.mat";
    public const string PedestrianPrefabPath = "Assets/_Project/Prefabs/Pedestrian.prefab";
    public const string MainScenePath = "Assets/Scenes/Main.unity";

    private const float ScaleEpsilon = 0.001f;
    private const float FootprintTolerance = 0.05f;
    private const int NormalizationPasses = 3;
    private const float MaterialSmoothness = 0.3f;
    private const float PedestrianFootprint = 1f;

    private static readonly string[] CatalogArray =
    {
        "casual_Female_G",
        "casual_Male_G",
        "casual_Female_K",
        "casual_Male_K",
        "elder_Female_A",
        "little_boy_B",
        "Doctor_Male_B",
        "police_Female_A"
    };

    private static readonly string[] CategoryFolders =
    {
        "city", "downtown", "elder", "little_kids", "professions"
    };

    private static bool _ensuring;

    public static IReadOnlyCollection<string> CatalogModelNames => CatalogArray;

    public static string ModelPath(string modelName)
    {
        foreach (var category in CategoryFolders)
        {
            var path = ModelsDir + "/" + category + "/" + modelName + ".fbx";
            if (File.Exists(path)) return path;
        }
        return ModelsDir + "/" + modelName + ".fbx";
    }

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
        EnsureMaterial();
        EnsureModelImports();
        EnsurePedestrianPrefab();
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    private static void EnsureMaterial()
    {
        EnsureFolder("Assets/_Project/Materials");
        EnsureFolder("Assets/_Project/Materials/People");
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            if (existing.shader.name == "Universal Render Pipeline/Lit" && existing.mainTexture != null) return;
            AssetDatabase.DeleteAsset(MaterialPath);
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var material = new Material(shader);
        material.SetTexture("_BaseMap", texture);
        material.mainTexture = texture;
        material.SetFloat("_Smoothness", MaterialSmoothness);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureModelImports()
    {
        for (int pass = 0; pass < NormalizationPasses; pass++)
        {
            bool changed = false;
            foreach (var name in CatalogArray)
            {
                var importer = AssetImporter.GetAtPath(ModelPath(name)) as ModelImporter;
                if (importer == null) continue;

                if (importer.animationType != ModelImporterAnimationType.None)
                {
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.importAnimation = false;
                    importer.SaveAndReimport();
                    changed = true;
                    continue;
                }

                if (importer.importAnimation)
                {
                    importer.importAnimation = false;
                    importer.SaveAndReimport();
                    changed = true;
                    continue;
                }

                if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.SaveAndReimport();
                    changed = true;
                    continue;
                }

                if (importer.useFileScale)
                {
                    importer.useFileScale = false;
                    importer.SaveAndReimport();
                    changed = true;
                    continue;
                }

                var extent = LoadWorldExtent(ModelPath(name));
                if (extent <= 0f) continue;
                var targetScale = importer.globalScale * PedestrianFootprint / extent;
                if (Mathf.Abs(targetScale - importer.globalScale) <= ScaleEpsilon) continue;

                importer.globalScale = targetScale;
                importer.SaveAndReimport();
                changed = true;
            }
            if (!changed) break;
        }
    }

    private static void EnsurePedestrianPrefab()
    {
        EnsureFolder("Assets/_Project/Prefabs");
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PedestrianPrefabPath);
        if (existing != null && IsPedestrianComplete(existing)) return;

        var mesh = LoadFirstMesh(ModelPath(CatalogArray[0]));
        if (mesh == null) return;

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null) return;

        var root = new GameObject("Pedestrian");
        var model = new GameObject("Model");
        model.transform.SetParent(root.transform);

        var filter = model.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        var bounds = mesh.bounds;
        var modelScale = PedestrianFootprint / Mathf.Max(bounds.size.x, bounds.size.z);
        model.transform.localScale = Vector3.one * modelScale;
        model.transform.rotation = bounds.size.x < bounds.size.z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

        var worldCenter = model.transform.rotation * (bounds.center * modelScale);
        var worldSize = bounds.size * modelScale;
        model.transform.localPosition = new Vector3(-worldCenter.x, -(worldCenter.y - worldSize.y * 0.5f), -worldCenter.z);

        PrefabUtility.SaveAsPrefabAsset(root, PedestrianPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static bool IsPedestrianComplete(GameObject prefab)
    {
        var filter = prefab.GetComponentInChildren<MeshFilter>(true);
        var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
        if (filter == null || filter.sharedMesh == null) return false;
        if (renderer == null || renderer.sharedMaterial == null) return false;

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (renderer.sharedMaterial != material) return false;

        var instance = Object.Instantiate(prefab);
        try
        {
            var live = instance.GetComponentInChildren<MeshRenderer>(true);
            var extent = Mathf.Max(live.bounds.size.x, live.bounds.size.z);
            return Mathf.Abs(extent - PedestrianFootprint) <= FootprintTolerance;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void EnsureMainScene()
    {
        if (!File.Exists(MainScenePath)) return;

        var guid = AssetDatabase.AssetPathToGUID(PedestrianPrefabPath);
        if (string.IsNullOrEmpty(guid)) return;
        if (File.ReadAllText(MainScenePath).Contains(guid)) return;

        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var pedestrian = AssetDatabase.LoadAssetAtPath<GameObject>(PedestrianPrefabPath);
        if (pedestrian != null)
            PrefabUtility.InstantiatePrefab(pedestrian, scene);

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

    private static Mesh LoadFirstMesh(string modelPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        return prefab == null ? null : prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
    }

    private static float LoadWorldExtent(string modelPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (prefab == null) return 0f;
        var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
        if (renderer == null) return 0f;
        return Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
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

public class PeoplePackPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var affected = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths);
        if (affected.Any(path => path.StartsWith(PeoplePackAssets.ModelsDir)))
            PeoplePackAssets.EnsureAssets();
    }
}
