using System;
using UnityEngine;
using UnityEngine.UI;
using FeedTheCat;
using FeedTheCat.Rewards;

/// <summary>
/// ItemCollector handles item behavior on the map.
/// When the player enters this cell, logs the reward with difficulty-based rarity.
/// Difficulty is parsed from level name format: "{difficulty}_LevelName" (e.g., "Easy_Level1", "Hard_Stage2")
/// </summary>
public class ItemCollector : MonoBehaviour
{
    [Header("Sprite Assets by Difficulty")]
    public Sprite easySprite;      // Common - Easy level
    public Sprite mediumSprite;    // Uncommon - Medium level
    public Sprite hardSprite;      // Rare - Hard level

    [Header("References")]
    public BoardGenerator boardGenerator;
    public GridCell cell;

    [Header("Position")]
    public int row = 0;
    public int column = 0;

    private RectTransform rectTransform;
    private Image image;
    private bool hasBeenCollected = false;

    #region Properties

    /// <summary>
    /// Get the current difficulty level from the active level.
    /// </summary>
    public Difficulty CurrentDifficulty
    {
        get
        {
            LevelData currentLevel = GameManager.Instance?.CurrentLevel;
            if (currentLevel == null || string.IsNullOrEmpty(currentLevel.levelName))
                return Difficulty.Medium;

            string levelName = currentLevel.levelName;
            string[] parts = levelName.Split('_');

            if (parts.Length > 0)
            {
                return parts[0].ToLower() switch
                {
                    "easy" => Difficulty.Easy,
                    "medium" => Difficulty.Medium,
                    "hard" => Difficulty.Hard,
                    _ => Difficulty.Medium
                };
            }

            return Difficulty.Medium;
        }
    }

    #endregion

    private void Start()
    {
        // Initialize grid reference if not assigned
        if (boardGenerator == null)
            boardGenerator = FindAnyObjectByType<BoardGenerator>();

        // Get the cell at this position
        if (boardGenerator != null)
        {
            cell = GetCellAtPosition(row, column);
        }

        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        // Set sprite and appearance based on difficulty
        SetSpriteByDifficulty();

        // Mark this cell as having an item
        if (cell != null)
        {
            cell.occupiedByItem = true;
        }
        RewardGenerator rewardGenerator = FindAnyObjectByType<RewardGenerator>();
        rewardGenerator.itemCollector = this;
        RewardScreenManager rewardScreenManager = FindAnyObjectByType<RewardScreenManager>();
        rewardScreenManager.SetupRewardScreen();
    }

    private void OnDestroy()
    {
        // Clear item occupancy when destroyed
        if (cell != null)
        {
            cell.occupiedByItem = false;
        }
    }

    /// <summary>
    /// Set item sprite based on level difficulty
    /// </summary>
    private void SetSpriteByDifficulty()
    {
        if (image == null)
            return;

        string difficultyStr = GetDifficultyFromLevelName();

        Sprite spriteToUse = null;
        switch (difficultyStr.ToLower())
        {
            case "easy":
                spriteToUse = easySprite;
                break;
            case "medium":
                spriteToUse = mediumSprite;
                break;
            case "hard":
                spriteToUse = hardSprite;
                break;
            default:
                spriteToUse = mediumSprite; // default fallback
                break;
        }

        if (spriteToUse != null)
        {
            image.sprite = spriteToUse;
            Debug.Log($"ItemCollector: Set sprite to '{spriteToUse.name}' for difficulty '{difficultyStr}'");
        }
        else
        {
            Debug.LogWarning($"ItemCollector: No sprite assigned for difficulty '{difficultyStr}'");
        }
    }

    /// <summary>
    /// Extract difficulty string from level name (format: "{difficulty}_LevelName")
    /// </summary>
    private string GetDifficultyFromLevelName()
    {
        LevelData currentLevel = GameManager.Instance?.CurrentLevel;
        if (currentLevel == null || string.IsNullOrEmpty(currentLevel.levelName))
        {
            return "Medium"; // default fallback
        }

        string levelName = currentLevel.levelName;
        string[] parts = levelName.Split('_');

        if (parts.Length > 0)
        {
            return parts[0];
        }

        return "Medium"; // default fallback
    }

    /// <summary>
    /// Called when player enters this cell (from GridCell)
    /// </summary>
    public void OnPlayerEnter()
    {
        if (hasBeenCollected) return;

        hasBeenCollected = true;

        // Parse difficulty from level name
        string rarity = GetRarityFromDifficulty();

        // Log reward with rarity
        Debug.Log($"nhận quà + {rarity}");
        RewardScreenManager rewardScreenManager = FindAnyObjectByType<RewardScreenManager>();
        rewardScreenManager?.rewardBg.SetActive(true);
        // Destroy the item
        Destroy(gameObject);
    }

    /// <summary>
    /// Extract difficulty from level name and return rarity string.
    /// Level name format: "{difficulty}_LevelName" (e.g., "Easy_Level1", "Hard_Stage2")
    /// </summary>
    private string GetRarityFromDifficulty()
    {
        // Get level name from GameManager
        LevelData currentLevel = GameManager.Instance?.CurrentLevel;
        if (currentLevel == null)
        {
            Debug.LogWarning("ItemCollector: Could not get current level from GameManager");
            return "Uncommon"; // Default fallback
        }

        string levelName = currentLevel.levelName;

        // Parse difficulty from level name (format: "{difficulty}_LevelName")
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogWarning("ItemCollector: Level name is empty");
            return "Uncommon"; // Default fallback
        }

        // Split by underscore
        string[] parts = levelName.Split('_');
        if (parts.Length < 1)
        {
            Debug.LogWarning("ItemCollector: Could not parse level name");
            return "Uncommon"; // Default fallback
        }

        string difficultyStr = parts[0].ToLower();

        // Map difficulty to rarity
        string rarity = difficultyStr switch
        {
            "easy" => "Common",
            "medium" => "Uncommon",
            "hard" => "Rare",
            _ => "Uncommon" // Default fallback
        };

        return rarity;
    }

    /// <summary>
    /// Get GridCell at the specified row and column
    /// </summary>
    private GridCell GetCellAtPosition(int r, int c)
    {
        if (boardGenerator == null || boardGenerator.grid == null) return null;

        int columns = boardGenerator.columns;
        int index = r * columns + c;

        if (index >= 0 && index < boardGenerator.grid.transform.childCount)
        {
            var child = boardGenerator.grid.transform.GetChild(index);
            if (child != null) return child.GetComponent<GridCell>();
        }

        return null;
    }
}
