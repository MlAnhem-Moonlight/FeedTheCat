# SaveManager Enhancement - Quick Reference

## What Was Done

Enhanced SaveManager to **track and persist item pickup state** (`hasPickup`) for each level.

## Key Changes at a Glance

| Component | Change | Impact |
|-----------|--------|--------|
| **SaveManager** | Changed from index-based to level-name-based storage | Saves: `hasWon`, `isLocked`, **`hasPickup` (NEW)** |
| **GameManager** | Updated 4 SaveLevelState calls | Now passes `levelName` and `hasPickup` parameter |
| **ItemCollector** | Added SaveManager call in OnPlayerEnter() | Immediately persists pickup state when collected |
| **LevelManager** | No changes needed | Already checks `hasPickup` before spawning items |

## PlayerPrefs Keys

```
OLD (Index-based):
  "Level_0_hasWon"      "Level_0_isLocked"

NEW (Name-based):
  "Level_Easy_Level1_hasWon"
  "Level_Easy_Level1_isLocked"
  "Level_Easy_Level1_hasPickup"    ← NEW
```

## Flow Diagram

```
Player Collects Item
	↓
ItemCollector.OnPlayerEnter()
	├─ Set levelManager.level.hasPickup = true
	└─ Call SaveManager.SaveLevelState(..., hasPickup: true)
	↓
SaveManager saves to PlayerPrefs
	├─ "Level_<name>_hasPickup" = 1
	└─ PlayerPrefs.Save()
	↓
Game Closed/Reopened
	↓
Game Loads Level
	├─ SaveManager.LoadLevelState() reads hasPickup from PlayerPrefs
	├─ LevelManager checks: if (!level.hasPickup) spawn items
	└─ Items NOT SPAWNED (already collected)
```

## Method Signatures

### Before
```csharp
SaveManager.SaveLevelState(int levelIndex, bool hasWon, bool isLocked)
SaveManager.LoadLevelState(int levelIndex, LevelData level)
SaveManager.SaveAllLevelStates(List<LevelData> allLevels)
SaveManager.LoadAllLevelStates(List<LevelData> allLevels)
```

### After
```csharp
SaveManager.SaveLevelState(string levelName, bool hasWon, bool isLocked, bool hasPickup = false)
SaveManager.LoadLevelState(string levelName, LevelData level)
SaveManager.SaveAllLevelStates(List<LevelData> allLevels)  // Uses levelName internally
SaveManager.LoadAllLevelStates(List<LevelData> allLevels)  // Uses levelName internally
```

## Important Notes

✅ **Items are now per-level**: Each level tracks whether its item was collected  
✅ **Automatic persistence**: Saved immediately when collected (no manual save needed)  
✅ **Level name as ID**: Stable identifier even if levels are reordered  
✅ **LevelManager ready**: Already has logic to skip spawning when `hasPickup = true`  
✅ **Backward compatible**: New levels default to `hasPickup = false`  

## Common Tasks

### Save Item Pickup State
```csharp
// Automatic! Called from ItemCollector.OnPlayerEnter()
SaveManager.SaveLevelState(levelName, hasWon, isLocked, hasPickup);
```

### Load Item Pickup State
```csharp
// Automatic! Called when level is loaded
SaveManager.LoadAllLevelStates(allLevels);
```

### Reset All Pickup States (for testing)
```csharp
// Via Inspector: Select SaveManager GameObject → right-click → "Reset All Level States"
// OR in code:
SaveManager.ResetAllLevelStates(allLevels);
```

### Check if Item Was Collected
```csharp
LevelData level = GameManager.Instance.CurrentLevel;
if (level.hasPickup)
{
	Debug.Log("Item already collected in this level");
}
else
{
	Debug.Log("Item available to collect");
}
```

## Testing Checklist

- [ ] Collect item in level
- [ ] Close and reopen game
- [ ] Load same level
- [ ] **Verify: Item NOT spawned** ← Key test
- [ ] **Verify: LevelData.hasPickup = true**
- [ ] Try different level - item should spawn there
- [ ] Use Reset context menu - items should respawn

## Files Changed

1. **SaveManager.cs** - Core persistence logic
2. **GameManager.cs** - Updated 4 SaveLevelState calls
3. **ItemCollector.cs** - Added SaveManager call in OnPlayerEnter()

**Build Status**: ✅ Successful - No compilation errors

