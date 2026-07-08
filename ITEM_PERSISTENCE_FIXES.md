# Item Persistence & Zero Quantity Fixes

## Summary
Fixed two critical issues with the item system:
1. **Item quantities were not persisted** across game sessions (when closing/reopening the game)
2. **Items with quantity 0 were still usable** (draggable and executable)

## Changes Made

### 1. ItemInventory.cs - Persistence Implementation

#### Added SAVED_ITEMS_KEY Constant
```csharp
private const string SAVED_ITEMS_KEY = "inventory_saved_items";
```
This key stores a comma-separated list of all item IDs that have been saved, allowing us to enumerate them on load (since Unity doesn't provide PlayerPrefs.GetAllKeys()).

#### Enhanced Save() Method
- Now maintains a list of saved item IDs
- Stores comma-separated item IDs in PlayerPrefs under SAVED_ITEMS_KEY
- Each item's quantity is stored with key: `persistenceKeyPrefix + itemID`

**Before:**
```csharp
public void Save()
{
	foreach (var kvp in inventory)
	{
		string key = persistenceKeyPrefix + kvp.Key;
		PlayerPrefs.SetInt(key, kvp.Value);
	}
	PlayerPrefs.Save();
}
```

**After:**
```csharp
public void Save()
{
	List<string> savedItemIDs = new List<string>();
	foreach (var kvp in inventory)
	{
		string key = persistenceKeyPrefix + kvp.Key;
		PlayerPrefs.SetInt(key, kvp.Value);
		savedItemIDs.Add(kvp.Key);
	}

	string savedItemsValue = string.Join(",", savedItemIDs);
	PlayerPrefs.SetString(SAVED_ITEMS_KEY, savedItemsValue);
	PlayerPrefs.Save();
}
```

#### Implemented Load() Method
- Retrieves the comma-separated list of saved item IDs
- Loads quantity for each saved item from PlayerPrefs
- Properly initializes the inventory dictionary on startup

**Before:**
```csharp
public void Load()
{
	if (isLoaded) return;
	inventory.Clear();
	isLoaded = true;
	OnInventoryLoaded?.Invoke();
}
```

**After:**
```csharp
public void Load()
{
	if (isLoaded) return;
	inventory.Clear();

	string savedItemsValue = PlayerPrefs.GetString(SAVED_ITEMS_KEY, "");
	if (!string.IsNullOrEmpty(savedItemsValue))
	{
		string[] itemIDs = savedItemsValue.Split(',');
		foreach (string itemID in itemIDs)
		{
			if (string.IsNullOrWhiteSpace(itemID)) continue;

			string key = persistenceKeyPrefix + itemID.Trim();
			int quantity = PlayerPrefs.GetInt(key, 0);

			if (quantity > 0)
			{
				inventory[itemID.Trim()] = quantity;
				LogFilter.LogItem($"ItemInventory: Loaded {itemID.Trim()} with quantity {quantity}");
			}
		}
	}

	isLoaded = true;
	OnInventoryLoaded?.Invoke();
}
```

#### Updated ClearAllInventory()
- Now uses the saved items list to identify and delete PlayerPrefs keys
- Clears SAVED_ITEMS_KEY itself

#### Added InitializeDefaultItems() Method
Public helper method to set default starting quantities (optional, for new game setup):
```csharp
public void InitializeDefaultItems(Dictionary<string, int> defaultQuantities)
{
	if (defaultQuantities == null) return;
	foreach (var kvp in defaultQuantities)
	{
		if (!inventory.ContainsKey(kvp.Key))
		{
			SetQuantity(kvp.Key, kvp.Value);
		}
	}
}
```

#### Logging Updates
- Replaced all `Debug.Log` calls with `LogFilter.LogItem()` for controlled console output
- Example: `LogFilter.LogItem($"ItemInventory: Loaded {inventory.Count} items from PlayerPrefs")`

### 2. ItemBase.cs - Zero Quantity Prevention

#### Updated CanUse() Method
Added explicit check to block usage when quantity is 0:

**Before:**
```csharp
public virtual bool CanUse()
{
	if (itemData == null) return false;
	if (itemData.ItemType == ItemType.Passive) return false;
	return CurrentQuantity > 0 && !IsOnCooldown();
}
```

**After:**
```csharp
public virtual bool CanUse()
{
	if (itemData == null) return false;
	if (itemData.ItemType == ItemType.Passive) return false;

	// CRITICAL: Block usage if quantity is 0
	if (CurrentQuantity <= 0)
		return false;

	return !IsOnCooldown();
}
```

#### How This Prevents Usage at Zero Quantity
1. **OnBeginDrag()** calls `CanUse()` - returns false when quantity is 0, preventing drag initiation
2. **UpdateUI()** sets `canvasGroup.interactable = canUse` - disables the button when quantity is 0
3. **UpdateUI()** dims the icon and shows "0" in gray when quantity is 0

## Persistence Flow

### Saving (During Gameplay)
1. Item is used → `ItemBase.DecreaseQuantity()` called
2. `ItemInventory.DecreaseItem(itemID, 1)` reduces quantity
3. `ItemInventory.OnQuantityChanged` event fires
4. `ItemBase.OnEndDrag()` calls `itemInventory.Save()`
5. **Result:** All current quantities stored in PlayerPrefs with saved items list

### Loading (On Game Startup)
1. **ItemInventory** singleton created (Awake)
2. **ItemInventory.Start()** calls `Load()`
3. **Load()** reads SAVED_ITEMS_KEY from PlayerPrefs
4. **Load()** populates inventory dictionary from saved quantities
5. **Scenes load** with **ItemBase** instances
6. **ItemBase.Start()** → `SubscribeToInventory()` + `UpdateUI()`
7. **UpdateUI()** reads current quantity and reflects in UI
8. **Result:** Items display correct quantities and are properly gated

## Testing Checklist

- [ ] Use an item → quantity decreases in UI
- [ ] Close and reopen game → quantity should be saved (use Unity Editor play/stop)
- [ ] Use item until quantity reaches 0 → button should dim and become uninteractable
- [ ] Attempt to drag item with quantity 0 → no drag should occur
- [ ] Use "Clear All Inventory" context menu → all quantities should reset to 0 and be cleared from PlayerPrefs
- [ ] Set item quantity to 0 manually → UI should update and prevent usage
- [ ] Cross-scene navigation → items should maintain their quantities

## Additional Notes

- **PlayerPrefs Storage:** Items are stored in player preferences (typically in registry on Windows, ~/Library on Mac, XML on other platforms)
- **Key Naming:** All inventory keys are prefixed with `persistenceKeyPrefix` (default: "inventory_")
- **New Game Scenario:** If no saved items exist, Load() will complete successfully with an empty inventory. Use `InitializeDefaultItems()` to set starting quantities for new games.
- **LogFilter Integration:** All item inventory logs now use LogFilter for controlled output (can be selectively enabled)

## Files Modified
1. **Assets/Script/Item/Inventory/ItemInventory.cs** - Persistence implementation
2. **Assets/Script/Item/Base/ItemBase.cs** - CanUse() check updated
