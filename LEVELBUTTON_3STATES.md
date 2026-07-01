# LevelButton Restructure - 3 States System

## Thay Đổi

### 1. Cấu Trúc Hierarchy
```
Button (LevelButton component)
├─ Lv Img (Image) - displays state sprite
└─ Lock Icon (Image) - shows only when locked
```

### 2. 3 Trạng Thái Level

| State | isLocked | hasWon | Sprite | Lock Icon | Interactable | Alpha |
|-------|----------|--------|--------|-----------|--------------|-------|
| **Locked** | true | - | lockedSprite | ✓ Shown | ✗ No | 0.6f |
| **Available** | false | false | availableSprite | ✗ Hidden | ✓ Yes | 1f |
| **Completed** | false | true | completedSprite | ✗ Hidden | ✓ Yes | 1f |

### 3. Inspector Setup

**References:**
- `Lv Img` → Image component (previously buttonImage)
- `Lock Icon` → Image component for lock icon
- `Level Name Text` → Text component for level name
- `Canvas Group` → CanvasGroup for alpha control

**Sprites (for Lv Img):**
- `Locked Sprite` → Sprite khi level locked
- `Available Sprite` → Sprite khi level có thể play
- `Completed Sprite` → Sprite khi level đã winning

**Loading:**
- `Gameplay Scene Name` → Scene name to load
- `Loading Screen Canvas Group` → CanvasGroup for loading overlay

**Current Level:**
- `Level Data` → LevelData asset

### 4. Code Changes

#### Removed
- `LevelLockManager.IsLocked()` check
- `buttonImage` reference (replaced with `lvImg`)
- 2-state sprite system

#### Added
- `LevelState` enum: Locked, Available, Completed
- `GetLevelState()` method: determine state from LevelData
- `RefreshVisuals()` public method: call khi level state thay đổi

#### Updated
- `UpdateButtonVisuals()`: now uses 3 states + 3 sprites
- `OnLevelButtonClicked()`: uses `levelData.isLocked` directly

### 5. LevelData Dependencies

```csharp
public class LevelData : ScriptableObject
{
	public bool isLocked = true;   // True = Locked state
	public bool hasWon = false;    // True (+ not locked) = Completed state
	// ... other fields
}
```

**State Logic:**
```csharp
if (levelData.isLocked)
	return LevelState.Locked;
else if (levelData.hasWon)
	return LevelState.Completed;
else
	return LevelState.Available;
```

### 6. Usage Examples

**Setup Button:**
```csharp
LevelButton btn = GetComponent<LevelButton>();
btn.SetLevelData(someLevel);  // Auto-updates visuals
```

**Refresh After Winning:**
```csharp
// In GameManager or level complete handler:
levelButton.GetLevelData().hasWon = true;
levelButton.RefreshVisuals();  // Updates sprite to Completed
```

**Check Current State:**
```csharp
// Internally used in UpdateButtonVisuals()
LevelState state = GetLevelState();
```

### 7. Inspector Workflow

1. Select Button GameObject in scene
2. Inspector → LevelButton component
3. Assign:
   - **Lv Img**: Select Image child
   - **Lock Icon**: Select lock icon Image child
   - **Level Name Text**: Select Text child
   - **Canvas Group**: Select Canvas Group
   - **Locked/Available/Completed Sprites**: Drag 3 different sprites
   - **Gameplay Scene Name**: "Gameplay" (or your scene name)
   - **Level Data**: Drag LevelData asset

### 8. Debug Logging

When button is setup or visuals updated:
```
LevelButton.SetLevelData: assigned level 'Level 1' (...) to button 'LevelButton'
LevelButton.UpdateButtonVisuals: 'Level 1' state=Available
```

When button clicked:
```
LevelButton -> GameManager queued level 'Level 1' (...)
```

When locked button clicked:
```
LevelButton: Level 'Level 2' is locked!
```

## Migration Checklist

- [ ] Rename `buttonImage` reference to `lvImg` in scene/prefabs
- [ ] Assign `completedSprite` in addition to locked/available sprites
- [ ] Update any code referencing `LevelLockManager.IsLocked()`
- [ ] Call `RefreshVisuals()` after level state changes
- [ ] Test all 3 states: Locked, Available, Completed
- [ ] Verify lock icon only shows when Locked
- [ ] Verify button interactable based on state
- [ ] Verify sprites swap correctly

## Notes

- `Lock Icon` is **always hidden except when state is Locked**
- Button remains **interactable for both Available and Completed** states
- Alpha darkens **only for Locked state**
- Use `RefreshVisuals()` public method when level state changes at runtime
- State determination is **automatic** based on LevelData flags
