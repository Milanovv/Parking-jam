using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SkinShelfTests
{
    [Test]
    public void EquippedSkinId_ResolvesStraightOntoThePaintShelf_ForEveryPaint()
    {
        CarPackAssets.Ensure();

        foreach (var name in CarPackAssets.PaintNames)
        {
            var material = CarPackAssets.PaintMaterial(name);
            Assert.IsNotNull(material, "The equipped skin id \"" + name + "\" maps straight onto the material shelf");
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), name + " is a URP paint");
        }
    }

    [Test]
    public void PaintShelf_IsExactlyTheEquippableSkinCatalog()
    {
        CarPackAssets.Ensure();

        var names = CarPackAssets.PaintNames;
        Assert.AreEqual(6, names.Length, "Six paints compose the skin catalog");

        var shelf = Directory.GetFiles(CarPackAssets.PaintsDir, "*.mat")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name)
            .ToArray();
        var expected = names.OrderBy(name => name).ToArray();
        Assert.AreEqual(expected, shelf,
            "The shelf holds exactly the equippable skins - no separate skin database, the save schema string maps 1:1");
    }

    [Test]
    public void VehiclePrefabs_AllDrawFromTheSharedRedPaint_NotPerPrefabCopies()
    {
        CarPackAssets.Ensure();

        var redPaint = CarPackAssets.PaintMaterial("Red");
        Assert.IsNotNull(redPaint, "Red paint exists on the shelf");

        var prefabPaths = new[] { CarPackAssets.PrefabPath, CarPackAssets.TruckPrefabPath, CarPackAssets.BusPrefabPath };
        foreach (var path in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path + " is authored");
            var renderer = prefab.transform.Find("Model").GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(renderer, path + " paints its model child");
            Assert.AreSame(redPaint, renderer.sharedMaterial,
                path + " draws from the shared Red paint so models match consistently");
        }
    }
}