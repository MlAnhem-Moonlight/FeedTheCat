using UnityEngine;

namespace FeedTheCat.Items
{
    /// <summary>
    /// CollisionDestructionItem - Passive item that destroys NPCs on collision.
    /// When the player collides with an NPC:
    /// - If item quantity > 0: Consume one quantity and destroy ONLY the collided NPC. Player survives.
    /// - If quantity == 0: Normal game over behavior applies.
    /// This item does NOT appear in the toolbar and cannot be dragged.
    /// Player can choose to use it or not via a UI prompt.
    /// </summary>
    public class CollisionDestructionItem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Item Configuration")]
        [SerializeField]
        [Tooltip("The ItemData for this passive item.")]
        private ItemData itemData;

        [SerializeField]
        [Tooltip("The player's controller component to hook collision events.")]
        private PlayerController playerController;

        [Tooltip("Reference to ItemInventory for quantity checks.")]
        private ItemInventory itemInventory;

        #endregion

        #region Cached References

        private bool isInitialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// The item data defining this passive item.
        /// </summary>
        public ItemData ItemData => itemData;

        /// <summary>
        /// Unique identifier for this item.
        /// </summary>
        public string ItemID => itemData?.ItemID ?? "passive_collision_item";

        /// <summary>
        /// Current quantity of this item in inventory.
        /// </summary>
        public int CurrentQuantity
        {
            get
            {
                if (itemInventory == null)
                    itemInventory = ItemInventory.Instance;

                return itemInventory != null ? itemInventory.GetQuantity(ItemID) : 0;
            }
        }

        #endregion

        #region Lifecycle

        private void Start()
        {
            InitializeReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Initialize cached references and subscribe to collision events.
        /// </summary>
        private void InitializeReferences()
        {
            if (isInitialized)
                return;

            if (itemData == null)
            {
                LogFilter.LogItemError($"CollisionDestructionItem: ItemData not assigned on {gameObject.name}");
                return;
            }


            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            // Do not rely on inspector-assigned cross-scene references. Resolve ItemInventory at runtime via its singleton.
            itemInventory = ItemInventory.Instance;

            if (playerController == null)
            {
                LogFilter.LogItemError($"CollisionDestructionItem: PlayerController not found in scene");
                return;
            }

            if (itemInventory == null)
            {
                LogFilter.LogItemError($"CollisionDestructionItem: ItemInventory.Instance not found in scene");
                return;
            }

            // Load this item's quantity from persistence
            itemInventory.LoadItem(ItemID);

            // Subscribe to player collision event
            // TODO: Modify PlayerController to expose an event like OnNPCCollision(NPCMover npc)
            // playerController.OnNPCCollision += HandleNPCCollision;

            isInitialized = true;
            LogFilter.LogItem($"CollisionDestructionItem: Initialized with ItemID '{ItemID}', Current Quantity: {CurrentQuantity}");
        }

        /// <summary>
        /// Unsubscribe from all events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (playerController != null)
            {
                // playerController.OnNPCCollision -= HandleNPCCollision;
            }
        }

        #endregion

        #region Collision Handling

        /// <summary>
        /// Called when the player collides with an NPC.
        /// Checks item quantity and either destroys the NPC or triggers game over.
        /// </summary>
        /// <param name="collidedNPC">The NPC the player collided with.</param>
        public void HandleNPCCollision(NPCMover collidedNPC)
        {
            if (!isInitialized || collidedNPC == null)
                return;

            // Check if this item has quantity available
            if (CurrentQuantity > 0)
            {
                // Use the item to destroy the NPC
                UseItemToDestroyNPC(collidedNPC);
            }
            else
            {
                // No quantity left - trigger normal game over
                TriggerGameOver(collidedNPC);
            }
        }

        /// <summary>
        /// Destroy the collided NPC and decrease item quantity.
        /// Player survives and continues playing.
        /// </summary>
        private void UseItemToDestroyNPC(NPCMover npc)
        {
            if (npc == null || itemInventory == null)
                return;

            // Decrease quantity
            itemInventory.DecreaseItem(ItemID, 1);

            // Save inventory
            itemInventory.Save();

            Debug.Log($"CollisionDestructionItem: Destroyed NPC '{npc.gameObject.name}'. Remaining quantity: {CurrentQuantity}");

            // Destroy the NPC
            Destroy(npc.gameObject);

            // Optionally play a visual/audio effect here
            SpawnDestructionEffect(npc.transform.position);
        }

        /// <summary>
        /// Trigger normal game over behavior when item quantity is exhausted.
        /// </summary>
        private void TriggerGameOver(NPCMover collidedNPC)
        {
            Debug.Log($"CollisionDestructionItem: No quantity remaining. Triggering game over due to collision with '{collidedNPC.gameObject.name}'");

            // TODO: Call game over logic
            // GameManager.Instance.TriggerGameOver();
            // or
            // TurnManager.Instance.EndGame();
        }

        /// <summary>
        /// Spawn a visual effect at the destruction location (e.g., explosion particle).
        /// </summary>
        private void SpawnDestructionEffect(Vector3 position)
        {
            // TODO: Instantiate a particle system or visual effect prefab at the position
            // For now, this is a placeholder
            Debug.Log($"CollisionDestructionItem: Spawning destruction effect at {position}");
        }

        #endregion

        #region Prompt/UI Interaction

        /// <summary>
        /// Show a UI prompt to the player asking if they want to use the item.
        /// Called when collision is detected and item is available.
        /// </summary>
        public void ShowUsagePrompt(NPCMover collidedNPC)
        {
            if (CurrentQuantity == 0)
            {
                Debug.Log("CollisionDestructionItem.ShowUsagePrompt: No quantity available");
                return;
            }

            // TODO: Show UI dialog with options:
            // - Use item: destroy the NPC and continue
            // - Don't use: trigger game over

            Debug.Log($"CollisionDestructionItem.ShowUsagePrompt: Show prompt for collision with '{collidedNPC.gameObject.name}'");
        }

        #endregion

        #region Debug

        [ContextMenu("Log Collision Item Info")]
        public void LogCollisionItemInfo()
        {
            if (itemData != null)
            {
                Debug.Log($"Item: {itemData.ItemName}\n" +
                         $"ID: {ItemID}\n" +
                         $"Current Quantity: {CurrentQuantity}\n" +
                         $"Rarity: {itemData.Rarity}");
            }
            else
            {
                Debug.Log("CollisionDestructionItem: ItemData not assigned");
            }
        }

        #endregion
    }
}
