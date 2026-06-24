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
        int maxTurns = 100)
    {
        DifficultyResultBFS result =
            new DifficultyResultBFS();

        DangerMapBuilder danger =
            new DangerMapBuilder(
                level.npcs,
                maxTurns);

        Queue<BFSState> queue =
            new Queue<BFSState>();

        HashSet<BFSState> visited =
            new HashSet<BFSState>();

        Dictionary<int, int> depthCount =
            new Dictionary<int, int>();

        BFSState start =
            new BFSState(
                level.playerStart.x,
                level.playerStart.y,
                0);

        queue.Enqueue(start);
        visited.Add(start);

        int shortestPath = -1;
        int shortestPathCount = 0;

        int reachableStates = 0;

        int deadEnds = 0;
        int branchingSum = 0;

        while (queue.Count > 0)
        {
            BFSState current =
                queue.Dequeue();

            reachableStates++;

            bool reachedGoal =
                IsGoal(
                    current.row,
                    current.col,
                    level);

            if (reachedGoal)
            {
                if (shortestPath < 0)
                {
                    shortestPath =
                        current.turn;

                    shortestPathCount = 1;
                }
                else if (
                    current.turn ==
                    shortestPath)
                {
                    shortestPathCount++;
                }

                continue;
            }

            int validMoves = 0;

            foreach (var move in Moves)
            {
                int nr =
                    current.row +
                    move.x;

                int nc =
                    current.col +
                    move.y;

                int nextTurn =
                    current.turn + 1;

                if (
                    nr < 0 ||
                    nr >= Rows ||
                    nc < 0 ||
                    nc >= Cols)
                {
                    continue;
                }

                bool isGoal =
                    IsGoal(
                        nr,
                        nc,
                        level);

                /*
                 * Goal thắng ngay.
                 * Không cần check NPC.
                 */
                if (isGoal)
                {
                    BFSState goalState =
                        new BFSState(
                            nr,
                            nc,
                            nextTurn);

                    if (
                        !visited.Contains(
                            goalState))
                    {
                        visited.Add(
                            goalState);

                        queue.Enqueue(
                            goalState);

                        validMoves++;
                    }

                    continue;
                }

                /*
                 * NPC đã di chuyển xong
                 * ở nextTurn
                 */
                if (
                    danger.IsDanger(
                        nextTurn,
                        nr,
                        nc))
                {
                    continue;
                }

                BFSState next =
                    new BFSState(
                        nr,
                        nc,
                        nextTurn);

                if (
                    visited.Contains(
                        next))
                {
                    continue;
                }

                visited.Add(next);

                queue.Enqueue(next);

                validMoves++;
            }

            branchingSum += validMoves;

            if (validMoves == 0)
            {
                deadEnds++;
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