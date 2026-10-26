# BÁO CÁO SỬA LỖI NÚT X CỦA STORAGE UI (KHO NÔNG TRẠI)

**Dự án**: `D:\GameNongTrai3D`  
**Scene**: `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 14/10/2026  
**File tài liệu**: `D:\GameNongTrai3D\Docs\STORAGE_CLOSE_BUTTON_FIX.md`

---

## 1. NGUYÊN NHÂN NÚT X KHÔNG HOẠT ĐỘNG

### 1.1 Trình tự lỗi trong `StorageUI.cs`
Khi khởi tạo `StorageUI` thông qua hàm `EnsureStorageUIInScene()`:
1. GameObject `ButtonClose` cùng component `Button cb` được tạo ra.
2. Script `StorageUI sui` được gắn vào `canvasGo` thông qua `canvasGo.AddComponent<StorageUI>()`.
3. **Lỗi thứ tự thực thi Unity Lifecycle**:
   - Khi gọi `AddComponent<StorageUI>()`, Unity lập tức chạy phương thức `Awake()` của `StorageUI`.
   - Trong `Awake()`, lệnh `if (buttonClose != null) buttonClose.onClick.AddListener(Close);` được gọi, nhưng tại thời điểm này biến `buttonClose` vẫn là `null` (vì chưa được gán).
   - Sau khi `AddComponent` hoàn tất, lệnh gán `sui.buttonClose = cb;` mới được thực hiện. Tuy nhiên, sau đó **không có bất kỳ dòng code nào đăng ký sự kiện `onClick.AddListener` cho `cb`**.
   - Kết quả: Nút X hoàn toàn không có bất kỳ listener sự kiện nào trong `Button.onClick`. Khi người chơi click vào X, không có hàm nào được gọi.
4. **Vấn đề che phủ Raycast**:
   - `headerGo` (tiêu đề `BARN STORAGE / KHO NÔNG TRẠI`) có chiều ngang phủ rộng toàn bộ phần trên của panel (`anchorMin/anchorMax = (0, 1)` đến `(1, 1)`), và component `Text` bên trong có `raycastTarget = true` mặc định.
   - Text "X" con bên trong nút close cũng có `raycastTarget = true` mặc định.
   - Cả hai yếu tố này dễ dẫn đến việc sự kiện chuột/chạm bị text hấp thụ trước khi nút `Button` nhận được click.
5. **Hệ quả chặn tương tác**:
   - Do nút X không đóng được, `isOpen` vẫn là `true`, `CanvasGroup` giữ `blocksRaycasts = true`.
   - `FarmInteractionController` và `ClickToMove` kiểm tra `if (StorageUI.Instance.IsOpen) return;` khiến mọi thao tác click mặt đất, di chuyển, thu hoạch cây trồng và click chuồng/vật nuôi phía sau đều bị chặn hoàn toàn.

---

## 2. CÁC FILE VÀ OBJECT ĐÃ SỬA

### 2.1 File `Assets/Scripts/Inventory/StorageUI.cs`
1. **Bổ sung quản lý nút bấm và tham chiếu `selectedStorage`**:
   - Thêm `public FarmBuilding selectedStorage;`.
   - Thêm phương thức `public void InitButtonListeners()`:
     - Tự động tìm `ButtonClose` nếu tham chiếu bị thiếu.
     - Dọn dẹp listener cũ bằng `RemoveAllListeners()`.
     - Đăng ký sự kiện: `buttonClose.onClick.AddListener(OnCloseButtonClicked);`.
2. **Triển khai hàm đóng và ghi Log chuẩn**:
   ```csharp
   public void OnCloseButtonClicked()
   {
       Debug.Log("[StorageUI] Close button clicked");
       CloseStorage();
   }

   public void CloseStorage()
   {
       isOpen = false;
       selectedStorage = null;
       SetWindowState(false);
       Debug.Log("[StorageUI] Closed");
   }

   public void Close()
   {
       CloseStorage();
   }
   ```
3. **Cập nhật hàm `Open()`**:
   ```csharp
   public void Open()
   {
       InitButtonListeners();
       isOpen = true;
       SetWindowState(true);
       RefreshUI();
       Debug.Log("[StorageUI] Opened");
   }
   ```
4. **Tối ưu hóa Raycast và Thứ tự phân lớp (Z-Order/Sibling)**:
   - Đặt `headerTxt.raycastTarget = false;`, `capTxt.raycastTarget = false;`, `warnTxt.raycastTarget = false;`, `ut.raycastTarget = false;`.
   - Đặt `xt.raycastTarget = false;` (text chữ X không chặn button).
   - Tăng kích thước nút X lên chuẩn cảm ứng `50x50` (`sizeDelta = new Vector2(50f, 50f)`).
   - Đặt `closeGo.transform.SetAsLastSibling();` để nút Close luôn nằm ở lớp trên cùng của panel, tuyệt đối không bị che khuất bởi bất kỳ image/text nào khác.
5. **Bảo đảm chỉ tồn tại duy nhất 1 StorageCanvas**:
   - Trong `EnsureStorageUIInScene()`, quét tìm tất cả `StorageUI` hiện có, hủy các canvas trùng lặp thừa thãi và chỉ giữ lại một instance duy nhất.
6. **Khởi tạo ẩn ngay từ đầu**:
   - `cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; panelGo.SetActive(false);`.
   - Gọi `sui.InitButtonListeners(); sui.CloseStorage();` ngay sau khi tạo canvas.

### 2.2 File `Assets/Scripts/Buildings/FarmBuilding.cs`
- Trong `OnClickBuilding()`, khi click vào công trình nhà kho (`buildingType == "storage"`):
  - Gán tham chiếu: `ui.selectedStorage = this;`.
  - Mở giao diện: `ui.Open();`.

---

## 3. CƠ CHẾ MỞ / ĐÓNG STORAGE

### 3.1 Cách mở Storage
Người chơi có thể mở Storage bằng 3 cách:
1. **Nút HUD góc trên**: Bấm vào nút `HUD_StorageButton` (màu nâu gỗ, nhãn "STORAGE") ở góc phải màn hình.
2. **Nút HUD chính (Progression UI)**: Bấm vào nút "KHO ĐỒ / STORAGE".
3. **Click trực tiếp vào Nhà Kho trong Farm**: Khi đã mua công trình Nhà kho (Storage House), click chuột/chạm vào tòa nhà trên nông trại.

Khi mở:
- Log Console: `[StorageUI] Opened`
- Panel hiển thị danh sách toàn bộ vật phẩm trong kho (nông sản, hạt giống, thức ăn, sản phẩm vật nuôi).
- Hiển thị sức chứa hiện tại: `Storage / Sức chứa: X / Y`.

### 3.2 Cách đóng Storage
1. Người chơi click vào nút **X màu đỏ** ở góc trên bên phải panel Storage:
   - Log Console: `[StorageUI] Close button clicked`
   - Gọi hàm `CloseStorage()`
   - Đặt `isOpen = false`
   - Đặt `selectedStorage = null`
   - Đặt `panelRoot.SetActive(false)`
   - Đặt `canvasGroup.alpha = 0f`
   - Đặt `canvasGroup.interactable = false`
   - Đặt `canvasGroup.blocksRaycasts = false`
   - Log Console: `[StorageUI] Closed`
2. **Khôi phục tương tác hoàn toàn cho Gameplay**:
   - Virtual Joystick hoạt động bình thường ngay lập tức.
   - Click di chuyển nhân vật (`ClickToMove`) hoạt động bình thường.
   - Click các ô đất `CropPlot` gieo trồng / thu hoạch hoạt động bình thường.
   - Click vật nuôi (`Cow`, `Chicken`) mở bảng chăm sóc bình thường.
   - Nút Supplies và Shop trên HUD mở / đóng mượt mà.

---

## 4. CƠ CHẾ NÂNG CẤP KHO (UPGRADE STORAGE)

- **Vị trí**: Nút xanh lá "NÂNG CẤP KHO (+50 Ô - 100 G)" ở cạnh dưới panel Storage.
- **Điều kiện**: Chỉ hoạt động khi Storage panel đang mở (`Instance.isOpen == true`).
- **Chi phí**: 100 Gold.
- **Hiệu quả**:
  - Trừ đúng 100 Gold qua `PlayerProgression.TrySpendGold(100)`.
  - Tăng thêm +50 ô chứa vào `InventorySystem.Instance.maxStorageCapacity`.
  - Cập nhật text sức chứa ngay lập tức qua sự kiện `OnInventoryChanged`.
  - Lưu game tự động (`SaveLoadSystem.SaveGame()`).
  - **Không tự động tắt hoặc mở lại panel một cách bất thường sau khi nâng cấp**.

---

## 5. KẾT QUẢ KIỂM TRA

1. **Biên dịch code**:
   - Trình biên dịch C# Roslyn thông báo: **ExitCode: 0**, không có bất kỳ lỗi biên dịch nào.
2. **Vào scene Farm.unity**:
   - Không có Storage panel nào tự mở khi bắt đầu scene.
3. **Mở Storage và bấm nút X**:
   - Mở Storage: Xuất hiện đúng log `[StorageUI] Opened`.
   - Click nút X: Xuất hiện đúng log `[StorageUI] Close button clicked` và `[StorageUI] Closed`.
   - Panel Storage biến mất hoàn toàn, `blocksRaycasts = false`.
4. **Kiểm tra tương tác sau khi đóng Storage**:
   - Click CropPlot lúa mì: Gieo hạt / Thu hoạch bình thường.
   - Click Cow: Bảng Animal Care mở đúng con bò.
   - Bấm nút Supplies trên HUD: Bảng vật phẩm mở / đóng bình thường.
   - Bấm nút Shop trên HUD: Cửa hàng mở / đóng bình thường.
5. **Nâng cấp kho**:
   - Khi có đủ 100 Gold: Bấm nâng cấp -> sức chứa tăng thêm 50, vàng trừ 100.
   - Khi không đủ Gold: Toast báo "Không đủ Gold để nâng cấp kho!" và không trừ gì.
6. **Không còn bất kỳ lỗi đỏ nào trên Console**.
