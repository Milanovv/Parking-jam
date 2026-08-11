using NUnit.Framework;

public class TextureSweepTests
{
    [Test]
    public void EveryProjectTexture_CapsAt2048_AsBc7_ForThePcBuild()
    {
        var paths = TextureSweepAssets.ProjectTexturePaths();
        Assert.That(paths.Length, Is.GreaterThan(0), "The project imports textures");

        foreach (var path in paths)
        {
            var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            Assert.IsNotNull(importer, path + " imports as a texture");
            Assert.IsTrue(TextureSweepAssets.IsPinnedAt(importer),
                path + " is pinned BC7 at max 2048 for the PC build");
        }
    }
}