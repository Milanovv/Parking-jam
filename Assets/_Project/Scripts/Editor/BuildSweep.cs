using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildSweep
{
    public const string ReportPath = "Builds/QA/BuildSweepReport.txt";
    public const string PlayerPath = "Builds/ParkingJam.exe";
    public const string SceneLinePrefix = "Scene: ";
    public const string SucceededMarker = "Build result: Succeeded";
    public const string ContentLinePrefix = "Content: ";
    public const string TextureLinePrefix = "Texture: ";
    public const string FootprintLinePrefix = "Footprint: ";
    public const string NoColliderLinePrefix = "Uncollided: ";

    private const string MainScenePath = "Assets/Scenes/Main.unity";

    private static readonly string[] AllowedRoots =
    {
        "Assets/_Project",
        "Assets/BrokenVector",
        "Assets/DenysAlmaral",
        "Assets/Palmov Island",
        "Assets/YughuesFreeConcreteMaterials",
        MainScenePath,
        "Assets/StreamingAssets",
        "Assets/Resources",
        "Assets/DefaultVolumeProfile.asset",
        "Assets/UniversalRenderPipelineGlobalSettings.asset",
        "Packages/"
    };

    private static readonly (string PrefabName, string ModelName)[] VehiclePrefabs =
    {
        ("Vehicle", "Car 1"),
        ("VehicleTruck", "Truck 1"),
        ("VehicleBus", "Bus")
    };

    private static readonly string[] SweptPrefabPaths =
    {
        CarPackAssets.PrefabPath,
        CarPackAssets.TruckPrefabPath,
        CarPackAssets.BusPrefabPath,
        BarrierAssets.BarrierPrefabPath,
        PeoplePackAssets.PedestrianPrefabPath,
        ConcretePackAssets.GroundPrefabPath,
        PalmovPackAssets.BackdropPrefabPath
    };

    public static IReadOnlyList<string> ContentPathPrefixes => AllowedRoots;

    public static IReadOnlyList<string> FootprintMarkers => VehiclePrefabs
        .Select(entry => FootprintLinePrefix + entry.PrefabName + " " + CarPackAssets.TileLength(entry.ModelName) + " tiles")
        .ToArray();

    public static IReadOnlyList<string> NoColliderMarkers => SweptPrefabPaths
        .Select(path => NoColliderLinePrefix + Path.GetFileNameWithoutExtension(path))
        .ToArray();

    public static void EnsureBuildScenes()
    {
        var current = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        if (current.Count == 1 && current[0] == MainScenePath) return;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        EnsureBuildScenes();
    }

    public static void Build()
    {
        EnsureBuildScenes();
        TextureSweepAssets.Ensure();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
            locationPathName = PlayerPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        ValidateAndWriteReport(report);
    }

    private static void ValidateAndWriteReport(BuildReport report)
    {
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + report.summary.result);

        var included = BuildContentPaths();
        var textures = included.Where(path => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D)).ToList();

        foreach (var texture in textures)
            VerifyTexture(texture);

        VerifyFootprints();
        VerifyNoPhysics();

        var lines = new List<string>
        {
            "Parking Jam build sweep report",
            SceneLinePrefix + EditorBuildSettings.scenes.Select(scene => scene.path).First(),
            SucceededMarker
        };
        lines.AddRange(included.Select(path => ContentLinePrefix + path));
        lines.AddRange(textures.Select(path => TextureLinePrefix + path + " BC7 max-2048"));
        lines.AddRange(FootprintMarkers);
        lines.AddRange(NoColliderMarkers);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllLines(ReportPath, lines);
    }

    private static List<string> BuildContentPaths()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
        var direntryPaths = new List<string>();
        foreach (var root in new[] { "Assets/StreamingAssets", "Assets/Resources" })
        {
            if (Directory.Exists(root))
                direntryPaths.AddRange(Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".meta")));
        }

        var all = scenePaths.Concat(direntryPaths).ToArray();
        var dependencies = AssetDatabase.GetDependencies(all, recursive: true)
            .Where(path => !path.EndsWith(".meta"))
            .ToList();

        foreach (var path in dependencies)
        {
            var allowed = AllowedRoots.Any(path.StartsWith);
            if (!allowed)
                throw new System.Exception(path + " is not sourced third-party or first-party content");
        }

        return dependencies;
    }

    private static void VerifyTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !TextureSweepAssets.IsPinnedAt(importer))
            throw new System.Exception(path + " is not BC7 at max 2048 for the PC build");
    }

    private static void VerifyFootprints()
    {
        const float tolerance = 0.1f;
        foreach (var entry in VehiclePrefabs)
        {
            var bounds = CarPackAssets.InstantiatedWorldBounds(entry.PrefabName);
            if (bounds == null)
                throw new System.Exception(entry.PrefabName + " is missing from the sweep");

            float extent = Mathf.Max(bounds.Value.size.x, bounds.Value.size.z);
            float expected = CarPackAssets.TileLength(entry.ModelName);
            if (Mathf.Abs(extent - expected) > tolerance)
                throw new System.Exception(entry.PrefabName + " footprint is " + extent + " tiles, expected " + expected);

            if (Mathf.Abs(bounds.Value.center.x) > tolerance || Mathf.Abs(bounds.Value.center.z) > tolerance)
                throw new System.Exception(entry.PrefabName + " is not grid-aligned: centre " + bounds.Value.center);
        }
    }

    private static void VerifyNoPhysics()
    {
        foreach (var path in SweptPrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new System.Exception(path + " is missing from the sweep");

            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new System.Exception(path + " carries a 3D collider - collision stays grid-space");
            if (prefab.GetComponentsInChildren<Collider2D>(true).Length > 0)
                throw new System.Exception(path + " carries a 2D collider - collision stays grid-space");
            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                throw new System.Exception(path + " carries a rigidbody - movement stays grid-space");
        }
    }
}