# Fix Lỗi Player Spawn Sai Tọa Độ

## Nguyên Nhân Root Cause

Khi player được đặt vào cell (0,0) nhưng hiển thị ở cell (0,1), nguyên nhân là:

1. **PlayerContainer không căn chỉnh chính xác với BoardRect**
   - `playerContainer` phải được:
	 - Đặt cùng parent với `boardRect` (cùng Canvas hoặc cùng parent)
	 - Có cùng anchors, pivot, anchoredPosition, sizeDelta, localRotation
   - Nếu không căn chỉnh, `InverseTransformPoint` sẽ tính toán vị trí sai

2. **MovePlayerObjectToTarget sử dụng InverseTransformPoint**
   - Lấy vị trí thế giới (world) của cell
   - Chuyển về local space của playerContainer
   - Nếu playerContainer không aligned với boardRect, tính toán sẽ bị lệch

## Các Sửa Lỗi

### 1. PlayerController.cs - InitializeAndPlace
**Trước:** Chỉ set `anchoredPosition`, `sizeDelta`, `localScale`
```csharp
playerContainer.anchoredPosition = boardGenerator.boardRect.anchoredPosition;
playerContainer.sizeDelta = boardGenerator.boardRect.sizeDelta;
playerContainer.localScale = Vector3.one;
```

**Sau:** Set đầy đủ tất cả transform properties
```csharp
var br = boardGenerator.boardRect;
playerContainer.SetParent(br.parent, false);  // Phải là sibling
playerContainer.anchorMin = br.anchorMin;
playerContainer.anchorMax = br.anchorMax;
playerContainer.pivot = br.pivot;
playerContainer.anchoredPosition = br.anchoredPosition;
playerContainer.sizeDelta = br.sizeDelta;
playerContainer.localRotation = br.localRotation;
playerContainer.localScale = Vector3.one;
```

### 2. PlayerController.cs - MovePlayerObjectToTarget
**Thêm:**
- Tính toán `boardRt` reference
- Log chi tiết vị trí cell, world position, local position trong container
- Log boardRect và playerContainer alignment để debug

```csharp
Debug.Log($"MovePlayerObjectToTarget: cell=({target.row},{target.column}) world={worldCenter:F2} localInContainer={localInContainer:F2} playerRt.localPos={playerRt.localPosition:F2}");
if (boardRt != null)
{
	Debug.Log($"  boardRect: pos={boardRt.anchoredPosition:F2} size={boardRt.sizeDelta:F2}");
	Debug.Log($"  playerContainer: pos={playerContainer.anchoredPosition:F2} size={playerContainer.sizeDelta:F2}");
}
```

### 3. LevelManager.cs - 2 nơi tạo PlayerContainer
Cả hai nơi tạo PlayerContainer (có playerPrefab và không có) đều được sửa để:
- Set parent đúng (br.parent)
- Copy đầy đủ transform properties từ boardRect
- Gọi `SetAsLastSibling()` để playerContainer nằm trên boardRect trong hierarchy

## Cách Test

1. Mở Scene, Assign LevelData và Prefabs vào LevelManager
2. Chạy Play hoặc gọi ApplyLevel
3. Kiểm tra Console logs:
   ```
   MovePlayerObjectToTarget: cell=(0,0) world=(...) localInContainer=(...) playerRt.localPos=(...)
   boardRect: pos=(...) size=(...)
   playerContainer: pos=(...) size=(...)
   ```
4. Xác minh:
   - playerContainer pos/size giống boardRect
   - player visual hiển thị tại cell (0,0) đúng
   - Khi player move, cell occupancy logic vẫn đúng

## Nếu Vẫn Sai

Nếu player vẫn spawn sai tọa độ:
1. Kiểm tra Canvas render mode (ScreenSpace-Overlay vs ScreenSpace-Camera vs WorldSpace)
2. Kiểm tra có multiple PlayerController instances không (singleton fix sẽ xóa duplicates)
3. Kiểm tra LevelData.playerStart coordinates (có thể cần swap X/Y)
4. Gửi Console logs từ MovePlayerObjectToTarget để debug chi tiết
