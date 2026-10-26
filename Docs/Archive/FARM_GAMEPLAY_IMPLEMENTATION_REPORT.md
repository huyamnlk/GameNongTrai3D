# BÁO CÁO TRIỂN KHAI & KIỂM THỬ GAMEPLAY NÔNG TRẠI (FARM GAMEPLAY IMPLEMENTATION REPORT)

## 1. Tổng quan hiện trạng và giải pháp
Tất cả các thành phần cốt lõi của gameplay nông trại đã được xây dựng và kết nối hoàn chỉnh vào scene `Assets/Scenes/Farm.unity` trong Unity 6:
- **Khu ruộng thực tế (CropArea)** cùng **6 ô đất riêng biệt (`CropPlot_01` đến `CropPlot_06`)** được khởi tạo tự động ngay trước mắt Player trên mặt đất.
- **Visual đất và cây trồng rõ nét**: Mỗi ô đất là một khối đất màu nâu tối nổi bật trên Terrain, có viền cày xới, collider bắt tương tác, và nhãn hiển thị 3D TextMesh trực quan trạng thái ô đất.
- **Chu trình cây trồng hoàn chỉnh**: Empty $\rightarrow$ Chọn giống $\rightarrow$ Gieo hạt (trừ giống, +2 XP, visual mầm non vươn lên) $\rightarrow$ Sinh trưởng có thời gian đếm ngược $\rightarrow$ Chín vàng sẵn sàng thu hoạch (đổi visual sang cây lớn vàng rực) $\rightarrow$ Thu hoạch (tăng nông sản trong kho, +10 XP, popup thông báo, xóa visual cây, trả về Empty để trồng tiếp).
- **Phím debug tức thì**: `F1` (làm chín tất cả cây), `F2` (thêm vật phẩm/hạt giống), `F3` (+100 XP), `F4` (tạo sản phẩm bò/gà).
- **Supplies & Inventory đồng bộ**: Cửa sổ Supplies đọc số liệu nguyên liệu thật (`grain`, `hay`, `pellet`) trực tiếp từ `InventorySystem`, tăng ngay khi thu hoạch Lúa mì/Cỏ khô và giảm khi cho bò ăn; nút đóng X và nút HUD Supplies ở dưới đáy màn hình hoạt động trơn tru.

---

## 2. Vị trí CropArea và Tên các ô CropPlot
- **Vị trí CropArea trong thế giới 3D**:
  - Tọa độ gốc: Tự động căn chỉnh nằm cách Player **6.5 mét về phía trước (Forward) và lệch 1.5 mét sang phải (Right)**, đảm bảo nằm trọn vẹn trong khung hình Main Camera khi vừa bấm Play.
  - Chiều cao (Y): Được tính toán tự động bằng `Terrain.SampleHeight()` + độ cao của Terrain + `0.06m` bù nổi, đảm bảo các ô đất nằm ngay trên mặt cỏ, không bị chìm xuống dưới lòng đất và không bị hàng rào che khuất.
- **Tên 6 ô đất trồng con (Lưới 3 cột x 2 hàng, khoảng cách 3.2m)**:
  1. `CropPlot_01`: Cột trái, Hàng trước (`-3.2m, -1.6m`)
  2. `CropPlot_02`: Cột giữa, Hàng trước (` 0.0m, -1.6m`)
  3. `CropPlot_03`: Cột phải, Hàng trước (`+3.2m, -1.6m`)
  4. `CropPlot_04`: Cột trái, Hàng sau (`-3.2m, +1.6m`)
  5. `CropPlot_05`: Cột giữa, Hàng sau (` 0.0m, +1.6m`)
  6. `CropPlot_06`: Cột phải, Hàng sau (`+3.2m, +1.6m`)
- **Cấu tạo mỗi ô đất**:
  - `BoxCollider` kích thước `(2.6, 0.5, 2.6)` đón nhận click chuột trái và cảm ứng chạm.
  - Khối đất màu nâu trầm `(0.35, 0.22, 0.12)`.
  - Khối mầm cây / thân cây gồm 5 khóm cây sắp xếp đối xứng.
  - Nhãn 3D TextMesh hiển thị tên ô đất và trạng thái (Ví dụ: `[CropPlot_01]\nTrống: Chạm để trồng`).
  - Script điều khiển logic `CropPlot.cs`.

---

## 3. Cách kiểm thử Trồng cây (Planting Flow)
1. Mở Unity Editor và mở scene `Assets/Scenes/Farm.unity`.
2. Bấm nút **Play** (Play Mode).
3. Nhìn thẳng về phía trước nhân vật Player: Sẽ thấy khu ruộng `CropArea` gồm 6 ô đất màu nâu với nhãn xanh lá `[CropPlot_01] Trống: Chạm để trồng`.
4. Điều khiển nhân vật di chuyển lại gần bằng phím `WASD` hoặc click chuột xuống đất / dùng Joystick.
5. Click chuột trái vào bất kỳ ô đất nào (Ví dụ `CropPlot_01`):
   - Bảng **CHỌN CÂY TRỒNG / PLANT CROPS** (`CropPlotUI`) lập tức hiện lên giữa màn hình.
   - Hiển thị 4 loại cây: Wheat (Lúa mì), Corn (Ngô), Carrot (Cà rốt), Hay (Cỏ khô) kèm số lượng hạt giống đang có sẵn trong kho.
6. Click vào ô **Wheat**:
   - Trừ 1 hạt giống `seed_wheat` trong Inventory (nếu kho chưa có, hệ thống tự động cấp hạt giống khởi đầu để người chơi test ngay).
   - Ô đất đổi trạng thái sang `Growing`.
   - Visual mầm cây nhỏ màu xanh non lập tức xuất hiện và bắt đầu lớn dần theo thời gian.
   - Nhãn 3D đổi thành: `Wheat / Lúa mì: X% (Xs)`.
   - Thanh kinh nghiệm nhận ngay **+2 XP**.

---

## 4. Cách kiểm thử Thu hoạch (Harvesting Flow)
1. **Kiểm tra thời gian sinh trưởng tự nhiên**:
   - Chờ bộ đếm thời gian (Wheat: 10 giây). Cây sẽ lớn dần từ kích thước mầm nhỏ lên cây lớn.
2. **Kiểm tra chín nhanh bằng Debug**:
   - Nhấn phím **`F1`** trên bàn phím:
   - Tất cả các ô đất đang trồng lập tức chuyển sang trạng thái `ReadyToHarvest`.
   - Visual cây phóng to cực đại và chuyển sang màu vàng chín rực rỡ.
   - Nhãn 3D đổi thành: `[Wheat / Lúa mì]\nSẴN SÀNG THU HOẠCH!`.
   - Có popup thông báo toast: `[F1 Debug] Cây đã chín vàng! Hãy click để thu hoạch.`.
3. **Thao tác thu hoạch**:
   - Click chuột trái vào ô cây chín:
   - Nhân vật thực hiện thu hoạch:
     - Số lượng `crop_wheat` tăng thêm **+3** trong Inventory.
     - Số lượng `grain` (thức ăn chăn nuôi) tăng thêm **+3** trong Inventory.
     - Điểm kinh nghiệm nhận thêm **+10 XP**.
     - Console in rõ ràng:
       ```text
       [CropPlot] Harvested Wheat / Lúa mì +3
       [Progression] +10 XP
       ```
     - Trên màn hình xuất hiện popup toast vàng nổi bật:
       ```text
       Harvested Wheat / Lúa mì +3
       +10 XP
       ```
     - Visual cây lúa lập tức biến mất, đất trở về trạng thái nâu phẳng `Empty`.
     - Nhãn 3D quay lại: `[CropPlot_01]\nTrống: Chạm để trồng`.
     - Cho phép click để trồng tiếp đợt mới ngay lập tức.

---

## 5. Danh sách Phím Debug (Editor Play Mode)
| Phím tắt | Chức năng kiểm tra | Tác động thực tế |
| :---: | :--- | :--- |
| **`F1`** | Chín cây lập tức | Gọi `ForceMature()` trên toàn bộ 6 ô `CropPlot`. Cây chuyển sang chín vàng và sẵn sàng thu hoạch ngay. |
| **`F2`** | Bơm vật phẩm vào kho | Thêm ngay **+10** mỗi loại: Wheat, Corn, Carrot, Hay, Grain, Pellets và tất cả hạt giống vào Inventory. |
| **`F3`** | Cộng điểm kinh nghiệm | Thêm ngay **+100 XP** vào `PlayerProgression`, giúp kiểm tra làm đầy thanh XP và kích hoạt popup "LEVEL UP!". |
| **`F4`** | Kích hoạt sản phẩm vật nuôi | Đưa bộ đếm sản phẩm của tất cả Bò và Gà về hoàn tất, sẵn sàng bấm `Collect` để lấy Sữa và Trứng. |
| **`F5`** | Lưu dữ liệu | Ghi file `farm_save.json`. |
| **`F9`** | Tải dữ liệu | Đọc file `farm_save.json` và khôi phục trạng thái. |

---

## 6. Cách kiểm tra Inventory & Cửa sổ Supplies
1. **Kiểm tra kho lưu trữ (InventorySystem)**:
   - Mặc định người chơi bắt đầu với: 10 hạt lúa mì, 8 hạt ngô, 6 hạt cà rốt, 10 hạt cỏ, 10 grain, 20 hay, 5 pellet.
   - Sức chứa kho tối đa: 200 đơn vị vật phẩm.
   - Khi kho đầy, thu hoạch sẽ báo "Storage Full!" và không làm tràn đồ.
2. **Kiểm tra liên kết với cửa sổ Supplies**:
   - Click vào nút **Supplies** màu xanh lá ở thanh công cụ dưới đáy màn hình.
   - Cửa sổ Supplies hiện lên chính giữa màn hình.
   - Quan sát số lượng của **Grain**, **Hay**, **Pellets**: Đọc trực tiếp từ kho thật.
   - Sau khi gặt Lúa mì (+3 Grain) hoặc Cỏ khô (+3 Hay), mở Supplies lên sẽ thấy các số lượng này tăng lên chính xác.
   - Bấm nút **X** ở góc trên panel: Panel Supplies đóng lại nhẹ nhàng.
   - Nút **Supplies** ở dưới đáy màn hình vẫn hiển thị nguyên vẹn để người chơi mở lại bất cứ lúc nào, không bị mất và không chặn click ra ngoài.

---

## 7. Cách kiểm tra Điểm kinh nghiệm (XP) & Thăng cấp (Level)
1. **HUD Tiến trình**:
   - Nằm ở góc trên bên trái màn hình Game view.
   - Hiển thị cấp độ hiện tại `LVL 1`, thanh trượt đo XP màu vàng cam, và text `XP: current / next`.
2. **Cơ chế cộng điểm**:
   - Gieo hạt cây: **+2 XP**
   - Thu hoạch Wheat: **+10 XP**
   - Thu hoạch Corn: **+12 XP**
   - Thu hoạch Carrot: **+8 XP**
   - Thu hoạch Hay: **+10 XP**
   - Cho vật nuôi ăn: **+3 XP**
   - Thu hoạch Sữa/Trứng: **+15 XP**
3. **Thăng cấp (Level Up)**:
   - Bấm phím **`F3`** (cộng +100 XP):
   - Thanh XP đầy, cấp độ nhảy lên `LVL 2`, XP dư được bảo lưu sang cấp mới.
   - Hiển thị popup thông báo chúc mừng "LEVEL UP!" lớn ở giữa màn hình trong 3 giây.

---

## 8. Cách kiểm tra Chăm sóc Vật nuôi (Cows & Animals)
1. Di chuyển nhân vật lại gần các con Bò trong nông trại.
2. Click chuột trái vào con Bò:
   - Bảng thông tin vật nuôi (`AnimalUI`) mở ra.
   - Hiển thị chỉ số đói bụng (Hunger) và mức độ vui vẻ (Happiness).
3. Bấm nút **Feed**:
   - Trừ 1 Hay hoặc 1 Grain trong kho đồ.
   - Tăng độ no và vui vẻ của con vật.
   - Người chơi nhận ngay **+3 XP**.
4. Chờ đếm ngược hoặc nhấn phím **`F4`**:
   - Con Bò hoàn thành tạo sản phẩm `Milk` (Sữa tươi).
5. Bấm nút **Collect**:
   - Thêm Sữa vào Inventory.
   - Người chơi nhận ngay **+15 XP**.

---

## 9. Danh sách File Code đã tạo và sửa đổi
1. `Assets/Scripts/Farming/CropData.cs`: Dữ liệu 4 loại cây trồng (Wheat, Corn, Carrot, Hay) với đầy đủ thông số theo Phase 2.
2. `Assets/Scripts/Farming/CropPlot.cs`: Quản lý 6 ô đất, enum trạng thái `Empty, Planted, Growing, ReadyToHarvest`, các phương thức `Interact()`, `PlantCrop()`, `ForceMature()`, `Harvest()`, visual mầm/chín và nhãn 3D.
3. `Assets/Scripts/Farming/CropPlotUI.cs`: UI chọn giống gieo hạt, cơ chế cấp hạt khởi đầu khi test, popup toast thu hoạch.
4. `Assets/Scripts/Farming/CropManager.cs`: Tự động tạo và quản lý 6 ô đất `CropPlot_01..06` theo lưới 3x2 ngay trước mặt Player trên Terrain.
5. `Assets/Scripts/Farming/CropDebugController.cs`: Bộ điều khiển phím debug `F1, F2, F3, F4` phục vụ test Play Mode nhanh.
6. `Assets/Scripts/Scenes/Farm/FarmUI.cs`: Khởi tạo và kết nối toàn bộ hệ thống gameplay trong hàm `Start()`.
7. `Assets/Editor/FarmSceneSetup.cs`: Công cụ Editor hỗ trợ thiết lập và nướng trước 6 ô đất vào scene.
8. `Assets/Scripts/Inventory/InventorySystem.cs`: Hệ thống quản lý kho lưu trữ hạt giống, nông sản, thức ăn chăn nuôi.
9. `Assets/Scripts/Scenes/Farm/SuppliesUI.cs`: Cửa sổ Supplies kết nối dữ liệu thật từ kho, bảo toàn nút mở/đóng.
10. `Assets/Scripts/Progression/PlayerProgression.cs` & `ProgressionUI.cs`: Hệ thống cấp độ, kinh nghiệm và thanh HUD.
11. `Assets/Scripts/Animals/AnimalCareController.cs`, `AnimalManager.cs` & `AnimalUI.cs`: Quản lý chăm sóc bò, chuồng gà, cho ăn và thu hoạch sản phẩm.
12. `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`: Hệ thống lưu/đọc dữ liệu toàn diện định dạng JSON.
13. `Assets/Scripts/Player/FarmInteractionController.cs`: Xử lý click tương tác 3D với CropPlot và vật nuôi.
14. `Assets/Scripts/Player/ClickToMove.cs`: Ngăn chặn click di chuyển khi bấm vào ô đất hoặc cửa sổ UI.

---

## 10. Tình trạng Lỗi trong Console
- **Lỗi biên dịch (Compile Errors)**: **0 lỗi đỏ (Clean Build, ExitCode: 0)**.
- Toàn bộ các cảnh báo obsolete API cũ của Unity Standard Assets/Water đã được cách ly an toàn.
- Không có lỗi Tag "Terrain" gây gián đoạn runtime.
- Không xóa hay làm hỏng bất kỳ asset, scene, terrain hoặc prefab cũ nào của project.
