using System;

public enum MiniGameType
{
    Pipes,
    Pattern,
    Memory
}

public enum MiniGameDifficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public struct PipeSpec
{
    public int Width;
    public int Height;
    public int RotatableTiles;
    public int TimeLimitSeconds;
    public int Hints;
}

[System.Serializable]
public struct PatternSpec
{
    public int ButtonCount;
    public int SequenceLength;
    public float FlashSeconds;
}

[System.Serializable]
public struct MemorySpec
{
    public int Pairs;
    public int Width;
    public int Height;
    public int MoveLimit;
}

public static class MiniGameCatalog
{
    public const string ScenesFolder = "Assets/_Project/Scenes/MiniGames";

    public static string SceneName(MiniGameType type, MiniGameDifficulty difficulty)
    {
        return "MiniGame_" + type + "_" + difficulty;
    }

    public static string ScenePath(MiniGameType type, MiniGameDifficulty difficulty)
    {
        return ScenesFolder + "/" + SceneName(type, difficulty) + ".unity";
    }

    public static string ControllerTypeName(MiniGameType type)
    {
        switch (type)
        {
            case MiniGameType.Pipes: return "PipeMiniGameController";
            case MiniGameType.Pattern: return "PatternMiniGameController";
            default: return "MemoryMiniGameController";
        }
    }

    public static bool TryParseSceneName(string sceneName, out MiniGameType type, out MiniGameDifficulty difficulty)
    {
        type = MiniGameType.Pipes;
        difficulty = MiniGameDifficulty.Easy;
        if (string.IsNullOrEmpty(sceneName)) return false;
        var parts = sceneName.Split('_');
        if (parts.Length != 3) return false;
        if (!Enum.TryParse(parts[1], out type)) return false;
        return Enum.TryParse(parts[2], out difficulty);
    }

    public static PipeSpec Pipes(MiniGameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MiniGameDifficulty.Easy: return new PipeSpec { Width = 3, Height = 3, RotatableTiles = 3, TimeLimitSeconds = 0, Hints = 0 };
            case MiniGameDifficulty.Medium: return new PipeSpec { Width = 4, Height = 3, RotatableTiles = 5, TimeLimitSeconds = 30, Hints = 1 };
            default: return new PipeSpec { Width = 4, Height = 4, RotatableTiles = 8, TimeLimitSeconds = 20, Hints = 2 };
        }
    }

    public static PatternSpec Pattern(MiniGameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MiniGameDifficulty.Easy: return new PatternSpec { ButtonCount = 4, SequenceLength = 4, FlashSeconds = 1.0f };
            case MiniGameDifficulty.Medium: return new PatternSpec { ButtonCount = 5, SequenceLength = 6, FlashSeconds = 0.8f };
            default: return new PatternSpec { ButtonCount = 6, SequenceLength = 8, FlashSeconds = 0.6f };
        }
    }

    public static MemorySpec Memory(MiniGameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MiniGameDifficulty.Easy: return new MemorySpec { Pairs = 3, Width = 3, Height = 2, MoveLimit = 0 };
            case MiniGameDifficulty.Medium: return new MemorySpec { Pairs = 6, Width = 4, Height = 3, MoveLimit = 12 };
            default: return new MemorySpec { Pairs = 8, Width = 4, Height = 4, MoveLimit = 16 };
        }
    }
}