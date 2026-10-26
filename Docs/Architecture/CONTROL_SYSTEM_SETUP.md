# HƯỚNG DẪN CẤU HÌNH VÀ SỬ DỤNG HỆ THỐNG ĐIỀU KHIỂN (CONTROL SYSTEM)

Tài liệu này mô tả chi tiết hệ thống điều khiển nhân vật, camera và tương tác được thiết lập cho project Unity **GameNongTrai3D** (Scene: `Assets/Scenes/Farm.unity`).

---

## 1. Cách điều khiển nhân vật

### 1.1. Di chuyển bằng bàn phím (PC / Editor)
* **Các phím hỗ trợ:**
  * `W` hoặc `Mũi tên lên`: Di chuyển tiến về phía trước (theo hướng camera).
  * `S` hoặc `Mũi tên xuống`: Di chuyển lùi lại phía sau.
  * `A` hoặc `Mũi tên trái`: Di chuyển sang trái.
  * `D` hoặc `Mũi tên phải`: Di chuyển sang phải.
* **Tính năng:**
  * Hướng di chuyển luôn được tính toán tương đối theo góc quay hiện tại của camera.
  * Nhân vật tự động xoay mượt mà (`Slerp`) theo hướng di chuyển.
  * Tốc độ di chuyển tiêu chuẩn được nâng lên mức hợp lý (~4.5 m/s), không còn bị kẹt ở tốc độ cũ (0.1 m/s).
  * Tự động phát âm thanh bước chân khi di chuyển và dừng lại khi đứng yên.

### 1.2. Click chuột để di chuyển (Click-To-Move)
* **Thao tác:**
  * Nhấp chuột trái (`Left Click`) vào bất kỳ vị trí hợp lệ nào trên mặt đất (Terrain hoặc mặt sàn).
* **Tính năng:**
  * Hệ thống sử dụng Raycast từ `Main Camera` xuống `TerrainCollider`.
  * Hiển thị vòng tròn chỉ thị mục tiêu (`ClickMoveMarker`) tại vị trí đích.
  * Nhân vật tự động xoay và chạy đến vị trí đích, dừng lại khi cách đích trong phạm vi `stoppingDistance` (mặc định 0.6m).
  * **Chống xung đột:** Nếu người dùng nhấn bất kỳ phím `WASD`, phím mũi tên hoặc kéo joystick, mục tiêu click-to-move sẽ tự động bị hủy và nhường quyền điều khiển ngay lập tức cho thao tác thủ công.
  * **Bảo vệ UI:** Khi click vào các nút hoặc bảng UI, sự kiện click xuống mặt đất sẽ tự động bị chặn (`EventSystem.current.IsPointerOverGameObject()`).

### 1.3. Điều khiển bằng Virtual Joystick (Mobile / Giả lập chuột)
* **Vị trí:** Góc dưới bên trái màn hình (được neo chuẩn tại bottom-left trên UI Canvas chuẩn Landscape).
* **Thao tác:**
  * **Trên thiết bị Android / màn hình cảm ứng:** Chạm và kéo ngón tay trong vùng joystick.
  * **Trong Unity Editor:** Nhấp giữ chuột trái và kéo tay cầm (handle) joystick.
* **Tính năng:**
  * Tự động điều chỉnh góc xoay theo camera.
  * Tích hợp tự động khởi tạo (`MobileJoystick.EnsureJoystickInScene()`) nếu scene chưa có sẵn UI Joystick Canvas.
  * Đồng bộ ngược với `VCAnalogJoystickBase` để đảm bảo 100% tương thích ngược với các script cũ.

---

## 2. Điều khiển Camera

* **Theo dõi nhân vật (Follow Player):**
  * Camera tự động bám theo Player mượt mà theo khoảng cách `difVec`.
  * `followTarget` được bật mặc định ngay khi scene khởi chạy.
* **Xoay góc nhìn (Orbit Camera):**
  * Giữ **chuột phải (`Right Click`)** và di chuột sang trái/phải (`Mouse X`) để xoay camera xung quanh nhân vật.
* **Chống chìm xuống Terrain (Anti-Terrain Clipping):**
  * Hệ thống tự động kiểm tra độ cao địa hình `Terrain.activeTerrain.SampleHeight(position)`.
  * Đảm bảo tọa độ `Y` của camera luôn cao hơn mặt đất tối thiểu `minHeightAboveTerrain` (1.5m), không bao giờ bị chui xuống dưới lòng đất.

---

## 3. Tương tác với bò và vật thể (Interaction)

* Khi người chơi nhấp chuột trái hoặc chạm vào một con bò (`Cow`):
  * Hệ thống `FarmInteractionController` nhận diện collider của bò.
  * Gọi tương tác `OnMouseDown()` của `CowController`.
  * Camera chuyển sang góc nhìn tương tác với bò và mở bảng giao diện chăm sóc/thông tin bò (`FarmUI`).
  * Mục tiêu click-to-move trên mặt đất bị hủy bỏ để nhân vật không va chạm vào bò.

---

## 4. Các script đã tạo mới và cập nhật

| Tên Script | Đường dẫn | Trạng thái | Chức năng |
|---|---|---|---|
| `Movement.cs` | `Assets/Scripts/Player/Movement.cs` | Cập nhật | Hệ thống di chuyển chính: nhận input WASD, joystick, click-to-move, xử lý trọng lực, va chạm terrain, xoay nhân vật và animation. |
| `PlayerMovement.cs` | `Assets/Scripts/Player/PlayerMovement.cs` | Tạo mới | Alias component kế thừa `Movement`, hỗ trợ gắn trực tiếp vào Player. |
| `ClickToMove.cs` | `Assets/Scripts/Player/ClickToMove.cs` | Tạo mới | Raycast địa hình, điều hướng nhân vật đến điểm click chuột, quản lý marker hiển thị. |
| `MobileJoystick.cs` | `Assets/Scripts/Player/MobileJoystick.cs` | Tạo mới | Virtual joystick cảm ứng & chuột, chuẩn UI EventSystem, tự động sinh Canvas Landscape. |
| `CameraController.cs` | `Assets/Scripts/Player/CameraController.cs` | Cập nhật | Camera follow mượt mà, xoay chuột phải, kẹp cao độ chống xuyên terrain. |
| `CameraFollow.cs` | `Assets/Scripts/Player/CameraFollow.cs` | Tạo mới | Component alias kế thừa `CameraController` cho camera. |
| `FarmInteractionController.cs` | `Assets/Scripts/Player/FarmInteractionController.cs` | Tạo mới | Bắt sự kiện click vào bò và vật thể, ngăn xung đột click-to-move và UI. |
| `FarmSceneInitializer.cs` | `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs` | Tạo mới | Tự động kích hoạt khi load scene Farm, đảm bảo Player, Camera, Terrain, Joystick và EventSystem luôn đầy đủ component và liên kết. |

---

## 5. Các Component cần có trên GameObject Player

Để nhân vật hoạt động hoàn hảo, object `Player` trong scene `Assets/Scenes/Farm.unity` được cấu hình đầy đủ các component sau:

1. **`Transform`**: Tọa độ nhân vật (tự động điều chỉnh `Y` nằm trên mặt Terrain khi scene chạy).
2. **`CharacterController`**:
   * `Center`: `(0, 1, 0)`
   * `Radius`: `0.5`
   * `Height`: `2.0`
3. **`Animator`**:
   * `Controller`: `PlayerAnimatorController.controller`
   * Tham số điều khiển: float `"Speed"`
4. **`AudioSource`**:
   * Âm thanh bước chân `moveSound`
5. **`Movement`** (hoặc **`PlayerMovement`**):
   * Tốc độ `Speed`: ~`4.5`
   * `rotationDamping`: ~`15`
   * `gravity`: `20`
6. **`ClickToMove`**:
   * Hỗ trợ nhấp chuột di chuyển trên địa hình.
7. **`FarmInteractionController`**:
   * Hỗ trợ tương tác với động vật và vật thể.

---

## 6. Các vấn đề đã xử lý triệt để

1. **Nhân vật không di chuyển trước đây:** Do script cũ gọi `VCAnalogJoystickBase.GetInstance("stick")` bị `NullReferenceException` khi thiếu joystick, làm đứt vòng lặp `Update()`. Đã được sửa an toàn và bổ sung nhận diện đa luồng input (WASD + Joystick + Click).
2. **Camera không theo dõi:** Script cũ để `followTarget = false` mặc định và không gọi kích hoạt. Đã được kích hoạt mặc định cùng thuật toán lerp mượt mà và chống xuyên địa hình.
3. **Lỗi chuyển cảnh (`Application.LoadLevel`):** Đã nâng cấp toàn bộ sang `UnityEngine.SceneManagement.SceneManager.LoadScene()`.
4. **Không còn bất kỳ lỗi compile đỏ nào trong Unity 6 Console.**
