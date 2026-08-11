using NUnit.Framework;

public class MiniGameCatalogTests
{
    [Test]
    public void Catalog_NamesEveryTypeAtEveryDifficulty()
    {
        var types = new[] { MiniGameType.Pipes, MiniGameType.Pattern, MiniGameType.Memory };
        var difficulties = new[] { MiniGameDifficulty.Easy, MiniGameDifficulty.Medium, MiniGameDifficulty.Hard };

        foreach (var type in types)
        {
            foreach (var difficulty in difficulties)
                Assert.AreEqual("MiniGame_" + type + "_" + difficulty, MiniGameCatalog.SceneName(type, difficulty));
        }
    }

    [Test]
    public void PipesSpecs_MatchTheDesignTable()
    {
        var easy = MiniGameCatalog.Pipes(MiniGameDifficulty.Easy);
        var medium = MiniGameCatalog.Pipes(MiniGameDifficulty.Medium);
        var hard = MiniGameCatalog.Pipes(MiniGameDifficulty.Hard);

        Assert.AreEqual(3, easy.Width);
        Assert.AreEqual(3, easy.Height);
        Assert.AreEqual(3, easy.RotatableTiles);
        Assert.AreEqual(0, easy.TimeLimitSeconds);
        Assert.AreEqual(0, easy.Hints);

        Assert.AreEqual(4, medium.Width);
        Assert.AreEqual(3, medium.Height);
        Assert.AreEqual(5, medium.RotatableTiles);
        Assert.AreEqual(30, medium.TimeLimitSeconds);
        Assert.AreEqual(1, medium.Hints);

        Assert.AreEqual(4, hard.Width);
        Assert.AreEqual(4, hard.Height);
        Assert.AreEqual(8, hard.RotatableTiles);
        Assert.AreEqual(20, hard.TimeLimitSeconds);
        Assert.AreEqual(2, hard.Hints);
    }

    [Test]
    public void PatternSpecs_MatchTheDesignTable()
    {
        var easy = MiniGameCatalog.Pattern(MiniGameDifficulty.Easy);
        var medium = MiniGameCatalog.Pattern(MiniGameDifficulty.Medium);
        var hard = MiniGameCatalog.Pattern(MiniGameDifficulty.Hard);

        Assert.AreEqual(4, easy.ButtonCount);
        Assert.AreEqual(4, easy.SequenceLength);
        Assert.AreEqual(1.0f, easy.FlashSeconds);

        Assert.AreEqual(5, medium.ButtonCount);
        Assert.AreEqual(6, medium.SequenceLength);
        Assert.AreEqual(0.8f, medium.FlashSeconds);

        Assert.AreEqual(6, hard.ButtonCount);
        Assert.AreEqual(8, hard.SequenceLength);
        Assert.AreEqual(0.6f, hard.FlashSeconds);
    }

    [Test]
    public void MemorySpecs_MatchTheDesignTable()
    {
        var easy = MiniGameCatalog.Memory(MiniGameDifficulty.Easy);
        var medium = MiniGameCatalog.Memory(MiniGameDifficulty.Medium);
        var hard = MiniGameCatalog.Memory(MiniGameDifficulty.Hard);

        Assert.AreEqual(3, easy.Pairs);
        Assert.AreEqual(3, easy.Width);
        Assert.AreEqual(2, easy.Height);
        Assert.AreEqual(0, easy.MoveLimit);

        Assert.AreEqual(6, medium.Pairs);
        Assert.AreEqual(4, medium.Width);
        Assert.AreEqual(3, medium.Height);
        Assert.AreEqual(12, medium.MoveLimit);

        Assert.AreEqual(8, hard.Pairs);
        Assert.AreEqual(4, hard.Width);
        Assert.AreEqual(4, hard.Height);
        Assert.AreEqual(16, hard.MoveLimit);
    }
}