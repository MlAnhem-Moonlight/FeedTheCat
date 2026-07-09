# Level 0 Always Unlocked - Bug Fix

## Problem Identified

Khi đẩy một list level mới vào GameManager, level 0 vẫn bị set `isLocked = true`.

### Root Cause

**Flow khi load level list mới:**

```
1. New LevelData list assigned in Inspector (allLevels)
   └─ LevelData default: isLocked = true (from ScriptableObject definition)

2. GameManager.Awake() called
   ├─ SaveManager.LoadAllLevelStates(allLevels) 
   │  └─ Reads from PlayerPrefs
   │     ├─ If key exists → overwrite with saved value
   │     └─ If key NOT exists → keep default (isLocked = true) ❌

3. Result: Level 0 stays locked if no saved state in PlayerPrefs
```

**Scenario:**
- First startup: Level 0 locked ❌
- After reset/update levels: Level 0 locked ❌
- Only after SaveManager.ResetAllLevelStates: Level 0 unlocked ✓

## Solution Implemented

Added guarantee in `GameManager.Awake()` that **level 0 is always unlocked**:

```csharp
private void Awake()
{
	// ... existing code ...

	// Load persistent level states
	SaveManager.LoadAllLevelStates(allLevels);

	// CRITICAL: Ensure first level is always unlocked (for new level lists)
	if (allLevels != null && allLevels.Count > 0)
	{
		allLevels[0].isLocked = false;  // ← Force unlock level 0
	}

	// ... rest of code ...
}
```

## Files Modified

### Assets/Script/Manager/GameManager.cs

**Location:** `Awake()` method  
**Change:** Added 4-line guarantee after `SaveManager.LoadAllLevelStates()`

```csharp
// BEFORE:
SaveManager.LoadAllLevelStates(allLevels);
// Level 0 might still be locked!

// AFTER:
SaveManager.LoadAllLevelStates(allLevels);

// CRITICAL: Ensure first level is always unlocked (for new level lists)
if (allLevels != null && allLevels.Count > 0)
{
	allLevels[0].isLocked = false;
}
```

## Behavior After Fix

### Scenario 1: Fresh Start
```
1. GameManager load with new level list
2. SaveManager.LoadAllLevelStates() → no saved keys
3. Level 0 stays locked (default)
4. GameManager force unlocks Level 0 ✅
5. Result: Level 0 is playable
```

### Scenario 2: After Save/Load
```
1. Player plays, completes level 1 (unlocks level 2)
2. SaveManager persists states
3. Game restart
4. GameManager loads saved states (level 1: won, level 2: unlocked)
5. GameManager ensures level 0: unlocked ✅
6. Result: All previous progress preserved + level 0 guaranteed
```

### Scenario 3: New Level List Pushed
```
1. New levels added to Inspector
2. GameManager restart
3. SaveManager.LoadAllLevelStates() → keys may not exist for new levels
4. New levels keep default isLocked = true
5. GameManager force unlocks level 0 ✅
6. Result: Level 0 playable immediately
```

## Why This Works

- ✅ **Non-destructive:** Doesn't prevent saving level 0 lock state
- ✅ **Simple:** Single assignment after load
- ✅ **Cheap:** O(1) operation
- ✅ **Safe:** Checks for null and count
- ✅ **Consistent:** Matches SaveManager.ResetAllLevelStates() logic

## Related Code

### SaveManager.ResetAllLevelStates()
Also ensures level 0 is unlocked:
```csharp
if (i == 0)
{
	allLevels[i].isLocked = false;  // Unlock first level
}
else
{
	allLevels[i].isLocked = true;   // Lock others
}
```

### LevelData Default
```csharp
public class LevelData : ScriptableObject
{
	public bool isLocked = true;  // Default: locked
	// ...
}
```

## Testing Checklist

- [ ] Start fresh → Level 0 not locked ✅
- [ ] Complete level 1 → Level 2 unlocked ✅
- [ ] Restart game → Level 0 still not locked ✅
- [ ] Update level list → Level 0 still not locked ✅
- [ ] Use SaveManager.ResetAllLevelStates → Level 0 not locked ✅

## Edge Cases Handled

| Case | Behavior |
|------|----------|
| allLevels is null | Safe check prevents error |
| allLevels is empty | Safe check prevents error |
| Level 0 already unlocked | Re-assign false (safe) |
| Level 0 should be locked | Can't happen (always unlocked) |
| Level 1+ locked | Unaffected by this code |

## Conclusion

Simple, robust fix ensures level 0 is always accessible regardless of:
- Fresh start
- Saved state
- Level list updates
- SaveManager reset

This provides consistent user experience and prevents "no levels playable" scenarios.

