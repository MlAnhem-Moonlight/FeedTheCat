using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages persistent storage of level states (hasWon, isLocked)
/// Uses PlayerPrefs for cross-session persistence
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string LEVEL_STATE_PREFIX = "Level_";
    private const string LEVEL_WON_SUFFIX = "_hasWon";
    private const string LEVEL_LOCKED_SUFFIX = "_isLocked";

    /// <summary>
    /// Save the state of a single level
    /// </summary>
    public static void SaveLevelState(int levelIndex, bool hasWon, bool isLocked)
    {
        string wonKey = LEVEL_STATE_PREFIX + levelIndex + LEVEL_WON_SUFFIX;
        string lockedKey = LEVEL_STATE_PREFIX + levelIndex + LEVEL_LOCKED_SUFFIX;

        PlayerPrefs.SetInt(wonKey, hasWon ? 1 : 0);
        PlayerPrefs.SetInt(lockedKey, isLocked ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"SaveManager: Saved Level {levelIndex} - hasWon={hasWon}, isLocked={isLocked}");
    }

    /// <summary>
    /// Load the state of a single level
    /// </summary>
    public static void LoadLevelState(int levelIndex, LevelData level)
    {
        if (level == null) return;

        string wonKey = LEVEL_STATE_PREFIX + levelIndex + LEVEL_WON_SUFFIX;
        string lockedKey = LEVEL_STATE_PREFIX + levelIndex + LEVEL_LOCKED_SUFFIX;

        // Default: first level is unlocked, others are locked
        bool defaultLocked = levelIndex > 0;

        level.hasWon = PlayerPrefs.GetInt(wonKey, 0) == 1;
        level.isLocked = PlayerPrefs.GetInt(lockedKey, defaultLocked ? 1 : 0) == 1;

        Debug.Log($"SaveManager: Loaded Level {levelIndex} - hasWon={level.hasWon}, isLocked={level.isLocked}");
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
            LoadLevelState(i, allLevels[i]);
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
            SaveLevelState(i, allLevels[i].hasWon, allLevels[i].isLocked);
        }
    }

    /// <summary>
    /// Reset all saved level states (for debugging/new game)
    /// </summary>
    public static void ResetAllLevelStates(List<LevelData> allLevels)
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        for (int i = 0; i < allLevels.Count; i++)
        {
            string wonKey = LEVEL_STATE_PREFIX + i + LEVEL_WON_SUFFIX;
            string lockedKey = LEVEL_STATE_PREFIX + i + LEVEL_LOCKED_SUFFIX;
            PlayerPrefs.DeleteKey(wonKey);
            PlayerPrefs.DeleteKey(lockedKey);
        }

        PlayerPrefs.Save();
        Debug.Log("SaveManager: Reset all level states");
    }
}
