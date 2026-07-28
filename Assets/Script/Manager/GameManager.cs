using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LevelButton;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public PageManager pageManager;

    [Header("Levels")]
    public List<LevelData> allLevels = new();

    // Runtime-selected LevelData. Public so other systems (LevelButton/SceneTransitionManager)
    // can assign the intended LevelData when LevelManager lives in a different scene.
    public LevelData currentLevel;

    public bool winGame = false;

    public int CurrentLevelIndex { get; private set; } = -1;

    public LevelData CurrentLevel =>
        CurrentLevelIndex >= 0 &&
        CurrentLevelIndex < allLevels.Count
            ? allLevels[CurrentLevelIndex]
            : null;

    private LevelManager levelManager;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // Load persistent level states
        SaveManager.LoadAllLevelStates(allLevels);

        // CRITICAL: Ensure first level is always unlocked (for new level lists)
        if (allLevels != null && allLevels.Count > 0)
        {
            allLevels[0].isLocked = false;
        }

        // Ensure there is a valid CurrentLevelIndex after loading states.
        // Default to the first unlocked level if available, otherwise 0.
        if (allLevels != null && allLevels.Count > 0 && CurrentLevelIndex < 0)
        {
            for (int i = 0; i < allLevels.Count; i++)
            {
                if (!allLevels[i].isLocked)
                {
                    CurrentLevelIndex = i;
                    break;
                }
            }

            if (CurrentLevelIndex < 0)
            {
                CurrentLevelIndex = 0;
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void TurnOnLevelChoice()
    {
        levelManager = FindAnyObjectByType<LevelManager>();

        PageManager page = pageManager == null ? FindAnyObjectByType<PageManager>() : pageManager;

        if (page != null)
        {
            page.SetLevels(allLevels);
        }

        // If a level was selected from another scene, ensure LevelManager receives it and ApplyLevel is called.
        if (levelManager != null)
        {
            // Prefer explicit runtime selection (currentLevel) if present, otherwise use index-based CurrentLevel
            if (currentLevel != null)
            {
                levelManager.level = currentLevel;
                //LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied runtime currentLevel '{currentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(currentLevel)})");
                levelManager.ApplyLevel();
                // Clear currentLevel after applying to prevent duplicate application if OnSceneLoaded is called again
                currentLevel = null;
            }
            else if (CurrentLevel != null)
            {
                levelManager.level = CurrentLevel;
                //LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied indexed CurrentLevel '{CurrentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(CurrentLevel)})");
                levelManager.ApplyLevel();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        levelManager = FindAnyObjectByType<LevelManager>();

        PageManager page = pageManager == null ? FindAnyObjectByType<PageManager>() : pageManager;

        if (page != null)
        {
            page.SetLevels(allLevels);
        }

        // If a level was selected from another scene, ensure LevelManager receives it and ApplyLevel is called.
        if (levelManager != null)
        {
            // Prefer explicit runtime selection (currentLevel) if present, otherwise use index-based CurrentLevel
            if (currentLevel != null)
            {
                levelManager.level = currentLevel;
                //LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied runtime currentLevel '{currentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(currentLevel)})");
                levelManager.ApplyLevel();
                // Clear currentLevel after applying to prevent duplicate application if OnSceneLoaded is called again
                currentLevel = null;
            }
            else if (CurrentLevel != null)
            {
                levelManager.level = CurrentLevel;
                //LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied indexed CurrentLevel '{CurrentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(CurrentLevel)})");
                levelManager.ApplyLevel();
            }
        }
    }

    public void StartLevel(int index)
    {
        if (index < 0 || index >= allLevels.Count)
            return;

        CurrentLevelIndex = index;
        // maintain a direct runtime reference for scenes where LevelManager is in a different scene
        currentLevel = allLevels[index];

        if (levelManager == null)
            levelManager = FindAnyObjectByType<LevelManager>();

        if (levelManager != null)
        {
            levelManager.level = allLevels[index];
            levelManager.ApplyLevel();
        }
        else
        {
            // LevelManager may be in another scene; it will pick up GameManager.currentLevel in OnSceneLoaded.
            Debug.LogWarning("GameManager.StartLevel: LevelManager not found in current scene. Level will be applied when Gameplay scene loads.");
        }
    }

    /// <summary>
    /// Returns the canonical LevelData instance from allLevels that matches the supplied LevelData.
    /// Matching prefers object identity then name. If not found, the supplied LevelData is appended to allLevels
    /// and the appended instance is returned. This ensures a single shared LevelData reference is used throughout runtime.
    /// </summary>
    public LevelData GetCanonicalLevel(LevelData level)
    {
        if (level == null) return null;
        int idx = allLevels.FindIndex(l => l == level || (l != null && l.levelName == level.levelName));
        if (idx >= 0) return allLevels[idx];
        // not found: append and return appended instance (best-effort fallback)
        allLevels.Add(level);
        //LogFilter.LogLevelTransfer($"GameManager.GetCanonicalLevel: Appended external LevelData '{level.levelName}' to allLevels at index {allLevels.Count - 1}.");
        return level;
    }

    /// <summary>
    /// Set the current level by LevelData reference (or by matching name). This is used when
    /// the UI/LevelButton exists in a different scene than the LevelManager. It updates the
    /// GameManager's CurrentLevelIndex so the Gameplay scene's LevelManager can consume it.
    /// </summary>
    public void SetCurrentLevelByReference(LevelData level, bool markForLoad = false)
    {
        if (level == null)
        {
            Debug.LogWarning("GameManager.SetCurrentLevelByReference called with null level");
            return;
        }

        // Use canonical LevelData from allLevels so all systems reference the same ScriptableObject instance.
        var canonical = GetCanonicalLevel(level);
        int index = allLevels.FindIndex(l => l == canonical);
        if (index < 0)
        {
            // Shouldn't happen because GetCanonicalLevel will append when missing, but guard just in case.
            allLevels.Add(canonical);
            index = allLevels.Count - 1;
        }

        CurrentLevelIndex = index;
        // store explicit runtime reference so OnSceneLoaded can immediately give it to LevelManager
        currentLevel = allLevels[index];

        Debug.Log($"GameManager: CurrentLevelIndex set to {CurrentLevelIndex} ('{allLevels[CurrentLevelIndex]?.levelName}'). markForLoad={markForLoad}");
    }

    public void WinLevel()
    {
        if (CurrentLevel == null)
        {
            Debug.Log("Win called but CurrentLevel is null");
            return;
        }

        Debug.Log("Win " + CurrentLevel.levelName);

        CurrentLevel.hasWon = true;

        int next = CurrentLevelIndex + 1;

        if (next < allLevels.Count)
            allLevels[next].isLocked = false;

        // Save the updated state to persistent storage
        SaveManager.SaveLevelState(CurrentLevel.levelName, CurrentLevel.hasWon, CurrentLevel.isLocked, CurrentLevel.hasPickup);
        if (next < allLevels.Count)
            SaveManager.SaveLevelState(allLevels[next].levelName, allLevels[next].hasWon, allLevels[next].isLocked, allLevels[next].hasPickup);
        winGame = true;
        FindAnyObjectByType<PageManager>()?.RefreshButtons();
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Load the next level in the level list using SceneTransitionManager for proper scene reload.
    /// Marks current level as won, unlocks next level, and reloads the scene with the next level.
    /// </summary>
    public void NextLevel()
    {
        int currentIndex = CurrentLevelIndex;
        int nextIndex = CurrentLevelIndex + 1;

        // Check if there is a next level
        if (nextIndex >= allLevels.Count)
        {
            Debug.LogWarning("GameManager.NextLevel: No more levels available");
            return;
        }

        // Mark current level as won and save state
        if (CurrentLevel != null)
        {
            CurrentLevel.hasWon = true;
            SaveManager.SaveLevelState(CurrentLevel.levelName, CurrentLevel.hasWon, CurrentLevel.isLocked, CurrentLevel.hasPickup);
            Debug.Log($"GameManager.NextLevel: Marked level '{CurrentLevel.levelName}' as won");
        }

        // Unlock next level and save state
        if (allLevels[nextIndex] != null)
        {
            allLevels[nextIndex].isLocked = false;
            SaveManager.SaveLevelState(allLevels[nextIndex].levelName, allLevels[nextIndex].hasWon, allLevels[nextIndex].isLocked, allLevels[nextIndex].hasPickup);
            Debug.Log($"GameManager.NextLevel: Unlocked level '{allLevels[nextIndex].levelName}'");
        }

        // Update current level index and reference
        CurrentLevelIndex = nextIndex;
        currentLevel = allLevels[nextIndex];

        // Reload scene with next level (same method as LoseLevel)
        if (CurrentLevel != null)
        {
            Debug.Log($"GameManager.NextLevel: Loading next level '{CurrentLevel.levelName}'");
            SceneTransitionManager.RestartLevel(CurrentLevel, true);
        }
        else
        {
            Debug.LogWarning("GameManager.NextLevel: CurrentLevel is null after updating index");
        }
    }

    public void LoseLevel()
    {
        Debug.Log("Lose");

        // If there is no current level, nothing to restart
        if (CurrentLevel == null)
        {
            Debug.LogWarning("LoseLevel called but CurrentLevel is null");
            return;
        }
        winGame = false;
        // Reload the gameplay flow for the current level to ensure proper initialization (spawns, containers, etc.)
        // This uses the SceneTransitionManager which will queue the level and load the loading scene if configured.
        SceneTransitionManager.RestartLevel(CurrentLevel, true);
    }


    [ContextMenu("Reset Level")]
    public void ResetLevel()
    {
        SaveManager.ResetAllLevelStates(allLevels);
    }

    #region Level Shuffling By Difficulty

    /// <summary>
    /// Repeating difficulty pattern used to re-order allLevels:
    /// Easy, Easy, Medium, Easy, Easy, Hard -> then repeats.
    /// (2 easy levels, then 1 medium; after 5 levels total -- 4 easy + 1 medium -- a hard level follows.)
    /// </summary>
    private static readonly string[] DifficultyShufflePattern = { "easy", "easy", "medium", "easy", "easy", "hard" };

    /// <summary>
    /// Rearranges allLevels to follow DifficultyShufflePattern, based on the difficulty parsed
    /// from each LevelData's levelName (format "{difficulty}_levelName", e.g. "Easy_Level1").
    /// If there is no level left of the required difficulty for a slot, a random level is
    /// picked from whatever remains in the pool (regardless of difficulty) so no level is skipped.
    /// </summary>
    [ContextMenu("Shuffle Levels By Difficulty")]
    public void ShuffleLevelsByDifficulty()
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogWarning("GameManager.ShuffleLevelsByDifficulty: allLevels is empty");
            return;
        }

        // Group levels into pools by parsed difficulty, preserving original order within each pool.
        List<LevelData> easyPool = new List<LevelData>();
        List<LevelData> mediumPool = new List<LevelData>();
        List<LevelData> hardPool = new List<LevelData>();
        List<LevelData> otherPool = new List<LevelData>(); // unrecognized/missing difficulty prefix

        foreach (var level in allLevels)
        {
            if (level == null)
                continue;

            switch (GetDifficultyPrefix(level.levelName))
            {
                case "easy":
                    easyPool.Add(level);
                    break;
                case "medium":
                    mediumPool.Add(level);
                    break;
                case "hard":
                    hardPool.Add(level);
                    break;
                default:
                    otherPool.Add(level);
                    break;
            }
        }

        List<LevelData> result = new List<LevelData>(allLevels.Count);

        for (int i = 0; i < allLevels.Count; i++)
        {
            string neededDifficulty = DifficultyShufflePattern[i % DifficultyShufflePattern.Length];

            List<LevelData> targetPool = neededDifficulty switch
            {
                "easy" => easyPool,
                "medium" => mediumPool,
                "hard" => hardPool,
                _ => null
            };

            LevelData chosen = null;

            if (targetPool != null && targetPool.Count > 0)
            {
                chosen = targetPool[0];
                targetPool.RemoveAt(0);
            }
            else
            {
                // Not enough levels of the required difficulty left: pick a random level
                // from whatever is still remaining across all pools.
                List<LevelData> leftovers = new List<LevelData>();
                leftovers.AddRange(easyPool);
                leftovers.AddRange(mediumPool);
                leftovers.AddRange(hardPool);
                leftovers.AddRange(otherPool);

                if (leftovers.Count == 0)
                {
                    Debug.LogWarning($"GameManager.ShuffleLevelsByDifficulty: No levels left to fill slot {i}. Stopping early.");
                    break;
                }

                int randomIndex = Random.Range(0, leftovers.Count);
                chosen = leftovers[randomIndex];

                easyPool.Remove(chosen);
                mediumPool.Remove(chosen);
                hardPool.Remove(chosen);
                otherPool.Remove(chosen);

                Debug.LogWarning($"GameManager.ShuffleLevelsByDifficulty: No '{neededDifficulty}' level left for slot {i}. " +
                    $"Used random fallback level '{chosen.levelName}' instead.");
            }

            result.Add(chosen);
        }

        // Mutate allLevels in place so any external references to this List<LevelData> stay valid.
        allLevels.Clear();
        allLevels.AddRange(result);

        Debug.Log($"GameManager.ShuffleLevelsByDifficulty: Reordered {allLevels.Count} levels using pattern " +
            $"[{string.Join(", ", DifficultyShufflePattern)}] (repeating).");
    }

    /// <summary>
    /// Parse the difficulty prefix from a level name (format "{difficulty}_levelName").
    /// Returns lowercase difficulty string ("easy"/"medium"/"hard"), or empty string if unrecognized.
    /// </summary>
    private string GetDifficultyPrefix(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return string.Empty;

        string[] parts = levelName.Split('_');
        return parts.Length > 0 ? parts[0].ToLower() : string.Empty;
    }

    #endregion
}