# TÀI LIỆU HỆ THỐNG TRỒNG TRỌT, THU HOẠCH & VẬT NUÔI (FARM GAMEPLAY)

## 1. Tổng quan hệ thống
Hệ thống gameplay nông trại hoàn chỉnh cho scene `Assets/Scenes/Farm.unity` trong Unity 6, bao gồm:
1. **Khu vực trồng trọt (CropArea)** với 4 ô đất độc lập (`CropPlot_01` .. `04`).
2. **Chu trình mùa vụ & 4 loại cây trồng**: Wheat (Lúa mì), Corn (Ngô), Carrot (Cà rốt), Hay (Cỏ khô).
3. **Hệ sinh thái thu hoạch & Kho đồ (InventorySystem)**: Lưu trữ nông sản, hạt giống, thức ăn gia súc và sản phẩm vật nuôi. Không làm mất dữ liệu khi chuyển scene (`DontDestroyOnLoad`).
4. **Tích hợp cửa sổ Supplies**: Đọc số lượng nguyên liệu thật (`grain`, `hay`, `pellet`) trực tiếp từ Inventory. Nút Supplies và nút X hoạt động ổn định, không chặn tương tác khi đóng.
5. **Tiến trình người chơi (PlayerProgression & UI)**: Cấp độ (Level), điểm kinh nghiệm (XP), thanh tiến trình XP, popup thăng cấp (Level Up).
6. **Chăm sóc vật nuôi (AnimalCareController & AnimalUI)**: Tương tác trực tiếp với Cow (Bò) và Chicken Coop (Chuồng gà); các chỉ số đói (Hunger), hạnh phúc (Happiness), hẹn giờ sản phẩm (Milk, Egg), cộng XP khi cho ăn và thu hoạch.
7. **Lưu & Đọc dữ liệu (SaveLoadSystem)**: Định dạng JSON (`farm_save.json`), có nút bấm SAVE/LOAD trực quan trên màn hình và phím tắt `F5` / `F9`.
8. **Phím tắt hỗ trợ kiểm tra (Debug Keys)**: `F1`, `F2`, `F3`, `F4`.

---

## 2. Vị trí và cấu trúc CropArea
- **Vị trí**: Nằm ngay cạnh vị trí Player xuất hiện (Player spawn: `(0, 1.5, 0)`), khu đất tọa lạc tại `(5.0, 0.05, 0.0)` với nền đất cày xới màu nâu tối rõ rệt.
- **Khuôn viên**: Được đánh dấu bởi 4 cọc gỗ góc ranh giới, không bị tường rào hay vật thể che khuất, nằm trọn trong góc nhìn camera chính.
- **Danh sách ô đất (4 ô đất riêng biệt xếp lưới 2x2)**:
  - `CropPlot_01`: tọa độ local `(-2.0, 0.05, -2.0)`
  - `CropPlot_02`: tọa độ local `( 2.0, 0.05, -2.0)`
  - `CropPlot_03`: tọa độ local `(-2.0, 0.05,  2.0)`
  - `CropPlot_04`: tọa độ local `( 2.0, 0.05,  2.0)`
- **Thành phần mỗi ô đất**:
  - `BoxCollider` để nhận click / chạm chuột.
  - Script `CropPlot`: Quản lý logic tăng trưởng, timer, visual cây con (`Sprout`) và cây lớn (`Mature`).
  - Điểm neo hiển thị cây trồng: `PlantPivot` tại `(0, 0.1, 0)`.

---

## 3. Cách trồng cây
1. **Di chuyển lại gần**: Điều khiển nhân vật bằng phím WASD, mũi tên hoặc click chuột xuống mặt đất/dùng Joystick ảo trên màn hình.
2. **Mở bảng chọn hạt giống**: Click chuột trái / chạm vào ô đất trống (`Empty` hoặc `Plowed`).
3. **Bảng chọn hạt giống (CropPlotUI)**: Hiển thị 4 loại cây trồng:
   - **Wheat (Lúa mì)**: Cần 1 hạt giống, lớn trong 10 giây, thu hoạch được 3 Lúa mì + 3 Hạt ngũ cốc (`grain`), +2 XP khi gieo, +10 XP khi thu hoạch.
   - **Corn (Ngô)**: Cần 1 hạt giống, lớn trong 14 giây, thu hoạch được 3 Bắp ngô, +2 XP khi gieo, +12 XP khi thu hoạch.
   - **Carrot (Cà rốt)**: Cần 1 hạt giống, lớn trong 12 giây, thu hoạch được 2 Củ cà rốt, +2 XP khi gieo, +8 XP khi thu hoạch.
   - **Hay (Cỏ khô)**: Cần 1 hạt giống, lớn trong 10 giây, thu hoạch được 3 Bó cỏ khô (`hay`), +2 XP khi gieo, +10 XP khi thu hoạch.
4. **Gieo hạt**: Click nút **Plant** tương ứng với loại cây muốn trồng. Hệ thống tự động trừ 1 hạt giống trong Inventory và chuyển trạng thái sang `Growing`.
*(Ghi chú: Nếu hết hạt giống, bạn có thể nhấn phím `F2` để nhận thêm 10 hạt giống mỗi loại).*

---

## 4. Cách thu hoạch
1. **Theo dõi sinh trưởng**: Ô đất hiển thị thanh thời gian lớn kèm tên cây trồng. Khi cây còn non, visual mầm cây nhỏ màu xanh mạ xuất hiện.
2. **Cây chín vàng**: Khi hết thời gian đếm ngược (hoặc nhấn `F1`), trạng thái chuyển sang `ReadyToHarvest` (Cây trưởng thành nở rộ màu vàng rực/lá xanh đậm, bảng thông báo đổi thành "Harvest!").
3. **Thao tác thu hoạch**:
   - Click chuột trái / chạm vào cây đã chín.
   - Sản phẩm nông sản lập tức được thêm vào Inventory.
   - Nhận điểm kinh nghiệm XP tương ứng và hiển thị thông báo popup thu hoạch nổi.
   - Ô đất tự động hoàn về trạng thái `Plowed` sẵn sàng cho đợt gieo hạt tiếp theo.
   - *Lưu ý*: Cây đang trong giai đoạn `Growing` không thể thu hoạch trước thời hạn.

---

## 5. Cách dùng cửa sổ Supplies
- **Mở cửa sổ**: Bấm vào nút **Supplies** màu xanh lá ở thanh công cụ dưới đáy màn hình.
- **Hiển thị số liệu thật**:
  - **Grain**: Số lượng ngũ cốc thật từ `InventorySystem.Instance.GetItemCount("grain")`.
  - **Hay**: Số lượng cỏ khô thật từ `InventorySystem.Instance.GetItemCount("hay")`.
  - **Pellets**: Số lượng cám viên từ `InventorySystem.Instance.GetItemCount("pellet")`.
- **Cơ chế cấp thức ăn nhanh cho Bò**:
  - Bấm vào biểu tượng Grain hoặc Hay trong Supplies sẽ trực tiếp cho con bò đang đứng gần ăn nếu trong kho còn nguyên liệu.
  - Sau khi cho ăn: trừ nguyên liệu trong kho, cộng +3 XP cho người chơi, cập nhật tức thì con số trên màn hình.
- **Đóng cửa sổ**:
  - Bấm nút **X** ở góc trên bên phải panel Supplies.
  - Cửa sổ đóng lại, không để lại panel vô hình chặn chuột hay che joystick.
  - Nút **Supplies** ở dưới đáy màn hình luôn luôn hiển thị để mở lại bất kỳ lúc nào.

---

## 6. Cách dùng Inventory (Kho đồ)
- **Tự động liên kết**: Mọi hoạt động thu hoạch cây trồng, cho vật nuôi ăn, thu hoạch trứng/sữa đều trực tiếp đồng bộ với `InventorySystem`.
- **Sức chứa kho**: Mặc định 200 đơn vị vật phẩm. Nếu kho đầy, hệ thống hiển thị thông báo cảnh báo "Storage Full!" và không làm tràn đồ.
- **Bảo lưu dữ liệu**: Hệ thống sử dụng `DontDestroyOnLoad`, đảm bảo tài sản không bị mất khi chuyển cảnh giữa Farm, Mart, Town, v.v.

---

## 7. Hệ thống Kinh nghiệm (XP) và Cấp độ (Level)
- **Bảng HUD Progression**: Nằm ở góc trên bên trái màn hình:
  - Cấp độ hiện tại: `LVL X`.
  - Thanh đo XP dạng thanh trượt màu vàng cam.
  - Tỉ lệ kinh nghiệm: `XP: hiện_tại / cần_thiết`.
- **Bảng điểm kinh nghiệm tiêu chuẩn**:
  - Gieo hạt cây bất kỳ: **+2 XP**
  - Thu hoạch Lúa mì (Wheat): **+10 XP**
  - Thu hoạch Ngô (Corn): **+12 XP**
  - Thu hoạch Cà rốt (Carrot): **+8 XP**
  - Thu hoạch Cỏ khô (Hay): **+10 XP**
  - Cho vật nuôi ăn (Feed Cow/Chicken): **+3 XP**
  - Thu hoạch sản phẩm vật nuôi (Collect Milk/Eggs): **+15 XP**
- **Thăng cấp (Level Up)**:
  - Khi thanh XP đạt ngưỡng, người chơi tăng 1 cấp, lượng XP thừa được bảo lưu cho cấp tiếp theo.
  - Hiển thị bảng chúc mừng "LEVEL UP! Reached Level X" ở giữa màn hình trong 3 giây.

---

## 8. Chăm sóc vật nuôi (Cows & Chicken Coop)
- **Vật nuôi trong Farm**:
  - Các con bò sữa (Cow) tại khu chuồng trại chăn thả.
  - Chuồng gà (Chicken Coop) nằm trong khu chăn nuôi gia cầm.
- **Tương tác**:
  - Click chuột / chạm trực tiếp vào con bò hoặc chuồng gà.
  - Mở bảng thông tin vật nuôi (`AnimalUI`):
    - Tên con vật: Cow / Chicken Coop.
    - Thanh đói bụng (Hunger): Khi ăn thức ăn sẽ hồi phục lên 100%.
    - Thanh hạnh phúc (Happiness): Tăng khi được cho ăn đầy đủ.
    - Thời gian ấp/tạo sản phẩm: Đếm ngược để cho Sữa (`Milk`) hoặc Trứng (`Egg`).
- **Nút hành động**:
  - **Feed (Hay / Grain)**: Trừ 1 Hay hoặc 1 Grain trong Inventory, tăng chỉ số no và vui vẻ, cộng **+3 XP**.
  - **Collect**: Khi sản phẩm sẵn sàng (hoặc bấm `F4`), nút Collect sáng lên. Bấm vào sẽ nhận 1 Sữa/Trứng vào Inventory và thưởng **+15 XP**.
  - **Nút X**: Đóng bảng thông tin vật nuôi một cách an toàn.

---

## 9. Hệ thống Lưu & Đọc (Save / Load)
- **Vị trí lưu trữ**: File JSON lưu tại `Application.persistentDataPath/farm_save.json`.
- **Dữ liệu được lưu**:
  - Level và điểm kinh nghiệm XP của người chơi.
  - Toàn bộ danh mục vật phẩm trong Inventory (hạt giống, cây trồng, sữa, trứng, thức ăn).
  - Trạng thái từng ô đất (`CropPlot_01` -> `04`), loại cây đang trồng, thời gian sinh trưởng còn lại.
  - Trạng thái các con vật, chỉ số đói bụng, chỉ số hạnh phúc, bộ đếm thời gian tạo sản phẩm.
- **Thao tác**:
  - Bấm nút **SAVE** / **LOAD** ở góc dưới bên trái màn hình HUD.
  - Hoặc sử dụng phím tắt: `F5` để Save, `F9` để Load.

---

## 10. Phím Debug (Dành cho Editor / Test nhanh)
| Phím tắt | Chức năng kiểm tra | Chi tiết tác động |
| :--- | :--- | :--- |
| **F1** | Trưởng thành tức thì tất cả cây | Chuyển tất cả các ô đất đang trồng sang trạng thái `ReadyToHarvest` ngay lập tức để kiểm tra thu hoạch. |
| **F2** | Thêm vật phẩm vào kho | Thêm ngay +10 mỗi loại hạt giống (`seed_wheat`, `seed_corn`, `seed_carrot`, `seed_hay`) và +10 thức ăn (`hay`, `grain`, `pellet`). |
| **F3** | Cộng kinh nghiệm | Cộng ngay **+50 XP** để kiểm tra thanh tiến trình và hiệu ứng popup Level Up. |
| **F4** | Tạo sản phẩm vật nuôi tức thì | Đưa bộ đếm sản phẩm của tất cả Bò và Gà về 0 giây, sẵn sàng để bấm Collect (thu hoạch sữa/trứng). |
| **F5** | Lưu dữ liệu | Ghi đè trạng thái toàn bộ trang trại vào file JSON. |
| **F9** | Tải dữ liệu | Đọc và khôi phục trạng thái toàn bộ trang trại từ file JSON. |

---

## 11. Danh sách file code đã tạo và chỉnh sửa
1. `Assets/Scripts/Farming/CropData.cs`: Định nghĩa dữ liệu cây trồng (Wheat, Corn, Carrot, Hay, thời gian, sản lượng, XP).
2. `Assets/Scripts/Farming/CropPlot.cs`: Quản lý ô đất, trạng thái sinh trưởng, hiển thị visual mầm cây/cây chín, cơ chế thu hoạch và phím F1.
3. `Assets/Scripts/Farming/CropPlotUI.cs`: Giao diện menu chọn loại hạt giống để gieo trồng khi click ô đất.
4. `Assets/Scripts/Farming/CropManager.cs`: Tự động khởi tạo khu đất `CropArea` và 4 ô `CropPlot_01..04`, các phím tắt debug F1..F4.
5. `Assets/Scripts/Inventory/InventorySystem.cs`: Quản lý kho lưu trữ vật phẩm, sức chứa, tự động nạp hạt giống ban đầu, hỗ trợ DontDestroyOnLoad.
6. `Assets/Scripts/Scenes/Farm/SuppliesUI.cs`: Cập nhật đọc số lượng thật từ Inventory, liên kết nút đóng X và nút HUD Supplies bên dưới.
7. `Assets/Scripts/Progression/PlayerProgression.cs`: Hệ thống tính điểm XP và cấp độ Level người chơi.
8. `Assets/Scripts/Progression/ProgressionUI.cs`: Giao diện HUD hiển thị thanh XP, Level và popup thăng cấp.
9. `Assets/Scripts/Animals/AnimalCareController.cs`: Logic chăm sóc từng con vật (Bò, Gà), tính thời gian tạo sản phẩm, cho ăn và thu hoạch.
10. `Assets/Scripts/Animals/AnimalManager.cs`: Quản lý toàn bộ vật nuôi trong scene Farm.
11. `Assets/Scripts/Animals/AnimalUI.cs`: Bảng UI tương tác chi tiết khi click vào vật nuôi.
12. `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`: Hệ thống lưu và tải dữ liệu toàn diện định dạng JSON, tích hợp nút UI SAVE/LOAD.
13. `Assets/Scripts/Player/ClickToMove.cs`: Ngăn chặn click xuyên thấu xuống đất khi các cửa sổ UI (Supplies, Crop, Animal) đang mở; xử lý an toàn tag "Terrain".
14. `Assets/Scripts/Player/FarmInteractionController.cs`: Bộ điều khiển raycast bắt tương tác với CropPlot và Animal trên thế giới 3D.

---

## 12. Tình trạng lỗi và lưu ý
- **Lỗi biên dịch (Compile errors)**: **0 lỗi đỏ (Clean Build, ExitCode: 0)**.
- **Nút Supplies**: Được bảo toàn 100%, không bị xóa hay ghi đè, hoạt động mở/đóng mượt mà.
- **Scene & Tài nguyên**: Toàn bộ scene cũ, terrain, prefab vật nuôi và model gốc được giữ nguyên vẹn.
