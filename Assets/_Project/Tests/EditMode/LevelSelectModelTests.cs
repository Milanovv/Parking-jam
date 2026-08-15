using NUnit.Framework;

public class LevelSelectModelTests
{
    [Test]
    public void FreshSave_UnlocksOnlyLevelOne()
    {
        var save = new SaveData();

        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 1), "Level 1 is always playable");
        for (int levelId = 2; levelId <= LevelSelectModel.TotalLevels; levelId++)
        {
            Assert.IsFalse(LevelSelectModel.IsUnlocked(save, levelId),
                "Level " + levelId + " is locked before any progress");
        }
    }

    [Test]
    public void CompletedLevels_UnlockTheNextLevel()
    {
        var save = new SaveData();
        save.CompleteLevel(3, 2);

        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 1));
        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 2));
        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 3), "The completed level stays replayable");
        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 4), "The next level unlocks behind the last completed one");
        Assert.IsFalse(LevelSelectModel.IsUnlocked(save, 5), "Two ahead stays locked");
        Assert.IsFalse(LevelSelectModel.IsUnlocked(save, 25));
    }

    [Test]
    public void CompletedLevel_AheadOfLastCompleted_StaysReplayable()
    {
        var save = new SaveData();
        save.CompleteLevel(1, 1);
        save.CompleteLevel(10, 1);

        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 10), "An out-of-order completion stays unlocked for replay");
        Assert.IsTrue(LevelSelectModel.IsUnlocked(save, 11), "The unlock frontier advances to the highest completed level");
    }

    [Test]
    public void OutOfRangeLevels_AreNeverUnlocked()
    {
        var save = new SaveData();
        save.CompleteLevel(25, 1);

        Assert.IsFalse(LevelSelectModel.IsUnlocked(save, 0));
        Assert.IsFalse(LevelSelectModel.IsUnlocked(save, 26));
        Assert.IsFalse(LevelSelectModel.IsUnlocked(save, -1));
    }

    [Test]
    public void NullSave_UnlocksOnlyLevelOne()
    {
        Assert.IsTrue(LevelSelectModel.IsUnlocked(null, 1));
        Assert.IsFalse(LevelSelectModel.IsUnlocked(null, 2));
    }

    [Test]
    public void TotalLevels_SpansAllTwentyFiveLevels()
    {
        Assert.AreEqual(25, LevelSelectModel.TotalLevels);
    }
}
