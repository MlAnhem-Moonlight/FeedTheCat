using System.Collections.Generic;
using UnityEngine;

// Cac ham/hang so dung chung giua MAPElitesGenerator (sinh level ngau nhien
// tu dau) va LevelMutator (dot bien 1 level co san), de dam bao ca 2 noi
// dung CHUNG 1 logic ve kich thuoc luoi, huong NPC Idle, va cach kiem tra
// / chiem o - tranh tinh trang 2 noi lech nhau roi sinh level bi chong o.
public static class LevelGenUtil
{
    // Phai khop voi kich thuoc dung trong BFSSolver / EnemySimulator / SimBoard / DangerMapBuilder.
    public const int Rows = 8; // so hang
    public const int Cols = 6; // so cot

    public static bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < Rows &&
               p.y >= 0 && p.y < Cols;
    }

    public static Vector2Int GetRandomCell()
    {
        return new Vector2Int(
            Random.Range(0, Rows),
            Random.Range(0, Cols));
    }

    // Huong ban dau cho NPC loai Idle (prefabIndex 0-3).
    // Phai khop voi SimNPC.InitializeDirection() de tinh dung o thu 2.
    public static Vector2Int GetIdleDirection(int prefabIndex)
    {
        switch (prefabIndex)
        {
            case 0: return new Vector2Int(-1, 0); // Idle Up
            case 1: return new Vector2Int(1, 0);  // Idle Down
            case 2: return new Vector2Int(0, -1); // Idle Left
            case 3: return new Vector2Int(0, 1);  // Idle Right
            default: return Vector2Int.zero;
        }
    }

    // Idle (0-3) chiem 2 o theo huong ban dau, giong SimNPC.OccupiesTwoCells().
    public static bool OccupiesTwoCells(int prefabIndex)
    {
        return prefabIndex <= 3;
    }

    // Gom toan bo o dang bi chiem trong 1 level: player, (cac) destination,
    // (cac) item, va tat ca NPC (ke ca o thu 2 cua NPC Idle).
    public static HashSet<Vector2Int> BuildOccupiedCells(LevelData level)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        occupied.Add(level.playerStart);

        if (level.destinations != null)
        {
            foreach (var d in level.destinations)
                occupied.Add(d);
        }

        if (level.items != null)
        {
            foreach (var item in level.items)
                occupied.Add(new Vector2Int(item.row, item.column));
        }

        if (level.npcs != null)
        {
            foreach (var npc in level.npcs)
            {
                Vector2Int pos = new Vector2Int(npc.row, npc.column);
                occupied.Add(pos);

                if (OccupiesTwoCells(npc.prefabIndex))
                {
                    Vector2Int second = pos + GetIdleDirection(npc.prefabIndex);
                    if (InBounds(second))
                        occupied.Add(second);
                }
            }
        }

        return occupied;
    }

    // Khoang cach Manhattan toi thieu giua player va dich, tranh truong hop
    // player dung ngay canh dich (dac biet hay xay ra o level Easy vi
    // shortestPath ngan -> BFS xep vao Easy dung, nhung choi rat nham chan
    // vi khong can di chuyen gi ca). Chinh so nay neu thay van con qua gan
    // hoac qua kho tim duoc vi tri hop le.
    public const int MinPlayerGoalDistance = 4;

    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Lay 1 o trong (khong nam trong occupied) va cach reference it nhat
    // minDistance (Manhattan). Neu sau nhieu lan thu khong tim duoc o nao
    // du xa (ban do qua nho / qua nhieu o da bi chiem), tra ve o xa nhat
    // tung tim duoc thay vi that bai hoan toan.
    public static Vector2Int GetUniqueRandomCellFarFrom(
        HashSet<Vector2Int> occupied,
        Vector2Int reference,
        int minDistance)
    {
        const int maxAttempts = 100;

        Vector2Int bestCell = default;
        int bestDistance = -1;
        bool foundAny = false;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2Int cell = GetRandomCell();

            if (occupied.Contains(cell))
                continue;

            int distance = ManhattanDistance(cell, reference);

            if (distance >= minDistance)
                return cell;

            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestCell = cell;
                foundAny = true;
            }
        }

        if (foundAny)
            return bestCell;

        // Truong hop cuc hiem: khong tim duoc o trong nao ca trong maxAttempts
        // lan thu -> roi ve cach lay o trong bat ky (khong rang buoc khoang cach).
        return GetUniqueRandomCell(occupied);
    }

    public static Vector2Int GetUniqueRandomCell(HashSet<Vector2Int> occupied)
    {
        const int maxAttempts = 50;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2Int cell = GetRandomCell();
            if (!occupied.Contains(cell))
                return cell;
        }

        // Truong hop hiem gap tren luoi 8x6 (48 o) day kin - tra ve 1 o bat ky,
        // BFSSolver se tu loai level nay neu no khong con giai duoc.
        return GetRandomCell();
    }

    // Thu tao/dat 1 NPC khong chong len occupied.
    // - forcedPrefabIndex: ep loai NPC cu the (dung khi doi loai 1 NPC co san). Null = random loai (0-8).
    // - forcedPos: ep vi tri cu the (dung khi doi loai NPC nhung giu nguyen cho). Null = random vi tri.
    public static bool TryPlaceNPC(
        HashSet<Vector2Int> occupied,
        out NPCDef npc,
        int? forcedPrefabIndex = null,
        Vector2Int? forcedPos = null)
    {
        npc = new NPCDef();

        npc.prefabIndex = forcedPrefabIndex ?? Random.Range(0, 9);
        npc.seed = Random.Range(int.MinValue, int.MaxValue);
        npc.fixedSteps = 3;
        npc.minRandomSteps = 1;
        npc.maxRandomSteps = 4;

        bool occupiesTwoCells = OccupiesTwoCells(npc.prefabIndex);
        Vector2Int dir = GetIdleDirection(npc.prefabIndex);

        if (forcedPos.HasValue)
        {
            Vector2Int pos = forcedPos.Value;
            Vector2Int second = pos + dir;

            bool firstFree = !occupied.Contains(pos);
            bool secondFree = !occupiesTwoCells ||
                (InBounds(second) && !occupied.Contains(second));

            if (!firstFree || !secondFree)
                return false;

            npc.row = pos.x;
            npc.column = pos.y;

            occupied.Add(pos);
            if (occupiesTwoCells)
                occupied.Add(second);

            return true;
        }

        const int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int pos = GetRandomCell();
            Vector2Int second = pos + dir;

            bool firstFree = !occupied.Contains(pos);
            bool secondFree = !occupiesTwoCells ||
                (InBounds(second) && !occupied.Contains(second));

            if (firstFree && secondFree)
            {
                npc.row = pos.x;
                npc.column = pos.y;

                occupied.Add(pos);
                if (occupiesTwoCells)
                    occupied.Add(second);

                return true;
            }
        }

        return false;
    }
}