# TỔNG HỢP CÁC LỖI ĐÃ XỬ LÝ & LƯU Ý KỸ THUẬT (KNOWN ISSUES & FIX LOG)

Tài liệu ghi lại toàn bộ các vấn đề đã được khắc phục qua các Phase phát triển, các cảnh báo tương thích trên Unity 6 (6.0.6f1 / 6000.6.5f1) và các lưu ý kỹ thuật khi vận hành dự án **GameNongTrai3D**.

---

## 1. Các lỗi đã được giải quyết triệt để

### [Lỗi 1] Tag: Terrain is not defined
- **Hiện tượng**: Khi chạy Play Mode trong `Assets/Scenes/Farm.unity`, Console liên tục ném lỗi:
  ```text
  UnityException: Tag: Terrain is not defined.
  ```
- **Nguyên nhân**: `ClickToMove.cs` gọi hàm `CompareTag("Terrain")` trong khi trong `TagManager.asset` của project chưa từng định nghĩa Tag tên là `Terrain`. Trong Unity, gọi `CompareTag` với một tag không tồn tại trong danh mục sẽ ném exception nghiêm trọng làm dừng xử lý sự kiện chuột.
- **Cách khắc phục**:
  1. Thêm tag `"Terrain"` vào danh sách tag của project trong file `ProjectSettings/TagManager.asset`.
  2. Nâng cấp code trong `ClickToMove.cs` để nhận diện mặt đất bằng kiểm tra an toàn `hit.collider is TerrainCollider || hit.collider.gameObject.name.Contains("Terrain")` mà không phụ thuộc cứng vào Tag.

### [Lỗi 2] Cửa sổ Supplies và các nút UI không click / chạm được
- **Hiện tượng**:
  - Khi mở scene `Farm.unity`, nút Supplies và cửa sổ Supplies hiển thị nhưng bấm chuột hoặc chạm tay không có phản hồi.
  - Nút "X" đóng cửa sổ không hoạt động.
- **Nguyên nhân**:
  - Hệ thống cũ sử dụng `OnGUI()` (Legacy IMGUI `GUI.Window`), trong khi scene có các thành phần Canvas uGUI và EventSystem đè lên phía trước. Khi GraphicRaycaster của Canvas hoạt động, nó chặn các sự kiện chuột truyền xuống lớp IMGUI.
  - Nút X và các slot Grain, Hay, Pellets trong IMGUI không có hitbox chuẩn xác và bị xung đột với `ClickToMove`.
- **Cách khắc phục**:
  - Chuyển đổi toàn bộ giao diện Supplies sang hệ thống Unity UI (uGUI) hiện đại thông qua script `SuppliesUI.cs`.
  - Cấu hình Canvas riêng biệt với `GraphicRaycaster`, `CanvasScaler` chuẩn tỷ lệ 1920x1080 Landscape và `CanvasGroup` quản lý trạng thái hiển thị / bắt tia raycast (`interactable`, `blocksRaycasts`).
  - Khi đóng bảng, `canvasGroup.blocksRaycasts = false`, giải phóng hoàn toàn vùng bấm chuột cho nhân vật và joystick.

### [Lỗi 3] Bấm vào UI đồng thời làm nhân vật chạy xuống đất
- **Hiện tượng**: Khi nhấp vào các nút UI (Supplies, Storage, Chọn hạt giống, Chăm sóc vật nuôi), nhân vật vẫn vô tình nhận lệnh chạy tới vị trí mặt đất bên dưới nút bấm.
- **Cách khắc phục**:
  - Bổ sung kiểm tra `EventSystem.current.IsPointerOverGameObject()` cho chuột trong Editor và `EventSystem.current.IsPointerOverGameObject(touch.fingerId)` cho cảm ứng trên thiết bị di động trong cả `ClickToMove.cs` và `FarmInteractionController.cs`.
  - Hủy bỏ di chuyển đất khi click trúng vật nuôi (`AnimalCareController`) hoặc ô đất (`CropPlot`).

### [Lỗi 4] NullReferenceException khi chạy trực tiếp `Farm.unity`
- **Hiện tượng**: Chạy trực tiếp scene `Farm.unity` mà không qua `Main Menu` gây crash do `GameController.Instance()` hoặc `player` chưa được khởi tạo.
- **Cách khắc phục**:
  - Cập nhật `GameController.cs` để tự động khởi tạo mặc định đối tượng `Farmer` nếu chưa có dữ liệu nạp sẵn từ Menu, cho phép nhà phát triển Play Mode từ bất kỳ scene nào một cách an toàn.

### [Lỗi 5] API cũ bị lỗi thời trong Unity 6
- **Hiện tượng**:
  - `Application.loadedLevelName` bị cảnh báo lỗi thời.
  - `Object.FindFirstObjectByType<T>()` và `FindObjectsSortMode` bị cảnh báo deprecated trong Unity 6.
- **Cách khắc phục**:
  - Thay thế bằng `SceneManager.GetActiveScene().name`.
  - Thay thế các phương thức tìm kiếm bằng `FindAnyObjectByType<T>()` và `FindObjectsByType<T>()` chuẩn Unity 6.

---

## 2. Lưu ý kỹ thuật & Vấn đề còn lại nếu có (Known Limitations)

### 1. Thư viện Google Play Games Plugin cũ
- **Hiện trạng**: Các script trong thư mục `Assets/GooglePlayGames/` là phiên bản cũ dành cho Unity 5/2017. Khi chạy trong Editor, chúng in cảnh báo `Score submitted?: False` khi quit game do chưa cấu hình App ID Google Play thực tế.
- **Đánh giá**: Cảnh báo này là bình thường trong Unity Editor và không ảnh hưởng đến bất kỳ tính năng gameplay nào của Farm.

### 2. Định dạng file Scene Farm.unity
- **Hiện trạng**: File `Assets/Scenes/Farm.unity` nguyên bản là file nhị phân Unity v5.2.
- **Giải pháp**: Tất cả các thành phần gameplay mới (Player Controller, Camera Follow, Joystick, EventSystem, Cụm đất trồng CropManager, Chuồng gà ChickenCoop, Hệ thống kho StorageUI, Hệ thống tiến trình ProgressionUI, Hệ thống lưu game SaveLoadSystem) được điều phối thông qua **`FarmSceneInitializer.cs`** với thuộc tính `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`.
- **Lợi ích**: Không làm thay đổi cấu trúc nhị phân của scene cũ, không làm mất bất kỳ asset gốc nào, và đảm bảo mọi hệ thống luôn tự động khởi tạo hoàn hảo mỗi khi Play scene.

### 3. Phím tắt Debug thuận tiện cho Tester
- Khi chạy game trong Unity Editor, nhấn phím **`F`** trên bàn phím:
  - Tức thì làm chín toàn bộ cây trồng trên các ô đất.
  - Tức thì hoàn tất chu kỳ sản xuất sản phẩm của tất cả Bò và Chuồng gà.
  - Giúp kiểm tra vòng lặp thu hoạch, cộng kho và thăng cấp chỉ trong vài giây mà không cần chờ bộ đếm thời gian.
