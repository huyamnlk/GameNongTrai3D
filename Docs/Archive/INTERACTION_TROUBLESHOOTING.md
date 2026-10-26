# HƯỚNG DẪN XỬ LÝ LỖI VÀ VẬN HÀNH TƯƠNG TÁC GAMEPLAY (INTERACTION TROUBLESHOOTING)

Tài liệu này ghi lại chi tiết các giải pháp kỹ thuật, cấu hình chuẩn và quy trình kiểm thử hệ thống tương tác và điều khiển cho dự án **Irish Farm Sim** (Unity 6 / Scene `Assets/Scenes/Farm.unity`).

---

## 1. CÁCH CHẠY PLAY MODE

1. **Mở Unity Editor** (Unity 6.0.6f1 / 6000.6.5f1 trở lên).
2. Trong cửa sổ **Project**, điều hướng đến: `Assets/Scenes/Farm.unity`.
3. Nhấp đúp chuột để mở scene `Farm.unity`.
4. Nhấn nút **Play** (▶) ở thanh công cụ phía trên giữa màn hình.
5. Sau khi vào Play Mode:
   - Hệ thống tự động kích hoạt `FarmSceneInitializer`.
   - Player và hệ thống Camera góc nhìn thứ 3 (Isometric/Third Person) được đồng bộ.
   - Giao diện `MobileControlsCanvas` cùng Joystick ảo xuất hiện ở góc dưới bên trái màn hình.
   - Giao diện HUD cấp độ, kinh nghiệm và thanh công cụ nông trại hiển thị ở các góc màn hình.

---

## 2. CÁCH SỬ DỤNG WASD / BÀN PHÍM (DESKTOP)

- **Di chuyển:**
  - `W` hoặc `Mũi tên lên (↑)`: Tiến về phía trước theo hướng camera.
  - `S` hoặc `Mũi tên xuống (↓)`: Lùi về phía sau theo hướng camera.
  - `A` hoặc `Mũi tên trái (←)`: Di chuyển sang trái.
  - `D` hoặc `Mũi tên phải (→)`: Di chuyển sang phải.
- Nhân vật sẽ tự động xoay mặt theo hướng di chuyển và kích hoạt animation đi bộ/chạy mượt mà.
- Khi sử dụng bàn phím, nếu có lệnh Click-to-Move trước đó, lệnh click sẽ tự động được hủy để ưu tiên điều khiển bàn phím tức thời.

---

## 3. CÁCH DÙNG JOYSTICK (MOBILE & GIẢ LẬP TRÊN PC)

- **Vị trí:** Cụm Joystick nằm ở góc dưới bên trái màn hình (`MobileControlsCanvas/MobileJoystick`).
- **Thao tác trên PC/Editor:**
  - Nhấn giữ **chuột trái** vào vòng tròn điều khiển (Joystick Knob/Background) và kéo theo hướng mong muốn.
  - Nhân vật lập tức di chuyển với vận tốc tỉ lệ thuận theo độ kéo của núm điều khiển.
  - Thả chuột trái để núm joystick tự động bật về tâm (Reset to Center) và nhân vật dừng lại.
- **Thao tác trên Android/Cảm ứng:**
  - Chạm 1 ngón tay vào vùng Joystick và trượt (Touch Drag).
  - Hỗ trợ đa chạm (Multi-touch): ngón tay trái điều khiển joystick, ngón tay phải có thể đồng thời bấm các nút UI hoặc thu hoạch.

---

## 4. CÁCH CLICK TƯƠNG TÁC GAMEPLAY

### A. Click di chuyển tới điểm chỉ định (Click-to-Move)
- Click chuột trái vào bất kỳ vị trí hợp lệ nào trên mặt đất (**Main Terrain**).
- Script `ClickToMove` sẽ bắn tia Raycast xuyên suốt từ Camera xuống Terrain, xác định tọa độ và điều khiển nhân vật chạy đến đích.
- Hệ thống tự động bỏ qua Collider của bản thân nhân vật Player để không bị kẹt hay giật lag.

### B. Tương tác với Bò (Cows) & Động vật
- Di chuyển nhân vật lại gần đàn bò hoặc click trực tiếp vào con bò.
- Tia Raycast phát hiện component `Cow` / `Animal`:
  - Mở bảng thông tin trạng thái vật nuôi (`AnimalUI`): mức độ đói/no, thời gian sẵn sàng vắt sữa, tâm trạng.
  - Cho phép cho ăn thức ăn tương ứng hoặc thu hoạch sữa khi đến chu kỳ.

### C. Tương tác với Ô Đất Trồng Trọt (Crop Plots)
- Click vào các luống đất để:
  - Chọn hạt giống từ kho.
  - Tưới nước chăm sóc cây trồng.
  - Thu hoạch nông sản khi chín và nhận điểm kinh nghiệm (XP Popup).

### D. Bảng UI Thức Ăn (Supplies)
- Click vào nút **Supplies** trên thanh HUD:
  - Bảng chọn thức ăn mở ra hiển thị các loại: **Grain (Ngũ cốc)**, **Hay (Cỏ khô)**, **Pellets (Thức ăn viên)**.
- Click chọn loại thức ăn để cho bò/gia súc ăn.
- Click vào nút **X** (hoặc click ra ngoài vùng panel nếu được cấu hình) để đóng bảng.
- Khi bảng Supplies đóng, toàn bộ vùng màn hình phía sau được giải phóng raycast, không làm cản trở joystick hay thao tác di chuyển.

---

## 5. CÁC COMPONENT BẮT BUỘC TRÊN PLAYER

Để nhân vật vận hành đầy đủ mọi tính năng, GameObject `Player` bắt buộc phải có các component sau:

1. **`CharacterController`**:
   - `Center`: `(0, 1, 0)`
   - `Radius`: `0.5`
   - `Height`: `2.0`
   - `Slope Limit`: `60` độ
   - `Step Offset`: `0.5`
2. **`Movement` (`IrishFarmSim.Movement`)**:
   - `Speed`: `6` – `8`
   - Tham chiếu tự động liên kết tới `CharacterController`, `Camera.main`, `MobileJoystick`, `ClickToMove`, `FarmInteractionController`.
3. **`MobileJoystick` (`IrishFarmSim.MobileJoystick`)**:
   - Tự động sinh `MobileControlsCanvas` chuẩn nếu trong scene chưa có.
4. **`ClickToMove` (`IrishFarmSim.ClickToMove`)**:
   - Quản lý logic di chuyển theo click chuột và raycast xuống mặt đất.
5. **`FarmInteractionController` (`IrishFarmSim.FarmInteractionController`)**:
   - Quản lý nhận diện tương tác với bò, vật phẩm, nông trại qua raycast 1000m.
6. **`Rigidbody`**:
   - `Is Kinematic`: **Được bật (true)** để tránh xung đột vật lý với `CharacterController`.
   - `Use Gravity`: **Tắt (false)** (trọng lực do `CharacterController` xử lý).
7. **`Collider` phụ (nếu có)**:
   - Phải được đặt `Is Trigger = true` để không tự chặn tia Raycast của chính nhân vật.

---

## 6. CÁC COMPONENT BẮT BUỘC TRÊN CANVAS

### A. MobileControlsCanvas (Joystick UI)
- **`Canvas`**:
  - `Render Mode`: `Screen Space - Overlay`
  - `Sorting Order`: `10`
- **`Canvas Scaler`**:
  - `UI Scale Mode`: `Scale With Screen Size`
  - `Reference Resolution`: `1920 x 1080`
  - `Match`: `0.5` (Width/Height)
- **`GraphicRaycaster`**:
  - Bắt buộc phải có để nhận sự kiện PointerDown/PointerUp/Drag.
- **`CanvasGroup`**:
  - `Alpha`: `1.0`
  - `Interactable`: `true`
  - `Blocks Raycasts`: `true`

### B. EventSystem trong Scene
- Trong toàn bộ scene **chỉ được phép tồn tại đúng 1 `EventSystem` duy nhất**.
- Component đính kèm:
  - `EventSystem`
  - `StandaloneInputModule` (hoặc `InputSystemUIInputModule` nếu chuyển hẳn sang New Input System).
- Nghiêm cấm đặt hai module cùng lúc để tránh vô hiệu hóa chu kỳ xử lý sự kiện UI.

### C. Canvas HUD / Supplies / ProgressionUI
- Tất cả các Image nền bán trong suốt hoặc panel thông báo pop-up (`FloatingXPPopup`, `LevelUpBanner`) phải đặt `raycastTarget = false` nếu chúng không có tính năng click, nhằm tránh che khuất tương tác bên dưới màn hình.

---

## 7. CÁC LỖI ĐÃ ĐƯỢC XỬ LÝ TRIỆT ĐỂ

| STT | Tên lỗi & Hiện trạng trước khi sửa | Nguyên nhân kỹ thuật | Giải pháp đã thực hiện |
|:---:|:---|:---|:---|
| 1 | **Hơn 999 lỗi `NullReferenceException` tại `VCTouchController.cs:169`** | Khi nhấn/kéo chuột trong Editor, `VCTouchController` tìm kiếm touch wrapper với finger ID giả lập. Do thiếu bước bắt `GetMouseButtonDown(0)`, biến `emulatedTw` bị `null` và gây Exception liên tục mỗi frame. Exception này bẻ gãy Event Loop của Unity, làm đơ toàn bộ UI và Raycast. | Bổ sung cơ chế null-safe touch slot allocation tự động và bắt sự kiện `Input.GetMouseButtonUp(0)` an toàn trong `VCTouchController.cs`. |
| 2 | **Nhân vật không di chuyển khi kéo Joystick** | Joystick prefab cũ bị mất GraphicRaycaster hoặc RectTransform nằm lệch viewport, đồng thời không có fallback tracking chuột khi chạy Editor. | Nâng cấp `MobileJoystick.cs`: tích hợp `CanvasGroup`, tự động đảm bảo `GraphicRaycaster`, bổ sung bộ lắng nghe tọa độ chuột/touch trực tiếp trong `Update()`. |
| 3 | **Click chuột xuống đất bị giật hoặc không ăn** | Tia Raycast của `ClickToMove` bị cản lại ngay lập tức bởi chính `CharacterController`/`Collider` của Player khi camera chiếu từ trên cao xuống. | Chuyển sang `Physics.RaycastAll`, lọc bỏ tất cả collider thuộc root transform của Player và lấy điểm chạm đầu tiên trên mặt đất. |
| 4 | **Click vào Bò / Vật nuôi không phản hồi** | Raycast tương tác bị giới hạn khoảng cách quá ngắn (`50f`). Khi camera đặt ở góc cao và xa, khoảng cách từ camera tới vật nuôi vượt quá 50m. | Mở rộng phạm vi raycast lên `1000f`, áp dụng layer mask chuẩn và hỗ trợ nhận diện các component `Cow`, `Animal`, `CropPlot`. |
| 5 | **Lỗi `Tag: Terrain is not defined`** | Main Terrain trong scene thiếu tag hoặc chưa gán TerrainCollider chuẩn. | Tự động kiểm tra và gán Tag `Terrain` cho `Main Terrain` trong `FarmSceneInitializer.cs`. |
| 6 | **Xung đột EventSystem / Đơ nút bấm** | Scene bị trùng lặp nhiều EventSystem từ các prefab NGUI và UGUI cũ. | `FarmSceneInitializer` tự động quét, loại bỏ các EventSystem dư thừa và bảo toàn duy nhất 1 `EventSystem` chuẩn với `StandaloneInputModule`. |
| 7 | **HUD kinh nghiệm chặn click màn hình** | Các banner kinh nghiệm và cấp độ trong `ProgressionUI` vô tình bật `raycastTarget = true`, làm thành bức tường vô hình cản click. | Tắt toàn bộ `raycastTarget` trên các thành phần đồ họa thụ động của `ProgressionUI`. |

---

## 8. CÁC CẢNH BÁO / LƯU Ý CÒN LẠI (KHÔNG ẢNH HƯỞNG GAMEPLAY)

1. **GooglePlayGames SDK warning khi đóng Play Mode:**
   - Khi dừng Play Mode, `GPGController` gọi lưu game và báo điểm lên Social Platform giả lập. Log này là luồng thông báo bình thường của plugin, không gây lỗi khi đang chơi.
2. **Cảnh báo Serialized Font NGUI cũ:**
   - Một số material font của NGUI có version cũ trước 2019. Unity tự động load tương thích mà không gây crash hay compile error.

---

## 9. QUY TRÌNH KIỂM THỬ XÁC NHẬN (ACCEPTANCE TEST)

1. **Test WASD:** Nhấn các phím `W`, `A`, `S`, `D` -> Player di chuyển mượt mà, xoay đúng hướng, không lỗi Console.
2. **Test Joystick:** Dùng chuột kéo núm Joystick ở góc trái màn hình -> Player di chuyển tương ứng, thả chuột núm tự về tâm.
3. **Test Click mặt đất:** Click vào các điểm bất kỳ trên Terrain -> Player tự động chạy đến vị trí click.
4. **Test Click Bò:** Click vào con bò -> Hệ thống nhận diện tương tác, hiển thị trạng thái vật nuôi.
5. **Test UI Supplies:** Click nút Supplies -> Mở danh sách thức ăn; click nút `X` -> Đóng bảng mượt mà; không bị hiểu nhầm click xuyên qua đất.
6. **Kiểm tra Console:** Không phát sinh bất kỳ lỗi đỏ (`LogType.Error` hoặc `Exception`) nào trong suốt quá trình chơi.
