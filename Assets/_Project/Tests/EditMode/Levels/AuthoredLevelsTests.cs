using System.Collections.Generic;
using NUnit.Framework;

public class AuthoredLevelsTests
{
    private List<LevelData> _levels;

    [OneTimeSetUp]
    public void LoadAllAuthoredLevels()
    {
        Assert.IsTrue(LevelLoader.TryLoadAll(LevelLoader.DefaultFolder, out _levels, out var error), error);
    }

    [Test]
    public void TwentyFiveLevels_SequentialIds()
    {
        Assert.AreEqual(25, _levels.Count, "The shipped level set must contain exactly 25 levels");
        for (int i = 0; i < _levels.Count; i++)
            Assert.AreEqual(i + 1, _levels[i].id, $"Level {i + 1} must have a sequential id");
    }

    [Test]
    public void UniqueNames()
    {
        var names = new HashSet<string>();
        foreach (var level in _levels)
            Assert.IsTrue(names.Add(level.name), $"Level name '{level.name}' is not unique");
    }

    [Test]
    public void EveryLevel_IsSolverSolvable()
    {
        foreach (var level in _levels)
            Assert.IsTrue(LevelSolver.Solvable(level), $"Level {level.id} ('{level.name}') has no solution");
    }
}