using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PalmovPackAssetsTests
{
    private const float ScaleTolerance = 0.05f;

    [Test]
    public void Catalog_CoversAllFortyNinePackModels_ExcludesFerrisWheel()
    {
        var expected = new[]
        {
            "big cottage 1 floor new",
            "brewery house",
            "catholic temple",
            "city hall",
            "cute house",
            "pizzeria house",
            "post office",
            "cottage tree 1",
            "cottage tree 2",
            "fir tree 1",
            "fir tree 2",
            "fir tree group 1",
            "fir tree group 2",
            "potted tree",
            "round tree",
            "spruce border 1",
            "spruce border 2",
            "tree",
            "bush 1",
            "bush 2",
            "bush 3",
            "plants",
            "plants 2",
            "plants 3",
            "bench",
            "bottle",
            "box with bottles",
            "chair",
            "dog house",
            "fence white left",
            "fence white right",
            "fountain",
            "lamppost",
            "table",
            "tennis net",
            "trash can",
            "asphalt ground large",
            "asphalt ground side turn",
            "asphalt ground small wide",
            "asphalt ground small",
            "asphalt ground wide side turn",
            "lake",
            "land",
            "tennis court brown",
            "paved road all directions",
            "paved road pedestrian crossing",
            "paved road straight turn",
            "paved road straight",
            "paved road turn"
        };

        var catalog = PalmovPackAssets.CatalogModelNames;

        Assert.AreEqual(expected.Length, catalog.Count, "The catalog covers every modelled entry in the pack");
        Assert.That(catalog.Distinct().Count(), Is.EqualTo(catalog.Count), "Catalog entries are unique");
        foreach (var name in expected)
        {
            Assert.IsTrue(catalog.Contains(name), name + " is catalogued");
        }
        Assert.IsFalse(catalog.Contains("ferris wheel"), "Ferris wheel is pruned per D11");
    }

    [Test]
    public void PrunedPack_HasNoLunaparkFolder_OrDemoScene()
    {
        var modelsDir = PalmovPackAssets.ModelsDir;
        var allAssets = AssetDatabase.FindAssets("", new[] { modelsDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();

        CollectionAssert.DoesNotContain(allAssets, ModelsFor("Lunapark"), "Lunapark models are pruned");
        CollectionAssert.DoesNotContain(allAssets, DemoScenePath(), "Demo scene is pruned");
        Assert.IsFalse(Directory.Exists(modelsDir + "/Lunapark"), "No Lunapark folder under Models");
    }

    [Test]
    public void Ensure_NormalizesEachModel_ToItsFootprintTarget()
    {
        PalmovPackAssets.Ensure();

        foreach (var name in PalmovPackAssets.CatalogModelNames)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PalmovPackAssets.ModelPath(name));
            Assert.IsNotNull(prefab, name + " imported");
            var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, name + " renders a mesh");
            var extent = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
            var target = PalmovPackAssets.FootprintTarget(name);
            Assert.AreEqual(target, extent, ScaleTolerance, name + " occupies its footprint");
        }
    }

    [Test]
    public void PalmovMaterial_IsUrpLit_WithPackTexture()
    {
        PalmovPackAssets.Ensure();

        var material = AssetDatabase.LoadAssetAtPath<Material>(PalmovPackAssets.MaterialPath);
        Assert.IsNotNull(material, "PalmovMain material is authored");
        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), "Material is URP lit");
        Assert.IsNotNull(material.mainTexture, "Material carries the pack texture");
        Assert.That(AssetDatabase.GetAssetPath(material.mainTexture), Is.EqualTo(PalmovPackAssets.TexturePath), "Texture comes from the Palmov pack");
        Assert.That(material.GetFloat("_Smoothness"), Is.LessThan(0.6f), "Walls read as matte, not gloss");
        Assert.That(material.GetFloat("_Metallic"), Is.LessThan(0.1f), "Walls are non-metallic");
    }

    [Test]
    public void BackdropPrefab_IsComposedFromModelEntries_SharePalmovMaterial()
    {
        PalmovPackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PalmovPackAssets.BackdropPrefabPath);
        Assert.IsNotNull(prefab, "Backdrop prefab is authored");

        var entries = PalmovPackAssets.BackdropEntries;
        Assert.AreEqual(prefab.transform.childCount, entries.Count, "One child per layout entry");

        var material = AssetDatabase.LoadAssetAtPath<Material>(PalmovPackAssets.MaterialPath);
        for (int i = 0; i < entries.Count; i++)
        {
            var child = prefab.transform.GetChild(i);
            Assert.AreEqual(entries[i].Model + " " + i, child.name, "Child " + i + " is the layout entry");
            Assert.AreEqual(entries[i].Position, child.transform.position, "Child " + i + " sits at its layout position");

            var filter = child.GetComponent<MeshFilter>();
            var renderer = child.GetComponent<MeshRenderer>();
            Assert.IsNotNull(filter, "Child " + i + " carries a mesh");
            Assert.IsNotNull(filter.sharedMesh, "Child " + i + " has geometry");
            Assert.IsNotNull(renderer, "Child " + i + " renders");
            Assert.AreEqual(material, renderer.sharedMaterial, "Child " + i + " shares the Palmov material");
            Assert.IsNull(child.GetComponent<Rigidbody>(), "Child " + i + " has no physics body");
            Assert.IsNull(child.GetComponent<Collider>(), "Child " + i + " has no collider");
            Assert.IsNull(child.GetComponent<MonoBehaviour>(), "Child " + i + " has no scripts");
        }
    }

    [Test]
    public void BackdropLayout_ClearsTheLotRect()
    {
        var entries = PalmovPackAssets.BackdropEntries;

        var lotMin = PalmovPackAssets.LotMin;
        var lotMax = PalmovPackAssets.LotMax;

        foreach (var entry in entries)
        {
            var clearLeft = entry.Position.x < lotMin;
            var clearRight = entry.Position.x > lotMax;
            var clearBottom = entry.Position.y < lotMin;
            var clearTop = entry.Position.y > lotMax;
            Assert.IsTrue(clearLeft || clearRight || clearBottom || clearTop,
                entry.Model + " at " + entry.Position + " overlaps the lot rect");
        }
    }

    [Test]
    public void MainScene_ContainsBackdropInstance_AndDirectionalSun()
    {
        PalmovPackAssets.Ensure();

        var scene = EditorSceneManager.OpenScene(PalmovPackAssets.MainScenePath, OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var sun = roots.FirstOrDefault(r => r.name == PalmovPackAssets.SunName);
            Assert.IsNotNull(sun, "Sun exists in the main scene");
            var light = sun.GetComponent<Light>();
            Assert.IsNotNull(light, "Sun is a light");
            Assert.AreEqual(LightType.Directional, light.type, "Sun is directional");

            var backdrop = roots.FirstOrDefault(r =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(r))
                    == PalmovPackAssets.BackdropPrefabPath);
            Assert.IsNotNull(backdrop, "The Backdrop prefab instance is in the main scene");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static string ModelsFor(string folder)
    {
        return PalmovPackAssets.ModelsDir + "/" + folder;
    }

    private static string DemoScenePath()
    {
        var pack = new DirectoryInfo("Assets/_Project/Packs/PalmovHouses");
        var scenes = pack.EnumerateFiles("*.unity", SearchOption.AllDirectories);
        return scenes.FirstOrDefault()?.FullName.Replace('\\', '/').Replace(Directory.GetCurrentDirectory() + "/", "");
    }
}