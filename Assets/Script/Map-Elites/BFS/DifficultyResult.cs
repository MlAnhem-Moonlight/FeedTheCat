using System;

[Serializable]
public struct DifficultyResultBFS
{
    public bool solvable;

    public int shortestPath;

    public int reachableStates;

    public int shortestPathCount;

    public float branchingFactor;

    public float deadEndRatio;

    public float fitness;

    public int npcScore;

    public LevelDifficulty difficulty;

    // Item (reward) co the toi duoc hay khong, bat ke con thang duoc hay
    // khong - dung de phat hien truong hop NPC chan kin item (item bi "chet",
    // khong bao gio nhat duoc du level van solvable toi dich theo duong khac).
    public bool itemReachable;

    // So luot ngan nhat de VUA nhat duoc item VUA toi duoc dich (-1 neu
    // khong ton tai duong nao lam duoc ca 2 viec). Day la "do kho thuc su"
    // ma nguoi choi gap phai neu ho muon an ca reward lan thang, khac voi
    // shortestPath (chi tinh rieng duong toi dich, bo qua item hoan toan).
    public int fullClearShortestPath;

    // true neu fullClearShortestPath >= 0, tuc la level nay chac chan choi
    // duoc theo kieu "nhat reward + thang" - dung lam dieu kien loc level
    // hop le trong MAPElitesGenerator (thay vi chi dua vao 'solvable').
    public bool fullClearSolvable;

    public override string ToString()
    {
        return
            $"Solvable={solvable} | " +
            $"Shortest={shortestPath} | " +
            $"Reachable={reachableStates} | " +
            $"PathCount={shortestPathCount} | " +
            $"Branching={branchingFactor:F2} | " +
            $"DeadEnd={deadEndRatio:F2} | " +
            $"NpcScore={npcScore} | " +
            $"ItemReachable={itemReachable} | " +
            $"FullClearPath={fullClearShortestPath} | " +
            $"FullClearSolvable={fullClearSolvable} | " +
            $"Difficulty={difficulty}";
    }
}