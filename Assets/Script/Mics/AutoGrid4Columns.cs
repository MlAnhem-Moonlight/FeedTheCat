using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGrid4Columns : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 4;

    public float paddingLeft = 25;
    public float paddingRight = 25;

    public float spacingX = 10;
    public float spacingY = 15;

    public bool squareCells = true;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();

        ApplyLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        ApplyLayout();
    }
#endif

    private void ApplyLayout()
    {
        if (grid == null || rectTransform == null)
            return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        grid.padding.left = Mathf.RoundToInt(paddingLeft);
        grid.padding.right = Mathf.RoundToInt(paddingRight);

        grid.spacing = new Vector2(spacingX, spacingY);

        float availableWidth =
            rectTransform.rect.width
            - paddingLeft
            - paddingRight
            - spacingX * (columns - 1);

        float cellWidth = availableWidth / columns;

        if (squareCells)
            grid.cellSize = new Vector2(cellWidth, cellWidth);
        else
            grid.cellSize = new Vector2(cellWidth, grid.cellSize.y);
    }
}