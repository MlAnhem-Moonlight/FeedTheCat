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
    [Header("Destination")]
    [Tooltip("Mark this cell as the destination (player can enter, NPCs cannot)")]
    public bool isDestination = false;
    public Sprite destinationSprite;
    public Color destinationColor = Color.green;

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
        // update legacy empty flag
        isEmpty = !(occupiedByPlayer || occupiedByNPC);
        ApplyStateVisual();
    }

    public void SetOccupiedByPlayer(bool occupied)
    {
        occupiedByPlayer = occupied;
        // update legacy flag
        isEmpty = !(occupiedByPlayer || occupiedByNPC);
        ApplyStateVisual();

        // win check: player entered destination
        if (occupiedByPlayer && isDestination)
        {
            Debug.Log("Player reached destination - WIN");
        }
    }

    public void SetOccupiedByNPC(bool occupied)
    {
        occupiedByNPC = occupied;
        // update legacy flag
        isEmpty = !(occupiedByPlayer || occupiedByNPC);
        ApplyStateVisual();
    }

    public void ClearOccupancy()
    {
        occupiedByPlayer = false;
        occupiedByNPC = false;
        ApplyStateVisual();
    }

    // Public helper to set destination flag and refresh visuals
    public void SetDestination(bool dest)
    {
        isDestination = dest;
        ApplyStateVisual();
    }

    // Determine if an NPC can enter this cell.
    // NPCs cannot enter destination cells or cells occupied by other NPCs.
    // Allow NPCs to enter cells occupied by the player (to enable collisions/interaction).
    public bool IsWalkableForNPC()
    {
        return !occupiedByNPC && !isDestination;
    }

    // Player walkability: player cannot enter cells occupied by NPCs or other players.
    // Destination is allowed for player.
    public bool IsEmptyForPlayer()
    {
        return !occupiedByNPC && !occupiedByPlayer;
    }

    public void SetHighlight(bool highlighted)
    {
        if (image == null)
            return;

        if (highlighted)
        {
            // If this cell is a destination, do not override its sprite; only tint it
            if (isDestination)
            {
                image.color = highlightColor;
            }
            else
            {
                if (highlightSprite != null)
                    image.sprite = highlightSprite;
                else
                    image.color = highlightColor;
            }
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
        if (isDestination)
        {
            if (destinationSprite != null)
            {
                image.sprite = destinationSprite;
            }
            else
            {
                image.sprite = originalSprite;
            }
            // always apply destination color so it's visible even when a sprite is set
            image.color = destinationColor;
            return;
        }

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
