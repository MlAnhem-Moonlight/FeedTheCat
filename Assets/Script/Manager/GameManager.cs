using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Levels")]
    public List<LevelData> allLevels = new();

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
    }

    public void StartLevel(int index)
    {
        if (index < 0 || index >= allLevels.Count)
            return;

        CurrentLevelIndex = index;

        if (levelManager == null)
            levelManager = FindAnyObjectByType<LevelManager>();

        levelManager.level = allLevels[index];

        levelManager.ApplyLevel();
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