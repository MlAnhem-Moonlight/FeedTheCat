using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FeedTheCat;
using FeedTheCat.Items;

namespace FeedTheCat.Rewards
{
    /// <summary>
    /// RewardCard UI script that displays a single reward card.
    /// Populates the card with item icon, name, description, quantity, and rarity-based color.
    /// Should be attached to a prefab containing Image, TextMeshProUGUI, and Button components.
    /// </summary>
    public class RewardCard : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Image component to display the item icon.")]
        private Image iconImage;

        [SerializeField]
        [Tooltip("TextMeshProUGUI component to display the item name.")]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        [Tooltip("TextMeshProUGUI component to display the item description.")]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        [Tooltip("TextMeshProUGUI component to display the reward quantity (e.g., '+3').")]
        private TextMeshProUGUI quantityText;

        [SerializeField]
        [Tooltip("Image component for the rarity background/border color.")]
        private Image rarityBackground;

        [SerializeField]
        [Tooltip("Button component for selecting this reward.")]
        private Button selectButton;

        #endregion

        #region Rarity Colors

        [Header("Rarity Assets")]
        [SerializeField] private Sprite commonFrame;
        [SerializeField] private Sprite rareFrame;
        [SerializeField] private Sprite epicFrame;
        [SerializeField] private Sprite legendaryFrame;

        #endregion

        #region Private Fields

        /// <summary>
        /// The item data for this reward card.
        /// </summary>
        private ItemData itemData;

        /// <summary>
        /// The reward quantity for this card.
        /// </summary>
        private int rewardQuantity;

        /// <summary>
        /// Callback when this card is selected.
        /// </summary>
        private System.Action<RewardCard> onCardSelected;

        #endregion

        #region Properties

        /// <summary>
        /// The ItemData displayed on this card.
        /// </summary>
        public ItemData ItemData => itemData;

        /// <summary>
        /// The reward quantity displayed on this card.
        /// </summary>
        public int RewardQuantity => rewardQuantity;

        #endregion

        #region Lifecycle

        private void Start()
        {
            InitializeReferences();
        }

        /// <summary>
        /// Initialize UI component references and setup button callback.
        /// </summary>
        private void InitializeReferences()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnCardSelected);
            }
            else
            {
                Debug.LogWarning($"RewardCard: SelectButton not assigned on {gameObject.name}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Populate this card with reward data.
        /// Should be called after instantiation and before displaying.
        /// </summary>
        /// <param name="item">The ItemData to display.</param>
        /// <param name="quantity">The reward quantity for this item.</param>
        /// <param name="selectionCallback">Callback invoked when this card is selected.</param>
        public void PopulateCard(ItemData item, int quantity, System.Action<RewardCard> selectionCallback = null)
        {
            if (item == null)
            {
                Debug.LogError("RewardCard.PopulateCard: ItemData is null");
                return;
            }

            itemData = item;
            rewardQuantity = quantity;
            onCardSelected = selectionCallback;

            UpdateCardDisplay();

            Debug.Log($"RewardCard.PopulateCard: Populated card with {item.ItemName} (Qty: {quantity})");
        }


        /// <summary>
        /// Gets the frame sprite corresponding to the item's rarity.
        /// </summary>
        private Sprite GetRaritySprite()
        {
            if (itemData == null)
                return null;

            return itemData.Rarity switch
            {
                ItemRarity.Common => commonFrame,
                ItemRarity.Rare => rareFrame,
                ItemRarity.Epic => epicFrame,
                ItemRarity.Legendary => legendaryFrame,
                _ => commonFrame
            };
        }

        /// <summary>
        /// Get the rarity level as a display string.
        /// </summary>
        /// <returns>String representation of rarity (e.g., "LEGENDARY").</returns>
        public string GetRarityDisplayText()
        {
            if (itemData == null)
                return "UNKNOWN";

            return itemData.Rarity.ToString().ToUpper();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update all UI elements with card data.
        /// </summary>
        private void UpdateCardDisplay()
        {
            if (itemData == null)
                return;

            // Update icon
            if (iconImage != null)
            {
                iconImage.sprite = itemData.Icon;
                iconImage.enabled = itemData.Icon != null;
            }

            // Update name
            if (itemNameText != null)
            {
                itemNameText.text = itemData.ItemName;
            }

            // Update description
            if (descriptionText != null)
            {
                descriptionText.text = itemData.Description;
            }

            // Update quantity
            if (quantityText != null)
            {
                quantityText.text = $"+{rewardQuantity}";
            }

            // Update rarity frame
            if (rarityBackground != null)
            {
                rarityBackground.sprite = GetRaritySprite();
                rarityBackground.SetNativeSize(); // chỉ dùng nếu muốn kích thước theo sprite
            }

            // Update button interactability (optional)
            if (selectButton != null)
            {
                selectButton.interactable = true;
            }

            Debug.Log($"RewardCard.UpdateCardDisplay: Updated display for {itemData.ItemName}");
        }

        /// <summary>
        /// Called when the select button is clicked.
        /// </summary>
        private void OnCardSelected()
        {
            if (itemData == null)
            {
                Debug.LogWarning("RewardCard.OnCardSelected: ItemData is null");
                return;
            }

            Debug.Log($"RewardCard.OnCardSelected: Player selected {itemData.ItemName} (Qty: {rewardQuantity})");

            // Invoke callback
            onCardSelected?.Invoke(this);
        }

        #endregion

        #region Debug

        [ContextMenu("Log Card Info")]
        public void LogCardInfo()
        {
            if (itemData == null)
            {
                Debug.Log("RewardCard: No item data assigned");
                return;
            }

            Debug.Log($"Reward Card:\n" +
                     $"  Item: {itemData.ItemName}\n" +
                     $"  Quantity: {rewardQuantity}\n" +
                     $"  Rarity: {GetRarityDisplayText()}\n" +
                     $"  Description: {itemData.Description}");
        }

        #endregion
    }
}
