using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CarPackAssets
{
    public const string ModelsDir = "Assets/_Project/Packs/LowPolyCarPack/Models";
    public const string PaintsDir = "Assets/_Project/Materials/Paints";
    public const string PrefabPath = "Assets/_Project/Prefabs/Vehicle.prefab";

    private const float PaintSmoothness = 0.8f;
    private const float PaintMetallic = 0.6f;
    private const float ScaleEpsilon = 0.001f;
    private const int NormalizationPasses = 3;

    private static readonly string[] ModelNamesArray =
    {
        "Car 1", "Car 2", "Car 3", "Car 4", "Car 5", "Car 6",
        "Policecar", "Truck 1", "Truck 2", "Bus"
    };

    private static readonly Dictionary<string, int> TileLengths = new Dictionary<string, int>
    {
        { "Car 1", 1 }, { "Car 2", 1 }, { "Car 3", 1 }, { "Car 4", 1 }, { "Car 5", 1 }, { "Car 6", 1 },
        { "Policecar", 1 },
        { "Truck 1", 2 }, { "Truck 2", 2 },
        { "Bus", 3 }
    };

    private static readonly string[] PaintNamesArray = { "Blue", "Green", "Purple", "Red", "Silver", "Yellow" };

    private static readonly Color[] PaintColors =
    {
        new Color(0.10f, 0.40f, 0.70f),
        new Color(0.45f, 0.60f, 0.28f),
        new Color(0.55f, 0.35f, 0.60f),
        new Color(0.78f, 0.18f, 0.12f),
        new Color(0.62f, 0.62f, 0.64f),
        new Color(0.85f, 0.65f, 0.25f)
    };

    private static bool _ensuring;

    public static string[] CatalogModelNames => ModelNamesArray;

    public static string[] PaintNames => PaintNamesArray;

    public static int TileLength(string modelName)
    {
        return TileLengths.ContainsKey(modelName) ? TileLengths[modelName] : 1;
    }

    public static string ModelPath(string modelName)
    {
        return ModelsDir + "/" + modelName + ".fbx";
    }

    public static Material PaintMaterial(string paintName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(PaintsDir + "/" + paintName + ".mat");
    }

public static void Ensure()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsurePaintSet();
            EnsureModelScales();
            EnsureVehiclePrefab();
        }
        finally
        {
            _ensuring = false;
        }
    }

    private static void EnsurePaintSet()
    {
        EnsureFolder(PaintsDir);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        for (int i = 0; i < PaintNamesArray.Length; i++)
        {
            var path = PaintsDir + "/" + PaintNamesArray[i] + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;

            var material = new Material(shader);
            material.SetColor("_BaseColor", PaintColors[i]);
            material.SetFloat("_Smoothness", PaintSmoothness);
            material.SetFloat("_Metallic", PaintMetallic);
            AssetDatabase.CreateAsset(material, path);
        }
        AssetDatabase.SaveAssets();
    }

    private static void EnsureModelScales()
    {
        for (int pass = 0; pass < NormalizationPasses; pass++)
        {
            bool changed = false;
            foreach (var name in ModelNamesArray)
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

                var extent = LoadWorldExtent(ModelPath(name));
                if (extent <= 0f) continue;
                var targetScale = importer.globalScale * TileLength(name) / extent;
                if (Mathf.Abs(targetScale - importer.globalScale) <= ScaleEpsilon) continue;

                importer.globalScale = targetScale;
                importer.SaveAndReimport();
                changed = true;
            }
            if (!changed) break;
        }
    }

    private static void EnsureVehiclePrefab()
    {
        EnsureFolder(PrefabPath.Substring(0, PrefabPath.LastIndexOf('/')));
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            if (IsVehiclePrefabComplete(existing)) return;
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        var mesh = LoadFirstMesh(ModelPath("Car 1"));
        if (mesh == null) return;

        var root = new GameObject("Vehicle");
        root.AddComponent<Vehicle>();
        root.AddComponent<VehicleMovement>();

        var model = new GameObject("Model");
        model.transform.SetParent(root.transform);
        var filter = model.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PaintMaterial("Red");

        var bounds = mesh.bounds;
        var modelScale = 1f / Mathf.Max(bounds.size.x, bounds.size.z);
        model.transform.localScale = Vector3.one * modelScale;
        model.transform.rotation = bounds.size.x < bounds.size.z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

        var worldCenter = model.transform.rotation * (bounds.center * modelScale);
        var worldSize = bounds.size * modelScale;
        model.transform.localPosition = new Vector3(-worldCenter.x, -(worldCenter.y - worldSize.y * 0.5f), -worldCenter.z);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    private static bool IsVehiclePrefabComplete(GameObject prefab)
    {
        if (prefab.GetComponent<Vehicle>() == null) return false;
        if (prefab.GetComponent<VehicleMovement>() == null) return false;

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return false;
        try
        {
            var model = instance.transform.Find("Model");
            if (model == null) return false;

            var renderer = model.GetComponentInChildren<MeshRenderer>(true);
            var filter = model.GetComponentInChildren<MeshFilter>(true);
            if (renderer == null || filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                return false;

            var bounds = renderer.bounds;
            return Mathf.Abs(bounds.size.x - 1f) <= 0.1f
                && Mathf.Abs(bounds.center.x) <= 0.1f
                && Mathf.Abs(bounds.center.z) <= 0.1f;
        }
        finally
        {
            Object.DestroyImmediate(instance);
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
        AssetDatabase.CreateFolder(parent, leaf);
    }
}

public class CarPackPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var affected = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths);
        if (affected.Any(path => path.StartsWith(CarPackAssets.ModelsDir)))
            CarPackAssets.Ensure();
    }
}