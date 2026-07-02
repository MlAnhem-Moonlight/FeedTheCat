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

            public StatusEffectInstance(NPCMover npc, StatusEffectType effectType, int duration)
            {
                npcMover = npc;
                type = effectType;
                remainingDuration = duration;
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

        private void OnEnable()
        {
            // Subscribe to player step event
            // PlayerController.OnPlayerStep += OnPlayerStep;
        }

        private void OnDisable()
        {
            // PlayerController.OnPlayerStep -= OnPlayerStep;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Apply a status effect to an NPC.
        /// </summary>
        public void ApplyStatusEffect(NPCMover npc, StatusEffectType effectType, int durationTurns)
        {
            if (npc == null)
            {
                Debug.LogWarning("StatusEffectSystem.ApplyStatusEffect: NPC is null");
                return;
            }

            if (effectType == StatusEffectType.None)
                return;

            // Check if NPC already has this effect
            StatusEffectInstance existingEffect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);

            if (existingEffect != null)
            {
                existingEffect.remainingDuration = durationTurns;
                Debug.Log($"StatusEffectSystem: Refreshed {effectType} on {npc.gameObject.name} for {durationTurns} turns");
            }
            else
            {
                StatusEffectInstance newEffect = new StatusEffectInstance(npc, effectType, durationTurns);
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
        /// Check if an NPC has a specific status effect.
        /// </summary>
        public bool HasStatusEffect(NPCMover npc, StatusEffectType effectType)
        {
            return activeEffects.Exists(e => e.npcMover == npc && e.type == effectType);
        }

        /// <summary>
        /// Get remaining duration of a status effect.
        /// </summary>
        public int GetEffectDuration(NPCMover npc, StatusEffectType effectType)
        {
            StatusEffectInstance effect = activeEffects.Find(e => e.npcMover == npc && e.type == effectType);
            return effect?.remainingDuration ?? 0;
        }

        #endregion

        #region Effect Management

        /// <summary>
        /// Called after each player move to decrease effect durations.
        /// </summary>
        private void OnPlayerStep()
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
