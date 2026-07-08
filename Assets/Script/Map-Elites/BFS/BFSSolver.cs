using System.Collections.Generic;
using UnityEngine;

public class BFSSolver
{
    private const int Rows = 8;
    private const int Cols = 6;

    private static readonly Vector2Int[] Moves =
    {
        new Vector2Int(-1,0),
        new Vector2Int(1,0),
        new Vector2Int(0,-1),
        new Vector2Int(0,1)
    };

    public DifficultyResultBFS Evaluate(
        LevelData level,
        int maxTurns = 50)
    {
        DifficultyResultBFS result =
            new DifficultyResultBFS();

        DangerMapBuilder danger =
            new DangerMapBuilder(
                level.npcs,
                maxTurns);

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        HashSet<Vector2Int>[] visited =
            new HashSet<Vector2Int>[maxTurns + 1];

        for (int t = 0; t <= maxTurns; t++)
        {
            visited[t] = new HashSet<Vector2Int>();
        }

        queue.Enqueue(level.playerStart);
        visited[0].Add(level.playerStart);

        int shortestPath = -1;
        int shortestPathCount = 0;

        int reachableStates = 0;

        int deadEnds = 0;
        int branchingSum = 0;

        for (int turn = 0; turn <= maxTurns; turn++)
        {
            int queueCountThisTurn = queue.Count;
            int statesThisTurn = 0;

            for (int i = 0; i < queueCountThisTurn; i++)
            {
                Vector2Int current =
                    queue.Dequeue();

                reachableStates++;
                statesThisTurn++;

                bool reachedGoal =
                    IsGoal(
                        current.x,
                        current.y,
                        level);

                if (reachedGoal)
                {
                    if (shortestPath < 0)
                    {
                        shortestPath = turn;
                        shortestPathCount = 1;
                    }
                    else if (turn == shortestPath)
                    {
                        shortestPathCount++;
                    }

                    continue;
                }

                int validMoves = 0;

                foreach (var move in Moves)
                {
                    int nr = current.x + move.x;
                    int nc = current.y + move.y;

                    if (
                        nr < 0 ||
                        nr >= Rows ||
                        nc < 0 ||
                        nc >= Cols)
                    {
                        continue;
                    }

                    bool isGoal =
                        IsGoal(nr, nc, level);

                    if (isGoal)
                    {
                        if (turn + 1 <= maxTurns)
                        {
                            Vector2Int goalPos =
                                new Vector2Int(nr, nc);

                            if (!visited[turn + 1].Contains(goalPos))
                            {
                                visited[turn + 1].Add(goalPos);
                                queue.Enqueue(goalPos);
                                validMoves++;
                            }
                        }

                        continue;
                    }

                    if (
                        danger.IsDanger(
                        turn + 1,
                        nr,
                        nc))
                    {
                        continue;
                    }

                    if (turn + 1 > maxTurns)
                    {
                        continue;
                    }

                    Vector2Int nextPos =
                        new Vector2Int(nr, nc);

                    if (!visited[turn + 1].Contains(nextPos))
                    {
                        visited[turn + 1].Add(nextPos);
                        queue.Enqueue(nextPos);
                        validMoves++;
                    }
                }

                branchingSum += validMoves;

                if (validMoves == 0)
                {
                    deadEnds++;
                }
            }
        }

        result.solvable =
            shortestPath >= 0;

        result.shortestPath =
            shortestPath;

        result.shortestPathCount =
            shortestPathCount;

        result.reachableStates =
            reachableStates;

        result.branchingFactor =
            reachableStates > 0
            ? (float)branchingSum /
              reachableStates
            : 0f;

        result.deadEndRatio =
            reachableStates > 0
            ? (float)deadEnds /
              reachableStates
            : 0f;

        result.npcScore =
            LevelDifficultyClassifier.ComputeNpcScore(
                level.npcs);

        result.fitness =
            CalculateFitness(
                result);

        return result;
    }

    private bool IsGoal(
        int row,
        int col,
        LevelData level)
    {
        if (
            level.destinations ==
            null)
        {
            return false;
        }

        foreach (
            var goal
            in level.destinations)
        {
            if (
                goal.x == row &&
                goal.y == col)
            {
                return true;
            }
        }

        return false;
    }

    private float CalculateFitness(
        DifficultyResultBFS d)
    {
        if (!d.solvable)
            return 0f;

        float score = 0f;

        score +=
            d.shortestPath * 2f;

        score +=
            Mathf.Clamp(
                d.branchingFactor,
                0,
                5);

        score +=
            d.deadEndRatio * 20f;

        score -=
            d.shortestPathCount * 0.5f;

        return score;
    }
}