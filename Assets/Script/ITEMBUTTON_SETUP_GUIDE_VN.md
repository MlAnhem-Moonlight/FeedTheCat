# ItemButton Setup Guide - Tối Ưu Hóa Hiển Thị

## 🎯 Overview (Tổng Quan)

ItemBase đã được tối ưu hóa hoàn toàn theo yêu cầu:

✅ **Dùng Image của ItemButton làm icon** - Tự động gán sprite từ ItemData
✅ **Hiển thị số lượng** - TextMeshProUGUI tự động cập nhật
✅ **CanvasGroup quản lý trạng thái** - Interactable, alpha, blocksRaycasts
✅ **Drag tắt blocksRaycasts** - Raycast đi qua xuống GridCell
✅ **Tự cập nhật UI** - Subscribe vào OnQuantityChanged từ ItemInventory
✅ **Tự gán sprite** - Từ ItemData.Icon trong SetupUI()
✅ **Không GetComponent lặp** - Cache tất cả reference trong Start()

---

## 🛠️ Setup Step-by-Step

### Step 1: Tạo ItemButton UI Prefab

Trong Canvas của bạn, tạo một Button UI:

```
Canvas
└── ItemToolbar (Panel)
	├── CharmItem2Button
	│   ├── Image (component - sẽ hiển thị icon)
	│   ├── Button (component)
	│   ├── CanvasGroup (component - bắt buộc)
	│   └── QuantityText (TextMeshProUGUI - child object)
	│
	├── StunItem3Button (tương tự)
	└── ...
```

#### Chi tiết tạo CharmItem2Button:

1. **Tạo Button UI:**
   - Right-click ItemToolbar → UI → Legacy → Button
   - Đặt tên: `CharmItem2Button`
   - Kích thước: 80x80 (hoặc tùy ý)

2. **Thêm CanvasGroup (Bắt buộc):**
   - Select `CharmItem2Button`
   - Add Component → CanvasGroup
   - Cài đặt:
	 - **Interactable:** true (sẽ được thay đổi bởi script)
	 - **Blocks Raycasts:** true (sẽ tắt/bật khi drag)
	 - **Alpha:** 1

3. **Setup Image (Icon):**
   - Select `CharmItem2Button` → Image component
   - **Source Image:** (để trống - script sẽ gán)
   - **Color:** White (bạn có thể dim nó nếu không dùng)
   - **Preserve Aspect:** ✓ (recommended)

4. **Xóa Text cũ:**
   - Delete "Text (Legacy)" child object
   - Chúng ta sẽ dùng TextMeshProUGUI thay vào

5. **Thêm TextMeshProUGUI để hiển thị số lượng:**
   - Select `CharmItem2Button`
   - Right-click → Create Empty
   - Đặt tên: `QuantityText`
   - Add Component → TextMeshProUGUI
   - Cài đặt:
	 - **Text:** "5" (ví dụ)
	 - **Font Size:** 20-30
	 - **Alignment:** Bottom Right (hoặc Bottom Center)
	 - **Color:** White
   - Tạo RectTransform cho nó:
	 - **Pos X:** 20, **Pos Y:** -20
	 - **Width:** 40, **Height:** 20
	 - **Anchors:** Bottom Right

6. **Thêm ItemBase Script:**
   - Select `CharmItem2Button`
   - Add Component → **CharmItem2Radius** (hoặc item type của bạn)

7. **Assign Fields trong Inspector:**

```
CharmItem2Radius (Script)
├── Item Data: (drag ItemData asset - charm_spell_2)
├── Icon Image: (drag Image component từ CharmItem2Button)
├── Quantity Text: (drag QuantityText - TextMeshProUGUI)
└── Canvas Group: (drag CanvasGroup component)
```

---

### Step 2: Tạo ItemData Asset

1. Right-click trong Assets → Create → FeedTheCat → Item → ItemData
2. Đặt tên: `CharmSpell2Radius`
3. Cài đặt Inspector:
   - **Item ID:** `charm_spell_2`
   - **Item Name:** `Charm Spell`
   - **Description:** `Charm a nearby NPC to move toward target. 2-turn duration.`
   - **Icon:** (drag một sprite vào)
   - **Item Type:** Active
   - **Effect Radius:** Radius2
   - **Target Type:** NPC
   - **Status Effect:** Charm
   - **Effect Duration:** 2
   - **Rarity:** Rare
   - **Cooldown:** 0

---

### Step 3: Tạo ItemInventory Singleton

1. Tạo empty GameObject trong Gameplay scene
2. Đặt tên: `ItemInventory`
3. Add Component → ItemInventory
4. Cài đặt:
   - **Persistence Key Prefix:** `inventory_`
   - **Max Quantity Per Item:** 999

---

### Step 4: Tạo StatusEffectSystem Singleton

1. Tạo empty GameObject
2. Đặt tên: `StatusEffectSystem`
3. Add Component → StatusEffectSystem

---

### Step 5: Setup NPCs

Mỗi NPC prefab cần có hai child objects:

```
NPC_Enemy (NPCMover)
├── stun (GameObject - empty hoặc có visual)
├── charm (GameObject - empty hoặc có visual)
└── [other children]
```

---

## 🎮 Cách Hoạt Động

### Initialization (Khởi Động)

```csharp
// Start() được gọi
1. InitializeReferences()
   - Cache RectTransform, CanvasGroup
   - Cache GraphicRaycaster
   - Get ItemInventory.Instance

2. SetupUI()
   - Auto-assign Image từ GetComponent
   - Assign sprite từ ItemData.Icon

3. SubscribeToInventory()
   - Lắng nghe OnQuantityChanged event

4. UpdateUI()
   - Cập nhật interactable based on CanUse()
   - Cập nhật icon color (white hoặc dim)
   - Cập nhật quantity text
```

### Quantity Change (Thay Đổi Số Lượng)

```
ItemInventory.IncreaseItem("charm_spell_2", 5)
	↓
	SetQuantity() được gọi
	↓
	OnQuantityChanged?.Invoke()
	↓
	ItemBase.HandleQuantityChanged()
	↓
	UpdateUI() được gọi
	↓
	- quantityText.text = "5"
	- canvasGroup.interactable = true
	- iconImage.color = white
```

### Drag & Drop (Kéo & Thả)

```
1. OnBeginDrag()
   - Check CanUse() → có qty > 0?
   - canvasGroup.blocksRaycasts = false (cho raycast qua)
   - canvasGroup.alpha = 0.7f (fade out)
   - Bắt đầu kéo

2. OnDrag()
   - Cập nhật position theo mouse delta

3. OnEndDrag()
   - canvasGroup.blocksRaycasts = true (restore)
   - canvasGroup.alpha = 1f (restore)
   - Raycast tìm GridCell dưới
   - Validate target
   - ExecuteEffect()
   - DecreaseQuantity()
   - (OnQuantityChanged trigger → UpdateUI tự động)
   - Save to PlayerPrefs
```

---

## 🔄 Auto Update Flow

**Khi bất kỳ item nào thay đổi quantity:**

```
ItemInventory thay đổi
	↓
OnQuantityChanged?.Invoke(itemID, newQty, oldQty)
	↓
Tất cả ItemBase đang lắng nghe
	↓
HandleQuantityChanged(changedItemID, ...)
	↓
if (changedItemID == this.ItemID)
	↓
	UpdateUI()
	↓
	- quantityText.text cập nhật
	- canvasGroup.interactable cập nhật
	- iconImage.color cập nhật
```

---

## 📊 CanvasGroup Quản Lý

### CanvasGroup Properties:

| Property | Khi Bắt Đầu Drag | Bình Thường | Khi Không Dùng |
|----------|-----------------|-----------|----------------|
| **Interactable** | false | true | false |
| **Blocks Raycasts** | false | true | true |
| **Alpha** | 0.7 | 1.0 | 1.0 |
| **Icon Color** | white | white | gray (0.5, 0.5, 0.5) |

---

## 🎯 Example Setup Complete

### CharmItem2Button (đã setup):
- **Sprite:** Charm icon từ ItemData
- **Quantity:** "5" (từ ItemInventory)
- **State:** 
  - Nếu qty > 0: interactable=true, color=white
  - Nếu qty == 0: interactable=false, color=gray

### Khi Click & Drag:
1. CanvasGroup.blocksRaycasts = false
2. Alpha = 0.7 (mờ)
3. Có thể kéo qua GridCell
4. Raycast tìm GridCell

### Khi Drop:
1. CanvasGroup.blocksRaycasts = true
2. Alpha = 1.0
3. Effect execute
4. Quantity decrease → OnQuantityChanged trigger
5. UpdateUI tự động (không cần gọi thủ công)

---

## 🚀 Testing

### Console Commands:

```csharp
// Tăng item
ItemInventory.Instance.IncreaseItem("charm_spell_2", 5);

// Xem số lượng
Debug.Log(ItemInventory.Instance.GetQuantity("charm_spell_2"));

// Giảm item
ItemInventory.Instance.DecreaseItem("charm_spell_2", 1);

// Save
ItemInventory.Instance.Save();

// Log tất cả items
ItemInventory.Instance.LogInventory();
```

### Kiểm Tra UI:
1. Play game
2. Xem button có icon không? ✓
3. Xem quantity text hiển thị? ✓
4. Thay đổi qty → UI cập nhật? ✓
5. qty = 0 → button dim? ✓
6. Kéo item → raycast qua? ✓

---

## ⚙️ Tối Ưu Hóa

### Caching (Tránh GetComponent lặp):

```csharp
// Start():
rectTransform = GetComponent<RectTransform>();         // Cache
canvasGroup = GetComponent<CanvasGroup>();             // Cache
iconImage = GetComponent<Image>();                     // Cache nếu null
quantityText = GetComponentInChildren<TextMeshProUGUI>(); // Cache nếu null
graphicRaycaster = canvas.GetComponent<GraphicRaycaster>(); // Cache

// OnBeginDrag(), OnDrag(), OnEndDrag():
// Dùng cached references, không GetComponent lại
```

### Event Subscribe:

```csharp
// OnEnable/OnDisable hay Start/OnDestroy:
itemInventory.OnQuantityChanged += HandleQuantityChanged;

// Không cần gọi UpdateUI() thủ công
// Event tự động trigger khi qty thay đổi
```

---

## 📝 Checklist

- [ ] CharmItem2Button UI được tạo
- [ ] CanvasGroup component được thêm
- [ ] Image component được setup
- [ ] QuantityText (TextMeshProUGUI) child được tạo
- [ ] CharmItem2Radius script được thêm
- [ ] ItemData asset được tạo với đúng ID
- [ ] Inspector fields được assign đúng
- [ ] ItemInventory GameObject được tạo
- [ ] StatusEffectSystem GameObject được tạo
- [ ] NPC có "stun" và "charm" child objects
- [ ] Play → Test drag & drop
- [ ] Play → Test quantity update
- [ ] Play → Test persistence (save/load)

---

## 🐛 Troubleshooting

| Vấn đề | Nguyên Nhân | Giải Pháp |
|--------|-----------|----------|
| Icon không hiển thị | ItemData.Icon empty | Gán sprite cho ItemData.Icon |
| Quantity text không cập nhật | quantityText not assigned | Drag QuantityText vào field |
| Không thể kéo item | CanvasGroup không có | Add Component CanvasGroup |
| Raycast bị block | blocksRaycasts=true khi drag | Script tự tắt - check OnBeginDrag |
| Item không dim khi qty=0 | iconImage not assigned | Auto-assign hoặc drag Image |
| Inventory không lưu | Save() not called | ExecuteEffect() gọi Save() |

---

## 📚 Reference

Xem thêm:
- `CODE_EXAMPLES_AND_API.md` - API usage
- `CONSUMABLE_ITEM_SYSTEM_GUIDE.md` - Complete guide
- Script comments trong `ItemBase.cs`
