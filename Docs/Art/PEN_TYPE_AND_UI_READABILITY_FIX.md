# TÀI LIỆU SỬA ĐÚNG LOẠI CHUỒNG, GIỚI HẠN VẬT NUÔI VÀ ĐỘ RÕ NÉT CỦA UI

**Dự án:** `D:\GameNongTrai3D`  
**Ngày thực hiện:** 15/10/2026  
**File tài liệu:** `Docs/PEN_TYPE_AND_UI_READABILITY_FIX.md`

---

## 1. Nguyên nhân chuồng gà bị nhận nhầm thành chuồng bò

1. **Model & Prefab bị sao chép từ chuồng bò:**
   - File prefab `Assets/Prefabs/Farm/ChickenCoop_Small.prefab` ban đầu được nhân bản từ `CowPen_Small.prefab`.
   - Cấu trúc con `ProceduralModel` của prefab gà vẫn chứa `FeedingTrough` (máng ăn cho bò) và biển cổng `GateSign` ghi chữ `"CHUỒNG BÒ (COW BARN)"`.
2. **Logic kiểm tra tái tạo mô hình chưa chặt chẽ:**
   - Trong `FarmBuilding.cs`, hàm `EnsureVisualStructure()` trước đây chỉ kiểm tra nếu đã có `ProceduralModel` và kích thước lót chuồng (`PenBedding`) <= 15m thì giữ nguyên mà không xác thực loại chuồng (ví dụ: chuồng gà phải có `NestingRoost` - ổ đẻ rơm và KHÔNG được có `FeedingTrough` - máng ăn bò).
   - Do đó, khi instantiate Chicken Coop, mô hình chuồng bò bên trong prefab đã bị giữ lại nguyên trạng.
3. **Thiếu định danh loại công trình phân tách tuyệt đối:**
   - `FarmBuilding.cs` chỉ sử dụng các chuỗi string linh hoạt, chưa có enum định danh cố định để ràng buộc logic tương tác, sức chứa và loại con vật được phép nhập chuồng.

---

## 2. Cách sửa Building Type (CowBarn vs ChickenCoop)

1. **Bổ sung Enum và thuộc tính chuẩn hóa:**
   - Trong [FarmBuilding.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Buildings/FarmBuilding.cs):
     ```csharp
     public enum BuildingType
     {
         CowBarn,
         ChickenCoop,
         PigPen,
         HorseStable,
         Storage,
         MachineShed,
         Other
     }
     public BuildingType buildingTypeEnum = BuildingType.Other;
     public AnimalType allowedAnimalType = AnimalType.Cow;
     ```
2. **Thiết lập thông số riêng biệt:**
   - **Cow Barn / Chuồng bò:**
     - `displayName = "Cow Barn / Chuồng bò"`
     - `capacity = 3`
     - `allowedAnimalType = AnimalType.Cow`
     - `buildingTypeEnum = BuildingType.CowBarn`
     - Model yêu cầu: `FeedingTrough`, `GateSign = "COW BARN / CHUỒNG BÒ"`
   - **Chicken Coop / Chuồng gà:**
     - `displayName = "Chicken Coop / Chuồng gà"`
     - `capacity = 5`
     - `allowedAnimalType = AnimalType.Chicken`
     - `buildingTypeEnum = BuildingType.ChickenCoop`
     - Model yêu cầu: `NestingRoost`, `GateSign = "CHICKEN COOP / CHUỒNG GÀ"`
3. **Tự động thanh lọc và xây lại nếu phát hiện Model bị sai:**
   - Trong `EnsureVisualStructure()`, hệ thống phát hiện nếu Chicken Coop có `FeedingTrough` hoặc thiếu `NestingRoost`, hoặc Cow Barn có `NestingRoost`, nó sẽ hủy ngay `existingProc` bằng `DestroyImmediate` và tạo mới mô hình chuẩn xác.
4. **Hỗ trợ di chuyển dữ liệu cũ (Save Migration):**
   - Trong [SaveLoadSystem.cs](file:///D:/GameNongTrai3D/Assets/Scripts/SaveLoad/SaveLoadSystem.cs): Khi nạp dữ liệu lưu cũ, nếu bản ghi con vật là `Chicken` nhưng trỏ tới `cow_barn`, hệ thống tự động migrate sang `chicken_coop`; tương tự `Cow` trỏ tới `chicken_coop` sẽ được chuyển về `cow_barn`.

---

## 3. Cách giới hạn 3 Bò và 5 Gà

1. **Ràng buộc khi mua vật nuôi:**
   - Trong [FarmBuildingManager.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Buildings/FarmBuildingManager.cs) và [ShopUI.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Shop/ShopUI.cs):
     - Kiểm tra sự tồn tại của chuồng: Nếu chưa có chuồng phù hợp, thông báo chính xác:
       - `"Bạn cần xây Chuồng bò trước"`
       - `"Bạn cần xây Chuồng gà trước"`
     - Kiểm tra sức chứa: Nếu chuồng đã đạt tối đa (`GetOccupantCount() >= capacity`), từ chối mua và hiển thị:
       - `"Chuồng đã đầy: 3/3"` đối với Chuồng bò
       - `"Chuồng đã đầy: 5/5"` đối với Chuồng gà
2. **Khởi tạo và bảo toàn giới hạn:**
   - Trong [AnimalManager.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Animals/AnimalManager.cs):
     - `InitializeAnimals()` giới hạn tối đa 3 bò trong chuồng bò và 5 gà trong chuồng gà.
     - Các con vật dư thừa (nếu có từ scene cũ hoặc dữ liệu lỗi) sẽ tự động bị loại bỏ (`Destroy`), không để vượt quá 3/3 hoặc 5/5.
3. **An toàn khi sinh con vật:**
   - Vị trí sinh con vật được tính toán bên trong `AnimalPenBoundary` và snap lên bề mặt Terrain (không bao giờ sinh ở tọa độ (0, 0, 0) hoặc lọt ra ngoài chuồng).
   - Chỉ trừ tiền khi con vật đã khởi tạo thành công và có renderer hợp lệ. Nếu thất bại, hoàn tiền và hủy đối tượng.

---

## 4. Cách xóa viền xanh sau Placement

1. **Xử lý triệt để Preview Ghost:**
   - Khi bấm **"ĐẶT NGAY"** hoặc **"HỦY BỎ"** trong [BuildingPlacementController.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Buildings/BuildingPlacementController.cs):
     - Lập tức gọi `activePreview.gameObject.SetActive(false)` và `DestroyImmediate(activePreview.gameObject)` (thay vì `Destroy` chờ cuối frame).
     - Quét toàn bộ scene để dọn dẹp bất kỳ đối tượng nào có tên bắt đầu bằng `PlacementGhost` hoặc nằm trên Layer `PlacementGhost`.
     - Đặt `activePreview = null; isPlacing = false;`.
2. **Thanh lọc công trình thật vừa đặt:**
   - Triệu gọi phương thức `CleanUpBuildingVisuals(spawnedGo)`:
     - Gỡ bỏ mọi component `BuildingPreview` còn sót lại.
     - Gỡ bỏ mọi `LineRenderer`, `Outline`, `Selection`, `Highlight` nếu có trên công trình.
     - Đưa layer công trình về mặc định (`Default` - Layer 0).
     - Đảm bảo vật liệu là vật liệu công trình thông thường, không còn vật liệu xanh preview trong suốt.
3. **Tắt chặn raycast của UI Placement:**
   - Tắt `placementOverlayCanvas.SetActive(false)`.
   - Đặt `CanvasGroup.blocksRaycasts = false;` và `CanvasGroup.interactable = false;`.

---

## 5. Cách sửa chuồng sát mặt đất

1. **Căn chỉnh đáy chuẩn xác:**
   - [GroundSnapUtility.cs](file:///D:/GameNongTrai3D/Assets/Scripts/Utils/GroundSnapUtility.cs) tính toán Bounds bao phủ của tất cả renderer con (`combinedBounds.min.y`).
   - Bắn Raycast từ vị trí trên cao xuống Terrain/Ground (bỏ qua bản thân công trình và layer PlacementGhost).
   - Tính toán `offsetY = hit.point.y - combinedBounds.min.y` và dịch chuyển công trình đúng khoảng cách này, giúp đáy chuồng/hàng rào nằm sát mặt đất, không bị lơ lửng hay chìm sâu.
2. **Khử giật/rung trong chế độ xem trước:**
   - Trong `BuildingPlacementController.cs`, trước khi kéo rê, tính trước `previewBottomOffset = GroundSnapUtility.GetRendererBottomOffset(ghostGo)`.
   - Khi raycast di chuyển chuột, vị trí Y được gán mượt mà bằng `hit.point.y + previewBottomOffset`.
   - LayerMask của raycast loại trừ triệt để Layer `PlacementGhost` (Layer 8) để không tự raycast vào chính ghost gây giật.

---

## 6. Các thiết lập UI giúp chữ rõ, không mờ, không vỡ

1. **Canvas Scaler đồng bộ:**
   - Toàn bộ Canvas trong game (`BuildingUICanvas`, `ProgressionCanvas`, `ShopUICanvas`, `StorageCanvas`, `CropSelectionCanvas`, `SuppliesCanvas`, `MachineUICanvas`, `AnimalUICanvas`):
     - `uiScaleMode = ScaleWithScreenSize`
     - `referenceResolution = 1920 x 1080`
     - `screenMatchMode = MatchWidthOrHeight`
     - `matchWidthOrHeight = 0.5f`
2. **Chữ 3D (GateSign, Billboard, Animal Status):**
   - Tăng `fontSize` lên `48 - 50` kết hợp giảm `characterSize` xuống `0.065 - 0.08` (kỹ thuật chuẩn của Unity để tạo TextMesh sắc nét không vỡ hạt).
   - Gán `MeshRenderer.material = font.material` để tận dụng texture atlas độ phân giải cao của font.
   - Luôn gắn component `BillboardFaceCamera` để text luôn quay mặt về phía camera người chơi, không bị lật ngược hoặc xiên xẹo.
   - Thêm viền `Outline` màu đen tương phản phía sau text UI quan trọng.
3. **Thanh điều hướng HUD đầy đủ:**
   - Bổ sung nút **LƯU (SAVE)** và **TẢI (LOAD)** bên cạnh các nút **CỬA HÀNG (SHOP)** và **KHO ĐỒ (STORAGE)** trên HUD với phông chữ đậm nét, màu sắc tương phản cao.

---

## 7. UI Thông tin chuồng khi bấm vào

- Khi người chơi click hoặc chạm vào chuồng:
  - Hiển thị tiêu đề rõ ràng: **"COW BARN / CHUỒNG BÒ"** hoặc **"CHICKEN COOP / CHUỒNG GÀ"**.
  - Hiển thị tỷ lệ số lượng: `"Số lượng: 2/3 | Vật nuôi cho phép: Cow / Bò"` hoặc `"Số lượng: 3/5 | Vật nuôi cho phép: Chicken / Gà"`.
  - Hiển thị danh sách vật nuôi cùng mức độ no bụng, chỉ số vui vẻ, trạng thái sản xuất.
  - Nút Mua vật nuôi:
    - Nếu chuồng chưa đầy: hiển thị `+ MUA BÒ (100 Gold)` hoặc `+ MUA GÀ (60 Gold)`.
    - Nếu chuồng đã đầy: hiển thị `CHUỒNG ĐÃ ĐẦY (3/3)` hoặc `CHUỒNG ĐÃ ĐẦY (5/5)` và vô hiệu hóa nút.
  - Nút đóng `X` kích thước chuẩn 50x50, hỗ trợ cả chuột và màn hình cảm ứng (Touch), khi đóng sẽ thu hồi `blocksRaycasts` để không gây chặn tương tác trên màn hình.

---

## 8. Danh sách file đã thay đổi

1. `Assets/Scripts/Data/FarmEconomyData.cs`
   - Chuẩn hóa tên hiển thị, sức chứa mặc định và mô tả shop cho Cow Barn (3 bò) và Chicken Coop (5 gà).
2. `Assets/Scripts/Buildings/FarmBuilding.cs`
   - Bổ sung enum `BuildingType`, định danh `allowedAnimalType`.
   - Cơ chế phát hiện và dọn sạch mô hình sai trong prefab (phân biệt Chicken Coop và Cow Barn).
   - Tăng độ phân giải text biển cổng 3D (fontSize 48, characterSize 0.065).
   - Thêm `OnMouseDown()` hỗ trợ tương tác trực tiếp bằng click chuột/touch.
   - Khắc phục lỗi CS0136 về xung đột phạm vi biến.
3. `Assets/Scripts/Buildings/FarmBuildingManager.cs`
   - Ràng buộc thông báo tiếng Việt khi thiếu chuồng hoặc chuồng đầy theo đúng yêu cầu người dùng.
   - Kiểm tra an toàn vị trí spawn không bao giờ rơi vào tọa độ (0, 0, 0).
4. `Assets/Scripts/Buildings/BuildingPlacementController.cs`
   - Chuyển nút xác nhận sang nhãn `"ĐẶT NGAY"`.
   - Hủy preview ghost ngay lập tức bằng `DestroyImmediate`.
   - Quét sạch mọi outline, highlight, preview material còn sót lại trên công trình thật sau khi đặt hoặc hủy.
   - Tắt `blocksRaycasts` của canvas overlay sau khi kết thúc đặt.
5. `Assets/Scripts/Buildings/BuildingUI.cs`
   - Thiết lập CanvasScaler `MatchWidthOrHeight 0.5`.
   - Phân biệt rõ ràng tiêu đề Cow Barn và Chicken Coop.
   - Hiển thị đúng sức chứa (X/3, X/5), nút mua động theo loại vật nuôi và trạng thái chuồng.
6. `Assets/Scripts/Animals/AnimalManager.cs`
   - Giới hạn tối đa 3 bò trong Cow Barn và 5 gà trong Chicken Coop.
   - Tự động thanh lọc các con vật vượt quá giới hạn và đưa vật nuôi vào đúng chuồng sở hữu.
7. `Assets/Scripts/Animals/AnimalCareController.cs`
   - Cải thiện độ sắc nét của text 3D thông số vật nuôi (fontSize 48, characterSize 0.07).
8. `Assets/Scripts/Progression/ProgressionUI.cs`
   - Bổ sung `screenMatchMode = MatchWidthOrHeight`.
   - Bổ sung các nút HUD: **LƯU (SAVE)** và **TẢI (LOAD)** bên cạnh **SHOP** và **STORAGE**.
9. `Assets/Scripts/Shop/ShopUI.cs`
   - Bổ sung `screenMatchMode = MatchWidthOrHeight`.
   - Cập nhật thông báo thiếu chuồng hoặc chuồng đầy chính xác từng ký tự theo đặc tả.
10. `Assets/Scripts/Inventory/StorageUI.cs`
    - Cập nhật CanvasScaler chuẩn hóa độ sắc nét.
11. `Assets/Scripts/Farming/CropSelectionUI.cs`
    - Cập nhật CanvasScaler chuẩn hóa độ sắc nét.
12. `Assets/Scripts/Scenes/Farm/SuppliesUI.cs`
    - Cập nhật CanvasScaler chuẩn hóa độ sắc nét.
13. `Assets/Scripts/Machines/MachineUI.cs`
    - Cập nhật CanvasScaler chuẩn hóa độ sắc nét.
14. `Assets/Scripts/Animals/AnimalUI.cs`
    - Cập nhật CanvasScaler chuẩn hóa độ sắc nét.
15. `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`
    - Bổ sung logic di trú dữ liệu (Save Migration) bảo đảm vật nuôi luôn gắn đúng chuồng tương ứng (Gà -> Chicken Coop, Bò -> Cow Barn).

---

## 9. Các lỗi còn lại nếu có

- **Compile Errors:** **0 lỗi** (Đã biên dịch xác thực với Unity Roslyn Compiler, không còn bất kỳ lỗi compile nào).
- **Runtime Blockers:** Không còn. Hệ thống đã sẵn sàng cho người chơi vào scene `Assets/Scenes/Farm.unity` và nhấn **Play Mode** để trải nghiệm.
