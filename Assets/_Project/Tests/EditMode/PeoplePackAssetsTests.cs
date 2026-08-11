using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PeoplePackAssetsTests
{
    private const float ScaleTolerance = 0.05f;

    [Test]
    public void Catalog_CoversAllEightPalettePeople_ExcludesConstructionDisabilityAndProps()
    {
        var expected = new[]
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

        var catalog = PeoplePackAssets.CatalogModelNames;

        Assert.AreEqual(expected.Length, catalog.Count, "The catalog covers every kept character");
        Assert.That(catalog.Distinct().Count(), Is.EqualTo(catalog.Count), "Catalog entries are unique");
        foreach (var name in expected)
        {
            Assert.IsTrue(catalog.Contains(name), name + " is catalogued");
        }
        CollectionAssert.DoesNotContain(catalog, "tradesperson_man", "Construction tradesperson is pruned");
        CollectionAssert.DoesNotContain(catalog, "worker_Male_constructor_B", "Construction worker is pruned");
        CollectionAssert.DoesNotContain(catalog, "prostheticLeg_girl_ani", "Animated disability variant is pruned");
    }

    [Test]
    public void PrunedPack_KeepsOnlyModelsAndPaletteTexture_NoAnimationsDemoScenesScriptsOrPrefabs()
    {
        var packDir = new DirectoryInfo(PeoplePackAssets.ModelsDir).Parent.FullName;
        var allFiles = Directory.GetFiles(packDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .Select(f => f.Replace('\\', '/').Replace(packDir.Replace('\\', '/') + "/", ""))
            .ToList();

        var expected = new[]
        {
            "Readme.md",
            "Models/city/casual_Female_G.fbx",
            "Models/city/casual_Male_G.fbx",
            "Models/downtown/casual_Female_K.fbx",
            "Models/downtown/casual_Male_K.fbx",
            "Models/elder/elder_Female_A.fbx",
            "Models/little_kids/little_boy_B.fbx",
            "Models/professions/Doctor_Male_B.fbx",
            "Models/professions/police_Female_A.fbx",
            "Textures/people_pal.png"
        };

        CollectionAssert.AreEquivalent(expected, allFiles,
            "The pack is pruned to models, the shared palette texture, and the ReadMe");

        Assert.IsFalse(Directory.Exists(packDir + "/Animations"), "No Animations folder");
        Assert.IsFalse(Directory.Exists(packDir + "/Demo_Scenes"), "No Demo_Scenes folder");
        Assert.IsFalse(Directory.Exists(packDir + "/Scripts"), "No Scripts folder");
        Assert.IsFalse(Directory.Exists(packDir + "/Prefabs"), "No pack prefabs folder");
        Assert.IsFalse(Directory.Exists(packDir + "/Materials"), "No pack materials folder");
        Assert.IsFalse(Directory.Exists(packDir + "/URP&Built-in"), "No conversion packages folder");

        Assert.IsFalse(allFiles.Any(f => f.EndsWith(".controller")), "No animator controllers");
        Assert.IsFalse(allFiles.Any(f => f.EndsWith(".unity")), "No demo scenes");
        Assert.IsFalse(allFiles.Any(f => f.EndsWith(".cs")), "No pack scripts");
    }

    [Test]
    public void Ensure_ImportsEachModel_WithoutRig_Animation_OrMaterials()
    {
        PeoplePackAssets.Ensure();

        foreach (var name in PeoplePackAssets.CatalogModelNames)
        {
            var path = PeoplePackAssets.ModelPath(name);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Assert.IsNotNull(importer, name + " imports as a model");

            Assert.AreEqual(ModelImporterAnimationType.None, importer.animationType,
                name + " imports with no rig (humanoid/generic rigs are pruned per ticket)");
            Assert.IsFalse(importer.importAnimation, name + " imports no animation clips");
            Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode,
                name + " extracts no pack materials");
            Assert.IsFalse(importer.useFileScale, name + " uses normalized scale, not file scale");

            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(imported, name + " imported");
            Assert.IsNull(imported.GetComponentInChildren<Animator>(true), name + " carries no Animator");
            Assert.IsNull(imported.GetComponentInChildren<SkinnedMeshRenderer>(true),
                name + " is a plain mesh in bind pose, not a skinned renderer");
        }
    }

    [Test]
    public void Ensure_NormalizesEachModel_ToOneTileFootprint()
    {
        PeoplePackAssets.Ensure();

        foreach (var name in PeoplePackAssets.CatalogModelNames)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeoplePackAssets.ModelPath(name));
            Assert.IsNotNull(prefab, name + " imported");
            var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, name + " renders a mesh");
            var extent = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
            Assert.AreEqual(1f, extent, ScaleTolerance, name + " occupies a one-tile footprint per D8");
        }
    }

    [Test]
    public void PeoplePaletteMaterial_IsUrpLit_WithPackPaletteTexture()
    {
        PeoplePackAssets.Ensure();

        var material = AssetDatabase.LoadAssetAtPath<Material>(PeoplePackAssets.MaterialPath);
        Assert.IsNotNull(material, "PeoplePalette material is authored");
        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), "Palette material is URP lit");
        Assert.IsNotNull(material.mainTexture, "Palette material carries the pack palette texture");
        Assert.That(AssetDatabase.GetAssetPath(material.mainTexture), Is.EqualTo(PeoplePackAssets.TexturePath),
            "Texture comes from the City People pack");
        Assert.That(material.GetFloat("_Smoothness"), Is.LessThan(0.6f), "Clothes read as matte, not gloss");
        Assert.That(material.GetFloat("_Metallic"), Is.LessThan(0.1f), "Clothes are non-metallic");
    }

    [Test]
    public void PedestrianPrefab_IsComposedFromCatalogModel_SharesPalette_NoScriptsNoAnimatorNoPhysics()
    {
        PeoplePackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeoplePackAssets.PedestrianPrefabPath);
        Assert.IsNotNull(prefab, "Pedestrian prefab is authored");

        Assert.IsNull(prefab.GetComponent<Animator>(), "Prefab root has no Animator");
        Assert.IsNull(prefab.GetComponent<MonoBehaviour>(), "Prefab root has no scripts");

        var filter = prefab.GetComponentInChildren<MeshFilter>(true);
        var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
        Assert.IsNotNull(filter, "Prefab carries a mesh");
        Assert.IsNotNull(filter.sharedMesh, "Prefab has geometry");
        Assert.IsNotNull(renderer, "Prefab renders");
        Assert.IsNotNull(renderer.sharedMaterial, "Prefab is painted");

        var material = AssetDatabase.LoadAssetAtPath<Material>(PeoplePackAssets.MaterialPath);
        Assert.AreEqual(material, renderer.sharedMaterial, "Prefab shares the PeoplePalette material");

        Assert.IsNull(prefab.GetComponentInChildren<Animator>(true), "Prefab carries no Animator anywhere");
        Assert.IsNull(prefab.GetComponentInChildren<MonoBehaviour>(true), "Prefab carries no scripts anywhere");
        Assert.IsNull(prefab.GetComponentInChildren<Collider>(true), "Prefab has no physics collider");
        Assert.IsNull(prefab.GetComponentInChildren<Rigidbody>(true), "Prefab has no physics body");
    }

    [Test]
    public void PedestrianPrefab_OccupiesOneTileFootprint()
    {
        PeoplePackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeoplePackAssets.PedestrianPrefabPath);
        Assert.IsNotNull(prefab, "Pedestrian prefab is authored");

        var instance = Object.Instantiate(prefab);
        try
        {
            var renderer = instance.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, "Prefab renders a mesh");
            var extent = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
            Assert.AreEqual(1f, extent, ScaleTolerance, "Pedestrian occupies a one-tile footprint per D8");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MainScene_ContainsPedestrianInstance_AndKeepsCameraTag()
    {
        PeoplePackAssets.Ensure();

        var scene = EditorSceneManager.OpenScene(PeoplePackAssets.MainScenePath, OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var pedestrian = roots.FirstOrDefault(r =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(r))
                    == PeoplePackAssets.PedestrianPrefabPath);
            Assert.IsNotNull(pedestrian, "The Pedestrian prefab instance is in the main scene");

            var camera = roots.FirstOrDefault(r => r.name == "MainCamera");
            Assert.IsNotNull(camera, "The main scene still has its camera");
            Assert.AreEqual("MainCamera", camera.tag, "Scene surgery did not reset the camera tag");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
