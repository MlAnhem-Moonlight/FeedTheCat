using System;

[Serializable]
public struct BFSState : IEquatable<BFSState>
{
    public int row;
    public int col;
    public int turn;

    public BFSState(int row, int col, int turn)
    {
        this.row = row;
        this.col = col;
        this.turn = turn;
    }

    public bool Equals(BFSState other)
    {
        return row == other.row &&
               col == other.col &&
               turn == other.turn;
    }

    public override bool Equals(object obj)
    {
        return obj is BFSState other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + row;
            hash = hash * 31 + col;
            hash = hash * 31 + turn;
            return hash;
        }
    }

    public override string ToString()
    {
        return $"({row},{col}) T={turn}";
    }
}