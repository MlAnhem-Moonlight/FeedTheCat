# Fix Player Placement Multiple Times & Cell Occupancy Self-Check

## Vấn Đề Được Sửa

### 1. Player Được Set Vị Trí 3 Lần
**Trước:**
- `InitializeAndPlace()` → `InitializeGrid()` (line 78)
- `InitializeAndPlace()` → `InitializeGrid()` (line 191) - **DUPLICATE**
- `InitializeAndPlace()` → `PlaceAt(r, c)` (line 192)
- `LevelManager` → `PlacePlayerAt()` (line 425) - **THÊM PLACEMENT LẦN NỮA**

**Sau:**
- `InitializeAndPlace()` → `InitializeGrid()` (line 78) - ONCE ONLY
- `InitializeAndPlace()` → `isInitialPlacement = true; PlaceAt(r, c); isInitialPlacement = false;` (line 192)
- `LevelManager` chỉ set `startRow/startColumn`, không gọi `PlacePlayerAt()` → Tránh duplicate

### 2. Cell Cần Tự Kiểm Tra Occupancy
**Thêm:**
- `GridCell.RefreshOccupancy()` - tự tính toán `isEmpty` từ flags và update visual
- `BoardGenerator.RefreshAllCells()` - call `RefreshOccupancy()` trên all cells
- Gọi ở cuối mỗi turn để cells sync visual với occupancy

## Các Sửa Lỗi Chi Tiết

### PlayerController.cs

#### 1. Loại bỏ InitializeGrid() thứ 2
```csharp
// Trước:
InitializeAndPlace() {
	InitializeGrid();  // lần 1
	...
	InitializeGrid();  // lần 2 - DUPLICATE!
	PlaceAt(r, c);
}

// Sau:
InitializeAndPlace() {
	InitializeGrid();  // lần duy nhất
	...
	isInitialPlacement = true;
	PlaceAt(r, c);
	isInitialPlacement = false;
}
```

#### 2. Thêm Flag isInitialPlacement
```csharp
private bool isInitialPlacement = false;  // Track initial placement
```

#### 3. PlaceAt() chỉ clear occupancy khi cần
```csharp
private void PlaceAt(int r, int c)
{
	InitializeGrid();

	// Chỉ clear occupancy nếu KHÔNG phải initial placement
	if (!isInitialPlacement && cells != null)
	{
		// clear other cells' player occupancy
	}

	// ... rest of placement logic
}
```

### GridCell.cs

#### Thêm RefreshOccupancy()
```csharp
public void RefreshOccupancy()
{
	// Recalculate isEmpty from current occupancy flags
	isEmpty = !(occupiedByPlayer || occupiedByNPC);

	// Refresh visual to match state
	ApplyStateVisual();

	Debug.Log($"GridCell.RefreshOccupancy: cell({row},{column}) isEmpty={isEmpty} occupiedByPlayer={occupiedByPlayer} occupiedByNPC={occupiedByNPC}");
}
```

### BoardGenerator.cs

#### Thêm RefreshAllCells()
```csharp
[ContextMenu("Refresh All Cells Occupancy")]
public void RefreshAllCells()
{
	if (grid == null) return;

	int childCount = grid.transform.childCount;
	Debug.Log($"BoardGenerator.RefreshAllCells: refreshing {childCount} cells");

	for (int i = 0; i < childCount; i++)
	{
		var child = grid.transform.GetChild(i);
		var cell = child.GetComponent<GridCell>();
		if (cell != null)
			cell.RefreshOccupancy();
	}

	Debug.Log("BoardGenerator.RefreshAllCells: complete");
}
```

### LevelManager.cs

#### Không gọi PlacePlayerAt(), chỉ set startRow/startColumn
```csharp
// Trước:
playerController.startRow = desiredRow;
playerController.startColumn = desiredCol;
playerController.PlacePlayerAt(desiredRow, desiredCol);  // DUPLICATE!

// Sau:
playerController.startRow = desiredRow;
playerController.startColumn = desiredCol;
Debug.Log($"LevelManager: Set player start to ({desiredRow},{desiredCol}). PlayerController.InitializeAndPlace() will place it.");
// PlayerController.InitializeAndPlace() sẽ đọc startRow/startColumn và place
```

## Cách Sử Dụng RefreshAllCells()

### Manual Test Trong Editor
1. Right-click BoardGenerator component → "Refresh All Cells Occupancy"
2. Kiểm tra Console logs để verify refresh

### Automatic ở Cuối Turn
```csharp
// Ở GameManager hoặc game flow logic:
private void EndTurn()
{
	// Player và NPC đã di chuyển xong
	boardGenerator.RefreshAllCells();  // Tất cả cells update visual
}
```

## Verification Checklist

- [ ] Player chỉ được place 1 lần ở startup (InitializeAndPlace)
- [ ] Player không được place lại khi LevelManager apply level
- [ ] Cell occupancy visual sync với flags (isEmpty = !(occupiedByPlayer || occupiedByNPC))
- [ ] Khi gọi RefreshAllCells(), console logs show tất cả cells refreshed
- [ ] Player move: highlight cells update đúng
- [ ] NPC move: cell occupancy update đúng
- [ ] Không có multiple occupancy trên 1 cell (sanity check)

## Console Logs Expected

```
LevelManager: Set player start to (0,0). PlayerController.InitializeAndPlace() will place it.
LevelManager: Expected player placement at (0,0). Cell isEmpty=true

MovePlayerObjectToTarget: cell=(0,0) world=(...) localInContainer=(...) playerRt.localPos=(...)

BoardGenerator.RefreshAllCells: refreshing 64 cells
GridCell.RefreshOccupancy: cell(0,0) isEmpty=false occupiedByPlayer=true occupiedByNPC=false
GridCell.RefreshOccupancy: cell(0,1) isEmpty=true occupiedByPlayer=false occupiedByNPC=false
...
BoardGenerator.RefreshAllCells: complete
```
