using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages spawning of level buttons for a single page.
/// Instantiates buttons for each available level data.
/// Attach this to a page/panel that contains a grid layout.
/// </summary>
public class LevelButtonManager : MonoBehaviour
{
    [Header("Button Prefab")]
    [SerializeField] private GameObject levelButtonPrefab;

    [Header("Button Configuration")]
    [SerializeField] private Sprite availableButtonSprite;
    [SerializeField] private Sprite lockedButtonSprite;
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private RectTransform pageRect;

    private void Start()
    {
        pageRect = GetComponent<RectTransform>();
        if (levelButtonPrefab == null)
        {
            Debug.LogError("LevelButtonManager: levelButtonPrefab is not assigned!");
            return;
        }
    }

    /// <summary>
    /// Spawn level buttons for the provided level data list
    /// </summary>
    public void SpawnLevelButtons(List<LevelData> levelsToDisplay)
    {
        if (levelsToDisplay == null || levelsToDisplay.Count == 0)
        {
            Debug.LogWarning("LevelButtonManager: No levels to display!");
            return;
        }

        ClearButtons();

        // Spawn all levels provided (no additional limit)
        for (int i = 0; i < levelsToDisplay.Count; i++)
        {
            SpawnButton(levelsToDisplay[i], i);
        }

        Debug.Log($"LevelButtonManager: Spawned {levelsToDisplay.Count} buttons on this page");

        // Force layout update
        if (pageRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
        }
    }

    private void SpawnButton(LevelData levelData, int index)
    {
        GameObject buttonGo = Instantiate(levelButtonPrefab, transform, false);
        buttonGo.name = $"LevelButton_{levelData.levelName}_{index}";

        LevelButton levelButton = buttonGo.GetComponent<LevelButton>();
        if (levelButton != null)
        {
            levelButton.SetLevelData(levelData);
        }
        else
        {
            Debug.LogError($"LevelButtonManager: LevelButton component not found on prefab!");
        }

        spawnedButtons.Add(buttonGo);
    }

    /// <summary>
    /// Clear all spawned buttons
    /// </summary>
    private void ClearButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        spawnedButtons.Clear();
    }

    /// <summary>
    /// Refresh all buttons (useful when levels are unlocked)
    /// </summary>
    public void RefreshButtons()
    {
        foreach (var buttonGo in spawnedButtons)
        {
            if (buttonGo != null)
            {
                LevelButton levelButton = buttonGo.GetComponent<LevelButton>();
                if (levelButton != null)
                {
                    // Refresh the button visual state by re-setting the level data
                    LevelData currentLevel = levelButton.GetLevelData();
                    if (currentLevel != null)
                    {
                        levelButton.SetLevelData(currentLevel);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        ClearButtons();
    }
}
