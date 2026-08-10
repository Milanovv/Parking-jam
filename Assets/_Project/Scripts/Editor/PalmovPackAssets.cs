using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PalmovPackAssets
{
    public const string ModelsDir = "Assets/_Project/Packs/PalmovHouses/Models";
    public const string TexturePath = "Assets/_Project/Packs/PalmovHouses/Textures/texture_main.png";
    public const string MaterialPath = "Assets/_Project/Materials/Environment/PalmovMain.mat";
    public const string BackdropPrefabPath = "Assets/_Project/Prefabs/Backdrop.prefab";
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string SunName = "Sun";

    private const float ScaleEpsilon = 0.001f;
    private const int NormalizationPasses = 3;
    private const float MaterialSmoothness = 0.3f;

    private const float HouseFootprint = 6f;
    private const float TreeFootprint = 1.8f;
    private const float PlantFootprint = 1f;
    private const float FenceFootprint = 3.5f;
    private const float LamppostFootprint = 0.9f;
    private const float SmallPropFootprint = 1.2f;
    private const float GroundFootprint = 12f;
    private const float RoadFootprint = 6f;

    private static readonly Dictionary<string, float> Footprints = new Dictionary<string, float>
    {
        { "big cottage 1 floor new", HouseFootprint },
        { "brewery house", HouseFootprint },
        { "catholic temple", HouseFootprint },
        { "city hall", HouseFootprint },
        { "cute house", HouseFootprint },
        { "pizzeria house", HouseFootprint },
        { "post office", HouseFootprint },
        { "cottage tree 1", TreeFootprint },
        { "cottage tree 2", TreeFootprint },
        { "fir tree 1", TreeFootprint },
        { "fir tree 2", TreeFootprint },
        { "fir tree group 1", 2.6f },
        { "fir tree group 2", 2.6f },
        { "potted tree", 1.2f },
        { "round tree", TreeFootprint },
        { "spruce border 1", TreeFootprint },
        { "spruce border 2", TreeFootprint },
        { "tree", TreeFootprint },
        { "bush 1", PlantFootprint },
        { "bush 2", PlantFootprint },
        { "bush 3", PlantFootprint },
        { "plants", PlantFootprint },
        { "plants 2", PlantFootprint },
        { "plants 3", PlantFootprint },
        { "bench", SmallPropFootprint },
        { "bottle", 0.6f },
        { "box with bottles", 0.7f },
        { "chair", 0.9f },
        { "dog house", 1.4f },
        { "fence white left", FenceFootprint },
        { "fence white right", FenceFootprint },
        { "fountain", 2.2f },
        { "lamppost", LamppostFootprint },
        { "table", SmallPropFootprint },
        { "tennis net", 2f },
        { "trash can", 0.8f },
        { "asphalt ground large", 14f },
        { "asphalt ground side turn", 10f },
        { "asphalt ground small wide", 10f },
        { "asphalt ground small", GroundFootprint },
        { "asphalt ground wide side turn", 10f },
        { "lake", GroundFootprint },
        { "land", GroundFootprint },
        { "tennis court brown", 10f },
        { "paved road all directions", RoadFootprint },
        { "paved road pedestrian crossing", RoadFootprint },
        { "paved road straight turn", RoadFootprint },
        { "paved road straight", RoadFootprint },
        { "paved road turn", RoadFootprint }
    };

    public const float LotMax = 12f;
    public const float LotMin = 0f;
    public const float Margin = 1f;

    public struct BackdropEntry
    {
        public readonly string Model;
        public readonly Vector3 Position;
        public readonly float YawY;

        public BackdropEntry(string model, Vector3 position, float yawY)
        {
            Model = model;
            Position = position;
            YawY = yawY;
        }
    }

    private static float HouseRowY => LotMax + 4f;
    private static float FenceRowY => LotMax + Margin;
    private static float LampRowY => LotMin - Margin;

    private static readonly (string Model, Vector3 Position, float YawY)[] BackdropLayout =
    {
        ("big cottage 1 floor new", new Vector3(-6.5f, HouseRowY, 0f), 0f),
        ("brewery house", new Vector3(-0.25f, HouseRowY, 0f), 0f),
        ("city hall", new Vector3(6f, HouseRowY, 0f), 0f),
        ("pizzeria house", new Vector3(12.25f, HouseRowY, 0f), 0f),
        ("post office", new Vector3(18.5f, HouseRowY, 0f), 0f),
        ("fence white left", new Vector3(-2f, FenceRowY, 0f), 0f),
        ("fence white right", new Vector3(2f, FenceRowY, 0f), 0f),
        ("fence white left", new Vector3(6f, FenceRowY, 0f), 0f),
        ("fence white right", new Vector3(10f, FenceRowY, 0f), 0f),
        ("fence white left", new Vector3(14f, FenceRowY, 0f), 0f),
        ("fence white left", new Vector3(LotMin - Margin - 0.75f, 3f, 0f), 0f),
        ("fence white left", new Vector3(LotMin - Margin - 0.75f, 8f, 0f), 0f),
        ("fence white right", new Vector3(LotMax + Margin + 0.75f, 3f, 0f), 0f),
        ("fence white right", new Vector3(LotMax + Margin + 0.75f, 8f, 0f), 0f),
        ("lamppost", new Vector3(1.5f, LampRowY, 0f), 0f),
        ("lamppost", new Vector3(4.5f, LampRowY, 0f), 0f),
        ("lamppost", new Vector3(7.5f, LampRowY, 0f), 0f),
        ("lamppost", new Vector3(10.5f, LampRowY, 0f), 0f),
        ("round tree", new Vector3(-1.5f, -1.5f, 0f), 0f),
        ("cottage tree 1", new Vector3(LotMax + 1.5f, -1.5f, 0f), 0f),
        ("fir tree 1", new Vector3(-1.5f, 4f, 0f), 0f),
        ("fir tree 2", new Vector3(LotMax + 1.5f, 4f, 0f), 0f),
        ("spruce border 1", new Vector3(-1.5f, 9f, 0f), 0f),
        ("spruce border 2", new Vector3(LotMax + 1.5f, 9f, 0f), 0f),
        ("bench", new Vector3(-2.5f, -1.5f, 0f), 0f),
        ("bench", new Vector3(LotMax + 2.5f, -1.5f, 0f), 0f),
        ("trash can", new Vector3(6f, -2f, 0f), 0f)
    };

    public static IReadOnlyList<BackdropEntry> BackdropEntries { get; } =
        BackdropLayout.Select(e => new BackdropEntry(e.Model, e.Position, e.YawY)).ToList();

    private static bool _ensuring;

    public static IReadOnlyCollection<string> CatalogModelNames => Footprints.Keys;

    public static float FootprintTarget(string modelName)
    {
        return Footprints.TryGetValue(modelName, out float target) ? target : 0f;
    }

    public static string ModelPath(string modelName)
    {
        return ModelPathForCategory(modelName);
    }

    private static string ModelPathForCategory(string modelName)
    {
        var categories = new[]
        {
            "Environment", "Grounds", "Houses", "Plants", "Roads", "Trees"
        };
        foreach (var category in categories)
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
        EnsureBackdropPrefab();
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    private static void EnsureMaterial()
    {
        EnsureFolder("Assets/_Project/Materials");
        EnsureFolder("Assets/_Project/Materials/Environment");
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
            foreach (var name in Footprints.Keys)
            {
                var importer = AssetImporter.GetAtPath(ModelPath(name)) as ModelImporter;
                if (importer == null) continue;

                if (importer.useFileScale)
                {
                    importer.useFileScale = false;
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

                var extent = LoadWorldExtent(ModelPath(name));
                if (extent <= 0f) continue;
                var targetScale = importer.globalScale * Footprints[name] / extent;
                if (Mathf.Abs(targetScale - importer.globalScale) <= ScaleEpsilon) continue;

                importer.globalScale = targetScale;
                importer.SaveAndReimport();
                changed = true;
            }
            if (!changed) break;
        }
    }

    private static void EnsureBackdropPrefab()
    {
        EnsureFolder("Assets/_Project/Prefabs");
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BackdropPrefabPath);
        if (existing != null && IsBackdropComplete())
        {
            if (AppliesSharedMaterial(existing)) return;
            AssetDatabase.DeleteAsset(BackdropPrefabPath);
        }

        var meshes = new Mesh[BackdropLayout.Length];
        for (int i = 0; i < BackdropLayout.Length; i++)
        {
            meshes[i] = LoadFirstMesh(ModelPath(BackdropLayout[i].Model));
            if (meshes[i] == null) return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null) return;
        var root = new GameObject("Backdrop");
        for (int i = 0; i < BackdropLayout.Length; i++)
        {
            var entry = BackdropLayout[i];
            var child = new GameObject(entry.Model + " " + i);
            child.transform.SetParent(root.transform);
            child.transform.position = entry.Position;
            child.transform.rotation = Quaternion.Euler(0f, entry.YawY, 0f);

            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = meshes[i];
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        PrefabUtility.SaveAsPrefabAsset(root, BackdropPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static bool IsBackdropComplete()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackdropPrefabPath);
        if (prefab == null) return false;
        if (prefab.transform.childCount != BackdropLayout.Length) return false;
        for (int i = 0; i < BackdropLayout.Length; i++)
        {
            var child = prefab.transform.GetChild(i);
            if (child.name != BackdropLayout[i].Model + " " + i) return false;
            if (child.transform.position != BackdropLayout[i].Position) return false;
            var filter = child.GetComponentInChildren<MeshFilter>(true);
            var renderer = child.GetComponentInChildren<MeshRenderer>(true);
            if (filter == null || filter.sharedMesh == null) return false;
            if (renderer == null || renderer.sharedMaterial == null) return false;
        }
        return true;
    }

    private static bool AppliesSharedMaterial(GameObject prefab)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        return renderers.All(r => r.sharedMaterial == material);
    }

    private static void EnsureMainScene()
    {
        if (!File.Exists(MainScenePath)) return;

        var guid = AssetDatabase.AssetPathToGUID(BackdropPrefabPath);
        if (string.IsNullOrEmpty(guid)) return;
        if (File.Exists(MainScenePath) && File.ReadAllText(MainScenePath).Contains(guid)) return;

        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var backdrop = AssetDatabase.LoadAssetAtPath<GameObject>(BackdropPrefabPath);
        if (backdrop != null)
        {
            PrefabUtility.InstantiatePrefab(backdrop, scene);
            EnsureSun(scene);
        }

        EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    private static void EnsureSun(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == SunName) return;
        }
        var sun = new GameObject(SunName);
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
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
        AssetDatabase.CreateFolder(parent, leaf);
    }
}

public class PalmovPackPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var affected = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths);
        if (affected.Any(path => path.StartsWith(PalmovPackAssets.ModelsDir)))
            PalmovPackAssets.EnsureAssets();
    }
}