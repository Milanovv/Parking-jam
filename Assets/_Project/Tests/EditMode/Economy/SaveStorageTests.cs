using System;
using System.IO;
using NUnit.Framework;

public class SaveStorageTests
{
    private string _path;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), "parking-jam-save-" + Guid.NewGuid() + ".json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Test]
    public void RoundTrip_PreservesEveryField()
    {
        var save = new SaveData { coins = 777, keys = 2, equippedVehicleSkinId = "Silver" };
        save.unlockedVehicleSkins.Add("Blue");
        save.RecordLevelAttempt(3);
        save.CompleteLevel(3, 2);
        save.RegisterDailyLogin("2026-08-11");

        var storage = new SaveStorage(_path);
        storage.Save(save);
        var loaded = storage.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(847, loaded.coins);
        Assert.AreEqual(777 + EconomyConfig.LevelBaseCoins + 2 * EconomyConfig.CoinPerUndoRemaining, loaded.coins);
        Assert.AreEqual(2, loaded.keys);
        Assert.AreEqual("Silver", loaded.equippedVehicleSkinId);
        Assert.AreEqual(new[] { "Blue", EconomyConfig.WelcomeSkinId }, loaded.unlockedVehicleSkins.ToArray());
        Assert.AreEqual(new[] { EconomyConfig.WelcomePedestrianSkinId }, loaded.unlockedPedestrianSkins.ToArray());
        Assert.AreEqual(1, loaded.completedLevels.Count);
        Assert.AreEqual(2, loaded.RecordFor(3).bestUndosRemaining);
        Assert.AreEqual(1, loaded.RecordFor(3).attemptCount);
        Assert.AreEqual(1, loaded.dailyLoginStreak);
        Assert.AreEqual("2026-08-11", loaded.lastLoginDate);
        Assert.AreEqual(2, loaded.bonusUndosRemaining);
    }

    [Test]
    public void MissingFile_LoadsNull()
    {
        Assert.IsNull(new SaveStorage(_path).Load());
    }

    [Test]
    public void CorruptFile_LoadsNull()
    {
        File.WriteAllText(_path, "this is not json at all {{{");

        Assert.IsNull(new SaveStorage(_path).Load());
    }

    [Test]
    public void EmptyFile_LoadsNull()
    {
        File.WriteAllText(_path, "");

        Assert.IsNull(new SaveStorage(_path).Load());
    }

    [Test]
    public void Save_Twice_OverwritesWithTheLatestState()
    {
        var storage = new SaveStorage(_path);
        storage.Save(new SaveData { coins = 10 });
        storage.Save(new SaveData { coins = 20 });

        Assert.AreEqual(20, storage.Load().coins);
    }
}