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

    public LevelDifficulty difficulty;

    public override string ToString()
    {
        return
            $"Solvable={solvable} | " +
            $"Shortest={shortestPath} | " +
            $"Reachable={reachableStates} | " +
            $"PathCount={shortestPathCount} | " +
            $"Branching={branchingFactor:F2} | " +
            $"DeadEnd={deadEndRatio:F2} | " +
            $"Difficulty={difficulty}";
    }
}