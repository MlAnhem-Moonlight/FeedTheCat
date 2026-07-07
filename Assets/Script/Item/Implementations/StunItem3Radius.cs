using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// StunItem3Radius - Stuns all NPCs within a 3-tile radius.
    /// Behavior: Every NPC in radius becomes stunned and cannot move.
    /// Duration: 3 player turns.
    /// </summary>
    public class StunItem3Radius : ItemBase
    {
        #region Stun Effect Implementation

        /// <summary>
        /// Validates that the target is a valid GridCell.
        /// </summary>
        protected override bool ValidateTarget(GridCell target)
        {
            return target != null;
        }

        /// <summary>
        /// Execute the stun effect:
        /// 1. Find all NPCs in 3-tile radius
        /// 2. Apply Stun status to all affected NPCs
        /// 3. Stunned NPCs cannot move during effect duration
        /// </summary>
        protected override void ExecuteEffect(GridCell target)
        {
            if (itemData == null)
            {
                Debug.LogError("StunItem3Radius.ExecuteEffect: ItemData is not assigned");
                return;
            }

            // Get all NPCs within 3-tile radius
            List<NPCMover> affectedNPCs = RadiusHelper.GetNPCsInRadius(target, itemData.EffectRadius);

            if (affectedNPCs.Count == 0)
            {
                Debug.LogWarning($"StunItem3Radius.ExecuteEffect: No NPCs found in {itemData.EffectRadius} radius");
                return;
            }

            // Apply Stun status effect to ALL affected NPCs
            // Duration: 3 player turns (as per specification)
            StatusEffectSystem statusSystem = StatusEffectSystem.Instance;
            if (statusSystem != null)
            {
                foreach (NPCMover npc in affectedNPCs)
                {
                    statusSystem.ApplyStatusEffect(npc, StatusEffectType.Stun, itemData.EffectDuration);
                }

                LogFilter.LogItem($"StunItem3Radius.ExecuteEffect: Stunned {affectedNPCs.Count} NPCs in {itemData.EffectRadius} radius for {itemData.EffectDuration} turns");
            }
            else
            {
                LogFilter.LogItemError("StunItem3Radius.ExecuteEffect: StatusEffectSystem not found");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log Stun Item Info")]
        public void LogStunInfo()
        {
            LogItemInfo();
        }

        #endregion
    }
}
