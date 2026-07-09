using UnityEngine;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Attached automatically to the visual instantiated on a GridCell when an
    /// item is dropped (see ItemBase.SpawnItemVisualOnGrid). Counts down player
    /// turns the same way StatusEffectSystem does - one tick per PlayerController
    /// step - so the visual disappears from the board at the same time the
    /// underlying Stun/Charm status effect expires on the affected NPCs.
    ///
    /// Pass durationTurns &lt;= 0 to make the visual persist forever (never
    /// auto-destroyed); useful for items that don't have a duration.
    /// </summary>
    public class ItemVisualEffect : MonoBehaviour
    {
        private int remainingTurns;
        private bool isCountingDown;

        /// <summary>
        /// Start the countdown. Call this right after instantiating the prefab.
        /// </summary>
        public void Initialize(int durationTurns)
        {
            remainingTurns = durationTurns;
            isCountingDown = remainingTurns > 0;

            if (isCountingDown)
                PlayerController.OnPlayerStep += HandlePlayerStep;
        }

        private void HandlePlayerStep()
        {
            if (!isCountingDown)
                return;

            remainingTurns--;

            if (remainingTurns <= 0)
            {
                isCountingDown = false;
                PlayerController.OnPlayerStep -= HandlePlayerStep;

                if (this != null && gameObject != null)
                    Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (isCountingDown)
                PlayerController.OnPlayerStep -= HandlePlayerStep;
        }
    }
}
