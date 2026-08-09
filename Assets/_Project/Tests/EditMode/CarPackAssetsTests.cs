using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CarPackAssetsTests
{
    private const float ScaleTolerance = 0.05f;

    [Test]
    public void Catalog_CoversAllPackModels_AtD8TileLengths()
    {
        var expected = new Dictionary<string, int>
        {
            { "Car 1", 1 }, { "Car 2", 1 }, { "Car 3", 1 }, { "Car 4", 1 }, { "Car 5", 1 }, { "Car 6", 1 },
            { "Policecar", 1 },
            { "Truck 1", 2 }, { "Truck 2", 2 },
            { "Bus", 3 }
        };

        var catalog = CarPackAssets.CatalogModelNames;

        Assert.AreEqual(expected.Count, catalog.Length, "The catalog covers every model in the pack");
        Assert.That(catalog.Distinct().Count(), Is.EqualTo(catalog.Length), "Catalog entries are unique");
        foreach (var name in expected.Keys)
        {
            Assert.IsTrue(catalog.Contains(name), name + " is catalogued");
            Assert.AreEqual(expected[name], CarPackAssets.TileLength(name), name + " tiles per D8");
        }
    }

    [Test]
    public void Ensure_NormalizesEachModel_ToOneTilePerFootprint()
    {
        CarPackAssets.Ensure();

        foreach (var name in CarPackAssets.CatalogModelNames)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.ModelPath(name));
            Assert.IsNotNull(prefab, name + " imported");
            var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, name + " renders a mesh");
            var extent = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
            Assert.AreEqual(CarPackAssets.TileLength(name), extent, ScaleTolerance, name + " occupies its tile footprint on the grid");
        }
    }

    [Test]
    public void PaintSet_HasSixUniversalLitMaterials()
    {
        CarPackAssets.Ensure();

        var names = CarPackAssets.PaintNames;
        Assert.AreEqual(6, names.Length, "Six paints recreate the pack palette");

        foreach (var name in names)
        {
            var material = CarPackAssets.PaintMaterial(name);
            Assert.IsNotNull(material, name + " paint material exists");
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), name + " is a URP lit material");
            Assert.That(material.GetFloat("_Smoothness"), Is.GreaterThanOrEqualTo(0.6f), name + " reads as car paint gloss");
            Assert.That(material.GetFloat("_Metallic"), Is.GreaterThanOrEqualTo(0.4f), name + " has a paint-like metal response");
        }
    }

    [Test]
    public void VehiclePrefab_IsComposedFromModelPlusOwnComponents()
    {
        CarPackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.PrefabPath);
        Assert.IsNotNull(prefab, "Vehicle prefab is authored");

        Assert.IsNotNull(prefab.GetComponent<Vehicle>(), "Prefab carries the grid Vehicle component");
        Assert.IsNotNull(prefab.GetComponent<VehicleMovement>(), "Prefab carries the drive component");
        Assert.IsNull(prefab.GetComponent<Rigidbody>(), "No physics body - collision stays grid-space");
        Assert.IsNull(prefab.GetComponent<SpriteRenderer>(), "No legacy 2D sprite renderer");

        var model = prefab.transform.Find("Model");
        Assert.IsNotNull(model, "Prefab nests the 3D model child");

        var renderer = model.GetComponentInChildren<MeshRenderer>(true);
        Assert.IsNotNull(renderer, "Model child renders a mesh");
        Assert.IsNotNull(model.GetComponentInChildren<MeshFilter>(true).sharedMesh, "Model child has geometry");
        Assert.IsNotNull(renderer.sharedMaterial, "Model child is painted");
        Assert.IsNull(model.GetComponent<SpriteRenderer>(), "Model child is 3D geometry, not a sprite");
    }

[Test]
    public void VehiclePrefab_ModelChild_ExtendsOneTileAlongX_GridAligned()
    {
        CarPackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.PrefabPath);
        Assert.IsNotNull(prefab, "Vehicle prefab is authored");
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.IsNotNull(instance, "Prefab instantiates");
        try
        {
            var model = instance.transform.Find("Model");
            Assert.IsNotNull(model, "Prefab nests the 3D model child");
            var renderer = model.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, "Model child renders a mesh");

            var bounds = renderer.bounds;
            Assert.AreEqual(1f, bounds.size.x, ScaleTolerance, "Car body occupies one tile along X");
            Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(bounds.size.y), "Length is the dominant axis");
            Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(bounds.size.z), "Length is the dominant axis");
            Assert.AreEqual(0f, bounds.center.x, ScaleTolerance, "Model is centred on the grid origin along X");
            Assert.AreEqual(0f, bounds.center.z, ScaleTolerance, "Model is centred on the grid origin along Z");
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
}