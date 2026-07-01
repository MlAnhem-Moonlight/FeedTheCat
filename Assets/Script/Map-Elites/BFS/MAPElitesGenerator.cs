using UnityEngine;

public class MAPElitesGenerator
{
    public MAPElitesArchive archive;

    private BFSSolver solver =
        new BFSSolver();

    public MAPElitesGenerator()
    {
        archive =
            new MAPElitesArchive(
                20,
                20);
    }

    public void Run(int iterations)
    {
        int validLevels = 0;

        for (int i = 0; i < iterations; i++)
        {
            LevelData level =
                CreateRandomLevel();

            DifficultyResultBFS d =
                solver.Evaluate(level);

            if (!d.solvable)
            {
                Object.DestroyImmediate(level);
                continue;
            }

            archive.TryInsert(
                level,
                d);

            validLevels++;

            if (i % 100 == 0 && i > 0)
            {
                System.GC.Collect();
            }
        }

        Debug.Log($"Generated {validLevels} valid levels out of {iterations} iterations");
    }

    private LevelData CreateRandomLevel()
    {
        LevelData level =
            ScriptableObject.CreateInstance<LevelData>();

        level.levelName =
            "Generated";

        level.playerStart =
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6));

        level.destinations.Add(
            new Vector2Int(
                Random.Range(0, 8),
                Random.Range(0, 6)));

        int npcCount =
            Random.Range(6, 10);

        for (int i = 0; i < npcCount; i++)
        {
            level.npcs.Add(
                CreateRandomNPC());
        }

        return level;
    }

    private NPCDef CreateRandomNPC()
    {
        NPCDef npc =
            new NPCDef();

        npc.prefabIndex =
            Random.Range(0, 9);

        npc.row =
            Random.Range(0, 8);

        npc.column =
            Random.Range(0, 6);

        npc.seed =
            Random.Range(
                int.MinValue,
                int.MaxValue);

        npc.fixedSteps = 3;
        npc.minRandomSteps = 1;
        npc.maxRandomSteps = 4;

        return npc;
    }
}