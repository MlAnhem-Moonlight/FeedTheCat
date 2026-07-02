# Item/Reward System Implementation Guide

## Overview
A new item/reward system has been added to FeedTheCat that allows items to spawn on the map. When players collect items, a debug log displays the rarity level based on the level's difficulty.

## Files Modified/Created

### 1. **LevelData.cs** (Modified)
- Added `ItemDef` class: Serializable data holder for item position and prefab index
- Added `items` list to `LevelData`: Stores all items for a level

### 2. **ItemCollector.cs** (Created)
- New script that handles item behavior
- **Key Features:**
  - Automatically marks its cell as `occupiedByItem = true`
  - Extracts difficulty from level name format: `{difficulty}_LevelName`
	- Example: `"Easy_Level1"`, `"Hard_Stage2"`, `"Medium_Challenge3"`
  - Maps difficulty to rarity:
	- `Easy` → `Common`
	- `Medium` → `Uncommon`
	- `Hard` → `Rare`
  - Logs collection: `Debug.Log("nhận quà + {rarity}")`
  - Destroys itself after collection

### 3. **GridCell.cs** (Modified)
- Added `occupiedByItem` bool flag
- Updated `SetOccupiedByPlayer()` to detect item collection
- Updated `ClearOccupancy()` to clear item flag

### 4. **LevelManager.cs** (Modified)
- Added `itemPrefabs` list: Holds item prefab references
- Added `spawnedItems` list: Tracks spawned items for cleanup
- Added item spawning logic in `ApplyLevelCoroutine()`
- Updated `ClearLevel()` to destroy spawned items

## How to Use

### Setup in Level Data
1. Create a new LevelData or open existing
2. In the **Items** list, add new entries with:
   - **Row**: Grid row position
   - **Column**: Grid column position
   - **Prefab Index**: Index into LevelManager's itemPrefabs list

### Setup in LevelManager Inspector
1. Assign item prefabs to the **Item Prefabs** list
2. Item prefabs should have:
   - `RectTransform` component (for UI positioning)
   - `ItemCollector` script component (for collection logic)

### Example Level Name Format
```
Easy_BeginnerLevel      → Item rarity = Common
Medium_IntermediateChallenge  → Item rarity = Uncommon
Hard_FinalBoss          → Item rarity = Rare
```

## Debug Output Example
When player collects an item from a "Hard_Level5" level:
```
nhận quà + Rare
```

## Item Behavior Flow
1. **Spawn**: LevelManager instantiates items at specified grid positions
2. **Placement**: ItemCollector marks its cell as `occupiedByItem = true`
3. **Detection**: Player moves to cell
4. **Collection**: GridCell.SetOccupiedByPlayer() calls ItemCollector.OnPlayerEnter()
5. **Logging**: ItemCollector logs rarity based on level difficulty
6. **Cleanup**: Item destroyed after collection

## Fallback Behavior
- If level name doesn't match `{difficulty}_LevelName` format → rarity = `Uncommon`
- If no item prefabs assigned → warning logged, items not spawned
- If difficulty string unrecognized → rarity = `Uncommon`

## Testing Checklist
- [ ] Add item entries to a test level in LevelData
- [ ] Assign item prefabs in LevelManager
- [ ] Run game and collect items
- [ ] Check Console for debug log: `nhận quà + {rarity}`
- [ ] Verify rarity matches expected difficulty level
- [ ] Verify items disappear after collection
- [ ] Test multiple items on different difficulties

## Item Prefab Requirements
```
ItemPrefab
├── RectTransform (UI positioning)
├── Image (visual representation - optional)
└── ItemCollector (required script)
```

## Notes
- Items are automatically parented to their grid cells for visual alignment
- Multiple items can exist on the same level
- Items cannot occupy the same cell as NPCs or other items at spawn time
- Items are destroyed when collected (not tracked in saves)
