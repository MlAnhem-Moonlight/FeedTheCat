using UnityEngine;

[System.Serializable]
public class EliteCellBFS
{
    public LevelData level;

    public DifficultyResultBFS result;

    public LevelDifficulty difficulty;

    public bool occupied;

    public float fitness;
}