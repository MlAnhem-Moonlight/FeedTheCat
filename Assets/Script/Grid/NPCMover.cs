using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCMover : MonoBehaviour
{
    public enum NPCType { PatrolFixed, PatrolRandomSteps, RandomDirection }
    public enum Direction { Up, Down, Left, Right }

    [Header("References")]
    public BoardGenerator boardGenerator; // reference to board

    [Header("Initial Position")]
    public int startRow = 0;
    public int startColumn = 0;

    [Header("Behavior")]
    public NPCType type = NPCType.PatrolFixed;
    public Direction initialDirection = Direction.Right;

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

    //private RectTransform entityContainer;
    private RectTransform rectTransform;
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

    private void Start()
    {
        if (boardGenerator == null)
            boardGenerator = FindAnyObjectByType<BoardGenerator>();

        // set starting direction
        dirVec = DirectionToVec(initialDirection);

        // initial steps
        ResetStepsForType();

        // place NPC at start
        InitializeAndPlace();
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

    private void InitializeAndPlace()
    {
        currentRow = Mathf.Clamp(startRow, 0, Math.Max(0, boardGenerator.rows - 1));
        currentColumn = Mathf.Clamp(startColumn, 0, Math.Max(0, boardGenerator.columns - 1));
        // create entity container (sibling to boardRect) if needed
        if (boardGenerator != null)
        {
            Transform parentForContainer = boardGenerator.boardRect != null ? boardGenerator.boardRect.parent : boardGenerator.transform;
            var go = new GameObject("NPCContainer", typeof(RectTransform));

            RectTransform goRt = go.GetComponent<RectTransform>();
            // align with boardRect if available: copy anchors/pivot/size and positions so container sits exactly over the board
            if (boardGenerator != null && boardGenerator.boardRect != null)
            {
                var br = boardGenerator.boardRect;
                goRt.anchorMin = br.anchorMin;
                goRt.anchorMax = br.anchorMax;
                goRt.pivot = br.pivot;
                goRt.sizeDelta = br.sizeDelta;
                goRt.localScale = Vector3.one;
                // copy anchored/local positions & rotation to match board exactly
                goRt.anchoredPosition = br.anchoredPosition;
                goRt.localRotation = br.localRotation;
                goRt.localPosition = br.localPosition;
            }
            else
            {
                // default stretch behavior if no board rect
                goRt.anchorMin = Vector2.zero;
                goRt.anchorMax = Vector2.one;
                goRt.sizeDelta = Vector2.zero;
                goRt.anchoredPosition = Vector2.zero;
                goRt.pivot = new Vector2(0.5f, 0.5f);
            }

            //entityContainer = goRt;
            //if (parentForContainer != null)
            //    entityContainer.SetParent(parentForContainer, false);
            //else
            //    entityContainer.SetParent(boardGenerator.transform, false);

            //// place container above board in hierarchy so visuals render on top
            //entityContainer.SetAsLastSibling();
            //entityContainer.localScale = Vector3.one;
        }

        //// ensure initial occupancy
        //var startCell = GetCell(currentRow, currentColumn);
        //if (startCell != null)
        //    startCell.SetOccupied(true);

        //// parent rectTransform under entityContainer for UI and position
        //if (rectTransform != null && entityContainer != null)
        //{
        //    rectTransform.SetParent(entityContainer, false);
        //    MoveToCellVisual(startCell);
        //}
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

        var nextCell = GetCell(nextR, nextC);
        if (nextCell == null || !nextCell.isEmpty)
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
        if (nextCell == null || !nextCell.isEmpty)
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
        var currentCell = GetCell(currentRow, currentColumn);
        var target = GetCell(r, c);
        if (target == null || !target.isEmpty)
            return;

        if (currentCell != null)
            currentCell.SetOccupiedByNPC(false);
        target.SetOccupiedByNPC(true);

        currentRow = r;
        currentColumn = c;

        MoveToCellVisual(target);
        // debug collision check with player RectTransform using CollisionDebug
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null && pc.playerObject != null)
        {
            var playerRt = pc.playerObject.GetComponent<RectTransform>();
            if (playerRt != null && rectTransform != null)
            {
                if (CollisionDebug.RectsOverlap(rectTransform, playerRt))
                {
                    Debug.Log($"NPCMover: NPC at ({r},{c}) COLLIDES with player at ({pc.startRow},{pc.startColumn})");
                }
            }
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