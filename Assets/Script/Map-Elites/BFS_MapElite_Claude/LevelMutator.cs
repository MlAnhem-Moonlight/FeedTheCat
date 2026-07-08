using System.Collections.Generic;
using UnityEngine;

// Dot bien (mutate) 1 level co san de tao ung vien level moi cho MAP-Elites,
// thay vi luon sinh hoan toan ngau nhien tu dau. Day la phan "exploitation"
// (khai thac quanh cac elite dang co) bo sung cho phan "exploration" (sinh
// ngau nhien) trong MAPElitesGenerator.
//
// Khong ke thua MonoBehaviour vi khong can gan vao GameObject - duoc
// MAPElitesGenerator tao va goi truc tiep.
public class LevelMutator
{
    // Moi lan Mutate() se ap dung ngau nhien 1-3 thao tac dot bien lien tiep.
    private const int MinOps = 1;
    private const int MaxOps = 3;

    private const int MinNpcCount = 1;
    private const int MaxNpcCount = 12;

    public LevelData Mutate(LevelData source)
    {
        LevelData mutant = CloneLevel(source);

        HashSet<Vector2Int> occupied =
            LevelGenUtil.BuildOccupiedCells(mutant);

        int opsCount = Random.Range(MinOps, MaxOps + 1);

        for (int i = 0; i < opsCount; i++)
        {
            ApplyRandomOperation(mutant, occupied);
        }

        return mutant;
    }

    private LevelData CloneLevel(LevelData source)
    {
        LevelData copy =
            ScriptableObject.CreateInstance<LevelData>();

        // Ten tam thoi - MAPElitesArchive.TryInsert se gan lai tien to
        // {Easy|Medium|Hard}_ khi biet do kho thuc te cua level nay.
        copy.levelName = "Mutant";

        copy.playerStart = source.playerStart;
        copy.hasWon = false;
        copy.isLocked = source.isLocked;

        copy.destinations = new List<Vector2Int>(source.destinations);
        copy.blockedCells = new List<Vector2Int>(source.blockedCells);

        copy.items = new List<ItemDef>();
        foreach (var item in source.items)
        {
            copy.items.Add(new ItemDef
            {
                row = item.row,
                column = item.column,
                prefabIndex = item.prefabIndex
            });
        }

        copy.npcs = new List<NPCDef>();
        foreach (var npc in source.npcs)
        {
            copy.npcs.Add(CloneNPC(npc));
        }

        return copy;
    }

    private NPCDef CloneNPC(NPCDef source)
    {
        return new NPCDef
        {
            row = source.row,
            column = source.column,
            prefabIndex = source.prefabIndex,
            seed = source.seed,
            fixedSteps = source.fixedSteps,
            minRandomSteps = source.minRandomSteps,
            maxRandomSteps = source.maxRandomSteps
        };
    }

    private void ApplyRandomOperation(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        // Trong so % cho tung loai dot bien - chinh lai neu muon thien
        // ve huong nao do (vi du tang % doi NPC de kho tang nhanh hon).
        int roll = Random.Range(0, 100);

        if (roll < 35)
            MoveRandomNPC(level, occupied);
        else if (roll < 50)
            ChangeRandomNPCType(level, occupied);
        else if (roll < 62)
            MoveGoal(level, occupied);
        else if (roll < 74)
            MovePlayer(level, occupied);
        else if (roll < 86)
            MoveItem(level, occupied);
        else if (roll < 93)
            AddNPC(level, occupied);
        else
            RemoveRandomNPC(level, occupied);
    }

    private void MoveRandomNPC(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.npcs.Count == 0)
            return;

        int index = Random.Range(0, level.npcs.Count);
        NPCDef npc = level.npcs[index];

        FreeNPCCells(npc, occupied);

        if (!LevelGenUtil.TryPlaceNPC(occupied, out NPCDef moved, npc.prefabIndex))
        {
            // Khong tim duoc cho moi trong sau nhieu lan thu -> giu nguyen vi tri cu
            ReoccupyNPCCells(npc, occupied);
            return;
        }

        // Giu nguyen thong so cu (seed, so buoc), chi doi vi tri
        moved.seed = npc.seed;
        moved.fixedSteps = npc.fixedSteps;
        moved.minRandomSteps = npc.minRandomSteps;
        moved.maxRandomSteps = npc.maxRandomSteps;

        level.npcs[index] = moved;
    }

    private void ChangeRandomNPCType(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.npcs.Count == 0)
            return;

        int index = Random.Range(0, level.npcs.Count);
        NPCDef npc = level.npcs[index];

        Vector2Int oldPos = new Vector2Int(npc.row, npc.column);

        FreeNPCCells(npc, occupied);

        int newType = Random.Range(0, 9);

        // Thu giu nguyen vi tri cu voi loai moi truoc (vi du: doi tu Fixed
        // sang Idle can them 1 o nen co the khong con hop le tai cho cu).
        if (!LevelGenUtil.TryPlaceNPC(occupied, out NPCDef changed, newType, oldPos))
        {
            // Khong hop le tai vi tri cu -> tim vi tri moi bat ky cho loai moi nay
            if (!LevelGenUtil.TryPlaceNPC(occupied, out changed, newType))
            {
                // That bai hoan toan -> giu nguyen NPC cu, khong mat gi ca
                ReoccupyNPCCells(npc, occupied);
                return;
            }
        }

        changed.seed = npc.seed;
        level.npcs[index] = changed;
    }

    private void MoveGoal(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.destinations.Count == 0)
            return;

        occupied.Remove(level.destinations[0]);

        // Dich moi phai cach player it nhat MinPlayerGoalDistance,
        // tranh dot bien vo tinh keo dich lai sat player.
        Vector2Int newGoal =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                level.playerStart,
                LevelGenUtil.MinPlayerGoalDistance);

        occupied.Add(newGoal);
        level.destinations[0] = newGoal;
    }

    private void MovePlayer(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        occupied.Remove(level.playerStart);

        // Vi tri player moi phai cach dich it nhat MinPlayerGoalDistance,
        // tranh dot bien vo tinh keo player lai sat dich.
        Vector2Int reference =
            level.destinations.Count > 0
                ? level.destinations[0]
                : level.playerStart;

        Vector2Int newPos =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                reference,
                LevelGenUtil.MinPlayerGoalDistance);

        occupied.Add(newPos);
        level.playerStart = newPos;
    }

    private void MoveItem(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.items.Count == 0)
            return;

        ItemDef item = level.items[0];
        occupied.Remove(new Vector2Int(item.row, item.column));

        Vector2Int newPos =
            LevelGenUtil.GetUniqueRandomCell(occupied);

        occupied.Add(newPos);

        item.row = newPos.x;
        item.column = newPos.y;
        level.items[0] = item;
    }

    private void AddNPC(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.npcs.Count >= MaxNpcCount)
            return;

        if (LevelGenUtil.TryPlaceNPC(occupied, out NPCDef npc))
        {
            level.npcs.Add(npc);
        }
    }

    private void RemoveRandomNPC(
        LevelData level,
        HashSet<Vector2Int> occupied)
    {
        if (level.npcs.Count <= MinNpcCount)
            return;

        int index = Random.Range(0, level.npcs.Count);

        FreeNPCCells(level.npcs[index], occupied);
        level.npcs.RemoveAt(index);
    }

    private void FreeNPCCells(
        NPCDef npc,
        HashSet<Vector2Int> occupied)
    {
        Vector2Int pos = new Vector2Int(npc.row, npc.column);
        occupied.Remove(pos);

        if (LevelGenUtil.OccupiesTwoCells(npc.prefabIndex))
        {
            Vector2Int second =
                pos + LevelGenUtil.GetIdleDirection(npc.prefabIndex);

            occupied.Remove(second);
        }
    }

    private void ReoccupyNPCCells(
        NPCDef npc,
        HashSet<Vector2Int> occupied)
    {
        Vector2Int pos = new Vector2Int(npc.row, npc.column);
        occupied.Add(pos);

        if (LevelGenUtil.OccupiesTwoCells(npc.prefabIndex))
        {
            Vector2Int second =
                pos + LevelGenUtil.GetIdleDirection(npc.prefabIndex);

            occupied.Add(second);
        }
    }
}