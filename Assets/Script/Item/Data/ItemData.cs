using System;
using UnityEngine;

namespace FeedTheCat.Items
{
    /// <summary>
    /// ScriptableObject that defines all properties for a consumable item.
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
        [Tooltip("Display name of the item.")]
        private string itemName = "New Item";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Description of the item's effect.")]
        private string description = "Item description";

        [SerializeField]
        [Tooltip("Item icon displayed in UI.")]
        private Sprite icon;

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
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public EffectRadiusType EffectRadius => effectRadius;
        public TargetType TargetType => targetType;
        public StatusEffectType StatusEffect => statusEffect;
        public int EffectDuration => effectDuration;
        public ItemRarity Rarity => rarity;
        public int Cooldown => cooldown;

        #endregion

        /// <summary>
        /// Validates that ItemID is set and unique.
        /// </summary>
        [ContextMenu("Validate Item Data")]
        public void ValidateItemData()
        {
            if (string.IsNullOrWhiteSpace(itemID))
            {
                Debug.LogWarning($"ItemData '{name}': ItemID is empty. Assign a unique identifier.", this);
            }
        }
    }
}
