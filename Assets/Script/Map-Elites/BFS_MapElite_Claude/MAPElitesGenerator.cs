using System.Collections.Generic;
using UnityEngine;

public class MAPElitesGenerator
{
    // Ti le sinh HOAN TOAN ngau nhien (exploration) so voi dot bien tu 1
    // elite co san trong archive (exploitation). Ap dung tu vong lap dau
    // tien archive co it nhat 1 elite. Tang gia tri nay neu muon archive
    // da dang hon; giam neu muon hoi tu nhanh quanh cac elite tot.
    private const float RandomInitChance = 0.2f;

    public MAPElitesArchive archive;

    private BFSSolver solver =
        new BFSSolver();

    private LevelMutator mutator =
        new LevelMutator();

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
                CreateCandidateLevel(i);

            DifficultyResultBFS d =
                solver.Evaluate(level);

            if (!d.solvable)
            {
                Object.DestroyImmediate(level);
                continue;
            }

            bool kept =
                archive.TryInsert(
                    level,
                    d);

            if (!kept)
            {
                // Level giai duoc nhung khong du tot de vao archive
                // (o cua no da co elite tot hon) -> huy de tranh ro ri bo nho.
                Object.DestroyImmediate(level);
            }
            else
            {
                validLevels++;
            }

            if (i % 100 == 0 && i > 0)
            {
                System.GC.Collect();
            }
        }

        Debug.Log($"Generated {validLevels} valid levels out of {iterations} iterations");
    }

    // Quyet dinh: sinh moi hoan toan ngau nhien, hay lay 1 elite ngau nhien
    // tu archive roi dot bien no (mutation-based MAP-Elites).
    private LevelData CreateCandidateLevel(int index)
    {
        LevelData parent =
            archive.GetRandomElite();

        bool shouldExplore =
            parent == null ||
            Random.value < RandomInitChance;

        if (shouldExplore)
        {
            return CreateRandomLevel(index);
        }

        return mutator.Mutate(parent);
    }

    private LevelData CreateRandomLevel(int index)
    {
        LevelData level =
            ScriptableObject.CreateInstance<LevelData>();

        // Chi la ten tam thoi, MAPElitesArchive.TryInsert se gan tien to
        // {Easy|Medium|Hard}_ khi biet do kho thuc te cua level.
        level.levelName =
            $"Generated_{index}";

        // Tap hop tat ca cac o da bi chiem (destination, player, item, NPC)
        // de dam bao khong co gi chong len nhau khi sinh level.
        HashSet<Vector2Int> occupied =
            new HashSet<Vector2Int>();

        Vector2Int destination =
            LevelGenUtil.GetRandomCell();

        occupied.Add(destination);
        level.destinations.Add(destination);

        Vector2Int playerStart =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                destination,
                LevelGenUtil.MinPlayerGoalDistance);

        occupied.Add(playerStart);
        level.playerStart = playerStart;

        Vector2Int itemPos =
            LevelGenUtil.GetUniqueRandomCell(occupied);

        occupied.Add(itemPos);
        level.items.Add(new ItemDef
        {
            row = itemPos.x,
            column = itemPos.y,
            prefabIndex = 0
        });

        int npcCount =
            Random.Range(6, 10);

        for (int i = 0; i < npcCount; i++)
        {
            if (LevelGenUtil.TryPlaceNPC(occupied, out NPCDef npc))
            {
                level.npcs.Add(npc);
            }
            // Neu khong tim duoc cho trong sau nhieu lan thu, bo qua NPC nay
            // thay vi lam level bi loi (chong o).
        }

        return level;
    }
}