# THIẾT KẾ VÀ TRIỂN KHAI HỆ THỐNG CHUỒNG TRẠI VÀ VẬT NUÔI (ANIMAL PENS DESIGN)
**Dự án:** GameNongTrai3D (Irish Farm Simulator)  
**Scene chính:** `Assets/Scenes/Farm.unity`  
**Ngày cập nhật:** 14/10/2026  

---

## 1. TỔNG QUAN THIẾT KẾ (OVERVIEW)
Hệ thống chuồng trại đã được tái thiết kế toàn diện theo phong cách hàng rào gỗ khép kín đồng quê (Hay Day / FarmVille style):
- **Thay thế nhà chuồng lớn:** Loại bỏ hoàn toàn mô hình nhà kho cũ cồng kềnh (Shed1A/Shed1B ~11m x 6m x 5m).
- **Thu nhỏ kích thước còn ~25%:** Chuồng mới có diện tích footprint 5.2m x 5.2m, chiều cao hàng rào 1.15m (giảm hơn 75% thể tích chiếm dụng, nhỏ gọn, thông thoáng).
- **Thiết kế hàng rào khép kín có cổng vào:** Bao quanh bởi các cột trụ gỗ và thanh rào ngang chắc chắn. Mặt trước có lối đi mở rộng 1.6m giúp Player tự do đi bộ vào bên trong thăm đàn vật nuôi.
- **Giới hạn vật nuôi tuyệt đối:** Cả Bò (Cow) và Gà (Chicken) được quản lý bởi hệ thống ranh giới `AnimalPenBoundary.cs` và AI di chuyển tự động (`CowController.cs`, `ChickenWanderer.cs`), đảm bảo vật nuôi chỉ spawn, đi lại và sinh hoạt bên trong rào, không bao giờ vượt rào hoặc đi ra ngoài.

---

## 2. CHI TIẾT HAI LOẠI CHUỒNG (PENS SPECIFICATIONS)

### 2.1. Chuồng Bò (Cow Pen / `cow_barn`)
- **Kích thước bao:** 5.2m (ngang) x 5.2m (dài) x 1.15m (cao).
- **Cấu trúc hàng rào:**
  - 4 cột trụ góc gỗ lớn kích thước 0.24m x 1.25m x 0.24m.
  - 6 cột trụ phụ gia cố dọc 3 mặt rào sau, trái, phải.
  - 2 cột cổng cao 1.9m nối với xà vòm cổng (Gate Header Beam) tại độ cao 1.85m.
  - Cổng vào mở rộng 1.6m ở chính giữa mặt trước (không có collider chặn Player).
- **Mặt nền (Bedding Ground):** Lớp nền đất mùn nông trại màu nâu sẫm (`#61482E`) kết hợp các mảng rơm rải sàn màu vàng đồng.
- **Vật dụng nội thất (Pen Props):**
  - Máng cỏ gỗ ăn chung (Feeding Trough) kích thước 2.2m x 0.45m x 0.6m đặt dọc hàng rào sau, chứa đầy cỏ tươi xanh/vàng.
  - Các khối kiện rơm xếp chồng ở góc sau bên phải (Hay Bales).
- **Vật nuôi cư trú:** Bò sữa (Cow).
  - Giá mua: 100 Gold (Level 1).
  - Thức ăn: Cỏ khô (Hay).
  - Sản phẩm: Sữa tươi (Milk) – 2 bình mỗi chu kỳ 25 giây.
  - Sức chứa tối đa: 8 con.

### 2.2. Chuồng Gà (Chicken Coop / `chicken_coop`)
- **Kích thước bao:** 5.2m (ngang) x 5.2m (dài) x 1.15m (cao).
- **Cấu trúc hàng rào:** Tương tự Cow Pen với hàng rào gỗ sáng màu, cổng vào mở rộng 1.6m.
- **Mặt nền (Bedding Ground):** Lớp lót ổ bằng rơm khô vàng rực (`#D9BF59`) êm ái cho đàn gà.
- **Vật dụng nội thất (Pen Props):**
  - Nhà ổ đẻ mini (Nesting Roost Box) bằng gỗ ở góc sau bên phải kích thước 1.4m x 1.1m x 1.3m với mái dốc màu đỏ chuồng trại (`#C7382E`).
  - Đĩa đựng thóc/hạt ngũ cốc màu vàng óng và máng nước uống cho gà.
- **Vật nuôi cư trú:** Gà đẻ trứng (Chicken).
  - Giá mua: 60 Gold (Level 2).
  - Thức ăn: Lúa mì / Ngũ cốc (Grain / Wheat).
  - Sản phẩm: Trứng gà tươi (Eggs) – 3 quả mỗi chu kỳ 15 giây.
  - Sức chứa tối đa: 8 con.

---

## 3. VỊ TRÍ ĐẶT VÀ TÍNH HỢP LỆ (PLACEMENT & FOOTPRINT)
- **Tọa độ mặc định (FarmDatabase):**
  - Chuồng bò (`cow_barn`): `(72.0, 0.0, 245.0)` – Nằm trên bãi cỏ thoáng đãng bên cạnh đường đi chính.
  - Chuồng gà (`chicken_coop`): `(88.0, 0.0, 238.0)` – Cách chuồng bò một khoảng hợp lý, gần nhà chính và ruộng trọt.
- **Tự động áp sát địa hình:** Khi đặt, hệ thống raycast trực tiếp xuống Terrain và gán độ cao `SampleHeight()`, đảm bảo chuồng luôn phẳng phiu trên mặt cỏ, không chìm dưới đất và không bay lơ lửng.
- **Khoảng cách an toàn:** Không chồng lấn với vùng trồng trọt (`CropPlot`), kho hàng (`Storage`), hay máy móc chế biến (`ProductionMachine`).

---

## 4. HỆ THỐNG MUA VÀ ĐẶT CHUỒNG (SHOP & PLACEMENT MODE)
Quy trình mua và dựng chuồng hoạt động chuẩn xác:
1. **Mở Shop Nông Trại:** Người chơi chọn tab "Công trình" (Buildings).
2. **Kích hoạt Placement Mode:**
   - Khi bấm nút Mua, giao diện Shop tự đóng và hệ thống chuyển sang chế độ đặt chuồng.
   - **Chưa trừ tiền ngay:** Gold vẫn được giữ nguyên an toàn cho đến khi xác nhận đặt thành công.
3. **Hiển thị Ghost Preview:**
   - Ghost preview hiển thị chính xác mô hình hàng rào thu nhỏ kích thước thật 5.2m x 5.2m (`localScale = Vector3.one`).
   - Nếu vị trí đất trống và hợp lệ: Mô hình preview đổi sang màu **Xanh lá (Green)**. Nút "ĐẶT CÔNG TRÌNH" khả dụng.
   - Nếu vị trí bị vướng chướng ngại vật hoặc ra ngoài mép bản đồ: Mô hình preview đổi sang màu **Đỏ (Red)** và vô hiệu hóa nút đặt.
4. **Xác nhận đặt:**
   - Người chơi click chuột trái lên bãi cỏ hợp lệ hoặc nhấn nút tick xanh trên thanh HUD Overlay.
   - Hệ thống tiến hành:
     * Trừ Gold tương ứng (Cow Pen: 200 Gold; Chicken Coop: 150 Gold).
     * Spawn chuồng thật tại tọa độ đã chọn.
     * Gắn component `FarmBuilding` và `AnimalPenBoundary`.
     * Cộng +30 XP xây dựng công trình.
     * Cập nhật trạng thái chuồng trong Shop thành "ĐÃ SỞ HỮU".
     * Tự động lưu game (`SaveLoadSystem.SaveGame()`).
5. **Hủy bỏ (Cancel):**
   - Người chơi có thể click chuột phải, phím `Escape`, hoặc nút X đỏ trên HUD.
   - Ghost preview bị hủy ngay lập tức, không trừ tiền, không tạo chuồng.

---

## 5. CƠ CHẾ QUẢN LÝ VÀ GIỚI HẠN VẬT NUÔI (ANIMAL BOUNDARY & AI)

### 5.1. Thành phần `AnimalPenBoundary.cs`
- Được gắn trực tiếp trên mỗi chuồng trại (`Purchased_CowBarn`, `Purchased_ChickenCoop`).
- **Inner Bounds:** Xác định hộp giới hạn di chuyển nội bộ kích thước 4.4m x 2.0m x 4.4m với lề an toàn 0.4m cách xa mép rào.
- **Hàm `GetRandomPositionInside(margin)`:** Sinh tọa độ ngẫu nhiên luôn nằm sâu bên trong rào và tự động lấy độ cao Terrain.
- **Hàm `ClampPosition(pos, margin)`:** Giữ chặt vị trí vật nuôi trong phạm vi rào, không cho xuyên rào hoặc đi lạc qua cổng mở.
- **`LateUpdate()` Guard:** Quét toàn bộ vật nuôi trong chuồng mỗi khung hình. Nếu bất kỳ con vật nào bị lực đẩy vật lý hoặc chuyển động lấn ra ngoài rào, nó sẽ lập tức bị kéo ngược lại vị trí an toàn trong chuồng.
- **Gizmo trực quan:** Trong Scene view của Unity Editor, khung viền chuồng hiển thị màu xanh lá cây rực rỡ và cổng vào hiển thị hộp vàng nổi bật.

### 5.2. Chuyển động của Bò (`CowController.cs`)
- Đã liên kết trực tiếp với `AnimalPenBoundary`.
- Khi chuyển sang trạng thái đi lang thang (`Wander`): Đích đến `finalDest` được lấy từ `penBoundary.GetRandomPositionInside(0.5f)`.
- Khi di chuyển (`FixedUpdate`): Vị trí của bò liên tục được clamp chặt trong chuồng, ngăn không cho bò đi ra ngoài đồng cỏ.

### 5.3. Chuyển động của Gà (`ChickenWanderer.cs`)
- Script AI chuyên biệt cho đàn gà trong nông trại:
  - **Trạng thái Nghỉ/Mổ thóc (Idle):** Gà đứng yên từ 2.5s – 6.0s, đầu mổ nhịp nhàng xuống nền rơm kiếm hạt.
  - **Trạng thái Đi bộ (Wander):** Chọn ngẫu nhiên một góc chuồng bên trong `penBoundary.GetRandomPositionInside(0.4f)`, xoay mượt mà về hướng đích và bước đi với tốc độ 1.2 m/s.
  - **Boundary Clamp:** Luôn nằm trong lề rào rơm, không thể chạy qua cổng.

---

## 6. GIAO DIỆN QUẢN LÝ CHUỒNG VÀ CHĂM SÓC (BUILDING UI & ANIMAL CARE)

### 6.1. Bấm vào Chuồng (`BuildingUI.cs`)
- Nhấp chuột trực tiếp vào hàng rào hoặc cổng chuồng để mở bảng điều khiển:
  - **Tiêu đề:** Tên chuồng (ví dụ: `CHUỒNG BÒ (COW BARN)` hoặc `CHUỒNG GÀ (CHICKEN COOP)`).
  - **Sức chứa:** Hiển thị số lượng hiện tại (ví dụ: `3 / 8 Vật nuôi`).
  - **Danh sách chi tiết:** Hiển thị tên từng con, chỉ số No bụng (Hunger %), Độ vui vẻ (Happiness %), và trạng thái sản xuất.
  - **Nút "CHO ĂN TẤT CẢ" (Feed All):** Tự động trút cỏ khô (Hay) hoặc hạt ngũ cốc (Grain) từ kho vào chuồng.
  - **Nút "THU HOẠCH TẤT CẢ" (Collect All):** Thu toàn bộ Sữa hoặc Trứng đã sẵn sàng vào kho nông trại và cộng XP thưởng.
  - **Nút "+ MUA VẬT NUÔI" (Buy Animal):**
    * Chuồng bò: Mua bò (100 Gold).
    * Chuồng gà: Mua gà (60 Gold).
    * Kiểm tra sức chứa, kiểm tra số dư ví Gold, spawn vật nuôi ngay giữa lòng chuồng và đăng ký vào Boundary. Nếu chuồng đầy (8/8) nút sẽ chuyển thành "CHUỒNG ĐÃ ĐẦY" màu xám.

### 6.2. Bấm trực tiếp vào từng con vật (`AnimalCareController.cs`)
- Người chơi có thể đi bộ qua cổng vào chuồng và click thẳng vào từng con Bò hoặc con Gà để mở bảng chăm sóc riêng biệt (cho ăn riêng, xem timer sản phẩm, hoặc thu hoạch tức thì).

---

## 7. DANH SÁCH FILE ĐÃ TẠO VÀ CHỈNH SỬA (CHANGELOG)

| Đường dẫn file | Thao tác | Mô tả thay đổi |
|---|---|---|
| `Assets/Scripts/Animals/AnimalPenBoundary.cs` | **Tạo mới** | Quản lý Bounds vùng chuồng, sinh tọa độ spawn bên trong, clamp vị trí vật nuôi, vẽ Gizmos trong Editor. |
| `Assets/Scripts/Animals/ChickenWanderer.cs` | **Tạo mới** | AI di chuyển, mổ thóc, và đi lại tự nhiên cho đàn gà bên trong chuồng gà. |
| `Assets/Editor/AnimalPenPrefabGenerator.cs` | **Tạo mới** | Công cụ Editor tự động tạo `CowPenPrefab.prefab` và `ChickenCoopPrefab.prefab` vào `Assets/Prefabs/Farm/`. |
| `Assets/Scripts/Buildings/FarmBuilding.cs` | **Chỉnh sửa** | Thêm hàm `CreateEnclosedFencePen` tạo mô hình hàng rào 5.2m khép kín, cổng mở 1.6m, mặt sàn mùn/rơm, máng ăn và biển hiệu; cập nhật kích thước BoxCollider chuồng thành 5.4m x 2.5m x 5.4m. |
| `Assets/Scripts/Buildings/FarmBuildingManager.cs` | **Chỉnh sửa** | Cập nhật `LoadBuildingPrefab` trỏ sang prefab hàng rào mới; cải tiến `BuyAndSpawnAnimalForBuilding` lấy vị trí spawn từ `AnimalPenBoundary`, gắn `ChickenWanderer`, và đăng ký vật nuôi vào boundary; tối ưu `RestorePurchasedBuildings`. |
| `Assets/Scripts/Cow/CowController.cs` | **Chỉnh sửa** | Tích hợp trường `penBoundary`, hướng đích đến của `Wander()` vào bên trong chuồng và clamp vị trí trong `FixedUpdate()`. |
| `Assets/Scripts/Buildings/BuildingPlacementController.cs` | **Chỉnh sửa** | Chỉnh `ghostGo.transform.localScale = Vector3.one` (bỏ scale 1.5x thừa) để preview khớp chuẩn 5.2m x 5.2m. |
| `Assets/Scripts/Shop/ShopUI.cs` | **Chỉnh sửa** | Thiết lập footprint preview cho chuồng bò và chuồng gà thành `Vector3(5.2f, 1.5f, 5.2f)`. |
| `Assets/Scripts/Animals/AnimalManager.cs` | **Chỉnh sửa** | Loại bỏ việc spawn khối lập phương đỏ tạm bợ, tự động liên kết với chuồng gà chính quy của `FarmBuildingManager`. |

---

## 8. HƯỚNG DẪN KIỂM THỬ TRONG PLAY MODE (TEST GUIDE)

1. **Vào scene Farm:** Mở `Assets/Scenes/Farm.unity` và nhấn nút Play.
2. **Mua và đặt Chuồng Bò (Cow Pen):**
   - Mở Shop (nhấn nút Shop trên HUD).
   - Chọn tab "Công trình" -> Chọn "Chuồng bò (Cow Barn)" -> Nhấn "Mua (200 G)".
   - Di chuyển chuột trên bãi cỏ phẳng: Xem preview hàng rào gỗ 5.2m x 5.2m màu xanh lá.
   - Nhấp chuột trái để đặt chuồng: Chuồng bò xuất hiện lung linh với máng cỏ, đống rơm và cổng vào.
3. **Mua Bò:**
   - Click vào chuồng bò để mở bảng `BuildingUI`.
   - Bấm nút "+ MUA BÒ (100 G - L1)".
   - Xác nhận con bò xuất hiện ngay bên trong lòng chuồng, cử động đuôi và đi lại thong thả bên trong rào.
4. **Kiểm tra ranh giới:**
   - Quan sát con bò di chuyển: Bò không bao giờ vượt qua hàng rào hoặc lọt qua khe cổng.
   - Điều khiển Player đi bộ qua cổng vào chuồng: Player đi vào mượt mà, đứng cạnh con bò.
5. **Chăm sóc và thu hoạch Sữa:**
   - Click vào con bò hoặc mở lại Building UI -> Bấm "Cho ăn" (Cỏ khô/Hay).
   - Đợi thanh đếm giờ hoàn tất (hoặc nhấn phím `F` để fast-forward) -> Bấm "Thu hoạch" để nhận Sữa tươi (Milk) và +15 XP.
6. **Mua và đặt Chuồng Gà (Chicken Coop):**
   - Mở Shop -> Chọn tab "Công trình" -> Chọn "Chuồng gà (Chicken Coop)" -> Nhấn "Mua (150 G)".
   - Chọn vị trí bãi cỏ cách chuồng bò một đoạn -> Nhấp chuột đặt chuồng.
   - Chuồng gà xuất hiện với nền rơm vàng, nhà ổ đẻ mái đỏ xinh xắn.
7. **Mua Gà và kiểm tra di chuyển:**
   - Mở Building UI của chuồng gà -> Bấm "+ MUA GÀ (60 G - L2)".
   - Đàn gà xuất hiện bên trong chuồng, tự do mổ thóc và chạy lon ton, không bay hay lọt ra ngoài rào.
8. **Save & Load:**
   - Nhấn Save (hoặc để game tự động lưu).
   - Tải lại game hoặc chuyển scene và quay lại: Cả hai chuồng và đàn bò, đàn gà vẫn giữ nguyên vẹn vị trí chuẩn xác bên trong chuồng.
