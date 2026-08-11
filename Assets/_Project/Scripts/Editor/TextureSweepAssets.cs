using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TextureSweepAssets
{
    public const int MaxTextureSize = 2048;
    public const TextureImporterFormat Format = TextureImporterFormat.BC7;
    private const string PlatformName = "Standalone";

    private static bool _ensuring;

    public static void Ensure()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsureConsistentImportSettings();
        }
        finally
        {
            _ensuring = false;
        }
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    private static void EnsureConsistentImportSettings()
    {
        bool changed = false;

        foreach (var path in ProjectTexturePaths())
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            if (importer.maxTextureSize != MaxTextureSize)
            {
                importer.maxTextureSize = MaxTextureSize;
                changed = true;
            }

            if (!IsPinnedAt(importer))
            {
                var settings = importer.GetPlatformTextureSettings(PlatformName);
                settings.overridden = true;
                settings.maxTextureSize = MaxTextureSize;
                settings.format = Format;
                importer.SetPlatformTextureSettings(settings);
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }

        if (changed) AssetDatabase.SaveAssets();
    }

    public static string[] ProjectTexturePaths()
    {
        return AssetDatabase.FindAssets("t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/_Project"))
            .ToArray();
    }

    public static bool IsPinnedAt(TextureImporter importer)
    {
        if (importer.maxTextureSize > MaxTextureSize) return false;
        var settings = importer.GetPlatformTextureSettings(PlatformName);
        return settings.overridden
            && settings.maxTextureSize <= MaxTextureSize
            && settings.format == Format;
    }
}