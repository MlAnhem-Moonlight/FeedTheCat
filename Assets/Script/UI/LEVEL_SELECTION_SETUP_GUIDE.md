
# Level Selection UI System - Setup Guide

## Overview
This system provides three scripts for managing level selection with pagination:
1. **LevelButton** - Individual level buttons with lock/unlock visual states
2. **LevelButtonManager** - Spawns and manages buttons for a single page
3. **PageManager** - Manages multiple pages and pagination

---

## Script 1: LevelButton.cs

### Purpose
Controls a single level button's appearance and behavior based on lock status.

### Attachment
Attach this script to a **Button** prefab that you'll use as a template.

### Inspector Configuration

**References Section:**
- **Button Image** - The Image component to show available/locked sprite
- **Lock Icon** - Image to show when level is locked (optional)
- **Level Name Text** - Text component to display level name
- **Canvas Group** - CanvasGroup for alpha transparency when locked

**Sprites Section:**
- **Available Sprite** - Button sprite when unlocked
- **Locked Sprite** - Button sprite when locked

**Loading Section:**
- **Gameplay Scene Name** - Name of the scene to load (default: "Gameplay")
- **Loading Screen Canvas Group** - CanvasGroup to show loading screen

### Features
- Displays level name
- Shows lock icon if level is locked
- Disables button interactivity when locked
- Darkens button appearance for locked levels
- Shows loading screen before scene transition
- Automatically assigns level to LevelManager when loading

### Flow
1. Click locked button → Shows warning message
2. Click unlocked button → Shows loading screen → Sets level in LevelManager → Loads scene

---

## Script 2: LevelButtonManager.cs

### Purpose
Manages spawning of multiple level buttons on a single page.

### Attachment
Attach to a **Page GameObject** that contains:
- A **LayoutGroup** (Grid Layout Group, etc.) for arranging buttons
- A **RectTransform** component

### Inspector Configuration

**Button Prefab Section:**
- **Level Button Prefab** - Prefab with LevelButton.cs attached

**Level Data Section:**
- **Level Data List** - List of LevelData ScriptableObjects to display
- **Max Levels Per Page** - Maximum buttons per page (default: 6)

**Button Configuration Section:**
- **Available Button Sprite** - Sprite for unlocked buttons
- **Locked Button Sprite** - Sprite for locked buttons
- **Loading Screen Canvas Group** - CanvasGroup for loading overlay
- **Gameplay Scene Name** - Scene to load

### Methods

#### `SpawnLevelButtons(List<LevelData> levelsToDisplay)`
Spawns buttons for the provided level list and arranges them with the layout group.

```csharp
List<LevelData> pageLevels = allLevels.GetRange(0, 6);
levelButtonManager.SpawnLevelButtons(pageLevels);
```

#### `RefreshButtons()`
Updates all button visuals (useful after unlocking levels).

```csharp
levelButtonManager.RefreshButtons();
```

### Example Usage in Code
```csharp
public class MyLevelLoader : MonoBehaviour
{
	public List<LevelData> allLevels;
	public LevelButtonManager buttonManager;

	void Start()
	{
		buttonManager.SpawnLevelButtons(allLevels);
	}
}
```

---

## Script 3: PageManager.cs

### Purpose
Creates and manages multiple pages of levels with automatic pagination.

### Attachment
Attach to the **HorizontalScrollSnap** container or its parent.

### Inspector Configuration

**References Section:**
- **Horizontal Scroll Snap** - Reference to HorizontalScrollSnap component (auto-found)
- **Page Prefab** - Prefab with LevelButtonManager component
- **Pagination Container** - Parent transform for pagination dots (optional)
- **Pagination Dot Prefab** - Toggle prefab for pagination dots (optional)

**Level Data Section:**
- **All Levels** - All LevelData ScriptableObjects (auto-populated or manually set)
- **Max Levels Per Page** - Levels per page (default: 6)
- **Max Pages** - Maximum pages to create (default: 4)

**Configuration Section:**
- **Level Button Manager Prefab** - If pages don't have the component, create one

### Methods

#### `InitializePages()`
Creates all pages and pagination dots from the level list.

```csharp
pageManager.InitializePages();
```

#### `SetLevels(List<LevelData> levels)`
Set all levels and rebuild pages.

```csharp
pageManager.SetLevels(myLevelsList);
```

#### `AddLevel(LevelData levelData)`
Add a new level and create pages if needed.

```csharp
pageManager.AddLevel(newLevel);
```

#### `RefreshAllPages()`
Update all buttons after level states change.

```csharp
pageManager.RefreshAllPages();
```

#### `OnPageChanged(int newPageIndex)`
Call when page changes (connect to HorizontalScrollSnap event).

```csharp
// In HorizontalScrollSnap.cs or in UI setup
pageManager.OnPageChanged(0);
```

---

## Unity Hierarchy Setup

```
Canvas
├── LevelSelectionScreen
│   ├── HorizontalScrollSnap (PageManager attached here)
│   │   ├── Page_0 (LevelButtonManager)
│   │   │   ├── LevelButton_Level1
│   │   │   ├── LevelButton_Level2
│   │   │   └── ... (up to 6 buttons)
│   │   ├── Page_1 (LevelButtonManager)
│   │   │   └── ...
│   │   └── Page_2
│   │       └── ...
│   ├── PaginationContainer
│   │   ├── Dot_0 (Toggle)
│   │   ├── Dot_1 (Toggle)
│   │   └── Dot_2 (Toggle)
│   └── LoadingScreen (CanvasGroup)
```

---

## Setup Steps in Unity

### Step 1: Create Prefabs

1. **Create LevelButton Prefab:**
   - Create a UI Button GameObject
   - Add LevelButton.cs script
   - Create child objects:
	 - Image (for button background)
	 - Image (for lock icon)
	 - Text (for level name)
   - Assign sprites and references in inspector

2. **Create Page Prefab:**
   - Create a Panel GameObject
   - Add GridLayoutGroup
   - Add LevelButtonManager.cs script
   - Set layout group to 3 columns, 2 rows (for 6 buttons)

3. **Create Pagination Dot Prefab:**
   - Create a UI Toggle
   - Style as a small circle/dot

### Step 2: Setup Level Selection Scene

1. Create Canvas with HorizontalScrollSnap
2. Add PageManager script to HorizontalScrollSnap
3. Assign prefabs in PageManager inspector
4. Assign level list in PageManager
5. Create PaginationContainer and assign to PageManager
6. Create LoadingScreen panel with CanvasGroup

### Step 3: Populate Levels

Option A - Manual:
```csharp
// In PageManager inspector, set "All Levels" list
```

Option B - Programmatic:
```csharp
public class LevelSetup : MonoBehaviour
{
	public PageManager pageManager;
	public LevelData[] levelAssets; // Load from Resources or drag in editor

	void Start()
	{
		pageManager.SetLevels(new List<LevelData>(levelAssets));
	}
}
```

---

## Integration with Level Locking System

### Unlocking Levels

When a player completes a level:

```csharp
// In your level completion handler
LevelLockManager.CompleteLevel(currentLevel, nextLevel);

// Refresh UI to show unlocked level
pageManager.RefreshAllPages();
```

### Example: Level Complete Handler

```csharp
public class GameWinHandler : MonoBehaviour
{
	public LevelManager levelManager;
	public PageManager pageManager;
	public LevelData nextLevelData;
	public string menuSceneName = "LevelSelection";

	public void OnLevelComplete()
	{
		LevelData currentLevel = levelManager.level;
		LevelLockManager.CompleteLevel(currentLevel, nextLevelData);

		// Show next level is unlocked
		pageManager.RefreshAllPages();

		// Return to menu after delay
		StartCoroutine(ReturnToMenuAfterDelay(2f));
	}

	IEnumerator ReturnToMenuAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		SceneManager.LoadScene(menuSceneName);
	}
}
```

---

## Common Issues & Solutions

**Issue:** Buttons don't appear
- Check levelButtonPrefab is assigned
- Verify LayoutGroup is on Page
- Check level data list is not empty

**Issue:** Pagination dots don't work
- Verify paginationContainer is assigned
- Verify paginationDotPrefab is assigned
- Check Toggle component is on dot prefab

**Issue:** Scene doesn't load
- Verify scene name matches (case-sensitive)
- Ensure scene is in Build Settings
- Check LevelManager exists in target scene

**Issue:** Loading screen doesn't show
- Verify loadingScreenCanvasGroup is assigned
- Check CanvasGroup is not set to "Ignore Interactions"
- Ensure LoadingScreen GameObject is active

---

## Tips & Best Practices

1. **Max Levels:** The scripts support up to 20 levels total (4 pages × 6 levels)
2. **Dynamic Adding:** Use `AddLevel()` to add levels after initialization
3. **Refresh After Unlock:** Always call `RefreshAllPages()` when levels are unlocked
4. **Scene Name:** Make sure gameplay scene name matches exactly
5. **Lock Default:** Set `isLocked = true` on levels and unlock only first few
6. **Loading Screen:** Keep it simple - just a semi-transparent overlay with loading text

