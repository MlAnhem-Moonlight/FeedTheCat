using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NPCDef
{
    public int row;
    public int column;

    [Tooltip("Index của prefab trong danh sách npcPrefabs")]
    public int prefabIndex = 0;


    //public NPCMover.Direction direction;
    public int seed;

    public int fixedSteps = 3;

    public int minRandomSteps = 1;
    public int maxRandomSteps = 4;

    //public NPCMover.NPCType type = NPCMover.NPCType.PatrolFixed;
    //public NPCMover.Direction initialDirection = NPCMover.Direction.Right;
    //[Tooltip("How many cells the NPC occupies along its facing direction (1 = single, 2 = spans two cells)")]
    //public int length = 1;
    //public int fixedSteps = 3;
    //public int minRandomSteps = 1;
    //public int maxRandomSteps = 3;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "FeedTheCat/LevelData", order = 0)]
public class LevelData : ScriptableObject
{
    public string levelName;
    public List<NPCDef> npcs = new List<NPCDef>();
    public List<Vector2Int> destinations = new List<Vector2Int>();
    public Vector2Int playerStart = new Vector2Int(0, 0);
    public List<Vector2Int> blockedCells = new List<Vector2Int>();
    public bool hasWon = false;
    [Tooltip("If true, level is locked and cannot be played until unlocked")]
    public bool isLocked = true;
}

public enum LevelDifficulty
{
    Easy,
    Medium,
    Hard
}