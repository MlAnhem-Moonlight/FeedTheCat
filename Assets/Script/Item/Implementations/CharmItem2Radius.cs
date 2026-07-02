using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// CharmItem2Radius - Charms nearby NPCs within a 2-tile radius.
    /// Behavior: Selects ONE random NPC to walk toward the target cell.
    /// All other affected NPCs stop moving.
    /// Duration: 2 player turns.
    /// </summary>
    public class CharmItem2Radius : ItemBase
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
        /// 1. Find all NPCs in 2-tile radius
        /// 2. Choose ONE random NPC
        /// 3. Apply Charm status to all affected NPCs
        /// 4. Only the chosen NPC will move toward target
        /// </summary>
        protected override void ExecuteEffect(GridCell target)
        {
            if (itemData == null)
            {
                Debug.LogError("CharmItem2Radius.ExecuteEffect: ItemData is not assigned");
                return;
            }

            // Get all NPCs within 2-tile radius
            List<NPCMover> affectedNPCs = RadiusHelper.GetNPCsInRadius(target, itemData.EffectRadius);

            if (affectedNPCs.Count == 0)
            {
                Debug.LogWarning($"CharmItem2Radius.ExecuteEffect: No NPCs found in {itemData.EffectRadius} radius");
                return;
            }

            // Select ONE random NPC from affected list
            charmedNPC = affectedNPCs[Random.Range(0, affectedNPCs.Count)];

            Debug.Log($"CharmItem2Radius.ExecuteEffect: Charmed NPC '{charmedNPC.gameObject.name}' selected from {affectedNPCs.Count} affected NPCs");

            // Apply Charm status effect to ALL affected NPCs
            // Duration: 2 player turns (as per specification)
            StatusEffectSystem statusSystem = StatusEffectSystem.Instance;
            if (statusSystem != null)
            {
                foreach (NPCMover npc in affectedNPCs)
                {
                    statusSystem.ApplyStatusEffect(npc, StatusEffectType.Charm, itemData.EffectDuration);
                }

                Debug.Log($"CharmItem2Radius.ExecuteEffect: Applied Charm to {affectedNPCs.Count} NPCs for {itemData.EffectDuration} turns");
            }
            else
            {
                Debug.LogError("CharmItem2Radius.ExecuteEffect: StatusEffectSystem not found");
            }

            // Command the chosen NPC to move toward target
            CommandCharmMovement(charmedNPC, target);
        }

        /// <summary>
        /// Command the charmed NPC to walk toward the target cell.
        /// Other NPCs in the effect radius will be frozen.
        /// </summary>
        private void CommandCharmMovement(NPCMover npc, GridCell targetCell)
        {
            if (npc == null || targetCell == null)
                return;

            // TODO: Integrate with NPC AI system to make the NPC walk toward targetCell
            // For now, this is a placeholder. The StatusEffectSystem will handle stopping other NPCs.

            Debug.Log($"CharmItem2Radius.CommandCharmMovement: Commanding '{npc.gameObject.name}' to move toward {targetCell.gameObject.name}");

            // Example integration point:
            // npc.SetTargetCell(targetCell);
            // or
            // npc.BeginWalkingToward(targetCell);
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
