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

        // BFS goc o tren CHI quan tam toi dich, hoan toan bo qua item -> 1
        // level van bi coi la solvable ke ca khi NPC chan kin duong toi item
        // (reward "chet", khong bao gio nhat duoc). EvaluateItemFeasibility
        // chay 1 BFS rieng tren khong gian trang thai (vi tri, luot, da-nhat-
        // item-hay-chua) de biet CHINH XAC co duong nao vua nhat item vua
        // toi duoc dich hay khong.
        EvaluateItemFeasibility(
            level,
            danger,
            maxTurns,
            out bool itemReachable,
            out int fullClearShortestPath);

        result.itemReachable = itemReachable;
        result.fullClearShortestPath = fullClearShortestPath;
        result.fullClearSolvable = fullClearShortestPath >= 0;

        result.fitness =
            CalculateFitness(
                result);

        return result;
    }

    // Kiem tra kha thi cua viec "vua an reward vua thang": BFS tren khong
    // gian trang thai (vi tri, luot, hasItem) thay vi chi (vi tri, luot) nhu
    // BFS chinh. hasItem chi chuyen 0 -> 1 khi di qua o item, va giu nguyen
    // 1 sau do (item nhat 1 lan la con mai, giong hanh vi thuc te trong game).
    // Chi xet item dau tien trong danh sach (hien tai generator luon chi tao
    // dung 1 item cho moi level).
    private void EvaluateItemFeasibility(
        LevelData level,
        DangerMapBuilder danger,
        int maxTurns,
        out bool itemReachable,
        out int fullClearShortestPath)
    {
        itemReachable = false;
        fullClearShortestPath = -1;

        if (level.items == null || level.items.Count == 0)
        {
            // Khong co item nao trong level -> yeu cau nay khong ap dung.
            return;
        }

        Vector2Int itemPos =
            new Vector2Int(level.items[0].row, level.items[0].column);

        // visited[turn, 0/1]: da tung o vi tri nay tai luot 'turn' voi
        // hasItem = 0 (chua nhat) hay 1 (da nhat) hay chua.
        HashSet<Vector2Int>[,] visited =
            new HashSet<Vector2Int>[maxTurns + 1, 2];

        for (int t = 0; t <= maxTurns; t++)
        {
            visited[t, 0] = new HashSet<Vector2Int>();
            visited[t, 1] = new HashSet<Vector2Int>();
        }

        int startHasItem =
            (level.playerStart == itemPos) ? 1 : 0;

        if (startHasItem == 1)
            itemReachable = true;

        Queue<(Vector2Int pos, int turn, int hasItem)> queue =
            new Queue<(Vector2Int, int, int)>();

        queue.Enqueue((level.playerStart, 0, startHasItem));
        visited[0, startHasItem].Add(level.playerStart);

        while (queue.Count > 0)
        {
            var (pos, turn, hasItem) = queue.Dequeue();

            bool atGoal = IsGoal(pos.x, pos.y, level);

            if (atGoal)
            {
                // Toi dich la trang thai "hap thu" (giong het hanh vi win
                // thuc te trong game - khong di tiep sau khi thang).
                if (hasItem == 1 && fullClearShortestPath < 0)
                {
                    // BFS duyet theo thu tu luot tang dan nen lan dau tien
                    // gap (hasItem=1, atGoal) chinh la duong ngan nhat.
                    fullClearShortestPath = turn;
                }

                continue;
            }

            if (turn >= maxTurns)
                continue;

            foreach (var move in Moves)
            {
                int nr = pos.x + move.x;
                int nc = pos.y + move.y;

                if (nr < 0 || nr >= Rows || nc < 0 || nc >= Cols)
                    continue;

                Vector2Int nextPos = new Vector2Int(nr, nc);
                bool nextIsGoal = IsGoal(nr, nc, level);

                // Giong BFS chinh: o dich luon an toan (khong bi tinh la
                // "nguy hiem" du danger map co danh dau hay khong).
                if (!nextIsGoal && danger.IsDanger(turn + 1, nr, nc))
                    continue;

                int nextHasItem = hasItem;
                if (nextPos == itemPos)
                    nextHasItem = 1;

                if (nextHasItem == 1)
                    itemReachable = true;

                if (!visited[turn + 1, nextHasItem].Contains(nextPos))
                {
                    visited[turn + 1, nextHasItem].Add(nextPos);
                    queue.Enqueue((nextPos, turn + 1, nextHasItem));
                }
            }
        }
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