using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public BoardGenerator boardGenerator;
    public PlayerController playerController;
    public List<GameObject> npcPrefabs = new List<GameObject>();

    [Tooltip("Level asset (ScriptableObject) to apply")]
    public LevelData level;

    private List<GameObject> spawnedNPCs = new List<GameObject>();
    [Header("Debug")]
    [Tooltip("Automatically call ApplyLevel on Start (playmode) for testing)")]
    public bool applyOnStart = false;

    public void ApplyLevel()
    {
        StartCoroutine(ApplyLevelCoroutine());
    }

    private void Start()
    {
        if (applyOnStart && Application.isPlaying)
            ApplyLevel();
    }

    [ContextMenu("Validate Level Config")]
    public void ValidateLevelConfig()
    {
        if (boardGenerator == null) Debug.LogWarning("LevelManager.Validate: boardGenerator is null");
        if (playerController == null) Debug.LogWarning("LevelManager.Validate: playerController is null");
        if (level == null) Debug.LogWarning("LevelManager.Validate: level (LevelData) is null");
        if (npcPrefabs == null || npcPrefabs.Count == 0) Debug.LogWarning("LevelManager.Validate: npcPrefabs is empty");
        if (level != null)
        {
            Debug.Log($"LevelManager.Validate: level '{level.levelName}' has {level.npcs?.Count ?? 0} NPC entries, {level.blockedCells?.Count ?? 0} blocked, {level.destinations?.Count ?? 0} destinations");
        }
    }

    public void ClearLevel()
    {
        // clear occupancy and destination flags
        if (boardGenerator == null || boardGenerator.grid == null) return;
        int children = boardGenerator.grid.transform.childCount;
        for (int i = 0; i < children; i++)
        {
            var child = boardGenerator.grid.transform.GetChild(i);
            var cell = child.GetComponent<GridCell>();
            if (cell == null) continue;
            cell.ClearOccupancy();
            cell.isDestination = false;
        }

        // destroy spawned NPCs
        foreach (var go in spawnedNPCs)
            if (go != null) Destroy(go);
        spawnedNPCs.Clear();
    }

    private IEnumerator ApplyLevelCoroutine()
    {
        if (boardGenerator == null || boardGenerator.grid == null)
        {
            Debug.LogError("LevelManager: missing BoardGenerator or grid reference");
            yield break;
        }

        // clear previous runtime data first (destroy NPCs, clear any per-cell flags)
        Debug.Log("LevelManager: ApplyLevel starting...");
        ClearLevel();

        // copy level cell attributes into boardGenerator so GenerateBoard can spawn cells accordingly
        if (level != null)
        {
            boardGenerator.blockedCells = new System.Collections.Generic.List<Vector2Int>(level.blockedCells ?? new System.Collections.Generic.List<Vector2Int>());
            boardGenerator.destinationCells = new System.Collections.Generic.List<Vector2Int>(level.destinations ?? new System.Collections.Generic.List<Vector2Int>());
        }

        // regenerate board (now boardGenerator has blocked/destination lists)
        Debug.Log($"LevelManager: Generating board rows={boardGenerator.rows} cols={boardGenerator.columns}, blocked={boardGenerator.blockedCells?.Count ?? 0}, destinations={boardGenerator.destinationCells?.Count ?? 0}");
        boardGenerator.GenerateBoard();

        // wait for layout and children
        int attempts = 0;
        int maxAttempts = 120;
        int expected = Mathf.Max(0, boardGenerator.rows * boardGenerator.columns);
        while (boardGenerator.grid.transform.childCount < expected && attempts < maxAttempts)
        {
            attempts++;
            yield return null;
        }

        // force layout passes
        var gridRect = boardGenerator.grid.GetComponent<RectTransform>();
        if (gridRect != null)
        {
            for (int i = 0; i < 3; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                yield return null;
            }
        }

        // enforce cell attributes now that cells exist (overwrite any editor state)
        Debug.Log("LevelManager: Applying blocked/destination flags to spawned cells");
        int total = boardGenerator.grid.transform.childCount;
        for (int i = 0; i < total; i++)
        {
            var child = boardGenerator.grid.transform.GetChild(i);
            var cell = child.GetComponent<GridCell>();
            if (cell == null) continue;
            // clear previous editor destination/occupancy so level data is authoritative
            cell.isDestination = false;
            cell.ClearOccupancy();
        }

        // apply blocked/destination lists from LevelData (if present)
        if (level != null)
        {
            Debug.Log($"LevelManager: level present name={level.levelName} npcs={level.npcs?.Count ?? 0} blocked={level.blockedCells?.Count ?? 0} destinations={level.destinations?.Count ?? 0}");

            if (level.blockedCells != null)
            {
                foreach (var b in level.blockedCells)
                {
                    var c = GetCell(b.x, b.y);
                    if (c != null)
                    {
                        c.SetOccupied(true);
                    }
                    else
                        Debug.LogWarning($"LevelManager: blocked cell out of bounds ({b.x},{b.y})");
                }
            }

            if (level.destinations != null)
            {
                foreach (var d in level.destinations)
                {
                    var c = GetCell(d.x, d.y);
                    if (c != null)
                    {
                        c.isDestination = true;
                    }
                    else
                        Debug.LogWarning($"LevelManager: destination cell out of bounds ({d.x},{d.y})");
                }
            }
            // spawn NPCs. Use prefabIndex when valid; otherwise fall back to npcPrefabs order.
            int spawnCounter = 0;
            if (level.npcs == null || level.npcs.Count == 0)
            {
                Debug.LogWarning("LevelManager: LevelData.npcs is empty — nothing to spawn");
            }
            foreach (var def in level.npcs)
            {
                if (npcPrefabs == null || npcPrefabs.Count == 0)
                {
                    Debug.LogWarning("LevelManager: no NPC prefabs assigned in LevelManager.npcPrefabs");
                    break;
                }

                GameObject prefabToSpawn = null;
                if (def != null && def.prefabIndex >= 0 && def.prefabIndex < npcPrefabs.Count)
                {
                    prefabToSpawn = npcPrefabs[def.prefabIndex];
                }
                else
                {
                    // fallback: pick prefab by spawn order so designer doesn't need to set prefabIndex in LevelData
                    prefabToSpawn = npcPrefabs[spawnCounter % npcPrefabs.Count];
                }

                var go = Instantiate(prefabToSpawn);
                Debug.Log($"LevelManager: Instantiated NPC prefab {prefabToSpawn.name} for level entry row={def.row} col={def.column}");
                go.SetActive(false);
                var mover = go.GetComponent<NPCMover>();
                if (mover != null)
                {
                    mover.boardGenerator = boardGenerator;
                    mover.startRow = def.row;
                    mover.startColumn = def.column;
                    //mover.length = Mathf.Max(1, def.length);
                    //mover.type = def.type;
                    //mover.initialDirection = def.initialDirection;
                    //mover.fixedSteps = def.fixedSteps;
                    //mover.minRandomSteps = def.minRandomSteps;
                    //mover.maxRandomSteps = def.maxRandomSteps;

                    // prefer using playerController's container so overlays match
                    if (playerController != null)
                        mover.overrideContainer = playerController.playerContainer;
                }
                go.SetActive(true);
                Debug.Log($"LevelManager: Activated NPC instance {go.name}");
                spawnedNPCs.Add(go);
                spawnCounter++;
            }

            // set player start and place
            if (playerController != null)
            {
                playerController.startRow = level.playerStart.x;
                playerController.startColumn = level.playerStart.y;
                playerController.PlacePlayerAt(level.playerStart.x, level.playerStart.y);
            }
        }

        yield break;
    }

    private GridCell GetCell(int r, int c)
    {
        if (boardGenerator == null || boardGenerator.grid == null) return null;
        int columns = boardGenerator.columns;
        int index = r * columns + c;
        if (index >= 0 && index < boardGenerator.grid.transform.childCount)
        {
            var child = boardGenerator.grid.transform.GetChild(index);
            if (child != null) return child.GetComponent<GridCell>();
        }
        return null;
    }
}
