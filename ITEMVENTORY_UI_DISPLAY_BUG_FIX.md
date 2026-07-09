# ItemInventory UI Display Bug Fix

## Problem
Items được save không hiển thị ngay khi vào level. Chỉ khi nhận item mới thì UI mới update và hiển thị tất cả items.

### Root Cause

**Flow khi load level:**

```
1. ItemInventory.Start() → Load()
   ├─ Reads items from PlayerPrefs
   └─ Populates inventory dictionary

2. ItemBase.Start() → SubscribeToInventory() + UpdateUI()
   ├─ Subscribe to OnQuantityChanged event
   ├─ Call UpdateUI()
   │  ├─ Read CurrentQuantity (from ItemInventory.GetQuantity())
   │  └─ Update UI display
   └─ Result: UI shows correct quantity ✓

3. BUT: No OnQuantityChanged event triggered for loaded items!
   ├─ Only fired when SetQuantity() is called
   └─ Load() doesn't trigger it → UI not notified of changes ✗
```

**Scenario:**
- Save with 5 Stun items
- Reload game
- ItemInventory loads: inventory["item_stun"] = 5
- ItemBase.UpdateUI() reads quantity = 5
- **BUT:** OnQuantityChanged wasn't triggered
- **Result:** UI shows 0 or wrong value initially

**Why?** ItemBase.UpdateUI() relies on reading current value, but subscriptions expect events. Some UI systems might cache old values before UpdateUI() is called.

## Solution

Trigger `OnQuantityChanged` event for each loaded item immediately after loading:

```csharp
isLoaded = true;

// CRITICAL: Trigger OnQuantityChanged for each loaded item so UI updates immediately
foreach (var kvp in inventory)
{
	OnQuantityChanged?.Invoke(kvp.Key, kvp.Value, 0);
	LogFilter.LogItem($"ItemInventory: Triggered UI update for {kvp.Key} (qty={kvp.Value})");
}

OnInventoryLoaded?.Invoke();
```

This ensures:
1. ✅ All subscribers are notified of loaded quantities
2. ✅ UI updates correctly on first display
3. ✅ Consistent with save/load behavior
4. ✅ Works with all UI systems that listen to events

## Files Modified

### Assets/Script/Item/Inventory/ItemInventory.cs

**Method:** `Load()`  
**Location:** After `isLoaded = true;`  
**Change:** Added event trigger loop for each loaded item

**Before:**
```csharp
isLoaded = true;
OnInventoryLoaded?.Invoke();
```

**After:**
```csharp
isLoaded = true;

// CRITICAL: Trigger OnQuantityChanged for each loaded item so UI updates immediately
foreach (var kvp in inventory)
{
	OnQuantityChanged?.Invoke(kvp.Key, kvp.Value, 0);
	LogFilter.LogItem($"ItemInventory: Triggered UI update for {kvp.Key} (qty={kvp.Value})");
}

OnInventoryLoaded?.Invoke();
```

## Event Flow After Fix

### Before (Broken):
```
Load() completes
   ├─ inventory loaded: {Stun: 5, Charm: 3}
   ├─ OnInventoryLoaded triggered
   └─ NO OnQuantityChanged events
	   └─ ItemBase.HandleQuantityChanged() never called
		   └─ UI doesn't reflect loaded values
```

### After (Fixed):
```
Load() completes
   ├─ inventory loaded: {Stun: 5, Charm: 3}
   ├─ OnQuantityChanged("Stun", 5, 0) ← Event 1
   │  └─ ItemBase.HandleQuantityChanged() → UpdateUI()
   ├─ OnQuantityChanged("Charm", 3, 0) ← Event 2
   │  └─ ItemBase.HandleQuantityChanged() → UpdateUI()
   ├─ OnInventoryLoaded triggered
   └─ UI now shows correct quantities ✓
```

## Why Pass 0 as Previous Quantity?

The event signature is: `OnQuantityChanged(itemID, newQuantity, previousQuantity)`

Passing 0 as previous:
- ✅ Simple and logical (loaded from empty state)
- ✅ Indicates this is a "load" event not a "use" event
- ✅ UI can differentiate behavior if needed
- ✅ Doesn't matter for standard UpdateUI() (only reads current qty)

## Debug Output

**Console after loading items:**
```
ItemInventory: Loaded item_stun_3radius with quantity 5 from PlayerPrefs
ItemInventory: Loaded item_charm_4radius with quantity 3 from PlayerPrefs
ItemInventory: Triggered UI update for item_stun_3radius (qty=5)
ItemInventory: Triggered UI update for item_charm_4radius (qty=3)
ItemInventory: Loaded 2 items from PlayerPrefs
```

Each item gets its UI update triggered immediately.

## Testing Checklist

- [ ] Save game with items (e.g., Stun: 5, Charm: 3)
- [ ] Close and reload game
- [ ] **Verify: Items show correct quantities immediately** ← Key test
- [ ] **Verify: No "0" display then "5" transition**
- [ ] Use an item → quantity updates
- [ ] Save/reload again → same quantity restored
- [ ] Check console for "Triggered UI update" logs

## Edge Cases Handled

| Case | Behavior |
|------|----------|
| No saved items | No events triggered (inventory empty) ✓ |
| Items with qty 0 | Not loaded (qty > 0 check) ✓ |
| New items on first run | No events (no saved items) ✓ |
| Quantity == 0 after load | Not added to inventory ✓ |

## Related Code

### ItemBase.HandleQuantityChanged (listener)
```csharp
private void HandleQuantityChanged(string changedItemID, int newQuantity, int previousQuantity)
{
	if (changedItemID != ItemID)
		return;
	UpdateUI();  // ← Called when event fired
}
```

### ItemInventory.SetQuantity (sender)
```csharp
public void SetQuantity(string itemID, int quantity)
{
	// ... clamp/validate ...
	inventory[itemID] = clampedQuantity;
	OnQuantityChanged?.Invoke(itemID, clampedQuantity, previousQuantity);  // ← Fires here
}
```

Now Load() also fires the event, ensuring consistency.

## Benefits

✅ **UI displays correctly on first load**  
✅ **Consistent event behavior (Load vs Set)**  
✅ **No timing issues or race conditions**  
✅ **Simple and maintainable**  
✅ **Works with all subscription patterns**  

## Conclusion

By triggering `OnQuantityChanged` events during Load(), we ensure all UI systems are properly notified of loaded quantities, preventing the "items don't show until used" bug.

