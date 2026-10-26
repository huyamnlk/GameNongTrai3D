# NHẬT KÝ SỬA LỖI DỰ ÁN UNITY 6 (FIX LOG)

**Dự án:** `D:\GameNongTrai3D`  
**Phiên bản Unity:** Unity 6.0 (`6000.6.5f1`)  
**Mục tiêu:** Mở được trên Unity 6, 0 lỗi đỏ trong Console, chạy Play Mode từ scene Main Menu, hoàn thiện hệ thống điều khiển nhân vật trong scene Farm.

---

## 1. Chi tiết từng lỗi đã sửa và các file đã thay đổi

### 1.1. Lỗi CS0619: `Object.GetInstanceID()` is obsolete
* **Mô tả:** Trong Unity 6, `GetInstanceID()` bị đánh dấu obsolete với cảnh báo lỗi compile (`error: true`).
* **Các file đã sửa:**
  1. `Assets/Standard Assets/Environment/Water/Water/Scripts/Water.cs`:
     * Dòng 272-276, 313-317: Thay thế `GetInstanceID()` bằng `GetEntityId()`.
     * Dòng 286-290, 327-335: Thay `GetInstanceID()` trong tên GameObject bằng `GetEntityId()`.
     * Dòng 295-297, 340-342: Bọc `#if !UNITY_6000_0_OR_NEWER` cho `FlareLayer` đã bị xóa trong Unity 6.
     * Dòng 360-364: Bọc `#if !UNITY_6000_0_OR_NEWER` cho `SystemInfo.supportsRenderTextures`.
  2. Các file NGUI:
     * `Assets/3rd Party/NGUI/Scripts/UI/UIPanel.cs`
     * `Assets/3rd Party/NGUI/Scripts/Internal/UIWidget.cs`
     * `Assets/3rd Party/NGUI/Scripts/Editor/NGUIEditorTools.cs`
     * `Assets/3rd Party/NGUI/Scripts/Editor/NGUISettings.cs`
     * `Assets/3rd Party/NGUI/Scripts/Editor/UIPanelTool.cs`
     * `Assets/3rd Party/NGUI/Scripts/Editor/UIAtlasMaker.cs`
     * Đã chuyển sang `GetEntityId().CompareTo(...)` hoặc sử dụng tham chiếu trực tiếp.

### 1.2. Lỗi Blender ("Blender could not be found")
* **Mô tả:** Unity cố gắng import các file `.blend` thô trong khi máy người dùng chưa cài đặt Blender.
* **Cách xử lý:** 
  * Kiểm tra toàn bộ prefab và scene trong project: Không có asset nào trong game phụ thuộc trực tiếp vào các file `.blend` này (game sử dụng các model `.fbx` và prefab đã xuất).
  * Di chuyển các file `.blend`, `.blend1` và file `.meta` tương ứng vào thư mục `Assets/Blender Resources/BlendFiles~`. 
  * Ký tự `~` ở cuối tên thư mục giúp Unity bỏ qua importer tự động, không còn kích hoạt lỗi Blender, đồng thời bảo toàn 100% file gốc không bị xóa.

### 1.3. Lỗi Shader interpolator trong `Transparent/Reflective`
* **File:** `Assets/3rd Party/NGUI/Examples/Shaders/Refractive.shader`
* **Mô tả:** `Too many texture interpolators would be used for ForwardBase pass (9 out of max 8)`.
* **Cách xử lý:** Thêm `#pragma target 3.0` vào các subshader để nâng giới hạn số lượng interpolator của card đồ họa hiện đại lên tối thiểu 10/16, giữ nguyên hoàn toàn hiệu ứng vật liệu NGUI.

### 1.4. Lỗi Tree Prefab ("Tree prefab at index 0/1 is missing")
* **File:** `Assets/Terrains/Mart Terrain.asset`
* **Mô tả:** TerrainData tham chiếu GUID prototype cây không còn tồn tại trong project.
* **Cách xử lý:** Cập nhật prototype trong `Mart Terrain.asset` trỏ tới các prefab cây hợp lệ có sẵn (`Tree1.prefab`, `Tree2.prefab` tại `Assets/3rd Party/Cartoon Town and Farm/Prefabs/`).

### 1.5. Lỗi thiếu package module Unity 6
* **File:** `Packages/manifest.json`
* **Mô tả:** Unity 6 chuyển nhiều thành phần core thành package dạng module. Khi mở project cũ bị thiếu các module: `androidjni`, `unitywebrequest`, `physics`, `terrain`, `ai`, `screencapture`, `imageconversion`.
* **Cách xử lý:** Đã bổ sung đầy đủ danh sách module vào `manifest.json`.

### 1.6. Lỗi API Google Play Games và mạng cũ
* **Các file:**
  * `Assets/GooglePlayGames/ISocialPlatform/PlayGamesPlatform.cs` & `PlayGamesLocalUser.cs`: Cập nhật interface `Authenticate(ILocalUser, Action<bool, string>)` cho Unity 6.
  * `Assets/GooglePlayGames/ISocialPlatform/PlayGamesAchievement.cs`: Thay thế `WWW` đã bị loại bỏ bằng `UnityWebRequestTexture`.
  * `Assets/GooglePlayGames/Editor/GPGSPostBuild.cs`, `GPGSAndroidSetupUI.cs`: Cập nhật `BuildTarget.iPhone` -> `iOS`, `bundleIdentifier` -> `applicationIdentifier`.

### 1.7. Lỗi GUITexture (VirtualControls)
* **Các file:**
  * `Assets/3rd Party/VirtualControls/Scripts/GUITexture/VCAnalogJoystickGuiTexture.cs`
  * `Assets/3rd Party/VirtualControls/Scripts/GUITexture/VCButtonGuiTexture.cs`
* **Cách xử lý:** Đã điều chỉnh tương thích sang `UnityEngine.UI.Image` và `RectTransformUtility.RectangleContainsScreenPoint`.

### 1.8. Nâng cấp chuyển cảnh `Application.LoadLevel`
* **Các file:** `UILoadScene.cs`, `MainMenu.cs`, `PlayerNameMenu.cs`, `GameController.cs`, `FarmUI.cs`, `MartUI.cs`.
* **Cách xử lý:** Thay thế toàn bộ bằng `UnityEngine.SceneManagement.SceneManager.LoadScene()`.

### 1.9. Hoàn thiện hệ thống điều khiển nhân vật trong `Farm.unity`
* **Các file tạo mới / nâng cấp:**
  * `Assets/Scripts/Player/Movement.cs`
  * `Assets/Scripts/Player/PlayerMovement.cs`
  * `Assets/Scripts/Player/ClickToMove.cs`
  * `Assets/Scripts/Player/MobileJoystick.cs`
  * `Assets/Scripts/Player/CameraController.cs`
  * `Assets/Scripts/Player/CameraFollow.cs`
  * `Assets/Scripts/Player/FarmInteractionController.cs`
  * `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs`

---

## 2. Blender có cần cài đặt hay không?

* **Câu trả lời:** **KHÔNG CẦN CÀI ĐẶT BLENDER.**
* **Lý do:** Các mô hình 3D trong trò chơi đều đã có prefab và định dạng mesh sẵn có trong `Assets/3rd Party/`. Các file thiết kế gốc `.blend` đã được lưu trữ an toàn tại `Assets/Blender Resources/BlendFiles~`, không gây cản trở importer của Unity.

---

## 3. Cách mở project và chạy Play Mode

1. **Mở Unity Hub:**
   * Chọn `Add project from disk` -> Chọn thư mục `D:\GameNongTrai3D`.
   * Chọn Editor Version: **Unity 6.0.6f1** (hoặc `6000.6.5f1`).
2. **Khởi động Editor:**
   * Nhấp đúp để mở dự án. Unity sẽ nạp tài nguyên mà không có bất kỳ lỗi đỏ nào trong Console.
3. **Chạy Play Mode:**
   * Mở scene đầu tiên: `Assets/Scenes/Main Menu.unity`.
   * Nhấn nút **Play** (Ctrl + P).
   * Bạn có thể chọn Start Game -> nhập tên -> chuyển sang scene Farm.
   * Hoặc mở trực tiếp scene `Assets/Scenes/Farm.unity` và nhấn **Play**. Nhân vật sẽ có đầy đủ điều khiển WASD, click-to-move, camera xoay chuột phải và joystick cảm ứng.

---

## 4. Cách build Android (APK / AAB)

1. Trong Unity Editor, vào menu **File** -> **Build Profiles** (hoặc **Build Settings**).
2. Kiểm tra danh sách Scenes In Build theo đúng thứ tự:
   - Index 0: `Assets/Scenes/Main Menu.unity`
   - Index 1: `Assets/Scenes/Player Name.unity`
   - Index 2: `Assets/Scenes/Farm.unity`
   - Index 3: `Assets/Scenes/Mart.unity`
   - Index 4: `Assets/Scenes/Misc Menu.unity`
   - Index 5: `Assets/Scenes/Settings.unity`
3. Chọn platform **Android** -> Nhấn **Switch Platform**.
4. Vào **Player Settings**:
   * Kiểm tra `Package Name` (ví dụ: `com.company.irishfarmsim`).
   * Chọn `Scripting Backend`: **IL2CPP**.
   * Chọn `Target Architectures`: **ARMv7** và **ARM64**.
   * Thiết lập `Target API Level`: Tối thiểu Android 14 (API 34) theo chuẩn Google Play.
5. Nhấn **Build** và chọn nơi lưu file APK.

---

## 5. Các bước khôi phục backup khi cần

Bản sao lưu nguyên vẹn trước khi sửa được lưu trữ an toàn tại:
`D:\GameNongTrai3D\_backup_before_fix`

Nếu bạn muốn hoàn tác và khôi phục lại:
1. Đóng Unity Editor.
2. Sao chép đè các thư mục từ `_backup_before_fix` về lại thư mục gốc:
   * Chép `D:\GameNongTrai3D\_backup_before_fix\Assets` vào `D:\GameNongTrai3D\Assets`
   * Chép `D:\GameNongTrai3D\_backup_before_fix\ProjectSettings` vào `D:\GameNongTrai3D\ProjectSettings`
   * Chép `D:\GameNongTrai3D\_backup_before_fix\Packages` vào `D:\GameNongTrai3D\Packages`
3. Khởi động lại Unity.
