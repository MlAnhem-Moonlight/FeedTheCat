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

    // Kiem tra CHAC CHAN 1 level khong co o nao bi 2 thanh phan cung chiem
    // (player, dich, item, NPC - ke ca o thu 2 cua NPC Idle). Dung nhu 1 luoi
    // an toan cuoi cung truoc khi chap nhan 1 level, phong truong hop logic
    // dat o o cac nhanh khac (vi du dot bien) co truong hop chua luong het.
    public static bool HasOverlap(LevelData level)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        cells.Add(level.playerStart);

        if (level.destinations != null)
        {
            cells.AddRange(level.destinations);
        }

        if (level.items != null)
        {
            foreach (var item in level.items)
                cells.Add(new Vector2Int(item.row, item.column));
        }

        if (level.npcs != null)
        {
            foreach (var npc in level.npcs)
            {
                Vector2Int pos = new Vector2Int(npc.row, npc.column);
                cells.Add(pos);

                if (OccupiesTwoCells(npc.prefabIndex))
                {
                    Vector2Int second = pos + GetIdleDirection(npc.prefabIndex);

                    if (InBounds(second))
                        cells.Add(second);
                }
            }
        }

        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();

        foreach (var cell in cells)
        {
            // HashSet.Add() tra ve false neu o nay DA co trong seen truoc do
            // -> chinh la dau hieu bi chong o.
            if (!seen.Add(cell))
                return true;
        }

        return false;
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

    // Khoang cach Manhattan toi thieu giua item (reward) va dich. Truoc day
    // item duoc dat hoan toan ngau nhien (chi tranh chong o), nen co truong
    // hop item nam NGAY SAT dich - nguoi choi buoc 1-2 buoc la nhat duoc
    // item roi thang luon, lam reward mat y nghia "phai di vong qua lay".
    public const int MinItemGoalDistance = 3;

    // Tran so NPC toi da theo TUNG do kho, dung chung boi:
    // - MAPElitesGenerator (luc sinh moi, gioi han GetNpcCountForDifficulty)
    // - LevelMutator (luc dot bien, AddNPC khong duoc vuot tran nay)
    // - LevelDifficultyClassifier (luc phan loai, ep 1 level qua nhieu NPC
    //   phai bi day xuong do kho cao hon, KE CA KHI diem so/quang duong cua
    //   no thap - tranh tinh trang 1 level 9 NPC van bi (hoac duoc) xep Easy
    //   chi vi shortestPath ngan).
    //
    // Truoc day KHONG co tran cung nay - CalculateFitness (BFSSolver) thuong
    // branchingFactor/deadEndRatio, ca 2 deu tang theo so NPC, nen trong MAP-
    // Elites, o nao cung "thang" ve phia candidate NHIEU NPC NHAT co the, bat
    // ke o do sau nay duoc gan nhan Easy hay Hard (nhan chi dua tren percentile
    // tuong doi, khong co tran tuyet doi) -> hau qua la moi do kho deu hoi tu
    // ve ~9 NPC nhu nhau.
    public const int EasyMaxNpcCount = 3;
    public const int MediumMaxNpcCount = 6;
    public const int MaxNpcCount = 9; // tran chung cho Hard va cho dot bien

    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // 1 rang buoc khoang cach toi thieu Manhattan toi 1 diem tham chieu, dung
    // cho GetUniqueRandomCellFarFromAll khi can thoa MAN NHIEU rang buoc cung
    // luc (vi du: dich phai vua cach xa player VUA cach xa item).
    public struct DistanceConstraint
    {
        public Vector2Int reference;
        public int minDistance;

        public DistanceConstraint(Vector2Int reference, int minDistance)
        {
            this.reference = reference;
            this.minDistance = minDistance;
        }
    }

    // Lay 1 o trong (khong nam trong occupied) thoa man CUNG LUC nhieu rang
    // buoc khoang cach toi thieu (vi du: dich can cach player >= 4 O VA cach
    // item >= 3). Neu sau nhieu lan thu khong tim duoc o nao thoa man HET tat
    // ca rang buoc, tra ve o co "slack" (khoang du) nho nhat trong so cac rang
    // buoc lon nhat - tuc la o gan thoa man nhat - thay vi that bai hoan toan.
    public static Vector2Int GetUniqueRandomCellFarFromAll(
        HashSet<Vector2Int> occupied,
        List<DistanceConstraint> constraints)
    {
        if (constraints == null || constraints.Count == 0)
            return GetUniqueRandomCell(occupied);

        const int maxAttempts = 200;

        Vector2Int bestCell = default;
        int bestSlack = int.MinValue;
        bool foundAny = false;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2Int cell = GetRandomCell();

            if (occupied.Contains(cell))
                continue;

            bool satisfiesAll = true;
            int minSlack = int.MaxValue;

            foreach (var c in constraints)
            {
                int distance = ManhattanDistance(cell, c.reference);
                int slack = distance - c.minDistance;

                if (slack < minSlack)
                    minSlack = slack;

                if (distance < c.minDistance)
                    satisfiesAll = false;
            }

            if (satisfiesAll)
                return cell;

            if (minSlack > bestSlack)
            {
                bestSlack = minSlack;
                bestCell = cell;
                foundAny = true;
            }
        }

        if (foundAny)
            return bestCell;

        // Cuc hiem: khong tim duoc o trong nao ca trong maxAttempts lan thu.
        return GetUniqueRandomCell(occupied);
    }

    // Tien ich goi lai GetUniqueRandomCellFarFromAll cho truong hop pho bien
    // nhat: CUNG LUC thoa man 2 rang buoc khoang cach (vi du: dich phai vua
    // cach xa player VUA cach xa item). Giu 1 nguon logic duy nhat (khong lap
    // lai vong lap tim o) thay vi viet rieng 1 ham tim-kiem khac.
    public static Vector2Int GetUniqueRandomCellFarFromBoth(
        HashSet<Vector2Int> occupied,
        Vector2Int reference1,
        int minDistance1,
        Vector2Int reference2,
        int minDistance2)
    {
        return GetUniqueRandomCellFarFromAll(
            occupied,
            new List<DistanceConstraint>
            {
                new DistanceConstraint(reference1, minDistance1),
                new DistanceConstraint(reference2, minDistance2)
            });
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

    // Trong so chon loai NPC (prefabIndex 0-8) theo tung do kho muc tieu, dung
    // chung boi MAPElitesGenerator (sinh moi) va LevelMutator (dot bien), de
    // chu dong tao ra nhieu level Easy/Hard hon thay vi random deu 0-8 roi
    // hy vong diem so roi dung khoang mong muon.
    // Thu tu trong so: [0]=IdleUp [1]=IdleDown [2]=IdleLeft [3]=IdleRight
    //                  [4]=FixedH [5]=FixedV [6]=RandomStepH [7]=RandomStepV [8]=RandomWay
    private static readonly int[] EasyNpcWeights =
        { 15, 15, 15, 15, 8, 8, 3, 3, 2 };   // uu tien Idle (an toan nhat)

    private static readonly int[] HardNpcWeights =
        { 3, 3, 3, 3, 8, 8, 20, 20, 32 };    // uu tien Random Patrol / Random Way (nguy hiem nhat)

    // Medium khong can bang trong so rieng - dung Random.Range(0,9) deu nhu cu.

    public static int GetWeightedRandomPrefabIndex(LevelDifficulty target)
    {
        switch (target)
        {
            case LevelDifficulty.Easy:
                return PickWeightedIndex(EasyNpcWeights);

            case LevelDifficulty.Hard:
                return PickWeightedIndex(HardNpcWeights);

            default:
                return Random.Range(0, 9);
        }
    }

    private static int PickWeightedIndex(int[] weights)
    {
        int total = 0;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        int roll = Random.Range(0, total);
        int cumulative = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return i;
        }

        // Khong nen toi day, chi la fallback an toan.
        return weights.Length - 1;
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