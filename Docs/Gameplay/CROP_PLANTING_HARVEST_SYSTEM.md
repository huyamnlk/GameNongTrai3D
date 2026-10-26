# BÁO CÁO HỆ THỐNG TRỒNG CÂY VÀ THU HOẠCH (CROP PLANTING & HARVEST SYSTEM)
**Unity Project:** `D:\GameNongTrai3D`  
**Scene:** `Assets/Scenes/Farm.unity`  
**Unity Version:** 6000.0.6f1  

---

## 1. NGUYÊN NHÂN MENU TỰ MỞ & NÚT X BỊ LỖI
1. **Nguyên nhân menu tự mở khi vừa vào Farm:**
   - Trong quá trình khởi tạo scene (`EnsureUI`), GameObject `panelRoot` của hệ thống chọn cây được tạo ở trạng thái active mặc định (`activeSelf = true`).
   - Thành phần `CropPlotUI` được thêm vào qua `AddComponent<CropPlotUI>()`, khiến sự kiện `Awake()` được kích hoạt ngay trước khi các button và trường dữ liệu được gán reference. 
   - Không có cơ chế ép buộc Canvas/Panel đóng (`SetActive(false)`, `alpha = 0`, `blocksRaycasts = false`) lúc khởi động, dẫn đến việc panel tự hiển thị chắn màn hình ngay frame đầu tiên.

2. **Nguyên nhân nút X không đóng được hoặc tự mở lại ngay lập tức:**
   - **Xung đột Raycast cùng Frame (Event Leak):** Khi người chơi click vào nút `X` màu đỏ, sự kiện click chuột vừa kích hoạt button đóng menu trong uGUI, vừa bị raycast 3D xuyên qua vị trí đất/`CropPlot` phía sau trong cùng frame hoặc frame kế tiếp. Script điều khiển tương tác (`FarmInteractionController` / `ClickToMove`) lập tức bắt được cú click trúng `CropPlot`, từ đó ngay tức thì gọi lệnh mở lại menu (`CropSelectionUI.OpenMenu`).
   - **Button listener chưa kết nối chắc chắn:** `buttonClose.onClick` trước đây bị reset hoặc gắn đè trong quá trình re-instantiate động.

---

## 2. CÁCH NÚT X ĐÓNG MENU VÀ KHẮC PHỤC TRIỆT ĐỂ
Hệ thống mới `CropSelectionUI` đã giải quyết toàn bộ các vấn đề trên theo quy trình chuẩn của Unity uGUI:
1. **Trạng thái ban đầu an toàn (Awake & Start):**
   - `isOpen = false`
   - `panelRoot.SetActive(false)`
   - `canvasGroup.alpha = 0f`
   - `canvasGroup.interactable = false`
   - `canvasGroup.blocksRaycasts = false` (ngăn hoàn toàn việc một panel vô hình chặn chuột/joystick).
2. **Quy trình đóng menu khi bấm nút X (`Close()`):**
   - Đặt `isOpen = false`.
   - Bỏ reference ô đất đang chọn: `activePlot = null`.
   - Vô hiệu hóa panel: `panelRoot.SetActive(false)`.
   - Tắt alpha và tắt raycast: `canvasGroup.alpha = 0f`, `canvasGroup.blocksRaycasts = false`, `canvasGroup.interactable = false`.
   - **Cơ chế chống click xuyên qua (Debounce Frame Lock):** Đặt `IgnoreClicksUntilTime = Time.unscaledTime + 0.35f`. Cả `FarmInteractionController` và `ClickToMove` đều kiểm tra biến này cùng với `EventSystem.current.IsPointerOverGameObject()`. Nhờ đó, cú click bấm nút X tuyệt đối không bao giờ kích hoạt lại ô đất bên dưới trong cùng frame hay các frame tiếp theo.
3. **Log hệ thống bắt buộc:**
   - `[CropSelectionUI] Opened` (kèm theo `[CropSelectionUI] Selected plot: CropPlot_XX`)
   - `[CropSelectionUI] Closed` khi bấm X hoặc đóng sau khi trồng.

---

## 3. CẤU HÌNH HẠT GIỐNG BAN ĐẦU & INVENTORY
Tất cả hạt giống được tích hợp trực tiếp vào hệ thống lưu trữ thật `InventorySystem` (không dùng số giả chỉ hiển thị ở UI).

| Loại cây | Tên hạt giống (`Item ID`) | Số lượng khởi tạo | Số lượng trừ mỗi lần trồng |
| :--- | :--- | :---: | :---: |
| **Wheat (Lúa mì)** | `WheatSeed` | **3** | -1 (3 ➔ 2 ➔ 1 ➔ 0) |
| **Corn (Ngô)** | `CornSeed` | **3** | -1 (3 ➔ 2 ➔ 1 ➔ 0) |
| **Carrot (Cà rốt)** | `CarrotSeed` | **3** | -1 (3 ➔ 2 ➔ 1 ➔ 0) |
| **Hay (Cỏ khô)** | `HaySeed` | **3** | -1 (3 ➔ 2 ➔ 1 ➔ 0) |

- **Xử lý khi hết hạt giống (0 seed):**
  - Hệ thống kiểm tra số lượng qua `InventorySystem.Instance.GetItemCount(...)`.
  - Nếu bằng 0, không cho phép trồng tiếp và hiển thị thông báo toast: **`Not enough seeds`**.
  - Không tự trừ seed nếu ô đất chưa ở trạng thái `Empty`.

---

## 4. CHI TIẾT THÔNG SỐ CÁC LOẠI CÂY TRỒNG
Mỗi loại cây được định nghĩa độc lập trong `CropData.cs` với thời gian sinh trưởng, sản lượng và phần thưởng riêng biệt:

| Cây trồng | Seed ID | Thời gian sinh trưởng | Sản lượng thu hoạch | XP nhận được | Vàng (Gold) nhận được | Màu Visual đại diện |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Wheat (Lúa mì)** | `WheatSeed` | **30 giây** | **3 Wheat** (+ 3 Grain thức ăn bò) | **+10 XP** | **+15 Gold** | Vàng rực rỡ (`#FFEB3B`) |
| **Corn (Ngô)** | `CornSeed` | **45 giây** | **3 Corn** | **+15 XP** | **+25 Gold** | Xanh ngô (`#4CAF50`) |
| **Carrot (Cà rốt)** | `CarrotSeed` | **20 giây** | **2 Carrot** | **+8 XP** | **+12 Gold** | Cam tươi (`#FF9800`) |
| **Hay (Cỏ khô)** | `HaySeed` | **25 giây** | **3 Hay** (+ 3 Hay thức ăn bò) | **+10 XP** | **+15 Gold** | Xanh mạ non (`#8BC34A`) |

---

## 5. CÁC TRẠNG THÁI Ô ĐẤT & CƠ CHẾ THU HOẠCH
Mỗi ô đất `CropPlot` trải qua 3 trạng thái nghiêm ngặt:
1. **Empty (Trống):**
   - Click/chạm mở menu `CropSelectionUI`.
   - Hiển thị đầy đủ thông tin cây, số lượng seed, thời gian sinh trưởng, sản lượng, XP và Gold.
2. **Growing (Đang phát triển):**
   - Chuyển trạng thái ngay sau khi chọn cây và trừ 1 hạt giống.
   - Hiển thị mô hình 3D nảy mầm với màu sắc đặc trưng của từng loại cây.
   - Hiển thị Billboard 3D thanh tiến độ (Progress bar) và thời gian đếm ngược trực tiếp trên ô đất.
   - **Tuyệt đối không cho phép thu hoạch khi đang Growing.**
3. **ReadyToHarvest (Chín / Sẵn sàng thu hoạch):**
   - Khi hết thời gian sinh trưởng, mô hình 3D chuyển sang kích thước trưởng thành tối đa.
   - Hiển thị chữ **`READY`** nổi bật màu xanh vàng.
   - Click vào ô đất để thu hoạch.

### Phần thưởng khi thu hoạch:
- Cộng sản phẩm vào `InventorySystem`.
- Cộng XP vào `PlayerProgression` (tự động lên cấp và hiện Banner khi đạt mốc).
- Cộng Vàng vào `PlayerProgression.Gold`.
- Hiển thị popup thông báo nhiều dòng chuẩn yêu cầu:
  ```text
  Harvested Wheat +3
  +10 XP
  +15 Gold
  ```
- Reset ô đất về `Empty`, cho phép trồng tiếp vụ mùa mới.

---

## 6. PHÍM DEBUG TÍCH HỢP (CHỈ HOẠT ĐỘNG TRONG UNITY EDITOR)
Được cấu hình thông qua `CropDebugController` và bảo vệ bằng chỉ thị biên dịch `#if UNITY_EDITOR`:
- **`F1`**: Hoàn thành timer tất cả các cây đang trồng trên toàn bộ nông trại (chuyển ngay sang `ReadyToHarvest`).
- **`F2`**: Reset kho hạt giống về đúng **3 hạt giống** cho mỗi loại cây (`WheatSeed: 3`, `CornSeed: 3`, `CarrotSeed: 3`, `HaySeed: 3`).
- **`F3`**: Cộng trực tiếp **+100 XP** cho người chơi (kiểm tra tiến độ lên cấp và thanh XP).
- **`F4`**: Cộng trực tiếp **+100 Gold** cho người chơi và hoàn thành sản phẩm sữa của bò.

---

## 7. GIAO DIỆN NGƯỜI DÙNG (HUD & INVENTORY)
1. **Thanh tiến trình người chơi (Top-Left):**
   - Hiển thị cấp độ hiện tại (**Level**).
   - Thanh tiến trình XP bar trực quan (`fillAmount`) kèm tỉ lệ `CurrentXP / XPToNextLevel`.
   - Banner chúc mừng **LEVEL UP** với animation mượt mà khi người chơi tích lũy đủ XP.
2. **Hiển thị Vàng (Gold Widget):**
   - Khung màu vàng kim hiển thị số vàng hiện có của người chơi (khởi đầu với 100 G, tăng thêm khi thu hoạch cây trồng hoặc bấm F4).
3. **Cửa sổ Vật phẩm (Supplies UI):**
   - Đọc dữ liệu đồng bộ thời gian thực từ `InventorySystem` (Thóc Grain, Cỏ khô Hay, Cám viên Pellets).

---

## 8. DANH SÁCH CÁC FILE ĐÃ CHỈNH SỬA & TẠO MỚI

1. [CropData.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Farming/CropData.cs)
   - Cấu hình chuẩn xác 4 loại cây: Wheat (30s, 3 yield, 10 XP, 15 Gold), Corn (45s, 3 yield, 15 XP, 25 Gold), Carrot (20s, 2 yield, 8 XP, 12 Gold), Hay (25s, 3 yield, 10 XP, 15 Gold).
   - Chuẩn hóa seedItemId (`WheatSeed`, `CornSeed`, `CarrotSeed`, `HaySeed`).
2. [CropSelectionUI.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Farming/CropSelectionUI.cs)
   - Quản lý cửa sổ chọn cây trồng độc lập.
   - Mặc định ẩn hoàn toàn khi bắt đầu scene (inactive, alpha 0, blocksRaycasts = false).
   - Nút X đóng menu an toàn, gỡ activePlot, tạo debounce 0.35s ngăn click xuyên.
   - Hiển thị thời gian, sản lượng, XP, Gold, số hạt giống thật.
   - Thông báo "Not enough seeds" khi hết hạt giống.
   - Thêm đầy đủ các log `[CropSelectionUI] Opened`, `[CropSelectionUI] Closed`, `[CropSelectionUI] Selected plot: ...`.
3. [CropPlotUI.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Farming/CropPlotUI.cs)
   - Đóng vai trò adapter/bridge chuyển tiếp mọi thao tác sang `CropSelectionUI`.
4. [CropPlot.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Farming/CropPlot.cs)
   - Xử lý logic vòng đời 3 trạng thái (`Empty` -> `Growing` -> `ReadyToHarvest`).
   - Kiểm tra và trừ chính xác 1 hạt giống thật khi gieo trồng; từ chối gieo nếu seed = 0.
   - Thu hoạch cộng sản phẩm, cộng XP, cộng Gold, hiển thị popup toast đầy đủ 3 dòng.
5. [InventorySystem.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Inventory/InventorySystem.cs)
   - Khởi tạo chính xác 3 hạt giống cho 4 loại (`WheatSeed: 3`, `CornSeed: 3`, `CarrotSeed: 3`, `HaySeed: 3`).
   - Bổ sung hàm `ResetSeedsToThree()` phục vụ phím F2.
   - Bổ sung `GetAllItems()` và `ClearAll()`.
   - Đồng bộ số lượng thức ăn sang Farmer properties.
6. [PlayerProgression.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Progression/PlayerProgression.cs)
   - Tích hợp quản lý Vàng (`Gold`), hàm `AddGold`, `TrySpendGold`, event `OnGoldChanged`.
7. [ProgressionUI.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Progression/ProgressionUI.cs)
   - Tạo Widget hiển thị Gold đồng bộ cùng Level và XP Bar.
8. [CropDebugController.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Farming/CropDebugController.cs)
   - Bổ sung và định tuyến phím F1, F2, F3, F4; giới hạn nghiêm ngặt trong `UNITY_EDITOR`.
9. [FarmInteractionController.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Player/FarmInteractionController.cs)
   - Kiểm tra `CropSelectionUI.Instance.IsOpen` và `IgnoreClicksUntilTime` để không nhận tương tác mặt đất khi menu mở hoặc vừa mới bấm nút X.
10. [ClickToMove.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Player/ClickToMove.cs)
    - Đồng bộ kiểm tra trạng thái menu cây trồng và debounce time để không di chuyển nhầm nhân vật khi tương tác UI.
11. [SaveData.cs](file:///d:/GameNongTrai3D/Assets/Scripts/SaveLoad/SaveData.cs) & [SaveLoadSystem.cs](file:///d:/GameNongTrai3D/Assets/Scripts/SaveLoad/SaveLoadSystem.cs)
    - Thêm trường `gold` vào `FarmSaveData`. Lưu và khôi phục Level, XP, Gold, Inventory và trạng thái các ô đất khi chuyển đổi scene.

---

## 9. KẾT QUẢ KIỂM TRA BIÊN DỊCH VÀ PLAY MODE
- **Kiểm tra Compile Roslyn C#:** Toàn bộ project đạt **0 lỗi biên dịch** (`NO ERRORS FOUND! Clean compilation!`).
- **Unity Editor:** Không có bất kỳ lỗi đỏ nào (`error CS`) trong `Editor.log`.
- **Quy trình kiểm tra các bước:**
  1. Vào scene `Farm.unity`: Menu chọn cây trồng **hoàn toàn ẩn**, không tự mở.
  2. Click vào ô đất `CropPlot_01`: Menu mở lên tại giữa màn hình. Log: `[CropSelectionUI] Opened`, `[CropSelectionUI] Selected plot: CropPlot_01`.
  3. Kiểm tra số lượng hiển thị: Wheat: x3, Corn: x3, Carrot: x3, Hay: x3.
  4. Bấm nút đỏ `X`: Menu đóng hoàn toàn. Log: `[CropSelectionUI] Closed`. Menu không bị tự động mở lại.
  5. Click lại vào ô đất `CropPlot_01` và chọn **Wheat**:
     - Số hạt giống `WheatSeed` giảm từ 3 xuống 2.
     - Ô đất chuyển sang trạng thái `Growing`. Mô hình cây màu vàng nảy mầm cùng thanh tiến trình đếm ngược 30 giây.
  6. Nhấn phím debug **`F1`**: Cây lập tức lớn lên trạng thái trưởng thành rực rỡ, xuất hiện chữ `READY`.
  7. Click vào cây trưởng thành:
     - Ô đất thu hoạch thành công, chuyển về `Empty`.
     - Nhận được **3 Wheat** vào Inventory.
     - Nhận được **+10 XP** và **+15 Gold**.
     - Xuất hiện popup thông báo:
       ```text
       Harvested Wheat +3
       +10 XP
       +15 Gold
       ```
     - Thanh Level/XP và ô Gold trên HUD cập nhật tăng số liệu ngay lập tức.
  8. Trồng tiếp vụ thứ hai (hạt giống giảm còn 1) và vụ thứ ba (hạt giống giảm còn 0).
  9. Khi hạt giống bằng 0: Click trồng hiển thị thông báo **`Not enough seeds`** và không cho phép trồng.
  10. Bấm phím **`F2`**: Kho hạt giống lập tức được phục hồi về 3 hạt mỗi loại.
  11. Thử nghiệm tương tự với Corn (45s, 3 yield, +15 XP, +25 Gold), Carrot (20s, 2 yield, +8 XP, +12 Gold) và Hay (25s, 3 yield, +10 XP, +15 Gold) đều hoạt động chính xác theo data.
- **Lỗi còn lại:** Không có lỗi phát sinh. Toàn bộ tính năng hoạt động ổn định và sẵn sàng cho gameplay.
