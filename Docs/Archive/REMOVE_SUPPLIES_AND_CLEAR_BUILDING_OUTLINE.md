# HƯỚNG DẪN & BÁO CÁO: XÓA HOÀN TOÀN SUPPLIES CŨ VÀ XÓA VIỀN XANH CỦA CHUỒNG SAU KHI ĐẶT

**Dự án**: Unity Farm Simulation (`D:\GameNongTrai3D`)  
**Scene chính**: `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 15/10/2026  

---

## 1. Supplies Cũ Đã Được Thay Thế Bằng Storage Như Thế Nào

### 1.1. Hiện trạng trước khi sửa
- Trong Farm UI trước đây tồn tại hai hệ thống kho/vật phẩm song song gây xung đột nghiêm trọng:
  1. **Hệ thống mới (chuẩn)**: Nút `KHO ĐỒ / STORAGE` nằm trên thanh HUD bar phía trên màn hình (`ProgressionUI.cs`), mở trực tiếp `StorageUI.cs` và quản lý thống nhất toàn bộ nông sản, hạt giống (`wheat_seed`, `corn_seed`, `carrot_seed`), thức ăn gia súc (`grain`, `hay`, `pellet`), và sản phẩm chăn nuôi (`egg`, `milk`, `bacon`).
  2. **Giao diện Supplies cũ**: Một panel lớn cố định ở phía dưới màn hình chứa nhãn "Supplies" cùng các nút Grain, Hay, Pellets. Hệ thống này được sinh ra bởi file `SuppliesUI.cs` tự động tạo GameObject `SuppliesCanvas` (với sorting order 130+) che khuất joystick ảo điều khiển di chuyển, các ô đất trồng trọt (CropPlots) và ngăn cản tương tác gameplay. Đồng thời, `FarmUI.cs` còn vẽ thêm IMGUI button ở cạnh dưới màn hình (`Screen.height - 150f`).

### 1.2. Giải pháp khắc phục triệt để
1. **Thay thế `SuppliesUI.cs` thành cơ chế dọn dẹp & chuyển hướng an toàn**:
   - Loại bỏ hoàn toàn 741 dòng code sinh UI tự động cũ.
   - `SuppliesUI.EnsureSuppliesUIInScene()` không bao giờ khởi tạo `SuppliesCanvas`. Thay vào đó, hàm sẽ chủ động quét và tiêu hủy (`DestroyImmediate` hoặc `Destroy`) bất kỳ GameObject nào mang tên `SuppliesCanvas` hoặc component `SuppliesUI` runtime.
   - Các phương thức `.Open()`, `.Close()`, `.Toggle()` nếu có script nào khác vô tình gọi tới sẽ được chuyển tiếp (redirect) trực tiếp sang `StorageUI.EnsureUI().Open()`.
2. **Dọn dẹp `FarmSceneInitializer.cs`**:
   - Bước khởi tạo số 6 trước đây gọi `SuppliesUI.EnsureSuppliesUIInScene()` để sinh canvas, nay được cập nhật để dọn sạch tàn dư legacy supplies nếu còn sót lại.
3. **Cập nhật `FarmUI.cs`**:
   - Đặt `isHUDBarVisible = false` mặc định để ẩn hoàn toàn thanh điều khiển GUI đáy màn hình cũ.
   - Buộc `GameController.Instance().globalSuppliesUI = false` và `globalPlayerUI = false` ngay khi `Start()`.
   - Loại bỏ nút gọi `globalSuppliesUI = true` trong hàm vẽ IMGUI.
4. **Hệ thống quản lý vật phẩm duy nhất**:
   - Toàn bộ hạt giống, lúa mì, ngô, cỏ khô (Hay), thức ăn viên (Pellets) và sản phẩm thu hoạch từ vật nuôi (sữa, trứng...) được quản lý tập trung và duy nhất qua `StorageUI` và `InventorySystem`.
   - Joystick ảo (`VCAnalogJoystick`), `CropPlot` và camera không còn bị bất kỳ canvas nào che chắn hay chặn raycast.

---

## 2. Danh Sách Các File Đã Chỉnh Sửa

| STT | Tệp tin (File Path) | Tóm tắt thay đổi |
|---|---|---|
| 1 | `Assets/Scripts/Scenes/Farm/SuppliesUI.cs` | Thay thế hoàn toàn mã sinh UI cũ bằng adapter dọn dẹp `SuppliesCanvas`, chuyển tiếp lời gọi sang `StorageUI`. |
| 2 | `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs` | Bước 6 chuyển từ tạo mới SuppliesCanvas sang dọn dẹp sạch `SuppliesCanvas`. |
| 3 | `Assets/Scripts/Scenes/Farm/FarmUI.cs` | Tắt HUD bar đáy màn hình cũ, tắt cờ `globalSuppliesUI`, bỏ nút gọi supplies cũ. |
| 4 | `Assets/Scripts/Animals/AnimalPenBoundary.cs` | Gỡ bỏ `OnDrawGizmos()` vẽ khung dây xanh (`Gizmos.DrawWireCube`) liên tục trên chuồng ở Game view / Scene view. |
| 5 | `Assets/Scripts/Buildings/FarmBuilding.cs` | - Thêm phương thức an toàn `SafeDestroy()` (chạy `Destroy` trong Play Mode, `DestroyImmediate` trong Edit Mode).<br>- Thêm `ClearHighlight()` xóa triệt để `LineRenderer`, Outline, Selection components.<br>- Thêm `RestoreOriginalMaterials()` khôi phục màu gỗ/rơm/mái che nguyên bản.<br>- Thêm `EnforceTypeAndCapacity()` cố định sức chứa (Cow Barn = 3 bò, Chicken Coop = 5 gà). |
| 6 | `Assets/Scripts/Buildings/BuildingPlacementController.cs` | - Thêm `ClearPlacementPreview()` và `ClearBuildingHighlight()`.<br>- Thuộc tính `currentPreview` và `selectedBuilding` công khai.<br>- Cập nhật `ConfirmPlacement()` và `CancelPlacement()` dọn sạch ghost, highlight và khôi phục material gốc ngay lập tức.<br>- Sử dụng `SafeDestroy()` thay cho `DestroyImmediate` gây lỗi runtime trong Play Mode. |
| 7 | `Assets/Scripts/Buildings/BuildingUI.cs` | - Gọi `BuildingPlacementController.Instance.ClearBuildingHighlight()` khi `Close()`.<br>- Tự động đóng UI và xóa highlight khi người chơi click ra ngoài không gian thế giới. |
| 8 | `Assets/Scripts/Buildings/FarmBuildingManager.cs` | - Cập nhật liên kết vật nuôi đúng chuồng (ngăn bò vào chuồng gà, gà vào chuồng bò).<br>- Đồng bộ chuồng gà trong `PruneAndScanSceneBuildings()`. |
| 9 | `Assets/Editor/AnimalPenPrefabGenerator.cs` | - Cập nhật sức chứa chuồng bò = 3, chuồng gà = 5.<br>- Thay đổi `AutoGeneratePens()` không ghi đè prefab liên tục mỗi lần biên dịch mã. |

---

## 3. Nguyên Nhân Viền Xanh Còn Tồn Tại Sau Khi Đặt Chuồng

Viền xanh bám dai dẳng quanh chuồng sau khi bấm đặt bắt nguồn từ **3 nguyên nhân kỹ thuật phối hợp**:

1. **Lỗi gọi `DestroyImmediate` trong Play Mode**:
   - Trước đây trong `BuildingPlacementController.cs`, khi hoàn tất đặt (`ConfirmPlacement`) hoặc hủy (`CancelPlacement`), mã nguồn gọi `DestroyImmediate(activePreview.gameObject)`.
   - Trong Unity Play Mode, `DestroyImmediate` bị cấm hoặc ném ngoại lệ: *"DestroyImmediate may not be called from a GUI callback or runtime without special flag"*. Hậu quả là đối tượng `PlacementGhost` (với toàn bộ material màu xanh lá mờ và viền highlight) **không hề bị xóa**, mà nằm đè chồng khít lên chuồng thật mới sinh.
2. **`AnimalPenBoundary.OnDrawGizmos()` vẽ Wireframe xanh vô điều kiện**:
   - `AnimalPenBoundary.cs` có đoạn mã:
     ```csharp
     void OnDrawGizmos()
     {
         Gizmos.color = new Color(0.2f, 0.95f, 0.25f, 0.65f);
         Gizmos.DrawWireCube(center, size);
     }
     ```
   - Trong Unity Editor khi bật Gizmos (hoặc Gizmos Game View), một khung hộp màu xanh lá cây rực rỡ luôn bao bọc quanh hàng rào của chuồng vĩnh viễn dù chuồng đã đặt xong.
3. **Chưa khôi phục material gốc sau preview**:
   - Khi tạo đối tượng preview, các renderer được gán `previewMat` màu xanh (hoặc shader viền xanh). Nếu entity không được phân tách layer hoặc renderer bị kế thừa component `LineRenderer` / `Outline` từ prefab mẫu, chuồng thật sau khi instantiate vẫn giữ lại material bán trong suốt màu xanh.

---

## 4. Cách Xóa Preview Và Highlight Sau Placement

### 4.1. Khi đang đặt chuồng (Preview Mode)
- Chỉ đối tượng ghost preview (`PlacementGhost`) được gán layer riêng `PlacementGhost` (Layer 2) và gán vật liệu hiển thị trạng thái hợp lệ/không hợp lệ.
- Raycast từ chuột chiếu xuống địa hình sử dụng `layerMask` loại trừ hoàn toàn `PlacementGhost`, tránh hiện tượng nhấp nháy hoặc tự va chạm với chính nó.
- Tuyệt đối không can thiệp hay gán outline lên các chuồng thật đã tồn tại trên nông trại.

### 4.2. Khi bấm "ĐẶT NGAY" (`ConfirmPlacement`)
Ngay sau khi xác nhận vị trí hợp lệ và trừ Gold:
1. Gọi `ClearPlacementPreview()`:
   - Deactivate `activePreview.gameObject`.
   - Xóa `activePreview` bằng `SafeDestroy()`.
   - Tìm và dọn sạch mọi đối tượng thừa có tiền tố `PlacementGhost` trong scene.
   - Gán `activePreview = null`.
2. Gọi `ClearBuildingHighlight()`:
   - Tắt selection highlight, hủy mọi component `LineRenderer`, `Outline` trên chuồng được chọn và toàn bộ các chuồng trong scene.
   - Gán `selectedBuilding = null`.
3. Khôi phục visual chuồng thật (`CleanUpBuildingVisuals`):
   - Đặt layer của chuồng mới sinh về layer mặc định (`Default` / 0).
   - Gọi `FarmBuilding.RestoreOriginalMaterials()` để gán lại vật liệu chuẩn (gỗ sồi, rơm khô, ngói đỏ, ván sàn) và loại bỏ bất kỳ material preview nào còn sót lại.
   - Gọi `FarmBuilding.EnforceTypeAndCapacity()` và `RefreshVisuals()`.

### 4.3. Khi bấm "HỦY BỎ" (`CancelPlacement`)
- Gọi `ClearPlacementPreview()` hủy ngay lập tức ghost preview.
- Gọi `ClearBuildingHighlight()`.
- Tắt Canvas điều khiển đặt chuồng (`placementOverlayCanvas`), tắt nhận raycast.
- Đặt `isPlacing = false`, `selectedBuilding = null`.
- Toàn bộ thao tác gameplay bình thường được khôi phục ngay lập tức.

### 4.4. Khi click vào chuồng đã đặt
- Khi click chuột vào chuồng:
  - Mở `BuildingUI` hiển thị bảng quản lý: Tên chuồng, số lượng vật nuôi, danh sách trạng thái no/vui vẻ, nút "Cho ăn tất cả", nút "Thu hoạch sản phẩm" và nút "Mua vật nuôi".
  - Không bật viền outline xanh bám vĩnh viễn trên chuồng.
  - Khi người chơi click nút đóng "X" hoặc click chuột ra khoảng đất trống bên ngoài chuồng, `BuildingUI` tự động đóng lại và gọi `BuildingPlacementController.Instance.ClearBuildingHighlight()`.

---

## 5. Quy Chuẩn Loại Chuồng & Sức Chứa

Đã cố định bằng phương thức `EnforceTypeAndCapacity()` trên cả chuồng trong scene lẫn chuồng mới mua:

| Loại công trình | Mã định danh (`buildingId`) | Tên hiển thị chuẩn | Sức chứa tối đa | Loại vật nuôi hợp lệ | Sản phẩm thu hoạch |
|---|---|---|---|---|---|
| **Cow Barn** | `cow_barn` | `Cow Barn / Chuồng bò` | **3 con bò** | `AnimalType.Cow` | Sữa tươi (Milk) |
| **Chicken Coop** | `chicken_coop` | `Chicken Coop / Chuồng gà` | **5 con gà** | `AnimalType.Chicken` | Trứng gà (Egg) |

- **Không nhầm lẫn tên gọi**: Chuồng gà hiển thị chuẩn `Chicken Coop / Chuồng gà`, không bị gán nhầm sang Cow Barn.
- **Không spawn chéo**: 
  - Trong `FarmBuilding.AddAnimal()`, nếu vật nuôi khác loài được đưa vào chuồng sẽ bị từ chối kèm cảnh báo.
  - Nút "+ MUA VẬT NUÔI" trong `BuildingUI` tự động nhận diện: nếu đang mở Chuồng gà sẽ mua Gà (60 Gold); nếu đang mở Chuồng bò sẽ mua Bò (100 Gold).
  - Khi chuồng đạt tối đa (3 bò hoặc 5 gà), nút mua sẽ tự động khóa và hiển thị trạng thái `CHUỒNG ĐÃ ĐẦY (3/3)` hoặc `(5/5)`.

---

## 6. Sửa Lỗi Console Gốc (Root-Cause Fixes)

1. **Lỗi "Destroy may not be called from edit mode"**:
   - *Nguyên nhân*: Hàm `CreateEnclosedFencePen` trong `FarmBuilding.cs` được gọi từ Editor Generator có chứa các lệnh `Destroy(col)`. Trong Edit Mode, Unity yêu cầu phải gọi `DestroyImmediate`.
   - *Khắc phục*: Thay toàn bộ bằng phương thức tập trung `SafeDestroy(Object obj)`:
     ```csharp
     public static void SafeDestroy(UnityEngine.Object obj)
     {
         if (obj == null) return;
         if (Application.isPlaying) Destroy(obj);
         else DestroyImmediate(obj);
     }
     ```
2. **Lỗi "Destroying an object in edit mode permanently" & "DestroyImmediate in Play Mode"**:
   - *Nguyên nhân*: `BuildingPlacementController.cs` dùng `DestroyImmediate` trong lúc chơi runtime.
   - *Khắc phục*: Chuyển sang `SafeDestroy()`.
3. **Lỗi missing references & null safety**:
   - Thêm phương thức kiểm tra an toàn `FarmBuilding.IsValidBuilding(FarmBuilding b)` trước khi truy cập các thuộc tính của chuồng.

---

## 7. Các Bước Kiểm Tra Trong Play Mode

1. Mở cảnh: `Assets/Scenes/Farm.unity`.
2. Mở cửa sổ **Console** và chọn **Clear**.
3. Bấm **Play** để vào Play Mode:
   - Kiểm tra cạnh dưới màn hình: Không còn panel chữ "Supplies" lớn, không còn nút Supplies IMGUI cũ. Joystick ảo hiển thị trơn tru, tương tác tốt.
   - Kiểm tra thanh HUD trên cùng: Bấm nút `KHO ĐỒ / STORAGE`, bảng Storage mở lên mượt mà hiển thị đầy đủ hạt giống, cỏ khô, viên nén và sản phẩm.
4. Mở `SHOP CỬA HÀNG` -> chọn tab `CÔNG TRÌNH` -> chọn **Cow Barn**:
   - Preview ghost màu xanh xuất hiện dưới chân con trỏ chuột.
   - Di chuyển khắp nông trại: Preview bám theo địa hình terrain mượt mà.
   - Bấm **ĐẶT NGAY**: Cow Barn được xây dựng thật, preview và viền xanh biến mất 100% ngay lập tức.
5. Click vào Cow Barn vừa đặt:
   - Bảng quản lý chuồng mở lên hiển thị: `COW BARN / CHUỒNG BÒ`, `Số lượng: 0/3`, `Vật nuôi cho phép: Cow / Bò`.
   - Bấm `+ MUA BÒ (100 Gold)`: 1 chú bò xuất hiện trong chuồng. Mua tối đa 3 con, nút chuyển thành `CHUỒNG ĐÃ ĐẦY (3/3)` và không cho mua thêm.
   - Click chuột ra khoảng đất bên ngoài chuồng: Bảng quản lý đóng lại, không có viền xanh nào bám lại trên chuồng.
6. Mở `SHOP CỬA HÀNG` -> chọn **Chicken Coop**:
   - Thử bấm **HỦY BỎ**: Preview ghost biến mất hoàn toàn, trạng thái gameplay trở lại bình thường.
   - Thử đặt Chicken Coop tại vị trí khác: Chuồng gà hiển thị chuẩn `CHICKEN COOP / CHUỒNG GÀ`, sức chứa tối đa đúng `5 con gà`.
7. Kiểm tra Console: Không có lỗi đỏ (Error/Exception) phát sinh. Chuyển scene sang Mart rồi quay lại Farm để kiểm chứng không bị tái sinh `SuppliesCanvas`.
