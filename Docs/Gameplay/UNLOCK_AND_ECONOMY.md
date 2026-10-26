# HỆ THỐNG KINH TẾ, MỞ KHÓA LEVEL VÀ KHO LƯU TRỮ (UNLOCK, ECONOMY & STORAGE)

Tài liệu hướng dẫn và đặc tả kỹ thuật chi tiết về Cấp độ (Level), Kinh nghiệm (XP), Tiền tệ (Gold), Nâng cấp Kho chứa (Storage) và Lưu trữ dữ liệu (Save/Load) trong **GameNongTrai3D** (`Assets/Scenes/Farm.unity`).

---

## 1. TỔNG QUAN HỆ THỐNG KINH TẾ VÀ TIẾN TRÌNH

Dự án áp dụng mô hình kinh tế nông trại hoàn chỉnh, kết nối đồng bộ giữa các hệ thống:
- **Tiến trình người chơi (`PlayerProgression`)**: Quản lý Level, điểm kinh nghiệm (XP) và số dư Vàng (Gold).
- **Kho lưu trữ (`InventorySystem` & `StorageUI`)**: Quản lý toàn bộ vật phẩm trong game theo từng danh mục với giới hạn sức chứa và cơ chế nâng cấp.
- **Hệ thống mở khóa nội dung (`ShopUI` & `FarmDatabase`)**: Kiểm tra cấp độ người chơi trước khi cho phép mua vật nuôi, chuồng trại hoặc máy móc.
- **Lưu trữ dữ liệu an toàn (`SaveLoadSystem`)**: Lưu lại chính xác tiến trình kinh tế, công trình, máy móc và vật nuôi bằng định dạng JSON độc lập, không phụ thuộc vào con trỏ Unity GameObject.

---

## 2. NGUỒN NHẬN ĐIỂM KINH NGHIỆM (XP) VÀ TIỀN VÀNG (GOLD)

### Nguồn nhận XP (Kinh nghiệm lên cấp):
1. **Trồng trọt**:
   - Gieo hạt cây trồng: **+5 XP**.
   - Thu hoạch nông sản (Wheat, Corn, Carrot, Hay): **+15 đến +25 XP** mỗi ô.
2. **Chăn nuôi**:
   - Cho vật nuôi ăn qua bảng chi tiết Supplies hoặc bảng Chuồng trại: **+15 XP** mỗi lần cho ăn.
   - Thu hoạch sản phẩm vật nuôi (Sữa bò, Trứng gà...): **+20 đến +30 XP**.
3. **Chế biến máy móc**:
   - Chế biến bột mì (Mill): **+12 XP**.
   - Nướng bánh mì (Bakery): **+25 XP**.
   - Làm phô mai (Dairy Machine): **+20 XP**.
   - Trộn cám viên (Feed Mixer): **+10 XP**.

### Nguồn nhận và tiêu dùng Vàng (Gold):
- **Thu nhập Gold**:
  - Bán nông sản thô và hạt giống dư thừa qua nút Sell trong Kho/Supplies.
  - Bán thành phẩm chế biến cao cấp (Bột mì, Bánh mì, Phô mai) với giá trị gấp 2 - 3 lần nguyên liệu thô.
  - Hoàn thành các đơn đặt hàng nông trại.
- **Tiêu dùng Gold**:
  - Mua hạt giống và thức ăn chăn nuôi.
  - Mua chuồng trại (200 - 400 Gold).
  - Mua máy móc chế biến (200 - 400 Gold).
  - Mua vật nuôi giống (50 - 200 Gold).
  - Nâng cấp sức chứa Kho lưu trữ (100 Gold / lần nâng cấp).

---

## 3. LỘ TRÌNH MỞ KHÓA NỘI DUNG THEO LEVEL (LEVEL UNLOCK ROADMAP)

Hệ thống khóa nội dung nghiêm ngặt nhằm tạo động lực cày cuốc và thăng tiến cho người chơi:

| Cấp độ (Level) | Vật nuôi mở khóa | Chuồng trại mở khóa | Máy móc mở khóa | Nông sản & Vật phẩm |
| :---: | :--- | :--- | :--- | :--- |
| **Level 1** | **Cow** *(Bò sữa - 150 G)* | **Cow Barn** *(300 G)* | **Mill** *(250 G)*, **Feed Mixer** *(200 G)* | Wheat, Corn, Carrot, Hay, Pellets, Grain |
| **Level 2** | **Chicken** *(Gà - 50 G)* | **Chicken Coop** *(200 G)* | **Bakery** *(400 G)*, **Dairy Machine** *(350 G)* | Trứng gà, Bột mì, Bánh mì, Phô mai |
| **Level 4** | **Pig** *(Heo - 200 G)* | **Pig Pen** *(400 G)* | Công thức chế biến thịt heo xông khói | Cải bắp, Thức ăn tổng hợp |
| **Level 6** | **Horse** *(Ngựa - 400 G)* | **Stable** *(Chuồng ngựa - 600 G)* | Trang thiết bị nông nghiệp cao cấp | Cà rốt chất lượng cao |

### Sự kiện khi Lên cấp (Level Up Event):
- Khi thanh tiến trình XP đạt 100%, `PlayerProgression` tự động tăng cấp độ lên `Level + 1`.
- Hiển thị Banner chúc mừng: *"CHÚC MỪNG BẠN ĐÃ ĐẠT LEVEL X! Các vật nuôi và máy móc mới đã được mở khóa trong Shop!"*.
- Bảng Cửa hàng (Shop UI) tự động mở khóa các biểu tượng có cấp độ tương ứng.

---

## 4. HỆ THỐNG KHO LƯU TRỮ VÀ NÂNG CẤP SỨC CHỨA (STORAGE EXPANSION)

1. **Hiển thị thông tin**:
   - Nút **[STORAGE]** nằm ở góc trên bên trái màn hình.
   - Khi nhấp vào, giao diện Kho mở ra hiển thị:
     - Tổng số ô vật phẩm đang sử dụng / Sức chứa tối đa (ví dụ: `Kho: 18 / 50`).
     - Danh sách từng nhóm hàng: *Hạt giống, Nông sản, Thức ăn chăn nuôi, Sản phẩm động vật, Thành phẩm chế biến*.
2. **Cơ chế Nâng cấp Kho (Storage Upgrade)**:
   - Trên bảng Storage có nút **[Nâng Cấp Kho (+50 Chỗ - 100 Vàng)]**.
   - Điều kiện: Người chơi có đủ 100 Gold.
   - Khi bấm nâng cấp:
     - Trừ 100 Gold từ tài khoản người chơi.
     - Tăng ngay lập tức **+50 ô chứa** cho Kho (ví dụ từ 50 lên 100, 150...).
     - Cập nhật số liệu trên UI và lưu ngay vào hệ thống dữ liệu.
     - Báo Toast thông báo: *"Nâng cấp kho thành công! Sức chứa mới: X ô"*.
3. **Cơ chế Cảnh báo và An toàn**:
   - Khi người chơi thu hoạch hoặc sản xuất mà Kho đã đạt giới hạn tối đa, hệ thống hiển thị cảnh báo đỏ *"Kho đã đầy!"* và ngăn không cho nhặt thêm đồ thô nhằm bảo toàn hàng hóa.
   - Thành phẩm từ máy móc được giữ an toàn trong máy, không bao giờ bị biến mất.

---

## 5. CẤU TRÚC DỮ LIỆU VÀ CƠ CHẾ SAVE/LOAD

Hệ thống được thiết kế theo nguyên lý chuẩn công nghiệp: **Không lưu Unity Object Reference trực tiếp vào file lưu trữ**.

### Các trường dữ liệu được lưu trong `SaveData.cs`:
- `playerLevel`: Cấp độ hiện tại.
- `currentXP`: Điểm kinh nghiệm hiện có.
- `targetXP`: Điểm kinh nghiệm cần để lên cấp tiếp theo.
- `gold`: Số dư vàng.
- `storageCapacity`: Sức chứa kho sau khi nâng cấp.
- `items`: Danh sách các vật phẩm tồn kho (`itemId`, `quantity`).
- `purchasedBuildings`: Danh sách chuồng trại đã mua và số lượng vật nuôi bên trong.
- `machines`: Danh sách máy móc đã mua (`machineId`, `state`, `currentRecipeId`, `elapsedTime`, `targetTime`).
- `plots`: Danh sách ô đất trồng cây, loại hạt giống và thời gian sinh trưởng.

### Cơ chế lưu và nạp:
- **Tự động lưu**: Được gọi khi hoàn thành giao dịch lớn (mua chuồng, nâng cấp kho) hoặc khi chuyển scene.
- **Tự động nạp**: Khi vào lại scene `Farm.unity`, dữ liệu JSON được đọc và tự động khôi phục toàn bộ vị trí, số lượng vật nuôi và trạng thái máy móc một cách chính xác.

---

## 6. BẢNG PHÍM TẮT DEBUG TRONG UNITY EDITOR (#if UNITY_EDITOR)

Bộ phím tắt hỗ trợ kiểm thử tính năng nhanh chóng trong môi trường Unity Editor:

| Phím tắt | Chức năng kiểm thử | Hiệu ứng trên màn hình |
| :---: | :--- | :--- |
| **F1** | **+100 XP** | Tăng thanh kinh nghiệm, lên cấp ngay lập tức để kiểm tra mở khóa Shop |
| **F2** | **+1000 Gold** | Bổ sung ngân sách dồi dào để test mua chuồng trại, máy móc và nâng cấp kho |
| **F3** | **Toggle Unlock All Shop** | Mở khóa toàn bộ vật phẩm trong Shop bất kể cấp độ để kiểm thử tính năng |
| **F4** | **Fast-Forward All Timers** | Hoàn thành ngay lập tức thời gian lớn của Cây trồng, Máy móc và Vật nuôi |
| **F5** | **Reset Test Data** | Trả toàn bộ dữ liệu về mặc định (3 hạt mỗi loại, Level 1, 0 XP, 500 Gold) |

---

## 7. DANH SÁCH FILE LIÊN QUAN

- `Assets/Scripts/Progression/PlayerProgression.cs`: Singleton tiến trình, quản lý XP, Level, Gold.
- `Assets/Scripts/Progression/ProgressionUI.cs`: Thanh HUD hiển thị Level, XP Bar, Gold và 2 nút mở nhanh SHOP / STORAGE.
- `Assets/Scripts/Inventory/InventorySystem.cs`: Logic kho hàng, giới hạn sức chứa, hàm `UpgradeCapacity()`.
- `Assets/Scripts/Inventory/StorageUI.cs`: Giao diện Kho hàng và nút nâng cấp sức chứa.
- `Assets/Scripts/SaveLoad/SaveData.cs` & `SaveLoadSystem.cs`: Cấu trúc JSON và logic lưu/tải dữ liệu bền vững.
- `Assets/Scripts/Farming/CropDebugController.cs`: Cài đặt các phím tắt F1 - F5.
