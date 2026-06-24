# NPC Idle Multi-Cell Placement Debug Guide

## Vấn Đề
NPC Idle với multi-cell (`length > 1`) cần spawn đúng tọa độ theo direction.
Ví dụ: `startRow=4, startColumn=3, initialDirection=Right, length=2` → chiếm cells `(4,3)` và `(4,4)`

## Giải Pháp

### 1. Verify Direction Vector Mapping
```
Up    = (x=0,  y=-1)  → row giảm
Down  = (x=0,  y=1)   → row tăng
Left  = (x=-1, y=0)   → column giảm
Right = (x=1,  y=0)   → column tăng
```

### 2. Cell Calculation Formula
```
Cho baseRow, baseCol, direction vector (dx, dy), length:
Cho i từ 0 đến length-1:
  r = baseRow + dy * i
  c = baseCol + dx * i
```

Ví dụ với `(4,3) Right length=2`:
- i=0: r=4+0*0=4, c=3+1*0=3 → **(4,3)**
- i=1: r=4+0*1=4, c=3+1*1=4 → **(4,4)**

### 3. Debugging dalam Editor

#### Method 1: DebugLogExpectedCells (ContextMenu)
1. Select NPC GameObject trong Hierarchy
2. Inspector → Component NPCMover
3. Click "Debug: Log Expected Occupied Cells" (ContextMenu)
4. Xem Console để verify expected cells

**Console Output Example:**
```
NPCMover.DebugLogExpectedCells: NPC 'NPC_Idle'
  Start Position: (4, 3)
  Type: Idle, Direction: Right, Length: 2
  Direction Vector: (1, 0)
  Expected Occupied Cells:
	Cell #0: (4, 3)
	Cell #1: (4, 4)
```

#### Method 2: Runtime SetOccupiedCellsForPosition Logs
Play game, NPC spawn → Console show chi tiết:

```
NPCMover.SetOccupiedCellsForPosition: base=(4,3) direction=(1,0) length=2
  Cell #0: target=(4,3)
  Cell #0 SET occupiedByNPC=true
  Cell #1: target=(4,4)
  Cell #1 SET occupiedByNPC=true
NPCMover.SetOccupiedCellsForPosition: SUCCESS - occupied 2 cells
NPCMover.InitializeAndPlace (Idle): IDLE NPC spawned and occupies 2 cell(s)
```

#### Method 3: Nếu Fail
Nếu SetOccupiedCellsForPosition FAIL, logs sẽ show:

```
NPCMover.SetOccupiedCellsForPosition: base=(4,3) direction=(1,0) length=2
  Cell #0: target=(4,3)
  Cell #0 SET occupiedByNPC=true
  Cell #1: target=(4,4)
  Cell #1 is NULL (out of bounds or not found). Rollback!
NPCMover.SetOccupiedCellsForPosition: SUCCESS - occupied 0 cells
NPCMover.InitializeAndPlace (Idle): FAILED to set occupancy for IDLE NPC
```

Có thể do:
- Cells (4,4) out of bounds (board chỉ có 4 cột: 0-3)
- Cells (4,4) đã bị NPC khác chiếm
- Cells (4,4) là destination (NPC không thể vào)

## Sửa Test Case: (4,3) Right length=2

### Setup trong Editor
1. Tạo NPC Idle GameObject
2. Assign NPCMover component
   - `type = Idle`
   - `startRow = 4`
   - `startColumn = 3`
   - `initialDirection = Right`
   - `length = 2`
3. Right-click NPCMover → "Debug: Log Expected Occupied Cells"

### Expected Console Output
```
NPCMover.DebugLogExpectedCells: NPC 'NPC_Idle'
  Start Position: (4, 3)
  Type: Idle, Direction: Right, Length: 2
  Direction Vector: (1, 0)
  Expected Occupied Cells:
	Cell #0: (4, 3)
	Cell #1: (4, 4)
```

### Play Mode Verification
1. Press Play
2. Check Console for SetOccupiedCellsForPosition logs
3. Verify NPC occupies exactly cells (4,3) and (4,4)
4. Inspect scene: 
   - NPC visual should span across both cells
   - Both cells should show `occupiedByNPC=true`

## Troubleshooting Checklist

- [ ] Board dimensions: verify có ít nhất 5 rows (0-4) và 5 columns (0-4)
- [ ] DirectionToVec() mapping: Up/Down/Left/Right directions map đúng
- [ ] initialDirection: được set chính xác trước Start()
- [ ] length: được set chính xác (≥ 1)
- [ ] Target cells: không out of bounds, walkable, không occupied
- [ ] Console logs: appear đầy đủ và chi tiết
- [ ] NPC Idle: không move (TryStepWhenPlayerMoves không gọi di chuyển)

## Code Changes Summary

### NPCMover.cs

**1. DirectionToVec() - VERIFIED**
```csharp
private Vector2Int DirectionToVec(Direction d)
{
	switch (d)
	{
		case Direction.Up: return new Vector2Int(0, -1);
		case Direction.Down: return new Vector2Int(0, 1);
		case Direction.Left: return new Vector2Int(-1, 0);
		case Direction.Right: return new Vector2Int(1, 0);
	}
	return Vector2Int.right;
}
```

**2. SetOccupiedCellsForPosition - WITH LOGGING**
- Logs base position, direction, length
- Logs mỗi cell check
- Logs success/rollback reason

**3. DebugLogExpectedCells() - NEW**
- ContextMenu helper
- Show expected cells trước khi play

**4. InitializeAndPlace() - WITH IDLE LOGGING**
- Log confirm occupancy success/failure cho Idle NPCs
