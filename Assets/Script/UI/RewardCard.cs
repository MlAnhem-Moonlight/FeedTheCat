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

        [Header("Rarity Colors")]
        [SerializeField]
        [Tooltip("Color for Common rarity items.")]
        private Color commonColor = Color.gray;

        [SerializeField]
        [Tooltip("Color for Rare rarity items.")]
        private Color rareColor = Color.cyan;

        [SerializeField]
        [Tooltip("Color for Epic rarity items.")]
        private Color epicColor = new Color(0.6f, 0.2f, 1f); // Purple

        [SerializeField]
        [Tooltip("Color for Legendary rarity items.")]
        private Color legendaryColor = Color.yellow;

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
        /// Get the rarity color based on item rarity.
        /// </summary>
        /// <returns>The Color corresponding to the item's rarity.</returns>
        public Color GetRarityColor()
        {
            if (itemData == null)
                return Color.white;

            return itemData.Rarity switch
            {
                ItemRarity.Common => commonColor,
                ItemRarity.Rare => rareColor,
                ItemRarity.Epic => epicColor,
                ItemRarity.Legendary => legendaryColor,
                _ => Color.white
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

            // Update rarity color
            Color rarityColor = GetRarityColor();
            if (rarityBackground != null)
            {
                rarityBackground.color = rarityColor;
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
