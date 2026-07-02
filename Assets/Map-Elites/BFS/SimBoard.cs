using UnityEngine;

public class SimBoard
{
    public const int Rows = 8;
    public const int Cols = 6;

    private bool[,] occupied;

    public SimBoard()
    {
        occupied = new bool[Rows, Cols];
    }

    public void Clear()
    {
        System.Array.Clear(
            occupied,
            0,
            occupied.Length);
    }

    public bool IsInside(
        int row,
        int col)
    {
        return
            row >= 0 &&
            row < Rows &&
            col >= 0 &&
            col < Cols;
    }

    public bool IsOccupied(
        int row,
        int col)
    {
        if (!IsInside(row, col))
            return true;

        return occupied[row, col];
    }

    public void Occupy(
        int row,
        int col)
    {
        if (IsInside(row, col))
            occupied[row, col] = true;
    }

    public void Release(
        int row,
        int col)
    {
        if (IsInside(row, col))
            occupied[row, col] = false;
    }
}