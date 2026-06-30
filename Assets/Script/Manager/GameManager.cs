using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Levels")]
    public List<LevelData> allLevels = new();

    // Runtime-selected LevelData. Public so other systems (LevelButton/SceneTransitionManager)
    // can assign the intended LevelData when LevelManager lives in a different scene.
    public LevelData currentLevel;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        levelManager = FindAnyObjectByType<LevelManager>();

        PageManager page = FindAnyObjectByType<PageManager>();

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
                LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied runtime currentLevel '{currentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(currentLevel)})");
                levelManager.ApplyLevel();
            }
            else if (CurrentLevel != null)
            {
                levelManager.level = CurrentLevel;
                LogFilter.LogLevelTransfer($"GameManager.OnSceneLoaded: Applied indexed CurrentLevel '{CurrentLevel.levelName}' to LevelManager. (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(CurrentLevel)})");
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
        LogFilter.LogLevelTransfer($"GameManager.GetCanonicalLevel: Appended external LevelData '{level.levelName}' to allLevels at index {allLevels.Count - 1}.");
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
        SaveManager.SaveLevelState(CurrentLevelIndex, CurrentLevel.hasWon, CurrentLevel.isLocked);
        if (next < allLevels.Count)
            SaveManager.SaveLevelState(next, allLevels[next].hasWon, allLevels[next].isLocked);

        FindAnyObjectByType<PageManager>()?.RefreshButtons();
        SceneManager.LoadScene("TestChoiceLevel");
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

        // Reload the gameplay flow for the current level to ensure proper initialization (spawns, containers, etc.)
        // This uses the SceneTransitionManager which will queue the level and load the loading scene if configured.
        SceneTransitionManager.RestartLevel(CurrentLevel, true);
    }
}