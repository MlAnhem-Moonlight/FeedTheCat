using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCMover : MonoBehaviour
{
    public enum NPCType { PatrolFixed, PatrolRandomSteps, RandomDirection, Idle }
    public enum Direction { Up, Down, Left, Right }

    [Header("References")]
    public BoardGenerator boardGenerator; // reference to board

    [Header("Initial Position")]
    public int startRow = 0;
    public int startColumn = 0;

    [Header("Behavior")]
    public NPCType type = NPCType.PatrolFixed;
    public Direction initialDirection = Direction.Right;
    [Tooltip("Number of cells this NPC occupies along its facing direction (1 = single cell, 2 = spans two cells)")]
    public int length = 1;

    // type 1
    public int fixedSteps = 3;

    // type 2
    public int minRandomSteps = 1;
    public int maxRandomSteps = 4;

    // type 3
    public int maxStepsBeforeChange = 3;

    // shared
    private int currentRow;
    private int currentColumn;
    private Vector2Int dirVec;
    private int stepsRemaining = 0;

    private RectTransform entityContainer;
    private RectTransform rectTransform;
    private List<GridCell> occupiedCells = new List<GridCell>();

    // Guard flag to prevent InitializeAndPlace from being called multiple times
    private bool isInitializing = false;

    [Header("Overrides")]
    [Tooltip("Optional: assign a shared container (e.g. PlayerContainer) so NPC aligns exactly with player. If empty the script will try to find or create one.")]
    public RectTransform overrideContainer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerStep += OnPlayerMoved;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerStep -= OnPlayerMoved;
    }

    private void OnDestroy()
    {
        // ensure we release occupancy if the NPC is destroyed at runtime
        ClearOccupiedCells();
    }

    private void Start()
    {
        if (boardGenerator == null)
            boardGenerator = FindAnyObjectByType<BoardGenerator>();

        // set starting direction
        dirVec = DirectionToVec(initialDirection);

        // initial steps
        ResetStepsForType();

        // place NPC at start (with guard flag to prevent duplicate initialization)
        if (!isInitializing)
        {
            isInitializing = true;
            StartCoroutine(InitializeAndPlace());
        }
        else
        {
            Debug.LogWarning($"NPCMover.Start: '{gameObject.name}' is already initializing, skipping duplicate InitializeAndPlace call");
        }
    }

    // Editor helper: log the expected occupied cells for this NPC
    [ContextMenu("Debug: Log Expected Occupied Cells")]
    private void DebugLogExpectedCells()
    {
        if (boardGenerator == null)
        {
            Debug.LogError("NPCMover.DebugLogExpectedCells: boardGenerator is null");
            return;
        }

        Vector2Int forward = DirectionToVec(initialDirection);
        if (forward == Vector2Int.zero) forward = Vector2Int.right;

        Debug.Log($"NPCMover.DebugLogExpectedCells: NPC '{gameObject.name}'");
        Debug.Log($"  Start Position: ({startRow}, {startColumn})");
        Debug.Log($"  Type: {type}, Direction: {initialDirection}, Length: {length}");
        Debug.Log($"  Direction Vector: {forward}");
        Debug.Log($"  Expected Occupied Cells:");

        for (int i = 0; i < length; i++)
        {
            int r = startRow + forward.y * i;
            int c = startColumn + forward.x * i;
            Debug.Log($"    Cell #{i}: ({r}, {c})");
        }
    }

    private void ResetStepsForType()
    {
        switch (type)
        {
            case NPCType.PatrolFixed:
                stepsRemaining = fixedSteps;
                break;
            case NPCType.PatrolRandomSteps:
                stepsRemaining = UnityEngine.Random.Range(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
                break;
            case NPCType.RandomDirection:
                stepsRemaining = UnityEngine.Random.Range(1, Mathf.Max(1, maxStepsBeforeChange) + 1);
                break;
        }
    }

    private System.Collections.IEnumerator InitializeAndPlace()
    {
        currentRow = Mathf.Clamp(startRow, 0, Math.Max(0, boardGenerator.rows - 1));
        currentColumn = Mathf.Clamp(startColumn, 0, Math.Max(0, boardGenerator.columns - 1));

        // Wait until the grid has been generated and layout has stabilized
        int attempts = 0;
        int maxAttempts = 120; // up to ~2 seconds at 60fps
        int expectedChildren = Mathf.Max(0, boardGenerator != null ? boardGenerator.rows * boardGenerator.columns : 0);

        while (boardGenerator == null || boardGenerator.grid == null || boardGenerator.grid.transform.childCount < expectedChildren)
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                Debug.LogWarning("NPCMover.InitializeAndPlace: timeout waiting for BoardGenerator/grid children");
                break;
            }
            yield return null;
        }

        // Force several layout passes until child rect sizes are non-zero
        RectTransform gridRect = null;
        if (boardGenerator != null && boardGenerator.grid != null)
            gridRect = boardGenerator.grid.GetComponent<RectTransform>();

        bool ready = false;
        attempts = 0;
        while (!ready && attempts < maxAttempts)
        {
            attempts++;
            if (gridRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            // ensure every child has a valid rect size
            ready = true;
            for (int i = 0; i < expectedChildren; i++)
            {
                if (boardGenerator == null || boardGenerator.grid == null || i >= boardGenerator.grid.transform.childCount)
                {
                    ready = false;
                    break;
                }
                var rt = boardGenerator.grid.transform.GetChild(i).GetComponent<RectTransform>();
                if (rt == null || Mathf.Approximately(rt.rect.width, 0f) || Mathf.Approximately(rt.rect.height, 0f))
                {
                    ready = false;
                    break;
                }
            }

            if (!ready)
                yield return new WaitForEndOfFrame();
        }

        if (!ready)
            Debug.LogWarning("NPCMover.InitializeAndPlace: grid children sizes not stable after wait");

        // try to find an existing shared container (prefer PlayerController's container).
        // Wait a few frames for PlayerController to create it if needed.
        entityContainer = null;
        for (int wait = 0; wait < 10; wait++)
        {
            entityContainer = FindSharedEntityContainer();
            if (entityContainer != null)
                break;
            yield return null;
        }

        if (entityContainer == null)
        {
            Transform parentForContainer = boardGenerator != null && boardGenerator.boardRect != null ? boardGenerator.boardRect.parent : (boardGenerator != null ? boardGenerator.transform : null);
            var go = new GameObject("NPCContainer", typeof(RectTransform));
            entityContainer = go.GetComponent<RectTransform>();
            if (parentForContainer != null)
                entityContainer.SetParent(parentForContainer, false);
            else if (boardGenerator != null)
                entityContainer.SetParent(boardGenerator.transform, false);

            // align with boardRect if available: copy anchored/local position so container sits exactly over the board
            if (boardGenerator != null && boardGenerator.boardRect != null)
            {
                var br = boardGenerator.boardRect;
                entityContainer.anchorMin = br.anchorMin;
                entityContainer.anchorMax = br.anchorMax;
                entityContainer.pivot = br.pivot;
                entityContainer.sizeDelta = br.sizeDelta;
                entityContainer.localScale = Vector3.one;
                // copy anchored/local positions to match board
                entityContainer.anchoredPosition = br.anchoredPosition;
                entityContainer.localRotation = br.localRotation;
                entityContainer.localPosition = br.localPosition;
                // place container above board in hierarchy so visuals render on top
                entityContainer.SetAsLastSibling();
            }
        }

        // ensure initial occupancy (NPC) for length (may span multiple cells)
        Debug.Log($"NPCMover.InitializeAndPlace: startCell lookup for ({currentRow},{currentColumn})");
        bool occupancySuccess = SetOccupiedCellsForPosition(currentRow, currentColumn);

        if (type == NPCType.Idle)
        {
            if (occupancySuccess)
                Debug.Log($"NPCMover.InitializeAndPlace (Idle): IDLE NPC spawned and occupies {occupiedCells.Count} cell(s)");
            else
                Debug.LogWarning($"NPCMover.InitializeAndPlace (Idle): FAILED to set occupancy for IDLE NPC");
        }

        // allow inspector override container
        if (overrideContainer != null)
            entityContainer = overrideContainer;

        // parent rectTransform under entityContainer for UI and position
        if (rectTransform != null && entityContainer != null)
        {
            rectTransform.SetParent(entityContainer, false);
            MoveToCellsVisual(occupiedCells);

            // force layout and retry so placement is stable
            if (gridRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                yield return null;
                MoveToCellsVisual(occupiedCells);
            }
        }

        // ensure NPC doesn't block raycasts
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        yield break;
    }

    private RectTransform FindSharedEntityContainer()
    {
        // Prefer a PlayerContainer if present
        var playerContainerGo = GameObject.Find("PlayerContainer");
        if (playerContainerGo != null)
            return playerContainerGo.GetComponent<RectTransform>();

        // Fallback to existing NPCContainer
        var npcContainerGo = GameObject.Find("NPCContainer");
        if (npcContainerGo != null)
            return npcContainerGo.GetComponent<RectTransform>();

        // If PlayerController exposes a known object, try to inspect it
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null && pc.playerObject != null)
        {
            // try parent container of player object
            var rt = pc.playerObject.GetComponent<RectTransform>();
            if (rt != null && rt.parent != null)
                return rt.parent as RectTransform;
        }

        return null;
    }

    private void OnPlayerMoved()
    {
        TryStepWhenPlayerMoves();
    }

    private void TryStepWhenPlayerMoves()
    {
        switch (type)
        {
            case NPCType.PatrolFixed:
                PatrolStep(false);
                break;
            case NPCType.PatrolRandomSteps:
                PatrolStep(true);
                break;
            case NPCType.RandomDirection:
                RandomDirectionStep();
                break;
            case NPCType.Idle:
                // do nothing
                break;
        }
    }

    private void PatrolStep(bool randomizeCountWhenReset)
    {
        if (stepsRemaining <= 0)
        {
            if (randomizeCountWhenReset)
                stepsRemaining = UnityEngine.Random.Range(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
            else
                stepsRemaining = fixedSteps;
        }

        int nextR = currentRow + dirVec.y;
        int nextC = currentColumn + dirVec.x;

        // when length > 1, ensure the target span is walkable as a whole
        var nextCell = GetCell(nextR, nextC);
        bool spanWalkable = true;
        if (length > 1)
        {
            // check second cell in same direction
            var second = GetCell(nextR + dirVec.y, nextC + dirVec.x);
            if (second == null || !second.IsWalkableForNPC())
                spanWalkable = false;
        }
        if (nextCell == null || !nextCell.IsWalkableForNPC() || !spanWalkable)
        {
            // hit wall or obstacle -> reverse direction and reset steps
            dirVec = -dirVec;
            stepsRemaining = randomizeCountWhenReset ? UnityEngine.Random.Range(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1) : fixedSteps;
            return; // no move this turn
        }

        // move
        MoveTo(nextR, nextC);
        stepsRemaining--;

        // if finished configured steps, reverse direction for next patrol leg
        if (stepsRemaining <= 0)
        {
            dirVec = -dirVec;
            stepsRemaining = randomizeCountWhenReset ? UnityEngine.Random.Range(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1) : fixedSteps;
            Debug.Log($"NPCMover.PatrolStep: completed steps at ({currentRow},{currentColumn}), reversing to ({dirVec.x},{dirVec.y}), nextSteps={stepsRemaining}");
        }
    }

    private void RandomDirectionStep()
    {
        if (stepsRemaining <= 0 || dirVec == Vector2Int.zero)
        {
            // pick random direction
            dirVec = DirectionToVec((Direction)UnityEngine.Random.Range(0, 4));
            stepsRemaining = UnityEngine.Random.Range(1, Mathf.Max(1, maxStepsBeforeChange) + 1);
        }

        int nextR = currentRow + dirVec.y;
        int nextC = currentColumn + dirVec.x;
        var nextCell = GetCell(nextR, nextC);
        bool spanWalkable = true;
        if (length > 1)
        {
            var second = GetCell(nextR + dirVec.y, nextC + dirVec.x);
            if (second == null || !second.IsWalkableForNPC())
                spanWalkable = false;
        }
        if (nextCell == null || !nextCell.IsWalkableForNPC() || !spanWalkable)
        {
            // pick a different random direction next time
            dirVec = DirectionToVec((Direction)UnityEngine.Random.Range(0, 4));
            stepsRemaining = UnityEngine.Random.Range(1, Mathf.Max(1, maxStepsBeforeChange) + 1);
            return;
        }

        MoveTo(nextR, nextC);
        stepsRemaining--;
    }

    private void MoveTo(int r, int c)
    {
        // moving from current span to new span; clear old occupied cells then set new ones
        ClearOccupiedCells();
        bool success = SetOccupiedCellsForPosition(r, c);
        if (!success)
        {
            // failed to occupy new span (blocked), do not update position
            // restore previous occupancy to be safe
            SetOccupiedCellsForPosition(currentRow, currentColumn);
            return;
        }

        bool hitPlayer = occupiedCells.Exists(x => x != null && x.occupiedByPlayer);

        currentRow = r;
        currentColumn = c;

        Debug.Log($"NPCMover.MoveTo: NPC moved to ({r},{c})");
        MoveToCellsVisual(occupiedCells);

        if (hitPlayer)
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager?.LoseLevel();
            Debug.Log("NPCMover: NPC entered player cell - PLAYER LOSE");
        }
    }

    private void MoveToCellVisual(GridCell target)
    {
        if (target == null)
            return;

        RectTransform targetRt =
            target.GetComponent<RectTransform>();

        if (targetRt == null)
            return;

        RectTransform boardRt =
            boardGenerator.boardRect;

        Vector3 localPos =
            boardRt.InverseTransformPoint(
                targetRt.position
            );

        rectTransform.SetParent(boardRt, false);

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.localPosition =
            new Vector3(
                localPos.x,
                localPos.y,
                0
            );

        rectTransform.SetAsLastSibling();
    }

    private GridCell GetCell(int r, int c)
    {
        if (boardGenerator == null || boardGenerator.grid == null)
            return null;

        if (r < 0 || r >= boardGenerator.rows || c < 0 || c >= boardGenerator.columns)
            return null;

        // Use child index mapping: index = row * columns + column (matches GridLayoutGroup ordering with Horizontal start axis)
        int columns = boardGenerator.columns;
        int index = r * columns + c;
        if (index >= 0 && index < boardGenerator.grid.transform.childCount)
        {
            var child = boardGenerator.grid.transform.GetChild(index);
            if (child != null)
                return child.GetComponent<GridCell>();
        }

        return null;
    }

    // --- multi-cell occupancy helpers ---
    private bool SetOccupiedCellsForPosition(int baseRow, int baseCol)
    {
        ClearOccupiedCells();
        occupiedCells.Clear();

        // gather cells along dirVec for length (length==1 uses only base cell)
        Vector2Int forward = dirVec;
        // if dirVec is zero (not set), default to Right to compute span
        if (forward == Vector2Int.zero) 
        {
            forward = Vector2Int.right;
            Debug.LogWarning($"NPCMover.SetOccupiedCellsForPosition: dirVec was zero, defaulting to Right");
        }

        Debug.Log($"NPCMover.SetOccupiedCellsForPosition: base=({baseRow},{baseCol}) direction={forward} length={length}");

        for (int i = 0; i < length; i++)
        {
            int r = baseRow + forward.y * i;
            int c = baseCol + forward.x * i;
            Debug.Log($"  Cell #{i}: target=({r},{c})");

            var cell = GetCell(r, c);
            if (cell == null)
            {
                Debug.LogWarning($"  Cell #{i} at ({r},{c}) is NULL (out of bounds or not found). Rollback!");
                // roll back
                foreach (var oc in occupiedCells)
                    if (oc != null) oc.SetOccupiedByNPC(false);
                occupiedCells.Clear();
                return false;
            }

            if (!cell.IsWalkableForNPC())
            {
                Debug.LogWarning($"  Cell #{i} at ({r},{c}) is NOT walkable (occupied or destination). Rollback!");
                // roll back
                foreach (var oc in occupiedCells)
                    if (oc != null) oc.SetOccupiedByNPC(false);
                occupiedCells.Clear();
                return false;
            }

            occupiedCells.Add(cell);
            cell.SetOccupiedByNPC(true);
            Debug.Log($"  Cell #{i} at ({r},{c}) SET occupiedByNPC=true");
        }

        Debug.Log($"NPCMover.SetOccupiedCellsForPosition: SUCCESS - occupied {occupiedCells.Count} cells");
        return true;
    }

    private void ClearOccupiedCells()
    {
        if (occupiedCells == null) return;
        foreach (var c in occupiedCells)
            if (c != null) c.SetOccupiedByNPC(false);
        occupiedCells.Clear();
    }

    private void MoveToCellsVisual(List<GridCell> cells)
    {
        if (cells == null || cells.Count == 0) return;

        // compute world bounds covering all cells
        RectTransform boardRt = boardGenerator.boardRect;
        if (boardRt == null) return;

        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        for (int i = 0; i < cells.Count; i++)
        {
            var rt = cells[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            for (int j = 0; j < 4; j++)
            {
                min = Vector3.Min(min, corners[j]);
                max = Vector3.Max(max, corners[j]);
            }
        }

        // midpoint in world space
        Vector3 worldCenter = (min + max) * 0.5f;

        // convert to entityContainer local space (entityContainer should be parent)
        if (entityContainer == null)
            entityContainer = rectTransform.parent as RectTransform;
        if (entityContainer == null)
            entityContainer = FindSharedEntityContainer();
        if (entityContainer == null) return;

        Vector3 localCenter = entityContainer.InverseTransformPoint(worldCenter);

        // set transform under entityContainer
        rectTransform.SetParent(entityContainer, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localPosition = new Vector3(localCenter.x, localCenter.y, 0);

        // compute desired size in world space then convert to local sizeDelta accounting for lossyScale
        Vector3 worldSize = max - min;
        Vector2 desiredSize = new Vector2(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y));
        Vector3 ls = entityContainer.lossyScale;
        Vector2 sizeDelta = new Vector2(desiredSize.x / (Mathf.Approximately(ls.x, 0f) ? 1f : ls.x), desiredSize.y / (Mathf.Approximately(ls.y, 0f) ? 1f : ls.y));
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.SetAsLastSibling();
    }

    private Vector2Int DirectionToVec(Direction d)
    {
        switch (d)
        {
            case Direction.Up: return new Vector2Int(0, -1);
            case Direction.Down: return new Vector2Int(0, 1);
            case Direction.Left: return new Vector2Int(-1, 0);
            case Direction.Right: return new Vector2Int(1, 0);
        }
        return Vector2Int.right;
    }
}