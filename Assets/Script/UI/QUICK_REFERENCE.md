# Level Selection System - Quick Reference

## Files Created

### UI Scripts
1. **Assets/Script/UI/LevelButton.cs** - Individual level button controller
2. **Assets/Script/UI/LevelButtonManager.cs** - Page button spawner
3. **Assets/Script/UI/PageManager.cs** - Multi-page manager with pagination
4. **Assets/Script/UI/LEVEL_SELECTION_SETUP_GUIDE.md** - Detailed setup guide

### Manager Scripts
1. **Assets/Script/Manager/LevelLockManager.cs** - Level lock/unlock utilities (created previously)
2. **Assets/Script/Manager/SceneTransitionManager.cs** - Scene loading helper

### Updated Scripts
1. **Assets/Script/Manager/LevelData.cs** - Added `isLocked` property

---

## Key Features

✅ **Level Button (LevelButton.cs)**
- Display level name
- Show available/locked sprites
- Disable interaction when locked
- Show loading screen before transition
- Auto-assign level to LevelManager

✅ **Page Manager (LevelButtonManager.cs)**
- Spawn buttons for a page
- Auto-arrange with layout groups
- Refresh button states on demand
- Support max 6 levels per page

✅ **Pagination (PageManager.cs)**
- Create multiple pages automatically
- Generate pagination dots (toggles)
- Max 4 pages (20 levels total)
- Handle page transitions
- Add/remove levels dynamically

---

## Integration Points

### With LevelLockManager
```csharp
// Check if locked
bool locked = LevelLockManager.IsLocked(level);

// Unlock when completed
LevelLockManager.CompleteLevel(currentLevel, nextLevel);

// Refresh UI
pageManager.RefreshAllPages();
```

### With HorizontalScrollSnap
- PageManager automatically manages pages
- Assigns correct positions
- Updates pagination dots

### With LevelManager
- LevelButton sets level automatically
- LevelManager loads and applies level
- Scene transitions to gameplay

### With Scene Manager
- Use SceneTransitionManager for cleaner transitions
- Optional loading scene support
- Handles timing and UI updates

---

## Usage Examples

### Setup All Levels at Start
```csharp
public class MenuSetup : MonoBehaviour
{
	public PageManager pageManager;
	public List<LevelData> allLevels;

	void Start()
	{
		pageManager.SetLevels(allLevels);
	}
}
```

### Complete Level & Unlock Next
```csharp
public class GameComplete : MonoBehaviour
{
	public void OnLevelWon()
	{
		LevelData currentLevel = levelManager.level;
		LevelData nextLevel = GetNextLevel();

		// Unlock and mark as won
		LevelLockManager.CompleteLevel(currentLevel, nextLevel);

		// Refresh if showing UI
		pageManager.RefreshAllPages();

		// Transition back to menu
		SceneTransitionManager.GoToLevelSelection();
	}
}
```

### Add Level Dynamically
```csharp
public void AddNewLevel(LevelData newLevel)
{
	pageManager.AddLevel(newLevel);
}
```

### Use Scene Transition Manager
```csharp
// Simple level load
SceneTransitionManager.GoToGameplay(levelData);

// With loading scene
SceneTransitionManager.GoToGameplay(levelData, useLoadingScene: true);

// Go back to menu
SceneTransitionManager.GoToLevelSelection();

// Restart current level
SceneTransitionManager.RestartLevel(currentLevel);
```

---

## Scene Setup Checklist

- [ ] Create "LevelSelection" scene with Canvas
- [ ] Add HorizontalScrollSnap to Canvas
- [ ] Attach PageManager script to HorizontalScrollSnap
- [ ] Create LevelButton prefab with all references
- [ ] Create Page prefab with GridLayoutGroup
- [ ] Create Pagination Dot prefab (Toggle)
- [ ] Create LoadingScreen panel with CanvasGroup
- [ ] Assign all prefabs in PageManager inspector
- [ ] Populate level list in PageManager
- [ ] Create "Gameplay" scene with LevelManager
- [ ] Test level selection and loading

---

## Common Customizations

### Change Levels Per Page
In PageManager inspector:
- `Max Levels Per Page` = 4 (for 2x2 grid)
- `Max Levels Per Page` = 8 (for 4x2 grid)

### Change Max Pages
In PageManager inspector:
- `Max Pages` = 10 (for more levels)

### Custom Loading Scene
Use SceneTransitionManager:
```csharp
SceneTransitionManager.SetLoadingSceneName("MyLoadingScene");
```

### Custom Sprites
In LevelButton prefab:
- Assign your available/locked button sprites
- Add lock icon sprite

### Custom Colors
Modify LevelButton.UpdateButtonVisuals():
```csharp
buttonImage.color = isLocked ? Color.red : Color.green;
```

---

## Debug Tips

### Check Level Lock Status
```csharp
foreach (var level in allLevels)
{
	Debug.Log($"{level.levelName}: {(LevelLockManager.IsLocked(level) ? "LOCKED" : "UNLOCKED")}");
}
```

### Verify Page Creation
Enable debug logs in PageManager.cs CreatePage() method

### Test Button Click Flow
- Click locked button → Should show warning
- Click unlocked button → Should show loading screen
- Scene should transition to Gameplay
- Level should be loaded in LevelManager

---

## Performance Notes

- Buttons are spawned on demand per page
- Pages are lazy-loaded as scrolled
- Pagination dots are lightweight (Toggles)
- Max 20 levels is designed limit (4 pages × 6 buttons)
- All scripts use object pooling pattern

---

## Support Scripts
- `LevelLockManager.cs` - Manage lock states
- `SceneTransitionManager.cs` - Handle scene transitions
- `LevelData.cs` - Contains level data (with `isLocked` property)
- `LevelManager.cs` - Load and apply levels (existing)
