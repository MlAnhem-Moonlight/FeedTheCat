using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour
{
    public BoardGenerator boardGenerator;
    public PlayerController playerController;
    public List<GameObject> npcPrefabs = new List<GameObject>();
    public List<GameObject> itemPrefabs = new List<GameObject>();
    [Tooltip("Player prefab to spawn when applying level (optional). If empty, playerController.playerObject will be used.)")]
    public GameObject playerPrefab;

    [Tooltip("Level asset (ScriptableObject) to apply")]
    public LevelData level;

    private List<GameObject> spawnedNPCs = new List<GameObject>();
    private List<GameObject> spawnedItems = new List<GameObject>();

    // Guard flag to prevent ApplyLevelCoroutine from running concurrently
    private bool isApplyingLevel = false;

    [Header("Debug")]
    [Tooltip("Automatically call ApplyLevel on Start (playmode) for testing)")]
    public bool applyOnStart = false;

    public void ApplyLevel()
    {
        // Prevent concurrent execution of ApplyLevelCoroutine
        if (isApplyingLevel)
        {
            Debug.LogWarning("LevelManager.ApplyLevel: Already applying a level, ignoring duplicate call");
            return;
        }

        isApplyingLevel = true;
        StartCoroutine(ApplyLevelCoroutine());
    }

    private void Start()
    {
        //if (applyOnStart && Application.isPlaying)
        //{
        //    ApplyLevel();
        //    return;
        //}

        //// If we arrived here via SceneTransitionManager.GoToGameplay (using a loading scene)
        //// the next level to load may be stored in the SceneTransitionManager. Ensure the
        //// LevelManager picks it up when the Gameplay scene starts.
        //var next = SceneTransitionManager.GetNextLevelToLoad();
        //if (next != null)
        //{
        //    // If a level was queued by SceneTransitionManager, adopt that instance (clear after use)
        //    level = next;
        //    SceneTransitionManager.ClearNextLevelToLoad();
        //    ApplyLevel();
        //}
        //else if (GameManager.Instance != null && GameManager.Instance.currentLevel != null)
        //{
        //    // If GameManager has a runtime currentLevel (selected from another scene), use it and clear the reference.
        //    level = GameManager.Instance.currentLevel;
        //    GameManager.Instance.currentLevel = null;
        //    ApplyLevel();
        //}
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

        // destroy spawned items
        foreach (var go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();
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
                        c.SetDestination(true);
                    }
                    else
                        Debug.LogWarning($"LevelManager: destination cell out of bounds ({d.x},{d.y})");
                }
            }
            // ensure a single shared NPCContainer exists so NPCs don't each create their own
            RectTransform sharedNpcContainer = null;
            var foundNpc = GameObject.Find("NPCContainer");
            if (foundNpc != null)
                sharedNpcContainer = foundNpc.GetComponent<RectTransform>();
            else
            {
                Transform parentForNpcContainer = boardGenerator != null && boardGenerator.boardRect != null ? boardGenerator.boardRect.parent : (boardGenerator != null ? boardGenerator.transform : null);
                var npcGo = new GameObject("NPCContainer", typeof(RectTransform));
                sharedNpcContainer = npcGo.GetComponent<RectTransform>();
                if (parentForNpcContainer != null)
                    sharedNpcContainer.SetParent(parentForNpcContainer, false);
                else if (boardGenerator != null)
                    sharedNpcContainer.SetParent(boardGenerator.transform, false);

                if (boardGenerator != null && boardGenerator.boardRect != null)
                {
                    var br = boardGenerator.boardRect;
                    sharedNpcContainer.anchorMin = br.anchorMin;
                    sharedNpcContainer.anchorMax = br.anchorMax;
                    sharedNpcContainer.pivot = br.pivot;
                    sharedNpcContainer.sizeDelta = br.sizeDelta;
                    sharedNpcContainer.localScale = Vector3.one;
                    sharedNpcContainer.anchoredPosition = br.anchoredPosition;
                    sharedNpcContainer.localRotation = br.localRotation;
                    sharedNpcContainer.localPosition = br.localPosition;
                    sharedNpcContainer.SetAsLastSibling();
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

                    // BUG FIX (tiep tuc): truoc day co goi ConfigureFromPrefabIndex +
                    // gan rieng le fixedSteps/minRandomSteps/maxRandomSteps, NHUNG
                    // "def.seed" chua bao gio duoc truyen xuong NPCMover ca. NPCMover
                    // lai dung UnityEngine.Random (khong seed) cho moi buoc di ngau
                    // nhien (Patrol Random Steps, Random Way), trong khi SimNPC ben
                    // BFSSolver dung System.Random(def.seed) xac dinh. Ket qua: NPC
                    // luc choi thuc te di CHUYEN KHAC HOAN TOAN so voi quy dao ma
                    // BFSSolver da mo phong de xac nhan level nay "solvable" - level
                    // sinh cang nhieu iteration (cang bi day ve cau hinh chi con 1
                    // duong song sot mong manh) thi sai lech nay cang de lam NPC
                    // chan chet duong di trong game that.
                    //
                    // Initialize(def) thay the toan bo khoi gan field rieng le o
                    // tren: no gan startRow/startColumn/fixedSteps/minRandomSteps/
                    // maxRandomSteps, GOI ConfigureFromPrefabIndex, VA quan trong
                    // nhat la new System.Random(def.seed) - dung 1 nguon duy nhat
                    // voi SimNPC(NPCDef npc) ben phia generator, dam bao hanh vi
                    // NPC luc choi khop chinh xac voi luc BFS da xac nhan an toan.
                    mover.Initialize(def);

                    // give mover the shared NPC container so all NPCs use one parent
                    if (sharedNpcContainer != null)
                        mover.overrideContainer = sharedNpcContainer;
                    else if (playerController != null)
                        mover.overrideContainer = playerController.playerContainer;
                }
                go.SetActive(true);
                Debug.Log($"LevelManager: Activated NPC instance {go.name}");
                spawnedNPCs.Add(go);
                spawnCounter++;
            }

            // spawn items from level data
            if (level.items != null && level.items.Count > 0 && level.hasWon == false && level.hasPickup == false)
            {
                int itemCounter = 0;
                foreach (var itemDef in level.items)
                {
                    if (itemPrefabs == null || itemPrefabs.Count == 0)
                    {
                        Debug.LogWarning("LevelManager: no item prefabs assigned in LevelManager.itemPrefabs");
                        break;
                    }

                    GameObject itemPrefabToSpawn = null;
                    if (itemDef != null && itemDef.prefabIndex >= 0 && itemDef.prefabIndex < itemPrefabs.Count)
                    {
                        itemPrefabToSpawn = itemPrefabs[itemDef.prefabIndex];
                    }
                    else
                    {
                        // fallback: pick prefab by spawn order
                        itemPrefabToSpawn = itemPrefabs[itemCounter % itemPrefabs.Count];
                    }

                    var itemGo = Instantiate(itemPrefabToSpawn);
                    Debug.Log($"LevelManager: Instantiated item prefab {itemPrefabToSpawn.name} at row={itemDef.row} col={itemDef.column}");
                    itemGo.SetActive(false);

                    var itemCollector = itemGo.GetComponent<ItemCollector>();
                    if (itemCollector != null)
                    {
                        itemCollector.boardGenerator = boardGenerator;
                        itemCollector.row = itemDef.row;
                        itemCollector.column = itemDef.column;
                    }

                    // position and scale item to match grid cell
                    var targetCell = GetCell(itemDef.row, itemDef.column);
                    if (targetCell != null)
                    {
                        var itemRt = itemGo.GetComponent<RectTransform>();
                        var targetRt = targetCell.GetComponent<RectTransform>();

                        if (itemRt != null && targetRt != null)
                        {
                            // parent item under grid cell
                            itemRt.SetParent(targetCell.transform, false);

                            // position item at cell center (0, 0)
                            itemRt.anchoredPosition = Vector2.zero;

                            // scale item to match cell size
                            itemRt.sizeDelta = targetRt.sizeDelta;

                            Debug.Log($"LevelManager: Positioned and scaled item at ({itemDef.row},{itemDef.column}). Cell size={targetRt.sizeDelta}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"LevelManager: Target cell not found for item at ({itemDef.row},{itemDef.column})");
                    }

                    itemGo.SetActive(true);
                    Debug.Log($"LevelManager: Activated item instance {itemGo.name}");
                    spawnedItems.Add(itemGo);
                    itemCounter++;
                }
            }
            else
            {
                Debug.Log("LevelManager: LevelData.items is empty — no items to spawn");
            }

            // set player start and place
            if (playerController == null)
            {
                // create a runtime PlayerController if none assigned so player can be spawned
                Debug.LogWarning("LevelManager: playerController was null — creating runtime PlayerController.");
                var pcGo = new GameObject("PlayerController_Runtime", typeof(PlayerController));
                var pc = pcGo.GetComponent<PlayerController>();
                pc.boardGenerator = boardGenerator;
                playerController = pc;
            }

            if (playerController != null)
            {
                Debug.Log($"LevelManager: preparing player spawn. playerPrefab={(playerPrefab != null ? playerPrefab.name : "<none>")}, existingPlayerObject={(playerController.playerObject != null ? playerController.playerObject.name : "<none>")}");
                // spawn or assign player prefab
                if (playerPrefab != null)
                {
                    // destroy existing runtime playerObject if present
                    if (playerController.playerObject != null)
                    {
                        Destroy(playerController.playerObject);
                        playerController.playerObject = null;
                    }

                    bool prefabHasController = playerPrefab.GetComponent<PlayerController>() != null;
                    if (prefabHasController && playerController != null)
                    {
                        Debug.Log("LevelManager: Prefab has PlayerController; destroying existing runtime PlayerController before instantiating prefab.");
                        // When called from the editor (edit-time), prefer DestroyImmediate and clear selection
                        if (!Application.isPlaying)
                        {
#if UNITY_EDITOR
                            DestroyImmediate(playerController.gameObject);
                            // Clear selection to avoid Editor Inspectors holding a reference to the destroyed object
                            Selection.objects = new UnityEngine.Object[0];
                            Selection.activeObject = null;
#else
                            DestroyImmediate(playerController.gameObject);
#endif
                        }
                        else
                        {
                            Destroy(playerController.gameObject);
                        }
                        playerController = null;
                    }

                    var playerInstance = Instantiate(playerPrefab);
                    playerInstance.SetActive(false);
                    // if the prefab contains a PlayerController component on the instantiated object, use it
                    var prefabPc = playerInstance.GetComponent<PlayerController>();
                    if (prefabPc != null)
                    {
                        Debug.Log($"LevelManager: Player prefab contains PlayerController component ({playerInstance.name}). Using prefab controller.");
                        playerController = prefabPc;
                        if (playerController.boardGenerator == null)
                            playerController.boardGenerator = boardGenerator;
                        if (playerController.playerObject == null)
                            playerController.playerObject = playerInstance;
                        // don't perform manual parenting/size here; let the prefab controller handle initialization
                    }
                    else
                    {
                        // assign to existing controller as the visual object
                        if (playerController == null)
                        {
                            // create runtime controller if missing
                            var pcGo = new GameObject("PlayerController_Runtime", typeof(PlayerController));
                            playerController = pcGo.GetComponent<PlayerController>();
                            playerController.boardGenerator = boardGenerator;
                        }
                        playerController.playerObject = playerInstance;
                        // parent and prepare will be handled below as before
                    }
                    Debug.Log($"LevelManager: Spawned player prefab instance {playerInstance.name}");

                    // ensure a playerContainer exists and is properly aligned with boardRect
                    if (playerController.playerContainer == null)
                    {
                        var br = boardGenerator != null ? boardGenerator.boardRect : null;
                        Transform parentForContainer = br != null && br.parent != null ? br.parent : (boardGenerator != null ? boardGenerator.transform : null);

                        var goCont = new GameObject("PlayerContainer", typeof(RectTransform));
                        var rt = goCont.GetComponent<RectTransform>();
                        if (parentForContainer != null)
                            rt.SetParent(parentForContainer, false);
                        else if (boardGenerator != null)
                            rt.SetParent(boardGenerator.transform, false);

                        // Perfectly align playerContainer with boardRect so InverseTransformPoint works correctly
                        if (br != null)
                        {
                            rt.anchorMin = br.anchorMin;
                            rt.anchorMax = br.anchorMax;
                            rt.pivot = br.pivot;
                            rt.anchoredPosition = br.anchoredPosition;
                            rt.sizeDelta = br.sizeDelta;
                            rt.localRotation = br.localRotation;
                            rt.localScale = Vector3.one;
                            rt.SetAsLastSibling();  // place above boardRect in hierarchy
                        }

                        playerController.playerContainer = rt;
                    }

                    // parent player under container and disable raycast blocking
                    var prt = playerController.playerObject.GetComponent<RectTransform>();
                    if (prt != null && playerController.playerContainer != null)
                    {
                        prt.SetParent(playerController.playerContainer, false);
                        var cg = playerController.playerObject.GetComponent<CanvasGroup>();
                        if (cg == null) cg = playerController.playerObject.AddComponent<CanvasGroup>();
                        cg.blocksRaycasts = false;
                    }

                    playerController.playerObject.SetActive(true);
                }

                // ensure we have a player object to place
                if (playerController.playerObject == null)
                {
                    Debug.LogWarning("LevelManager: no playerPrefab assigned and playerController.playerObject is null. Creating runtime placeholder player.");
                    var placeholder = new GameObject("Player_Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    var img = placeholder.GetComponent<UnityEngine.UI.Image>();
                    img.color = Color.cyan;
                    // parent under playerContainer
                    if (playerController.playerContainer == null)
                    {
                        var br = boardGenerator != null ? boardGenerator.boardRect : null;
                        Transform parentForContainer = br != null && br.parent != null ? br.parent : (boardGenerator != null ? boardGenerator.transform : null);

                        var goCont = new GameObject("PlayerContainer", typeof(RectTransform));
                        var rt = goCont.GetComponent<RectTransform>();
                        if (parentForContainer != null)
                            rt.SetParent(parentForContainer, false);
                        else if (boardGenerator != null)
                            rt.SetParent(boardGenerator.transform, false);

                        // Perfectly align playerContainer with boardRect so InverseTransformPoint works correctly
                        if (br != null)
                        {
                            rt.anchorMin = br.anchorMin;
                            rt.anchorMax = br.anchorMax;
                            rt.pivot = br.pivot;
                            rt.anchoredPosition = br.anchoredPosition;
                            rt.sizeDelta = br.sizeDelta;
                            rt.localRotation = br.localRotation;
                            rt.localScale = Vector3.one;
                            rt.SetAsLastSibling();  // place above boardRect in hierarchy
                        }

                        playerController.playerContainer = rt;
                    }

                    var prt = placeholder.GetComponent<RectTransform>();
                    if (prt != null && playerController.playerContainer != null)
                    {
                        prt.SetParent(playerController.playerContainer, false);
                        var cg = placeholder.GetComponent<CanvasGroup>();
                        if (cg == null) cg = placeholder.AddComponent<CanvasGroup>();
                        cg.blocksRaycasts = false;
                    }

                    playerController.playerObject = placeholder;
                    Debug.Log("LevelManager: Created Player_Placeholder at runtime");
                }

                // Interpret level.playerStart as (x=row, y=column). If the provided values are out of bounds
                // try swapping them (common UI confusion between X/Y and Row/Column).
                int desiredRow = level.playerStart.x;
                int desiredCol = level.playerStart.y;
                int maxRows = boardGenerator != null ? boardGenerator.rows : 0;
                int maxCols = boardGenerator != null ? boardGenerator.columns : 0;

                if ((desiredRow < 0 || desiredRow >= maxRows || desiredCol < 0 || desiredCol >= maxCols)
                    && (level.playerStart.y >= 0 && level.playerStart.y < maxRows && level.playerStart.x >= 0 && level.playerStart.x < maxCols))
                {
                    Debug.LogWarning($"LevelManager: playerStart appears swapped in LevelData. Swapping coordinates ({level.playerStart.x},{level.playerStart.y}) -> ({level.playerStart.y},{level.playerStart.x}).");
                    // swap
                    int tmp = desiredRow;
                    desiredRow = desiredCol;
                    desiredCol = tmp;
                }

                // clamp to valid ranges
                if (maxRows > 0) desiredRow = Mathf.Clamp(desiredRow, 0, Mathf.Max(0, maxRows - 1));
                if (maxCols > 0) desiredCol = Mathf.Clamp(desiredCol, 0, Mathf.Max(0, maxCols - 1));

                // Set player start position on PlayerController.
                // PlayerController.InitializeAndPlace() will use these values to place the player.
                // Do NOT call PlacePlayerAt() here to avoid duplicate placement!
                playerController.startRow = desiredRow;
                playerController.startColumn = desiredCol;
                Debug.Log($"LevelManager: Set player start to ({desiredRow},{desiredCol}). PlayerController.InitializeAndPlace() will place it.");

                // Verify placement will happen (log expected cell)
                var expectedCell = GetCell(desiredRow, desiredCol);
                if (expectedCell != null)
                {
                    Debug.Log($"LevelManager: Expected player placement at ({desiredRow},{desiredCol}). Cell isEmpty={expectedCell.isEmpty}");
                }
                else
                {
                    Debug.LogWarning($"LevelManager: Expected player placement cell out of bounds ({desiredRow},{desiredCol})");
                }

                // scale player to match cell size if possible
                if (playerController.playerObject != null)
                {
                    var targetCell = GetCell(desiredRow, desiredCol);
                    if (targetCell != null)
                    {
                        var targetRt = targetCell.GetComponent<RectTransform>();
                        var playerRt = playerController.playerObject.GetComponent<RectTransform>();
                        var container = playerController.playerContainer;
                        if (targetRt != null && playerRt != null && container != null)
                        {
                            // compute desired size from target cell rect (world size)
                            Vector3[] corners = new Vector3[4];
                            targetRt.GetWorldCorners(corners);
                            Vector3 min = corners[0];
                            Vector3 max = corners[2];
                            Vector2 worldSize = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
                            Vector3 ls = container.lossyScale;
                            Vector2 sizeDelta = new Vector2(worldSize.x / (Mathf.Approximately(ls.x, 0f) ? 1f : ls.x), worldSize.y / (Mathf.Approximately(ls.y, 0f) ? 1f : ls.y));
                            playerRt.sizeDelta = sizeDelta;
                        }
                    }
                }

                // ensure playerController references the spawned object
                // (playerController.playerObject already set above)
            }
        }

        // Reset guard flag to allow future ApplyLevel calls
        isApplyingLevel = false;

        Debug.Log("LevelManager: ApplyLevelCoroutine completed successfully");
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