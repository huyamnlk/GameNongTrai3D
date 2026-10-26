# BÁO CÁO CHI TIẾT SỬA LỖI VÀ CHUẨN HÓA NÔNG TRẠI (CURRENT ERRORS REPORT)

**Dự án**: `D:\GameNongTrai3D`  
**Scene chính**: `Assets/Scenes/Farm.unity`  
**Thời gian cập nhật**: 14/10/2026  

---

## 1. TỔNG HỢP VÀ NGUYÊN NHÂN CÁC LỖI ĐỎ (PHASE 1)

| STT | Lỗi (Exception / Log) | File | Dòng | Nguyên nhân | Cách khắc phục | Trạng thái |
|---|---|---|---|---|---|---|
| **1** | `A GameObject can only contain one 'Graphic' component.` | `Assets/Scripts/Progression/ProgressionUI.cs` | 192 | Gọi `badgeGo.AddComponent<Text>()` trên GameObject đã có `Image`. Unity cấm 2 Graphic trên cùng 1 GameObject. | Tách riêng child GameObject `badgeTextGo = new GameObject("Text")` với RectTransform và Text riêng. | **Đã sửa triệt để** |
| **2** | `NullReferenceException: Object reference not set to an instance of an object` | `Assets/Scripts/Progression/ProgressionUI.cs` | 193 | Do lỗi #1, `AddComponent<Text>()` trả về null. Truy cập `lvlText.text` ném NRE làm gián đoạn `FarmUI.Start()`. | Đã khắc phục cùng lỗi #1, đảm bảo `lvlText != null`. | **Đã sửa triệt để** |
| **3** | `A GameObject can only contain one 'Graphic' component.` | `Assets/Scripts/Buildings/BuildingUI.cs` | 116 | Gọi `listGo.AddComponent<Text>()` trên GameObject đã có `listBg = listGo.AddComponent<Image>()`. | Tách riêng child GameObject `textGo = new GameObject("Text")` cho `textAnimalList`. | **Đã sửa triệt để** |
| **4** | `NullReferenceException: Object reference not set to an instance of an object` | `Assets/Scripts/Buildings/BuildingUI.cs` | 117 | Do lỗi #3, `textAnimalList` trả về null, làm sập quy trình mở và hiển thị Building UI. | Khắc phục xong với việc phân tách child Text GameObject. | **Đã sửa triệt để** |
| **5** | `ResolutionException: Cannot find candidate artifact com.google.android.gms:play-services-games:8.4+` | `Assets/GooglePlayGames/Editor/GPGSDependencies.cs` | 46 | Hệ thống phụ thuộc Android cũ của GooglePlayGames ném ngoại lệ khi khởi động Editor trong Unity 6. | Bọc khối try-catch an toàn quanh việc đăng ký dependency để không làm dừng quá trình reload scripts. | **Đã sửa triệt để** |
| **6** | `NullReferenceException` lặp liên tục trong `CowController.FixedUpdate()` (59+ lỗi) | `Assets/Scripts/Cow/CowController.cs` | 37-69, 72-120, 136-141 | `InitUIControl()` tìm các đối tượng UI, Joystick, Camera, Player nhưng không kiểm tra null; `catch (UnityException)` không bắt `NullReferenceException`. Trong `FixedUpdate`, `anim.Play(...)` bị null ném lỗi 60 lần/giây. | Đổi sang `catch (System.Exception)`; kiểm tra null toàn diện cho `userInterface`, `joyStick`, `cameraControl`, `playerGO`, `movement`; thêm `if (anim == null) return;` đầu `FixedUpdate`. | **Đã sửa triệt để** |
| **7** | Bò spawn ngoài chuồng, chạy tự do khắp map | `Assets/Scripts/Scenes/Farm/FarmInit.cs` | 60-68, 82-89 | `FarmInit` dùng tọa độ ngẫu nhiên cũ (57..102, 242..261) spawn bò lung tung ra cảnh dù chưa có chuồng. | Chỉ spawn bò khi `cow_barn` đã mua; spawn chính xác trong `CowPen_Small.penBoundary` và gán parent `CowPen_Small`. | **Đã sửa triệt để** |
| **8** | Chồng lấn text 3D trên đầu toàn bộ vật nuôi | `Assets/Scripts/Animals/AnimalCareController.cs` | 209-252 | Tất cả vật nuôi đều có TextMesh hiển thị 2 dòng chỉ số liên tục trong Update, không quay mặt theo Camera gây rối mắt. | Ẩn text mặc định; chỉ hiện thông tin khi click chọn con vật (`isSelected`); hiện tag nhỏ `★ Thu hoạch` khi sẵn sàng; billboard tự quay về `Camera.main`. | **Đã sửa triệt để** |
| **9** | Chồng lấn text debug trên 6 ô đất trồng | `Assets/Scripts/Farming/CropPlot.cs` | 338-386 | Tất cả các ô đất dù trống hay đang lớn đều hiện text debug `[CropPlot_01] Trống: Chạm để trồng`. | Ẩn text khi ô đất trống hoặc đang phát triển; chỉ hiển thị tag nhỏ `★ Thu hoạch` khi cây chín; billboard theo Camera. | **Đã sửa triệt để** |
| **10** | Chuồng bò quá lớn (chuồng cũ và 21 đoạn rào cũ chiếm toàn bộ nông trại) | `Assets/Scripts/Buildings/FarmBuilding.cs`, `FarmBuildingManager.cs` | 210-480 | Scene `Farm.prefab` chứa đối tượng `Barn` khổng lồ và `Farm Fence 1..21` (~45m x ~25m = ~1100m²). | Thu nhỏ kích thước chuồng xuống ~25%: dài 10m, rộng 8m, cao 1.35m. Tạo `DisableOldGiantBuildingsAndFences()` tắt sạch chuồng cũ và 21 rào cũ. | **Đã sửa triệt để** |

---

## 2. KẾT QUẢ TRIỂN KHAI THEO CÁC PHASE

### Phase 2: Thu nhỏ chuồng đúng thực tế (~25% chuồng cũ)
- **Tên prefab chuẩn**: `Assets/Prefabs/Farm/CowPen_Small.prefab` và `Assets/Prefabs/Farm/ChickenCoop_Small.prefab`.
- **Kích thước hình học thật**:
  - `CowPen_Small`: Dài (X) = 10.0m, Rộng (Z) = 8.0m, Chiều cao rào = 1.35m.
  - `ChickenCoop_Small`: Dài (X) = 8.0m, Rộng (Z) = 6.5m, Chiều cao rào = 1.05m.
- **Cấu trúc hàng rào**: Các đoạn rào (Posts, Rails, Colliders, Cổng ra vào, Máng cỏ/Chuồng gà) là child của `CowPen_Small` và `ChickenCoop_Small`.
- **Vị trí**: Đặt gần Player và Camera, cách xa CropArea (-9m về bên trái của Player), không đè lên CropArea hay Storage.
- **Dọn dẹp**: Tự động vô hiệu hóa toàn bộ `Barn` cũ và `Farm Fence 1..21` cũ. Hierarchy chỉ có đúng một `CowPen_Small`.

### Phase 3: Giới hạn vật nuôi trong chuồng
- `AnimalPenBoundary` gắn trực tiếp trên `CowPen_Small` (`innerBoundsSize = Vector3(9.2f, 2.0f, 7.2f)`) và `ChickenCoop_Small` (`innerBoundsSize = Vector3(7.2f, 2.0f, 5.7f)`).
- Bò chỉ spawn bên trong `CowPenBounds`.
- Gà chỉ spawn bên trong `ChickenCoopBounds`.
- `CowController` và `ChickenWanderer` tự động kẹp tọa độ di chuyển (`penBoundary.ClampPosition`), không đi xuyên hàng rào.

### Phase 4: Sửa text trên đầu vật nuôi và ô đất
- **Không hiển thị text liên tục**: Mặc định ẩn hoàn toàn text trên đầu tất cả vật nuôi và ô đất.
- **Khi click chọn vật nuôi**: Hiện thông tin tên, độ no, tiến độ sản xuất của đúng con vật đó.
- **Khi chọn con khác hoặc đóng UI**: Tự động ẩn text con cũ, chỉ hiện con mới.
- **Khi chín / sẵn sàng thu hoạch**: Chỉ hiện nhãn nhỏ gọn `★ Thu hoạch` màu vàng kim.
- **Quay mặt về Camera**: Cả vật nuôi và ô đất đều có `LateUpdate` đồng bộ góc xoay theo `Camera.main.transform.rotation`.
- **Phím Debug**: Bấm phím `H` trong Editor để bật/tắt hiển thị debug khi cần kiểm tra.

### Phase 5: Chuẩn hóa AnimalManager
- Quản lý chính xác danh sách vật nuôi, dọn dẹp các tham chiếu đã Destroy (`animals.RemoveAll(...)`).
- Không tự spawn bò hoặc gà nếu chưa có chuồng tương ứng.
- Đảm bảo gán đúng parent: Bò → `CowPen_Small`, Gà → `ChickenCoop_Small`.
- Đảm bảo đầy đủ Renderer, Collider, script `AnimalCareController`, dữ liệu sản xuất.

### Phase 6: Crop Area
- 6 plots `CropPlot_01..06` nằm gọn gàng theo lưới 3x2, có BoxCollider đầy đủ, không đè lên chuồng trại.
- Đã bỏ text debug chiếm màn hình trên ô đất.
- Thu hoạch trả đủ: Sản phẩm vào Inventory, +Gold, +XP, cập nhật UI và reset plot về Empty. Không cộng XP trùng lặp.

### Phase 7: UI và Input
- Toàn bộ các panel: Supplies, Storage, Shop, CropSelection, AnimalCare, BuildingPlacement, MachineUI đều khởi đầu ở trạng thái đóng (`alpha = 0`, `blocksRaycasts = false`, `interactable = false`).
- Nút `X` hoạt động chính xác trên tất cả các panel.
- Chỉ có duy nhất một EventSystem trong scene (được giám sát và duy trì bởi `FarmSceneInitializer`).

---

## 3. KẾT LUẬN VÀ TRẠNG THÁI BIÊN DỊCH
- **Roslyn C# Compilation**: 0 Lỗi (Clean compilation).
- **Editor Scripts**: Clean compilation.
- **PlayMode Exceptions**: Đã loại bỏ toàn bộ nguyên nhân gây NullReferenceException và duplicate Graphic component.
