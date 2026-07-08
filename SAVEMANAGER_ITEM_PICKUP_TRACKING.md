# SaveManager Enhancement - Item Pickup State Tracking

## Overview
Enhanced SaveManager to track and persist `hasPickup` state for each level, ensuring that items don't respawn after being collected.

## Changes Made

### 1. SaveManager.cs

#### Added LEVEL_PICKUP_SUFFIX Constant
```csharp
private const string LEVEL_PICKUP_SUFFIX = "_hasPickup";
```

#### Updated SaveLevelState Method Signature
**Before:**
```csharp
public static void SaveLevelState(int levelIndex, bool hasWon, bool isLocked)
```

**After:**
```csharp
public static void SaveLevelState(string levelName, bool hasWon, bool isLocked, bool hasPickup = false)
```

**Key Changes:**
- Changed first parameter from `int levelIndex` to `string levelName` (uses level name as unique identifier)
- Added `hasPickup` parameter with default value `false`
- Saves `hasPickup` state to PlayerPrefs with key: `LEVEL_STATE_PREFIX + levelName + LEVEL_PICKUP_SUFFIX`

#### Updated LoadLevelState Method
**Before:**
```csharp
public static void LoadLevelState(int levelIndex, LevelData level)
{
	// Used levelIndex as key
	level.hasWon = PlayerPrefs.GetInt(wonKey, 0) == 1;
	level.isLocked = PlayerPrefs.GetInt(lockedKey, defaultLocked ? 1 : 0) == 1;
}
```

**After:**
```csharp
public static void LoadLevelState(string levelName, LevelData level)
{
	// Uses levelName as key
	level.hasWon = PlayerPrefs.GetInt(wonKey, 0) == 1;
	level.isLocked = PlayerPrefs.GetInt(lockedKey, 1) == 1;
	level.hasPickup = PlayerPrefs.GetInt(pickupKey, 0) == 1;
}
```

#### Updated LoadAllLevelStates & SaveAllLevelStates
Now iterate through levels using `level.levelName` instead of index:

```csharp
public static void LoadAllLevelStates(List<LevelData> allLevels)
{
	foreach (level in allLevels)
		LoadLevelState(level.levelName, level);  // Uses levelName
}

public static void SaveAllLevelStates(List<LevelData> allLevels)
{
	foreach (level in allLevels)
		SaveLevelState(level.levelName, level.hasWon, level.isLocked, level.hasPickup);
}
```

#### Updated ResetAllLevelStates
Now properly deletes `hasPickup` keys when resetting:

```csharp
public static void ResetAllLevelStates(List<LevelData> allLevels)
{
	foreach (level in allLevels)
	{
		string pickupKey = LEVEL_STATE_PREFIX + level.levelName + LEVEL_PICKUP_SUFFIX;
		PlayerPrefs.DeleteKey(pickupKey);
	}
}
```

### 2. GameManager.cs

Updated all SaveLevelState calls to use `levelName` and include `hasPickup`:

**Before:**
```csharp
SaveManager.SaveLevelState(CurrentLevelIndex, CurrentLevel.hasWon, CurrentLevel.isLocked);
```

**After:**
```csharp
SaveManager.SaveLevelState(CurrentLevel.levelName, CurrentLevel.hasWon, CurrentLevel.isLocked, CurrentLevel.hasPickup);
```

Updated 4 locations:
- Line 223: When level is won
- Line 225: When next level is unlocked
- Line 251: In NextLevel() method - current level
- Line 259: In NextLevel() method - next level

### 3. ItemCollector.cs

Enhanced `OnPlayerEnter()` to save `hasPickup` state immediately when item is collected:

**Before:**
```csharp
public void OnPlayerEnter()
{
	if (hasBeenCollected) return;
	hasBeenCollected = true;

	LevelManager levelManager = FindAnyObjectByType<LevelManager>();
	levelManager.level.hasPickup = true;  // Set in memory only

	RewardScreenManager rewardScreenManager = FindAnyObjectByType<RewardScreenManager>();
	rewardScreenManager?.rewardBg.SetActive(true);

	Destroy(gameObject);
}
```

**After:**
```csharp
public void OnPlayerEnter()
{
	if (hasBeenCollected) return;
	hasBeenCollected = true;

	LevelManager levelManager = FindAnyObjectByType<LevelManager>();
	levelManager.level.hasPickup = true;

	// CRITICAL: Save the pickup state to PlayerPrefs
	SaveManager.SaveLevelState(levelManager.level.levelName, 
							   levelManager.level.hasWon, 
							   levelManager.level.isLocked, 
							   true);  // hasPickup = true

	RewardScreenManager rewardScreenManager = FindAnyObjectByType<RewardScreenManager>();
	rewardScreenManager?.rewardBg.SetActive(true);

	Destroy(gameObject);
}
```

## PlayerPrefs Storage Format

### Keys Stored Per Level
```
Key: "Level_Easy_Level1_hasWon"      → Value: 0 or 1
Key: "Level_Easy_Level1_isLocked"    → Value: 0 or 1
Key: "Level_Easy_Level1_hasPickup"   → Value: 0 or 1 (NEW)
```

### Example Flow
```
Session 1:
  ├─ Load Level "Easy_Level1" → hasPickup: false (default)
  ├─ Player collects item → hasPickup set to true
  ├─ ItemCollector.OnPlayerEnter() calls SaveManager
  └─ PlayerPrefs stores "Level_Easy_Level1_hasPickup" = 1

Session 2:
  ├─ Load Level "Easy_Level1" → LoadLevelState() reads hasPickup: true
  ├─ LevelManager checks hasPickup before spawning items
  └─ Item NOT spawned (because hasPickup = true)
```

## Integration Points

### 1. Level Loading
When a level is loaded, SaveManager automatically restores `hasPickup` state:
```csharp
SaveManager.LoadAllLevelStates(allLevels);  // Restores hasPickup for all levels
```

### 2. Item Spawning
When LevelManager spawns items, it should check `level.hasPickup`:
```csharp
// In LevelManager or ItemSpawner
if (!level.hasPickup)
{
	SpawnItemsForLevel(level);
}
```

### 3. State Persistence
Every action that changes level state automatically saves:
- Player collects item → ItemCollector saves `hasPickup = true`
- Player wins level → GameManager saves `hasWon = true`
- Player unlocks next level → GameManager saves `isLocked = false`

## Testing Checklist

- [ ] Collect an item in a level
- [ ] Save the game (or quit)
- [ ] Reload the same level
- [ ] **Verify item is NOT spawned again** (check in inspector or level)
- [ ] **Verify hasPickup is true** in LevelData
- [ ] Try different levels - items should be independent per level
- [ ] Use SaveManager.ResetAllLevelStates() - should clear all pickup states

## Key Differences from Previous Implementation

| Aspect | Before | After |
|--------|--------|-------|
| **Identifier** | Index (0, 1, 2...) | Level Name ("Easy_Level1") |
| **Tracked State** | hasWon, isLocked | hasWon, isLocked, **hasPickup** |
| **Storage Key** | "Level_0_hasWon" | "Level_Easy_Level1_hasWon" |
| **When Saved** | Manual calls | Automatic on collection, win, or level progression |
| **Reliability** | Could break if levels reordered | Stable, uses name as identifier |

## Important Notes

1. **Level Name as ID**: Always ensure each level has a unique `levelName`. This is used as the persistent identifier.
2. **Auto-Save**: When an item is collected, `hasPickup` is immediately saved to PlayerPrefs.
3. **Per-Level State**: Each level maintains its own `hasPickup` state independently.
4. **Default Value**: New levels default to `hasPickup = false` (items will spawn).
5. **Global Items**: Item quantities are still global (tracked in ItemInventory), only pickup state is per-level.

## Migration from Old System (if needed)

If transitioning from index-based storage:
```csharp
// Old keys: "Level_0_hasWon", "Level_0_isLocked"
// New keys: "Level_Easy_Level1_hasWon", "Level_Easy_Level1_isLocked"

// You may need to manually migrate or clear old keys
PlayerPrefs.DeleteKey("Level_0_hasWon");
PlayerPrefs.DeleteKey("Level_0_isLocked");
PlayerPrefs.DeleteKey("Level_0_hasPickup");
```

