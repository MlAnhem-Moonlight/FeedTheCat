using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages persistent storage of level states (hasWon, isLocked, hasPickup)
/// Uses PlayerPrefs for cross-session persistence
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string LEVEL_STATE_PREFIX = "Level_";
    private const string LEVEL_WON_SUFFIX = "_hasWon";
    private const string LEVEL_LOCKED_SUFFIX = "_isLocked";
    private const string LEVEL_PICKUP_SUFFIX = "_hasPickup";

    /// <summary>
    /// Save the state of a single level (by levelName)
    /// </summary>
    public static void SaveLevelState(string levelName, bool hasWon, bool isLocked, bool hasPickup = false)
    {
        string wonKey = LEVEL_STATE_PREFIX + levelName + LEVEL_WON_SUFFIX;
        string lockedKey = LEVEL_STATE_PREFIX + levelName + LEVEL_LOCKED_SUFFIX;
        string pickupKey = LEVEL_STATE_PREFIX + levelName + LEVEL_PICKUP_SUFFIX;

        PlayerPrefs.SetInt(wonKey, hasWon ? 1 : 0);
        PlayerPrefs.SetInt(lockedKey, isLocked ? 1 : 0);
        PlayerPrefs.SetInt(pickupKey, hasPickup ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"SaveManager: Saved Level '{levelName}' - hasWon={hasWon}, isLocked={isLocked}, hasPickup={hasPickup}");
    }

    /// <summary>
    /// Load the state of a single level (by levelName)
    /// </summary>
    public static void LoadLevelState(string levelName, LevelData level)
    {
        if (level == null) return;

        string wonKey = LEVEL_STATE_PREFIX + levelName + LEVEL_WON_SUFFIX;
        string lockedKey = LEVEL_STATE_PREFIX + levelName + LEVEL_LOCKED_SUFFIX;
        string pickupKey = LEVEL_STATE_PREFIX + levelName + LEVEL_PICKUP_SUFFIX;

        // Default: levels are locked, not won, no pickup
        level.hasWon = PlayerPrefs.GetInt(wonKey, 0) == 1;
        level.isLocked = PlayerPrefs.GetInt(lockedKey, 1) == 1;
        level.hasPickup = PlayerPrefs.GetInt(pickupKey, 0) == 1;

        Debug.Log($"SaveManager: Loaded Level '{levelName}' - hasWon={level.hasWon}, isLocked={level.isLocked}, hasPickup={level.hasPickup}");
    }

    /// <summary>
    /// Load all level states from persistent storage
    /// </summary>
    public static void LoadAllLevelStates(List<LevelData> allLevels)
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null)
            {
                LoadLevelState(allLevels[i].levelName, allLevels[i]);
            }
        }
    }

    /// <summary>
    /// Save all level states
    /// </summary>
    public static void SaveAllLevelStates(List<LevelData> allLevels)
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null)
            {
                SaveLevelState(allLevels[i].levelName, allLevels[i].hasWon, allLevels[i].isLocked, allLevels[i].hasPickup);
            }
        }
    }

    /// <summary>
    /// Reset all saved level states (for debugging/new game).
    /// The first level (index 0) is unlocked by default, all others are locked.
    /// </summary>
    public static void ResetAllLevelStates(List<LevelData> allLevels)
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null)
            {
                string levelName = allLevels[i].levelName;
                string wonKey = LEVEL_STATE_PREFIX + levelName + LEVEL_WON_SUFFIX;
                string lockedKey = LEVEL_STATE_PREFIX + levelName + LEVEL_LOCKED_SUFFIX;
                string pickupKey = LEVEL_STATE_PREFIX + levelName + LEVEL_PICKUP_SUFFIX;

                // Clear all saved states from PlayerPrefs
                PlayerPrefs.DeleteKey(wonKey);
                PlayerPrefs.DeleteKey(lockedKey);
                PlayerPrefs.DeleteKey(pickupKey);

                // Set in-memory state: first level unlocked, others locked
                if (i == 0)
                {
                    allLevels[i].isLocked = false;  // Unlock first level
                    allLevels[i].hasWon = false;
                    allLevels[i].hasPickup = false;
                }
                else
                {
                    allLevels[i].isLocked = true;   // Lock all other levels
                    allLevels[i].hasWon = false;
                    allLevels[i].hasPickup = false;
                }
            }
        }

        PlayerPrefs.Save();
        Debug.Log("SaveManager: Reset all level states (Level 0 unlocked, all others locked)");
    }
}
