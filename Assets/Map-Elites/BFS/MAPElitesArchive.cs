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

    public void TryInsert(
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

        if (
            !cell.occupied ||
            result.fitness >
            cell.fitness)
        {
            cell.occupied = true;
            cell.level = level;
            cell.result = result;
            cell.fitness =
                result.fitness;
        }
        cell.difficulty = GetDifficulty(x, y);
    }

    private LevelDifficulty GetDifficulty(
    int x,
    int y)
    {
        float nx =
            (float)x / (width - 1);

        float ny =
            (float)y / (height - 1);

        float score =
            (nx + ny) * 0.5f;

        if (score < 0.33f)
            return LevelDifficulty.Easy;

        if (score < 0.66f)
            return LevelDifficulty.Medium;

        return LevelDifficulty.Hard;
    }

    public EliteCellBFS Get(
        int x,
        int y)
    {
        return archive[x, y];
    }
}