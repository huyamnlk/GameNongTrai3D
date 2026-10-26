# Báo Cáo Lỗi Thực Tế Và Sửa Đổi Hệ Thống Công Trình, Chuồng Trại & Preview (Unity 6)

- **Dự án**: `D:\GameNongTrai3D`
- **Scene**: `Assets/Scenes/Farm.unity`
- **Mục tiêu**: Xóa bỏ hoàn toàn hiện tượng chuồng bị phình to (40m x 32m), preview/ghost bị rung lắc/giật hình, lỗi gọi hàm lặp, vật nuôi thoát khỏi chuồng, mất script reference, và lỗi runtime console.
- **Ngày thực hiện**: 15/10/2026

---

## 1. Danh Sách Lỗi Thực Tế Đã Phát Hiện & Khắc Phục

### LỖI 1: Lỗi Missing Script trên Prefab / GameObject (`BillboardFaceCamera`)
- **File**: `Assets/Scripts/Buildings/FarmBuilding.cs` (cũ) & Prefabs: `CowPen_Small.prefab`, `ChickenCoop_Small.prefab`.
- **Dòng**: Dòng ~790 trong `FarmBuilding.cs` cũ.
- **Hiện tượng**: Console Unity liên tục in cảnh báo đỏ/vàng:
  ```text
  The referenced script on this Behaviour (Game Object 'Billboard' / 'GateSign') is missing!
  ```
- **Nguyên nhân**: Class `BillboardFaceCamera` được khai báo lồng bên trong file `FarmBuilding.cs`. Unity YAML serialization đòi hỏi một MonoBehaviour phải nằm trong file `.cs` có cùng tên (`BillboardFaceCamera.cs`) để cấp phát GUID ổn định. Do đó, các instance được gán component này trên Prefab/Scene bị mất liên kết script (Missing Script).
- **Thay đổi thực hiện**:
  1. Tách hẳn `BillboardFaceCamera` ra file riêng: `Assets/Scripts/Utils/BillboardFaceCamera.cs`.
  2. Tạo meta file và liên kết chính xác class `IrishFarmSim.BillboardFaceCamera`.
  3. Xóa bỏ class trùng lặp trong `FarmBuilding.cs`.
- **Kết quả**: Hết hoàn toàn lỗi missing script trên các biển hiệu Billboard và GateSign.

---

### LỖI 2: Hàm `DisableOldGiantBuildingsAndFences()` Gọi Lặp Ở Cả `Awake()` Và `Start()`
- **File**: `Assets/Scripts/Buildings/FarmBuildingManager.cs`
- **Dòng**: Dòng 35 (`Awake`) và Dòng 62 (`Start`).
- **Hiện tượng**: Quá trình quét hierarchy và vô hiệu hóa các chuồng cũ chạy lặp đi lặp lại 2 lần liên tiếp khi scene khởi động, gây tụt FPS và có nguy cơ tắt nhầm các chuồng hợp lệ mới được khởi tạo giữa chừng.
- **Nguyên nhân**: Thiếu cờ trạng thái kiểm soát lượt chạy (`oldBuildingsDisabled`).
- **Thay đổi thực hiện**:
  1. Thêm biến static: `private static bool oldBuildingsDisabled = false;`.
  2. Thêm guard clause vào đầu hàm `DisableOldGiantBuildingsAndFences()`:
     ```csharp
     if (oldBuildingsDisabled) return;
     oldBuildingsDisabled = true;
     ```
  3. Cập nhật hàm đệ quy `DisableOldFencesRecursively` để bảo lưu tuyệt đối các chuồng hợp lệ: `CowPen_Small`, `ChickenCoop_Small`, hoặc bất kỳ GameObject nào mang component `FarmBuilding`.
- **Kết quả**: Chỉ thực thi đúng 1 lần duy nhất khi nạp scene, không duyệt lại hierarchy thừa.

---

### LỖI 3: Chuồng Nuôi Bị Phình To 40m x 32m (Không Đúng Tỷ Lệ Thực Tế)
- **File**: `Assets/Scripts/Buildings/FarmBuilding.cs`, `Assets/Scripts/Shop/ShopUI.cs`, `Assets/Scripts/Buildings/BuildingPlacementController.cs`.
- **Dòng**: Dòng ~295–350 (`CreateEnclosedFencePen`) và Dòng ~140–170 (`EnsureVisualStructure`).
- **Hiện tượng**: Chuồng bò và chuồng gà được sinh ra có kích thước khổng lồ (chiều dài 40m, chiều rộng 32m), lấn át toàn bộ nông trại và các luống rau.
- **Nguyên nhân**: Kích thước hình học trong hàm tạo procedural `CreateEnclosedFencePen` trước đây bị gán nhầm giá trị `penLenX = 40f`, `penWidZ = 32f`. Khi prefab được lưu hoặc scene được chạy, các kích thước này chiếm toàn bộ không gian.
- **Thay đổi thực hiện**:
  1. Điều chỉnh lại kích thước chuẩn xác theo yêu cầu Phase 2:
     - **CowPen (Chuồng bò)**: Chiều dài $X = 11.0\text{ m}$ (yêu cầu 10–12m), Chiều rộng $Z = 8.0\text{ m}$ (yêu cầu 7–9m), Chiều cao hàng rào $= 1.7\text{ m}$ (yêu cầu 1.5–2m).
     - **ChickenCoop (Chuồng gà)**: Chiều dài $X = 8.0\text{ m}$ (yêu cầu 7–9m), Chiều rộng $Z = 6.0\text{ m}$ (yêu cầu 5–7m), Chiều cao hàng rào $= 1.5\text{ m}$ (yêu cầu 1.2–1.8m).
  2. Cập nhật `AnimalPenBoundary.innerBoundsSize`:
     - CowPen: `Vector3(9.8f, 2.5f, 6.8f)`
     - ChickenCoop: `Vector3(6.8f, 2.0f, 4.8f)`
  3. Cập nhật footprint trong `ShopUI.cs`:
     - Cow Barn: `Vector3(11f, 2.5f, 8f)`
     - Chicken Coop: `Vector3(8f, 2.0f, 6f)`
  4. Trong `FarmBuilding.EnsureVisualStructure()` và `BuildingPlacementController.StartPlacement()`: Thêm thuật toán phát hiện renderers có kích thước $> 15\text{ m}$ (tàn dư của phiên bản cũ), lập tức xóa sạch bằng `DestroyImmediate` và tái tạo tự động theo đúng tỷ lệ chuẩn mới.
- **Kết quả**: Chuồng bò và chuồng gà hiển thị vừa vặn, cân đối, sắc nét và đúng chuẩn tỷ lệ nông trại.

---

### LỖI 4: Ghost Preview Bị Rung Lắc / Giật Hình (Jitter) Khi Di Chuột
- **File**: `Assets/Scripts/Buildings/BuildingPlacementController.cs`, `Assets/Scripts/Buildings/BuildingPreview.cs`, `ProjectSettings/TagManager.asset`.
- **Dòng**: Dòng ~230–280 (`StartPlacement`) và Dòng ~360–385 (`Update`).
- **Hiện tượng**: Khi đang trong chế độ đặt chuồng (Placement Mode), mô hình preview (ghost) bị nhấp nháy, rung giật theo trục Y và có lúc nhảy vị trí liên tục.
- **Nguyên nhân**:
  1. Ghost preview vẫn giữ lại các `BoxCollider` của hàng rào. Khi di chuột, tia Raycast từ camera chạm trúng chính các collider của ghost thay vì chạm xuống mặt đất (`Terrain`), gây nên vòng lặp tự va chạm và nhảy độ cao.
  2. Trong hàm `Update()`, mỗi frame đều gọi lại `GroundSnapUtility.SnapToGround()`, tính toán lại toàn bộ `Renderer.bounds` liên tục từng frame.
  3. Khi di chuột ra mép ngoài vùng raycast trúng, script bị gán vị trí về `Vector3.zero` thay vì giữ nguyên vị trí hợp lệ cuối cùng.
- **Thay đổi thực hiện**:
  1. Đăng ký Layer 8 trong `TagManager.asset` là `PlacementGhost`.
  2. Khi khởi tạo ghost trong `StartPlacement()`:
     - Gán toàn bộ ghost và các con sang Layer `PlacementGhost`.
     - Vô hiệu hóa và hủy hoàn toàn tất cả `Collider` và `Rigidbody` trên ghost preview.
     - Tính trước độ dời đáy một lần duy nhất: `previewBottomOffset = GroundSnapUtility.GetRendererBottomOffset(ghostGo)`.
  3. Cập nhật tia raycast trong `Update()`: Mặt nạ raycast loại trừ triệt để Layer `PlacementGhost`, Layer `Ignore Raycast` (2) và Layer `UI` (5).
  4. Cập nhật tọa độ ghost bằng công thức mượt:
     ```csharp
     targetPos = new Vector3(hit.point.x, hit.point.y + previewBottomOffset, hit.point.z);
     ```
  5. Xóa bỏ hoàn toàn việc gọi `SnapToGround` trong `Update()`.
  6. Nếu raycast không trúng mặt đất: giữ nguyên vị trí cũ, không bao giờ đặt về `Vector3.zero`.
- **Kết quả**: Ghost preview di chuyển cực kỳ mượt mà theo con trỏ chuột, bám sát bề mặt địa hình mà không hề có độ trễ hay rung giật.

---

### LỖI 5: Đáy Chuồng Không Sát Mặt Đất (Lơ Lửng Hoặc Chìm) & Gọi `SnapToGround` Rải Rác
- **File**: `Assets/Scripts/Utils/GroundSnapUtility.cs`, `Assets/Scripts/Buildings/FarmBuilding.cs`, `Assets/Scripts/Buildings/FarmBuildingManager.cs`.
- **Dòng**: `GroundSnapUtility.cs` (Dòng 15–50).
- **Hiện tượng**: Một số chuồng bị chìm cọc rào xuống đất hoặc lơ lửng trên không trung do dùng pivot làm gốc thay vì đáy thực tế của các renderers con. `SnapToGround` bị gọi chồng chéo từ `BuildingPlacementController.ConfirmPlacement`, `FarmBuildingManager.SpawnPurchasedBuildingAtPosition`, và `FarmBuilding.SetupBuilding`.
- **Nguyên nhân**: Chưa có hàm chuẩn hóa duy nhất tính toán đáy từ `Bounds` tổng của tất cả renderers.
- **Thay đổi thực hiện**:
  1. Triển khai hàm chuẩn hóa duy nhất `SnapBuildingToGround(GameObject building)` theo đúng quy chuẩn Phase 4:
     - Duyệt toàn bộ `Renderer` active trong building và các child (bỏ qua Billboard).
     - Đóng gói thành `combinedBounds`.
     - Bắn raycast từ vị trí trung tâm xuống mặt phẳng `Terrain/Ground`.
     - Tính toán độ lệch chính xác:
       ```csharp
       float offsetY = hit.point.y - combinedBounds.min.y;
       building.transform.position += Vector3.up * offsetY;
       ```
  2. Loại bỏ các lời gọi `SnapBuildingToGround` trùng lặp: chỉ gọi đúng 1 lần khi kết thúc khởi tạo cấu trúc thật trong `FarmBuilding.SetupBuilding()` và 1 lần khi Load Game.
- **Kết quả**: Đáy nền chuồng và các chân cọc rào nằm sát và tiếp xúc chính xác 100% với mặt đất tự nhiên của Terrain.

---

### LỖI 6: Vật Nuôi Spawn Sai Vị Trí, Thoát Khỏi Chuồng Hoặc Bị Nhân Bản Khi Load Game
- **File**: `Assets/Scripts/Buildings/FarmBuildingManager.cs`, `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`, `Assets/Scripts/Animals/AnimalManager.cs`.
- **Dòng**: `FarmBuildingManager.cs` (Dòng ~490–585), `SaveLoadSystem.cs` (Dòng ~335–370).
- **Hiện tượng**:
  1. Khi mua Bò hoặc Gà, vật nuôi có lúc spawn ngoài chuồng hoặc rớt xuống dưới mặt đất.
  2. Khi nạp lại Save Game (`LoadGame`), hàm phục hồi gọi `BuyAndSpawnAnimalForBuilding()`, khiến người chơi bị trừ tiếp tiền Gold và hiện popup toast như thể vừa mua mới.
- **Nguyên nhân**:
  1. Điểm spawn vật nuôi không được ràng buộc chặt chẽ với ranh giới của chuồng và không clamp vào độ cao Terrain.
  2. Chưa có phương thức riêng biệt để spawn vật nuôi phục vụ Load Game (miễn phí, không tính XP, không trừ tiền).
- **Thay đổi thực hiện**:
  1. Tạo các GameObject con định danh riêng: `CowPenBounds` (trong chuồng bò) và `ChickenCoopBounds` (trong chuồng gà).
  2. Trong `FarmBuildingManager.SpawnAnimalForBuilding()`:
     - Lấy tọa độ spawn ngẫu nhiên bên trong ranh giới: `boundary.GetRandomPositionInside(0.5f)`.
     - Ràng buộc chặt chẽ vị trí với `boundary.ClampPosition()`.
     - Lấy độ cao chuẩn từ `Terrain.activeTerrain.SampleHeight(spawnPos)`.
     - Gán trực tiếp parent của vật nuôi vào chính chuồng tương ứng (`CowPen_Small` / `ChickenCoop_Small`).
  3. Bổ sung tham số `bool deductGold = true, bool awardXP = true` vào hàm `SpawnAnimalForBuilding`.
  4. Trong `SaveLoadSystem.cs`: Khi phục hồi dữ liệu động vật, truyền `deductGold: false, awardXP: false`, đồng thời gán lại đúng `animalName` để bảo toàn dữ liệu trạng thái đói/hạnh phúc/sản xuất.
- **Kết quả**: Bò và Gà luôn luôn ở bên trong phạm vi chuồng nuôi, không bị lọt ra ngoài, không bị chìm dưới đất, và quá trình Load Game diễn ra chính xác mà không bị mất tiền.

---

### LỖI 7: Chất Liệu Màu Hồng/Tím (Missing Material / Shader Không Tương Thích)
- **File**: `Assets/Scripts/Buildings/FarmBuilding.cs`, `Assets/Scripts/Buildings/BuildingPreview.cs`.
- **Dòng**: `FarmBuilding.cs` (Dòng ~175–205).
- **Hiện tượng**: Trong một số cấu hình pipeline đồ họa, hàng rào hoặc các chi tiết procedural có thể bị hiển thị màu tím/hồng nếu shader mặc định bị thiếu.
- **Nguyên nhân**: `Shader.Find("Standard")` có thể trả về null nếu đang chạy shader stripping hoặc Universal Render Pipeline.
- **Thay đổi thực hiện**:
  1. Triển khai hàm `GetSafeShader()` đa tầng dự phòng:
     `Standard` $\rightarrow$ `Universal Render Pipeline/Lit` $\rightarrow$ `Mobile/Diffuse` $\rightarrow$ `Diffuse` $\rightarrow$ `Unlit/Color`.
  2. Triển khai hàm `CreateBuildingMaterial(name, color)` và `ValidateRenderersAndMaterials(root)`: Quét toàn bộ `Renderer` và `MeshFilter`, nếu thiếu material thì tự động tạo material fallback màu nâu gỗ/đất tự nhiên, tuyệt đối không để xảy ra màu hồng tím.
- **Kết quả**: Tất cả các cọc rào, thanh chắn, máng cỏ, máng nước, chuồng gà đều hiển thị màu sắc tự nhiên, hài hòa và rõ nét.

---

## 2. Bảng Tóm Tắt Các Tệp Mã Nguồn Đã Chỉnh Sửa

| Tệp Mã Nguồn | Mục Đích Sửa Đổi |
| :--- | :--- |
| `Assets/Scripts/Utils/BillboardFaceCamera.cs` | **Tạo mới**: Tách class riêng biệt để sửa lỗi Missing Script GUID trên Prefab và Hierarchy. |
| `ProjectSettings/TagManager.asset` | Đăng ký Layer 8: `PlacementGhost`. |
| `Assets/Scripts/Utils/GroundSnapUtility.cs` | Triển khai `SnapBuildingToGround(GameObject)` tính theo Combined Bounds min.y và `GetRendererBottomOffset()` chống giật. |
| `Assets/Scripts/Buildings/FarmBuilding.cs` | Sửa kích thước hình học chuẩn (Cow 11x8x1.7m, Chicken 8x6x1.5m), bảo vệ Ghost khỏi Awake/Start, tự động phát hiện và dọn dẹp model cũ $> 15\text{m}$ bằng `DestroyImmediate`, bổ sung shader/material fallback. |
| `Assets/Scripts/Buildings/FarmBuildingManager.cs` | Thêm cờ static chống gọi lặp `DisableOldGiantBuildingsAndFences`, tách luồng tạo chuồng duy nhất, hỗ trợ `SpawnAnimalForBuilding` không trừ tiền khi nạp Save. |
| `Assets/Scripts/Buildings/BuildingPlacementController.cs` | Loại bỏ giật lag ghost: gán layer `PlacementGhost`, xóa colliders/rigidbodies, raycast lọc bỏ ghost, xóa per-frame ground snap, lưu giữ vị trí hợp lệ cuối. |
| `Assets/Scripts/Buildings/BuildingPreview.cs` | Xóa collider/rigidbody trên preview, gán shader transparent an toàn, lọc layer mask. |
| `Assets/Scripts/Animals/AnimalManager.cs` | Khởi tạo động vật đúng chuồng `CowPen_Small` / `ChickenCoop_Small`, loại bỏ roaming ngoài chuồng. |
| `Assets/Scripts/Animals/AnimalPenBoundary.cs` | Cung cấp hàm `ClampPosition` và `GetRandomPositionInside` theo kích thước thu gọn mới. |
| `Assets/Scripts/SaveLoad/SaveLoadSystem.cs` | Phục hồi động vật an toàn thông qua `SpawnAnimalForBuilding(deductGold: false)`, bảo toàn `animalName`. |
| `Assets/Scripts/Shop/ShopUI.cs` | Cập nhật footprint chuẩn xác trong Shop để preview grid khớp với hình học thực tế. |

---

## 3. Quy Trình Kiểm Thử Đã Thực Hiện

1. **Khởi Động Scene `Farm.unity`**:
   - `DisableOldGiantBuildingsAndFences` chỉ chạy đúng 1 lần.
   - Không xuất hiện chuồng cũ khổng lồ.
   - Không có lỗi Missing Script `BillboardFaceCamera`.
2. **Mua và Đặt Chuồng Bò (Cow Barn)**:
   - Preview ghost xuất hiện với đúng kích thước nhỏ gọn 11m x 8m x 1.7m.
   - Di chuyển chuột khắp nông trại: ghost bám sát mặt đất, không bị rung giật, không nhấp nháy, không bị chuyển về gốc $(0,0,0)$.
   - Xác nhận đặt: Chuồng xuất hiện trong Hierarchy (`Farm/CowPen_Small`), đáy chuồng tiếp xúc chuẩn xác với mặt đất.
3. **Mua Bò (Cow)**:
   - Bò spawn ngay bên trong ranh giới `CowPenBounds`.
   - Bò được gán làm con của `CowPen_Small`, đi lại tự nhiên trong phạm vi hàng rào.
4. **Mua và Đặt Chuồng Gà (Chicken Coop)**:
   - Kích thước hiển thị 8m x 6m x 1.5m.
   - Đặt thành công tại vị trí mong muốn.
5. **Mua Gà (Chicken)**:
   - Gà spawn bên trong ranh giới `ChickenCoopBounds`, parented vào `ChickenCoop_Small`.
6. **Lưu và Tải Game (Save/Load)**:
   - Dữ liệu vị trí chuồng, số lượng vật nuôi được lưu và nạp lại chuẩn xác.
   - Khi load, số vàng (Gold) không bị trừ thêm, không xuất hiện popup toast mua hàng.
7. **Trạng Thái Console**:
   - Không còn lỗi đỏ runtime liên quan đến các hệ thống trên.
