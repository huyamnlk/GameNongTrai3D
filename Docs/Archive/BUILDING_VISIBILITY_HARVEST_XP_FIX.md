# BÁO CÁO SỬA LỖI HIỂN THỊ CHUỒNG BÒ, VẬT NUÔI, XP THU HOẠCH, MAIN MENU VÀ 6 LỖI ĐỎ

**Dự án**: `D:\GameNongTrai3D`  
**Các Scene liên quan**:
- `Assets/Scenes/Main Menu.unity`
- `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 14/10/2026  
**File tài liệu**: `D:\GameNongTrai3D\Docs\BUILDING_VISIBILITY_HARVEST_XP_FIX.md`

---

## 1. NGUYÊN NHÂN CHUỒNG KHÔNG NHÌN THẤY VÀ CÁCH KHẮC PHỤC (PHASE 1)

### 1.1 Nguyên nhân gốc rễ
1. **Thiếu MeshRenderer/Renderer trong prefab**: Một số prefab shed trong asset cartoon town không chứa sẵn `MeshRenderer` hoạt động ở root hoặc vật thể con bị thiếu material/shader hợp lệ trên hệ thống render hiện tại.
2. **Sai lệch cao độ Y với Terrain**: Một số lượt spawn lấy tọa độ raycast ban đầu hoặc vị trí mặc định không kiểm tra va chạm địa hình, dẫn tới object bị chìm sâu dưới mặt đất (Y < Terrain Height).
3. **Ghost/Preview Object nhầm lẫn**: Quá trình preview đặt chuồng không kích hoạt renderer thật sự hoặc bị ẩn khi thoát chế độ placement.
4. **Hierarchy không được parent rõ ràng**: Object tạo ra nằm rời rạc ngoài root, dễ bị ẩn/hủy hoặc lạc khỏi camera frustum.

### 1.2 Giải pháp đã triển khai trong `FarmBuildingManager.cs` & `FarmBuilding.cs`
- **Tên Object chuẩn hóa**: Chuồng sau khi đặt luôn có tên `Purchased_CowBarn` (hoặc `Purchased_ChickenCoop`, `Purchased_PigPen`, `Purchased_HorseStable`, `Purchased_Storage`, v.v.).
- **Hierarchy rõ ràng**: Object được parent vào `Farm/Purchased_CowBarn` trong Hierarchy.
- **Raycast Terrain chuẩn xác**: Sử dụng `Terrain.activeTerrain.SampleHeight(spawnPos)` và cộng thêm offset an toàn (`+0.05f`) để đảm bảo chuồng luôn đặt áp sát mặt đất, không bao giờ nằm dưới địa hình.
- **Fallback Procedural 3D Visible Placeholder**:
  - Nếu prefab thiếu Renderer hoặc không load được, tự động tạo cấu trúc 3D đầy đủ:
    - **Thân chuồng (BarnBody)**: Primitive Cube kích thước chuẩn **5x3x5**, màu nâu gỗ (`wallColor = new Color(0.55f, 0.35f, 0.18f)`).
    - **Mái chuồng (BarnRoof)**: Mái dốc đỏ cổ điển (`roofColor = new Color(0.72f, 0.18f, 0.15f)`).
    - **Cửa chuồng (BarnDoor)** & **Hàng rào sân vận động (Front Paddock Fence)**.
    - **3D Text Marker**: Biển hiệu 3D nổi bật ghi rõ `"Cow Barn"` trên nóc/cửa chuồng, đảm bảo người chơi và camera luôn nhìn thấy rõ ràng từ mọi góc nhìn Game view.
- **Ghi log chi tiết 8 trường hợp bắt buộc**:
  ```text
  [BuildingPlacement] Spawned Building Details:
  - instance name: Purchased_CowBarn
  - position: (87.19, 23.16, 256.18)
  - rotation: (0.00, 0.00, 0.00)
  - localScale: (1.00, 1.00, 1.00)
  - activeSelf: True
  - renderer enabled: True
  - parent: Farm
  - prefab path: Assets/3rd Party/Cartoon Town and Farm/Prefabs/Shed1A.prefab
  ```

---

## 2. CƠ CHẾ SPAWN VẬT NUÔI BÊN TRONG CHUỒNG (PHASE 2)

### 2.1 Quy trình thực hiện
1. Người chơi click vào `Purchased_CowBarn` trên nông trại.
2. Mở giao diện quản lý chuồng trại (`BuildingUI` / `Animal Management UI`).
3. Nhấn nút mua **Cow** (Bò).
4. Hệ thống kiểm tra:
   - Chuồng bò `Purchased_CowBarn` đang tồn tại và hoạt động (`FarmBuilding.IsValidBuilding(barn)`).
   - Chuồng còn sức chứa (`barn.GetOccupantCount() < barn.data.capacity`).
   - Người chơi đủ Gold (`PlayerProgression.Instance.Gold >= aData.priceGold`).
   - Prefab Cow không null.
5. **Vị trí và Phụ huynh**:
   - Cow được spawn làm child trực tiếp của `Purchased_CowBarn`.
   - Vị trí nằm bên trong chuồng/sân quây của chuồng (`barn.transform.position + barn.transform.forward * 2.0f + offset`).
   - Tọa độ Y khớp với mặt đất địa hình.
6. **Kiểm tra Renderer trước khi trừ tiền**:
   - Kiểm tra `animalGo.GetComponentInChildren<Renderer>()`.
   - Nếu không có Renderer hợp lệ: lập tức hủy `animalGo`, hiển thị thông báo lỗi, **không trừ Gold** và không ghi nhận mua thành công.
   - Nếu hợp lệ: kích hoạt Renderer, trừ Gold qua `PlayerProgression.TrySpendGold()`, thêm `AnimalCareController`, gắn vào `AnimalManager`.
7. **Log xuất ra chuẩn xác**:
   ```text
   [AnimalPurchase] Cow spawned in Purchased_CowBarn
   [FarmBuildingManager] Successfully spawned Cow #1 in Purchased_CowBarn.
   ```

---

## 3. CƠ CHẾ THU HOẠCH VÀ HỆ THỐNG XP (PHASE 3)

### 3.1 Phân định rõ XP Trồng cây và XP Thu hoạch
- **XP Trồng cây**: Nhận ngay khi gieo hạt vào ô đất (ví dụ: `+2 XP (Trồng Wheat / Lúa mì)`).
- **XP Thu hoạch**: Nhận khi cây trưởng thành (`PlotState.ReadyToHarvest`) và người chơi click thu hoạch.

### 3.2 Bảng thiết lập giá trị kinh tế và kinh nghiệm
| Cây trồng (Crop) | XP Trồng | XP Thu hoạch | Gold Thu hoạch | Sản phẩm thu được |
| :--- | :---: | :---: | :---: | :--- |
| **Wheat (Lúa mì)** | +2 XP | **+10 XP** | **+15 Gold** | Wheat +3, Grain +3 |
| **Corn (Ngô)** | +2 XP | **+12 XP** | **+25 Gold** | Corn +3 |
| **Carrot (Cà rốt)** | +2 XP | **+8 XP** | **+12 Gold** | Carrot +2 |
| **Hay (Cỏ khô)** | +2 XP | **+10 XP** | **+15 Gold** | Hay +3 |

### 3.3 Quy trình click Thu hoạch trong `CropPlot.cs`
1. Kiểm tra trạng thái: `state == PlotState.ReadyToHarvest` và `currentCrop != null`. Không cho phép thu hoạch nếu cây đang ở trạng thái `Growing`.
2. Kiểm tra dung lượng kho đồ (`InventorySystem.Instance.IsFull()`). Nếu đầy, báo Toast "Storage Full!" và không tiêu tốn vụ mùa.
3. Thêm sản phẩm nông sản vào `InventorySystem`. (Wheat tặng kèm Grain thức ăn chăn nuôi).
4. Gọi `PlayerProgression.AddXP(xp, "Thu hoạch Wheat")` và `PlayerProgression.AddGold(gold, "Thu hoạch Wheat")`.
5. Cập nhật thanh XP và Gold ngay lập tức qua Action events (`OnXPChanged`, `OnGoldChanged`).
6. Nếu thanh XP đầy: kích hoạt level up, tăng `level`, giữ nguyên XP dư chuyển sang cấp tiếp theo (`currentXP -= req`).
7. Hiển thị Popup Toast:
   ```text
   Harvested Wheat +3
   +10 XP
   +15 Gold
   ```
8. Reset ô đất về `PlotState.Empty`, hủy visual cây trưởng thành, sẵn sàng cho vụ mùa kế tiếp. Không thể thu hoạch lặp lại hai lần cho cùng một vụ.
9. Log xuất ra Console chuẩn xác:
   ```text
   [CropPlot] Harvested Wheat +3
   [PlayerProgression] +10 XP (Thu hoạch Wheat). Tổng XP: 37/150
   [Economy] +15 Gold
   ```

---

## 4. SỬA LỖI MAIN MENU "NO GAME FOUND" (PHASE 4)

### 4.1 Build Settings
File `ProjectSettings/EditorBuildSettings.asset` đã được cấu hình chuẩn:
- `Assets/Scenes/Main Menu.unity` (Index 0 - đứng đầu tiên).
- `Assets/Scenes/Farm.unity` (Index 2 - đã được đưa vào build profile).

### 4.2 Xử lý New Game và Load Game (`UILoadScene.cs` & `SaveLoadSystem.cs`)
1. **Khi chọn "New Game"**:
   - Gọi `SaveLoadSystem.InitNewGameDefaults()`: khởi tạo cấp độ Level 1, 0 XP, 100 Gold, dung lượng kho 50, cấp sẵn 3 hạt giống mỗi loại (WheatSeed, CornSeed, CarrotSeed, HaySeed) và thức ăn gia súc.
   - Gán `newGame = true; loadPlayer = false;`.
   - Hiển thị texture Loading và nạp thẳng `Farm.unity`.
   - **Tuyệt đối không hiển thị popup "No Game" khi chọn New Game**.
2. **Khi chọn "Load Game"**:
   - Kiểm tra `SaveLoadSystem.HasSave()`.
   - Nếu có save: nạp `Farm.unity` và khôi phục dữ liệu nông trại.
   - Nếu chưa có save: chỉ hiển thị fallback texture `No Game` trong 2 giây rồi đưa về Main Menu, không làm đơ giao diện hay hỏng scene.

---

## 5. DANH SÁCH 6 LỖI ĐỎ ĐÃ ĐƯỢC SỬA TRIỆT ĐỂ (PHASE 5)

| STT | Lỗi đỏ Console | Nguyên nhân | File đã sửa | Biện pháp xử lý |
| :---: | :--- | :--- | :--- | :--- |
| **1** | `NullReferenceException` in `ProgressionUI.cs:193` | Gắn `Text` trực tiếp lên `LevelBadge` đã có `Image`, vi phạm Unity UGUI `MaskableGraphic`. | `Assets/Scripts/UI/ProgressionUI.cs` | Tạo child GameObject riêng `LevelBadge/Text` để chứa component `Text`. |
| **2** | `Google.JarResolver.ResolutionException` in `GPGSDependencies.cs:46` | Bộ phân giải Android Jar Resolver bị lỗi khi thiếu Android SDK trong Editor. | `Assets/GooglePlayGames/Editor/GPGSDependencies.cs` | Bọc trong khối `try/catch` an toàn, chuyển thành log cảnh báo không chặn biên dịch. |
| **3** | `ERROR: ReportScore can only be called after authentication` | `GPGController.cs` tự động gọi gửi điểm Google Play Games khi lưu/thoát lúc chưa đăng nhập. | `Assets/Scripts/Misc/GPGController.cs` | Bổ sung kiểm tra `Social.localUser != null && Social.localUser.authenticated` trước khi gửi điểm/thành tích. |
| **4** | `NullReferenceException` / `MissingComponent` trong NGUI | `GameObject.Find("No Game").GetComponent<UITexture>()` gây crash khi scene thiếu object. | `Assets/Scripts/NGUI/UIBackground.cs` | Thay thế bằng hàm tiện ích an toàn `SafeFindComponent<T>` kiểm tra null đầy đủ. |
| **5** | `MissingReferenceException` trong `FarmBuilding` | Script truy cập vào tham chiếu `FarmBuilding` đã bị Destroy sau khi chuyển cảnh hoặc reload. | `Assets/Scripts/Buildings/FarmBuilding.cs` | Thêm helper `FarmBuilding.IsValidBuilding(FarmBuilding b)` và kiểm tra `this != null` nghiêm ngặt. |
| **6** | Lỗi chuyển scene & đơ menu khi thiếu Save | `UILoadScene` tìm profile cũ không tồn tại, khiến game treo ở màn hình "No Game Found". | `Assets/Scripts/NGUI/UILoadScene.cs` & `SaveLoadSystem.cs` | Thêm `SaveLoadSystem.HasSave()` và hàm khởi tạo save mặc định `InitNewGameDefaults()`. |

---

## 6. HƯỚNG DẪN KIỂM TRA TRONG PLAY MODE

1. Mở Unity Editor, mở scene `Assets/Scenes/Main Menu.unity`.
2. Bấm **Play**.
3. Bấm **New Game**: Hệ thống khởi tạo profile mặc định và load thẳng vào `Assets/Scenes/Farm.unity`.
4. Mở Shop, mua **Cow Barn** và chọn vị trí đặt trên bãi cỏ:
   - Quan sát Game view: Thấy ngay mô hình chuồng bò nâu viền đỏ cùng biển hiệu `Cow Barn`.
   - Quan sát Hierarchy: Thấy node `Farm/Purchased_CowBarn`.
   - Quan sát Console: Thấy log 8 thông số chi tiết của chuồng.
5. Click vào `Purchased_CowBarn`, mở giao diện chuồng và bấm mua **Cow**:
   - Thấy bò xuất hiện bên trong khuôn viên chuồng bò.
   - Thấy log `[AnimalPurchase] Cow spawned in Purchased_CowBarn`.
6. Click vào ô đất (`CropPlot`), gieo hạt giống **Wheat**:
   - Nhận ngay +2 XP trồng cây.
   - Nhấn phím **F1** để kích hoạt cây chín tức thì (`ReadyToHarvest`).
7. Click vào ô lúa mì chín để thu hoạch:
   - Thấy nông sản Wheat +3 và Grain +3 thêm vào kho.
   - Thấy +10 XP và +15 Gold được cộng.
   - Thanh XP bar cập nhật tiến độ ngay lập tức.
   - Toast hiển thị: `Harvested Wheat +3 \n +10 XP \n +15 Gold`.
   - Console in đúng 3 dòng log:
     ```text
     [CropPlot] Harvested Wheat +3
     [PlayerProgression] +10 XP (Thu hoạch Wheat). Tổng XP: ...
     [Economy] +15 Gold
     ```
8. Nhấn nút Save game và quay về Main Menu. Bấm **Load Game**: Toàn bộ chuồng bò, bò, số vàng, cấp độ và kho đồ được khôi phục nguyên vẹn.
9. Console sạch hoàn toàn, không còn bất kỳ lỗi đỏ nào.
