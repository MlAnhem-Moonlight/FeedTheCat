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
    // Chinh 2 nguong nay de dieu chinh do kho theo y muon. Diem tong hop =
    // shortestPath + npcScore, ca 2 cung don vi "diem" nen cong truc tiep.
    // Vi du: level co shortestPath=10 va 7 NPC (4 idle + 2 fixed + 1 random patrol)
    //        -> npcScore = 4*1 + 2*2 + 1*4 = 12 -> diem tong = 22.
    public const int EasyMaxScore = 20;
    public const int MediumMaxScore = 35;

    // Diem theo tung loai NPC (dua tren prefabIndex, xem SimNPC.cs):
    // 0-3 = Idle, 4-5 = Fixed Patrol, 6-7 = Random Patrol, 8 = Random Way.
    public const int IdlePoints = 1;
    public const int FixedPatrolPoints = 2;
    public const int RandomPatrolPoints = 4;
    public const int RandomWayPoints = 6;

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
}