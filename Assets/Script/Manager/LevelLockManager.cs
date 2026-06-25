using UnityEngine;

/// <summary>
/// Manages level locking and unlocking logic.
/// Use this to check if a level is locked, and to unlock levels based on progression.
/// </summary>
public static class LevelLockManager
{
    /// <summary>
    /// Check if a level is locked
    /// </summary>
    public static bool IsLocked(LevelData level)
    {
        if (level == null) return true;
        return level.isLocked;
    }

    /// <summary>
    /// Unlock a level
    /// </summary>
    public static void UnlockLevel(LevelData level)
    {
        if (level == null) return;
        level.isLocked = false;
        Debug.Log($"Level '{level.levelName}' is now unlocked!");
    }

    /// <summary>
    /// Lock a level
    /// </summary>
    public static void LockLevel(LevelData level)
    {
        if (level == null) return;
        level.isLocked = true;
        Debug.Log($"Level '{level.levelName}' is now locked!");
    }

    /// <summary>
    /// Toggle lock state
    /// </summary>
    public static void ToggleLock(LevelData level)
    {
        if (level == null) return;
        level.isLocked = !level.isLocked;
        Debug.Log($"Level '{level.levelName}' lock state toggled to: {level.isLocked}");
    }

    /// <summary>
    /// Mark level as completed and optionally unlock next level
    /// </summary>
    public static void CompleteLevel(LevelData level, LevelData nextLevel = null)
    {
        if (level == null) return;

        level.hasWon = true;
        Debug.Log($"Level '{level.levelName}' marked as completed!");

        if (nextLevel != null)
        {
            UnlockLevel(nextLevel);
        }
    }
}
