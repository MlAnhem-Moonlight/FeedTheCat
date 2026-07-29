using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// CharmItem4Radius - Charms nearby NPCs within a 3-tile radius.
    /// Behavior: Selects ONE random NPC to walk toward the target cell.
    /// All other affected NPCs stop moving.
    /// Duration: 4 player turns.
    /// </summary>
    public class CharmItem4Radius : ItemBase
    {
        #region Private Fields

        /// <summary>
        /// The single NPC chosen to move toward the target.
        /// </summary>
        private NPCMover charmedNPC;

        #endregion

        #region Charm Effect Implementation

        /// <summary>
        /// Validates that the target is a valid GridCell.
        /// </summary>
        protected override bool ValidateTarget(GridCell target)
        {
            return target != null;
        }

        /// <summary>
        /// Execute the charm effect:
        /// 1. Find all NPCs in 3-tile radius
        /// 2. Choose ONE random NPC
        /// 3. Apply Charm status to all affected NPCs
        /// 4. Only the chosen NPC will move toward target
        /// </summary>
        protected override void ExecuteEffect(GridCell target)
        {
            if (itemData == null)
            {
                Debug.LogError("CharmItem4Radius.ExecuteEffect: ItemData is not assigned");
                return;
            }

            // Get all NPCs within 3-tile radius
            List<NPCMover> affectedNPCs = RadiusHelper.GetNPCsInRadius(target, itemData.EffectRadius);

            if (affectedNPCs.Count == 0)
            {
                Debug.LogWarning($"CharmItem4Radius.ExecuteEffect: No NPCs found in {itemData.EffectRadius} radius");
                return;
            }

            // Select ONE random NPC from affected list
            // Prefer picking the "chosen" charmed NPC from those that can actually
            // move (excludes Idle NPCs, which never walk regardless of Charm).
            // If everyone in radius happens to be Idle, fall back to the full
            // list - they'll just all show the Charm visual and stay frozen.
            List<NPCMover> movableCandidates = affectedNPCs.FindAll(n => n != null && n.CanMove());
            List<NPCMover> selectionPool = movableCandidates.Count > 0 ? movableCandidates : affectedNPCs;

            charmedNPC = selectionPool[Random.Range(0, selectionPool.Count)];

            Debug.Log($"CharmItem4Radius.ExecuteEffect: Charmed NPC '{charmedNPC.gameObject.name}' selected from {affectedNPCs.Count} affected NPCs");

            // Apply Charm status effect to ALL affected NPCs
            // Duration: 4 player turns (as per specification)
            // Only the chosen NPC gets a moveTarget - that's what tells NPCMover
            // to actually walk toward the clicked cell each turn. Every other
            // affected NPC gets moveTarget=null, which keeps it frozen in place.
            StatusEffectSystem statusSystem = StatusEffectSystem.Instance;
            if (statusSystem != null)
            {
                foreach (NPCMover npc in affectedNPCs)
                {
                    GridCell moveTarget = (npc == charmedNPC) ? target : null;
                    statusSystem.ApplyStatusEffect(npc, StatusEffectType.Charm, itemData.EffectDuration, moveTarget);
                }

                Debug.Log($"CharmItem4Radius.ExecuteEffect: Applied Charm to {affectedNPCs.Count} NPCs for {itemData.EffectDuration} turns (chosen NPC will walk toward target each turn)");
            }
            else
            {
                Debug.LogError("CharmItem4Radius.ExecuteEffect: StatusEffectSystem not found");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log Charm Item Info")]
        public void LogCharmInfo()
        {
            LogItemInfo();
            if (charmedNPC != null)
                Debug.Log($"  Currently Charmed NPC: {charmedNPC.gameObject.name}");
        }

        #endregion
    }
}