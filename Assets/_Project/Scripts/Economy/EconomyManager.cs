using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public static string CustomSavePath { get; set; }

    public SaveData State { get; private set; }

    public int BonusUndosRemaining => State != null ? State.bonusUndosRemaining : 0;

    private SaveStorage _storage;

    public static EconomyManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var host = new GameObject("EconomyManager");
        return host.AddComponent<EconomyManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _storage = new SaveStorage(string.IsNullOrEmpty(CustomSavePath) ? SaveStorage.DefaultPath : CustomSavePath);
        State = _storage.Load() ?? new SaveData();
        if (string.IsNullOrEmpty(State.equippedVehicleSkinId)) State.equippedVehicleSkinId = "Red";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public DailyLoginResult RegisterDailyLogin(DateTime? today = null)
    {
        string date = (today ?? DateTime.Now).ToString("yyyy-MM-dd");
        var result = State.RegisterDailyLogin(date);
        Save();
        return result;
    }

    public void LevelCompleted(int levelId, int remainingUndos)
    {
        if (State == null) return;
        State.CompleteLevel(levelId, remainingUndos);
        Save();
    }

    public void RecordLevelAttempt(int levelId)
    {
        if (State == null) return;
        State.RecordLevelAttempt(levelId);
        Save();
    }

    public void ConsumeBonusUndo()
    {
        if (State == null) return;
        State.ConsumeBonusUndo();
        Save();
    }

    public void CompleteDailyMission()
    {
        if (State == null) return;
        State.CompleteDailyMission();
        Save();
    }

    public void CompleteChallenge()
    {
        if (State == null) return;
        State.CompleteChallenge();
        Save();
    }

    public bool TryBuyCommonSkin(string skinId)
    {
        if (State == null || !State.TryBuyCommonSkin(skinId)) return false;
        Save();
        return true;
    }

    public bool TryBuyExclusiveSkin(string skinId)
    {
        if (State == null || !State.TryBuyExclusiveSkin(skinId)) return false;
        Save();
        return true;
    }

    public bool EquipSkin(string skinId)
    {
        if (State == null || !State.EquipSkin(skinId)) return false;
        var controller = FindFirstObjectByType<SkinController>();
        if (controller != null) controller.Equip(skinId);
        Save();
        return true;
    }

    public bool TrySpendCoins(int amount)
    {
        if (State == null || !State.TrySpendCoins(amount)) return false;
        Save();
        return true;
    }

    public void Save()
    {
        if (_storage != null && State != null) _storage.Save(State);
    }
}