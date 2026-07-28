using UnityEngine;
using System.Collections.Generic;
using FeedTheCat;
using FeedTheCat.Items;

namespace FeedTheCat.Rewards
{
    /// <summary>
    /// RewardGenerator generates random reward cards based on difficulty level.
    /// Rolls 3 reward cards. For each card, a rarity is rolled using the
    /// difficulty's drop-rate table, then an item matching that rarity is
    /// picked from the reward pool. The same item may appear more than once
    /// across the 3 cards, but if it does, it is guaranteed (best-effort)
    /// to have a different rolled quantity each time.
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
        public ItemCollector itemCollector;

        [Header("Generation Settings")]
        [SerializeField]
        [Tooltip("Number of reward cards to generate per call.")]
        private int rewardCardCount = 3;

        [SerializeField]
        [Tooltip("Max attempts to re-roll quantity so a repeated item doesn't get the same quantity twice.")]
        private int maxQuantityRerollAttempts = 50;

        #endregion

        #region Rarity Tables

        /// <summary>
        /// Rarity drop rates for Easy difficulty.
        /// Common: 60%, Rare: 35%, Epic: 5%
        /// </summary>
        private readonly RewardTable easyTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic },
            probabilities = new[] { 0.60f, 0.35f, 0.05f },
            quantityRanges = new[]
            {
                new Vector2Int(1, 3),   // Common: 1-3
                new Vector2Int(1, 2),   // Rare: 1-2
                new Vector2Int(1, 1)    // Epic: 1
            }
        };

        /// <summary>
        /// Rarity drop rates for Medium difficulty.
        /// Common: 35%, Rare: 45%, Epic: 20%
        /// </summary>
        private readonly RewardTable mediumTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic },
            probabilities = new[] { 0.35f, 0.45f, 0.20f },
            quantityRanges = new[]
            {
                new Vector2Int(2, 4),   // Common: 2-4
                new Vector2Int(2, 3),   // Rare: 2-3
                new Vector2Int(1, 1)    // Epic: 1
            }
        };

        /// <summary>
        /// Rarity drop rates for Hard difficulty.
        /// Common: 25%, Rare: 40%, Epic: 25%, Legendary: 5%
        /// </summary>
        private readonly RewardTable hardTable = new RewardTable
        {
            rarities = new[] { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary },
            probabilities = new[] { 0.25f, 0.40f, 0.25f, 0.05f },
            quantityRanges = new[]
            {
                new Vector2Int(3, 4),   // Common: 3-4
                new Vector2Int(3, 4),   // Rare: 3-4
                new Vector2Int(1, 3),   // Epic: 1-3
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
        /// Generate reward cards (default 3) based on current difficulty.
        /// Each card rolls a rarity from the difficulty's drop table, then
        /// picks a matching item from the reward pool. Items can repeat,
        /// but repeated items are re-rolled to try to get a different quantity.
        /// </summary>
        /// <returns>List of GeneratedReward objects.</returns>
        public List<GeneratedReward> GenerateRewardCards()
        {
            List<GeneratedReward> rewards = new List<GeneratedReward>();

            if (rewardPool == null || rewardPool.Length == 0)
            {
                Debug.LogError("RewardGenerator.GenerateRewardCards: Reward pool is empty");
                return rewards;
            }

            Difficulty difficulty = GetCurrentDifficulty();
            RewardTable table = GetTableForDifficulty(difficulty);

            Debug.Log($"RewardGenerator.GenerateRewardCards: Generating {rewardCardCount} rewards for {difficulty} difficulty");

            // Tracks quantities already rolled per item, so repeats get a different quantity
            Dictionary<ItemData, HashSet<int>> usedQuantitiesPerItem = new Dictionary<ItemData, HashSet<int>>();

            for (int i = 0; i < rewardCardCount; i++)
            {
                GeneratedReward reward = GenerateSingleReward(table, usedQuantitiesPerItem);
                if (reward == null)
                    continue;

                rewards.Add(reward);

                if (!usedQuantitiesPerItem.TryGetValue(reward.itemData, out HashSet<int> quantities))
                {
                    quantities = new HashSet<int>();
                    usedQuantitiesPerItem[reward.itemData] = quantities;
                }
                quantities.Add(reward.quantity);
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
        /// Lazy-loads ItemCollector on first call to ensure it's initialized.
        /// (Assumes ItemCollector.CurrentDifficulty is derived from the
        /// "{difficulty}_{levelName}" scene/level naming convention.)
        /// </summary>
        private Difficulty GetCurrentDifficulty()
        {
            if (itemCollector == null)
            {
                itemCollector = FindAnyObjectByType<ItemCollector>();
                if (itemCollector == null)
                {
                    Debug.LogWarning("RewardGenerator.GetCurrentDifficulty: ItemCollector not found in scene. Using Medium difficulty as fallback.");
                    return Difficulty.Medium;
                }
            }

            return itemCollector.CurrentDifficulty;
        }

        /// <summary>
        /// Resolve the RewardTable for a given difficulty.
        /// </summary>
        private RewardTable GetTableForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => easyTable,
                Difficulty.Medium => mediumTable,
                Difficulty.Hard => hardTable,
                _ => mediumTable
            };
        }

        /// <summary>
        /// Generate one reward card: roll a rarity by drop rate, pick a matching
        /// item from the pool, roll a quantity, and (best-effort) avoid repeating
        /// the exact same quantity if this item was already rolled this batch.
        /// </summary>
        private GeneratedReward GenerateSingleReward(RewardTable table, Dictionary<ItemData, HashSet<int>> usedQuantitiesPerItem)
        {
            ItemRarity rarity = RollRarity(table);
            List<ItemData> matchingItems = GetItemsByRarity(rarity);

            // Fallback: if no pool item has the rolled rarity, try the other
            // rarities supported by this difficulty table before giving up.
            if (matchingItems.Count == 0)
            {
                Debug.LogWarning($"RewardGenerator.GenerateSingleReward: No items in pool with rarity {rarity}. Trying fallback rarities.");

                foreach (ItemRarity fallbackRarity in table.rarities)
                {
                    matchingItems = GetItemsByRarity(fallbackRarity);
                    if (matchingItems.Count > 0)
                    {
                        rarity = fallbackRarity;
                        break;
                    }
                }
            }

            if (matchingItems.Count == 0)
            {
                Debug.LogError("RewardGenerator.GenerateSingleReward: No valid items found in reward pool for any rarity in this difficulty table");
                return null;
            }

            ItemData selectedItem = matchingItems[Random.Range(0, matchingItems.Count)];

            GetQuantityRange(table, rarity, out int quantityMin, out int quantityMax);
            int quantity = Random.Range(quantityMin, quantityMax + 1);

            // If this item was already picked earlier in this batch, try to
            // roll a different quantity so the two cards aren't identical.
            if (usedQuantitiesPerItem.TryGetValue(selectedItem, out HashSet<int> existingQuantities))
            {
                int attempts = 0;
                while (existingQuantities.Contains(quantity) && attempts < maxQuantityRerollAttempts)
                {
                    quantity = Random.Range(quantityMin, quantityMax + 1);
                    attempts++;
                }

                if (existingQuantities.Contains(quantity))
                {
                    Debug.LogWarning($"RewardGenerator.GenerateSingleReward: Could not find a unique quantity for repeated item " +
                        $"'{selectedItem.ItemName}' within range [{quantityMin}-{quantityMax}] after {maxQuantityRerollAttempts} attempts. " +
                        $"Using duplicate quantity {quantity}.");
                }
            }

            Debug.Log($"RewardGenerator.GenerateSingleReward: Rolled {rarity} -> {selectedItem.ItemName} x{quantity}");

            return new GeneratedReward(selectedItem, quantity, rarity);
        }

        /// <summary>
        /// Get all reward-pool items whose ItemData.Rarity matches the given rarity.
        /// </summary>
        private List<ItemData> GetItemsByRarity(ItemRarity rarity)
        {
            List<ItemData> result = new List<ItemData>();

            if (rewardPool == null)
                return result;

            foreach (ItemData item in rewardPool)
            {
                if (item != null && item.Rarity == rarity)
                    result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Look up the quantity range configured for a rarity in a table.
        /// </summary>
        private void GetQuantityRange(RewardTable table, ItemRarity rarity, out int min, out int max)
        {
            min = 1;
            max = 1;

            for (int i = 0; i < table.rarities.Length; i++)
            {
                if (table.rarities[i] == rarity)
                {
                    min = table.quantityRanges[i].x;
                    max = table.quantityRanges[i].y;
                    return;
                }
            }
        }

        /// <summary>
        /// Roll a rarity based on the difficulty table's cumulative probability.
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

            // Fallback (floating point rounding edge case)
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