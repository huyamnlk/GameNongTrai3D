# BÁO CÁO HỆ THỐNG CHỈNH SỬA BỐ CỤC NÔNG TRẠI & TỐI ƯU ÁNH SÁNG TƯƠI SÁNG

**Dự án**: Unity Irish Farm Simulator (`D:\GameNongTrai3D`)  
**Ngày thực hiện**: 15/10/2026  
**Trạng thái**: Hoàn tất 100% - 0 Compiler Errors  

---

## 1. Cách Bật / Tắt Edit Mode (Chế Độ Chỉnh Sửa)

- **Cách bật**:
  - Người chơi bấm vào nút **"CHỈNH SỬA NÔNG TRẠI"** nằm trên thanh HUD đầu màn hình (ngay cạnh nút TẢI / LOAD).
  - Khi bật:
    - Giao diện chỉnh sửa xuất hiện gồm banner tiêu đề phía trên và nút **"XONG / HOÀN TẤT"** ở góc phải.
    - Cần điều khiển ảo (Mobile Joystick) được tạm ẩn để giải phóng không gian cảm ứng cho màn hình điện thoại và máy tính bảng.
    - Toàn bộ cơ chế Click-to-Move và mở menu tương tác (Shop, Animal Care, Storage, Crop Menu) được tạm khóa, đảm bảo thao tác chạm/kéo hoàn toàn tập trung vào việc bố trí nông trại.
- **Cách tắt**:
  - Bấm vào nút **"XONG / HOÀN TẤT"** ở góc trên bên phải hoặc phím **Escape** / Chuột phải khi không chọn vật thể.
  - Khi tắt:
    - Mọi outline/highlight tạm thời được xóa sạch.
    - Joystick ảo và quyền điều khiển di chuyển nhân vật được kích hoạt lại.
    - Toàn bộ bố cục mới được tự động lưu lại vào file Save (`farm_save.json`).

---

## 2. Danh Sách Đối Tượng Được Phép & Không Được Phép Di Chuyển

### Được phép di chuyển:
1. **Cow Barn (Chuồng bò)**: di chuyển chuồng và toàn bộ đàn bò bên trong.
2. **Chicken Coop (Chuồng gà)**: di chuyển chuồng và toàn bộ đàn gà bên trong.
3. **Các chuồng vật nuôi khác / Animal Pens**.
4. **Storage (Kho nông trại / Kho đồ)**.
5. **Các máy chế biến / Production Machines** (Máy thức ăn gia súc, máy đóng gói, lò nướng,...).
6. **Các ô đất trồng trọt (CropPlots)**: `CropPlot_01` đến `CropPlot_06`.
7. **Các vật phẩm trang trí đã mua**.

### Tuyệt đối không cho phép di chuyển:
- **Main Camera**: góc nhìn camera chính.
- **Player**: nhân vật nông dân.
- **Terrain**: địa hình mặt đất và đồi núi.
- **Environment & Boundary Walls**: cây cối môi trường tĩnh, tường rào biên cố định, nguồn nước.
- **EventSystem & UI Canvases**.
- **Con vật riêng lẻ**: không kéo lẻ từng con bò/gà ra khỏi chuồng; vật nuôi tự động bám theo chuồng của chúng.

---

## 3. Cách Di Chuyển Chuồng & FarmPlot

- **Chọn vật thể**:
  - Chạm hoặc click chuột trái vào công trình hoặc ô đất trồng.
  - Xuất hiện khung viền highlight thanh mảnh xung quanh chân vật thể (tự động xóa khi bỏ chọn).
  - Thanh điều khiển hiển thị tên vật thể được chọn và các nút chức năng:
    - **[XÓA CHỌN]**: Bỏ chọn vật thể.
    - **[XOAY ↺ 45°]**: Xoay ngược chiều kim đồng hồ 45 độ.
    - **[XOAY ↻ 45°]**: Xoay theo chiều kim đồng hồ 45 độ.
    - **[HỦY DI CHUYỂN]**: Trả vật thể ngay lập tức về vị trí ban đầu trước khi kéo.
- **Kéo & Thả (Drag & Drop)**:
  - Giữ chuột hoặc ngón tay và kéo trên màn hình.
  - Raycast chiếu trực tiếp xuống mặt đất Terrain (bỏ qua bản thân vật thể đang kéo để tránh giật hình).
  - Độ cao $Y$ được bám sát theo độ cao thực tế của Terrain (`Terrain.activeTerrain.SampleHeight`).
  - Đối với **CropPlot**: tích hợp cơ chế **Snap Grid 0.5m**, giúp người chơi dễ dàng xếp các ô đất thẳng hàng, ngay ngắn và đẹp mắt.
  - Khung viền đổi sang màu **Xanh lá** nếu vị trí hợp lệ và **Màu đỏ** nếu vị trí bị cản.
- **Kiểm tra hợp lệ khi thả**:
  - Không cho phép đặt chồng lên công trình khác.
  - Không cho phép đặt chồng lên ô đất trồng khác.
  - Không cho phép đặt ra ngoài phạm vi nông trại.
  - Không cho phép đặt trên tường rào hoặc mặt nước.
  - Nếu vị trí **không hợp lệ**: vật thể tự động quay về vị trí ban đầu và hiển thị thông báo hướng dẫn.
  - Nếu vị trí **hợp lệ**: cập nhật vị trí mới và lưu dữ liệu.

---

## 4. Xử Lý Vật Nuôi Khi Di Chuyển Chuồng

- Bò chỉ thuộc Cow Barn (tối đa 3 bò) và gà chỉ thuộc Chicken Coop (tối đa 5 gà).
- Khi di chuyển Cow Barn hoặc Chicken Coop:
  - Toàn bộ đàn bò/gà được di chuyển đồng bộ theo vector dời của chuồng (`NotifyPenMoved`).
  - Điểm neo di chuyển và vùng giới hạn (`AnimalPenBoundary`) được cập nhật theo vị trí mới.
  - Toàn bộ vật nuôi được ép nằm gọn trong hàng rào mới (`ClampPosition`) và snap sát mặt đất, ngăn ngừa tình trạng rơi khỏi chuồng hoặc lọt ra ngoài.
  - Không nhân bản, không làm mất và không spawn sai số lượng vật nuôi.

---

## 5. Di Chuyển FarmPlot (Ô Đất Trồng)

- Giữ nguyên 100% dữ liệu cây trồng:
  - ID ô đất (`CropPlot_01` ... `CropPlot_06`).
  - Trạng thái sinh trưởng (`Empty`, `Planted`, `Growing`, `ReadyToHarvest`).
  - Loại nông sản (`wheat`, `corn`, `carrot`, `hay`).
  - Bộ đếm thời gian sinh trưởng (`growthTimer`).
  - Model cây trồng và tỷ lệ lớn của cây bám theo ô đất khi di chuyển.
- Khóa tương tác thu hoạch / mở menu gieo hạt trong lúc đang ở Edit Mode để tránh thao tác nhầm.

---

## 6. Lưu / Tải Vị Trí Trong SaveLoadSystem

- Mở rộng cấu trúc dữ liệu trong `SaveData.cs`:
  - `PlotSaveData`: bổ sung `posX`, `posY`, `posZ`, `rotY`, `hasCustomPosition`.
  - `MachineSaveData`: bổ sung `posX`, `posY`, `posZ`, `rotY`, `hasCustomPosition`.
  - `BuildingSaveData`: lưu `posX`, `posY`, `posZ`, `rotY`, `isPurchased`.
- Khi Save: ghi nhận tọa độ thế giới chính xác của toàn bộ chuồng, máy móc và ô đất trồng.
- Khi Load: khôi phục nguyên vẹn vị trí/góc xoay đã lưu, tự động bám sát mặt đất Terrain, tương thích hoàn toàn với các file save cũ.

---

## 7. Các Thiết Lập Ánh Sáng & Màu Sắc Đã Thay Đổi (Tươi Sáng, Rõ Ràng)

File [FarmLightingOptimizer.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Scenes/Farm/FarmLightingOptimizer.cs) tự động cấu hình môi trường ban ngày rực rỡ:

1. **Directional Light (Mặt Trời)**:
   - Màu sắc: Vàng nắng nhẹ ban ngày `Color(1.0, 0.96, 0.91)`.
   - Cường độ (Intensity): Tăng lên **1.35** (sáng rõ, sinh động).
   - Góc chiếu: `(50°, -35°, 0°)` chiếu từ trên cao chếch phía trước, triệt tiêu hiện tượng mặt trước công trình và vật nuôi bị tối đen.
   - Bóng đổ (Shadows): `Soft Shadows` với độ đậm vừa phải `shadowStrength = 0.48` tạo cảm giác mềm mại, dễ chịu.
2. **Environment Ambient Lighting (Trilight)**:
   - Chế độ: `AmbientMode.Trilight`.
   - Màu bầu trời (Sky Color): Xanh lam trong trẻo `Color(0.72, 0.84, 0.98)`.
   - Màu đường chân trời (Equator Color): Xanh lá cỏ tự nhiên `Color(0.68, 0.76, 0.68)`.
   - Màu phản xạ mặt đất (Ground Color): Nâu ấm dịu `Color(0.50, 0.46, 0.42)`.
   - Cường độ ánh sáng môi trường: **1.28** giúp làm sáng các ngóc ngách tối tăm.
3. **Sương mù khí quyển (Atmospheric Fog)**:
   - Màu sắc: Xanh lam nhạt ban ngày `Color(0.80, 0.88, 0.98)`.
   - Mật độ: Rất nhẹ `0.0012` tạo chiều sâu không gian ở phía xa mà không làm đục mờ cảnh vật gần.
4. **Phông nền Camera**:
   - `Camera.main.backgroundColor`: Xanh da trời mùa hè `Color(0.54, 0.74, 0.96)`.

---

## 8. Danh Sách Các File Đã Chỉnh Sửa & Tạo Mới

| STT | Tệp tin | Loại | Mô tả thay đổi |
|---|---|---|---|
| 1 | `Assets/Scripts/Buildings/FarmEditModeController.cs` | Tạo mới | Controller trung tâm điều khiển chế độ chỉnh sửa bố cục, UI và tương tác chuột/touch |
| 2 | `Assets/Scripts/Buildings/MovableFarmObject.cs` | Tạo mới | Nhận diện đối tượng được phép di chuyển, lưu vị trí gốc, tạo highlight viền chọn |
| 3 | `Assets/Scripts/Buildings/FarmObjectPlacementValidator.cs` | Tạo mới | Kiểm tra tính hợp lệ về mặt đất, biên giới hạn, chống va chạm/chồng lấn công trình |
| 4 | `Assets/Scripts/Scenes/Farm/FarmLightingOptimizer.cs` | Tạo mới | Bộ tối ưu ánh sáng ban ngày ấm áp, tươi sáng, tăng ambient trilight và khử góc tối |
| 5 | `Assets/Scripts/SaveLoad/SaveData.cs` | Chỉnh sửa | Bổ sung tọa độ và góc xoay cho `PlotSaveData`, `MachineSaveData`, và `FarmLayoutSaveData` |
| 6 | `Assets/Scripts/SaveLoad/SaveLoadSystem.cs` | Chỉnh sửa | Lưu và khôi phục tọa độ của các ô đất trồng (`CropPlot`) và máy móc (`ProductionMachine`) |
| 7 | `Assets/Scripts/Animals/AnimalPenBoundary.cs` | Chỉnh sửa | Bổ sung `NotifyPenMoved` để di chuyển và ép đàn bò/gà nằm đúng trong chuồng mới |
| 8 | `Assets/Scripts/Progression/ProgressionUI.cs` | Chỉnh sửa | Thêm nút **"CHỈNH SỬA NÔNG TRẠI"** vào thanh menu trên cùng |
| 9 | `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs` | Chỉnh sửa | Khởi tạo tự động `FarmLightingOptimizer` và `FarmEditModeController` |
| 10 | `Assets/Scripts/Player/FarmInteractionController.cs` | Chỉnh sửa | Khóa tương tác thông thường khi đang ở chế độ chỉnh sửa |
| 11 | `Assets/Scripts/Player/ClickToMove.cs` | Chỉnh sửa | Khóa di chuyển nhân vật khi click/kéo trong chế độ chỉnh sửa |
| 12 | `Assets/Scripts/Player/Movement.cs` | Chỉnh sửa | Khóa bàn phím di chuyển nhân vật khi đang chỉnh sửa |
| 13 | `Assets/Scripts/Farming/CropPlot.cs` | Chỉnh sửa | Ngăn thu hoạch hoặc mở menu gieo hạt khi đang chỉnh sửa |

---

## 9. Tình Trạng Lỗi Còn Lại
- **Console Compiler Errors**: **0 lỗi**.
- Toàn bộ chức năng cũ (Shop, Kho đồ Storage, Bán nông sản, Trồng trọt, Chăm sóc bò/gà, Lưu/Tải game) hoạt động trơn tru 100%.
