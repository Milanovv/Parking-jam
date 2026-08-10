using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ConcretePackAssetsTests
{
    private const float Tolerance = 0.02f;

    [Test]
    public void Pack_KeepsExactlyPatterns03And07_NoPattern19()
    {
        var files = Directory.GetFiles(ConcretePackAssets.TexturesDir, "*.png")
            .Select(f => Path.GetFileName(f))
            .ToList();

        Assert.AreEqual(4, files.Count, "Only the two kept patterns, diffuse + normal each");
        CollectionAssert.Contains(files, "pattern03_diffuse.png");
        CollectionAssert.Contains(files, "pattern03_normal.png");
        CollectionAssert.Contains(files, "pattern07_diffuse.png");
        CollectionAssert.Contains(files, "pattern07_normal.png");
        Assert.IsFalse(files.Any(f => f.Contains("pattern19")), "Pattern 19 is pruned per R4");
    }

    [Test]
    public void ImportSettings_ArePng_Bc7_Max2048_ForEveryTexture()
    {
        ConcretePackAssets.Ensure();

        var platform = new TextureImporterPlatformSettings
        {
            name = "Standalone",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.BC7
        };

        foreach (var path in ConcretePackAssets.AllTexturePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path + " imports as a texture");
            Assert.IsTrue(path.EndsWith(".png"), path + " is a PNG");

            var actual = importer.GetPlatformTextureSettings("Standalone");
            Assert.IsTrue(actual.overridden, path + " overrides the Standalone platform");
            Assert.AreEqual(2048, actual.maxTextureSize, path + " caps at 2048");
            Assert.AreEqual(TextureImporterFormat.BC7, actual.format, path + " compresses BC7, not ASTC");
            Assert.AreEqual(platform.maxTextureSize, importer.maxTextureSize, path + " base max size matches");
        }
    }

    [Test]
    public void NormalMaps_AreImportedAsNormalMaps_DiffuseAsDefault()
    {
        ConcretePackAssets.Ensure();

        foreach (var path in ConcretePackAssets.DiffusePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path + " imports");
            Assert.AreEqual(TextureImporterType.Default, importer.textureType, path + " stays a default texture");
        }

        foreach (var path in ConcretePackAssets.NormalPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path + " imports");
            Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType, path + " is tagged NormalMap so it feeds _BumpMap");
        }
    }

    [Test]
    public void LotGroundMaterial_IsUrpLit_NormalMapped_TilesOneMetre()
    {
        ConcretePackAssets.Ensure();

        var material = AssetDatabase.LoadAssetAtPath<Material>(ConcretePackAssets.LotMaterialPath);
        Assert.IsNotNull(material, "Lot ground material is authored");
        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), "Lot ground is URP lit");
        Assert.IsNotNull(material.mainTexture, "Lot ground carries a diffuse map");
        Assert.That(AssetDatabase.GetAssetPath(material.mainTexture), Is.EqualTo(ConcretePackAssets.DiffusePath03), "Diffuse is pattern 03");
        Assert.IsNotNull(material.GetTexture("_BumpMap"), "Lot ground carries a normal map");
        Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")), Is.EqualTo(ConcretePackAssets.NormalPath03), "Normal map is pattern 03");
        Assert.That(material.GetFloat("_Smoothness"), Is.LessThan(0.6f), "Concrete reads as matte, not gloss");
        Assert.That(material.GetFloat("_Metallic"), Is.LessThan(0.1f), "Concrete is non-metallic");
        Assert.That(material.mainTextureScale, Is.EqualTo(new Vector2(ConcretePackAssets.LotSize, ConcretePackAssets.LotSize)).Within(Tolerance),
            "Pattern 03 tiles once per metre across the 12-metre lot");
    }

    [Test]
    public void ApronGroundMaterial_IsUrpLit_NormalMapped_TilesOneMetre()
    {
        ConcretePackAssets.Ensure();

        var material = AssetDatabase.LoadAssetAtPath<Material>(ConcretePackAssets.ApronMaterialPath);
        Assert.IsNotNull(material, "Apron ground material is authored");
        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), "Apron ground is URP lit");
        Assert.IsNotNull(material.mainTexture, "Apron ground carries a diffuse map");
        Assert.That(AssetDatabase.GetAssetPath(material.mainTexture), Is.EqualTo(ConcretePackAssets.DiffusePath07), "Diffuse is pattern 07");
        Assert.IsNotNull(material.GetTexture("_BumpMap"), "Apron ground carries a normal map");
        Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")), Is.EqualTo(ConcretePackAssets.NormalPath07), "Normal map is pattern 07");
        Assert.That(material.GetFloat("_Smoothness"), Is.LessThan(0.6f), "Concrete reads as matte, not gloss");
        Assert.That(material.GetFloat("_Metallic"), Is.LessThan(0.1f), "Concrete is non-metallic");
        Assert.That(material.mainTextureScale, Is.EqualTo(new Vector2(ConcretePackAssets.ApronWidth, ConcretePackAssets.LotSize)).Within(Tolerance),
            "Pattern 07 tiles once per metre across the apron");
    }

    [Test]
    public void GroundPrefab_CoversTheLotRect_AndApronOffTheExitEdge()
    {
        ConcretePackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConcretePackAssets.GroundPrefabPath);
        Assert.IsNotNull(prefab, "Ground prefab is authored");

        var lotChild = prefab.transform.Find(ConcretePackAssets.LotChildName);
        var apronChild = prefab.transform.Find(ConcretePackAssets.ApronChildName);
        Assert.IsNotNull(lotChild, "The lot floor quad exists");
        Assert.IsNotNull(apronChild, "The exit apron quad exists");

        var lotBounds = BoundsOf(lotChild);
        var apronBounds = BoundsOf(apronChild);

        Assert.AreEqual(ConcretePackAssets.LotMin, lotBounds.min.x, Tolerance, "Lot floor starts at the lot min X");
        Assert.AreEqual(ConcretePackAssets.LotMin, lotBounds.min.y, Tolerance, "Lot floor starts at the lot min Y");
        Assert.AreEqual(ConcretePackAssets.LotMax, lotBounds.max.x, Tolerance, "Lot floor ends at the lot max X");
        Assert.AreEqual(ConcretePackAssets.LotMax, lotBounds.max.y, Tolerance, "Lot floor ends at the lot max Y");

        Assert.AreEqual(ConcretePackAssets.LotMax, apronBounds.min.x, Tolerance, "Apron starts right at the exit edge");
        Assert.That(apronBounds.max.x, Is.GreaterThan(ConcretePackAssets.LotMax), "Apron runs off the lot");
        Assert.AreEqual(ConcretePackAssets.LotMin, apronBounds.min.y, Tolerance, "Apron spans the full lot depth");
        Assert.AreEqual(ConcretePackAssets.LotMax, apronBounds.max.y, Tolerance, "Apron spans the full lot depth");
    }

    [Test]
    public void GroundPrefab_ChildrenArePlainQuads_NoPhysics_NoScripts()
    {
        ConcretePackAssets.Ensure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConcretePackAssets.GroundPrefabPath);
        Assert.IsNotNull(prefab, "Ground prefab is authored");

        foreach (var name in new[] { ConcretePackAssets.LotChildName, ConcretePackAssets.ApronChildName })
        {
            var child = prefab.transform.Find(name);
            Assert.IsNotNull(child, name + " exists");

            var filter = child.GetComponent<MeshFilter>();
            var renderer = child.GetComponent<MeshRenderer>();
            Assert.IsNotNull(filter, name + " carries a mesh");
            Assert.IsNotNull(filter.sharedMesh, name + " has geometry");
            Assert.IsNotNull(renderer, name + " renders");
            Assert.IsNotNull(renderer.sharedMaterial, name + " is painted");

            Assert.IsNull(child.GetComponent<Collider>(), name + " has no physics collider");
            Assert.IsNull(child.GetComponent<Rigidbody>(), name + " has no physics body");
            Assert.IsNull(child.GetComponent<MonoBehaviour>(), name + " has no scripts");

            Assert.That(Quaternion.Angle(child.transform.rotation, FacingCameraRotation), Is.LessThan(0.01f),
                name + " faces the camera (-Z), not its backside");
        }
    }

    [Test]
    public void MainScene_ContainsGroundInstance_AndKeepsCameraTag()
    {
        ConcretePackAssets.Ensure();

        var scene = EditorSceneManager.OpenScene(ConcretePackAssets.MainScenePath, OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var ground = roots.FirstOrDefault(r =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(r))
                    == ConcretePackAssets.GroundPrefabPath);
            Assert.IsNotNull(ground, "The Ground prefab instance is in the main scene");

            var camera = roots.FirstOrDefault(r => r.name == "MainCamera");
            Assert.IsNotNull(camera, "The main scene still has its camera");
            Assert.AreEqual("MainCamera", camera.tag, "Scene surgery did not reset the camera tag");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static Bounds BoundsOf(Transform child)
    {
        var instance = Object.Instantiate(child.gameObject);
        try
        {
            return instance.GetComponent<MeshRenderer>().bounds;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static readonly Quaternion FacingCameraRotation = Quaternion.Euler(0f, 180f, 0f);
}