using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LevelLoader
{
    public static string DefaultFolder
    {
        get { return Path.Combine(Application.streamingAssetsPath, "Levels"); }
    }

    public static bool TryLoad(int levelId, out LevelData level, out string error)
    {
        return TryLoadFromFile(PathFor(levelId), out level, out error);
    }

    public static bool TryLoadFromFile(string path, out LevelData level, out string error)
    {
        level = null;
        if (!File.Exists(path))
        {
            error = $"Level file not found: {path}";
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception)
        {
            error = $"Failed to read level file {path}: {exception.Message}";
            return false;
        }

        LevelData parsed;
        try
        {
            parsed = JsonUtility.FromJson<LevelData>(json);
        }
        catch (Exception exception)
        {
            error = $"Level file {path} is not valid level JSON: {exception.Message}";
            return false;
        }
        if (parsed == null)
        {
            error = $"Level file {path} is not valid level JSON";
            return false;
        }

        string validation;
        if (!LevelValidator.TryValidate(parsed, out validation))
        {
            error = $"Level file {path} failed validation: {validation}";
            return false;
        }

        level = parsed;
        error = null;
        return true;
    }

    public static bool TryLoadAll(string folder, out List<LevelData> levels, out string error)
    {
        levels = new List<LevelData>();
        if (!Directory.Exists(folder))
        {
            error = $"Levels folder not found: {folder}";
            return false;
        }

        foreach (var path in Directory.GetFiles(folder, "*.json"))
        {
            LevelData level;
            string fileError;
            if (!TryLoadFromFile(path, out level, out fileError))
            {
                Debug.LogError(fileError);
                continue;
            }
            levels.Add(level);
        }
        error = null;
        return true;
    }

    public static string PathFor(int levelId)
    {
        return Path.Combine(DefaultFolder, string.Format("level_{0:D3}.json", levelId));
    }
}