using System.Collections.Generic;
using UnityEngine;

public static class LevelDifficultySolver
{
    private static readonly Vector2Int[] dirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static DifficultyResult Evaluate(LevelData level)
    {
        DifficultyResult result =
            new DifficultyResult();

        const int ROWS = 8;
        const int COLS = 6;

        bool[,] blocked =
            new bool[ROWS, COLS];

        foreach (var npc in level.npcs)
        {
            blocked[npc.row, npc.column] = true;

            if (npc.prefabIndex <= 3)
            {
                int r2 = npc.row;
                int c2 = npc.column;

                switch (npc.prefabIndex)
                {
                    case 0:
                    case 1:
                        c2++;
                        break;

                    case 2:
                    case 3:
                        r2++;
                        break;
                }

                if (r2 >= 0 &&
                    r2 < ROWS &&
                    c2 >= 0 &&
                    c2 < COLS)
                {
                    blocked[r2, c2] = true;
                }
            }
        }

        Vector2Int goal =
            level.destinations[0];

        Queue<Vector2Int> q =
            new Queue<Vector2Int>();

        bool[,] visited =
            new bool[ROWS, COLS];

        int[,] dist =
            new int[ROWS, COLS];

        q.Enqueue(level.playerStart);

        visited[
            level.playerStart.x,
            level.playerStart.y] = true;

        int reachable = 0;

        while (q.Count > 0)
        {
            Vector2Int cur =
                q.Dequeue();

            reachable++;

            foreach (var d in dirs)
            {
                Vector2Int next =
                    cur + d;

                if (next.x < 0 ||
                    next.x >= ROWS ||
                    next.y < 0 ||
                    next.y >= COLS)
                    continue;

                if (visited[next.x, next.y])
                    continue;

                if (blocked[next.x, next.y])
                    continue;

                visited[next.x, next.y] = true;

                dist[next.x, next.y] =
                    dist[cur.x, cur.y] + 1;

                q.Enqueue(next);
            }
        }

        if (!visited[goal.x, goal.y])
        {
            result.solvable = false;
            return result;
        }

        int shortest =
            dist[goal.x, goal.y];

        float branching =
            reachable /
            Mathf.Max(1f, shortest);

        result.solvable = true;
        result.shortestPath = shortest;
        result.reachableStates = reachable;
        result.branchingFactor = branching;

        result.fitness =
            shortest +
            branching * 5f +
            level.npcs.Count;

        return result;
    }
}