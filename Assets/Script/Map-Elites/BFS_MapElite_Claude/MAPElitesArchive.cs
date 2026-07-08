using System.Collections.Generic;
using UnityEngine;

public class MAPElitesArchive
{
    private EliteCellBFS[,] archive;

    public int width;
    public int height;

    public MAPElitesArchive(
        int width,
        int height)
    {
        this.width = width;
        this.height = height;

        archive =
            new EliteCellBFS[
                width,
                height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                archive[x, y] =
                    new EliteCellBFS();
            }
        }
    }

    // Tra ve true neu level nay duoc chap nhan vao archive (moi hoac tot hon
    // elite hien co trong cung 1 o). Generator dung gia tri nay de biet
    // level bi tu choi thi can DestroyImmediate de tranh ro ri bo nho.
    public bool TryInsert(
        LevelData level,
        DifficultyResultBFS result)
    {
        int x =
            Mathf.Clamp(
                result.shortestPath / 2,
                0,
                width - 1);

        int y =
            Mathf.Clamp(
                result.reachableStates / 10,
                0,
                height - 1);

        EliteCellBFS cell =
            archive[x, y];

        // Do kho duoc tinh tu ca so buoc ngan nhat de thang (shortestPath)
        // VA tong diem nguy hiem cua cac NPC (npcScore, da tinh san trong
        // BFSSolver.Evaluate) - dung 1 nguon logic duy nhat (LevelDifficultyClassifier)
        // thay vi suy ra tu vi tri o (x, y) trong luoi archive nhu truoc.
        LevelDifficulty difficulty =
            LevelDifficultyClassifier.Classify(
                result.shortestPath,
                result.npcScore);

        // Dat ten ngay khi biet do kho, dung format {DoKho}_{TenLevel}
        // de khop voi cach ItemCollector.cs doc do kho tu levelName.
        level.levelName =
            $"{difficulty}_{level.levelName}";

        bool isBetter =
            !cell.occupied ||
            result.fitness >
            cell.fitness;

        if (isBetter)
        {
            LevelData previous = cell.level;

            cell.occupied = true;
            cell.level = level;
            cell.result = result;
            cell.fitness =
                result.fitness;

            // O nay da co elite cu bi thay the -> huy de khong ro ri bo nho
            // (ScriptableObject.CreateInstance khong tu GC nhu object thuong).
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }
        }

        cell.difficulty = difficulty;

        return isBetter;
    }

    public EliteCellBFS Get(
        int x,
        int y)
    {
        return archive[x, y];
    }

    // Lay ngau nhien 1 level elite dang co trong archive (dung lam "parent"
    // de LevelMutator dot bien). Tra ve null neu archive con trong hoan toan.
    public LevelData GetRandomElite()
    {
        List<LevelData> occupiedLevels = new List<LevelData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (archive[x, y].occupied)
                {
                    occupiedLevels.Add(archive[x, y].level);
                }
            }
        }

        if (occupiedLevels.Count == 0)
            return null;

        return occupiedLevels[
            Random.Range(0, occupiedLevels.Count)];
    }
}