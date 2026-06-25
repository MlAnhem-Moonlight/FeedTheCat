# Level Selection System - Implementation Summary

## What Was Created

You now have a complete **Level Selection UI System** with these components:

### 1️⃣ LevelButton.cs
**Purpose:** Individual level button controller  
**Features:**
- Displays level name
- Shows available/locked visual states (2 sprites)
- Disables button when locked
- Shows loading screen overlay during transition
- Automatically assigns level to LevelManager
- Integrates with LevelLockManager for lock state

**Attach To:** Button prefab in level selection

### 2️⃣ LevelButtonManager.cs
**Purpose:** Spawns and manages buttons for a single page  
**Features:**
- Spawns buttons from prefab (max 6 per page)
- Auto-arranges with layout groups
- Destroys buttons when page changes
- Refreshes button visuals after unlock

**Attach To:** Page panel/content area

### 3️⃣ PageManager.cs
**Purpose:** Manages multiple pages with pagination  
**Features:**
- Creates pages automatically based on level count
- Generates pagination dots (toggles)
- Distributes levels across pages (6 per page default)
- Max 4 pages (20 levels total)
- Adds/removes pages dynamically
- Updates pagination when page changes
- Supports dynamic level addition

**Attach To:** HorizontalScrollSnap container

### 4️⃣ SceneTransitionManager.cs (Bonus)
**Purpose:** Utility for scene transitions  
**Methods:**
- `GoToGameplay(LevelData level)` - Load gameplay
- `GoToLevelSelection()` - Return to menu
- `RestartLevel(LevelData level)` - Restart current
- `ShowLoadingScreen()` / `HideLoadingScreen()` - Control loading UI

---

## How It Works

### User Flow
```
Level Selection Screen
	↓
Player clicks level button
	↓
Check if locked
	├─ YES → Show warning, do nothing
	└─ NO → Show loading screen
		↓
	Set level in LevelManager
		↓
	Load Gameplay scene
		↓
	Level plays
		↓
	Level completed
		↓
	Unlock next level
		↓
	Refresh UI
		↓
	Return to level selection
```

### Technical Flow
```
PageManager.Initialize()
	↓
	Creates pages based on level count
		↓
		Page 1 → LevelButtonManager
					↓
					Spawns buttons 1-6
		Page 2 → LevelButtonManager
					↓
					Spawns buttons 7-12
		Page 3 → LevelButtonManager
					↓
					Spawns buttons 13-18
	↓
	Creates pagination dots
	↓
	HorizontalScrollSnap arranges pages
```

---

## Integration with Existing Systems

### LevelLockManager (Previously Created)
```csharp
// Check lock status
bool isLocked = LevelLockManager.IsLocked(level);

// Unlock level
LevelLockManager.UnlockLevel(level);

// Complete level and unlock next
LevelLockManager.CompleteLevel(currentLevel, nextLevel);
```

### LevelData
```csharp
[Serializable]
public class LevelData : ScriptableObject
{
	public string levelName;
	public bool isLocked = true;  // ← NEW!
	// ... other properties
}
```

### LevelManager (Existing)
```csharp
// LevelButton automatically sets this
levelManager.level = selectedLevel;

// Then scene loads and calls
levelManager.ApplyLevel();
```

---

## Setup Steps (Quick)

### Step 1: Create Prefabs
- **LevelButton Prefab**: Button + Image + Text + LevelButton.cs
- **Page Prefab**: Panel + GridLayout + LevelButtonManager.cs
- **Dot Prefab**: Toggle (styled as circle)

### Step 2: Setup Scene
- Add HorizontalScrollSnap to Canvas
- Attach PageManager to it
- Add PaginationContainer for dots
- Create LoadingScreen panel

### Step 3: Configure
- Assign prefabs in PageManager
- Set level list (6 max per page)
- Assign loading screen reference

### Step 4: Populate Levels
```csharp
pageManager.SetLevels(myLevelsList);
```

---

## Key Configuration

### Customize Button Behavior
```csharp
// In LevelButton.cs OnLevelButtonClicked()
// - Change scene name
// - Add custom effects
// - Add analytics tracking
```

### Customize Page Layout
```csharp
// In LevelButtonManager
maxLevelsPerPage = 4; // 2x2 grid instead of 3x2
```

### Customize Pagination
```csharp
// In PageManager
maxPages = 10;  // Support more pages
```

---

## Properties Summary

### LevelButton Inspector
- Button Image (Image)
- Lock Icon (Image)
- Level Name Text (Text)
- Canvas Group (CanvasGroup)
- Available Sprite
- Locked Sprite
- Gameplay Scene Name (string)
- Loading Screen Canvas Group (CanvasGroup)

### LevelButtonManager Inspector
- Level Button Prefab (GameObject)
- Max Levels Per Page (int, default 6)
- Available Button Sprite (Sprite)
- Locked Button Sprite (Sprite)
- Loading Screen Canvas Group (CanvasGroup)
- Gameplay Scene Name (string)

### PageManager Inspector
- Horizontal Scroll Snap (HorizontalScrollSnap)
- Page Prefab (GameObject)
- Pagination Container (Transform)
- Pagination Dot Prefab (Toggle)
- All Levels (List<LevelData>)
- Max Levels Per Page (int, default 6)
- Max Pages (int, default 4)

---

## Visual States

### Level Button States

**Unlocked:**
- Available sprite displayed
- Full opacity (1.0)
- Button interactive (enabled)
- Lock icon hidden

**Locked:**
- Locked sprite displayed
- Reduced opacity (0.6)
- Button non-interactive (disabled)
- Lock icon visible

---

## Example Workflow

### 1. Initialize at Menu Start
```csharp
public class MenuController : MonoBehaviour
{
	public PageManager pageManager;
	public List<LevelData> levels;

	void Start()
	{
		pageManager.SetLevels(levels);
	}
}
```

### 2. Player Wins Level
```csharp
public class GameWinHandler : MonoBehaviour
{
	public void OnWin()
	{
		LevelData current = levelManager.level;
		LevelData next = GetNextLevel();

		// Unlock next level
		LevelLockManager.CompleteLevel(current, next);

		// Go back to menu
		StartCoroutine(ReturnToMenu());
	}

	IEnumerator ReturnToMenu()
	{
		yield return new WaitForSeconds(2);
		SceneTransitionManager.GoToLevelSelection();
	}
}
```

### 3. Return to Menu, UI Refreshes
```csharp
// In menu scene OnEnable
public class MenuScene : MonoBehaviour
{
	public PageManager pageManager;

	void OnEnable()
	{
		// Refresh all button states
		pageManager.RefreshAllPages();
	}
}
```

---

## File Structure

```
Assets/Script/
├── UI/
│   ├── LevelButton.cs
│   ├── LevelButtonManager.cs
│   ├── PageManager.cs
│   ├── LEVEL_SELECTION_SETUP_GUIDE.md
│   └── QUICK_REFERENCE.md
├── Manager/
│   ├── LevelData.cs (updated: added isLocked)
│   ├── LevelLockManager.cs
│   ├── SceneTransitionManager.cs
│   └── LevelManager.cs (existing)
└── ... other scripts
```

---

## Next Steps

1. ✅ Create the prefabs in your scene
2. ✅ Configure PageManager with your levels
3. ✅ Set first level as unlocked (isLocked = false)
4. ✅ Test level selection and loading
5. ✅ Connect win conditions to level unlock logic
6. ✅ Add any custom effects or analytics

---

## Troubleshooting

**Buttons don't appear?**
- Check levelButtonPrefab is assigned
- Verify level list has items
- Ensure LayoutGroup is on Page

**Pagination dots don't work?**
- Check paginationContainer is assigned
- Verify paginationDotPrefab is assigned

**Scene doesn't load?**
- Check scene is in Build Settings
- Verify scene name matches (case-sensitive)
- Ensure LevelManager exists in target scene

**Buttons not updating after unlock?**
- Call pageManager.RefreshAllPages() after completing level

---

**You're all set!** 🎮 Your level selection system is ready to integrate.
