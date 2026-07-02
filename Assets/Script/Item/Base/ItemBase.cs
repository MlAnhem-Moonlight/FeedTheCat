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
        protected Vector3 originalPosition;
        protected Transform originalParent;
        protected GraphicRaycaster graphicRaycaster;

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

            return CurrentQuantity > 0 && !IsOnCooldown();
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

            // Cache the GraphicRaycaster
            Canvas canvas = rectTransform?.GetComponentInParent<Canvas>();
            if (canvas != null)
                graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
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
        /// Called when drag begins. Store original position and disable raycast blocking.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemData == null || itemData.ItemType != ItemType.Active)
                return;

            if (!CanUse())
                return;

            if (rectTransform == null || canvasGroup == null)
                return;

            originalPosition = rectTransform.position;
            originalParent = rectTransform.parent;

            // Fade out during drag
            canvasGroup.alpha = 0.7f;

            // Disable raycasts on this item to allow raycast through to GridCell
            canvasGroup.blocksRaycasts = false;

            rectTransform.SetAsLastSibling();

            Debug.Log($"ItemBase.OnBeginDrag: Started dragging {ItemData.ItemName}");
        }

        /// <summary>
        /// Called while dragging. Update item position to follow cursor.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (rectTransform == null)
                return;

            rectTransform.position += (Vector3)eventData.delta;
        }

        /// <summary>
        /// Called when drag ends. Validate target and apply effect or revert position.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (rectTransform == null || canvasGroup == null)
                return;

            // Re-enable raycasts
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (graphicRaycaster == null)
            {
                RevertPosition();
                return;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerData, results);

            GridCell targetGridCell = null;

            foreach (RaycastResult result in results)
            {
                if (result.gameObject == gameObject)
                    continue;

                targetGridCell = result.gameObject.GetComponent<GridCell>();
                if (targetGridCell != null)
                    break;
            }

            if (targetGridCell == null)
            {
                RevertPosition();
                Debug.Log($"ItemBase.OnEndDrag: No valid GridCell target found. Reverting position.");
                return;
            }

            targetCell = targetGridCell;

            if (!ValidateTarget(targetCell))
            {
                RevertPosition();
                Debug.Log($"ItemBase.OnEndDrag: Target validation failed for {targetCell.gameObject.name}");
                return;
            }

            ExecuteEffect(targetCell);
            DecreaseQuantity();

            if (itemInventory != null)
                itemInventory.Save();

            Debug.Log($"ItemBase.OnEndDrag: Item effect executed on {targetCell.gameObject.name}. New quantity: {CurrentQuantity}");
        }

        /// <summary>
        /// Revert item to original position after failed drag.
        /// </summary>
        private void RevertPosition()
        {
            if (rectTransform == null)
                return;

            rectTransform.position = originalPosition;
            if (originalParent != null)
                rectTransform.SetParent(originalParent);
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
        /// Spawns a visual preview at the target cell.
        /// Override in derived classes for custom previews.
        /// </summary>
        protected virtual void SpawnPreview(GridCell targetCell)
        {
            Debug.Log($"ItemBase.SpawnPreview: Spawning preview for {ItemData.ItemName} at {targetCell.gameObject.name}");
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
