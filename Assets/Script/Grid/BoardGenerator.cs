using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BoardGenerator : MonoBehaviour
{
    [Header("Board Size")]
    public int rows = 8;
    public int columns = 8;

    [Header("References")]
    public GridLayoutGroup grid;
    public RectTransform boardRect;
    public GameObject cellPrefab;

    [Header("Spacing")]
    public Vector2 spacing = Vector2.zero;

    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (boardRect == null)
            boardRect = GetComponent<RectTransform>();

        if (grid == null || boardRect == null || cellPrefab == null)
        {
            Debug.LogError("Missing references!");
            return;
        }

        ClearChildren();

        // enforce sensible grid settings
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.spacing = spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        float availableWidth =
            boardRect.rect.width
            - grid.padding.left
            - grid.padding.right
            - spacing.x * (columns - 1);

        float availableHeight =
            boardRect.rect.height
            - grid.padding.top
            - grid.padding.bottom
            - spacing.y * (rows - 1);

        // Allow non-square cells so the grid exactly fills the boardRect
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        grid.cellSize = new Vector2(cellWidth, cellHeight);

        int totalCells = rows * columns;

        for (int i = 0; i < totalCells; i++)
        {
            GameObject cell;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cell = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(cellPrefab);
                cell.transform.SetParent(grid.transform, false);
            }
            else
#endif
            {
                cell = Instantiate(cellPrefab, grid.transform);
            }

            cell.name = $"Cell_{i}";
            // assign row/column if GridCell component exists
            var gc = cell.GetComponent<GridCell>();
                if (gc != null)
                {
                    int r = i / columns;
                    int c = i % columns;
                    gc.row = r;
                    gc.column = c;
                    // default empty state
                    gc.ClearOccupancy();
                }
            // Ensure the instantiated cell uses the correct RectTransform settings so
            // GridLayoutGroup sizing maps 1:1 to the prefab visuals.
            var rt = cell.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.pivot = new Vector2(0.5f, 0.5f);
                // use centered anchors so sizeDelta becomes the exact size in pixels
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = GetAdjustedCellSize(grid.cellSize);
            }
        }

        // Force layout rebuild so GridLayoutGroup arranges children immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());
    }

    [ContextMenu("Update Cell Size")]
    public void UpdateCellSize()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (boardRect == null)
            boardRect = GetComponent<RectTransform>();

        float availableWidth =
            boardRect.rect.width
            - grid.padding.left
            - grid.padding.right
            - spacing.x * (columns - 1);

        float availableHeight =
            boardRect.rect.height
            - grid.padding.top
            - grid.padding.bottom
            - spacing.y * (rows - 1);

        // Allow non-square cells to fully fill the available area
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        // Update existing children so their RectTransforms match the new cell size
        for (int i = 0; i < grid.transform.childCount; i++)
        {
            var child = grid.transform.GetChild(i).gameObject;
            var rt = child.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = GetAdjustedCellSize(grid.cellSize);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());
    }

    void ClearChildren()
    {
#if UNITY_EDITOR
        while (grid.transform.childCount > 0)
        {
            DestroyImmediate(grid.transform.GetChild(0).gameObject);
        }
#else
    while (grid.transform.childCount > 0)
    {
        Destroy(grid.transform.GetChild(0).gameObject);
    }
#endif
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying)
            UpdateCellSize();
    }

    // Compensate for any parent transform scaling so sizeDelta maps visually 1:1
    private Vector2 GetAdjustedCellSize(Vector2 desiredSize)
    {
        if (grid == null)
            return desiredSize;

        var gridRect = grid.GetComponent<RectTransform>();
        if (gridRect == null)
            return desiredSize;

        Vector3 lossy = gridRect.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;

        return new Vector2(desiredSize.x / sx, desiredSize.y / sy);
    }
}