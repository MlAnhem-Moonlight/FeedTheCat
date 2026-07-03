using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace FeedTheCat.Items
{
    /// <summary>
    /// Reads ItemData and assigns its values to UI components.
    /// </summary>
    public class ItemUIBinder : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ItemData itemData;

        [Header("UI References (optional)")]
        [SerializeField] private Image iconImage;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text itemTypeText;
        [SerializeField] private TMP_Text radiusText;
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text durationText;

        private void Awake()
        {
            AutoFindReferences();
            Refresh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoFindReferences();

            if (!Application.isPlaying)
                Refresh();
        }
#endif

        /// <summary>
        /// Assign another ItemData at runtime.
        /// </summary>
        [ContextMenu("Set Item Data")]
        public void SetItem(ItemData data)
        {
            itemData = data;
            Refresh();
        }

        /// <summary>
        /// Update all UI from ItemData.
        /// </summary>
        public void Refresh()
        {
            if (itemData == null)
                return;

            if (iconImage != null)
                iconImage.sprite = itemData.Icon;

            if (nameText != null)
                nameText.text = itemData.ItemName;

            if (descriptionText != null)
                descriptionText.text = itemData.Description;

            //if (rarityText != null)
            //    rarityText.text = itemData.Rarity.ToString();

            //if (cooldownText != null)
            //    cooldownText.text = itemData.Cooldown.ToString();

            //if (itemTypeText != null)
            //    itemTypeText.text = itemData.ItemType.ToString();

            //if (radiusText != null)
            //    radiusText.text = itemData.EffectRadius.ToString();

            //if (targetText != null)
            //    targetText.text = itemData.TargetType.ToString();

            //if (statusText != null)
            //    statusText.text = itemData.StatusEffect.ToString();

            //if (durationText != null)
            //    durationText.text = itemData.EffectDuration.ToString();
        }

        /// <summary>
        /// Automatically find child UI objects by name.
        /// </summary>
        private void AutoFindReferences()
        {
            if (iconImage == null)
                iconImage = FindImage("Icon");

            if (nameText == null)
                nameText = FindTMP("Name");

            if (descriptionText == null)
                descriptionText = FindTMP("Description");

            //if (rarityText == null)
            //    rarityText = FindTMP("Rarity");

            //if (cooldownText == null)
            //    cooldownText = FindTMP("Cooldown");

            //if (itemTypeText == null)
            //    itemTypeText = FindTMP("Type");

            //if (radiusText == null)
            //    radiusText = FindTMP("Radius");

            //if (targetText == null)
            //    targetText = FindTMP("Target");

            //if (statusText == null)
            //    statusText = FindTMP("Status");

            //if (durationText == null)
            //    durationText = FindTMP("Duration");
        }

        private Image FindImage(string childName)
        {
            Transform t = transform.Find(childName);
            return t ? t.GetComponent<Image>() : null;
        }

        private TMP_Text FindTMP(string childName)
        {
            Transform t = transform.Find(childName);
            return t ? t.GetComponent<TMP_Text>() : null;
        }
    }
}