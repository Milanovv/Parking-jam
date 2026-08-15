using NUnit.Framework;

public class SkinCatalogTests
{
    [Test]
    public void Catalog_IsExactlyThePaintShelf()
    {
        CarPackAssets.Ensure();

        Assert.AreEqual(CarPackAssets.PaintNames.Length, SkinCatalog.All.Length,
            "The shop catalog lists every equippable paint");
        foreach (var name in CarPackAssets.PaintNames)
        {
            Assert.IsTrue(SkinCatalog.Contains(name),
                "Catalog entry \"" + name + "\" maps onto the paint shelf");
        }
    }

    [Test]
    public void Catalog_BlueIsACoinSkin_AlignedWithTheSaveSchema()
    {
        var blue = SkinCatalog.Find("Blue");

        Assert.IsFalse(blue.Exclusive, "Blue buys with coins");
    }

    [Test]
    public void Catalog_YellowIsAKeySkin_AlignedWithTheSaveSchema()
    {
        var yellow = SkinCatalog.Find("Yellow");

        Assert.IsTrue(yellow.Exclusive, "Yellow buys with keys");
    }

    [Test]
    public void Catalog_Find_ReturnsDefaultForUnknown()
    {
        Assert.IsNull(SkinCatalog.Find("Nonexistent").Id);
    }

    [Test]
    public void Prices_AreDrivenByEconomyConfig()
    {
        Assert.AreEqual(EconomyConfig.CommonSkinPriceCoins, 200, "Common skins cost coins");
        Assert.AreEqual(EconomyConfig.ExclusiveSkinPriceKeys, 1, "Exclusive skins cost a key");
    }
}
