using System.Collections.Generic;
using UnityEngine;

public class MAPElitesGenerator
{
    // Ti le sinh HOAN TOAN ngau nhien (exploration) so voi dot bien tu 1
    // elite co san trong archive (exploitation). Ap dung tu vong lap dau
    // tien archive co it nhat 1 elite. Tang gia tri nay neu muon archive
    // da dang hon; giam neu muon hoi tu nhanh quanh cac elite tot.
    private const float RandomInitChance = 0.2f;

    // Ti le muc tieu Easy : Medium : Hard trong archive. Moi vong lap se
    // tinh do kho nao dang "thieu" nhat so voi ti le nay (dua tren so luong
    // elite da co trong archive) va chu dong nhan candidate moi ve dung do
    // kho do (giam/tang so NPC, doi loai NPC an toan/nguy hiem hon). Chinh
    // cac gia tri nay neu muon ti le khac (vi du 3:2:1, 1:1:1, ...).
    private static readonly Dictionary<LevelDifficulty, int> TargetDifficultyRatio =
        new Dictionary<LevelDifficulty, int>
        {
            { LevelDifficulty.Easy, 4 },
            { LevelDifficulty.Medium, 2 },
            { LevelDifficulty.Hard, 1 },
        };

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
    // tu archive roi dot bien no (mutation-based MAP-Elites). Ca 2 nhanh
    // deu nhan candidate ve dung do kho dang thieu nhat (targetDifficulty).
    private LevelData CreateCandidateLevel(int index)
    {
        LevelDifficulty targetDifficulty =
            DetermineTargetDifficulty();

        LevelData parent =
            archive.GetRandomElite(targetDifficulty);

        bool shouldExplore =
            parent == null ||
            Random.value < RandomInitChance;

        if (shouldExplore)
        {
            return CreateRandomLevel(index, targetDifficulty);
        }

        return mutator.Mutate(parent, targetDifficulty);
    }

    // Tim do kho dang "thieu" nhat so voi ti le muc tieu (TargetDifficultyRatio),
    // dua tren so luong elite THUC TE dang co trong archive (khong phai so
    // luong da sinh ra, vi nhieu candidate bi loai vi khong du tot). Chon do
    // kho co khoang cach (ti le mong muon - ti le hien tai) lon nhat.
    private LevelDifficulty DetermineTargetDifficulty()
    {
        Dictionary<LevelDifficulty, int> counts =
            archive.GetDifficultyCounts();

        int total =
            counts[LevelDifficulty.Easy] +
            counts[LevelDifficulty.Medium] +
            counts[LevelDifficulty.Hard];

        int totalRatio =
            TargetDifficultyRatio[LevelDifficulty.Easy] +
            TargetDifficultyRatio[LevelDifficulty.Medium] +
            TargetDifficultyRatio[LevelDifficulty.Hard];

        LevelDifficulty best = LevelDifficulty.Easy;
        float bestGap = float.NegativeInfinity;

        foreach (var pair in TargetDifficultyRatio)
        {
            float desiredProportion =
                (float)pair.Value / totalRatio;

            float currentProportion =
                total > 0
                    ? (float)counts[pair.Key] / total
                    : 0f;

            float gap =
                desiredProportion - currentProportion;

            if (gap > bestGap)
            {
                bestGap = gap;
                best = pair.Key;
            }
        }

        return best;
    }

    private LevelData CreateRandomLevel(int index, LevelDifficulty targetDifficulty)
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
            GetNpcCountForDifficulty(targetDifficulty);

        for (int i = 0; i < npcCount; i++)
        {
            int prefabIndex =
                LevelGenUtil.GetWeightedRandomPrefabIndex(targetDifficulty);

            if (LevelGenUtil.TryPlaceNPC(occupied, out NPCDef npc, prefabIndex))
            {
                level.npcs.Add(npc);
            }
            // Neu khong tim duoc cho trong sau nhieu lan thu, bo qua NPC nay
            // thay vi lam level bi loi (chong o).
        }

        return level;
    }

    // So luong NPC ban dau lech theo do kho muc tieu: Easy it NPC hon (giam
    // npcScore va giam kha nang can duong -> shortestPath cung ngan hon),
    // Hard nhieu NPC hon. Ket hop voi GetWeightedRandomPrefabIndex (uu tien
    // loai NPC an toan/nguy hiem tuong ung) de chu dong keo diem so ve dung
    // khoang do kho mong muon, thay vi random 6-9 NPC deu loai nhu cu.
    private int GetNpcCountForDifficulty(LevelDifficulty targetDifficulty)
    {
        switch (targetDifficulty)
        {
            case LevelDifficulty.Easy:
                return Random.Range(2, 5);

            case LevelDifficulty.Hard:
                return Random.Range(9, 13);

            default: // Medium - giu nguyen range cu
                return Random.Range(6, 10);
        }
    }
}