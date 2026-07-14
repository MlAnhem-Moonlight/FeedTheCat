using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using FeedTheCat.Items;

// Requires GridCell script on each cell and BoardGenerator to set rows/columns
public class PlayerController : MonoBehaviour
{
    public static event Action OnPlayerStep;
    private static PlayerController s_instance;
    public BoardGenerator boardGenerator;
    public GameObject playerObject; // UI object representing player
    [Tooltip("Optional container transform to host the player (must NOT be the GridLayoutGroup transform). If empty, one will be created as a sibling of the boardRect.")]
    public RectTransform playerContainer;

    [Header("Start Position (row, column)")]
    public int startRow = 0;
    public int startColumn = 0;

    private GridCell[,] cells;
    private int rows;
    private int columns;

    private int currentRow;
    private int currentColumn;
    private bool hasPlaced = false;
    private bool isInitialPlacement = false;  // Flag to track if this is the initial placement from InitializeAndPlace()

    private List<GridCell> highlighted = new List<GridCell>();

    private void Start()
    {
        // Enforce a single PlayerController instance. If another exists, destroy this one to avoid duplicate placement logic.
        if (s_instance != null && s_instance != this)
        {
            Debug.LogWarning($"PlayerController: another instance already exists ({s_instance.gameObject.name}). Destroying duplicate ({gameObject.name}).");
            if (Application.isPlaying)
                Destroy(this.gameObject);
            else
                DestroyImmediate(this.gameObject);
            return;
        }
        s_instance = this;
        if (boardGenerator == null)
            //Debug.LogError("PlayerController needs a reference to BoardGenerator.");
            boardGenerator = FindAnyObjectByType<BoardGenerator>();

        if (playerObject == null)
            Debug.LogError("Assign playerObject (UI) to PlayerController.");

        // quick diagnostics for UI input
        if (boardGenerator != null && boardGenerator.boardRect != null)
        {
            var canvas = boardGenerator.boardRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                Debug.LogWarning("PlayerController: No Canvas found in parents of boardRect. UI clicks will not work.");
            else
            {
                var gr = canvas.GetComponent<GraphicRaycaster>();
                if (gr == null)
                    Debug.LogWarning("PlayerController: Canvas missing GraphicRaycaster. Add one so UI clicks are detected.");
            }
        }

        if (UnityEngine.EventSystems.EventSystem.current == null)
            Debug.LogWarning("PlayerController: No EventSystem in scene. Add one for UI pointer events.");

        StartCoroutine(InitializeAndPlace());

    }

    private IEnumerator InitializeAndPlace()
    {
        // Wait until BoardGenerator has created cells (or timeout)
        int attempts = 0;
        const int maxAttempts = 60; // wait up to ~1 second

        while (attempts < maxAttempts)
        {
            InitializeGrid();

            if (rows > 0 && columns > 0 && cells != null && boardGenerator.grid != null && boardGenerator.grid.transform.childCount >= rows * columns)
                break;

            attempts++;
            yield return null;
        }

        // Ensure playerObject is parented under a dedicated container that is NOT the GridLayoutGroup transform
        if (playerObject != null)
        {
            // create container if not assigned
            if (playerContainer == null)
            {
                Transform parentForContainer = null;
                if (boardGenerator != null && boardGenerator.boardRect != null && boardGenerator.boardRect.parent != null)
                    parentForContainer = boardGenerator.boardRect.parent;
                else if (boardGenerator != null)
                    parentForContainer = boardGenerator.transform.parent;

                var go = new GameObject("PlayerContainer", typeof(RectTransform));
                var rt = go.GetComponent<RectTransform>();
                if (parentForContainer != null)
                    rt.SetParent(parentForContainer, false);
                else
                    rt.SetParent(boardGenerator != null ? boardGenerator.transform : null, false);

                // match boardRect size/anchoring if possible, ensure neutral transform and no layout components
                if (boardGenerator != null && boardGenerator.boardRect != null)
                {
                    rt.anchorMin = boardGenerator.boardRect.anchorMin;
                    rt.anchorMax = boardGenerator.boardRect.anchorMax;
                    rt.pivot = boardGenerator.boardRect.pivot;
                    rt.sizeDelta = boardGenerator.boardRect.sizeDelta;
                    rt.localScale = Vector3.one;
                }

                // remove any layout components that might exist on the created container's parent side-effects
                var lg = go.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (lg != null)
                    GameObject.DestroyImmediate(lg);

                playerContainer = rt;
            }

            // parent player under the container (this avoids GridLayoutGroup reflow)
            var playerRt = playerObject.GetComponent<RectTransform>();
            if (playerRt != null && playerContainer != null)
            {
                playerRt.SetParent(playerContainer, false);
            }
            else if (playerObject.transform.parent == boardGenerator.grid.transform)
            {
                // ensure it's not parented to grid
                playerObject.transform.SetParent(playerContainer != null ? playerContainer : boardGenerator.transform, false);
            }

            // make player not block raycasts so underlying cells receive touches
            var cg = playerObject.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = playerObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
        }

        // Clamp start position
        int r = Mathf.Clamp(startRow, 0, Mathf.Max(0, boardGenerator.rows - 1));
        int c = Mathf.Clamp(startColumn, 0, Mathf.Max(0, boardGenerator.columns - 1));

        // If requested cell is unavailable, find first empty cell
        if (cells == null || cells[r, c] == null || !cells[r, c].isEmpty)
        {
            bool found = false;
            if (cells != null)
            {
                for (int i = 0; i < rows && !found; i++)
                    for (int j = 0; j < columns && !found; j++)
                        if (cells[i, j] != null && cells[i, j].isEmpty)
                        {
                            r = i; c = j; found = true;
                        }
            }
        }

        // Final placement
        // Ensure layout positions are finalized before placing player
        if (boardGenerator != null && boardGenerator.grid != null)
        {
            var gridRect = boardGenerator.grid.GetComponent<RectTransform>();
            if (gridRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                // wait a frame to let Unity finish layout passes
                yield return null;
            }
        }

        // Ensure playerContainer is perfectly aligned with boardRect: same parent, position, size, and anchors.
        // This guarantees InverseTransformPoint in MovePlayerObjectToTarget produces correct coordinates.
        if (playerContainer != null && boardGenerator != null && boardGenerator.boardRect != null)
        {
            var br = boardGenerator.boardRect;
            playerContainer.SetParent(br.parent, false);  // must be sibling of boardRect
            playerContainer.anchorMin = br.anchorMin;
            playerContainer.anchorMax = br.anchorMax;
            playerContainer.pivot = br.pivot;
            playerContainer.anchoredPosition = br.anchoredPosition;
            playerContainer.sizeDelta = br.sizeDelta;
            playerContainer.localRotation = br.localRotation;
            playerContainer.localScale = Vector3.one;
        }

        // Grid is already initialized above (line 78), no need to re-initialize
        isInitialPlacement = true;  // Mark this as initial placement
        PlaceAt(r, c);
        isInitialPlacement = false;
        CollisionDestructionItem cdi = FindAnyObjectByType<CollisionDestructionItem>();
        cdi?.InitializeReferences();
    }

    // Position player object without making it a child of the GridLayoutGroup.
    // If playerObject has a RectTransform (UI), parent it under boardRect and set local position to match the cell.
    // Otherwise, set world position to the cell's world position.
    private void MovePlayerObjectToTarget(GridCell target)
    {
        if (playerObject == null || target == null)
            return;

        RectTransform playerRt = playerObject.GetComponent<RectTransform>();
        RectTransform targetRt = target.GetComponent<RectTransform>();
        RectTransform boardRt = boardGenerator != null ? boardGenerator.boardRect : null;

        // If player is UI and we have a playerContainer, position relative to that container
        if (playerRt != null && playerContainer != null)
        {
            // Ensure player rect uses centered anchors so localPosition maps to center
            playerRt.anchorMin = new Vector2(0.5f, 0.5f);
            playerRt.anchorMax = new Vector2(0.5f, 0.5f);
            playerRt.pivot = new Vector2(0.5f, 0.5f);

            // Compute target world position
            Vector3 worldCenter = (targetRt != null) ? targetRt.position : target.transform.position;

            // CRITICAL: Convert world position to playerContainer's local space
            // PlayerContainer should be aligned with boardRect (same parent, position, size)
            Vector3 localInContainer = playerContainer.InverseTransformPoint(worldCenter);

            // Parent under container and set local position
            playerRt.SetParent(playerContainer, false);
            playerRt.localPosition = new Vector3(localInContainer.x, localInContainer.y, 0f);

            Debug.Log($"MovePlayerObjectToTarget: cell=({target.row},{target.column}) world={worldCenter:F2} localInContainer={localInContainer:F2} playerRt.localPos={playerRt.localPosition:F2}");

            // Also log boardRect and playerContainer alignment for debugging
            if (boardRt != null)
            {
                Debug.Log($"  boardRect: pos={boardRt.anchoredPosition:F2} size={boardRt.sizeDelta:F2}");
                Debug.Log($"  playerContainer: pos={playerContainer.anchoredPosition:F2} size={playerContainer.sizeDelta:F2}");
            }

            // Ensure player is rendered above cells
            playerRt.SetAsLastSibling();
            return;
        }

        // Otherwise, place as a world object (non-UI)
        playerObject.transform.position = target.transform.position;
    }

    private void InitializeGrid()
    {
        if (boardGenerator == null || boardGenerator.grid == null)
        {
            rows = 0; columns = 0; cells = null;
            return;
        }

        rows = boardGenerator.rows;
        columns = boardGenerator.columns;
        cells = new GridCell[rows, columns];

        // Map children by hierarchy order to rows/columns (this matches GridLayoutGroup ordering)
        int childCount = boardGenerator.grid.transform.childCount;
        //Debug.Log($"PlayerController.InitializeGrid: grid childCount={childCount}, expected={rows * columns}");
        for (int i = 0; i < childCount; i++)
        {
            var child = boardGenerator.grid.transform.GetChild(i);
            var c = child.GetComponent<GridCell>();
            if (c == null)
                continue;

            int r = i / columns;
            int col = i % columns;
            if (r >= 0 && r < rows && col >= 0 && col < columns)
            {
                c.row = r;
                c.column = col;
                cells[r, col] = c;
                c.onClick -= HandleCellClicked;
                c.onClick += HandleCellClicked;
            }
        }

        int mapped = 0;
        for (int i = 0; i < rows; i++) for (int j = 0; j < columns; j++) if (cells[i, j] != null) mapped++;
        //Debug.Log($"PlayerController.InitializeGrid: mapped {mapped}/{rows * columns} cells (by child index)");
    }

    private void HandleCellClicked(GridCell cell)
    {
        // If clicked cell is one of highlighted (reachable) cells, move there
        if (cell == null)
            return;

        Debug.Log($"HandleCellClicked: clicked cell ({cell.row},{cell.column}) isEmpty={cell.isEmpty}");

        if (IsAdjacent(cell.row, cell.column) && cell.IsEmptyForPlayer())
        {
            MoveTo(cell.row, cell.column);
        }
    }

    private bool IsAdjacent(int r, int c)
    {
        int dr = Mathf.Abs(r - currentRow);
        int dc = Mathf.Abs(c - currentColumn);
        return (dr + dc) == 1; // Manhattan distance of 1 (up/down/left/right)
    }

    private void MoveTo(int r, int c)
    {
        var current = cells[currentRow, currentColumn];
        GridCell target = cells[r, c];

        if (target == null)
            return;

        if (!target.IsEmptyForPlayer())
            return;

        if (current != null)
            current.SetOccupiedByPlayer(false);
        target.SetOccupiedByPlayer(true);

        currentRow = r;
        currentColumn = c;

        Debug.Log($"MoveTo: moved player to ({r},{c})");

        MovePlayerObjectToTarget(target);

        UpdateHighlights();
        // notify NPCs that player moved one step (NPCs read their current
        // Stun/Charm status here and move - or don't - accordingly)
        OnPlayerStep?.Invoke();

        // Now advance status effect durations for the turn that just happened.
        // This MUST run after OnPlayerStep so NPCs are still correctly
        // considered Stunned/Charmed on the exact turn their effect expires.
        StatusEffectSystem.Instance?.Tick();
    }

    private void PlaceAt(int r, int c)
    {
        // Ensure grid is initialized
        InitializeGrid();

        // Only clear other player occupancy if this is NOT the initial placement from InitializeAndPlace.
        // During initial placement, we trust LevelManager's setup.
        if (!isInitialPlacement && cells != null)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var cc = cells[i, j];
                    if (cc == null) continue;
                    if (i == r && j == c) continue;
                    if (cc.occupiedByPlayer) cc.SetOccupiedByPlayer(false);
                }
            }
        }

        currentRow = r;
        currentColumn = c;

        GridCell target = cells[r, c];

        if (target != null)
        {
            target.SetOccupiedByPlayer(true);
            MovePlayerObjectToTarget(target);
        }

        hasPlaced = true;

        UpdateHighlights();

        // Sanity check: ensure only one cell is marked as occupiedByPlayer. If multiple, log diagnostic info.
        if (cells != null && !isInitialPlacement)
        {
            int found = 0;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    var cc = cells[i, j];
                    if (cc != null && cc.occupiedByPlayer) found++;
                }
            if (found > 1)
            {
                Debug.LogWarning($"PlayerController.PlaceAt: multiple cells ({found}) marked occupiedByPlayer after placing at ({r},{c}). This may indicate duplicate PlayerControllers or stale cell state.");
            }
        }
    }

    // Public helper to allow external callers (e.g. NPCMover) to request the player highlight refresh
    public void RefreshHighlights()
    {
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        // clear previous
        foreach (var h in highlighted)
        {
            if (h != null)
                h.SetHighlight(false);
        }
        highlighted.Clear();

        // four directions
        TryHighlight(currentRow - 1, currentColumn);
        TryHighlight(currentRow + 1, currentColumn);
        TryHighlight(currentRow, currentColumn - 1);
        TryHighlight(currentRow, currentColumn + 1);
    }

    private void TryHighlight(int r, int c)
    {
        if (r < 0 || r >= rows || c < 0 || c >= columns)
            return;

        var cell = cells[r, c];
        if (cell == null)
            return;

        if (cell.isEmpty)
        {
            cell.SetHighlight(true);
            highlighted.Add(cell);
        }
    }

    private void OnDestroy()
    {
        // unsubscribe
        if (cells == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                var cell = cells[r, c];
                if (cell != null)
                    cell.onClick -= HandleCellClicked;
            }
    }

    // Public helper so LevelManager can place the player at runtime after level data is applied
    public void PlacePlayerAt(int r, int c)
    {
        InitializeGrid();
        PlaceAt(r, c);
    }
}