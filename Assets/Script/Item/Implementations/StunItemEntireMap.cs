using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// StunItemEntireMap - Stuns all NPCs on the entire map.
    /// Behavior: Every NPC in the scene becomes stunned and cannot move.
    /// Duration: 4 player turns.
    /// </summary>
    public class StunItemEntireMap : ItemBase
    {
        #region Stun Effect Implementation

        /// <summary>
        /// Validates that the target is a valid GridCell.
        /// Even though this item affects the entire map, we still require a target click.
        /// </summary>
        protected override bool ValidateTarget(GridCell target)
        {
            return target != null;
        }

        /// <summary>
        /// Execute the stun effect:
        /// 1. Find all NPCs in the scene
        /// 2. Apply Stun status to all NPCs
        /// 3. All stunned NPCs cannot move during effect duration
        /// </summary>
        protected override void ExecuteEffect(GridCell target)
        {
            if (itemData == null)
            {
                Debug.LogError("StunItemEntireMap.ExecuteEffect: ItemData is not assigned");
                return;
            }

            // Find all NPCs on the entire map
            NPCMover[] allNPCs = Object.FindObjectsByType<NPCMover>(FindObjectsSortMode.None);

            if (allNPCs.Length == 0)
            {
                Debug.LogWarning("StunItemEntireMap.ExecuteEffect: No NPCs found in scene");
                return;
            }

            // Apply Stun status effect to ALL NPCs
            // Duration: 4 player turns (as per specification)
            StatusEffectSystem statusSystem = StatusEffectSystem.Instance;
            if (statusSystem != null)
            {
                foreach (NPCMover npc in allNPCs)
                {
                    if (npc != null)
                    {
                        statusSystem.ApplyStatusEffect(npc, StatusEffectType.Stun, itemData.EffectDuration);
                    }
                }

                Debug.Log($"StunItemEntireMap.ExecuteEffect: Stunned {allNPCs.Length} NPCs on entire map for {itemData.EffectDuration} turns");
            }
            else
            {
                Debug.LogError("StunItemEntireMap.ExecuteEffect: StatusEffectSystem not found");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log Stun Item Info")]
        public void LogStunInfo()
        {
            LogItemInfo();
            int npcCount = Object.FindObjectsByType<NPCMover>(FindObjectsSortMode.None).Length;
            Debug.Log($"  Total NPCs in scene: {npcCount}");
        }

        #endregion
    }
}
