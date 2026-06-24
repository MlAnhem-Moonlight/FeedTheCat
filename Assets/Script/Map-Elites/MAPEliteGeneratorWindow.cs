using UnityEditor;
using UnityEngine;

public class MAPEliteGeneratorWindow :
    EditorWindow
{
    int iterations = 5000;

    [MenuItem(
        "FeedTheCat/MAP-Elites Generator 2")]
    static void Open()
    {
        GetWindow<MAPEliteGeneratorWindow>();
    }

    private void OnGUI()
    {
        iterations =
            EditorGUILayout.IntField(
                "Iterations",
                iterations);

        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
    }

    void Generate()
    {
        MAPEliteArchive archive =
            new MAPEliteArchive(5, 5);

        for (int i = 0; i < iterations; i++)
        {
            LevelData level =
                LevelCandidateGenerator.Generate();

            DifficultyResult r =
                LevelDifficultySolver.Evaluate(
                    level);

            if (!r.solvable)
                continue;

            int x =
                Mathf.Clamp(
                    r.shortestPath / 5,
                    0,
                    4);

            int y =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        r.branchingFactor),
                    0,
                    4);

            archive.TryInsert(
                x,
                y,
                r.fitness,
                level);
        }

        string root =
            "Assets/ScriptableObject/Level";

        if (!AssetDatabase.IsValidFolder(root))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "GeneratedLevels");
        }

        int index = 0;

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                EliteCell cell =
                    archive.GetCell(x, y);

                if (cell.level == null)
                    continue;

                LevelExporter.Save(
                    Object.Instantiate(cell.level),
                    $"{root}/Level_{index}.asset");

                index++;
            }
        }

        Debug.Log(
            $"Generated {index} MAP-Elites levels");
    }
}