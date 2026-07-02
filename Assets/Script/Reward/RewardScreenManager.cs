using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Rewards
{
    /// <summary>
    /// RewardScreenManager manages the reward screen flow.
    /// Instantiates and populates reward cards, handles selection, and processes rewards.
    /// Attach to the reward screen Canvas.
    /// </summary>
    public class RewardScreenManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Reward Cards")]
        [SerializeField]
        [Tooltip("Prefab for a reward card. Should contain RewardCard script and Button component.")]
        private GameObject rewardCardPrefab;

        [SerializeField]
        [Tooltip("Parent transform to instantiate reward cards into (e.g., RewardCardsContainer).")]
        private Transform rewardCardsContainer;

        [Header("Managers")]
        [SerializeField]
        [Tooltip("Reference to RewardGenerator for generating rewards.")]
        private RewardGenerator rewardGenerator;

        [SerializeField]
        [Tooltip("Reference to SceneTransitionManager or game flow manager.")]
        private MonoBehaviour gameFlowManager;

        #endregion

        #region Private Fields

        /// <summary>
        /// Generated rewards for current screen.
        /// </summary>
        private List<RewardGenerator.GeneratedReward> currentRewards;

        /// <summary>
        /// Instantiated reward card UI objects.
        /// </summary>
        private List<RewardCard> rewardCards = new List<RewardCard>();

        /// <summary>
        /// Flag to prevent multiple selections.
        /// </summary>
        private bool hasSelected = false;

        #endregion

        #region Lifecycle

        private void Start()
        {
            InitializeReferences();
            SetupRewardScreen();
        }

        /// <summary>
        /// Initialize cached references.
        /// </summary>
        private void InitializeReferences()
        {
            if (rewardGenerator == null)
                rewardGenerator = FindAnyObjectByType<RewardGenerator>();

            if (rewardGenerator == null)
                Debug.LogError("RewardScreenManager: RewardGenerator not found in scene");

            if (rewardCardsContainer == null)
                rewardCardsContainer = transform;
        }

        #endregion

        #region Setup

        /// <summary>
        /// Generate rewards and populate reward cards on screen.
        /// </summary>
        public void SetupRewardScreen()
        {
            if (rewardGenerator == null)
            {
                Debug.LogError("RewardScreenManager.SetupRewardScreen: RewardGenerator not initialized");
                return;
            }

            hasSelected = false;

            // Generate rewards
            currentRewards = rewardGenerator.GenerateRewardCards();

            if (currentRewards.Count == 0)
            {
                Debug.LogError("RewardScreenManager.SetupRewardScreen: No rewards generated");
                return;
            }

            // Clear existing cards
            ClearRewardCards();

            // Create and populate cards
            foreach (var reward in currentRewards)
            {
                CreateRewardCard(reward);
            }

            Debug.Log($"RewardScreenManager.SetupRewardScreen: Created {rewardCards.Count} reward cards");
        }

        /// <summary>
        /// Instantiate a reward card UI object.
        /// </summary>
        private void CreateRewardCard(RewardGenerator.GeneratedReward reward)
        {
            if (rewardCardPrefab == null)
            {
                Debug.LogError("RewardScreenManager.CreateRewardCard: rewardCardPrefab is not assigned");
                return;
            }

            // Instantiate card
            GameObject cardObject = Instantiate(rewardCardPrefab, rewardCardsContainer);
            RewardCard cardScript = cardObject.GetComponent<RewardCard>();

            if (cardScript == null)
            {
                Debug.LogError($"RewardScreenManager.CreateRewardCard: RewardCard script not found on prefab");
                Destroy(cardObject);
                return;
            }

            // Populate card data and set selection callback
            cardScript.PopulateCard(reward.itemData, reward.quantity, OnCardSelected);

            rewardCards.Add(cardScript);

            Debug.Log($"RewardScreenManager.CreateRewardCard: Created card for {reward.itemData.ItemName}");
        }

        /// <summary>
        /// Clear all instantiated reward cards.
        /// </summary>
        private void ClearRewardCards()
        {
            foreach (var card in rewardCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            rewardCards.Clear();
        }

        #endregion

        #region Selection Handling

        /// <summary>
        /// Called when a reward card is selected.
        /// </summary>
        private void OnCardSelected(RewardCard selectedCard)
        {
            if (hasSelected)
            {
                Debug.LogWarning("RewardScreenManager.OnCardSelected: Already selected a reward");
                return;
            }

            if (selectedCard == null || selectedCard.ItemData == null)
            {
                Debug.LogError("RewardScreenManager.OnCardSelected: Invalid card");
                return;
            }

            hasSelected = true;

            // Find the reward data for this card
            RewardGenerator.GeneratedReward selectedReward = FindRewardForCard(selectedCard);

            if (selectedReward == null)
            {
                Debug.LogError("RewardScreenManager.OnCardSelected: Could not find reward data for card");
                return;
            }

            // Apply reward
            ApplyRewardAndContinue(selectedReward);
        }

        /// <summary>
        /// Find the GeneratedReward corresponding to a RewardCard.
        /// </summary>
        private RewardGenerator.GeneratedReward FindRewardForCard(RewardCard card)
        {
            if (card == null || currentRewards == null)
                return null;

            foreach (var reward in currentRewards)
            {
                if (reward.itemData == card.ItemData)
                {
                    return reward;
                }
            }

            return null;
        }

        /// <summary>
        /// Apply the selected reward and continue the game flow.
        /// </summary>
        private void ApplyRewardAndContinue(RewardGenerator.GeneratedReward reward)
        {
            if (rewardGenerator == null)
                return;

            // Apply reward to inventory
            rewardGenerator.ApplyReward(reward);

            Debug.Log($"RewardScreenManager.ApplyRewardAndContinue: Applied {reward.quantity}x {reward.itemData.ItemName}");

            // Transition to next scene or close reward screen
            OnRewardScreenComplete();
        }

        /// <summary>
        /// Called after reward is selected and applied.
        /// Override or modify this method to match your game's flow.
        /// </summary>
        private void OnRewardScreenComplete()
        {
            // TODO: Transition to next level, close reward screen, or trigger game flow continuation
            // Example:
            // SceneManager.LoadScene("Gameplay");
            // or
            // gameFlowManager.ContinueGame();

            Debug.Log("RewardScreenManager.OnRewardScreenComplete: Reward screen complete. Implement game flow continuation.");

            // Fade out and transition
            // TimeManager.Instance?.LoadNextLevel();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reset and regenerate rewards (useful for testing).
        /// </summary>
        [ContextMenu("Regenerate Rewards")]
        public void RegenerateRewards()
        {
            SetupRewardScreen();
        }

        #endregion

        #region Debug

        [ContextMenu("Log Current Rewards")]
        public void LogCurrentRewards()
        {
            if (currentRewards == null || currentRewards.Count == 0)
            {
                Debug.Log("RewardScreenManager: No rewards generated yet");
                return;
            }

            string log = "Current Rewards:\n";
            foreach (var reward in currentRewards)
            {
                log += $"  {reward.itemData.ItemName}: {reward.quantity}x ({reward.rarity})\n";
            }

            Debug.Log(log);
        }

        #endregion
    }
}
