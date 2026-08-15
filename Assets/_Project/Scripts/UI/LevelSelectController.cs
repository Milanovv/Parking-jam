using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelectController : MonoBehaviour
{
    private Transform _gridParent;
    private readonly Dictionary<int, bool> _unlocked = new Dictionary<int, bool>();

    public event Action<int> LevelChosen;

    public int TotalLevels => LevelSelectModel.TotalLevels;

    public bool IsUnlocked(int levelId)
    {
        return _unlocked.TryGetValue(levelId, out bool value) && value;
    }

    public int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (var pair in _unlocked)
            {
                if (pair.Value) count++;
            }
            return count;
        }
    }

    public void Build(SaveData save, Transform gridParent)
    {
        ClearGrid();
        _unlocked.Clear();
        _gridParent = gridParent;
        if (gridParent == null) return;

        for (int levelId = 1; levelId <= TotalLevels; levelId++)
        {
            bool unlocked = LevelSelectModel.IsUnlocked(save, levelId);
            _unlocked[levelId] = unlocked;
            int captured = levelId;
            GameUiFactory.CreateLevelButton(gridParent, levelId, unlocked, () => LevelChosen?.Invoke(captured));
        }
    }

    private void ClearGrid()
    {
        if (_gridParent == null) return;
        for (int i = _gridParent.childCount - 1; i >= 0; i--)
        {
            var child = _gridParent.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }
}
