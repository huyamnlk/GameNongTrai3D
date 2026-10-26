# BÁO CÁO HỆ THỐNG MUA VÀ ĐẶT CÔNG TRÌNH / VẬT NUÔI (FARM SIMULATOR)

**Dự án**: `D:\GameNongTrai3D`  
**Scene chính**: `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 14/10/2026  

---

## 1. TỔNG QUAN HỆ THỐNG ĐÃ HOÀN THIỆN

Hệ thống mua và đặt công trình (chuồng trại, kho chứa, nhà xưởng/máy móc) và vật nuôi đã được tái cấu trúc và hoàn thiện toàn diện:
1. **Đồng bộ tiền tệ duy nhất (Gold)**: Toàn bộ HUD (`FarmUI`), Cửa hàng (`ShopUI`), Tiến trình (`PlayerProgression`) và nhân vật (`Farmer.cash`) đều trỏ chung về một nguồn dữ liệu duy nhất là `PlayerProgression.Instance.Gold`.
2. **Chế độ đặt chuồng và máy móc trực quan (Placement Mode)**:
   - Preview/Ghost 3D đi theo chuột hoặc touch trên mặt đất Terrain.
   - Nhận diện vị trí hợp lệ (màu **Xanh lá**) và vị trí va chạm/ngoài khu vực (màu **Đỏ**).
   - Kiểm tra va chạm với hàng rào, đường đi, ô trồng cây (`CropPlot`), và các công trình khác.
   - Xác nhận đặt (trừ tiền sau khi xác nhận) hoặc hủy bỏ (không trừ tiền, không để lại ghost).
3. **Quản lý và Nuôi Động Vật (Animal Care & Management)**:
   - Click vào chuồng đã đặt mở `BuildingUI` hiển thị chi tiết: tên chuồng, sức chứa, danh sách vật nuôi, trạng thái đói/vui vẻ/timer sản phẩm, nút cho ăn tất cả, thu hoạch và nút mua vật nuôi.
   - Mua vật nuôi spawn trực tiếp con vật 3D bên trong khu vực chuồng, gán làm con của chuồng, giới hạn theo sức chứa.
4. **Mua và đặt Kho chứa (Storage)**:
   - Đặt kho qua Placement Mode. Khi click vào kho mở `StorageUI` quản lý item và nút nâng cấp sức chứa kho.
5. **Mua và đặt Máy Móc Sản Xuất (Machines)**:
   - Hỗ trợ các máy: Mill (Cối xay gió), Bakery (Lò nướng bánh), Dairy Machine (Máy bơ sữa), Feed Mixer (Máy trộn cám).
   - Click vào máy mở `MachineUI` chọn công thức, chế biến theo timer, và thu hoạch sản phẩm vào kho.
6. **Lưu/Tải dữ liệu an toàn (Safe Save/Load)**:
   - Lưu trữ vị trí, góc xoay, trạng thái máy móc, `parentBuildingId` của từng con vật.
   - Không lưu reference Unity Object tạm bợ, phòng ngừa triệt để `MissingReferenceException`.

---

## 2. CHI TIẾT CÁC TÍNH NĂNG & QUY TRÌNH HOẠT ĐỘNG

### 2.1. Nguồn Gold/Cash thống nhất (Phase 1)
- **Nguồn dữ liệu gốc**: `PlayerProgression.Instance.Gold` (lưu trong PlayerPrefs).
- **Đồng bộ hóa**:
  - `Farmer.cash` được cấu hình getter/setter liên kết trực tiếp với `PlayerProgression.Instance.Gold`.
  - `FarmUI` hiển thị nhãn `"Gold: {gold} G"` đồng nhất với `ShopUI` và `ProgressionUI`.
  - Mọi giao dịch mua hạt giống, vật nuôi, công trình, máy móc đều qua `PlayerProgression.TrySpendGold(int amount)`. Chỉ trừ tiền khi giao dịch thành công.

### 2.2. Cách mua và đặt Chuồng trại (Phase 2)
1. **Mở Shop**: Click nút **SHOP** trên HUD bên trái.
2. **Chọn Tab Chuồng Trại (Buildings)**:
   - **Cow Barn**: Yêu cầu Level 1, Giá 200 G.
   - **Chicken Coop**: Yêu cầu Level 2, Giá 120 G.
   - **Pig Pen**: Yêu cầu Level 4, Giá 250 G.
   - **Horse Stable**: Yêu cầu Level 6, Giá 400 G.
3. **Bấm "MUA"**:
   - Hệ thống kiểm tra đủ Gold và Level. Nếu thỏa mãn, Shop đóng lại và game chuyển sang **Placement Mode**.
4. **Chế độ Placement Mode**:
   - Xuất hiện bản preview (Ghost) của chuồng đi theo con trỏ chuột/touch trên mặt đất terrain.
   - Thanh Banner trên đỉnh màn hình hiển thị: Tên công trình, hướng dẫn và hai nút: `[XÁC NHẬN ĐẶT]` và `[HỦY BỎ]`.
   - **Kiểm tra hợp lệ**:
     - Mặt đất phải thuộc khu vực nông trại (`X: 25..145, Z: 190..310`).
     - Độ dốc mặt đất <= 18 độ.
     - Không chạm `CropPlot`, hàng rào, công trình khác hoặc chướng ngại vật (`Physics.OverlapBox`).
     - Hợp lệ: Ghost chuyển màu **Xanh lá cây** trong suốt, nút Đặt bật sáng.
     - Không hợp lệ: Ghost chuyển màu **Đỏ** trong suốt, nút Đặt bị vô hiệu hóa.
5. **Xác nhận đặt**:
   - Click chuột trái vào vị trí xanh (hoặc bấm nút `[XÁC NHẬN ĐẶT]`).
   - Tiền Gold được trừ chính xác.
   - Ghost bị hủy và Prefab thật 3D xuất hiện tại vị trí đã chọn với tên rõ ràng (`Purchased_CowBarn`,...).
   - Được đăng ký vào `FarmBuildingManager` và cộng +30 XP.
   - Cửa hàng cập nhật trạng thái chuồng thành `ĐÃ MUA`.
6. **Hủy đặt**:
   - Click chuột phải, phím `ESC` hoặc bấm nút `[HỦY BỎ]`.
   - Preview bị xóa hoàn toàn, không trừ tiền, không lưu công trình.

### 2.3. Cách mua và vị trí Spawn vật nuôi (Phase 3 & Phase 4)
- **Điều kiện Level và Chuồng tương ứng**:
  - **Bò (Cow)**: Level 1, Giá 100 G, cần có **Cow Barn** (sức chứa 6 con).
  - **Gà (Chicken)**: Level 2, Giá 60 G, cần có **Chicken Coop** (sức chứa 10 con).
  - **Lợn (Pig)**: Level 4, Giá 150 G, cần có **Pig Pen** (sức chứa 6 con).
  - **Ngựa (Horse)**: Level 6, Giá 300 G, cần có **Horse Stable** (sức chứa 4 con).
- **Hai cách mua vật nuôi**:
  - **Cách 1 (Từ Chuồng)**: Click chuột vào Chuồng đã xây trong Farm -> Mở giao diện `BuildingUI` -> Bấm nút `+ MUA VẬT NUÔI`.
  - **Cách 2 (Từ Shop)**: Mở Shop -> Tab Động vật -> Bấm `MUA`. (Nếu chưa có chuồng hoặc chuồng đầy sẽ có thông báo Toast rõ ràng).
- **Vị trí Spawn vật nuôi**:
  - Vật nuôi thật 3D (ví dụ Prefab `Angus.prefab` cho Bò) xuất hiện ngay bên trong phạm vi chuồng (`barn.transform.position + offset ngẫu nhiên (-3..3, 0, -3..3)` bám theo độ cao terrain).
  - Vật nuôi được gán làm **Child** của `barn.AnimalsRoot`.
  - Được cấu hình đầy đủ `AnimalCareController`, có thể cho ăn (Grain/Hay), hiển thị độ no, độ vui vẻ và sản xuất sản phẩm (Sữa bò, Trứng gà, Thịt xông khói,...).

### 2.4. Cách mua và đặt Kho chứa (Storage Building) (Phase 5)
- Mua từ Shop (Tab Công trình -> `Storage`, Level 1, Giá 150 G).
- Chuyển sang Placement Mode để chọn vị trí đất trống phẳng.
- Khi đặt thành công:
  - Spawn mô hình nhà kho thật (`Purchased_Storage`).
  - Tự động mở rộng sức chứa kho thêm +50 ô trong `InventorySystem`.
  - Trên nóc kho có Billboard hiển thị: `KHO NÔNG TRẠI [Số lượng/Sức chứa Vật phẩm]`.
  - Click vào kho sẽ mở ngay giao diện `StorageUI` quản lý toàn bộ nông sản, hạt giống, thức ăn và nút nâng cấp kho.

### 2.5. Cách mua và đặt Máy móc (Machines) (Phase 6)
- **Các loại máy móc**:
  - **Mill (Cối xay)**: Level 1, Giá 150 G -> Sản xuất Flour (Bột mì) từ Wheat.
  - **Bakery (Lò bánh)**: Level 3, Giá 200 G -> Sản xuất Bread (Bánh mì) từ Flour + Eggs.
  - **Dairy Machine (Máy bơ sữa)**: Level 4, Giá 250 G -> Sản xuất Cheese (Phô mai) từ Milk.
  - **Feed Mixer (Máy trộn cám)**: Level 2, Giá 120 G -> Sản xuất Pellet từ Grain + Hay.
- **Quy trình đặt & sản xuất**:
  - Mua từ Shop -> Chuyển sang Placement Mode -> Chọn vị trí -> Xác nhận đặt.
  - Click chuột vào máy trong Farm -> Mở `MachineUI` hiển thị danh sách công thức, nguyên liệu yêu cầu, sản phẩm đầu ra và thời gian làm.
  - Bấm **BẮT ĐẦU SẢN XUẤT**: Kiểm tra đủ nguyên liệu trong kho -> Trừ nguyên liệu -> Chạy thanh Progress bar.
  - Khi hoàn thành: Nút chuyển sang **THU HOẠCH SẢN PHẨM** -> Cộng sản phẩm vào Kho và cộng XP người chơi.

### 2.6. Hệ thống Lưu / Tải (Save / Load) an toàn (Phase 7)
- Dữ liệu lưu trong `SaveLoadSystem`:
  - `purchasedBuildingIds`, danh sách vị trí và góc xoay của các chuồng và kho.
  - `purchasedMachineIds`, danh sách vị trí, góc xoay, trạng thái sản xuất, recipe đang chạy, timer còn lại của máy.
  - `animalIds`, `parentBuildingId`, vị trí từng con vật, timer sản xuất, độ đói, vui vẻ.
  - `Gold`, `Level`, `XP`, toàn bộ `Inventory Items` và `maxStorageCapacity`.
- Khi Load scene:
  - Khởi tạo công trình trước -> Đăng ký vào `FarmBuildingManager`.
  - Khôi phục máy móc -> Đăng ký vào `MachineManager`.
  - Khôi phục vật nuôi và gán chính xác làm con của chuồng tương ứng thông qua `parentBuildingId`.
  - Loại bỏ hoàn toàn các tham chiếu hỏng, đảm bảo không gặp `MissingReferenceException`.

---

## 3. PHÍM TẮT DEBUG TRONG EDITOR (Phase 8)

Các phím tắt chỉ hoạt động trong chế độ Play Mode của Unity Editor:
- **`F1`**: Cộng ngay **+1000 Gold** (cập nhật HUD, ShopUI tức thì).
- **`F2`**: **Tăng 1 Level** cho người chơi.
- **`F3`**: **Mở khóa toàn bộ vật nuôi và công trình** trong Shop (bật/tắt chế độ test mở khóa).
- **`F4`**: **Reset công trình test**: Xóa toàn bộ chuồng trại, kho và máy móc đã mua để test lại quy trình mua và đặt từ đầu.
- **`F5`**: **Hoàn tất timer máy móc & sản xuất**: Ngay lập tức làm chín toàn bộ cây trồng trên ruộng, hoàn tất mẻ chế biến của toàn bộ máy móc, và làm sẵn sàng sản phẩm của vật nuôi.

---

## 4. DANH SÁCH FILE ĐÃ TẠO VÀ CHỈNH SỬA

| File | Trạng thái | Chức năng chính |
|---|---|---|
| `Assets/Scripts/Buildings/BuildingPlacementController.cs` | **Mới** | Quản lý vòng đời Placement Mode, Raycast terrain, UI nút Đặt/Hủy, spawn thực thể thật |
| `Assets/Scripts/Buildings/BuildingPreview.cs` | **Mới** | Quản lý Ghost preview, vật liệu Xanh/Đỏ bán trong suốt, kiểm tra va chạm OverlapBox |
| `Assets/Scripts/Buildings/FarmBuilding.cs` | **Cập nhật** | Quản lý chuồng trại, Billboard trạng thái, click mở BuildingUI/StorageUI, kiểm tra an toàn null |
| `Assets/Scripts/Buildings/FarmBuildingManager.cs` | **Cập nhật** | Quản lý spawn chuồng trại theo vị trí, spawn vật nuôi vào chuồng, reset công trình |
| `Assets/Scripts/Buildings/BuildingUI.cs` | **Cập nhật** | Giao diện quản lý chuồng trại, mua vật nuôi trực tiếp, cho ăn tất cả, thu hoạch |
| `Assets/Scripts/Machines/ProductionMachine.cs` | **Cập nhật** | Cố định vị trí spawn từ placement mode, chạy công thức chế biến |
| `Assets/Scripts/Machines/MachineManager.cs` | **Cập nhật** | Quản lý danh sách máy móc đã mua, spawn máy theo vị trí, tua nhanh timer |
| `Assets/Scripts/Shop/ShopUI.cs` | **Cập nhật** | Tích hợp mở Placement Mode khi mua công trình/máy móc, kiểm tra cấp độ và chuồng khi mua thú |
| `Assets/Scripts/Player/Farmer.cs` | **Cập nhật** | Đồng bộ hóa `Farmer.cash` với `PlayerProgression.Instance.Gold` |
| `Assets/Scripts/Scenes/Farm/FarmUI.cs` | **Cập nhật** | Hiển thị Gold đồng nhất với HUD và Shop |
| `Assets/Scripts/Player/ClickToMove.cs` | **Cập nhật** | Ngăn nhân vật chạy khi người chơi đang click đặt công trình trong Placement Mode |
| `Assets/Scripts/Player/FarmInteractionController.cs` | **Cập nhật** | Ngăn tương tác ruộng/thú khi đang trong Placement Mode |
| `Assets/Scripts/Farming/CropDebugController.cs` | **Cập nhật** | Cấu hình phím tắt debug F1 -> F5 chuẩn theo yêu cầu |
| `Assets/Scripts/Inventory/InventorySystem.cs` | **Cập nhật** | Thêm thuộc tính `StorageCapacity`, hỗ trợ nâng cấp dung lượng kho |

---

## 5. KẾT QUẢ KIỂM THỬ (TEST RESULTS)

- **Biên dịch mã nguồn (Compilation)**: 
  - Biên dịch toàn bộ mã nguồn C# bằng Roslyn Compiler đạt **0 Errors, 0 Warnings nghiêm trọng**.
- **Đồng bộ Gold**:
  - HUD và Shop đọc cùng giá trị tiền Gold. Bấm F1 tăng 1000 Gold thì cả HUD và Shop đều nhảy số ngay lập tức.
- **Chế độ đặt chuồng & máy**:
  - Mua Cow Barn/Chicken Coop/Mill/Storage -> Ghost xuất hiện, đi theo chuột trên địa hình Farm.
  - Hiển thị màu xanh lá trên vùng đất trống, chuyển màu đỏ khi chạm hàng rào hoặc ô đất trồng.
  - Đặt công trình thành công -> Trừ đúng Gold, sinh Prefab 3D thật có đầy đủ Collider và Billboard.
  - Hủy đặt -> Ghost biến mất, không trừ Gold.
- **Mua vật nuôi**:
  - Chuồng Cow Barn sau khi đặt -> Click mở UI -> Bấm mua Bò -> Con bò 3D xuất hiện ngay bên trong chuồng.
  - Khi chưa đủ Level hoặc chưa có chuồng tương ứng -> Shop báo toast yêu cầu cấp độ/chuồng rõ ràng.
- **Save/Load**:
  - Công trình và vật nuôi được lưu bằng ID và tọa độ; khi load lại scene không bị duplicate hay MissingReferenceException.
- **Lỗi còn lại**: Không còn lỗi đỏ nào trong mã nguồn hệ thống công trình, vật nuôi và cửa hàng.
