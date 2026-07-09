using System.Collections.Generic;

// Phan loai do kho dua tren 2 yeu to:
// 1) So buoc ngan nhat can de thang (BFS shortestPath)
// 2) Tong "diem nguy hiem" cua cac NPC trong level, quy doi theo loai:
//    Idle = 1 diem, Fixed Patrol = 2 diem, Random Patrol = 4 diem, Random Way = 6 diem
//    (Random Way nguy hiem nhat vi khong the doan truoc huong di).
//
// Tach rieng thanh 1 class de MAPElitesArchive va bat ky noi nao khac
// (vi du: dat ten level, xuat file) deu dung chung 1 nguon logic duy nhat,
// tranh viec 2 noi tinh do kho khac nhau roi dat sai ten file.
public static class LevelDifficultyClassifier
{
    // KHONG con la "const" nua ma la field tinh (static) co the chinh truc
    // tiep tu MAPElitesWindow (xem 2 o "Easy Max Score" / "Medium Max Score"
    // trong cua so Editor) - vi nguong "dep" phu thuoc vao cach ban cau hinh
    // so luong/loai NPC, khong the doan dung ngay lan dau.
    //
    // Gia tri mac dinh duoc uoc luong lai (26 / 40) dua tren mo phong thuc te
    // phan phoi npcScore voi cau hinh hien tai (6-9 NPC, prefabIndex random
    // deu 0-8): npcScore trung binh ~18, trung vi ~18, p75 ~22, p90 ~26.
    // Nguong 20 truoc do qua thap - gan bang chinh npcScore trung binh, nen
    // hau nhu moi level (chi can shortestPath > 0) deu bi day qua Medium/Hard.
    public static int EasyMaxScore = 26;
    public static int MediumMaxScore = 40;

    // Diem theo tung loai NPC (dua tren prefabIndex, xem SimNPC.cs):
    // 0-3 = Idle, 4-5 = Fixed Patrol, 6-7 = Random Patrol, 8 = Random Way.
    public const int IdlePoints = 1;
    public const int FixedPatrolPoints = 2;
    public const int RandomPatrolPoints = 4;
    public const int RandomWayPoints = 6;

    // So NPC toi da khong bi phat them diem (>= so nay se bat dau bi cong
    // them diem "roi/kho theo doi" du tung NPC rieng le co the rat hien -
    // vi du 9 NPC Idle chi cham 9 diem npcScore, tuong duong voi gan nhu 1
    // level "khong co gi nguy hiem", nhung choi thuc te lai rat roi mat va
    // kho theo doi cung luc nhieu NPC. Truoc day khong co phat nay nen Easy
    // (xep theo diem thap) van co the co toi 9 NPC.
    public const int NpcCountPenaltyThreshold = 5;

    // Diem phat cho MOI NPC vuot qua NpcCountPenaltyThreshold.
    public const int NpcCountPenaltyPerExtra = 2;

    public static int GetNpcPoints(int prefabIndex)
    {
        if (prefabIndex >= 0 && prefabIndex <= 3)
            return IdlePoints;

        if (prefabIndex == 4 || prefabIndex == 5)
            return FixedPatrolPoints;

        if (prefabIndex == 6 || prefabIndex == 7)
            return RandomPatrolPoints;

        if (prefabIndex == 8)
            return RandomWayPoints;

        return 0;
    }

    public static int ComputeNpcScore(List<NPCDef> npcs)
    {
        if (npcs == null)
            return 0;

        int total = 0;

        foreach (var npc in npcs)
        {
            total += GetNpcPoints(npc.prefabIndex);
        }

        // Phat them theo SO LUONG NPC vuot qua nguong, doc lap voi loai NPC -
        // dam bao 1 level co qua nhieu NPC (du toan Idle) khong the tiep tuc
        // duoc xep Easy chi vi tung con diem thap.
        int extraNpcs = npcs.Count - NpcCountPenaltyThreshold;
        if (extraNpcs > 0)
        {
            total += extraNpcs * NpcCountPenaltyPerExtra;
        }

        return total;
    }

    public static int ComputeDifficultyScore(int shortestPath, int npcScore)
    {
        return shortestPath + npcScore;
    }

    public static LevelDifficulty Classify(int shortestPath, int npcScore)
    {
        int score =
            ComputeDifficultyScore(shortestPath, npcScore);

        if (score <= EasyMaxScore)
            return LevelDifficulty.Easy;

        if (score <= MediumMaxScore)
            return LevelDifficulty.Medium;

        return LevelDifficulty.Hard;
    }

    // Bo tien to do kho cu ("Easy_", "Medium_", "Hard_") khoi ten level neu co,
    // dung khi Reclassify de tranh ten bi chong tien to (vi du "Medium_Easy_Generated_3").
    public static string StripDifficultyPrefix(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return levelName;

        string[] parts = levelName.Split(new[] { '_' }, 2);

        if (parts.Length == 2 &&
            System.Enum.TryParse<LevelDifficulty>(parts[0], out _))
        {
            return parts[1];
        }

        return levelName;
    }
}