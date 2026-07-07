using UnityEngine;

[System.Serializable]
public class EliteCell
{
    public LevelData level;
    public float fitness;
}

public class MAPEliteArchive
{
    public readonly int width;
    public readonly int height;

    private EliteCell[,] cells;

    public MAPEliteArchive(int w, int h)
    {
        width = w;
        height = h;

        cells = new EliteCell[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                cells[x, y] = new EliteCell();
                cells[x, y].fitness = float.MinValue;
            }
        }
    }

    public void TryInsert(
        int x,
        int y,
        float fitness,
        LevelData level)
    {
        if (fitness <= cells[x, y].fitness)
            return;

        cells[x, y].fitness = fitness;
        cells[x, y].level = level;
    }

    public EliteCell GetCell(int x, int y)
    {
        return cells[x, y];
    }
}
public struct DifficultyResult
{
    public bool solvable;

    public int shortestPath;

    public int reachableStates;

    public float branchingFactor;

    public float fitness;
}
