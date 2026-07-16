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

    // Giam tu 12 -> 9 -> 8 de khop voi tran NPC moi cua MAPElitesGenerator
    // (Hard toi da 5-8 NPC luc sinh moi) - tranh truong hop AddNPC lien tuc
    // qua nhieu vong dot bien lai day so luong NPC vuot xa muc sinh-moi ban
    // dau, gay nghet ban do 8x6.
    private const int MaxNpcCount = 8;

    // target: do kho ma MAPElitesGenerator dang muon nhan candidate nay toi
    // (dua tren ti le muc tieu, vi du 4:2:1 cho Easy:Medium:Hard). Anh huong
    // toi trong so cac phep dot bien (them/bot/doi loai NPC) - mac dinh
    // Medium neu khong truyen vao, giu hanh vi cu.
    public LevelData Mutate(LevelData source, LevelDifficulty target = LevelDifficulty.Medium)
    {
        LevelData mutant = CloneLevel(source);

        HashSet<Vector2Int> occupied =
            LevelGenUtil.BuildOccupiedCells(mutant);

        int opsCount = Random.Range(MinOps, MaxOps + 1);

        for (int i = 0; i < opsCount; i++)
        {
            ApplyRandomOperation(mutant, occupied, target);
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
        HashSet<Vector2Int> occupied,
        LevelDifficulty target)
    {
        // Trong so % cho tung loai dot bien, lech theo do kho muc tieu:
        // - Easy: uu tien Remove NPC / doi sang loai NPC an toan hon, han che Add NPC.
        // - Hard: uu tien Add NPC / doi sang loai NPC nguy hiem hon, han che Remove NPC.
        // - Medium: giu nguyen ti le goc.
        // Thu tu: MoveNPC, ChangeType, MoveGoal, MovePlayer, MoveItem, AddNPC, RemoveNPC (tong = 100).
        int[] weights = GetOperationWeights(target);

        int roll = Random.Range(0, 100);
        int cumulative = 0;

        cumulative += weights[0];
        if (roll < cumulative) { MoveRandomNPC(level, occupied); return; }

        cumulative += weights[1];
        if (roll < cumulative) { ChangeRandomNPCType(level, occupied, target); return; }

        cumulative += weights[2];
        if (roll < cumulative) { MoveGoal(level, occupied); return; }

        cumulative += weights[3];
        if (roll < cumulative) { MovePlayer(level, occupied); return; }

        cumulative += weights[4];
        if (roll < cumulative) { MoveItem(level, occupied); return; }

        cumulative += weights[5];
        if (roll < cumulative) { AddNPC(level, occupied, target); return; }

        RemoveRandomNPC(level, occupied);
    }

    private int[] GetOperationWeights(LevelDifficulty target)
    {
        switch (target)
        {
            case LevelDifficulty.Easy:
                return new[] { 30, 20, 10, 10, 10, 3, 17 };

            case LevelDifficulty.Hard:
                return new[] { 30, 20, 10, 10, 10, 17, 3 };

            default: // Medium - giong ti le goc truoc khi co quota
                return new[] { 35, 15, 12, 12, 12, 7, 7 };
        }
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
        HashSet<Vector2Int> occupied,
        LevelDifficulty target)
    {
        if (level.npcs.Count == 0)
            return;

        int index = Random.Range(0, level.npcs.Count);
        NPCDef npc = level.npcs[index];

        Vector2Int oldPos = new Vector2Int(npc.row, npc.column);

        FreeNPCCells(npc, occupied);

        // Loai NPC moi lech theo do kho muc tieu (Easy -> uu tien Idle,
        // Hard -> uu tien Random Patrol/Way) thay vi random deu 0-8.
        int newType = LevelGenUtil.GetWeightedRandomPrefabIndex(target);

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

        Vector2Int newGoal;

        if (level.items.Count > 0)
        {
            // Dich moi phai VUA cach player it nhat MinPlayerGoalDistance,
            // VUA cach item it nhat MinItemGoalDistance - tranh dot bien vo
            // tinh keo dich lai sat player HOAC sat item co san.
            Vector2Int itemPos =
                new Vector2Int(
                    level.items[0].row,
                    level.items[0].column);

            newGoal =
                LevelGenUtil.GetUniqueRandomCellFarFromBoth(
                    occupied,
                    level.playerStart,
                    LevelGenUtil.MinPlayerGoalDistance,
                    itemPos,
                    LevelGenUtil.MinItemGoalDistance);
        }
        else
        {
            // Dich moi phai cach player it nhat MinPlayerGoalDistance,
            // tranh dot bien vo tinh keo dich lai sat player.
            newGoal =
                LevelGenUtil.GetUniqueRandomCellFarFrom(
                    occupied,
                    level.playerStart,
                    LevelGenUtil.MinPlayerGoalDistance);
        }

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

        // Item moi phai cach dich it nhat MinItemGoalDistance - tranh dot
        // bien vo tinh keo item lai sat dich (nguoi choi toi dich la vo tinh
        // "tien" luon reward ma khong can chu y gi ca).
        Vector2Int reference =
            level.destinations.Count > 0
                ? level.destinations[0]
                : level.playerStart;

        Vector2Int newPos =
            LevelGenUtil.GetUniqueRandomCellFarFrom(
                occupied,
                reference,
                LevelGenUtil.MinItemGoalDistance);

        occupied.Add(newPos);

        item.row = newPos.x;
        item.column = newPos.y;
        level.items[0] = item;
    }

    private void AddNPC(
        LevelData level,
        HashSet<Vector2Int> occupied,
        LevelDifficulty target)
    {
        if (level.npcs.Count >= MaxNpcCount)
            return;

        int prefabIndex = LevelGenUtil.GetWeightedRandomPrefabIndex(target);

        if (LevelGenUtil.TryPlaceNPC(occupied, out NPCDef npc, prefabIndex))
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