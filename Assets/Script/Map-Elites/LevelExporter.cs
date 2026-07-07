using UnityEditor;
using UnityEngine;

public static class LevelExporter
{
    public static void Save(
        LevelData level,
        string path)
    {
        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
    }
}