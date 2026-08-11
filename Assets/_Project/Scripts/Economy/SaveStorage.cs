using System.IO;
using UnityEngine;

public class SaveStorage
{
    private readonly string _filePath;

    public SaveStorage(string filePath)
    {
        _filePath = filePath;
    }

    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

    public void Save(SaveData data)
    {
        File.WriteAllText(_filePath, JsonUtility.ToJson(data));
    }

    public SaveData Load()
    {
        if (!File.Exists(_filePath)) return null;
        try
        {
            string text = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(text) || !text.Contains("{")) return null;
            return JsonUtility.FromJson<SaveData>(text);
        }
        catch
        {
            return null;
        }
    }
}