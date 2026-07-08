# SaveManager Enhancement - Complete Summary

## What Was Changed

### Problem Statement
- Items need to be persisted across game sessions
- Items should not respawn after being collected in a level
- Level pickup state (`hasPickup`) needs to be saved and restored

### Solution
Enhanced SaveManager to track and persist `hasPickup` state for each level using level name as unique identifier.

## Files Modified

### 1. **Assets/Script/Manager/SaveManager.cs**
- Added `LEVEL_PICKUP_SUFFIX` constant
- Changed `SaveLevelState()` to accept `levelName` (string) instead of `levelIndex` (int)
- Added `hasPickup` parameter to `SaveLevelState()`
- Updated `LoadLevelState()` to restore `hasPickup` from PlayerPrefs
- Updated `LoadAllLevelStates()` and `SaveAllLevelStates()` to use level names
- Updated `ResetAllLevelStates()` to delete `hasPickup` keys

### 2. **Assets/Script/Manager/GameManager.cs**
- Updated 4 `SaveLevelState()` calls to use `levelName` instead of index
- Added `hasPickup` parameter to all `SaveLevelState()` calls
- Ensures level state is properly persisted when levels are won/unlocked

### 3. **Assets/Script/Grid/ItemCollector.cs**
- Added `SaveManager.SaveLevelState()` call in `OnPlayerEnter()`
- Immediately persists `hasPickup = true` when item is collected
- Ensures state is saved even if game crashes before exiting level

## How It Works

### Save Flow
```
Player collects item in Level "Easy_Level1"
	↓
ItemCollector.OnPlayerEnter() called
	↓
levelManager.level.hasPickup = true (in memory)
	↓
SaveManager.SaveLevelState("Easy_Level1", hasWon, isLocked, true)
	↓
PlayerPrefs stores:
  - "Level_Easy_Level1_hasPickup" = 1
	↓
Game closed/reopened
```

### Load Flow
```
Game loads level list
	↓
SaveManager.LoadAllLevelStates() called for each level
	↓
For each level, reads:
  - "Level_Easy_Level1_hasPickup" from PlayerPrefs
	↓
Sets level.hasPickup = true (if key exists and = 1)
	↓
LevelManager checks: if (level.hasPickup == false) spawn items
	↓
Items NOT spawned (because hasPickup = true)
```

## Storage Format

### PlayerPrefs Keys
```
"Level_<levelName>_hasWon"     → 0 or 1
"Level_<levelName>_isLocked"   → 0 or 1
"Level_<levelName>_hasPickup"  → 0 or 1 (NEW)
```

### Example
```
"Level_Easy_Level1_hasWon"     = 0 (not won yet)
"Level_Easy_Level1_isLocked"   = 0 (unlocked)
"Level_Easy_Level1_hasPickup"  = 1 (item collected)
```

## Key Features

✅ **Per-Level Tracking**: Each level has independent `hasPickup` state  
✅ **Persistent Storage**: Uses PlayerPrefs (survives game restart)  
✅ **Automatic Save**: Saved immediately when item is collected  
✅ **Level Name Based**: Uses level name as unique identifier (stable if levels reordered)  
✅ **Auto-Skip Spawning**: LevelManager already checks `hasPickup` before spawning items  
✅ **No Item Loss**: Items are never lost (different from item quantities which are global)  

## Integration Checklist

- [x] SaveManager saves `hasPickup` state
- [x] SaveManager loads `hasPickup` state
- [x] GameManager calls SaveManager with `hasPickup` parameter
- [x] ItemCollector saves state when item is collected
- [x] LevelManager checks `hasPickup` before spawning items
- [x] All methods use level name as identifier
- [x] Code compiles without errors
- [x] No breaking changes to existing functionality

## Testing Steps

1. **Play a level and collect the item**
   - Verify item disappears after collection
   - Verify `hasPickup` shows as true in LevelData

2. **Save and reload the game**
   - Close the game completely
   - Reopen and load the same level
   - **Verify: Item does NOT spawn again**

3. **Play a different level**
   - Item in this level should still spawn (independent state)
   - Collect item and verify state is saved independently

4. **Reset using SaveManager**
   - Call `SaveManager.ResetAllLevelStates(allLevels)` via context menu
   - All `hasPickup` states should reset to false
   - Items should spawn again when level is reloaded

## Backward Compatibility

- Old save data using index-based keys ("Level_0_hasPickup") will not be migrated
- First run after update will treat all levels as `hasPickup = false`
- Items will spawn again (fresh start for new system)
- Users can manually clear PlayerPrefs if issues occur

## Benefits

1. **Items Don't Respawn**: Collected items stay collected across sessions
2. **Per-Level Independence**: Each level has its own pickup state
3. **Stable ID**: Uses level name instead of index (survives level reordering)
4. **Automatic Save**: No manual save calls needed in gameplay code
5. **Consistent with Level State**: Saves alongside `hasWon` and `isLocked`

## Future Enhancements (Optional)

- Track individual item collection (not just "any item collected")
- Add UI to show which levels have items collected
- Add achievement for collecting all items
- Export/import save data across devices

