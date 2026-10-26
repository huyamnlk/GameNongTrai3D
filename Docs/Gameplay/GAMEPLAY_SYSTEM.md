# HƯỚNG DẪN HỆ THỐNG GAMEPLAY TOÀN DIỆN (GAMEPLAY SYSTEM)

Tài liệu hướng dẫn cơ chế vận hành, cách tương tác, trồng trọt, chăn nuôi, thu hoạch và điều khiển trong scene chính `Assets/Scenes/Farm.unity` của dự án **GameNongTrai3D** (Unity 6.0.6f1).

---

## 1. Tổng quan cách chơi (How to Play)

Khi khởi chạy scene `Farm.unity` (hoặc chuyển scene sang Farm), toàn bộ hệ thống được khởi tạo tự động qua `FarmSceneInitializer.cs`:
1. **Điều khiển nhân vật (Player Movement)**:
   - **PC / Editor**:
     - Phím **W, A, S, D** hoặc 4 phím mũi tên để di chuyển.
     - **Click chuột trái (Left-click)** vào mặt đất/Terrain để nhân vật chạy đến vị trí nhấp (Click-to-Move với vòng chỉ báo).
     - Raycast thông minh: Nhấp vào UI, ô đất hoặc vật nuôi sẽ tự động hủy di chuyển mặt đất để thực hiện tương tác.
   - **Mobile (Android Touch)**:
     - Cần điều khiển ảo **Virtual Joystick** ở góc dưới bên trái màn hình.
     - Chạm vào màn hình để tương tác trực quan.
2. **Giao diện HUD màn hình ngang (Landscape 1920x1080)**:
   - **Góc trên bên trái**: Thanh kinh nghiệm (XP Bar), huy hiệu Cấp độ (Level Badge) và thông số XP hiện tại / XP cần thiết.
   - **Góc trên bên phải**: 4 nút truy cập nhanh:
     - `SUPPLIES`: Mở cửa sổ vật phẩm & thức ăn.
     - `STORAGE`: Mở kho nông trại (Barn Storage).
     - `SAVE`: Lưu trạng thái game (Level, XP, Kho, Đất trồng, Vật nuôi).
     - `LOAD`: Tải lại trạng thái game từ file JSON / PlayerPrefs.

---

## 2. Hệ thống Vật phẩm & Cửa sổ Supplies (Phase 1)

### Cách mở và đóng Supplies
- Nhấp chuột hoặc chạm vào nút **`SUPPLIES`** ở góc trên bên phải màn hình.
- Đóng cửa sổ bằng nút **`X`** ở góc trên bên phải bảng Supplies.

### Tương tác với Grain, Hay, Pellets
- Trong bảng Supplies hiển thị 3 loại thức ăn chính với số lượng thời gian thực:
  - **Grain (Ngũ cốc / Ngô hạt)**
  - **Hay (Cỏ khô)**
  - **Pellets (Cám viên dinh dưỡng)**
- Khi đang chọn một con bò, nhấp vào thẻ Grain/Hay/Pellets sẽ cho bò ăn ngay lập tức, hồi máu & tăng hạnh phúc cho bò, đồng thời trừ số lượng tương ứng trong kho và cộng XP.
- Nút Supplies và cửa sổ Supplies sử dụng Unity UI (uGUI) hiện đại, tích hợp GraphicRaycaster và EventSystem, không chặn Joystick hoặc click mặt đất khi đã đóng.

---

## 3. Hệ thống Trồng cây & Thu hoạch (Phase 3)

### Vị trí và cơ chế Farm Plot
- `CropManager` tự động tạo một cụm đất canh tác gồm 6 ô đất (3x2 plots) gần nhân vật với mô hình 3D đất xới và cây trồng lớn dần trực quan.
- **Các trạng thái ô đất**:
  - `Plowed / Empty`: Ô đất đã cày xới, sẵn sàng gieo hạt. Nhãn hiển thị: `[Ô đất: Chạm để trồng]`.
  - `Growing`: Cây đang sinh trưởng, kích thước cây tăng dần theo thời gian, hiển thị % và số giây còn lại.
  - `ReadyToHarvest`: Cây đã chín vàng/đỏ, nhãn hiển thị: `[CHÍN! Chạm để thu hoạch]`.

### Các loại cây trồng
| Loại cây | Hạt giống cần | Thời gian lớn | Sản phẩm thu hoạch | XP trồng | XP thu hoạch |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Wheat (Lúa mì)** | 1x `seed_wheat` | 12 giây | 2x `wheat` | +2 XP | +10 XP |
| **Corn (Bắp Ngô)** | 1x `seed_corn` | 18 giây | 2x `corn` | +2 XP | +12 XP |
| **Carrot (Cà rốt)** | 1x `seed_carrot` | 15 giây | 3x `carrot` | +2 XP | +10 XP |
| **Hay Grass (Cỏ khô)** | 1x `seed_hay` | 10 giây | 4x `hay` | +2 XP | +8 XP |

### Quy trình trồng và thu hoạch
1. **Gieo hạt**:
   - Nhấp vào ô đất trống (`Plowed`). Menu chọn hạt giống (`CropPlotUI`) sẽ mở ra.
   - Chọn loại hạt giống muốn trồng (Lúa mì, Ngô, Cà rốt, Cỏ).
   - Hệ thống tự động kiểm tra số lượng hạt trong kho, trừ 1 hạt giống, gieo vào ô đất và cộng **+2 XP**.
2. **Chăm sóc & Phím tắt tăng tốc trong Editor**:
   - Nhấn phím **`F`** trên bàn phím: Tất cả cây đang lớn trên toàn bộ ô đất sẽ **chín ngay lập tức** để kiểm tra gameplay nhanh chóng.
3. **Thu hoạch**:
   - Khi cây chín (`ReadyToHarvest`), nhấp/chạm vào ô đất.
   - Nông sản tự động được chuyển vào kho `InventorySystem`, hiển thị thông báo popup **`+10 XP (Thu hoạch Lúa mì)`**, và ô đất tự reset về trạng thái `Plowed`.
   - Nếu kho đã đầy (Storage Full), hệ thống sẽ từ chối thu hoạch và cảnh báo cho người chơi dọn kho trước.

---

## 4. Hệ thống Vật nuôi & Sản phẩm (Phase 5)

### Danh sách vật nuôi hỗ trợ
- **Bò (Cows)**: Sử dụng các mô hình bò 3D có sẵn trong project (`Hereford`, `Angus`, `Holstein`, `Charolais`, `Brangus`, `Shorthorn`).
  - Thức ăn chấp nhận: **Hay (Cỏ khô)**, **Grain (Ngũ cốc)**, **Pellets (Cám viên)**.
  - Sản phẩm tạo ra: **Sữa bò tươi (`milk`)** (2 bình/lần).
- **Gà (Chicken Coop)**: Chuồng gà trang trại tại vị trí cố định trên sân.
  - Thức ăn chấp nhận: **Grain (Ngô hạt)**, **Pellets**.
  - Sản phẩm tạo ra: **Trứng gà tươi (`egg`)** (3 quả/lần).

### Chỉ số vật nuôi
- **Độ no (Hunger, 0 - 100%)**: Giảm dần theo thời gian. Khi đói (< 20%), vật nuôi sẽ tạm dừng sản xuất sản phẩm.
- **Hạnh phúc (Happiness, 0 - 100%)**: Tăng tốc độ sản xuất sản phẩm khi đạt mức cao.
- **Bộ đếm sản xuất (Product Timer)**: Đếm ngược thời gian tạo sản phẩm (15s - 25s).

### Cách chăm sóc và thu hoạch
1. **Mở bảng tương tác**: Nhấp chuột trái hoặc chạm vào con Bò hoặc Chuồng gà. Bảng `AnimalUI` sẽ mở ra.
2. **Cho ăn (Feed)**:
   - Nhấp nút `Cho ăn Cỏ khô`, `Cho ăn Ngô hạt` hoặc `Cho ăn Thức ăn viên`.
   - Tự động trừ 1 đơn vị thức ăn trong kho, tăng +35% độ no, +25% độ vui vẻ, đẩy nhanh tiến độ sản xuất và cộng **+3 XP**.
3. **Phím tắt tăng tốc**: Nhấn phím **`F`** để hoàn tất ngay chu kỳ sản xuất sản phẩm.
4. **Thu hoạch sản phẩm (Collect)**:
   - Khi sản phẩm sẵn sàng, nút `THU HOẠCH SẢN PHẨM` sáng lên.
   - Nhấp nút để thu hoạch: nhận ngay **+2 Sữa** hoặc **+3 Trứng** vào kho và cộng **+15 XP**.

---

## 5. Danh sách file code đã tích hợp và thay đổi

| Đường dẫn file | Mô tả vai trò |
| :--- | :--- |
| `Assets/Scripts/Player/ClickToMove.cs` | Sửa kiểm tra TerrainCollider an toàn, hủy di chuyển khi bấm vào UI, cây hoặc vật nuôi |
| `Assets/Scripts/Player/FarmInteractionController.cs` | Raycaster tương tác 3D cho Bò, Cây trồng và Chuồng gà |
| `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs` | Khởi tạo tự động toàn bộ 11 hệ thống của scene Farm |
| `Assets/Scripts/Scenes/Farm/SuppliesUI.cs` | Giao diện vật phẩm uGUI thay thế legacy IMGUI, hỗ trợ touch/mouse |
| `Assets/Scripts/Inventory/InventorySystem.cs` | Quản lý kho, sức chứa 150, DontDestroyOnLoad, chống mất dữ liệu |
| `Assets/Scripts/Inventory/StorageUI.cs` | Cửa sổ hiển thị kho dạng lưới, cảnh báo Storage Full |
| `Assets/Scripts/Farming/CropData.cs` | Dữ liệu cấu hình các loại cây trồng (Wheat, Corn, Carrot, Hay) |
| `Assets/Scripts/Farming/CropPlot.cs` | Logic sinh trưởng, hiển thị 3D và thu hoạch của từng ô đất |
| `Assets/Scripts/Farming/CropPlotUI.cs` | Menu chọn hạt giống gieo trồng uGUI |
| `Assets/Scripts/Farming/CropManager.cs` | Quản lý patch 6 ô đất và phím tắt F |
| `Assets/Scripts/Progression/PlayerProgression.cs` | Quản lý Level, XP, ngưỡng thăng cấp, cộng dồn dư XP |
| `Assets/Scripts/Progression/ProgressionUI.cs` | Thanh XP, Level, floating popup "+XP" và nút Save/Load trên HUD |
| `Assets/Scripts/Animals/AnimalCareController.cs` | Logic độ no, hạnh phúc, sản xuất sữa/trứng của vật nuôi |
| `Assets/Scripts/Animals/AnimalUI.cs` | Bảng chăm sóc, cho ăn và thu hoạch sản phẩm vật nuôi |
| `Assets/Scripts/Animals/AnimalManager.cs` | Quản lý vật nuôi và khởi tạo Chuồng gà |
| `Assets/Scripts/SaveLoad/SaveData.cs` | Cấu trúc dữ liệu JSON lưu trữ an toàn |
| `Assets/Scripts/SaveLoad/SaveLoadSystem.cs` | Lưu/Tải game qua JSON và PlayerPrefs |
