# BÁO CÁO SỬA LỖI ANIMAL CARE UI TỰ MỞ VÀ CHẶN THAO TÁC

**Dự án**: `D:\GameNongTrai3D`  
**Scene**: `Assets/Scenes/Farm.unity`  
**Ngày thực hiện**: 14/10/2026  
**File tài liệu**: `D:\GameNongTrai3D\Docs\ANIMAL_CARE_UI_FIX.md`

---

## 1. NGUYÊN NHÂN ANIMAL CARE UI TỰ MỞ KHI VÀO FARM (PHASE 1)

### 1.1 Trình tự gây lỗi
Trong `FarmSceneInitializer.cs` và `FarmUI.cs`, khi bắt đầu scene `Farm.unity`, hệ thống gọi hàm tiện ích:
```csharp
AnimalUI.EnsureUI();
```
Bên trong `AnimalUI.EnsureUI()`, quy trình khởi tạo xảy ra như sau:
1. Tạo GameObject `AnimalCarePanel` và gắn `CanvasGroup cg`:
   - Mặc định một GameObject mới tạo có `activeSelf = true`.
   - `CanvasGroup` mới tạo có `alpha = 1.0f`, `interactable = true`, `blocksRaycasts = true`.
2. Gọi `canvasGo.AddComponent<AnimalUI>()`:
   - Ngay lập tức Unity thực thi phương thức `Awake()` của component `AnimalUI`.
   - Tại thời điểm `Awake()` chạy, các biến tham chiếu `panelRoot`, `canvasGroup`, `buttonClose` đều đang có giá trị `null` (vì chưa được gán).
   - Do đó, lệnh `SetWindowState(false)` và đăng ký sự kiện cho `buttonClose` trong `Awake()` hoàn toàn bị bỏ qua.
3. Sau khi `AddComponent<AnimalUI>()` kết thúc, các trường mới được gán giá trị, nhưng không có lệnh đóng panel hoặc đăng ký sự kiện nào được gọi lại.
4. **Hệ quả**: Panel Animal Care xuất hiện ở trạng thái mở ngay từ frame đầu tiên với `sortingOrder = 160`, chiếm trọn vùng giữa màn hình và chặn toàn bộ raycast/thao tác chuột và cảm ứng đối với thế giới 3D và các nút UI phía sau.

### 1.2 Giải pháp khắc phục
1. **Khởi tạo ẩn ngay lập tức**:
   - Khi tạo `panel`, đặt `panel.SetActive(false)`.
   - Khi tạo `cg`, đặt `cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;`.
2. **Kích hoạt đóng và gắn sự kiện hoàn chỉnh trong `EnsureUI()`**:
   ```csharp
   Instance = ui;
   ui.SetupActionButtons();
   if (ui.buttonClose != null)
   {
       ui.buttonClose.onClick.RemoveAllListeners();
       ui.buttonClose.onClick.AddListener(ui.Close);
   }
   ui.Close();
   ```
3. **Thêm lớp bảo vệ trong `Start()`**:
   - Bổ sung `void Start() { SetWindowState(false); }` để đảm bảo dù scene reload hoặc có bất kỳ script nào can thiệp, Animal Care UI luôn bắt đầu ở trạng thái đóng hoàn toàn.
4. **Không tự mở từ bất kỳ hàm khởi tạo nào**:
   - Xác nhận Animal Care UI chỉ được mở thông qua:
     `FarmInteractionController` khi người chơi click/chạm vào một con vật (`AnimalCareController`) hoặc chuồng trại.
5. **Định dạng Log xuất ra chuẩn xác**:
   - Khi mở: `[AnimalCareUI] Opened for animal: {animalName}`
   - Khi đóng:
     ```text
     [AnimalCareUI] Closed
     [AnimalCareUI] Selected animal cleared
     ```

---

## 2. SỬA NÚT X VÀ KHẮC PHỤC CHẶN INPUT (PHASE 2)

### 2.1 Nguyên nhân nút X không đóng được
1. Sự kiện `buttonClose.onClick.AddListener(Close)` trước đây nằm trong `Awake()`, nhưng lúc đó `buttonClose` là `null` nên listener không được đăng ký.
2. Nút X có text con mang component `Text` với `raycastTarget = true` mặc định, đôi khi nuốt mất sự kiện click trước khi tới `Button`.
3. Kích thước nút X nhỏ (42x42), khó bấm chính xác trên màn hình cảm ứng Android.

### 2.2 Giải pháp khắc phục
1. Tăng kích thước vùng bấm lên chuẩn `50x50` (`sizeDelta = new Vector2(50f, 50f)`).
2. Đặt `xt.raycastTarget = false;` để text không nuốt raycast, toàn bộ tương tác chuột/touch được chuyển thẳng đến `Button`.
3. Đăng ký rõ ràng `buttonClose.onClick.AddListener(ui.Close)`.
4. Trong hàm `Close()`:
   - Gán `activeAnimal = null;`.
   - Tắt hiển thị: `panelRoot.SetActive(false);`.
   - Vô hiệu hóa raycast: `canvasGroup.alpha = 0f; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false;`.
5. Khi panel đóng, các hệ thống input phía sau hoạt động thông suốt:
   - Mobile virtual joystick điều khiển di chuyển người chơi bình thường.
   - Click/chạm Terrain di chuyển bình thường (`ClickToMove`).
   - Click CropPlot gieo hạt/thu hoạch bình thường.
   - Nút HUD bar: Supplies, Shop, Storage mở/đóng mượt mà.

---

## 3. SỬA LỖI TRÙNG GRAPHIC COMPONENT (PHASE 3)

### 3.1 Lỗi Console
```text
A GameObject can only contain one 'Graphic' component.
```

### 3.2 Vị trí phát hiện
Trong `Assets/Scripts/Buildings/BuildingUI.cs` tại đoạn code tạo danh sách vật nuôi (`AnimalList`):
```csharp
// ĐOẠN CODE CŨ GÂY LỖI:
GameObject listGo = new GameObject("AnimalList");
Image listBg = listGo.AddComponent<Image>(); // listGo đã có 1 Graphic là Image
textAnimalList = listGo.AddComponent<Text>(); // Cố tình gắn thêm Graphic thứ 2 là Text lên cùng listGo!
```
Unity quy định một GameObject chỉ được có duy nhất 1 component kế thừa từ `UnityEngine.UI.Graphic` (ví dụ `Image`, `RawImage`, `Text`, `TextMeshProUGUI`). Việc cố gắng gắn đồng thời cả `Image` và `Text` lên `listGo` khiến Unity in thông báo lỗi và hàm `AddComponent<Text>()` trả về `null`.

### 3.3 Cách sửa cấu trúc
Tách riêng biệt Background và Text:
- `listGo` chỉ chứa `Image` làm nền khung danh sách (`listBg`).
- Tạo GameObject con `Text` (`listTextGo`) làm child của `listGo`, chứa component `Text` (`textAnimalList`) với padding lề an toàn:
```csharp
GameObject textGo = new GameObject("Text");
textGo.transform.SetParent(listGo.transform, false);
RectTransform textRt = textGo.AddComponent<RectTransform>();
textRt.anchorMin = Vector2.zero;
textRt.anchorMax = Vector2.one;
textRt.offsetMin = new Vector2(10f, 10f);
textRt.offsetMax = new Vector2(-10f, -10f);

textAnimalList = textGo.AddComponent<Text>();
```
Sau khi sửa, không còn bất kỳ GameObject UI nào bị gắn trùng 2 component `Graphic`.

---

## 4. SỬA NULLREFERENCEEXCEPTION TẠI BuildingUI.cs:117 (PHASE 4)

### 4.1 Nguyên nhân
Tại dòng 117 cũ của `BuildingUI.cs`:
```csharp
textAnimalList.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
```
Vì lệnh `textAnimalList = listGo.AddComponent<Text>()` ở dòng 116 bị Unity từ chối do trùng Graphic, biến `textAnimalList` nhận giá trị `null`. Khi truy cập thuộc tính `.font`, hệ thống ném ra ngoại lệ:
```text
NullReferenceException: Error initializing farm gameplay: Assets/Scripts/Buildings/BuildingUI.cs:117
```
Ngoại lệ này khiến quá trình khởi tạo gameplay trong `FarmUI.cs` nhảy thẳng vào khối `catch`, làm các hệ thống đăng ký phía sau (`MachineManager`, `MachineUI`, `ShopUI`, `SaveLoadSystem`) không được hoàn tất đúng cách.

### 4.2 Giải pháp khắc phục
1. Tách cấu trúc GameObject để `textAnimalList` được khởi tạo thành công 100%.
2. Bổ sung kiểm tra `null` an toàn trước khi gán font và cấu hình:
   ```csharp
   if (textAnimalList != null)
   {
       textAnimalList.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
       textAnimalList.fontSize = 16;
       textAnimalList.alignment = TextAnchor.UpperLeft;
       textAnimalList.color = Color.white;
   }
   ```
3. Bổ sung kiểm tra null toàn diện trong `RefreshDisplay()` cho các tham chiếu:
   - `textTitle != null`
   - `textOccupancy != null`
   - `buttonBuyAnimal != null`
   - `textAnimalList != null`
   - `FarmBuilding.IsValidBuilding(activeBuilding)`

---

## 5. KIỂM TRA CHỨC NĂNG ANIMAL CARE (PHASE 5)

1. **Khi vào Farm**: Panel Animal Care đóng hoàn toàn, không xuất hiện trên Game view.
2. **Click Cow / Chicken**:
   - `FarmInteractionController` phát hiện va chạm raycast với `AnimalCareController`.
   - Mở giao diện `AnimalUI` hiển thị đúng thông tin con vật được click.
3. **Thông tin hiển thị đầy đủ**:
   - Tiêu đề: `CHĂM SÓC: [TÊN CON VẬT]`
   - Giống, Độ no (Hunger %), Mức độ vui vẻ (Happiness %) với thanh màu trực quan.
   - Trạng thái sản xuất: Phần trăm tiến độ sản xuất và đếm ngược thời gian (seconds).
   - Loại sản phẩm: `Sữa bò tươi (Milk)` hoặc `Trứng gà tươi (Eggs)`.
4. **Nút Cho ăn (Feed)**:
   - 3 lựa chọn thức ăn: Cỏ khô (Hay), Ngô hạt (Grain), Thức ăn viên (Pellets).
   - Kiểm tra kho đồ: trừ đúng 1 đơn vị thức ăn, tăng 35% độ no, 25% vui vẻ, cộng XP.
   - Nếu kho không có thức ăn: hiển thị thông báo cảnh báo và không trừ gì.
5. **Nút Thu hoạch (Collect)**:
   - Khi sản phẩm chưa chín: Nút thu hoạch bị khóa (`interactable = false`), nhãn ghi rõ "CHƯA SẴN SÀNG ĐỂ THU HOẠCH". Không thể thu hoạch trước hạn.
   - Khi sản phẩm đã sẵn sàng: Nút thu hoạch sáng xanh (`interactable = true`), nhãn ghi rõ "THU HOẠCH SẢN PHẨM (+XP)".
   - Click thu hoạch: Thêm Sữa bò / Trứng gà vào kho, cộng XP người chơi, cập nhật thanh XP, reset tiến độ về 0%.
6. **Nút X**:
   - Click nút X: Panel đóng ngay tức thì, xóa reference `activeAnimal`, in log `[AnimalCareUI] Closed` và `[AnimalCareUI] Selected animal cleared`.
   - Click sang con vật khác: Mở panel hiển thị chính xác con vật mới.

---

## 6. KIỂM TRA TƯƠNG TÁC TOÀN BỘ CÁC UI KHÁC (PHASE 6)

| UI Component | Trạng thái khi vào Farm | Thao tác mở/đóng | Khả năng tương tác với Gameplay |
| :--- | :---: | :---: | :---: |
| **HUD Bar chính** | Hiển thị cố định | Luôn sẵn sàng | Không che khuất tầm nhìn |
| **Animal Care UI** | **Đóng hoàn toàn** | Click vật nuôi để mở / Nút X đóng | Đóng xong không chặn raycast |
| **Supplies UI** | Đóng | Bấm nút Supplies ở HUD / Nút X đóng | Hoạt động bình thường |
| **Shop UI** | Đóng | Bấm nút Shop ở HUD / Nút X đóng | Mua chuồng, vật nuôi bình thường |
| **Storage UI** | Đóng | Bấm nút Kho / Nút X đóng | Xem và nâng cấp kho bình thường |
| **CropSelection UI** | Đóng | Click CropPlot trống để mở / Chọn hạt để gieo | Không mở tự do |
| **Building UI** | Đóng | Click vào chuồng bò/gà để mở / Nút X đóng | Cho ăn tất cả, mua thêm vật nuôi |
| **Mobile Joystick** | Kích hoạt | Nhận thao tác kéo rê di chuyển người chơi | Hoạt động mượt mà |

---

## 7. KẾT QUẢ VÀ TRẠNG THÁI CUỐI CÙNG

- **Biên dịch**: 0 lỗi (ExitCode: 0 từ C# Roslyn Compiler).
- **Lỗi đỏ Console**: Không còn bất kỳ lỗi nào liên quan đến `Graphic component` hoặc `NullReferenceException` ở `BuildingUI.cs:117`.
- **Gameplay**:
  - Không có panel nào tự mở khi load scene `Farm.unity`.
  - Nút X của Animal Care hoạt động nhạy và ổn định trên cả chuột Editor và touch Android.
  - Các ô đất, vật nuôi, cửa hàng, kho đồ, thanh XP và tiền tệ phối hợp mượt mà.
