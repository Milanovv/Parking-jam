public static class EconomyConfig
{
    public const int LevelBaseCoins = 50;
    public const int CoinPerUndoRemaining = 10;
    public const int CommonSkinPriceCoins = 200;
    public const int ExclusiveSkinPriceKeys = 1;
    public const int CoinSkipPriceCoins = 100;
    public const int DailyLoginBonusUndos = 2;
    public const int MissionCoins = 30;
    public const int MissionBonusUndos = 1;
    public const int ChallengeCoins = 100;
    public const string WelcomeSkinId = "Green";
    public const string WelcomePedestrianSkinId = "PeoplePalette";

    private static readonly (int Level, string SkinId)[] Milestones =
    {
        (5, "Blue"),
        (10, "Purple"),
        (15, "Silver"),
        (20, "Yellow")
    };

    public static string SkinForMilestone(int level)
    {
        foreach (var entry in Milestones)
        {
            if (entry.Level == level) return entry.SkinId;
        }
        return null;
    }
}