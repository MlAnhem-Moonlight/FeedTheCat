using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Script for individual level buttons in the level selection menu.
/// Handles locked/unlocked visual states and level loading.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Text levelNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sprites")]
    [SerializeField] private Sprite availableSprite;
    [SerializeField] private Sprite lockedSprite;

    [Header("Loading")]
    // Default scene name should match the Gameplay scene in the project.
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;
    [Header("Curent level")]
    [SerializeField] private LevelData levelData;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLevelButtonClicked);
        }
    }

    /// <summary>
    /// Initialize this level button with level data
    /// </summary>
    public void SetLevelData(LevelData level)
    {
        levelData = level;
        LogFilter.LogLevelTransfer($"LevelButton.SetLevelData: assigned level '{level?.levelName}' (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(level)}) to button '{gameObject.name}'");
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Get the level data assigned to this button
    /// </summary>
    public LevelData GetLevelData()
    {
        return levelData;
    }

    private void UpdateButtonVisuals()
    {
        if (levelData == null) return;

        bool isLocked = LevelLockManager.IsLocked(levelData);

        if (buttonImage != null)
        {
            if (isLocked)
            {
                buttonImage.sprite = lockedSprite;
                buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Darken locked button
            }
            else
            {
                buttonImage.sprite = availableSprite;
                buttonImage.color = Color.white;
            }
        }

        if (lockIcon != null)
        {
            lockIcon.enabled = isLocked;
        }

        if (levelNameText != null)
        {
            levelNameText.text = levelData.levelName;
        }

        if (button != null)
        {
            button.interactable = !isLocked;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isLocked ? 0.6f : 1f;
        }
    }

    private void OnLevelButtonClicked()
    {
        if (levelData == null)
        {
            Debug.LogError("LevelButton: levelData is null!");
            return;
        }

        if (LevelLockManager.IsLocked(levelData))
        {
            Debug.LogWarning($"LevelButton: Level '{levelData.levelName}' is locked!");
            return;
        }

        // Show loading screen
        ShowLoadingScreen(true);

        // Prefer to set the requested level via GameManager (centralized intermediary)
        if (GameManager.Instance != null)
        {
            var canonical = GameManager.Instance.GetCanonicalLevel(levelData);
            GameManager.Instance.SetCurrentLevelByReference(canonical, true);
            LogFilter.LogLevelTransfer($"LevelButton -> GameManager queued level '{(canonical!=null?canonical.levelName:"<null>")}' (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(canonical)})");
        }
        else
        {
            // Fallback: queue via SceneTransitionManager if GameManager isn't present
            var toQueue = levelData;
            SceneTransitionManager.GoToGameplay(toQueue, true);
            Debug.Log($"LevelButton: GameManager missing, queued level '{toQueue.levelName}' via SceneTransitionManager.");
        }

        // Load gameplay scene (GameManager.OnSceneLoaded will start the level after scene load)
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void ShowLoadingScreen(bool show)
    {
        if (loadingScreenCanvasGroup != null)
        {
            loadingScreenCanvasGroup.alpha = show ? 1f : 0f;
            loadingScreenCanvasGroup.blocksRaycasts = show;
            loadingScreenCanvasGroup.interactable = show;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnLevelButtonClicked);
        }
    }
}

