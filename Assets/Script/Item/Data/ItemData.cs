using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Stores localized text in multiple languages.
    /// Serializable so it can be embedded in ScriptableObjects like ItemData.
    /// Language is now driven by Unity Localization's LocalizationSettings.SelectedLocale
    /// (not Application.systemLanguage), so it follows whatever language the
    /// player picks in-game, not just the OS language at startup.
    /// </summary>
    [System.Serializable]
    public class LocalizedString
    {
        [TextArea(2, 4)]
        [SerializeField]
        [Tooltip("English version of the text")]
        public string english = "";

        [TextArea(2, 4)]
        [SerializeField]
        [Tooltip("Vietnamese version of the text")]
        public string vietnamese = "";

        /// <summary>
        /// Get the text based on the player's currently selected language
        /// (UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale).
        /// Falls back to English if localization isn't initialized yet or the
        /// selected locale isn't one we have a translation for.
        /// </summary>
        public string GetLocalizedText()
        {
            Locale selectedLocale = LocalizationSettings.HasSettings ? LocalizationSettings.SelectedLocale : null;
            string code = selectedLocale != null ? selectedLocale.Identifier.Code : "en";
            return GetText(code);
        }

        /// <summary>
        /// Get text in a specific language. Accepts full locale codes
        /// (e.g. "en-US", "vi-VN") as well as short codes ("en", "vi").
        /// </summary>
        public string GetText(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return english;

            string code = languageCode.ToLowerInvariant();

            if (code.StartsWith("vi"))
                return vietnamese;

            return english;
        }

        /// <summary>
        /// Constructor for convenience.
        /// </summary>
        public LocalizedString(string en = "", string vi = "")
        {
            english = en;
            vietnamese = vi;
        }

        /// <summary>
        /// Check if all language variants are filled.
        /// </summary>
        public bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(english) && !string.IsNullOrWhiteSpace(vietnamese);
        }

        public override string ToString()
        {
            return GetLocalizedText();
        }
    }

    /// <summary>
    /// ScriptableObject that defines all properties for a consumable item.
    /// Supports localized name and description in multiple languages.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "FeedTheCat/Item/ItemData")]
    public class ItemData : ScriptableObject
    {
        #region Core Properties

        [Header("Identity")]
        [SerializeField]
        [Tooltip("Unique identifier for this item. Used for persistence and lookups.")]
        private string itemID = "item_placeholder";

        [SerializeField]
        [Tooltip("Display name of the item (localized in multiple languages).")]
        private LocalizedString itemName = new LocalizedString("New Item", "Item mới");

        [SerializeField]
        //[TextArea(2, 4)]
        [Tooltip("Description of the item's effect (localized in multiple languages).")]
        private LocalizedString description = new LocalizedString("Item description", "Mô tả item");

        [SerializeField]
        [Tooltip("Item icon displayed in UI.")]
        private Sprite icon;

        [SerializeField]
        [Tooltip("Prefab instantiated on the GridCell when this item is dropped, so each item can show its own unique visual on the board (e.g. a bomb model for Stun, a heart model for Charm). Leave empty to fall back to a generic icon-based visual.")]
        private GameObject visualPrefab;

        #endregion

        #region Effect Properties

        [Header("Effect Properties")]
        [SerializeField]
        [Tooltip("Type of this item (Active = usable, Passive = automatic).")]
        private ItemType itemType = ItemType.Active;

        [SerializeField]
        [Tooltip("The radius within which this item affects NPCs.")]
        private EffectRadiusType effectRadius = EffectRadiusType.Radius2;

        [SerializeField]
        [Tooltip("Type of target this item affects.")]
        private TargetType targetType = TargetType.NPC;

        [SerializeField]
        [Tooltip("Status effect applied by this item (None for passive items).")]
        private StatusEffectType statusEffect = StatusEffectType.None;

        [SerializeField]
        [Range(0, 10)]
        [Tooltip("Duration of status effect in player turns.")]
        private int effectDuration = 2;

        #endregion

        #region Rarity & Cooldown

        [Header("Rarity & Cooldown")]
        [SerializeField]
        [Tooltip("Rarity level of this item.")]
        private ItemRarity rarity = ItemRarity.Common;

        [SerializeField]
        [Range(0, 5)]
        [Tooltip("Cooldown in turns before item can be used again (0 = no cooldown).")]
        private int cooldown = 0;

        #endregion

        #region Properties (Getters)

        public string ItemID => itemID;

        /// <summary>
        /// Get item name in current language (based on system language).
        /// </summary>
        public string ItemName => itemName.GetLocalizedText();

        /// <summary>
        /// Get item name in specific language.
        /// </summary>
        public string GetItemName(string languageCode) => itemName.GetText(languageCode);

        /// <summary>
        /// Get description in current language (based on system language).
        /// </summary>
        public string Description => description.GetLocalizedText();

        /// <summary>
        /// Get description in specific language.
        /// </summary>
        public string GetDescription(string languageCode) => description.GetText(languageCode);

        public Sprite Icon => icon;
        public GameObject VisualPrefab => visualPrefab;
        public ItemType ItemType => itemType;
        public EffectRadiusType EffectRadius => effectRadius;
        public TargetType TargetType => targetType;
        public StatusEffectType StatusEffect => statusEffect;
        public int EffectDuration => effectDuration;
        public ItemRarity Rarity => rarity;
        public int Cooldown => cooldown;

        #endregion

        /// <summary>
        /// Validates that ItemID is set and localized strings are complete.
        /// </summary>
        [ContextMenu("Validate Item Data")]
        public void ValidateItemData()
        {
            if (string.IsNullOrWhiteSpace(itemID))
            {
                Debug.LogWarning($"ItemData '{name}': ItemID is empty. Assign a unique identifier.", this);
            }

            if (itemName == null || !itemName.IsComplete())
            {
                Debug.LogWarning($"ItemData '{name}': Item name is not complete in all languages.", this);
            }

            if (description == null || !description.IsComplete())
            {
                Debug.LogWarning($"ItemData '{name}': Description is not complete in all languages.", this);
            }
        }
    }
}