using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Abstract base class for all consumable items.
    /// Handles drag-drop mechanics, quantity management, and effect execution.
    /// Automatically manages UI (icon, quantity text, state).
    /// Derive from this class to create specific item implementations.
    /// </summary>
    public abstract class ItemBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Serialized Fields

        [Header("Item Configuration")]
        [SerializeField]
        protected ItemData itemData;

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Image component for item icon (will auto-assign if empty)")]
        protected Image iconImage;

        [SerializeField]
        [Tooltip("TextMeshProUGUI to display quantity (will auto-find if empty)")]
        protected TextMeshProUGUI quantityText;

        [SerializeField]
        [Tooltip("CanvasGroup for managing state (interactable, alpha, blocks raycasts)")]
        protected CanvasGroup canvasGroup;

        #endregion

        #region Cached References

        protected RectTransform rectTransform;
        protected ItemInventory itemInventory;
        protected GridCell targetCell;

        // The toolbar icon (this.rectTransform) NEVER moves or gets reparented anymore.
        // Instead, a lightweight visual-only "ghost" is spawned on the root canvas and
        // is the thing that actually follows the pointer while dragging. This guarantees
        // the item bar slot is never emptied/removed - only the ghost clone is discarded
        // (destroyed) once the drag ends, whether the drop succeeded or not.
        private RectTransform dragGhost;

        /// <summary>
        /// The topmost Canvas in this item's hierarchy. Used to reparent the
        /// item while dragging so parent Layout Groups / masks don't fight
        /// the manual position updates, and so the item renders above everything.
        /// </summary>
        protected Canvas rootCanvas;

        /// <summary>
        /// GridCell currently highlighted because the pointer is hovering it while dragging.
        /// </summary>
        protected GridCell hoveredCell;

        #endregion

        #region Properties

        /// <summary>
        /// The item data asset defining this item's properties.
        /// </summary>
        public ItemData ItemData => itemData;

        /// <summary>
        /// Unique identifier for this item.
        /// </summary>
        public string ItemID => itemData?.ItemID ?? "unknown";

        /// <summary>
        /// Current quantity of this item in inventory.
        /// </summary>
        public int CurrentQuantity
        {
            get
            {
                if (itemInventory == null)
                    itemInventory = ItemInventory.Instance;

                return itemInventory != null ? itemInventory.GetQuantity(ItemID) : 0;
            }
        }

        /// <summary>
        /// Returns true if this item can be used (quantity > 0 and not on cooldown).
        /// </summary>
        public virtual bool CanUse()
        {
            if (itemData == null)
                return false;

            if (itemData.ItemType == ItemType.Passive)
                return false;

            // CRITICAL: Block usage if quantity is 0
            if (CurrentQuantity <= 0)
                return false;

            return !IsOnCooldown();
        }

        /// <summary>
        /// Returns true if this item is on cooldown.
        /// </summary>
        protected virtual bool IsOnCooldown()
        {
            return false;
        }

        #endregion

        #region Lifecycle

        protected virtual void Start()
        {
            InitializeReferences();
            SetupUI();
            SubscribeToInventory();
            UpdateUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInventory();
        }

        /// <summary>
        /// Initialize cached references (one-time only).
        /// </summary>
        protected virtual void InitializeReferences()
        {
            if (itemData == null)
                Debug.LogError($"ItemBase: ItemData not assigned on {gameObject.name}");

            // Cache references to avoid GetComponent calls during gameplay
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                Debug.LogError($"ItemBase: RectTransform not found on {gameObject.name}");

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                Debug.LogError($"ItemBase: CanvasGroup not found on {gameObject.name}");

            itemInventory = ItemInventory.Instance;
            if (itemInventory == null)
                Debug.LogError("ItemBase: ItemInventory.Instance not found in scene");

            // Cache the ROOT canvas (not just the nearest one). We reparent the
            // item to this during drag so it renders above every other UI
            // (including the board's GridCells, which may live under a
            // different sub-canvas) and so parent Layout Groups don't reset
            // its position while dragging.
            Canvas nearestCanvas = rectTransform?.GetComponentInParent<Canvas>();
            rootCanvas = nearestCanvas != null ? nearestCanvas.rootCanvas : null;

            if (rootCanvas == null)
                Debug.LogError($"ItemBase: Could not find a root Canvas for {gameObject.name}");
        }

        /// <summary>
        /// Setup UI components (icon, quantity text).
        /// </summary>
        private void SetupUI()
        {
            if (itemData == null)
                return;

            // Auto-assign Image if not set
            if (iconImage == null)
                iconImage = GetComponent<Image>();

            // Assign sprite from ItemData
            if (iconImage != null && itemData.Icon != null)
                iconImage.sprite = itemData.Icon;

            // Auto-find TextMeshProUGUI if not set
            if (quantityText == null)
                quantityText = GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>
        /// Subscribe to inventory changes to auto-update UI.
        /// </summary>
        private void SubscribeToInventory()
        {
            if (itemInventory == null)
                return;

            itemInventory.OnQuantityChanged += HandleQuantityChanged;
        }

        /// <summary>
        /// Unsubscribe from inventory changes.
        /// </summary>
        private void UnsubscribeFromInventory()
        {
            if (itemInventory == null)
                return;

            itemInventory.OnQuantityChanged -= HandleQuantityChanged;
        }

        /// <summary>
        /// Called whenever any item quantity changes in inventory.
        /// </summary>
        private void HandleQuantityChanged(string changedItemID, int newQuantity, int previousQuantity)
        {
            // Only update if this is our item
            if (changedItemID != ItemID)
                return;

            UpdateUI();
        }

        #endregion

        #region Drag-Drop Implementation

        /// <summary>
        /// Called when drag begins. Store original position/parent/sibling index,
        /// disable raycast blocking on self, and reparent to the root Canvas so
        /// the drag isn't fought by parent Layout Groups and renders on top.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemData == null || itemData.ItemType != ItemType.Active)
                return;

            if (!CanUse())
                return;

            if (rectTransform == null || canvasGroup == null)
                return;

            // IMPORTANT: the toolbar icon itself (this GameObject) is left completely
            // untouched - it is not reparented, moved, hidden, or destroyed. We only
            // spawn a separate, visual-only "ghost" clone that follows the pointer.
            if (rootCanvas != null && iconImage != null)
            {
                var ghostGo = new GameObject($"DragGhost_{ItemID}", typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));

                var ghostImage = ghostGo.GetComponent<UnityEngine.UI.Image>();
                ghostImage.sprite = iconImage.sprite;
                ghostImage.raycastTarget = false; // never blocks raycasts to GridCells beneath it

                var ghostCanvasGroup = ghostGo.GetComponent<CanvasGroup>();
                ghostCanvasGroup.alpha = 0.85f;
                ghostCanvasGroup.blocksRaycasts = false;
                ghostCanvasGroup.interactable = false;

                var ghostRect = ghostGo.GetComponent<RectTransform>();
                ghostRect.SetParent(rootCanvas.transform, false);
                ghostRect.sizeDelta = rectTransform.sizeDelta;
                ghostRect.position = rectTransform.position;
                ghostRect.SetAsLastSibling();

                dragGhost = ghostRect;
            }

            LogFilter.LogItem($"ItemBase.OnBeginDrag: Started dragging {ItemData.ItemName} (id={ItemID})");
        }

        /// <summary>
        /// Called while dragging. Moves the drag ghost (not the toolbar icon) to follow
        /// the cursor and highlights whichever GridCell is currently under the pointer.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost == null)
                return;

            dragGhost.position += (Vector3)eventData.delta;

            UpdateHoverHighlight(eventData);
        }

        /// <summary>
        /// Called when drag ends. Validate target and apply effect or revert position.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (rectTransform == null || canvasGroup == null)
                return;

            // Clear any leftover hover highlight now that the drag is finishing
            ClearHoverHighlight();

            // The toolbar icon never moved, so there is nothing to "revert" - we only
            // ever need to get rid of the ghost clone that followed the pointer.
            GridCell targetGridCell = GetGridCellUnderPointer(eventData);

            if (targetGridCell == null)
            {
                DestroyDragGhost();
                LogFilter.LogItemWarning($"ItemBase.OnEndDrag: No valid GridCell target found. (ItemID={ItemID})");
                return;
            }

            targetCell = targetGridCell;

            if (!ValidateTarget(targetCell))
            {
                DestroyDragGhost();
                LogFilter.LogItemWarning($"ItemBase.OnEndDrag: Target validation failed for {targetCell.gameObject.name} (ItemID={ItemID})");
                return;
            }

            ExecuteEffect(targetCell);
            SpawnItemVisualOnGrid(targetCell);
            DecreaseQuantity();

            if (itemInventory != null)
                itemInventory.Save();

            // The toolbar slot/icon was never removed or moved, so nothing needs to be
            // "put back" - just clean up the ghost that was following the pointer.
            DestroyDragGhost();

            LogFilter.LogItem($"ItemBase.OnEndDrag: Item effect executed on {targetCell.gameObject.name}. New quantity: {CurrentQuantity} (ItemID={ItemID})");
        }

        /// <summary>
        /// Destroy the drag ghost created in OnBeginDrag, if any. Called whether the
        /// drop succeeded or was cancelled - the toolbar icon itself is never affected.
        /// </summary>
        private void DestroyDragGhost()
        {
            if (dragGhost != null)
            {
                Object.Destroy(dragGhost.gameObject);
                dragGhost = null;
            }
        }

        /// <summary>
        /// Raycast under the pointer across ALL active raycasters/canvases (not just
        /// the item's own canvas) and return the first GridCell hit, if any.
        /// Using EventSystem.RaycastAll makes this robust even if the item toolbar
        /// and the game board live under different Canvas objects.
        /// </summary>
        private GridCell GetGridCellUnderPointer(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return null;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = eventData.position
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject == gameObject)
                    continue;

                GridCell cell = result.gameObject.GetComponent<GridCell>();
                if (cell != null)
                    return cell;
            }

            return null;
        }

        /// <summary>
        /// Update which GridCell (if any) is highlighted based on the current pointer position.
        /// </summary>
        private void UpdateHoverHighlight(PointerEventData eventData)
        {
            GridCell cellUnderPointer = GetGridCellUnderPointer(eventData);

            if (cellUnderPointer == hoveredCell)
                return;

            if (hoveredCell != null)
                hoveredCell.SetHighlight(false);

            if (cellUnderPointer != null && ValidateTarget(cellUnderPointer))
                cellUnderPointer.SetHighlight(true);

            hoveredCell = cellUnderPointer;
        }

        /// <summary>
        /// Turn off highlight on whichever cell is currently hovered, if any.
        /// </summary>
        private void ClearHoverHighlight()
        {
            if (hoveredCell != null)
                hoveredCell.SetHighlight(false);

            hoveredCell = null;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Validates whether the target cell is valid for this item's effect.
        /// </summary>
        protected abstract bool ValidateTarget(GridCell target);

        /// <summary>
        /// Execute the item's effect on the target cell.
        /// </summary>
        protected abstract void ExecuteEffect(GridCell target);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Decrease item quantity by 1.
        /// Calls inventory which will trigger OnQuantityChanged event.
        /// </summary>
        protected virtual void DecreaseQuantity()
        {
            if (itemInventory == null)
                return;

            int currentQty = itemInventory.GetQuantity(ItemID);
            if (currentQty > 0)
            {
                itemInventory.DecreaseItem(ItemID, 1);
                // UI update happens automatically via OnQuantityChanged event
            }
        }

        /// <summary>
        /// Update UI to reflect current quantity and state.
        /// Called automatically when quantity changes.
        /// </summary>
        protected virtual void UpdateUI()
        {
            // Update interactability based on item state
            if (canvasGroup != null)
            {
                bool canUse = CanUse();
                canvasGroup.interactable = canUse;

                // Dim button if can't use
                if (iconImage != null)
                    iconImage.color = canUse ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // Update quantity text
            if (quantityText != null)
            {
                int qty = CurrentQuantity;
                quantityText.text = qty > 0 ? qty.ToString() : "0";
                quantityText.color = qty > 0 ? Color.white : Color.gray;
            }
        }

        /// <summary>
        /// Spawns this item's visual on the GridCell it was dropped on, so the
        /// player can see the item that was used sitting on the board.
        /// Each item can define its own unique prefab via ItemData.VisualPrefab
        /// (e.g. a bomb model for a Stun item, a heart model for a Charm item).
        /// The visual auto-destroys in sync with the item's EffectDuration (it
        /// disappears on the same turn the underlying status effect expires).
        /// Override in derived classes if a specific item needs custom placement logic.
        /// </summary>
        protected virtual void SpawnItemVisualOnGrid(GridCell targetCell)
        {
            if (targetCell == null || itemData == null)
                return;

            GameObject visualGo;

            if (itemData.VisualPrefab != null)
            {
                // Preferred path: each item has its own dedicated prefab (model/animation/particles/etc).
                visualGo = Object.Instantiate(itemData.VisualPrefab, targetCell.transform);
            }
            else if (itemData.Icon != null)
            {
                // Fallback: no dedicated prefab assigned yet, just show the item's icon.
                LogFilter.LogItemWarning($"ItemBase.SpawnItemVisualOnGrid: No VisualPrefab assigned for '{ItemData.ItemName}' (ItemID={ItemID}), falling back to icon sprite.");

                visualGo = new GameObject($"ItemVisual_{ItemID}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var img = visualGo.GetComponent<UnityEngine.UI.Image>();
                img.sprite = itemData.Icon;
                img.raycastTarget = false;
            }
            else
            {
                LogFilter.LogItemWarning($"ItemBase.SpawnItemVisualOnGrid: No VisualPrefab or Icon assigned for '{ItemID}', nothing to show on grid.");
                return;
            }

            // Center the visual on the cell it landed on.
            var rt = visualGo.GetComponent<RectTransform>();
            if (rt == null)
                rt = visualGo.AddComponent<RectTransform>();

            rt.SetParent(targetCell.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            // Match the prefab's size to the GridCell it's placed on, so it visually fits the tile.
            var cellRect = targetCell.GetComponent<RectTransform>();
            rt.sizeDelta = cellRect != null ? cellRect.rect.size : new Vector2(64, 64);

            visualGo.name = $"ItemVisual_{ItemID}";

            // Auto-destroy the visual in sync with the item's effect duration (in player turns).
            // Items with EffectDuration <= 0 keep their visual on the board permanently.
            var lifetime = visualGo.GetComponent<ItemVisualEffect>();
            if (lifetime == null)
                lifetime = visualGo.AddComponent<ItemVisualEffect>();
            lifetime.Initialize(itemData.EffectDuration);

            LogFilter.LogItem($"ItemBase.SpawnItemVisualOnGrid: Spawned visual for '{ItemData.ItemName}' on {targetCell.gameObject.name} (ItemID={ItemID}, duration={itemData.EffectDuration} turns)");
        }

        #endregion

        #region Debug

        [ContextMenu("Log Item Info")]
        public void LogItemInfo()
        {
            if (itemData == null)
                return;

            Debug.Log($"Item: {ItemData.ItemName}\n" +
                     $"ID: {ItemID}\n" +
                     $"Current Quantity: {CurrentQuantity}\n" +
                     $"Can Use: {CanUse()}\n" +
                     $"Rarity: {ItemData.Rarity}\n" +
                     $"Effect Radius: {ItemData.EffectRadius}");
        }

        #endregion
    }
}