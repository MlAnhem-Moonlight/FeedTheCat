using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySimulator
{
    private const int ROWS = 8;
    private const int COLS = 6;

    private readonly List<SimNPC> npcs;
    private readonly List<SimNPC> workingState;
    private SimBoard board;

    public EnemySimulator(List<NPCDef> sourceNPCs)
    {
        npcs = new List<SimNPC>();
        workingState = new List<SimNPC>();
        board = new SimBoard();

        foreach (var npc in sourceNPCs)
        {
            npcs.Add(new SimNPC(npc));
        }
    }

    public List<Vector2Int> GetOccupiedCells(int turn)
    {
        ResetWorkingState();

        for (int t = 0; t < turn; t++)
        {
            StepAll(workingState);
        }

        List<Vector2Int> result =
            new List<Vector2Int>();

        foreach (var npc in workingState)
        {
            AddOccupiedCells(result, npc);
        }

        return result;
    }

    private void ResetWorkingState()
    {
        workingState.Clear();

        foreach (var npc in npcs)
        {
            workingState.Add(new SimNPC(npc.source));
        }
    }

    public bool[,] BuildDangerGrid(int turn)
    {
        bool[,] danger =
            new bool[ROWS, COLS];

        var occupied =
            GetOccupiedCells(turn);

        foreach (var p in occupied)
        {
            if (Inside(p.x, p.y))
                danger[p.x, p.y] = true;
        }

        return danger;
    }

    private void StepAll(
        List<SimNPC> state)
    {
        RebuildOccupancy(state);

        foreach (var npc in state)
        {
            UnmarkOccupied(npc);

            if (npc.IsIdle())
            {
                MarkOccupied(npc);
                continue;
            }

            if (npc.IsFixed())
                StepFixed(npc);

            else if (npc.IsRandomStep())
                StepRandomStep(npc);

            else if (npc.IsRandomWay())
                StepRandomWay(npc);

            MarkOccupied(npc);
        }
    }

    private void StepFixed(SimNPC npc)
    {
        if (npc.stepsRemaining <= 0)
        {
            Reverse(npc);
            npc.stepsRemaining =
                npc.source.fixedSteps;
        }

        int nextR = npc.row + npc.dirRow;
        int nextC = npc.col + npc.dirCol;

        if (!CanMoveTo(npc, nextR, nextC))
        {
            Reverse(npc);

            npc.stepsRemaining =
                npc.source.fixedSteps;

            return;
        }

        npc.row = nextR;
        npc.col = nextC;

        npc.stepsRemaining--;
    }

    private void StepRandomStep(SimNPC npc)
    {
        if (npc.stepsRemaining <= 0)
        {
            Reverse(npc);

            npc.stepsRemaining =
                npc.rng.Next(
                    npc.source.minRandomSteps,
                    npc.source.maxRandomSteps + 1);
        }

        int nextR = npc.row + npc.dirRow;
        int nextC = npc.col + npc.dirCol;

        if (!CanMoveTo(npc, nextR, nextC))
        {
            Reverse(npc);

            npc.stepsRemaining =
                npc.rng.Next(
                    npc.source.minRandomSteps,
                    npc.source.maxRandomSteps + 1);

            return;
        }

        npc.row = nextR;
        npc.col = nextC;

        npc.stepsRemaining--;
    }

    private void StepRandomWay(SimNPC npc)
    {
        if (npc.stepsRemaining <= 0)
        {
            PickRandomDirection(npc);

            npc.stepsRemaining =
                npc.rng.Next(
                    npc.source.minRandomSteps,
                    npc.source.maxRandomSteps + 1);
        }

        int nextR = npc.row + npc.dirRow;
        int nextC = npc.col + npc.dirCol;

        if (!CanMoveTo(npc, nextR, nextC))
        {
            PickRandomDirection(npc);

            npc.stepsRemaining =
                npc.rng.Next(
                    npc.source.minRandomSteps,
                    npc.source.maxRandomSteps + 1);

            return;
        }

        npc.row = nextR;
        npc.col = nextC;

        npc.stepsRemaining--;
    }

    private void PickRandomDirection(SimNPC npc)
    {
        int d = npc.rng.Next(4);

        switch (d)
        {
            case 0:
                npc.dirRow = -1;
                npc.dirCol = 0;
                break;

            case 1:
                npc.dirRow = 1;
                npc.dirCol = 0;
                break;

            case 2:
                npc.dirRow = 0;
                npc.dirCol = -1;
                break;

            default:
                npc.dirRow = 0;
                npc.dirCol = 1;
                break;
        }
    }

    private bool CanMoveTo(
        SimNPC npc,
        int row,
        int col)
    {
        if (!board.IsInside(row, col))
            return false;

        if (board.IsOccupied(row, col))
            return false;

        if (!npc.OccupiesTwoCells())
            return true;

        int r2 = row + npc.dirRow;
        int c2 = col + npc.dirCol;

        if (!board.IsInside(r2, c2))
            return false;

        if (board.IsOccupied(r2, c2))
            return false;

        return true;
    }

    private void Reverse(SimNPC npc)
    {
        npc.dirRow = -npc.dirRow;
        npc.dirCol = -npc.dirCol;
    }

    private void AddOccupiedCells(
        List<Vector2Int> cells,
        SimNPC npc)
    {
        cells.Add(
            new Vector2Int(
                npc.row,
                npc.col));

        if (!npc.OccupiesTwoCells())
            return;

        int r2 =
            npc.row + npc.dirRow;

        int c2 =
            npc.col + npc.dirCol;

        if (Inside(r2, c2))
        {
            cells.Add(
                new Vector2Int(
                    r2,
                    c2));
        }
    }

    private bool Inside(
        int row,
        int col)
    {
        return
            row >= 0 &&
            row < ROWS &&
            col >= 0 &&
            col < COLS;
    }
    private void RebuildOccupancy(
    List<SimNPC> state)
    {
        board.Clear();

        foreach (var npc in state)
        {
            MarkOccupied(npc);
        }
    }

    private void MarkOccupied(
    SimNPC npc)
    {
        board.Occupy(
            npc.row,
            npc.col);

        if (!npc.OccupiesTwoCells())
            return;

        int r2 = npc.row + npc.dirRow;
        int c2 = npc.col + npc.dirCol;

        if (!Inside(r2, c2))
            return;

        board.Occupy(r2, c2);
    }

    private void UnmarkOccupied(
    SimNPC npc)
    {
        board.Release(
            npc.row,
            npc.col);

        if (!npc.OccupiesTwoCells())
            return;

        int r2 = npc.row + npc.dirRow;
        int c2 = npc.col + npc.dirCol;

        if (!Inside(r2, c2))
            return;

        board.Release(r2, c2);
    }
}