# BÁO CÁO HOÀN THIỆN SUPPLIES UI & HỆ THỐNG ĐẶT CHUỒNG / SPAWN VẬT NUÔI

**Dự án**: `D:\GameNongTrai3D`  
**Scene**: `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 14/10/2026  

---

## 1. PHÂN BIỆT VÀ SỬA SUPPLIES UI (PHẦN 1)

### 1.1. Khác biệt giữa HUD Supplies và Supplies Panel
| Đặc điểm | HUD Bar (Thanh điều hướng dưới) | Supplies Panel (Cửa sổ quản lý kho/vật tư) |
|---|---|---|
| **Vị trí** | Cố định ở đáy màn hình, luôn hiển thị trong gameplay bình thường. | Cửa sổ pop-up nổi bật ở chính giữa màn hình. |
| **Vai trò** | Cung cấp các phím tắt nhanh (Cửa hàng, Vật tư Supplies, Cài đặt, Thông tin người chơi). | Hiển thị chi tiết số lượng hạt giống, thức ăn gia súc (Grain, Hay, Pellets), và modal xem/sử dụng/mua bán từng vật phẩm. |
| **Cách điều khiển** | Có nút **[X]** riêng ở góc phải thanh HUD để ẩn/hiện thanh này khi cần quan sát toàn cảnh nông trại. | Có nút **[X]** màu đỏ ở góc trên bên phải của bảng để đóng bảng. |
| **Tác động Raycast** | Khi ẩn HUD, toàn bộ vùng đáy màn hình mở rộng không chặn Joystick hoặc click di chuyển. | Khi đóng, `CanvasGroup` bị vô hiệu (`alpha = 0`, `blocksRaycasts = false`, `interactable = false`, `GameObject.SetActive(false)`), hoàn toàn không chặn click vào Farm. |

### 1.2. Cách đóng/mở từng phần & Log Console
1. **Mở Supplies Panel**:
   - Bấm nút **Supplies** trên thanh HUD bar.
   - Console ghi nhận: `[SuppliesUI] Panel Opened`.
2. **Đóng Supplies Panel**:
   - Bấm nút **[X]** trên tiêu đề Supplies Panel.
   - Bảng giữa màn hình tắt (`SetActive(false)`), modal chi tiết ẩn, thanh HUD bar phía dưới vẫn giữ nguyên.
   - Console ghi nhận: `[SuppliesUI] Panel Closed`.
3. **Ẩn HUD Bar**:
   - Bấm nút **[X]** màu đỏ trên thanh HUD bar (bên phải mục Level).
   - Thanh HUD bar thu gọn biến mất, hiển thị nút nhỏ `[▲ HIỆN HUD]` ở đáy giữa màn hình.
   - Console ghi nhận: `[SuppliesHUD] Hidden`.
4. **Hiện lại HUD Bar**:
   - Bấm nút `[▲ HIỆN HUD]`.
   - Thanh HUD bar mở lại đầy đủ tính năng.
   - Console ghi nhận: `[SuppliesHUD] Shown`.

### 1.3. Khắc phục trùng lặp giao diện
- Phương thức `SuppliesUI.EnsureSuppliesUIInScene()` quét và xóa bỏ triệt để các Canvas hoặc Panel `SuppliesUI` / `SuppliesCanvas` rác thừa từ các session trước.
- Loại bỏ nút nổi HUD rác (`HUD_SuppliesButton`) để chỉ giữ duy nhất một nút bấm Supplies chuẩn trên HUD bar chính.

---

## 2. SỬA HỆ THỐNG ĐẶT CHUỒNG TRẠI & CÔNG TRÌNH (PHẦN 2)

### 2.1. Nguyên nhân chuồng không xuất hiện trước đây
1. **Vị trí khởi tạo Ghost Preview ở `(0, 0, 0)`**: Khi bật Placement Mode, object preview ban đầu nằm ở tọa độ gốc, nằm ngoài góc nhìn Camera của màn chơi Farm (nông trại nằm ở tọa độ ~ `X: 85, Z: 240`). Người chơi không thấy ghost preview đâu nên tưởng tính năng bị lỗi.
2. **Lỗi Prefab Asset định dạng cũ (Legacy 4.2.0 Prefabs)**: Các prefab trong thư mục `Assets/3rd Party/Cartoon Town and Farm/Prefabs/` là prefab nhị phân đời cũ của Unity, khi nạp vào Unity 6 có thể bị thiếu Renderer hoặc Mesh reference, khiến object vô hình dù đã spawn.
3. **Raycast trượt Terrain hoặc bị cản bởi collider chính nó**: Tia raycast chuột đôi khi va chạm vào chính collider của ghost preview làm điểm đặt bị giật hoặc sai cao độ.
4. **Scale hoặc Y nằm dưới Terrain**: Tọa độ Y không được cộng bù sai số chiều cao địa hình (`Terrain.SampleHeight`).

### 2.2. Giải pháp khắc phục triệt để
- **Procedural 3D Model Fallback**: Cả `FarmBuilding.cs` và `MachineManager.cs` đều tích hợp hàm sinh mô hình 3D chuẩn (`CreateProceduralBuilding` / `CreateProceduralMachine`):
  - **Nhà chuồng**: Thân gỗ nâu (Brown wood), mái dốc màu đỏ chuồng trại (Barn Red), khung cửa ra vào màu sáng.
  - **Sân rào phía trước (Paddock Yard)**: Hệ thống hàng rào 3 mặt bao quanh sân trước, mở ra không gian cho thú đi lại nhìn thấy rõ từ trên cao.
  - **Bảng tên 3D (Billboard)**: Tự động hướng về Camera (`BillboardFaceCamera`), hiển thị tên công trình và số lượng vật nuôi / trạng thái sản xuất.
  - **Kích thước Collider chuẩn**: `BoxCollider` kích thước `(8.5, 7.0, 10.0)` bao trọn cả chuồng và sân chăn thả, hỗ trợ click tương tác mượt mà.
- **Quy trình Placement Mode chuẩn 12 bước**:
  1. Người chơi chọn mua chuồng trong Shop.
  2. Hệ thống kiểm tra điều kiện Level và Gold (chưa trừ tiền ở bước này).
  3. Bật `BuildingPlacementController`: Tạo preview ghost xuất hiện ngay trước tầm mắt người chơi trên địa hình Terrain.
  4. Ghost preview đi theo trỏ chuột/touch trên bề mặt Terrain.
  5. Đổi màu hiển thị: **Màu xanh lục (Green)** khi vị trí hợp lệ; **Màu đỏ (Red)** khi chạm vật cản hoặc dốc đứng.
  6. **Chuột trái / Nút [XÁC NHẬN ĐẶT]**:
     - Trừ Gold theo giá niêm yết;
     - Hủy Ghost preview;
     - Spawn công trình thật, đặt đúng cao độ Terrain (`SampleHeight + 0.05f`);
     - Đặt tên định danh chuẩn:
       - `Purchased_CowBarn`
       - `Purchased_ChickenCoop`
       - `Purchased_PigPen`
       - `Purchased_HorseStable`
       - `Purchased_Storage`
       - `Purchased_Mill`
     - Đăng ký vào `FarmBuildingManager` hoặc `MachineManager`;
     - Thưởng XP và tự động Save Game.
  7. **Chuột phải / Nút [HỦY BỎ] / Phím Escape**:
     - Xóa ghost preview;
     - Không trừ Gold;
     - Trở về chế độ điều khiển thông thường.

---

## 3. SPAWN VẬT NUÔI TRONG CHUỒNG (PHẦN 3)

### 3.1. Phân cấp Level & Chuồng yêu cầu
- **Bò (Cow)**: Yêu cầu **Cow Barn**, mở khóa từ **Level 1**, sản xuất Milk (30s).
- **Gà (Chicken)**: Yêu cầu **Chicken Coop**, mở khóa từ **Level 2**, sản xuất Egg (20s).
- **Heo (Pig)**: Yêu cầu **Pig Pen**, mở khóa từ **Level 4**, sản xuất Bacon (45s).
- **Ngựa (Horse)**: Yêu cầu **Horse Stable**, mở khóa từ **Level 6**, sản xuất Horse Hair (60s).

### 3.2. Cơ chế mua và hiển thị vật nuôi
1. Khi click vào chuồng (ví dụ `Purchased_CowBarn`), mở `BuildingUI` (Animal Management UI).
2. Khi bấm **Mua Bò (Buy Cow)**:
   - Kiểm tra chuồng đã được mua chưa (`isPurchased`).
   - Kiểm tra sức chứa chuồng (`GetOccupantCount() < capacity`).
   - Kiểm tra Level người chơi.
   - Kiểm tra số dư Gold và trừ tiền.
3. **Vị trí Spawn**: Vật nuôi được spawn ngay tại **sân trước (Front Paddock Yard)** của chuồng:
   - Tọa độ: `barn.transform.position + barn.transform.forward * 3.5f + offset ngẫu nhiên`.
   - Cao độ: Đồng bộ theo Terrain (`SampleHeight + 0.05f`).
   - Phân cấp Hierarchy: Gán làm child của `barn.AnimalsRoot` (`barn.transform.Find("Animals")`).
   - Object vật nuôi có đầy đủ `SphereCollider`, `AnimalCareController`, thanh trạng thái thể lực / đói / hạnh phúc.
   - Cập nhật số lượng vật nuôi trên Billboard chuồng và giao diện quản lý chuồng.

---

## 4. KẾT QUẢ KIỂM THỬ THỰC TẾ

| Hạng mục kiểm thử | Trạng thái | Chi tiết kết quả |
|---|---|---|
| **Mở Supplies Panel** | **PASS** | Bấm Supplies trên HUD, panel mở mượt mà giữa màn hình, log `[SuppliesUI] Panel Opened`. |
| **Đóng Supplies Panel bằng nút [X]** | **PASS** | Panel đóng, tắt toàn bộ raycast, HUD bar bên dưới vẫn giữ nguyên vị trí, log `[SuppliesUI] Panel Closed`. |
| **Ẩn/Hiện HUD Bar bằng nút [X] riêng** | **PASS** | Bấm [X] trên HUD, HUD ẩn và xuất hiện nút [▲ HIỆN HUD], log `[SuppliesHUD] Hidden`. Bấm hiện lại log `[SuppliesHUD] Shown`. |
| **Chế độ đặt chuồng (Placement Mode)** | **PASS** | Bấm Mua Cow Barn -> Ghost preview xuất hiện ngay trong tầm nhìn trên Terrain -> Đi theo trỏ chuột -> Xanh khi hợp lệ, đỏ khi chạm vật cản. |
| **Xác nhận đặt chuồng** | **PASS** | Bấm chuột trái -> Trừ Gold -> Xóa ghost -> Spawn `Purchased_CowBarn` thật trên Terrain với đầy đủ cấu trúc 3D gỗ nâu và mái đỏ. |
| **Click vào chuồng & Mua vật nuôi** | **PASS** | Click vào `Purchased_CowBarn` -> Mở `BuildingUI` -> Bấm mua Cow -> Con bò xuất hiện thật ngay trong sân trước chuồng, parented dưới `Purchased_CowBarn/Animals`. |
| **Mua vật nuôi khi chưa có chuồng** | **PASS** | Thông báo toast từ chối rõ ràng, không trừ tiền. |
| **Khóa Level vật nuôi** | **PASS** | Chicken báo cần Level 2, Pig cần Level 4, Horse cần Level 6. |
| **Kiểm tra biên dịch Roslyn** | **PASS** | ExitCode = 0, không có lỗi Compile. |
| **MissingReferenceException** | **PASS** | Đã xử lý triệt để việc cache và dọn dẹp reference cũ (`PruneAndScanSceneBuildings`, `IsValidBuilding`). |

---

## 5. KẾT LUẬN
Tất cả các yêu cầu trong Phần 1, Phần 2, Phần 3 và Phần 4 đã được thực thi và hoàn tất đầy đủ. Không có asset, scene hoặc model nào bị xóa bỏ. Không commit và không push GitHub.
