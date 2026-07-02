# Consumable Item System - Code Examples & API Reference

## Quick Reference API

### ItemInventory

```csharp
// Get singleton instance
ItemInventory inv = ItemInventory.Instance;

// Query quantities
int qty = inv.GetQuantity("charm_spell_2");
bool hasItem = inv.CanUse("charm_spell_2");

// Modify quantities
inv.IncreaseItem("charm_spell_2", 5);    // Add 5 items
inv.DecreaseItem("charm_spell_2", 1);    // Remove 1 item
inv.SetQuantity("charm_spell_2", 10);    // Set exactly 10

// Persistence
inv.Save();   // Write to PlayerPrefs
inv.Load();   // Load from PlayerPrefs
inv.LoadItem("charm_spell_2");  // Load single item

// Events
inv.OnQuantityChanged += (itemID, newQty, oldQty) => 
{
	Debug.Log($"{itemID}: {oldQty} -> {newQty}");
};

inv.OnItemAdded += (itemID, addedQty) =>
{
	Debug.Log($"Added {addedQty} of {itemID}");
};

// Debugging
inv.LogInventory();           // Print all items
inv.ClearAllInventory();      // Reset (testing only)
```

### StatusEffectSystem

```csharp
// Get singleton
StatusEffectSystem status = StatusEffectSystem.Instance;

// Apply effects
status.ApplyStatusEffect(npcMover, StatusEffectType.Stun, 3);
status.ApplyStatusEffect(npcMover, StatusEffectType.Charm, 2);

// Query effects
bool isStunned = status.HasStatusEffect(npcMover, StatusEffectType.Stun);
int remainingTurns = status.GetEffectDuration(npcMover, StatusEffectType.Stun);

// Remove effects
status.RemoveStatusEffect(npcMover, StatusEffectType.Stun);

// Debugging
status.LogActiveEffects();
```

### RadiusHelper

```csharp
// Find NPCs in radius
List<NPCMover> nearbyNPCs = RadiusHelper.GetNPCsInRadius(
	gridCell, 
	EffectRadiusType.Radius2
);

// Check single NPC
bool inRange = RadiusHelper.IsNPCInRadius(
	gridCell, 
	npcMover, 
	EffectRadiusType.Radius3
);

// Debugging
RadiusHelper.DebugLogNPCsInRadius(gridCell, EffectRadiusType.Radius2);
```

### ItemData (ScriptableObject)

```csharp
// Create in code (rarely needed, use Inspector)
ItemData itemData = ScriptableObject.CreateInstance<ItemData>();

// Access properties
string id = itemData.ItemID;
string name = itemData.ItemName;
string desc = itemData.Description;
Sprite icon = itemData.Icon;

ItemType type = itemData.ItemType;  // Active or Passive
EffectRadiusType radius = itemData.EffectRadius;
TargetType target = itemData.TargetType;
StatusEffectType effect = itemData.StatusEffect;
int duration = itemData.EffectDuration;
ItemRarity rarity = itemData.Rarity;
int cooldown = itemData.Cooldown;

// Validate
itemData.ValidateItemData();
```

### RewardGenerator

```csharp
RewardGenerator gen = GetComponent<RewardGenerator>();

// Generate rewards
List<RewardGenerator.GeneratedReward> rewards = gen.GenerateRewardCards();

// Each reward contains:
// - reward.itemData (ItemData asset)
// - reward.quantity (int, e.g., 5)
// - reward.rarity (ItemRarity enum)

// Apply reward to inventory
gen.ApplyReward(rewards[0]);

// Debugging
gen.DebugGenerateRewards();
```

### RewardCard

```csharp
RewardCard card = GetComponent<RewardCard>();

// Populate card
card.PopulateCard(
	itemData,           // ItemData asset
	quantity,           // int quantity
	OnCardSelected      // Action<RewardCard> callback
);

// Access card data
ItemData itemData = card.ItemData;
int qty = card.RewardQuantity;
Color rarityColor = card.GetRarityColor();
string rarityText = card.GetRarityDisplayText();

// Debugging
card.LogCardInfo();
```

### ItemBase (for custom items)

```csharp
// Create custom item class
public class MyCustomItem : ItemBase
{
	protected override bool ValidateTarget(GridCell target)
	{
		// Check if target is valid for this item
		return target != null && !target.occupiedByNPC;
	}

	protected override void ExecuteEffect(GridCell target)
	{
		// Apply item effect
		List<NPCMover> affected = RadiusHelper.GetNPCsInRadius(
			target, 
			itemData.EffectRadius
		);

		foreach (NPCMover npc in affected)
		{
			StatusEffectSystem.Instance.ApplyStatusEffect(
				npc, 
				itemData.StatusEffect, 
				itemData.EffectDuration
			);
		}
	}
}

// ItemBase provides:
// - ItemID (string)
// - CurrentQuantity (int)
// - CanUse() method
// - Drag-drop event handlers
// - Automatic UI updates
// - Inventory persistence
```

---

## Integration Examples

### Example 1: Display Item Quantity in UI

```csharp
using FeedTheCat.Items;
using UnityEngine;
using TMPro;

public class ItemQuantityDisplay : MonoBehaviour
{
	[SerializeField] private string itemID = "charm_spell_2";
	[SerializeField] private TextMeshProUGUI quantityText;

	private ItemInventory inventory;

	private void Start()
	{
		inventory = ItemInventory.Instance;

		// Subscribe to changes
		inventory.OnQuantityChanged += UpdateDisplay;

		// Initial display
		UpdateDisplay(itemID, inventory.GetQuantity(itemID), 0);
	}

	private void UpdateDisplay(string changedItemID, int newQty, int oldQty)
	{
		if (changedItemID != itemID)
			return;

		quantityText.text = newQty > 0 ? newQty.ToString() : "0";
		quantityText.color = newQty > 0 ? Color.white : Color.gray;
	}

	private void OnDestroy()
	{
		if (inventory != null)
			inventory.OnQuantityChanged -= UpdateDisplay;
	}
}
```

### Example 2: Handle NPC Collision with Passive Item

```csharp
using FeedTheCat.Items;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[SerializeField] private CollisionDestructionItem passiveItem;

	public static event System.Action<NPCMover> OnNPCCollision;

	private void Start()
	{
		passiveItem = FindAnyObjectByType<CollisionDestructionItem>();
	}

	private void OnTriggerEnter(Collider other)
	{
		NPCMover npc = other.GetComponent<NPCMover>();
		if (npc == null)
			return;

		// Notify passive item system
		OnNPCCollision?.Invoke(npc);

		// Or direct call
		if (passiveItem != null)
		{
			if (passiveItem.CurrentQuantity > 0)
			{
				passiveItem.HandleNPCCollision(npc);
			}
			else
			{
				TriggerGameOver();
			}
		}
	}

	private void TriggerGameOver()
	{
		Debug.Log("Game Over! No more protective amulets.");
		// Show game over screen, restart level, etc.
	}
}
```

### Example 3: Countdown Status Effects on Player Turn

```csharp
using FeedTheCat.Items;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
	private StatusEffectSystem statusSystem;

	public event System.Action OnPlayerTurnEnd;

	private void Start()
	{
		statusSystem = StatusEffectSystem.Instance;
	}

	public void EndPlayerTurn()
	{
		Debug.Log("Player turn ended");

		// Notify all systems that turn ended
		// This triggers status effect countdown
		OnPlayerTurnEnd?.Invoke();

		// Start NPC turn
		StartNPCTurn();
	}

	private void StartNPCTurn()
	{
		// NPC AI moves, attacks, etc.
	}
}
```

Update StatusEffectSystem:
```csharp
private void OnEnable()
{
	// Subscribe to turn end event
	TurnManager turnManager = FindAnyObjectByType<TurnManager>();
	if (turnManager != null)
		turnManager.OnPlayerTurnEnd += OnPlayerStep;
}

private void OnDisable()
{
	TurnManager turnManager = FindAnyObjectByType<TurnManager>();
	if (turnManager != null)
		turnManager.OnPlayerTurnEnd -= OnPlayerStep;
}
```

### Example 4: Create Custom Item (Heal Spell)

```csharp
using FeedTheCat.Items;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom item that heals NPCs instead of harming them
/// </summary>
public class HealSpell : ItemBase
{
	[SerializeField] private int healAmount = 20;

	protected override bool ValidateTarget(GridCell target)
	{
		// Heal any valid cell
		return target != null;
	}

	protected override void ExecuteEffect(GridCell target)
	{
		if (itemData == null)
			return;

		// Find NPCs in radius
		List<NPCMover> affectedNPCs = RadiusHelper.GetNPCsInRadius(
			target, 
			itemData.EffectRadius
		);

		foreach (NPCMover npc in affectedNPCs)
		{
			// Assuming NPCs have health component
			Health health = npc.GetComponent<Health>();
			if (health != null)
			{
				health.Heal(healAmount);
				Debug.Log($"Healed {npc.gameObject.name} for {healAmount} HP");
			}
		}
	}
}
```

### Example 5: Reward Selection Handler

```csharp
using FeedTheCat.Rewards;
using FeedTheCat.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RewardScreenHandler : MonoBehaviour
{
	[SerializeField] private RewardScreenManager rewardScreenManager;
	[SerializeField] private RewardGenerator rewardGenerator;

	private void Start()
	{
		if (rewardScreenManager == null)
			rewardScreenManager = GetComponent<RewardScreenManager>();

		if (rewardGenerator == null)
			rewardGenerator = FindAnyObjectByType<RewardGenerator>();
	}

	/// <summary>
	/// Called when player selects a reward card
	/// </summary>
	public void OnRewardSelected(RewardCard selectedCard)
	{
		if (selectedCard == null)
			return;

		ItemData item = selectedCard.ItemData;
		int quantity = selectedCard.RewardQuantity;

		Debug.Log($"Player selected: {item.ItemName} x{quantity}");

		// Apply reward (already done by RewardGenerator)
		// Just provide feedback

		// Show confirmation
		ShowRewardConfirmation(item, quantity);

		// After delay, continue to next level
		Invoke(nameof(ContinueToNextLevel), 2f);
	}

	private void ShowRewardConfirmation(ItemData item, int quantity)
	{
		// Show UI feedback
		Debug.Log($"Added {quantity} x {item.ItemName} to inventory!");
	}

	private void ContinueToNextLevel()
	{
		SceneManager.LoadScene("Gameplay");
	}
}
```

### Example 6: Difficulty-Based Rewards Debug

```csharp
using FeedTheCat;
using FeedTheCat.Items;
using FeedTheCat.Rewards;
using UnityEngine;

public class RewardDebugger : MonoBehaviour
{
	private RewardGenerator rewardGenerator;
	private ItemCollector itemCollector;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.G))
		{
			GenerateTestRewards();
		}
	}

	private void GenerateTestRewards()
	{
		rewardGenerator = FindAnyObjectByType<RewardGenerator>();
		itemCollector = FindAnyObjectByType<ItemCollector>();

		if (rewardGenerator == null || itemCollector == null)
		{
			Debug.LogError("Missing RewardGenerator or ItemCollector");
			return;
		}

		Difficulty currentDifficulty = itemCollector.CurrentDifficulty;
		Debug.Log($"Current Difficulty: {currentDifficulty}");

		var rewards = rewardGenerator.GenerateRewardCards();

		foreach (var reward in rewards)
		{
			Debug.Log($"{reward.itemData.ItemName}: x{reward.quantity} ({reward.rarity})");
		}
	}
}
```

---

## Common Patterns

### Pattern 1: Conditional Item Usage

```csharp
if (ItemInventory.Instance.CanUse("charm_spell_2"))
{
	// Player can drag and use the item
	itemButton.interactable = true;
}
else
{
	// Item is out of stock
	itemButton.interactable = false;
	itemButton.image.color = Color.gray;
}
```

### Pattern 2: Check NPC Status Before Acting

```csharp
StatusEffectSystem status = StatusEffectSystem.Instance;

if (status.HasStatusEffect(npc, StatusEffectType.Stun))
{
	// NPC is stunned, can't move
	return;
}

if (status.HasStatusEffect(npc, StatusEffectType.Charm))
{
	// NPC is charmed, move toward charm target
	npc.MoveToward(charmTarget);
}
else
{
	// NPC follows normal AI
	npc.FollowAI();
}
```

### Pattern 3: Batch Item Operations

```csharp
ItemInventory inv = ItemInventory.Instance;

// Add multiple items at once
inv.IncreaseItem("charm_spell_2", 3);
inv.IncreaseItem("stun_blast_3", 2);
inv.IncreaseItem("protective_amulet", 1);

// Save once
inv.Save();
```

### Pattern 4: Item Reward Logic

```csharp
var reward = new RewardGenerator.GeneratedReward(
	itemData: charmsSpell,
	qty: 5,
	rarity: ItemRarity.Rare
);

// Apply to inventory
ItemInventory.Instance.IncreaseItem(reward.itemData.ItemID, reward.qty);
ItemInventory.Instance.Save();

// Notify UI
Debug.Log($"Received {reward.qty}x {reward.itemData.ItemName} ({reward.rarity})");
```

---

## Error Handling

### Null Reference Safety

```csharp
// Safe item usage
ItemInventory inv = ItemInventory.Instance;
if (inv != null && inv.CanUse("charm_spell_2"))
{
	// Use item safely
}

StatusEffectSystem status = StatusEffectSystem.Instance;
if (status != null && npc != null)
{
	status.ApplyStatusEffect(npc, StatusEffectType.Stun, 3);
}
```

### Validation Patterns

```csharp
// Validate ItemData before use
if (itemData == null)
{
	Debug.LogError("ItemBase: ItemData not assigned");
	return false;
}

if (string.IsNullOrEmpty(itemData.ItemID))
{
	Debug.LogError("ItemData: ItemID is empty");
	return false;
}

// Validate NPC before effect
if (npc == null || npc.gameObject == null)
{
	Debug.LogWarning("NPC is null, cannot apply effect");
	return;
}
```

---

## Performance Tips

### 1. Cache References

```csharp
private ItemInventory _inventory;
private StatusEffectSystem _status;

private void Start()
{
	_inventory = ItemInventory.Instance;
	_status = StatusEffectSystem.Instance;
}

// Reuse cached references instead of calling FindAnyObjectByType
```

### 2. Batch Operations

```csharp
// Less efficient: multiple saves
inv.IncreaseItem("item1", 1);
inv.Save();

inv.IncreaseItem("item2", 1);
inv.Save();

// More efficient: batch then save
inv.IncreaseItem("item1", 1);
inv.IncreaseItem("item2", 1);
inv.Save(); // Single save
```

### 3. Reuse Lists

```csharp
private List<NPCMover> _cachedNPCs = new List<NPCMover>();

private void UpdateEffects()
{
	_cachedNPCs.Clear();
	_cachedNPCs.AddRange(RadiusHelper.GetNPCsInRadius(cell, radius));

	foreach (var npc in _cachedNPCs)
	{
		// Process...
	}
}
```

---

## Testing Utilities

### Console Commands for Testing

```csharp
// In Update or public method
if (Input.GetKeyDown(KeyCode.I))
{
	ItemInventory.Instance.IncreaseItem("charm_spell_2", 10);
	ItemInventory.Instance.LogInventory();
}

if (Input.GetKeyDown(KeyCode.S))
{
	StatusEffectSystem.Instance.LogActiveEffects();
}

if (Input.GetKeyDown(KeyCode.R))
{
	RewardGenerator reward = FindAnyObjectByType<RewardGenerator>();
	reward.DebugGenerateRewards();
}
```

---

**Keep this file handy for development reference!** 🚀
