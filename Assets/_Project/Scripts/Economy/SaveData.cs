using System;
using System.Collections.Generic;
using System.Globalization;

public struct LevelReward
{
    public int CoinsEarned;
    public string MilestoneSkinId;
}

public struct DailyLoginResult
{
    public bool FirstLogin;
    public int Streak;
    public bool BonusGranted;
    public bool WelcomeSkinGranted;
}

[Serializable]
public class LevelRecord
{
    public int levelId;
    public int bestUndosRemaining;
    public int attemptCount;
}

[Serializable]
public class SaveData
{
    public int coins;
    public int keys;
    public List<string> unlockedVehicleSkins = new List<string>();
    public List<string> unlockedPedestrianSkins = new List<string>();
    public string equippedVehicleSkinId;
    public string equippedPedestrianSkinId;
    public int lastCompletedLevel;
    public List<int> completedLevels = new List<int>();
    public List<LevelRecord> levelRecords = new List<LevelRecord>();
    public int dailyLoginStreak;
    public string lastLoginDate;
    public int bonusUndosRemaining;

    public LevelRecord RecordFor(int levelId)
    {
        foreach (var record in levelRecords)
        {
            if (record.levelId == levelId) return record;
        }
        return null;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }

    public bool TrySpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        return true;
    }

    public void AddKeys(int amount)
    {
        keys += amount;
    }

    public bool TrySpendKeys(int amount)
    {
        if (keys < amount) return false;
        keys -= amount;
        return true;
    }

    public bool IsSkinUnlocked(string skinId)
    {
        return unlockedVehicleSkins.Contains(skinId) || unlockedPedestrianSkins.Contains(skinId);
    }

    public bool TryBuyCommonSkin(string skinId)
    {
        if (IsSkinUnlocked(skinId) || !TrySpendCoins(EconomyConfig.CommonSkinPriceCoins)) return false;
        UnlockVehicleSkin(skinId);
        return true;
    }

    public bool TryBuyExclusiveSkin(string skinId)
    {
        if (IsSkinUnlocked(skinId) || !TrySpendKeys(EconomyConfig.ExclusiveSkinPriceKeys)) return false;
        UnlockVehicleSkin(skinId);
        return true;
    }

    public void UnlockVehicleSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || IsSkinUnlocked(skinId)) return;
        unlockedVehicleSkins.Add(skinId);
    }

    public void UnlockPedestrianSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || IsSkinUnlocked(skinId)) return;
        unlockedPedestrianSkins.Add(skinId);
    }

    public bool EquipSkin(string skinId)
    {
        if (!IsSkinUnlocked(skinId)) return false;
        equippedVehicleSkinId = skinId;
        return true;
    }

    public void RecordLevelAttempt(int levelId)
    {
        var record = GetOrCreateRecord(levelId);
        record.attemptCount++;
    }

    private LevelRecord GetOrCreateRecord(int levelId)
    {
        var record = RecordFor(levelId);
        if (record != null) return record;
        record = new LevelRecord { levelId = levelId };
        levelRecords.Add(record);
        return record;
    }

    public LevelReward CompleteLevel(int levelId, int remainingUndos)
    {
        if (!completedLevels.Contains(levelId)) completedLevels.Add(levelId);
        if (levelId > lastCompletedLevel) lastCompletedLevel = levelId;

        var record = GetOrCreateRecord(levelId);
        if (remainingUndos > record.bestUndosRemaining)
            record.bestUndosRemaining = remainingUndos;

        int earned = EconomyConfig.LevelBaseCoins + remainingUndos * EconomyConfig.CoinPerUndoRemaining;
        AddCoins(earned);

        string milestoneSkinId = EconomyConfig.SkinForMilestone(levelId);
        string grantedSkin = null;
        if (milestoneSkinId != null && !IsSkinUnlocked(milestoneSkinId))
        {
            UnlockVehicleSkin(milestoneSkinId);
            grantedSkin = milestoneSkinId;
        }

        return new LevelReward { CoinsEarned = earned, MilestoneSkinId = grantedSkin };
    }

    public void AddBonusUndos(int count)
    {
        bonusUndosRemaining += count;
    }

    public DailyLoginResult RegisterDailyLogin(string today)
    {
        var result = new DailyLoginResult();
        if (string.IsNullOrEmpty(lastLoginDate))
        {
            result.FirstLogin = true;
            result.Streak = 1;
            result.BonusGranted = true;
            result.WelcomeSkinGranted =
                !IsSkinUnlocked(EconomyConfig.WelcomeSkinId) || !IsSkinUnlocked(EconomyConfig.WelcomePedestrianSkinId);
            if (!IsSkinUnlocked(EconomyConfig.WelcomeSkinId)) UnlockVehicleSkin(EconomyConfig.WelcomeSkinId);
            if (!IsSkinUnlocked(EconomyConfig.WelcomePedestrianSkinId)) UnlockPedestrianSkin(EconomyConfig.WelcomePedestrianSkinId);
        }
        else if (lastLoginDate == today)
        {
            result.Streak = dailyLoginStreak;
        }
        else
        {
            result.Streak = IsConsecutiveDay(lastLoginDate, today) ? dailyLoginStreak + 1 : 1;
            result.BonusGranted = true;
        }

        if (result.BonusGranted) AddBonusUndos(EconomyConfig.DailyLoginBonusUndos);
        dailyLoginStreak = result.Streak;
        lastLoginDate = today;
        return result;
    }

    public void ConsumeBonusUndo()
    {
        if (bonusUndosRemaining > 0) bonusUndosRemaining--;
    }

    public void CompleteDailyMission()
    {
        AddCoins(EconomyConfig.MissionCoins);
        AddBonusUndos(EconomyConfig.MissionBonusUndos);
    }

    public void CompleteChallenge()
    {
        AddCoins(EconomyConfig.ChallengeCoins);
    }

    public static bool IsConsecutiveDay(string previous, string today)
    {
        const string format = "yyyy-MM-dd";
        var culture = CultureInfo.InvariantCulture;
        if (!DateTime.TryParseExact(previous, format, culture, DateTimeStyles.None, out var a)) return false;
        if (!DateTime.TryParseExact(today, format, culture, DateTimeStyles.None, out var b)) return false;
        return (b - a).Days == 1;
    }
}