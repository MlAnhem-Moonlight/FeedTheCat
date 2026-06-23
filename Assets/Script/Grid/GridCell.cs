using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCell : MonoBehaviour, IPointerClickHandler
{
    public int row;
    public int column;

    [Header("State Sprites")]
    public Sprite emptySprite;
    //public Sprite occupiedSprite;
    public Sprite highlightSprite;

    [Header("Colors (used if sprite not provided)")]
    public Color emptyColor = Color.white;
    public Color occupiedColor = Color.gray;
    public Color highlightColor = Color.yellow;

    public bool isEmpty = true;

    // occupancy flags (separate for player and NPC)
    public bool occupiedByPlayer = false;
    public bool occupiedByNPC = false;

    public Image image;

    // event for clicks
    public event Action<GridCell> onClick;

    private Sprite originalSprite;
    private Color originalColor;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
        {
            originalSprite = image.sprite;
            originalColor = image.color;
            // Ensure this Image receives pointer events
            image.raycastTarget = true;
        }

        // initialize visuals based on isEmpty
        ApplyStateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"GridCell clicked: ({row},{column}) - isEmpty={isEmpty}");
        onClick?.Invoke(this);
    }

    public void SetOccupied(bool occupied)
    {
        // legacy: set both occupancies
        occupiedByPlayer = occupied;
        occupiedByNPC = occupied;
        ApplyStateVisual();
    }

    public void SetOccupiedByPlayer(bool occupied)
    {
        occupiedByPlayer = occupied;
        ApplyStateVisual();
    }

    public void SetOccupiedByNPC(bool occupied)
    {
        occupiedByNPC = occupied;
        ApplyStateVisual();
    }

    public void ClearOccupancy()
    {
        occupiedByPlayer = false;
        occupiedByNPC = false;
        ApplyStateVisual();
    }

    public void SetHighlight(bool highlighted)
    {
        if (image == null)
            return;

        if (highlighted)
        {
            if (highlightSprite != null)
                image.sprite = highlightSprite;
            else
                image.color = highlightColor;
        }
        else
        {
            ApplyStateVisual();
        }
    }

    private void ApplyStateVisual()
    {
        if (image == null)
            return;

        if (isEmpty)
        {
            if (emptySprite != null)
            {
                image.sprite = emptySprite;
                image.color = originalColor;
            }
            else
            {
                image.sprite = originalSprite;
                image.color = emptyColor;
            }
        }
        //else
        //{
        //    if (occupiedSprite != null)
        //    {
        //        image.sprite = occupiedSprite;
        //        image.color = originalColor;
        //    }
        //    else
        //    {
        //        image.sprite = originalSprite;
        //        image.color = occupiedColor;
        //    }
        //}
    }
}
