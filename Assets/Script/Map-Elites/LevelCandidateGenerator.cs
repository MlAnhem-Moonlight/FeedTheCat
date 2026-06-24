using UnityEngine;

public static class LevelCandidateGenerator
{
    public static LevelData Generate()
    {
        LevelData level =
            ScriptableObject.CreateInstance<LevelData>();

        level.playerStart =
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6));

        level.destinations.Add(
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6)));

        int enemyCount =
            Random.Range(4, 12);

        for (int i = 0; i < enemyCount; i++)
        {
            NPCDef npc =
                new NPCDef();

            npc.row =
                Random.Range(0, 8);

            npc.column =
                Random.Range(0, 6);

            npc.prefabIndex =
                Random.Range(0, 9);

            level.npcs.Add(npc);
        }

        return level;
    }
}