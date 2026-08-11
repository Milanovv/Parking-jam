using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class LevelLoaderTests
{
    private string _folder;
    private string _path;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "parking-jam-levels-" + Guid.NewGuid());
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "level_001.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private static string ValidJson()
    {
        return @"
{
  ""id"": 1,
  ""name"": ""Getting Started"",
  ""gridWidth"": 5,
  ""gridHeight"": 5,
  ""levelUndos"": 5,
  ""exitTiles"": [{""x"": 4, ""y"": 2}],
  ""vehicles"": [
    { ""id"": ""car_red"", ""orientation"": ""horizontal"",
      ""tiles"": [{""x"": 0, ""y"": 2}, {""x"": 1, ""y"": 2}] },
    { ""id"": ""car_blue"", ""orientation"": ""vertical"",
      ""tiles"": [{""x"": 2, ""y"": 0}, {""x"": 2, ""y"": 1}] }
  ]
}";
    }

    [Test]
    public void Load_ValidFile_ParsesAndValidates()
    {
        File.WriteAllText(_path, ValidJson());

        LevelData level;
        string error;
        Assert.IsTrue(LevelLoader.TryLoadFromFile(_path, out level, out error), error);
        Assert.AreEqual(1, level.id);
        Assert.AreEqual("Getting Started", level.name);
        Assert.AreEqual(5, level.gridWidth);
        Assert.AreEqual(5, level.levelUndos);
        Assert.AreEqual(2, level.vehicles.Length);
        Assert.AreEqual(1, level.exitTiles.Length);
        Assert.AreEqual(level.vehicles[0].tiles[0], new Vector2Int(0, 2));
        Assert.AreEqual(level.vehicles[1].tiles[1], new Vector2Int(2, 1));
        Assert.AreEqual(level.exitTiles[0], new Vector2Int(4, 2));
    }

    [Test]
    public void Load_MissingFile_FailsClosed()
    {
        LevelData level;
        string error;
        Assert.IsFalse(LevelLoader.TryLoadFromFile(_path, out level, out error));
        Assert.IsNull(level, "A failed load never yields a half-loaded level");
        Assert.IsNotEmpty(error);
    }

    [Test]
    public void Load_NotJson_FailsClosedWithMessage()
    {
        File.WriteAllText(_path, "this is not json {{{");

        LevelData level;
        string error;
        Assert.IsFalse(LevelLoader.TryLoadFromFile(_path, out level, out error));
        Assert.IsNull(level);
        Assert.IsNotEmpty(error);
    }

    [Test]
    public void Load_EmptyFile_FailsClosed()
    {
        File.WriteAllText(_path, "");

        LevelData level;
        string error;
        Assert.IsFalse(LevelLoader.TryLoadFromFile(_path, out level, out error));
        Assert.IsNull(level);
        Assert.IsNotEmpty(error);
    }

    [Test]
    public void Load_InvalidLevel_FailsClosedWithValidationError()
    {
        File.WriteAllText(_path, ValidJson().Replace("\"gridWidth\": 5", "\"gridWidth\": 3"));

        LevelData level;
        string error;
        Assert.IsFalse(LevelLoader.TryLoadFromFile(_path, out level, out error));
        Assert.IsNull(level);
        Assert.IsTrue(error.Contains("validation"), error);
        Assert.IsTrue(error.Contains("gridWidth"), error);
    }

    [Test]
    public void LoadAll_CollectsEveryJsonFile()
    {
        File.WriteAllText(_path, ValidJson());
        File.WriteAllText(Path.Combine(_folder, "level_002.json"), ValidJson().Replace("\"id\": 1", "\"id\": 2"));

        var levels = LevelLoader.TryLoadAll(_folder, out var result, out var error);
        Assert.IsTrue(levels, error);
        Assert.AreEqual(2, result.Count);
    }

    [Test]
    public void LoadAll_SkipsInvalidFilesAndKeepsTheValidOnes()
    {
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new Regex("not valid level JSON"));
        File.WriteAllText(_path, ValidJson());
        File.WriteAllText(Path.Combine(_folder, "level_002.json"), "not json {{{");
        File.WriteAllText(Path.Combine(_folder, "level_003.json"), ValidJson().Replace("\"id\": 1", "\"id\": 3"));

        var levels = LevelLoader.TryLoadAll(_folder, out var result, out var error);
        Assert.IsTrue(levels, "A single invalid file must not abort the whole set");
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result[0].id);
        Assert.AreEqual(3, result[1].id);
    }

    [Test]
    public void LoadAll_MissingFolder_FailsClosed()
    {
        Assert.IsFalse(LevelLoader.TryLoadAll(Path.Combine(_folder, "nope"), out _, out var error));
        Assert.IsNotEmpty(error);
    }
}