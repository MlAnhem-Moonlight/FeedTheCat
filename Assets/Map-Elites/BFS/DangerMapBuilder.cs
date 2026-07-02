using System.Collections.Generic;
using UnityEngine;

public class DangerMapBuilder
{
    public readonly int maxTurns;

    private bool[,,] danger;
    private EnemySimulator simulator;

    public DangerMapBuilder(
        List<NPCDef> npcs,
        int maxTurns)
    {
        this.maxTurns = maxTurns;
        this.simulator = new EnemySimulator(npcs);

        danger =
            new bool[
                maxTurns + 1,
                8,
                6
            ];

        Build();
    }

    private void Build()
    {
        for (int turn = 0;
             turn <= maxTurns;
             turn++)
        {
            var occupied =
                simulator.GetOccupiedCells(turn);

            foreach (var cell in occupied)
            {
                if (
                    cell.x >= 0 &&
                    cell.x < 8 &&
                    cell.y >= 0 &&
                    cell.y < 6)
                {
                    danger[
                        turn,
                        cell.x,
                        cell.y
                    ] = true;
                }
            }
        }
    }

    public bool IsDanger(
        int turn,
        int row,
        int col)
    {
        if (
            row < 0 ||
            row >= 8 ||
            col < 0 ||
            col >= 6)
        {
            return true;
        }

        turn =
            Mathf.Clamp(
                turn,
                0,
                maxTurns);

        return danger[
            turn,
            row,
            col];
    }

    public bool[,] GetDangerGrid(
        int turn)
    {
        bool[,] grid =
            new bool[8, 6];

        turn =
            Mathf.Clamp(
                turn,
                0,
                maxTurns);

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                grid[r, c] =
                    danger[
                        turn,
                        r,
                        c];
            }
        }

        return grid;
    }

    public int CountDangerCells(
        int turn)
    {
        turn =
            Mathf.Clamp(
                turn,
                0,
                maxTurns);

        int count = 0;

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                if (danger[
                    turn,
                    r,
                    c])
                {
                    count++;
                }
            }
        }

        return count;
    }
}