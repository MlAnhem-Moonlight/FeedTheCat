# MAPElites Window - Reclassify Button Fix

## Problem
Button "Reclassify Difficulties" không hiển thị trong editor vì nó nằm trong block `if (generator != null)`.

### Before
```csharp
if (generator != null)  // ← Button chỉ hiển thị khi generator != null
{
	// Export buttons...

	if (GUILayout.Button("Reclassify Difficulties"))  // ← Hidden until Generate is called
	{
		generator.archive.RecalculateDifficulties();
	}
}
```

**Result:** Button không thấy cho đến khi chạy Generate

## Solution
Tách button ra khỏi block `if (generator != null)` - nó luôn hiển thị nhưng với logic kiểm tra bên trong:

### After
```csharp
if (generator != null)
{
	// Export buttons... (still conditional)
}

// Reclassify button ALWAYS visible (outside the if block)
if (GUILayout.Button("Reclassify Difficulties"))
{
	if (generator != null && generator.archive != null)  // ← Check inside instead
	{
		generator.archive.RecalculateDifficulties();
		Debug.Log("Da phan loai lai do kho cho toan bo archive.");
	}
	else
	{
		Debug.LogWarning("No archive loaded. Run 'Generate' first.");
	}
}
```

**Result:** Button luôn hiển thị, nhưng với feedback rõ ràng nếu chưa generate

## Changes Made

### File: Assets/Script/Map-Elites/BFS_MapElite_Claude/MAPElitesWindow.cs

**Location:** `OnGUI()` method

**Key Changes:**
1. Moved "Reclassify Difficulties" button **outside** the `if (generator != null)` block
2. Added **inner null-check** instead of outer visibility block
3. Added **warning message** if archive not loaded

### GUI Layout Flow

**Before:**
```
[Generate Button]
   ↓
[Archive Display] (if generator != null)
   ↓
[Export Buttons] (if generator != null)
   [Reclassify Button] (if generator != null) ← Hidden!
```

**After:**
```
[Generate Button]
   ↓
[Archive Display] (if generator != null)
   ↓
[Export Buttons] (if generator != null)
   ↓
[Reclassify Button] ← Always visible!
  ├─ If generator exists → Works
  └─ If generator null → Shows warning
```

## User Experience

### Scenario 1: After Running Generate
```
✅ Archive visible
✅ Export buttons visible
✅ Reclassify button visible and works
```

### Scenario 2: Without Running Generate
```
❌ Archive hidden (correct - no data)
❌ Export buttons hidden (correct - no data)
✅ Reclassify button VISIBLE (new!)
   └─ Click → Shows warning: "Run Generate first"
```

### Scenario 3: Future Enhancement (Load Archive)
```
If archive loading is implemented later:
✅ Reclassify button always ready to use
✅ Works with loaded archive immediately
```

## Debug Output

**When successful:**
```
Da phan loai lai do kho cho toan bo archive.
```

**When generator not initialized:**
```
MAPElitesWindow: No archive loaded. Run 'Generate' first or load an archive.
```

## Benefits

✅ **Discoverability:** Button is visible even if generator is null  
✅ **User Guidance:** Clear message if nothing to reclassify  
✅ **Future-Proof:** Ready for archive loading feature  
✅ **Consistent:** Same null-check pattern as other methods  

## Related Methods

Other buttons still conditional because they depend on generator data:
- Export Easy/Medium/Hard → Need archive with levels
- Archive Display → Needs levels to display

But Reclassify is now always visible with clear feedback.

## Testing

1. Open MAP-Elites window
2. **Button should be visible immediately** (not need to click Generate first)
3. Click Reclassify without Generate → See warning message
4. Click Generate, then click Reclassify → See success message
5. Close and reopen window → Button still visible

