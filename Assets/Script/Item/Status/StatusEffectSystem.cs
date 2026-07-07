using UnityEngine;
using System.Collections.Generic;

namespace FeedTheCat.Items
{
    /// <summary>
    /// Manages status effects (Charm, Stun) on NPCs.
    /// Effects are duration-based, counting down ONLY after player moves (via OnPlayerStep event).
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour
    {
        #region Singleton

        private static StatusEffectSystem s_instance;

        public static StatusEffectSystem Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<StatusEffectSystem>();
                    if (s_instance == null)
                    {
                        Debug.LogError("StatusEffectSystem: Instance not found in scene. Create StatusEffectSystem GameObject.");
                    }
                }
                return s_instance;
            }
        }

        #endregion

        #region Data Structure

        /// <summary>
        /// Tracks a single status effect instance on an NPC.
        /// </summary>
        private class StatusEffectInstance
        {
            public NPCMover npcMover;
            public StatusEffectType type;
            public int remainingDuration;

            /// <summary>
            /// Optional destination cell used by Charm: only the ONE "chosen" charmed NPC
            /// has this set (non-null), which tells NPCMover to walk toward it each turn.
            /// All other charmed NPCs have this null and simply stay frozen in place.
            /// </summary>
            public GridCell moveTarget;

            public StatusEffectInstance(NPCMover npc, StatusEffectType effectType, int duration, GridCell target)
            {
                npcMover = npc;
                type = effectType;
                remainingDuration = duration;
                moveTarget = target;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// List of active status effects on NPCs.
        /// </summary>
        private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Debug.LogWarning("StatusEffectSystem: Duplicate instance found. Destroying.");
                Destroy(gameObject);
                return;
            }

            s_instance = this;
        }

        // NOTE: This system intentionally does NOT subscribe to
        // PlayerController.OnPlayerStep directly. If it did, whether NPCMover's
        // movement check ran before or after this system's decrement would depend
        // on unpredictable event-subscription order, causing off-by-one bugs in
        // Stun/Charm duration. Instead, PlayerController explicitly calls Tick()
        // right after OnPlayerStep is invoked (i.e. after NPCs have already moved
        // and read their current status), guaranteeing correct ordering.

        #endregion

        #region Public API

        /// <summary>
        /// Apply a status effect to an NPC.
        /// </summary>
        public void ApplyStatusEffect(NPCMover npc, StatusEffectType effectType, int durationTurns, GridCell moveTarget = null)
        {
            if (npc == null)
            {
                Debug.LogWarning("StatusEffectSystem.ApplyStatusEffect: NPC is null");
                return;
            }

            if (effectType == StatusEffectType.None)
                return;

            if (durationTurns <= 0)
            {
                Debug.LogWarning($"StatusEffectSystem.ApplyStatusEffect: durationTurns <= 0 for {effectType} on {npc.gameObject.name}, ignoring (no effect applied)");
                return;
            }

            // Check if NPC already has this effect
            StatusEffectInstance existingEffect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);

            if (existingEffect != null)
            {
                existingEffect.remainingDuration = durationTurns;
                existingEffect.moveTarget = moveTarget;
                Debug.Log($"StatusEffectSystem: Refreshed {effectType} on {npc.gameObject.name} for {durationTurns} turns");
            }
            else
            {
                StatusEffectInstance newEffect = new StatusEffectInstance(npc, effectType, durationTurns, moveTarget);
                activeEffects.Add(newEffect);
                Debug.Log($"StatusEffectSystem: Applied {effectType} to {npc.gameObject.name} for {durationTurns} turns");
            }

            // Enable visual indicator
            EnableStatusVisual(npc, effectType);
        }

        /// <summary>
        /// Remove a specific status effect from an NPC.
        /// </summary>
        public void RemoveStatusEffect(NPCMover npc, StatusEffectType effectType)
        {
            if (npc == null)
                return;

            StatusEffectInstance effect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);
            if (effect != null)
            {
                activeEffects.Remove(effect);
                Debug.Log($"StatusEffectSystem: Removed {effectType} from {npc.gameObject.name}");
                DisableStatusVisual(npc, effectType);
            }
        }

        /// <summary>
        /// Check if an NPC has a specific status effect that is still active
        /// (i.e. remainingDuration > 0). An effect applied with 0 turns is
        /// treated as never having taken hold.
        /// </summary>
        public bool HasStatusEffect(NPCMover npc, StatusEffectType effectType)
        {
            return activeEffects.Exists(e => e.npcMover == npc && e.type == effectType && e.remainingDuration > 0);
        }

        /// <summary>
        /// Get remaining duration of a status effect.
        /// </summary>
        public int GetEffectDuration(NPCMover npc, StatusEffectType effectType)
        {
            StatusEffectInstance effect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);
            return effect?.remainingDuration ?? 0;
        }

        /// <summary>
        /// Get the move target associated with a status effect on this NPC (Charm only).
        /// Returns null if the NPC doesn't have the effect, or has it but is not the
        /// "chosen" NPC that should walk toward a target (i.e. it should stay frozen).
        /// </summary>
        public GridCell GetEffectMoveTarget(NPCMover npc, StatusEffectType effectType)
        {
            StatusEffectInstance effect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);
            return effect?.moveTarget;
        }

        #endregion

        #region Effect Management

        /// <summary>
        /// Advance all active status effects by one player turn: decrease durations,
        /// and remove/disable-visual any effect that has just expired.
        /// Must be called by PlayerController AFTER OnPlayerStep has been invoked
        /// (i.e. after NPCs have already read their current status and moved),
        /// so an NPC is still correctly treated as Stunned/Charmed during the exact
        /// turn on which its effect finally expires.
        /// </summary>
        public void Tick()
        {
            List<StatusEffectInstance> effectsToRemove = new List<StatusEffectInstance>();

            foreach (var effect in activeEffects)
            {
                if (effect.npcMover == null)
                {
                    effectsToRemove.Add(effect);
                    continue;
                }

                effect.remainingDuration--;

                if (effect.remainingDuration <= 0)
                {
                    effectsToRemove.Add(effect);
                    DisableStatusVisual(effect.npcMover, effect.type);
                    Debug.Log($"StatusEffectSystem: {effect.type} expired on {effect.npcMover.gameObject.name}");
                }
            }

            foreach (var effect in effectsToRemove)
            {
                activeEffects.Remove(effect);
            }
        }

        /// <summary>
        /// Enable the visual indicator for a status effect.
        /// </summary>
        private void EnableStatusVisual(NPCMover npc, StatusEffectType effectType)
        {
            if (npc == null)
                return;

            string childName = effectType switch
            {
                StatusEffectType.Stun => "stun",
                StatusEffectType.Charm => "charm",
                _ => null
            };

            if (string.IsNullOrEmpty(childName))
                return;

            Transform child = npc.transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(true);
                Debug.Log($"StatusEffectSystem: Enabled {childName} visual on {npc.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"StatusEffectSystem: Could not find child '{childName}' on {npc.gameObject.name}");
            }
        }

        /// <summary>
        /// Disable the visual indicator for a status effect.
        /// </summary>
        private void DisableStatusVisual(NPCMover npc, StatusEffectType effectType)
        {
            if (npc == null)
                return;

            string childName = effectType switch
            {
                StatusEffectType.Stun => "stun",
                StatusEffectType.Charm => "charm",
                _ => null
            };

            if (string.IsNullOrEmpty(childName))
                return;

            Transform child = npc.transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Debug.Log($"StatusEffectSystem: Disabled {childName} visual on {npc.gameObject.name}");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log Active Effects")]
        public void LogActiveEffects()
        {
            if (activeEffects.Count == 0)
            {
                Debug.Log("StatusEffectSystem: No active effects");
                return;
            }

            string log = "Active Status Effects:\n";
            foreach (var effect in activeEffects)
            {
                log += $"  {effect.npcMover.gameObject.name}: {effect.type} ({effect.remainingDuration} turns)\n";
            }

            Debug.Log(log);
        }

        #endregion
    }
}