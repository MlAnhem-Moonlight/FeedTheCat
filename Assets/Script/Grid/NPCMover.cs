using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FeedTheCat.Items;

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

    // RNG rieng cho NPC nay, PHAI duoc seed tu NPCDef.seed (xem Initialize())
    // de khop CHINH XAC voi SimNPC.rng dung trong BFSSolver luc generate.
    //
    // TRUOC DAY: moi cho random step-count/huong-di deu dung
    // UnityEngine.Random (RNG toan cuc, KHONG lien quan gi toi npc.seed) ->
    // NPC luc choi thuc te di CHUYEN HOAN TOAN KHAC voi quy dao ma BFSSolver
    // da mo phong de xac nhan level "solvable". Day la nguyen nhan chinh
    // khien level nhieu iteration (bi MAP-Elites day ve cau hinh "chi co
    // dung 1 duong song sot mong manh") de vo tinh bi NPC that chan chet
    // duong di, du BFS da xac nhan solvable/fullClearSolvable tren giay.
    private System.Random rng;

    // Expose current grid coordinates for other systems (radius checks, debugging)
    public int CurrentRow => currentRow;
    public int CurrentColumn => currentColumn;

    /// <summary>
    /// Goi ngay sau khi Instantiate prefab NPC va TRUOC KHI Start() chay (tuc
    /// la ngay trong cung frame instantiate - Start() cua Unity chi chay o
    /// frame/Update dau tien nen thu tu nay luon dam bao duoc). Thiet lap
    /// toan bo thong so NPC (vi tri, loai, so buoc, VA quan trong nhat la
    /// RNG seed) tu dung 1 nguon NPCDef - giong het cach SimNPC(NPCDef npc)
    /// khoi tao ben phia generator, de hanh vi luc choi khop 100% voi luc
    /// BFSSolver da mo phong va xac nhan level nay an toan.
    /// </summary>
    public void Initialize(NPCDef def)
    {
        startRow = def.row;
        startColumn = def.column;
        fixedSteps = def.fixedSteps;
        minRandomSteps = def.minRandomSteps;
        maxRandomSteps = def.maxRandomSteps;

        rng = new System.Random(def.seed);

        ConfigureFromPrefabIndex(def.prefabIndex);
    }

    /// <summary>
    /// Whether this NPC is capable of moving at all. Idle NPCs never move under
    /// any circumstance - not even when targeted by a movement-inducing status
    /// effect like Charm. They can still receive status effects and show the
    /// corresponding visual indicator, but no code path should ever call MoveTo()
    /// on an Idle NPC. Item scripts (e.g. Charm items) should check this before
    /// picking which NPC gets told to walk toward a target.
    /// </summary>
    public bool CanMove()
    {
        return type != NPCType.Idle;
    }

    // Deterministic mapping from NPCDef.prefabIndex to (type, initialDirection, length),
    // matching EXACTLY the convention used on the generation side:
    // SimNPC.InitializeDirection() / LevelGenUtil.GetIdleDirection() / OccupiesTwoCells().
    // 0-3 = Idle (Up/Down/Left/Right, spans 2 cells), 4-5 = Fixed Patrol (Horizontal/Vertical),
    // 6-7 = Random-step Patrol (Horizontal/Vertical), 8 = Random Way.
    // Called by LevelManager right after instantiating the NPC prefab so that in-game
    // behavior always matches what BFSSolver/LevelGenUtil validated during generation,
    // instead of depending on whatever type/direction/length happens to be set in that
    // particular prefab's Inspector.
    public void ConfigureFromPrefabIndex(int prefabIndex)
    {
        switch (prefabIndex)
        {
            case 0: type = NPCType.Idle; initialDirection = Direction.Up; length = 2; break;
            case 1: type = NPCType.Idle; initialDirection = Direction.Down; length = 2; break;
            case 2: type = NPCType.Idle; initialDirection = Direction.Left; length = 2; break;
            case 3: type = NPCType.Idle; initialDirection = Direction.Right; length = 2; break;
            case 4: type = NPCType.PatrolFixed; initialDirection = Direction.Right; length = 1; break;
            case 5: type = NPCType.PatrolFixed; initialDirection = Direction.Down; length = 1; break;
            case 6: type = NPCType.PatrolRandomSteps; initialDirection = Direction.Right; length = 1; break;
            case 7: type = NPCType.PatrolRandomSteps; initialDirection = Direction.Down; length = 1; break;
            case 8: type = NPCType.RandomDirection; initialDirection = Direction.Right; length = 1; break;
            default:
                Debug.LogWarning($"NPCMover.ConfigureFromPrefabIndex: unknown prefabIndex {prefabIndex}, keeping whatever is set in the Inspector ({type}, {initialDirection}, length={length})");
                break;
        }
    }

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

        // An toan cuoi cung: neu ai do quen goi Initialize(NPCDef) sau khi
        // Instantiate (vi du code cu con dat startRow/startColumn/type truc
        // tiep tu Inspector hoac field-by-field), dung UnityEngine.Random
        // lam nguon fallback thay vi NullReferenceException - nhung canh bao
        // ro rang vi hanh vi NPC nay se KHONG khop voi BFSSolver da mo
        // phong (mat toan bo bao dam "solvable" cua level).
        if (rng == null)
        {
            Debug.LogWarning(
                $"NPCMover.Start: '{gameObject.name}' chua duoc goi Initialize(NPCDef) " +
                "-> dung RNG khong seed (fallback). Hanh vi NPC se KHONG khop voi " +
                "BFSSolver luc generate, level co the khong con dam bao solvable!");

            rng = new System.Random();
        }

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
                stepsRemaining = rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
                break;
            case NPCType.RandomDirection:
                // SUA: truoc day dung field "maxStepsBeforeChange" (khong lien
                // quan gi toi NPCDef), khien so buoc luc choi LECH HOAN TOAN so
                // voi SimNPC.InitializeStepCounter() case 8 (dung
                // minRandomSteps/maxRandomSteps). Doi sang dung dung 2 field
                // nay de khop 100% voi mo phong ben BFSSolver.
                stepsRemaining = rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
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
        StatusEffectSystem statusSystem = StatusEffectSystem.Instance;

        // Stunned NPCs cannot move at all this turn - skip normal AI entirely.
        if (statusSystem != null && statusSystem.HasStatusEffect(this, StatusEffectType.Stun))
        {
            Debug.Log($"NPCMover.TryStepWhenPlayerMoves: '{gameObject.name}' is Stunned, skipping movement");
            return;
        }

        // Charmed NPCs override their normal AI:
        // - the ONE chosen NPC (has a stored move target) walks toward the clicked cell
        // - every OTHER NPC that got the Charm status but has no target stays frozen
        if (statusSystem != null && statusSystem.HasStatusEffect(this, StatusEffectType.Charm))
        {
            GridCell moveTarget = statusSystem.GetEffectMoveTarget(this, StatusEffectType.Charm);

            if (moveTarget != null && CanMove())
            {
                CharmStep(moveTarget);
            }
            else if (moveTarget != null && !CanMove())
            {
                // Safety net: an Idle NPC should never actually walk, even if it
                // was (incorrectly) picked as the "chosen" charmed NPC. It still
                // shows the Charm visual (handled by StatusEffectSystem) but stays put.
                Debug.Log($"NPCMover.TryStepWhenPlayerMoves: '{gameObject.name}' is Idle - showing Charm visual only, not moving");
            }
            else
            {
                Debug.Log($"NPCMover.TryStepWhenPlayerMoves: '{gameObject.name}' is Charmed (not chosen), staying frozen");
            }
            return;
        }

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

    /// <summary>
    /// Called each player turn while this NPC is the "chosen" charmed NPC.
    /// Greedily steps one tile closer to moveTarget (row first, then column,
    /// falling back to the other axis if the preferred direction is blocked).
    /// Does nothing once the NPC has already reached the target cell.
    /// </summary>
    private void CharmStep(GridCell moveTarget)
    {
        int targetR = moveTarget.row;
        int targetC = moveTarget.column;

        if (currentRow == targetR && currentColumn == targetC)
        {
            Debug.Log($"NPCMover.CharmStep: '{gameObject.name}' already at charm target ({targetR},{targetC})");
            return;
        }

        int dr = targetR - currentRow;
        int dc = targetC - currentColumn;

        int nextR = currentRow;
        int nextC = currentColumn;

        bool triedRowFirst = Mathf.Abs(dr) >= Mathf.Abs(dc);

        if (triedRowFirst && dr != 0)
            nextR = currentRow + (dr > 0 ? 1 : -1);
        else if (dc != 0)
            nextC = currentColumn + (dc > 0 ? 1 : -1);

        var nextCell = GetCell(nextR, nextC);

        // Preferred axis blocked (or no movement chosen) - try the other axis instead.
        if (nextCell == null || !nextCell.IsWalkableForNPC())
        {
            nextR = currentRow;
            nextC = currentColumn;

            if (triedRowFirst && dc != 0)
                nextC = currentColumn + (dc > 0 ? 1 : -1);
            else if (!triedRowFirst && dr != 0)
                nextR = currentRow + (dr > 0 ? 1 : -1);

            nextCell = GetCell(nextR, nextC);
        }

        if (nextCell == null || !nextCell.IsWalkableForNPC())
        {
            Debug.Log($"NPCMover.CharmStep: '{gameObject.name}' blocked on both axes this turn, staying put");
            return;
        }

        // Keep the facing direction in sync so multi-cell (length > 1) NPCs occupy
        // the correct span while being led toward the charm target.
        dirVec = new Vector2Int(nextC - currentColumn, nextR - currentRow);

        MoveTo(nextR, nextC);
    }

    private void PatrolStep(bool randomizeCountWhenReset)
    {
        if (stepsRemaining <= 0)
        {
            if (randomizeCountWhenReset)
                stepsRemaining = rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
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
            stepsRemaining = randomizeCountWhenReset ? rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1) : fixedSteps;
            return; // no move this turn
        }

        // move
        MoveTo(nextR, nextC);
        stepsRemaining--;

        // if finished configured steps, reverse direction for next patrol leg
        if (stepsRemaining <= 0)
        {
            dirVec = -dirVec;
            stepsRemaining = randomizeCountWhenReset ? rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1) : fixedSteps;
            Debug.Log($"NPCMover.PatrolStep: completed steps at ({currentRow},{currentColumn}), reversing to ({dirVec.x},{dirVec.y}), nextSteps={stepsRemaining}");
        }
    }

    private void RandomDirectionStep()
    {
        if (stepsRemaining <= 0 || dirVec == Vector2Int.zero)
        {
            // pick random direction
            dirVec = DirectionToVec((Direction)rng.Next(0, 4));
            // Dung minRandomSteps/maxRandomSteps (khop SimNPC case 8), khong
            // dung maxStepsBeforeChange nhu truoc.
            stepsRemaining = rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
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
            dirVec = DirectionToVec((Direction)rng.Next(0, 4));
            stepsRemaining = rng.Next(minRandomSteps, Mathf.Max(minRandomSteps, maxRandomSteps) + 1);
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
            InGameMainMenu inGameMenu = FindAnyObjectByType<InGameMainMenu>();
            inGameMenu?.losePanel.SetActive(true);
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