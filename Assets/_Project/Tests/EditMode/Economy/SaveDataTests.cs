using NUnit.Framework;

public class SaveDataTests
{
    private static SaveData Fresh()
    {
        return new SaveData();
    }

    [Test]
    public void Completion_CreditsBasePlusUndoBonus()
    {
        var data = Fresh();
        var reward = data.CompleteLevel(1, 5);

        Assert.AreEqual(50 + 5 * 10, reward.CoinsEarned);
        Assert.AreEqual(50 + 5 * 10, data.coins);
        Assert.AreEqual(1, data.lastCompletedLevel);
        Assert.AreEqual(new[] { 1 }, data.completedLevels.ToArray());
        Assert.AreEqual(5, data.RecordFor(1).bestUndosRemaining);
    }

    [Test]
    public void Completion_WithFewerRemainingUndos_CreditsLess()
    {
        var data = Fresh();

        data.CompleteLevel(1, 2);

        Assert.AreEqual(70, data.coins);
    }

    [Test]
    public void RecordLevelAttempt_TracksAttemptCountPerLevel()
    {
        var data = Fresh();

        data.RecordLevelAttempt(1);
        data.RecordLevelAttempt(1);
        data.RecordLevelAttempt(2);

        Assert.AreEqual(2, data.RecordFor(1).attemptCount);
        Assert.AreEqual(1, data.RecordFor(2).attemptCount);
    }

    [Test]
    public void Completion_RecordsBestRemainingUndosAcrossAttempts()
    {
        var data = Fresh();

        data.RecordLevelAttempt(1);
        data.CompleteLevel(1, 2);
        data.RecordLevelAttempt(1);
        data.CompleteLevel(1, 4);

        Assert.AreEqual(4, data.RecordFor(1).bestUndosRemaining);
        Assert.AreEqual(2, data.RecordFor(1).attemptCount, "Attempts count starts, not completions");
    }

    [Test]
    public void Completion_OutOfOrder_StillTracksEveryLevelAndMax()
    {
        var data = Fresh();

        data.CompleteLevel(3, 1);
        data.CompleteLevel(1, 1);

        Assert.AreEqual(3, data.lastCompletedLevel);
        Assert.AreEqual(2, data.completedLevels.Count);
    }

    [Test]
    public void Completion_Replay_CreditsAgainWithoutDuplicatingProgress()
    {
        var data = Fresh();

        data.CompleteLevel(1, 0);
        int afterFirst = data.coins;
        data.CompleteLevel(1, 0);

        Assert.AreEqual(afterFirst * 2, data.coins, "Replays pay out again");
        Assert.AreEqual(1, data.completedLevels.Count);
    }

    [Test]
    public void CoinDebit_Insufficient_FailsWithoutSideEffects()
    {
        var data = Fresh();
        data.AddCoins(60);

        Assert.IsFalse(data.TrySpendCoins(100));
        Assert.AreEqual(60, data.coins);
    }

    [Test]
    public void CoinDebit_Sufficient_Spends()
    {
        var data = Fresh();
        data.AddCoins(200);

        Assert.IsTrue(data.TrySpendCoins(100));
        Assert.AreEqual(100, data.coins);
    }

    [Test]
    public void KeyDebit_FollowsTheSameRules()
    {
        var data = Fresh();
        data.AddKeys(3);

        Assert.IsTrue(data.TrySpendKeys(2));
        Assert.IsFalse(data.TrySpendKeys(2));
        Assert.AreEqual(1, data.keys);
    }

    [Test]
    public void CommonSkin_BuysWithCoins_AndCannotBeBoughtTwice()
    {
        var data = Fresh();
        data.AddCoins(500);

        Assert.IsTrue(data.TryBuyCommonSkin("Blue"));
        Assert.AreEqual(300, data.coins);
        Assert.IsTrue(data.IsSkinUnlocked("Blue"));

        Assert.IsFalse(data.TryBuyCommonSkin("Blue"), "An owned skin cannot be bought again");
        Assert.AreEqual(300, data.coins, "No refund on a duplicate purchase");
    }

    [Test]
    public void CommonSkin_InsufficientCoins_DoesNotUnlock()
    {
        var data = Fresh();
        data.AddCoins(100);

        Assert.IsFalse(data.TryBuyCommonSkin("Blue"));
        Assert.IsFalse(data.IsSkinUnlocked("Blue"));
        Assert.AreEqual(100, data.coins);
    }

    [Test]
    public void ExclusiveSkin_BuysWithKeys()
    {
        var data = Fresh();
        data.AddKeys(1);

        Assert.IsTrue(data.TryBuyExclusiveSkin("Yellow"));
        Assert.AreEqual(0, data.keys);
        Assert.IsTrue(data.IsSkinUnlocked("Yellow"));
    }

    [Test]
    public void ExclusiveSkin_WithoutKeys_DoesNotUnlock()
    {
        var data = Fresh();

        Assert.IsFalse(data.TryBuyExclusiveSkin("Yellow"));
        Assert.IsFalse(data.IsSkinUnlocked("Yellow"));
    }

    [Test]
    public void Equip_RequiresTheSkinToBeUnlocked()
    {
        var data = Fresh();

        Assert.IsFalse(data.EquipSkin("Blue"));
        Assert.IsNull(data.equippedVehicleSkinId);
        Assert.IsNull(data.equippedPedestrianSkinId, "Pedestrian skin stays untouched");

        data.UnlockVehicleSkin("Blue");
        Assert.IsTrue(data.EquipSkin("Blue"));
        Assert.AreEqual("Blue", data.equippedVehicleSkinId);
    }

    [Test]
    public void FirstLogin_GrantsBonusUndosWelcomeSkinAndStreak()
    {
        var data = Fresh();

        var result = data.RegisterDailyLogin("2026-08-11");

        Assert.IsTrue(result.FirstLogin);
        Assert.IsTrue(result.BonusGranted);
        Assert.IsTrue(result.WelcomeSkinGranted);
        Assert.AreEqual(1, result.Streak);
        Assert.AreEqual(1, data.dailyLoginStreak);
        Assert.AreEqual(2, data.bonusUndosRemaining);
        Assert.IsTrue(data.IsSkinUnlocked(EconomyConfig.WelcomeSkinId));
        Assert.IsTrue(data.IsSkinUnlocked(EconomyConfig.WelcomePedestrianSkinId), "The welcome gift covers a pedestrian skin too");
        Assert.AreEqual("2026-08-11", data.lastLoginDate);
    }

    [Test]
    public void SameDayLogin_GrantsNothingNew()
    {
        var data = Fresh();
        data.RegisterDailyLogin("2026-08-11");

        var result = data.RegisterDailyLogin("2026-08-11");

        Assert.IsFalse(result.FirstLogin);
        Assert.IsFalse(result.BonusGranted);
        Assert.IsFalse(result.WelcomeSkinGranted);
        Assert.AreEqual(1, data.dailyLoginStreak);
        Assert.AreEqual(2, data.bonusUndosRemaining);
    }

    [Test]
    public void ConsecutiveDay_StreakIncrements_AndGrantsBonus()
    {
        var data = Fresh();
        data.RegisterDailyLogin("2026-08-11");

        var result = data.RegisterDailyLogin("2026-08-12");

        Assert.IsTrue(result.BonusGranted);
        Assert.AreEqual(2, result.Streak);
        Assert.AreEqual(2, data.dailyLoginStreak);
        Assert.AreEqual(4, data.bonusUndosRemaining);
    }

    [Test]
    public void GapDay_ResetsTheStreak_ButStillGrantsBonus()
    {
        var data = Fresh();
        data.RegisterDailyLogin("2026-08-11");
        data.RegisterDailyLogin("2026-08-12");

        var result = data.RegisterDailyLogin("2026-08-15");

        Assert.IsTrue(result.BonusGranted);
        Assert.AreEqual(1, result.Streak);
        Assert.AreEqual(1, data.dailyLoginStreak);
        Assert.AreEqual(6, data.bonusUndosRemaining);
    }

    [Test]
    public void MonthBoundary_CountsAsConsecutive()
    {
        var data = Fresh();
        data.RegisterDailyLogin("2026-12-31");

        var result = data.RegisterDailyLogin("2027-01-01");

        Assert.AreEqual(2, result.Streak);
        Assert.IsTrue(result.BonusGranted);
    }

    [Test]
    public void BonusUndos_AccumulateAcrossGrants()
    {
        var data = Fresh();

        data.AddBonusUndos(3);
        data.AddBonusUndos(2);

        Assert.AreEqual(5, data.bonusUndosRemaining);
    }

    [Test]
    public void MilestoneSkin_GrantedWhenLevelReachesTheThreshold()
    {
        var data = Fresh();

        var atThreshold = data.CompleteLevel(5, 1);
        var belowThreshold = data.CompleteLevel(4, 1);

        Assert.AreEqual(EconomyConfig.SkinForMilestone(5), atThreshold.MilestoneSkinId);
        Assert.IsTrue(data.IsSkinUnlocked(atThreshold.MilestoneSkinId));
        Assert.IsNull(belowThreshold.MilestoneSkinId);
    }

    [Test]
    public void MilestoneSkin_NotGrantedTwiceForTheSameLevel()
    {
        var data = Fresh();
        data.CompleteLevel(5, 1);

        var replay = data.CompleteLevel(5, 1);

        Assert.IsNull(replay.MilestoneSkinId, "Replaying the milestone level grants no second skin");
    }

    [Test]
    public void ConsumeBonusUndo_DrainsTheBank_AndClampsAtZero()
    {
        var data = Fresh();
        data.AddBonusUndos(2);

        data.ConsumeBonusUndo();
        data.ConsumeBonusUndo();

        Assert.AreEqual(0, data.bonusUndosRemaining);
        data.ConsumeBonusUndo();
        Assert.AreEqual(0, data.bonusUndosRemaining, "Spending below zero changes nothing");
    }

    [Test]
    public void DailyMission_AddsCoinsAndBonusUndos()
    {
        var data = Fresh();

        data.CompleteDailyMission();

        Assert.AreEqual(EconomyConfig.MissionCoins, data.coins);
        Assert.AreEqual(EconomyConfig.MissionBonusUndos, data.bonusUndosRemaining);
    }

    [Test]
    public void Challenge_AddsCoinsAboveTheMissionRate()
    {
        var data = Fresh();

        data.CompleteChallenge();

        Assert.AreEqual(EconomyConfig.ChallengeCoins, data.coins);
    }

    [Test]
    public void WelcomeGift_UnlocksInTheRightShelves()
    {
        var data = Fresh();

        data.RegisterDailyLogin("2026-08-11");

        Assert.IsTrue(data.unlockedVehicleSkins.Contains(EconomyConfig.WelcomeSkinId));
        Assert.IsTrue(data.unlockedPedestrianSkins.Contains(EconomyConfig.WelcomePedestrianSkinId));
        Assert.IsFalse(data.unlockedVehicleSkins.Contains(EconomyConfig.WelcomePedestrianSkinId),
            "The pedestrian skin does not leak into the vehicle shelf");
    }
}