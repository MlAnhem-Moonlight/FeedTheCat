using UnityEngine;
using System.Collections.Generic;
using FeedTheCat;
using FeedTheCat.Items;

namespace FeedTheCat.Rewards
{
    /// <summary>
    /// RewardGenerator generates random reward cards based on difficulty level.
    /// Selects 3 unique items from a pool of 5 possible rewards.
    /// Difficulty affects drop rates and quantities.
    /// </summary>
    public class RewardGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Reward Pool")]
        [SerializeField]
        [Tooltip("Array of 5 ItemData assets available as rewards.")]
        private ItemData[] rewardPool = new ItemData[5];

        [Header("Difficulty Source")]
        [SerializeField]
        [Tooltip("Reference to ItemCollector for difficulty level.")]
        private ItemCollector itemCollector;

        #endregion

        #region Rarity Tables

        /// <summary>
        /// Rarity drop rates for Easy difficulty.
        /// </summary>
        private readonly RewardTable easyTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic },
            probabilities = new[] { 0.60f, 0.35f, 0.05f },
            quantityRanges = new[]
            {
                new Vector2Int(1, 5),   // Common: 1-5
                new Vector2Int(1, 3),   // Rare: 1-3
                new Vector2Int(1, 1)    // Epic: 1
            }
        };

        /// <summary>
        /// Rarity drop rates for Medium difficulty.
        /// </summary>
        private readonly RewardTable mediumTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic },
            probabilities = new[] { 0.35f, 0.45f, 0.20f },
            quantityRanges = new[]
            {
                new Vector2Int(2, 5),   // Common: 2-5
                new Vector2Int(2, 6),   // Rare: 2-6
                new Vector2Int(1, 3)    // Epic: 1-3
            }
        };

        /// <summary>
        /// Rarity drop rates for Hard difficulty.
        /// </summary>
        private readonly RewardTable hardTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary },
            probabilities = new[] { 0.25f, 0.40f, 0.25f, 0.05f },
            quantityRanges = new[]
            {
                new Vector2Int(5, 8),   // Common: 5-8
                new Vector2Int(3, 8),   // Rare: 3-8
                new Vector2Int(2, 5),   // Epic: 2-5
                new Vector2Int(1, 1)    // Legendary: 1
            }
        };

        #endregion

        #region Data Structures

        /// <summary>
        /// Contains rarity distribution and quantity ranges for a difficulty level.
        /// </summary>
        private class RewardTable
        {
            public ItemRarity[] rarities;
            public float[] probabilities;
            public Vector2Int[] quantityRanges;
        }

        /// <summary>
        /// Represents a single generated reward.
        /// </summary>
        public class GeneratedReward
        {
            public ItemData itemData;
            public int quantity;
            public ItemRarity rarity;

            public GeneratedReward(ItemData item, int qty, ItemRarity rarity)
            {
                itemData = item;
                quantity = qty;
                this.rarity = rarity;
            }
        }

        #endregion

        #region Lifecycle

        private void Start()
        {
            InitializeReferences();
        }

        /// <summary>
        /// Initialize references to required managers.
        /// </summary>
        private void InitializeReferences()
        {
            if (itemCollector == null)
                itemCollector = FindAnyObjectByType<ItemCollector>();

            if (itemCollector == null)
                Debug.LogError("RewardGenerator: ItemCollector not found in scene");

            ValidateRewardPool();
        }

        /// <summary>
        /// Validate that reward pool has exactly 5 items.
        /// </summary>
        private void ValidateRewardPool()
        {
            if (rewardPool == null || rewardPool.Length != 5)
            {
                Debug.LogError("RewardGenerator: Reward pool must contain exactly 5 ItemData assets");
            }

            foreach (var item in rewardPool)
            {
                if (item == null)
                    Debug.LogWarning("RewardGenerator: Reward pool contains null ItemData");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generate 3 unique random reward cards based on current difficulty.
        /// </summary>
        /// <returns>List of 3 GeneratedReward objects.</returns>
        public List<GeneratedReward> GenerateRewardCards()
        {
            List<GeneratedReward> rewards = new List<GeneratedReward>();

            // Get difficulty from ItemCollector
            Difficulty difficulty = GetCurrentDifficulty();

            // Select 3 unique items from reward pool
            List<ItemData> selectedItems = SelectUniqueItems(3);

            if (selectedItems.Count != 3)
            {
                Debug.LogError("RewardGenerator: Failed to select 3 unique items");
                return rewards;
            }

            // Generate reward data for each selected item
            foreach (ItemData item in selectedItems)
            {
                GeneratedReward reward = GenerateRewardForItem(item, difficulty);
                rewards.Add(reward);
            }

            Debug.Log($"RewardGenerator.GenerateRewardCards: Generated {rewards.Count} rewards for {difficulty} difficulty");

            return rewards;
        }

        /// <summary>
        /// Process a selected reward card: increase inventory and save.
        /// </summary>
        /// <param name="reward">The reward to apply.</param>
        public void ApplyReward(GeneratedReward reward)
        {
            if (reward?.itemData == null)
            {
                Debug.LogError("RewardGenerator.ApplyReward: Invalid reward");
                return;
            }

            ItemInventory inventory = ItemInventory.Instance;
            if (inventory == null)
            {
                Debug.LogError("RewardGenerator.ApplyReward: ItemInventory not found");
                return;
            }

            // Increase inventory quantity
            string itemID = reward.itemData.ItemID;
            inventory.IncreaseItem(itemID, reward.quantity);

            // Save immediately
            inventory.Save();

            Debug.Log($"RewardGenerator.ApplyReward: Applied {reward.quantity}x {reward.itemData.ItemName}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get current difficulty from ItemCollector.
        /// </summary>
        private Difficulty GetCurrentDifficulty()
        {
            if (itemCollector == null)
                return Difficulty.Medium; // Default fallback

            return itemCollector.CurrentDifficulty;
        }

        /// <summary>
        /// Select 3 unique items from the reward pool.
        /// </summary>
        private List<ItemData> SelectUniqueItems(int count)
        {
            List<ItemData> selected = new List<ItemData>();

            if (rewardPool == null || rewardPool.Length < count)
            {
                Debug.LogWarning($"RewardGenerator.SelectUniqueItems: Reward pool has fewer than {count} items");
                return selected;
            }

            // Create a copy of the pool to shuffle
            List<ItemData> availableItems = new List<ItemData>(rewardPool);

            // Fisher-Yates shuffle and pick first 'count' items
            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(i, availableItems.Count);

                // Swap
                (availableItems[i], availableItems[randomIndex]) = (availableItems[randomIndex], availableItems[i]);

                selected.Add(availableItems[i]);
            }

            return selected;
        }

        /// <summary>
        /// Generate a single reward for an item based on difficulty.
        /// </summary>
        private GeneratedReward GenerateRewardForItem(ItemData item, Difficulty difficulty)
        {
            RewardTable table = difficulty switch
            {
                Difficulty.Easy => easyTable,
                Difficulty.Medium => mediumTable,
                Difficulty.Hard => hardTable,
                _ => mediumTable
            };

            // Roll for rarity
            ItemRarity rarity = RollRarity(table);

            // Get quantity range for this rarity
            int quantityMin = 1, quantityMax = 1;
            for (int i = 0; i < table.rarities.Length; i++)
            {
                if (table.rarities[i] == rarity)
                {
                    quantityMin = table.quantityRanges[i].x;
                    quantityMax = table.quantityRanges[i].y;
                    break;
                }
            }

            // Roll quantity within range
            int quantity = Random.Range(quantityMin, quantityMax + 1);

            return new GeneratedReward(item, quantity, rarity);
        }

        /// <summary>
        /// Roll a rarity based on probability table.
        /// </summary>
        private ItemRarity RollRarity(RewardTable table)
        {
            float roll = Random.Range(0f, 1f);
            float cumulativeProbability = 0f;

            for (int i = 0; i < table.rarities.Length; i++)
            {
                cumulativeProbability += table.probabilities[i];
                if (roll <= cumulativeProbability)
                {
                    return table.rarities[i];
                }
            }

            // Fallback (should not reach here)
            return table.rarities[table.rarities.Length - 1];
        }

        #endregion

        #region Debug

        [ContextMenu("Generate Test Rewards")]
        public void DebugGenerateRewards()
        {
            List<GeneratedReward> rewards = GenerateRewardCards();

            string log = "Generated Rewards:\n";
            foreach (var reward in rewards)
            {
                log += $"  {reward.itemData.ItemName}: {reward.quantity}x ({reward.rarity})\n";
            }

            Debug.Log(log);
        }

        #endregion
    }
}
