using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Script for individual level buttons in the level selection menu.
/// Handles three level states (locked, available, completed) and level loading.
/// </summary>
public class LevelButton : MonoBehaviour
{
    public enum LevelState { Locked, Available, Completed }

    [Header("References")]
    [SerializeField] private Image lvImg;  // Level image (swaps sprite based on state)
    [SerializeField] private Image lockIcon;  // Shows only when state is Locked
    [SerializeField] private Text levelNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sprites (for lvImg)")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite availableSprite;
    [SerializeField] private Sprite completedSprite;

    [Header("Loading")]
    // Default scene name should match the Gameplay scene in the project.
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;

    [Header("Current level")]
    [SerializeField] private LevelData levelData;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLevelButtonClicked);
        }

        // Ensure lvImg is enabled (sometimes it's disabled in hierarchy)
        if (lvImg != null && !lvImg.isActiveAndEnabled)
        {
            lvImg.enabled = true;
            if (lvImg.gameObject != null)
            {
                lvImg.gameObject.SetActive(true);
            }
        }

        // Ensure lockIcon is enabled (will be controlled by visuals)
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(true);

            // Set lockIcon size to 1/9 of button size
            ResizeLockIcon();
        }
    }

    /// <summary>
    /// Resize lockIcon to 1/9 of button size
    /// </summary>
    private void ResizeLockIcon()
    {
        if (lockIcon == null)
            return;

        RectTransform buttonRect = GetComponent<RectTransform>();
        RectTransform lockRect = lockIcon.GetComponent<RectTransform>();

        if (buttonRect == null || lockRect == null)
            return;

        // Button size
        Vector2 buttonSize = buttonRect.rect.size;

        // Lock icon size = 1/9 of button size (1/3 width and 1/3 height)
        Vector2 lockSize = buttonSize / 2f;

        lockRect.sizeDelta = lockSize;

        // Position lock icon at bottom right
        // Anchor at bottom right
        lockRect.anchorMin = new Vector2(1f, 0f);  // Bottom right corner
        lockRect.anchorMax = new Vector2(1f, 0f);  // Bottom right corner

        // Pivot at bottom right so icon aligns to corner
        lockRect.pivot = new Vector2(1f, 0f);

        // Offset from corner (small margin)
        lockRect.anchoredPosition = new Vector2(-5f, 5f);  // 5px padding from edges

        Debug.Log($"LevelButton.ResizeLockIcon: Button size={buttonSize}, Lock icon size={lockSize}, Position=BottomRight");
    }

    /// <summary>
    /// Initialize this level button with level data
    /// </summary>
    public void SetLevelData(LevelData level)
    {
        levelData = level;
        //LogFilter.LogLevelTransfer($"LevelButton.SetLevelData: assigned level '{level?.levelName}' (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(level)}) to button '{gameObject.name}'");
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Get the level data assigned to this button
    /// </summary>
    public LevelData GetLevelData()
    {
        return levelData;
    }

    /// <summary>
    /// Refresh visuals when level state changes (e.g., after winning a level)
    /// </summary>
    public void RefreshVisuals()
    {
        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        if (levelData == null) return;

        // Determine level state
        LevelState state = GetLevelState();

        // Update lvImg sprite based on state and ensure it's enabled
        if (lvImg != null)
        {
            // Ensure lvImg GameObject and Image component are enabled
            if (!lvImg.gameObject.activeSelf)
            {
                lvImg.gameObject.SetActive(true);
            }
            if (!lvImg.enabled)
            {
                lvImg.enabled = true;
            }

            switch (state)
            {
                case LevelState.Locked:
                    lvImg.sprite = lockedSprite;
                    lvImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);  // Darken locked
                    break;

                case LevelState.Available:
                    lvImg.sprite = availableSprite;
                    lvImg.color = Color.white;
                    break;

                case LevelState.Completed:
                    lvImg.sprite = completedSprite;
                    lvImg.color = Color.white;
                    break;
            }
        }

        // lockIcon shows only when state is Locked
        if (lockIcon != null)
        {
            lockIcon.enabled = (state == LevelState.Locked);
        }

        // Update level name text
        if (levelNameText != null)
        {
            levelNameText.text = levelData.levelName;
        }

        // Button interactable only if Available or Completed
        if (button != null)
        {
            button.interactable = (state != LevelState.Locked);
        }

        // Adjust canvas group alpha based on state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = (state == LevelState.Locked) ? 0.6f : 1f;
        }

        Debug.Log($"LevelButton.UpdateButtonVisuals: '{levelData.levelName}' state={state}");
    }

    /// <summary>
    /// Determine the current state of the level based on LevelData flags
    /// </summary>
    private LevelState GetLevelState()
    {
        if (levelData == null)
            return LevelState.Locked;

        // If locked, state is Locked
        if (levelData.isLocked)
            return LevelState.Locked;

        // If not locked and hasWon, state is Completed
        if (levelData.hasWon)
            return LevelState.Completed;

        // If not locked and not won, state is Available
        return LevelState.Available;
    }

    private void OnLevelButtonClicked()
    {
        if (levelData == null)
        {
            Debug.LogError("LevelButton: levelData is null!");
            return;
        }

        // Check if level is locked
        if (levelData.isLocked)
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
            //LogFilter.LogLevelTransfer($"LevelButton -> GameManager queued level '{(canonical!=null?canonical.levelName:"<null>")}' (hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(canonical)})");
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

