using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Displays an item's localized name and description (e.g. a detail panel
    /// or tooltip shown on hover/select/drag-start of a toolbar item).
    ///
    /// Automatically re-renders the currently shown item whenever the player
    /// changes the game's language via Unity Localization
    /// (LocalizationSettings.SelectedLocale), so the text updates immediately
    /// without needing to re-select the item.
    /// </summary>
    public class ItemDetailUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField]
        [Tooltip("TextMeshProUGUI showing the item's localized name.")]
        private TextMeshProUGUI nameText;

        [SerializeField]
        [Tooltip("TextMeshProUGUI showing the item's localized description.")]
        private TextMeshProUGUI descriptionText;

        #endregion

        #region Fields

        /// <summary>
        /// The item currently being displayed. Null when the panel is hidden/cleared.
        /// </summary>
        private ItemData currentItemData;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            // Re-render whenever the player switches language while this panel is active.
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show (or refresh) the detail panel for the given item.
        /// Call this from wherever the player selects/hovers/drags an item
        /// (e.g. ItemBase.OnBeginDrag, or a pointer-enter handler on the toolbar icon).
        /// </summary>
        public void ShowItem(ItemData itemData)
        {
            currentItemData = itemData;
            Refresh();
        }

        /// <summary>
        /// Clear the panel (e.g. when nothing is selected/hovered).
        /// </summary>
        public void Clear()
        {
            currentItemData = null;

            if (nameText != null)
                nameText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;
        }

        #endregion

        #region Localization

        /// <summary>
        /// Called by Unity Localization whenever LocalizationSettings.SelectedLocale changes.
        /// </summary>
        private void HandleLocaleChanged(Locale newLocale)
        {
            // Nothing shown right now - no need to touch the UI.
            if (currentItemData == null)
                return;

            Refresh();
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Re-pull the localized name/description from ItemData and push them into the UI.
        /// ItemData.ItemName / ItemData.Description already resolve against the
        /// current LocalizationSettings.SelectedLocale each time they're read,
        /// so simply re-reading them here is enough to reflect a language change.
        /// </summary>
        private void Refresh()
        {
            if (currentItemData == null)
                return;

            if (nameText != null)
                nameText.text = currentItemData.ItemName;

            if (descriptionText != null)
                descriptionText.text = currentItemData.Description;
        }

        #endregion
    }
}
