# Consumable Item System - Quick Start Checklist

## ✅ Phase 1: Scene Setup (5 minutes)

- [ ] Create **ItemInventory** GameObject in gameplay scene
  - [ ] Add ItemInventory component
  - [ ] Set `persistenceKeyPrefix = "inventory_"`
  - [ ] Set `maxQuantityPerItem = 999`

- [ ] Create **StatusEffectSystem** GameObject in gameplay scene
  - [ ] Add StatusEffectSystem component
  - [ ] Ensure it has Singleton setup

- [ ] Create **RewardGenerator** GameObject in reward screen scene
  - [ ] Add RewardGenerator component
  - [ ] (Will assign ItemData pool in Phase 2)

---

## ✅ Phase 2: ItemData Creation (10 minutes)

Create 5 ItemData ScriptableObjects:

- [ ] **Charm Spell 2** (`charm_spell_2`)
  - Type: Active | Radius: 2 | Effect: Charm | Duration: 2 | Rarity: Rare

- [ ] **Charm Aura 4** (`charm_aura_4`)
  - Type: Active | Radius: 3 | Effect: Charm | Duration: 4 | Rarity: Epic

- [ ] **Stun Blast 3** (`stun_blast_3`)
  - Type: Active | Radius: 3 | Effect: Stun | Duration: 3 | Rarity: Epic

- [ ] **Stun Pulse 4** (`stun_pulse_4all`)
  - Type: Active | Radius: EntireMap | Effect: Stun | Duration: 4 | Rarity: Legendary

- [ ] **Protective Amulet** (`protective_amulet`)
  - Type: Passive | Rarity: Rare | (Other fields can be defaults)

---

## ✅ Phase 3: UI Setup - Active Items (15 minutes)

For **each active item** in your Item Toolbar:

- [ ] Create Button UI element
- [ ] Add appropriate script component:
  - [ ] CharmItem2Radius for Charm Spell 2
  - [ ] CharmItem4Radius for Charm Aura 4
  - [ ] StunItem3Radius for Stun Blast 3
  - [ ] StunItemEntireMap for Stun Pulse 4

- [ ] For each item script, assign in Inspector:
  - [ ] `itemData` → ItemData asset (from Phase 2)
  - [ ] `canvasGroup` → Button's CanvasGroup component
  - [ ] `rectTransform` → Button's RectTransform component

---

## ✅ Phase 4: NPC Setup (10 minutes)

For **each NPC prefab**:

- [ ] Create child GameObject named **"stun"**
  - [ ] (Optional) Add visual effect (Sprite, Particle System, etc.)
  - [ ] Position under NPC root

- [ ] Create child GameObject named **"charm"**
  - [ ] (Optional) Add visual effect
  - [ ] Position under NPC root

- [ ] Verify NPCMover component exists

**Example Hierarchy:**
```
NPC_Enemy (NPCMover component)
├── stun (empty or with visual)
├── charm (empty or with visual)
└── [other children]
```

---

## ✅ Phase 5: Passive Item Setup (10 minutes)

- [ ] Create empty GameObject named **"PassiveItemSystem"**
  - [ ] Add CollisionDestructionItem component

- [ ] In CollisionDestructionItem Inspector:
  - [ ] Assign `itemData` → Protective Amulet ItemData
  - [ ] Assign `playerController` → Your PlayerController
  - [ ] Assign `itemInventory` → ItemInventory (or leave empty to auto-find)

- [ ] In PlayerController, add collision handler:
  ```csharp
  private void OnTriggerEnter(Collider other)
  {
	  NPCMover npc = other.GetComponent<NPCMover>();
	  if (npc != null)
	  {
		  passiveItem?.HandleNPCCollision(npc);
	  }
  }
  ```

---

## ✅ Phase 6: Reward Screen Setup (15 minutes)

- [ ] Create **Reward Screen Canvas**

- [ ] Create **5 RewardCard UI elements** (buttons or images):
  - [ ] Each must have Button component
  - [ ] Each must have:
	- [ ] Image component for icon
	- [ ] TextMeshProUGUI for name
	- [ ] TextMeshProUGUI for description
	- [ ] TextMeshProUGUI for quantity (displays "+X")
	- [ ] Image component for rarity background

- [ ] Create container element (e.g., "RewardCardsContainer") to hold the 5 cards

- [ ] Create **RewardScreenManager** GameObject:
  - [ ] Add RewardScreenManager component
  - [ ] Assign `rewardCardPrefab` → One of the 5 RewardCard prefabs
  - [ ] Assign `rewardCardsContainer` → The container Transform
  - [ ] Assign `rewardGenerator` → RewardGenerator GameObject

- [ ] Assign **RewardGenerator** component to RewardGenerator GameObject:
  - [ ] `rewardPool[0]` → Charm Spell 2
  - [ ] `rewardPool[1]` → Charm Aura 4
  - [ ] `rewardPool[2]` → Stun Blast 3
  - [ ] `rewardPool[3]` → Stun Pulse 4
  - [ ] `rewardPool[4]` → Protective Amulet
  - [ ] `itemCollector` → ItemCollector (in scene with difficulty parsing)

---

## ✅ Phase 7: Integration Verification (5 minutes)

- [ ] **Test in Editor:**

  1. Open Gameplay scene
  2. Play game
  3. In Console, run:
	 ```
	 ItemInventory.Instance.IncreaseItem("charm_spell_2", 5);
	 ItemInventory.Instance.LogInventory();
	 ```
  4. Verify output shows 5 charges

  5. Drag a charm item onto a grid cell with NPCs nearby
  6. Check Console for "Charmed NPC" message

  7. Verify NPC "charm" child object is enabled

  8. Open Reward Screen
  9. Run `RewardScreenManager.RegenerateRewards()`
  10. Verify 3 cards appear with different rarities

- [ ] **Check PlayerPrefs Persistence:**

  1. Give player items in one session
  2. Close game
  3. Reopen game
  4. Verify items are still in inventory

---

## ✅ Phase 8: Optional Enhancements (20+ minutes)

- [ ] Add **UI Quantity Display** for each active item
  ```csharp
  ItemInventory.Instance.OnQuantityChanged += (id, newQty, _) =>
  {
	  if (id == "charm_spell_2")
		  quantityText.text = newQty.ToString();
  };
  ```

- [ ] Add **Confirmation Dialog** for passive item usage
  - [ ] Show "Use amulet?" prompt on collision
  - [ ] Let player choose yes/no

- [ ] Add **Cooldown Visuals** for items
  - [ ] Dim button when on cooldown
  - [ ] Show countdown timer

- [ ] Add **Sound Effects**
  - [ ] When item is used
  - [ ] When status effect starts/ends
  - [ ] When reward is selected

- [ ] Add **Particle Effects**
  - [ ] When charm effect activates
  - [ ] When stun effect activates
  - [ ] NPC destruction animation

---

## Common Integration Points

### PlayerController Hook (REQUIRED)
```csharp
// PlayerController.cs
[SerializeField] private CollisionDestructionItem passiveItem;

private void OnTriggerEnter(Collider other)
{
	NPCMover npc = other.GetComponent<NPCMover>();
	if (npc != null)
	{
		if (passiveItem != null && passiveItem.CurrentQuantity > 0)
			passiveItem.HandleNPCCollision(npc);
		else
			TriggerGameOver();
	}
}
```

### TurnManager Hook (OPTIONAL but RECOMMENDED)
```csharp
// TurnManager.cs (or similar)
public event System.Action OnPlayerTurnEnd;

private void EndPlayerTurn()
{
	// Countdown status effects
	OnPlayerTurnEnd?.Invoke();

	// StatusEffectSystem listens for this event
}
```

Update StatusEffectSystem to listen:
```csharp
private void OnEnable()
{
	TurnManager.Instance.OnPlayerTurnEnd += OnPlayerStep;
}

private void OnDisable()
{
	TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerStep;
}
```

### ItemCollector Integration (AUTOMATIC)
- ItemCollector.CurrentDifficulty is already integrated
- RewardGenerator reads it automatically
- No additional setup needed if ItemCollector is in scene

---

## Testing Checklist

- [ ] **Drag & Drop:**
  - [ ] Can drag active items
  - [ ] Can't drag without quantity
  - [ ] Effect applies on valid target
  - [ ] Effect reverses on invalid target
  - [ ] Quantity decreases after use

- [ ] **Status Effects:**
  - [ ] Visual indicators appear
  - [ ] NPC can't move while stunned
  - [ ] Effects countdown
  - [ ] Duration expires correctly
  - [ ] Multiple effects don't stack wrong

- [ ] **Persistence:**
  - [ ] Items save to PlayerPrefs
  - [ ] Items load on game restart
  - [ ] Quantities correct after reload

- [ ] **Passive Item:**
  - [ ] Protects player on collision
  - [ ] Destroys NPC when used
  - [ ] Game over when quantity = 0
  - [ ] Can't use without quantity

- [ ] **Rewards:**
  - [ ] Cards display correctly
  - [ ] Rarity colors match
  - [ ] Difficulty affects probabilities
  - [ ] No duplicate cards
  - [ ] Selection adds to inventory

---

## Troubleshooting Quick Links

| Issue | Solution |
|-------|----------|
| Items not draggable | Check ItemType = Active, quantity > 0, CanvasGroup assigned |
| Status effects not showing | Check NPC has "stun"/"charm" children, system is in scene |
| Rewards not saving | Call `itemInventory.Save()`, check ItemID unique |
| Radius not working | Check GridCell positions valid, NPCs findable, enum correct |
| Effects not counting down | Add TurnManager hook or call OnPlayerStep() manually |

---

## File Checklist (All Should Exist)

- [ ] Assets/Script/Item/Base/ItemBase.cs
- [ ] Assets/Script/Item/Data/ItemData.cs
- [ ] Assets/Script/Item/Inventory/ItemInventory.cs
- [ ] Assets/Script/Item/Status/StatusEffectSystem.cs
- [ ] Assets/Script/Item/Enums/ItemType.cs
- [ ] Assets/Script/Item/Enums/ItemRarity.cs
- [ ] Assets/Script/Item/Enums/StatusEffectType.cs
- [ ] Assets/Script/Item/Enums/TargetType.cs
- [ ] Assets/Script/Item/Enums/EffectRadiusType.cs
- [ ] Assets/Script/Item/Utility/RadiusHelper.cs
- [ ] Assets/Script/Item/Implementations/CharmItem2Radius.cs
- [ ] Assets/Script/Item/Implementations/CharmItem4Radius.cs
- [ ] Assets/Script/Item/Implementations/StunItem3Radius.cs
- [ ] Assets/Script/Item/Implementations/StunItemEntireMap.cs
- [ ] Assets/Script/Item/Implementations/CollisionDestructionItem.cs
- [ ] Assets/Script/UI/RewardCard.cs
- [ ] Assets/Script/Reward/RewardGenerator.cs
- [ ] Assets/Script/Reward/RewardScreenManager.cs
- [ ] Assets/Script/Manager/Difficulty.cs
- [ ] Assets/Script/CONSUMABLE_ITEM_SYSTEM_GUIDE.md (this guide)

---

## Estimated Time to Full Integration

- **Minimal Setup (drag-drop + persistence):** 30 minutes
- **Full Setup (all phases):** 2-3 hours
- **With Enhancements:** 4-6 hours

**Enjoy your complete consumable item system! 🎮✨**
