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
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;

    private LevelData levelData;
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

        // Find and configure LevelManager
        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.level = levelData;
        }
        else
        {
            Debug.LogWarning("LevelButton: LevelManager not found in scene!");
        }

        // Load gameplay scene
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

