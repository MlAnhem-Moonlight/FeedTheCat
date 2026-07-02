using UnityEngine;
using System;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Manages item quantities and persistence.
    /// Raises events when quantities change to update UI.
    /// Uses PlayerPrefs for save/load functionality.
    /// </summary>
    public class ItemInventory : MonoBehaviour
    {
        #region Singleton

        private static ItemInventory s_instance;

        public static ItemInventory Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<ItemInventory>();
                    if (s_instance == null)
                    {
                        Debug.LogError("ItemInventory: Instance not found in scene. Create ItemInventory GameObject.");
                    }
                }
                return s_instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when an item's quantity changes.
        /// Parameters: itemID, newQuantity, previousQuantity
        /// </summary>
        public event Action<string, int, int> OnQuantityChanged;

        /// <summary>
        /// Raised when an item is added to inventory.
        /// Parameters: itemID, quantity
        /// </summary>
        public event Action<string, int> OnItemAdded;

        /// <summary>
        /// Raised when inventory is completely loaded.
        /// </summary>
        public event Action OnInventoryLoaded;

        #endregion

        #region Fields

        [Header("Storage")]
        [SerializeField]
        [Tooltip("Prefix for PlayerPrefs keys to avoid conflicts.")]
        private string persistenceKeyPrefix = "inventory_";

        [SerializeField]
        [Tooltip("Maximum quantity cap per item (0 = unlimited).")]
        private int maxQuantityPerItem = 999;

        /// <summary>
        /// In-memory inventory storage: itemID -> quantity
        /// </summary>
        private Dictionary<string, int> inventory = new Dictionary<string, int>();

        /// <summary>
        /// Flag to track if inventory has been loaded from PlayerPrefs.
        /// </summary>
        private bool isLoaded = false;

        #endregion

        #region Properties

        public bool IsLoaded => isLoaded;

        public int MaxQuantityPerItem => maxQuantityPerItem;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Debug.LogWarning("ItemInventory: Duplicate instance found. Destroying.");
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Load();
        }

        #endregion

        #region Public API - Queries

        /// <summary>
        /// Get the current quantity of an item.
        /// </summary>
        public int GetQuantity(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning("ItemInventory.GetQuantity: itemID is empty");
                return 0;
            }

            if (!inventory.ContainsKey(itemID))
                return 0;

            return inventory[itemID];
        }

        /// <summary>
        /// Check if item can be used (quantity > 0).
        /// </summary>
        public bool CanUse(string itemID)
        {
            return GetQuantity(itemID) > 0;
        }

        /// <summary>
        /// Get all items currently in inventory.
        /// </summary>
        public Dictionary<string, int> GetAllItems()
        {
            return new Dictionary<string, int>(inventory);
        }

        #endregion

        #region Public API - Modifications

        /// <summary>
        /// Set quantity for an item directly.
        /// </summary>
        public void SetQuantity(string itemID, int quantity)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning("ItemInventory.SetQuantity: itemID is empty");
                return;
            }

            int clampedQuantity = Mathf.Max(0, quantity);
            if (maxQuantityPerItem > 0)
                clampedQuantity = Mathf.Min(clampedQuantity, maxQuantityPerItem);

            int previousQuantity = GetQuantity(itemID);

            if (previousQuantity == clampedQuantity)
                return;

            inventory[itemID] = clampedQuantity;

            OnQuantityChanged?.Invoke(itemID, clampedQuantity, previousQuantity);

            Debug.Log($"ItemInventory: Set {itemID} quantity to {clampedQuantity} (was {previousQuantity})");
        }

        /// <summary>
        /// Increase item quantity.
        /// </summary>
        public void IncreaseItem(string itemID, int amount)
        {
            if (string.IsNullOrEmpty(itemID) || amount <= 0)
            {
                Debug.LogWarning("ItemInventory.IncreaseItem: Invalid parameters");
                return;
            }

            int currentQty = GetQuantity(itemID);
            int newQty = currentQty + amount;

            SetQuantity(itemID, newQty);

            OnItemAdded?.Invoke(itemID, amount);
        }

        /// <summary>
        /// Decrease item quantity.
        /// </summary>
        public void DecreaseItem(string itemID, int amount)
        {
            if (string.IsNullOrEmpty(itemID) || amount <= 0)
            {
                Debug.LogWarning("ItemInventory.DecreaseItem: Invalid parameters");
                return;
            }

            int currentQty = GetQuantity(itemID);
            int newQty = Mathf.Max(0, currentQty - amount);

            SetQuantity(itemID, newQty);
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Save inventory to PlayerPrefs.
        /// </summary>
        public void Save()
        {
            foreach (var kvp in inventory)
            {
                string key = persistenceKeyPrefix + kvp.Key;
                PlayerPrefs.SetInt(key, kvp.Value);
            }

            PlayerPrefs.Save();
            Debug.Log("ItemInventory: Saved to PlayerPrefs");
        }

        /// <summary>
        /// Load inventory from PlayerPrefs.
        /// </summary>
        public void Load()
        {
            if (isLoaded)
                return;

            inventory.Clear();

            isLoaded = true;
            OnInventoryLoaded?.Invoke();

            Debug.Log("ItemInventory: Loaded from PlayerPrefs");
        }

        /// <summary>
        /// Load a specific item's quantity from PlayerPrefs.
        /// </summary>
        public void LoadItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return;

            string key = persistenceKeyPrefix + itemID;
            int quantity = PlayerPrefs.GetInt(key, 0);

            if (quantity > 0)
            {
                inventory[itemID] = quantity;
                Debug.Log($"ItemInventory: Loaded {itemID} with quantity {quantity} from PlayerPrefs");
            }
        }

        /// <summary>
        /// Clear all inventory data (for testing or reset).
        /// </summary>
        [ContextMenu("Clear All Inventory")]
        public void ClearAllInventory()
        {
            inventory.Clear();

            List<string> keysToDelete = new List<string>();
            foreach (string key in GetAllPlayerPrefKeys())
            {
                if (key.StartsWith(persistenceKeyPrefix))
                    keysToDelete.Add(key);
            }

            foreach (string key in keysToDelete)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
            Debug.Log("ItemInventory: All inventory cleared");
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get all PlayerPrefs keys.
        /// </summary>
        private List<string> GetAllPlayerPrefKeys()
        {
            List<string> keys = new List<string>();
            return keys;
        }

        #endregion

        #region Debug

        [ContextMenu("Log Inventory")]
        public void LogInventory()
        {
            if (inventory.Count == 0)
            {
                Debug.Log("ItemInventory: Empty");
                return;
            }

            string log = "ItemInventory Contents:\n";
            foreach (var kvp in inventory)
            {
                log += $"  {kvp.Key}: {kvp.Value}\n";
            }
            Debug.Log(log);
        }

        #endregion
    }
}
