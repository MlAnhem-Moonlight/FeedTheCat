using UnityEditor;
using UnityEngine;

public class MAPElitesWindow : EditorWindow
{
    private int iterations = 5000;

    private MAPElitesGenerator generator;

    [MenuItem("FeedTheCat/MAP-Elites Generator BFS")]
    public static void Open()
    {
        GetWindow<MAPElitesWindow>(
            "MAP-Elites");
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Feed The Cat Generator",
            EditorStyles.boldLabel);

        iterations =
            EditorGUILayout.IntField(
                "Iterations",
                iterations);

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Generate"))
        {
            Generate();
        }

        GUILayout.Space(20);

        if (generator != null)
        {
            DrawArchive();
        }
        GUILayout.Space(20);

        if (generator != null)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Export Easy"))
            {
                ExportDifficulty(
                    LevelDifficulty.Easy);
            }

            if (GUILayout.Button("Export Medium"))
            {
                ExportDifficulty(
                    LevelDifficulty.Medium);
            }

            if (GUILayout.Button("Export Hard"))
            {
                ExportDifficulty(
                    LevelDifficulty.Hard);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void Generate()
    {
        generator =
            new MAPElitesGenerator();

        generator.Run(
            iterations);

        Debug.Log(
            "Generation Complete");
    }

    private void DrawArchive()
    {
        GUILayout.Label(
            "Archive");

        for (
            int y = generator.archive.height - 1;
            y >= 0;
            y--)
        {
            GUILayout.BeginHorizontal();

            for (
                int x = 0;
                x < generator.archive.width;
                x++)
            {
                EliteCellBFS cell =
                    generator.archive.Get(x, y);
                if (!cell.occupied)
                {
                    GUI.backgroundColor =
                        Color.gray;
                }
                else
                {
                    switch (cell.difficulty)
                    {
                        case LevelDifficulty.Easy:
                            GUI.backgroundColor =
                                Color.green;
                            break;

                        case LevelDifficulty.Medium:
                            GUI.backgroundColor =
                                Color.yellow;
                            break;

                        case LevelDifficulty.Hard:
                            GUI.backgroundColor =
                                Color.red;
                            break;
                    }
                }

                if (
                    GUILayout.Button(
                        "",
                        GUILayout.Width(20),
                        GUILayout.Height(20)))
                {
                    if (cell.occupied)
                    {
                        SaveLevel(cell);
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor =
            Color.white;
    }

    private void SaveLevel(
        EliteCellBFS cell)
    {
        string path =
            EditorUtility.SaveFilePanelInProject(
                "Save Level",
                "GeneratedLevel",
                "asset",
                "");

        if (string.IsNullOrEmpty(path))
            return;

        LevelData copy =
            Object.Instantiate(
                cell.level);

        AssetDatabase.CreateAsset(
            copy,
            path);

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Saved: " + path);
    }

    private LevelDifficulty Classify(
    DifficultyResultBFS r)
    {
        if (
            r.shortestPath < 8 &&
            r.reachableStates < 50)
        {
            return LevelDifficulty.Easy;
        }

        if (
            r.shortestPath < 20 &&
            r.reachableStates < 150)
        {
            return LevelDifficulty.Medium;
        }

        return LevelDifficulty.Hard;
    }

    private void ExportDifficulty(
    LevelDifficulty difficulty)
    {
        string folder =
            EditorUtility.OpenFolderPanel(
                "Save Levels",
                "Assets",
                "");

        if (string.IsNullOrEmpty(folder))
            return;

        folder =
            FileUtil.GetProjectRelativePath(
                folder);

        int count = 0;

        for (int x = 0; x < generator.archive.width; x++)
        {
            for (int y = 0; y < generator.archive.height; y++)
            {
                EliteCellBFS cell =
                    generator.archive.Get(x, y);

                if (!cell.occupied)
                    continue;

                if (cell.difficulty != difficulty)
                    continue;

                LevelData copy =
                    Object.Instantiate(
                        cell.level);

                AssetDatabase.CreateAsset(
                    copy,
                    $"{folder}/Level_{count}.asset");

                count++;
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Exported {count} {difficulty} levels");
    }
}