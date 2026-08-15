public static class LevelSelectModel
{
    public const int TotalLevels = 25;

    public static bool IsUnlocked(SaveData save, int levelId)
    {
        if (levelId < 1 || levelId > TotalLevels) return false;
        if (save == null) return levelId == 1;
        return levelId == 1
            || save.completedLevels.Contains(levelId)
            || save.lastCompletedLevel >= levelId - 1;
    }
}
