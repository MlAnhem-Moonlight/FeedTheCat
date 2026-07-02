using UnityEngine;
using System.Collections.Generic;
using FeedTheCat.Items;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Utility class for radius-based NPC queries.
    /// Provides methods to find all NPCs within a specified effect radius from a target cell.
    /// </summary>
    public static class RadiusHelper
    {
        #region Public API

        /// <summary>
        /// Get all NPCs within the specified radius from a target GridCell.
        /// </summary>
        /// <param name="targetCell">The center cell for the radius calculation.</param>
        /// <param name="radiusType">The type of radius (Radius1, Radius2, Radius3, or EntireMap).</param>
        /// <returns>List of NPCMover objects within range.</returns>
        public static List<NPCMover> GetNPCsInRadius(GridCell targetCell, EffectRadiusType radiusType)
        {
            List<NPCMover> affectedNPCs = new List<NPCMover>();

            if (targetCell == null)
            {
                Debug.LogWarning("RadiusHelper.GetNPCsInRadius: targetCell is null");
                return affectedNPCs;
            }

            // Get the radius in tiles
            int radius = GetRadiusValue(radiusType);

            // Find all NPCs in the scene
            NPCMover[] allNPCs = Object.FindObjectsByType<NPCMover>(FindObjectsSortMode.None);

            if (allNPCs.Length == 0)
            {
                Debug.LogWarning("RadiusHelper.GetNPCsInRadius: No NPCs found in scene");
                return affectedNPCs;
            }

            // Check distance for each NPC
            foreach (NPCMover npc in allNPCs)
            {
                if (npc == null)
                    continue;

                if (IsNPCInRadius(targetCell, npc, radius))
                {
                    affectedNPCs.Add(npc);
                }
            }

            return affectedNPCs;
        }

        /// <summary>
        /// Check if a specific NPC is within radius of a target cell.
        /// </summary>
        /// <param name="targetCell">The center cell for radius calculation.</param>
        /// <param name="npc">The NPC to check.</param>
        /// <param name="radiusType">The radius type to use.</param>
        /// <returns>True if NPC is within range, false otherwise.</returns>
        public static bool IsNPCInRadius(GridCell targetCell, NPCMover npc, EffectRadiusType radiusType)
        {
            int radius = GetRadiusValue(radiusType);
            return IsNPCInRadius(targetCell, npc, radius);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Convert EffectRadiusType enum to numeric radius value in tiles.
        /// </summary>
        private static int GetRadiusValue(EffectRadiusType radiusType)
        {
            return radiusType switch
            {
                EffectRadiusType.Radius1 => 1,
                EffectRadiusType.Radius2 => 2,
                EffectRadiusType.Radius3 => 3,
                EffectRadiusType.EntireMap => int.MaxValue, // Affects all NPCs
                _ => 0
            };
        }

        /// <summary>
        /// Check if NPC is within a numeric radius (Manhattan distance).
        /// </summary>
        private static bool IsNPCInRadius(GridCell targetCell, NPCMover npc, int radiusInTiles)
        {
            if (targetCell == null || npc == null)
                return false;

            // Use Manhattan distance for grid-based calculations
            Vector3 targetPos = targetCell.GetComponent<RectTransform>()?.anchoredPosition ?? targetCell.transform.position;
            Vector3 npcPos = npc.GetComponent<RectTransform>()?.anchoredPosition ?? npc.transform.position;

            float distance = Vector3.Distance(targetPos, npcPos);

            // Assume each grid cell is 1 unit; adjust if your grid uses different scaling
            float maxDistance = radiusInTiles * 100f; // Convert tiles to approximate world distance

            return distance <= maxDistance;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Log all NPCs found within radius (for debugging).
        /// </summary>
        public static void DebugLogNPCsInRadius(GridCell targetCell, EffectRadiusType radiusType)
        {
            List<NPCMover> npcs = GetNPCsInRadius(targetCell, radiusType);

            string log = $"RadiusHelper: Found {npcs.Count} NPCs in {radiusType} radius:\n";
            foreach (var npc in npcs)
            {
                log += $"  - {npc.gameObject.name}\n";
            }

            Debug.Log(log);
        }

        #endregion
    }
}
