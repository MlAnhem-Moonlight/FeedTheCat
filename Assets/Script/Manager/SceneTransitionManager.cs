using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Utility class for scene management and level transitions.
/// Optional helper script to make scene management easier.
/// </summary>
public static class SceneTransitionManager
{
    private static LevelData nextLevelToLoad;
    // Default loading scene name should match the actual gameplay scene name used elsewhere
    private static string loadingSceneName = "GamePlay";

    /// <summary>
    /// Transition to level selection scene
    /// </summary>
    public static void GoToLevelSelection()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelection");
    }

    /// <summary>
    /// Transition to gameplay with specified level
    /// </summary>
    public static void GoToGameplay(LevelData level, bool useLoadingScene = true)
    {
        if (level == null)
        {
            Debug.LogError("SceneTransitionManager: level is null!");
            return;
        }

        Time.timeScale = 1f;
        nextLevelToLoad = level;

        if (useLoadingScene)
        {
            SceneManager.LoadScene(loadingSceneName);
        }
        else
        {
            ApplyLevelAndLoad(level);
        }
    }

    /// <summary>
    /// Restart current level
    /// </summary>
    public static void RestartLevel(LevelData currentLevel, bool useLoadingScene = true)
    {
        GoToGameplay(currentLevel, useLoadingScene);
    }

    /// <summary>
    /// Go to next level (if provided)
    /// </summary>
    public static void GoToNextLevel(LevelData nextLevel)
    {
        GoToGameplay(nextLevel);
    }

    /// <summary>
    /// Apply level and load gameplay scene (called from loading scene)
    /// </summary>
    public static void ApplyLevelAndLoad(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("SceneTransitionManager: Cannot apply null level!");
            return;
        }

        LevelManager levelManager = Object.FindAnyObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.level = level;
            levelManager.ApplyLevel();
        }

        SceneManager.LoadScene("Gameplay");
    }

    /// <summary>
    /// Get the level that should be loaded
    /// </summary>
    public static LevelData GetNextLevelToLoad()
    {
        return nextLevelToLoad;
    }

    /// <summary>
    /// Clear any queued next level. Call this after consuming nextLevelToLoad to avoid stale state.
    /// </summary>
    public static void ClearNextLevelToLoad()
    {
        nextLevelToLoad = null;
    }

    /// <summary>
    /// Set custom loading scene name
    /// </summary>
    public static void SetLoadingSceneName(string sceneName)
    {
        loadingSceneName = sceneName;
    }

    /// <summary>
    /// Show loading screen with message
    /// </summary>
    public static void ShowLoadingScreen(CanvasGroup loadingCanvasGroup, string message = "Loading...")
    {
        if (loadingCanvasGroup == null) return;

        loadingCanvasGroup.alpha = 1f;
        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;

        if (message != null)
        {
            Debug.Log($"Loading: {message}");
        }
    }

    /// <summary>
    /// Hide loading screen
    /// </summary>
    public static void HideLoadingScreen(CanvasGroup loadingCanvasGroup)
    {
        if (loadingCanvasGroup == null) return;

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.interactable = false;
    }
}
