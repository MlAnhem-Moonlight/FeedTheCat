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

            // Chi gan difficulty khi level nay THUC SU duoc nhan vao o -
            // truoc day dong nay nam ngoai if(isBetter) nen 1 candidate bi
            // TU CHOI (khong du tot) van co the ghi de cell.difficulty, lam
            // no tam thoi lech voi cell.level (level thuc su dang nam trong o).
            cell.difficulty = difficulty;

            // O nay da co elite cu bi thay the -> huy de khong ro ri bo nho
            // (ScriptableObject.CreateInstance khong tu GC nhu object thuong).
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }
        }

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

    // Phan loai lai do kho cua TAT CA level dang co trong archive dua tren
    // TAM PHAN VI (percentile) cua diem do kho (shortestPath + npcScore)
    // trong chinh tap level vua sinh ra - thay vi nguong co dinh doan truoc
    // (LevelDifficultyClassifier.EasyMaxScore/MediumMaxScore). Cach nay dam
    // bao luon co khoang 1/3 Easy, 1/3 Medium, 1/3 Hard bat ke phan bo NPC
    // ngau nhien ra sao, tranh tinh trang ty le Easy qua thap/qua cao khi
    // du lieu thuc te lech xa so voi uoc luong ban dau.
    //
    // Nen goi 1 lan sau khi MAPElitesGenerator.Run() chay xong.
    public void RecalculateDifficulties()
    {
        List<EliteCellBFS> occupiedCells = new List<EliteCellBFS>();
        List<int> scores = new List<int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                EliteCellBFS cell = archive[x, y];

                if (!cell.occupied)
                    continue;

                occupiedCells.Add(cell);

                scores.Add(
                    LevelDifficultyClassifier.ComputeDifficultyScore(
                        cell.result.shortestPath,
                        cell.result.npcScore));
            }
        }

        if (occupiedCells.Count == 0)
            return;

        List<int> sortedScores = new List<int>(scores);
        sortedScores.Sort();

        int easyCutoff = Percentile(sortedScores, 1f / 3f);
        int mediumCutoff = Percentile(sortedScores, 2f / 3f);

        for (int i = 0; i < occupiedCells.Count; i++)
        {
            EliteCellBFS cell = occupiedCells[i];
            int score = scores[i];

            LevelDifficulty difficulty =
                score <= easyCutoff
                    ? LevelDifficulty.Easy
                    : score <= mediumCutoff
                        ? LevelDifficulty.Medium
                        : LevelDifficulty.Hard;

            cell.difficulty = difficulty;

            // Doi lai tien to ten level cho khop voi do kho vua phan loai lai
            // (bo tien to cu neu co, roi gan tien to moi).
            string baseName =
                LevelDifficultyClassifier.StripDifficultyPrefix(
                    cell.level.levelName);

            cell.level.levelName = $"{difficulty}_{baseName}";
        }
    }

    private int Percentile(List<int> sortedScores, float percentile)
    {
        if (sortedScores.Count == 1)
            return sortedScores[0];

        int index =
            Mathf.Clamp(
                Mathf.FloorToInt(percentile * (sortedScores.Count - 1)),
                0,
                sortedScores.Count - 1);

        return sortedScores[index];
    }
}