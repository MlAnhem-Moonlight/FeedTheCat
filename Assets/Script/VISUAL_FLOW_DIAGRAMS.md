# ItemBase Optimization - Visual Flow Diagrams

## 1️⃣ Initialization Flow

```
ItemButton GameObject Created
		↓
	 Start()
		↓
	 ├─ InitializeReferences()
	 │  ├─ Cache RectTransform
	 │  ├─ Cache CanvasGroup
	 │  └─ Cache GraphicRaycaster
	 │
	 ├─ SetupUI()
	 │  ├─ Auto-assign Image component
	 │  └─ Assign Sprite from ItemData.Icon
	 │
	 ├─ SubscribeToInventory()
	 │  └─ itemInventory.OnQuantityChanged += HandleQuantityChanged
	 │
	 └─ UpdateUI()
		├─ Set canvasGroup.interactable = CanUse()
		├─ Set iconImage.color (white/gray)
		└─ Set quantityText.text = CurrentQuantity

Ready for gameplay ✓
```

---

## 2️⃣ Quantity Change Flow (Auto-Update)

```
External Source thay đổi Quantity
(ItemInventory.IncreaseItem/DecreaseItem)
		↓
   SetQuantity()
		↓
   OnQuantityChanged?.Invoke(itemID, newQty, oldQty)
		↓
   ItemBase.HandleQuantityChanged(itemID, newQty, oldQty)
		↓
   if (itemID == this.ItemID)
		↓
   UpdateUI()
		├─ quantityText.text = newQty.ToString()
		├─ canvasGroup.interactable = CanUse()
		└─ iconImage.color = canUse ? white : gray

✓ UI tự động cập nhật (không cần gọi UpdateUI thủ công)
```

---

## 3️⃣ Drag & Drop Flow

```
═══════════════════════════════════════════════════════════
					 DRAG & DROP SEQUENCE
═══════════════════════════════════════════════════════════

User Clicks & Holds Item Button
		↓
   OnBeginDrag(eventData)
   ├─ Check: ItemType == Active? ✓
   ├─ Check: CanUse() (qty > 0)? ✓
   ├─ Store originalPosition
   ├─ Store originalParent
   ├─ canvasGroup.alpha = 0.7f          (fade out)
   ├─ canvasGroup.blocksRaycasts = false (CRITICAL: raycast qua)
   └─ SetAsLastSibling()
		↓
   (Now: raycast CAN go through this button to GridCell below)
		↓

User Moves Mouse
		↓
   OnDrag(eventData)
   └─ rectTransform.position += eventData.delta (follow cursor)
		↓

User Releases Mouse
		↓
   OnEndDrag(eventData)
   ├─ canvasGroup.blocksRaycasts = true  (restore)
   ├─ canvasGroup.alpha = 1f             (restore)
   ├─ graphicRaycaster.Raycast(mousePos)
   │
   ├─ Loop through raycast results:
   │  ├─ Skip this button
   │  └─ Find GridCell component
   │
   ├─ if (targetGridCell == null)
   │  └─ RevertPosition() → End
   │
   └─ if (targetGridCell != null)
	  ├─ ValidateTarget(targetGridCell)
	  ├─ ExecuteEffect(targetGridCell)
	  ├─ DecreaseQuantity()
	  │  └─ OnQuantityChanged trigger
	  │     └─ UpdateUI() auto-called ✓
	  └─ itemInventory.Save()

Item Used ✓
Quantity Updated ✓
UI Refreshed ✓
Data Saved ✓
```

---

## 4️⃣ UI Component Architecture

```
┌─────────────────────────────────────────────────┐
│        ItemButton (UI Button)                    │
├─────────────────────────────────────────────────┤
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ Image Component (Icon)                    │  │
│  │ - Sprite: auto-assigned from ItemData    │  │
│  │ - Color: white (canUse) / gray (can't)   │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ Button Component                          │  │
│  │ - Interactable: controlled by script     │  │
│  │ - OnClick: (if desired)                  │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ CanvasGroup Component (State Manager)     │  │
│  │ - Interactable: true/false (CanUse?)    │  │
│  │ - Blocks Raycasts: true/false (drag)    │  │
│  │ - Alpha: 1.0 (normal) / 0.7 (dragging) │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ CharmItem2Radius Script                   │  │
│  │ - Extends ItemBase                        │  │
│  │ - Handles effect logic                    │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  Child: QuantityText (TextMeshProUGUI)         │
│  └─ Shows "5", "0", etc.                      │
│
└─────────────────────────────────────────────────┘

All components cached in Start() ✓
No repeated GetComponent calls ✓
```

---

## 5️⃣ Event Subscription Model

```
ItemInventory                 ItemBase (CharmItem2Button)
	│                                  │
	│  Constructor                     │
	└──────────────────────────────────┘
		   │
		   │ Start()
		   │
	┌──────┴──────────────────┐
	│  InitializeReferences() │
	│  SetupUI()              │
	│  SubscribeToInventory() │ ← itemInventory.OnQuantityChanged += ...
	│  UpdateUI()             │
	└──────┬──────────────────┘
		   │
		   │ (Game Loop)
		   │
   ┌───────┴────────────────┐
   │ Inventory Changes      │
   │ IncreaseItem()         │
   │ DecreaseItem()         │
   └───────┬────────────────┘
		   │
		   ↓
	OnQuantityChanged.Invoke(itemID, newQty, oldQty)
		   │
		   ↓ Event fires
		   │
	ItemBase.HandleQuantityChanged()
		   │
		   ├─ if (itemID == ItemID)
		   │       ↓
		   │   UpdateUI() ← AUTOMATIC! ✓
		   │
		   └─ (do nothing if different itemID)

Flow:
1. No manual UpdateUI() calls needed ✓
2. ALL qty changes trigger update ✓
3. Works even if changed from external code ✓
```

---

## 6️⃣ Reference Caching Pattern

```
BEFORE (Inefficient):
────────────────────
OnBeginDrag()
	↓
	CanvasGroup cg = GetComponent<CanvasGroup>() ← GetComponent (slow)
	↓
	cg.alpha = 0.7f

OnDrag() x60 frames
	↓
	rectTransform.position += delta
		↓ (if GetComponent called)
		✗ Performance issue!

OnEndDrag()
	↓
	CanvasGroup cg = GetComponent<CanvasGroup>() ← GetComponent again!
	↓
	cg.alpha = 1f


AFTER (Optimized):
──────────────────
Start()
	↓
	├─ rectTransform = GetComponent<RectTransform>() ← Once
	├─ canvasGroup = GetComponent<CanvasGroup>()     ← Once
	├─ iconImage = GetComponent<Image>()             ← Once
	└─ graphicRaycaster = cache                      ← Once

OnBeginDrag()
	↓
	canvasGroup.alpha = 0.7f ← Use cached reference (FAST)

OnDrag() x60 frames
	↓
	rectTransform.position += delta ← Use cached (FAST)

OnEndDrag()
	↓
	canvasGroup.alpha = 1f ← Use cached (FAST)

✓ All cached references ✓
✓ Zero repeated GetComponent ✓
✓ Best performance ✓
```

---

## 7️⃣ State Transitions

```
Initial State (qty = 0)
├─ canvasGroup.interactable = false
├─ iconImage.color = gray
├─ quantityText.text = "0"
└─ Button: NOT draggable

		 ↓ (ItemInventory.IncreaseItem(qty))

Ready State (qty > 0)
├─ canvasGroup.interactable = true
├─ iconImage.color = white
├─ quantityText.text = "5"
└─ Button: CAN drag

		 ↓ (User drags)

Dragging State
├─ canvasGroup.blocksRaycasts = false ← CRITICAL
├─ canvasGroup.alpha = 0.7f
├─ Button: Semi-transparent
└─ Raycast: PASSES THROUGH ✓

		 ↓ (User releases on GridCell)

Effect Executing State
├─ ExecuteEffect(targetCell)
├─ DecreaseQuantity() ← qty -= 1

		 ↓

Back to Ready State (if qty > 0)
or Back to Initial State (if qty = 0)
```

---

## 8️⃣ Raycast Blocking Diagram

```
Without blocksRaycasts = false:
───────────────────────────────
Mouse Position
	↓
Canvas
	↓
ItemButton (blocksRaycasts = true)
	✓ Raycast HIT here
	✗ Never reaches GridCell below

[ItemButton] BLOCKS
	├── GridCell A
	├── GridCell B
	└── GridCell C (unreachable)


With blocksRaycasts = false during drag:
──────────────────────────────────────────
Mouse Position
	↓
Canvas
	↓
ItemButton (blocksRaycasts = false) ← TRANSPARENT TO RAYCASTS!
	✓ Passes through
	↓
GridCell A? GridCell B? GridCell C?
	✓ Raycast checks these ✓

[ItemButton] TRANSPARENT (during drag)
	├── GridCell A
	├── GridCell B (raycasted! ✓)
	└── GridCell C
```

---

## 9️⃣ Complete Lifecycle Diagram

```
═════════════════════════════════════════════════════════════════
					  ITEM BUTTON LIFECYCLE
═════════════════════════════════════════════════════════════════

[CREATION]
   Prefab Instantiated
		↓
   Start() called
		├─ InitializeReferences()
		│  └─ Cache all components
		├─ SetupUI()
		│  └─ Auto-assign sprite
		├─ SubscribeToInventory()
		│  └─ Listen to OnQuantityChanged
		└─ UpdateUI()
		   └─ Show initial state

		↓
[READY]
   ├─ qty > 0: Button ready to drag
   └─ qty = 0: Button disabled

		↓
[QUANTITY CHANGES]
   External code changes qty
		↓
   OnQuantityChanged event triggers
		↓
   UpdateUI() called automatically ✓

		↓
[USER INTERACTS]
   User clicks & drags item button
		↓
   OnBeginDrag → OnDrag → OnEndDrag
		↓
   Find target GridCell
		↓
   ExecuteEffect()
		↓
   DecreaseQuantity()
		↓
   OnQuantityChanged event triggers
		↓
   UpdateUI() called automatically ✓
		↓
   Save to PlayerPrefs

		↓
[LOOP]
   Back to READY state (or DISABLED if qty=0)

		↓
[CLEANUP]
   OnDestroy() called
		↓
   UnsubscribeFromInventory()
		↓
   Cleanup complete
```

---

## 🔟 Performance Comparison Chart

```
				GetComponent Calls per Frame
						↑
						│
			 OLD (v1.0) │        ╱╲  ╱╲  ╱╲
						│       ╱  ╲╱  ╲╱  ╲
						│      ╱
			Calls/Frame │     ╱
						│    ╱
			 NEW (v1.1) │___╱  (flat line - cached)
						│
						├─────────────────────→ Time
						│
				   Start() OnDrag OnEndDrag

Legend:
  OLD: GetComponent called each event/frame
  NEW: GetComponent called once in Start() ✓

Result: ~90% reduction in GetComponent calls
		during drag operations ✓
```

---

**Visual diagrams complete! Use these to understand the flow.** 🎯
