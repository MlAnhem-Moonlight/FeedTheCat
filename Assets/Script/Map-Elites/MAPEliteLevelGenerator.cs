using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MAPEliteLevelGenerator : EditorWindow
{
    const int ROWS = 8;
    const int COLS = 6;

    private int easyCount = 50;
    private int mediumCount = 50;
    private int hardCount = 50;

    [MenuItem("FeedTheCat/Generate Levels")]
    static void Open()
    {
        GetWindow<MAPEliteLevelGenerator>();
    }

    void OnGUI()
    {
        GUILayout.Label("Level Generator", EditorStyles.boldLabel);

        easyCount = EditorGUILayout.IntField("Easy", easyCount);
        mediumCount = EditorGUILayout.IntField("Medium", mediumCount);
        hardCount = EditorGUILayout.IntField("Hard", hardCount);

        if (GUILayout.Button("Generate"))
        {
            GenerateAll();
        }
    }

    void GenerateAll()
    {
        GenerateDifficulty(LevelDifficulty.Easy, easyCount);
        GenerateDifficulty(LevelDifficulty.Medium, mediumCount);
        GenerateDifficulty(LevelDifficulty.Hard, hardCount);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Generate Complete");
    }

    void GenerateDifficulty(LevelDifficulty difficulty, int count)
    {
        string folder =
            $"Assets/ScriptableObject/Level/{difficulty}";

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObject/Level"))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(
                "Assets/ScriptableObject/Level",
                difficulty.ToString()
            );
        }

        for (int i = 0; i < count; i++)
        {
            LevelData level =
                GenerateLevel(difficulty,i);

            string assetPath =
                $"{folder}/{difficulty}_{i + 1:D3}.asset";

            AssetDatabase.CreateAsset(level, assetPath);
        }
    }

    LevelData GenerateLevel(LevelDifficulty difficulty, int index)
    {
        LevelData level =
            ScriptableObject.CreateInstance<LevelData>();

        level.levelName =
            difficulty.ToString() + "_" + (index).ToString("D3");

        HashSet<Vector2Int> occupied =
            new HashSet<Vector2Int>();

        // PLAYER
        Vector2Int start =
            RandomCell();

        level.playerStart = start;

        occupied.Add(start);

        // GOAL
        Vector2Int goal =
            RandomFarCell(start);

        level.destinations.Add(goal);

        occupied.Add(goal);

        // ENEMY COUNT
        int enemyCount = GetEnemyCount(difficulty);

        for (int i = 0; i < enemyCount; i++)
        {
            NPCDef npc =
                GenerateNPC(difficulty, occupied);

            level.npcs.Add(npc);

            occupied.Add(
                new Vector2Int(
                    npc.row,
                    npc.column
                )
            );
        }

        return level;
    }

    int GetEnemyCount(LevelDifficulty difficulty)
    {
        switch (difficulty)
        {
            case LevelDifficulty.Easy:
                return Random.Range(2, 5);

            case LevelDifficulty.Medium:
                return Random.Range(4, 7);

            case LevelDifficulty.Hard:
                return Random.Range(7, 10);
        }

        return 3;
    }

    NPCDef GenerateNPC(
        LevelDifficulty difficulty,
        HashSet<Vector2Int> occupied)
    {
        NPCDef npc =
            new NPCDef();

        Vector2Int pos;

        do
        {
            pos = RandomCell();
        }
        while (occupied.Contains(pos));

        npc.row = pos.x;
        npc.column = pos.y;

        npc.prefabIndex =
            GetPrefabByDifficulty(difficulty);

        return npc;
    }

    int GetPrefabByDifficulty(
        LevelDifficulty difficulty)
    {
        switch (difficulty)
        {
            case LevelDifficulty.Easy:

                return Random.Range(0, 6);

            case LevelDifficulty.Medium:

                return Random.Range(0, 8);

            case LevelDifficulty.Hard:

                return Random.Range(0, 9);
        }

        return 0;
    }

    Vector2Int RandomCell()
    {
        return new Vector2Int(
            Random.Range(0, ROWS),
            Random.Range(0, COLS)
        );
    }

    Vector2Int RandomFarCell(
        Vector2Int start)
    {
        Vector2Int cell;

        do
        {
            cell = RandomCell();
        }
        while (
            Mathf.Abs(cell.x - start.x)
            + Mathf.Abs(cell.y - start.y)
            < 6
        );

        return cell;
    }
}