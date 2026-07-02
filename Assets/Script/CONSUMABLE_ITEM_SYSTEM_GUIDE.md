# Consumable Item System - Complete Usage Guide

## Overview

This consumable item system provides a complete framework for implementing limited-use items in your FeedTheCat puzzle game. The system includes:

- **5 Active Items** with drag-drop mechanics and status effects
- **1 Passive Item** for NPC destruction on collision
- **Reward Card UI** with rarity colors and difficulty-based generation
- **Inventory persistence** using PlayerPrefs
- **Status Effect System** for Charm and Stun effects on NPCs

---

## Architecture Overview

### Core Components

```
ItemBase (Abstract)
├── CharmItem2Radius
├── CharmItem4Radius
├── StunItem3Radius
└── StunItemEntireMap

CollisionDestructionItem (Passive, standalone)

ItemData (ScriptableObject)
├── ItemID, ItemName, Description
├── Effect Properties (Radius, Type, Duration)
└── Rarity, Cooldown

ItemInventory (Singleton)
├── Stores quantities in memory
├── Persists via PlayerPrefs
└── Raises events on quantity changes

StatusEffectSystem (Singleton)
├── Tracks active effects on NPCs
├── Manages duration countdown
└── Enables/Disables visual indicators

RadiusHelper (Utility)
└── Finds NPCs within effect radius

RewardGenerator & RewardCard
└── Creates random rewards based on difficulty

Enums:
├── ItemType (Active, Passive)
├── ItemRarity (Common, Rare, Epic, Legendary)
├── EffectRadiusType (Radius1, 2, 3, EntireMap)
├── StatusEffectType (None, Charm, Stun)
├── TargetType (NPC, Player, Mixed)
└── Difficulty (Easy, Medium, Hard)
```

---

## Setup Instructions

### Step 1: Create ItemInventory Singleton

1. In your Gameplay scene (or main game scene):
   - Create a new **empty GameObject** named `ItemInventory`
   - Add the **ItemInventory** component from `Assets/Script/Item/Inventory/ItemInventory.cs`
   - In Inspector, set `persistenceKeyPrefix = "inventory_"`
   - Set `maxQuantityPerItem = 999` (or your preferred cap)
   - **Important:** This must be persistent across scenes
	 - Mark it with `DontDestroyOnLoad()` in the script (already done)

### Step 2: Create StatusEffectSystem Singleton

1. Create a new **empty GameObject** named `StatusEffectSystem`
2. Add the **StatusEffectSystem** component
3. Leave it in the Gameplay scene or make it persistent if needed
4. **Critical:** NPCs must have child objects named **"stun"** and **"charm"** for visual indicators

### Step 3: Create ItemData Assets

1. Right-click in your Assets folder → **Create** → **FeedTheCat** → **Item** → **ItemData**
2. Create 5 items:

#### Item 1: Charm Spell (2-Radius)
- **Item ID:** `charm_spell_2`
- **Item Name:** Charm Spell
- **Description:** Charm a nearby NPC to move toward target. 2-turn duration.
- **Item Type:** Active
- **Effect Radius:** Radius2
- **Status Effect:** Charm
- **Effect Duration:** 2
- **Rarity:** Rare

#### Item 2: Charm Aura (3-Radius)
- **Item ID:** `charm_aura_4`
- **Item Name:** Charm Aura
- **Description:** Charm nearby NPCs with extended range. 4-turn duration.
- **Item Type:** Active
- **Effect Radius:** Radius3
- **Status Effect:** Charm
- **Effect Duration:** 4
- **Rarity:** Epic

#### Item 3: Stun Blast (3-Radius)
- **Item ID:** `stun_blast_3`
- **Item Name:** Stun Blast
- **Description:** Stun all nearby NPCs, preventing movement for 3 turns.
- **Item Type:** Active
- **Effect Radius:** Radius3
- **Status Effect:** Stun
- **Effect Duration:** 3
- **Rarity:** Epic

#### Item 4: Stun Pulse (Entire Map)
- **Item ID:** `stun_pulse_4all`
- **Item Name:** Stun Pulse
- **Description:** Stun ALL NPCs on the map for 4 turns.
- **Item Type:** Active
- **Effect Radius:** EntireMap
- **Status Effect:** Stun
- **Effect Duration:** 4
- **Rarity:** Legendary

#### Item 5: Protective Amulet (Passive)
- **Item ID:** `protective_amulet`
- **Item Name:** Protective Amulet
- **Description:** Saves you when colliding with an NPC. Consumes one charge per use.
- **Item Type:** Passive
- **Effect Radius:** Radius1 (not used)
- **Status Effect:** None
- **Effect Duration:** 0
- **Rarity:** Rare

### Step 4: Set Up Item UI

1. **For Active Items (Charm/Stun spells):**
   - Create UI buttons in your Item Toolbar
   - Add **ItemBase-derived scripts** (CharmItem2Radius, StunItem3Radius, etc.) to each button
   - Assign the button's Image component to the `canvasGroup` field
   - Assign the ItemData ScriptableObject to the `itemData` field
   - Assign the Button's RectTransform to the `rectTransform` field

2. **For Passive Item (Protective Amulet):**
   - Create an empty GameObject for the passive system (doesn't need UI)
   - Add **CollisionDestructionItem** component
   - Assign the ItemData for the Protective Amulet
   - Assign your PlayerController reference
   - **Hook PlayerController collision events** (see Integration section)

### Step 5: Set Up Reward Screen

1. Create a Canvas for the reward screen
2. Create 5 **RewardCard** UI elements (can be buttons or images with Button component):
   - Each should have:
	 - Image component (for icon)
	 - TextMeshProUGUI for name
	 - TextMeshProUGUI for description
	 - TextMeshProUGUI for quantity
	 - Image for rarity background
	 - Button component (for selection)
3. Add **RewardScreenManager** to an empty GameObject:
   - Assign the RewardCard prefab (or use one of the 5 manually created cards)
   - Assign the container Transform
   - Assign RewardGenerator reference
4. Add **RewardGenerator** to an empty GameObject:
   - Assign 5 ItemData assets to the reward pool (in order)
   - Assign ItemCollector reference

---

## Script Usage & Integration

### Using ItemBase Items (Charm/Stun)

```csharp
// These items are drag-droppable by default
// Player drags them onto a GridCell to execute the effect

// Example: CharmItem2Radius
public class MyCharmItem : CharmItem2Radius
{
	// Inherits all drag-drop logic from ItemBase
	// override ExecuteEffect to customize behavior

	protected override void ExecuteEffect(GridCell target)
	{
		base.ExecuteEffect(target); // Or custom implementation
	}
}
```

### Using ItemInventory

```csharp
// Get singleton
ItemInventory inventory = ItemInventory.Instance;

// Get quantity
int qty = inventory.GetQuantity("charm_spell_2");

// Add items
inventory.IncreaseItem("charm_spell_2", 3); // Add 3 charges

// Remove items
inventory.DecreaseItem("charm_spell_2", 1); // Use 1 charge

// Check if item can be used
if (inventory.CanUse("charm_spell_2"))
{
	// Item has quantity > 0
}

// Subscribe to quantity changes
inventory.OnQuantityChanged += (itemID, newQty, prevQty) =>
{
	Debug.Log($"{itemID} changed from {prevQty} to {newQty}");
	UpdateUI(); // Refresh UI
};

// Save to PlayerPrefs
inventory.Save();

// Load from PlayerPrefs
inventory.Load();
```

### Using StatusEffectSystem

```csharp
StatusEffectSystem statusSystem = StatusEffectSystem.Instance;

// Apply effect to NPC
statusSystem.ApplyStatusEffect(npcMover, StatusEffectType.Stun, 3);

// Check if NPC has effect
if (statusSystem.HasStatusEffect(npcMover, StatusEffectType.Stun))
{
	// NPC is stunned
}

// Get remaining duration
int remaining = statusSystem.GetEffectDuration(npcMover, StatusEffectType.Stun);

// Remove effect manually
statusSystem.RemoveStatusEffect(npcMover, StatusEffectType.Stun);

// Log active effects
statusSystem.LogActiveEffects();
```

### Using RadiusHelper

```csharp
// Get all NPCs within radius
List<NPCMover> affected = RadiusHelper.GetNPCsInRadius(targetCell, EffectRadiusType.Radius2);

foreach (NPCMover npc in affected)
{
	// Apply effects...
}

// Check single NPC
bool isInRange = RadiusHelper.IsNPCInRadius(targetCell, npc, EffectRadiusType.Radius2);

// Debug logging
RadiusHelper.DebugLogNPCsInRadius(targetCell, EffectRadiusType.Radius2);
```

### Using RewardGenerator

```csharp
RewardGenerator generator = GetComponent<RewardGenerator>();

// Generate 3 random rewards based on current difficulty
List<RewardGenerator.GeneratedReward> rewards = generator.GenerateRewardCards();

foreach (var reward in rewards)
{
	Debug.Log($"{reward.itemData.ItemName}: {reward.quantity}x ({reward.rarity})");
}

// Apply a reward to inventory
generator.ApplyReward(rewards[0]);
```

### Using RewardCard

```csharp
RewardCard card = GetComponent<RewardCard>();

ItemData item = ScriptableObject.CreateInstance<ItemData>();
item.ItemName = "Fire Spell";
// ... set other properties ...

card.PopulateCard(item, 5, OnCardSelected);

private void OnCardSelected(RewardCard card)
{
	Debug.Log($"Player selected {card.ItemData.ItemName}");
}
```

---

## Integration with PlayerController (Collision)

### Hook Collision Detection

You need to **modify your PlayerController** to support the passive item collision:

```csharp
// In PlayerController.cs

[SerializeField] private CollisionDestructionItem passiveItem;

private void Start()
{
	passiveItem = FindAnyObjectByType<CollisionDestructionItem>();
}

// When player collides with NPC
private void OnTriggerEnter(Collider other)
{
	NPCMover npc = other.GetComponent<NPCMover>();
	if (npc != null)
	{
		// Notify passive item system
		if (passiveItem != null)
			passiveItem.HandleNPCCollision(npc);
		else
			TriggerGameOver(); // No passive item protection
	}
}

// Or use event pattern
public static event System.Action<NPCMover> OnNPCCollision;

private void OnTriggerEnter(Collider other)
{
	NPCMover npc = other.GetComponent<NPCMover>();
	if (npc != null)
	{
		OnNPCCollision?.Invoke(npc);
	}
}
```

Then subscribe in CollisionDestructionItem:

```csharp
private void OnEnable()
{
	PlayerController.OnNPCCollision += HandleNPCCollision;
}

private void OnDisable()
{
	PlayerController.OnNPCCollision -= HandleNPCCollision;
}
```

---

## Difficulty-Based Reward Rates

The RewardGenerator creates rewards based on **ItemCollector.CurrentDifficulty**:

### Easy Difficulty
- **Common:** 60% drop rate, 1-5 quantity
- **Rare:** 35% drop rate, 1-3 quantity
- **Epic:** 5% drop rate, 1 quantity

### Medium Difficulty
- **Common:** 35% drop rate, 2-5 quantity
- **Rare:** 45% drop rate, 2-6 quantity
- **Epic:** 20% drop rate, 1-3 quantity

### Hard Difficulty
- **Common:** 25% drop rate, 5-8 quantity
- **Rare:** 40% drop rate, 3-8 quantity
- **Epic:** 25% drop rate, 2-5 quantity
- **Legendary:** 5% drop rate, 1 quantity

The difficulty is parsed from **level names** in format: `{difficulty}_{levelName}`
- Example: `Easy_Level1`, `Hard_BossStage`

---

## Status Effect Visual Indicators

### NPC Setup

Each NPC must have **two child GameObjects**:

```
NPC (parent)
├── stun (GameObject)
│   └── [Optional: Image/Particle for stun effect]
└── charm (GameObject)
	└── [Optional: Image/Particle for charm effect]
```

When a status effect is applied:
- The corresponding child GameObject is **enabled**
- When the effect expires, the child is **disabled**

### Example Prefab Structure

```csharp
public class MyNPC : NPCMover
{
	private void Start()
	{
		// StatusEffectSystem automatically finds and manages these children
		// Just ensure they exist in the prefab
	}
}
```

---

## Testing & Debugging

### Test Item Quantities

```csharp
// In-game, open Console and run:
ItemInventory.Instance.IncreaseItem("charm_spell_2", 10);
ItemInventory.Instance.LogInventory();
```

### Test Status Effects

```csharp
// Stun an NPC for testing
StatusEffectSystem.Instance.ApplyStatusEffect(someNPC, StatusEffectType.Stun, 3);
StatusEffectSystem.Instance.LogActiveEffects();
```

### Test Rewards

```csharp
// Regenerate rewards on reward screen
RewardScreenManager manager = GetComponent<RewardScreenManager>();
manager.RegenerateRewards();
manager.LogCurrentRewards();
```

### Context Menu Commands

All main scripts have **[ContextMenu]** debug methods. Right-click the component in Inspector:
- **ItemInventory:** "Log Inventory"
- **StatusEffectSystem:** "Log Active Effects"
- **RewardGenerator:** "Generate Test Rewards"
- **RewardCard:** "Log Card Info"
- **ItemBase:** "Log Item Info"

---

## Common Issues & Solutions

### Issue: Items Not Draggable
- **Check:** ItemData.ItemType must be `Active`, not `Passive`
- **Check:** ItemInventory.GetQuantity() must return > 0
- **Check:** CanvasGroup component must be assigned

### Issue: Status Effects Not Showing
- **Check:** NPCs must have child GameObjects named "stun" and "charm"
- **Check:** StatusEffectSystem must be in scene
- **Check:** Effect duration > 0 and type != None

### Issue: Rewards Not Saving
- **Check:** Call `itemInventory.Save()` after applying rewards
- **Check:** ItemID must be unique across all items
- **Check:** PlayerPrefs has storage space

### Issue: Radius Not Working
- **Check:** GridCell objects must have valid positions
- **Check:** NPCMover objects must be in scene and findable
- **Check:** EffectRadiusType enum value must match ItemData

---

## Performance Considerations

- **StatusEffectSystem:** O(n) scan per player turn, acceptable for ~20 NPCs
- **RadiusHelper:** O(n) NPC scan per item use, consider caching NPCs if > 50
- **ItemInventory:** O(1) lookups, minimal memory footprint
- **RewardGenerator:** O(5) item pool, negligible cost

For large-scale projects (100+ NPCs), consider:
- Spatial partitioning for radius checks
- Object pooling for status effects
- Caching NPC references

---

## Example: Complete Level Flow

```csharp
// Level Setup
1. Load level with difficulty (Easy_Level1)
2. ItemCollector reads difficulty → Easy
3. ItemInventory initializes
4. StatusEffectSystem initializes
5. NPCs spawn with stun/charm child objects

// Gameplay
1. Player can drag items onto grid cells
2. Items apply status effects via StatusEffectSystem
3. Effects countdown each player turn
4. Passive item prevents game over if quantity > 0

// End Level / Reward Screen
1. RewardGenerator creates 3 random cards
2. Difficulty-based probabilities apply
3. Player selects 1 reward
4. Quantity added to inventory
5. Inventory saved to PlayerPrefs
6. Next level loads
```

---

## File Structure

```
Assets/Script/
├── Item/
│   ├── Base/
│   │   └── ItemBase.cs
│   ├── Data/
│   │   └── ItemData.cs
│   ├── Inventory/
│   │   └── ItemInventory.cs
│   ├── Status/
│   │   └── StatusEffectSystem.cs
│   ├── Enums/
│   │   ├── ItemType.cs
│   │   ├── ItemRarity.cs
│   │   ├── StatusEffectType.cs
│   │   ├── TargetType.cs
│   │   └── EffectRadiusType.cs
│   ├── Utility/
│   │   └── RadiusHelper.cs
│   └── Implementations/
│       ├── CharmItem2Radius.cs
│       ├── CharmItem4Radius.cs
│       ├── StunItem3Radius.cs
│       ├── StunItemEntireMap.cs
│       └── CollisionDestructionItem.cs
├── UI/
│   └── RewardCard.cs
├── Reward/
│   ├── RewardGenerator.cs
│   └── RewardScreenManager.cs
├── Manager/
│   └── Difficulty.cs
└── Grid/
	└── ItemCollector.cs (modified)
```

---

## Next Steps

1. **Create ItemData ScriptableObjects** for all 5 items
2. **Set up ItemInventory** in your gameplay scene
3. **Set up StatusEffectSystem** in your gameplay scene
4. **Create UI buttons** for active items with ItemBase components
5. **Hook PlayerController** collision to CollisionDestructionItem
6. **Create reward screen** with RewardCard prefabs
7. **Modify NPCs** to include "stun" and "charm" child objects
8. **Test in-game** using Console debug commands

---

**Happy building! The system is production-ready and fully extensible. 🎮**
