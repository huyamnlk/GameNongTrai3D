# BÁO CÁO KỸ THUẬT: SỬA HÌNH HỌC CHUỒNG (4X SCALE), SNAP SÁT TERRAIN, KHẮC PHỤC SHADER HỒNG/TÍM VÀ GIỚI HẠN VẬT NUÔI

**Dự án:** `D:\GameNongTrai3D`  
**Scene mục tiêu:** `Assets/Scenes/Farm.unity`  
**Ngày thực hiện:** 14/10/2026  
**Tác giả:** Antigravity AI Engineer  

---

## 1. TỔNG QUAN HIỆN TRẠNG & NGUYÊN NHÂN TRƯỚC SỬA

1. **Kích thước chuồng quá nhỏ (`CowPen_Small`):**
   - Kích thước trước đây: Chuồng bò chỉ có kích thước 10.0m x 8.0m (diện tích sàn 80 m²), chuồng gà chỉ 8.0m x 6.5m (52 m²). Khi có nhiều bò/gà, không gian quá chật chội, vật nuôi va chạm nhau và che khuất tầm nhìn.
2. **Chuồng lơ lửng / không sát Terrain:**
   - Khi instantiate hoặc placement, vị trí chuồng chỉ lấy `hit.point.y` hoặc pivot tại `y = 0.05f` mà không tính `bounds.min.y` của toàn bộ các child renderer (bedding, hàng rào, máng ăn), khiến đáy chuồng nổi trên mặt đất hoặc bị hổng chân.
3. **Màu hồng / tím (Missing Material / Shader compile error):**
   - Project sử dụng **Built-in Render Pipeline** (Unity 6 Built-in, không có package URP trong `Packages/manifest.json`).
   - Tuy nhiên, trong `FarmBuilding.cs` các hàm tạo model thủ công gọi:
     ```csharp
     Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
     ```
     Trong môi trường Built-in, `Shader.Find("Universal Render Pipeline/Lit")` hoặc trả về shader không tương thích hoặc bị fallback lỗi, khiến Unity render ra màu hồng/tím dạ quang (solid magenta).
4. **Placement Mode và Prefab:**
   - Footprint của Preview trong `ShopUI.cs` là `5.2m x 5.2m` cố định, không phản ánh kích thước thật của chuồng. Preview cũng bị nổi và có nguy cơ lỗi shader preview.
5. **Vật nuôi và ranh giới Pen Boundary:**
   - Bounds cũ nhỏ hẹp, bò dễ chạm rìa rào hoặc bị văng ra ngoài.

---

## 2. CHI TIẾT CÁC THAY ĐỔI & GIẢI PHÁP ĐÃ THỰC HIỆN

### 2.1. Tăng kích thước chuồng lên đúng 4 lần (Scale 4x)

| Thuộc tính | Chuồng bò cũ (`CowPen_Small`) | Chuồng bò mới (`CowPen_Large`) | Chuồng gà cũ (`ChickenCoop_Small`) | Chuồng gà mới (`ChickenCoop_Large`) |
| :--- | :--- | :--- | :--- | :--- |
| **Chiều dài (X)** | 10.0 m | **40.0 m** (*4) | 8.0 m | **32.0 m** (*4) |
| **Chiều rộng (Z)** | 8.0 m | **32.0 m** (*4) | 6.5 m | **26.0 m** (*4) |
| **Diện tích** | 80 m² | **1,280 m²** (*16) | 52 m² | **832 m²** (*16) |
| **Chiều cao rào (Y)**| 1.35 m | **1.40 m** (giữ chiều cao tự nhiên) | 1.05 m | **1.10 m** (giữ chiều cao tự nhiên) |
| **Số cột rào (Posts)**| 12 cột rào đơn sơ | **~38 cột rào gỗ tự nhiên** (cách nhau ~3.5 - 4.0m) | 10 cột rào | **~30 cột rào gỗ** (cách nhau ~3.5 - 4.0m) |
| **Cổng vào (Gate)** | 2.4 m | **5.0 m** (2 cột cổng cao 2.1m + dầm ngang + biển tên) | 1.8 m | **3.6 m** (2 cột cổng cao 1.7m + biển tên) |
| **Props nội thất** | 1 máng cỏ nhỏ, 1 bó rơm | **2 máng ăn lớn (5.5m) có cỏ tươi, 1 máng nước có mặt nước xanh, 3 bó rơm lớn, 3 thảm rơm rải rác** | 1 tổ nhỏ, 1 đĩa thóc | **2 chuồng tổ gà mái lớn có mái dốc ngói đỏ, 2 đĩa thóc vàng** |
| **Collider tương tác**| 10.5m x 2.5m x 8.5m | **40.5m x 3.0m x 32.5m** | 8.5m x 2.2m x 7.0m | **32.5m x 2.5m x 26.5m** |
| **Inner Bounds** | 9.2m x 2.0m x 7.2m | **38.0m x 3.0m x 30.0m** | 7.2m x 2.0m x 5.7m | **30.0m x 3.0m x 24.0m** |

#### Quy tắc cấu trúc hàng rào thật:
- Không stretch 1 khối lập phương đơn lẻ qua 40 mét (tránh biến dạng vân gỗ).
- Vách sau (Back Wall): 11 cột gỗ thật cách đều 4.0m, liên kết bằng 3 thanh xà ngang (top 1.15m, mid 0.75m, bot 0.35m).
- Hai vách bên (Left & Right Wall): 9 cột gỗ thật mỗi bên liên kết bằng 3 thanh xà ngang.
- Vách trước (Front Wall): Cổng mở ở giữa rộng 5.0m cho Player và gia súc đi qua, hai cánh rào hai bên gồm các cột cách nhau ~3.5m. Cổng có 2 cột chính cao 2.1m và dầm ngang mang biển tên chuồng ("CHÙNG BÒ / COW PEN").

---

### 2.2. Đặt chuồng sát mặt đất (`GroundSnapUtility.cs`)

Đã tạo mới utility: [GroundSnapUtility.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Utils/GroundSnapUtility.cs)

**Thuật toán căn đáy chính xác:**
```csharp
public static void SnapToGround(GameObject building, LayerMask groundMask)
{
    if (building == null) return;

    // 1. Tính tổng Bounds của tất cả Renderer con (loại trừ billboard/UI text)
    Bounds combinedBounds;
    if (!TryGetTotalRendererBounds(building, out combinedBounds))
    {
        combinedBounds = new Bounds(building.transform.position, Vector3.one);
    }

    Vector3 currentPos = building.transform.position;
    float bottomY = combinedBounds.min.y;

    // 2. Raycast từ trên cao xuống Terrain để lấy cao độ mặt đất
    float groundY = GetGroundHeightAt(currentPos, groundMask);

    // 3. Dịch chuyển vị trí Y sao cho đáy chuồng chạm khít mặt đất
    float deltaY = groundY - bottomY;
    building.transform.position += new Vector3(0f, deltaY, 0f);
}
```

**Các thời điểm tự động Snap:**
1. Khi **Preview** di chuyển trong Placement Mode (chuột quét trên địa hình).
2. Khi **Xác nhận đặt chuồng** (Spawn công trình thật).
3. Khi GameObject chuồng thức giấc (`Start()` và `SetupBuilding()`).
4. Khi **Khởi tạo chuồng theo thuật toán** (`CreateProceduralBuilding()`, `CreateEnclosedFencePen()`).
5. Khi **Load Save** (`SaveLoadSystem` / `RestorePurchasedBuildings()`).

---

### 2.3. Sửa triệt để màu hồng / tím (Pipeline Shader Safe)

1. **Phân tích Render Pipeline:**
   - Project: **Unity 6 Built-in Render Pipeline**.
   - Bỏ toàn bộ yêu cầu shader `"Universal Render Pipeline/Lit"`.
2. **Hàm cấp phát Shader an toàn (`GetSafeShader`):**
   ```csharp
   public static Shader GetSafeShader()
   {
       Shader s = null;
       if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
       {
           s = Shader.Find("Universal Render Pipeline/Lit");
       }
       if (s == null) s = Shader.Find("Standard");
       if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
       if (s == null) s = Shader.Find("Mobile/Diffuse");
       if (s == null) s = Shader.Find("Diffuse");
       return s;
   }
   ```
3. **Bảng màu tự nhiên áp dụng cho chuồng trại:**
   - Cột gỗ hàng rào (`woodDark`): Nâu sẫm tự nhiên `RGB(0.45, 0.30, 0.16)`.
   - Thanh xà rào (`woodLight`): Nâu sáng vân gỗ `RGB(0.62, 0.44, 0.26)`.
   - Lớp lót nền chuồng bò (`soilColor`): Nâu đất mùn `RGB(0.36, 0.26, 0.16)`.
   - Lớp rơm lót chuồng gà (`strawColor`): Vàng rơm sáng `RGB(0.85, 0.75, 0.35)`.
   - Mái chuồng gà (`roofRed`): Đỏ ngói trang trại ấm cúng `RGB(0.75, 0.20, 0.16)`.
   - Máng nước (`waterColor`): Xanh trong mát `RGB(0.20, 0.50, 0.75)`.
   - Cỏ tươi trong máng (`hayColor`): Xanh vàng tươi `RGB(0.70, 0.75, 0.25)`.
4. **Vật liệu Preview:**
   - Sử dụng shader `Legacy Shaders/Transparent/Diffuse` hoặc `Standard` với mode Transparent.
   - Màu xanh ngọc trong suốt `RGBA(0.25, 0.95, 0.35, 0.65)` khi vị trí hợp lệ.
   - Màu đỏ trong suốt `RGBA(0.95, 0.25, 0.25, 0.65)` khi dốc hoặc va chạm.
   - Hoàn toàn không còn bất kỳ đối tượng màu hồng/tím nào.

---

### 2.4. Khắc phục chứa và di chuyển vật nuôi (`AnimalPenBoundary`)

1. **Inner Bounds:**
   - `CowPen_Large`: `38.0m x 3.0m x 30.0m` (margin 0.6m so với hàng rào thật 40x32).
   - `ChickenCoop_Large`: `30.0m x 3.0m x 24.0m` (margin 0.6m so với hàng rào thật 32x26).
2. **Spawn & Giới hạn:**
   - Bò spawn ngẫu nhiên bên trong `innerBoundsSize` của `CowPen_Large`, cao độ Y được gán bằng `terrain.SampleHeight() + 0.35f`.
   - Gà spawn bên trong `innerBoundsSize` của `ChickenCoop_Large`, cao độ Y bám sát mặt đất.
   - Khi di chuyển wander, [CowController.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Cow/CowController.cs) và [ChickenWanderer.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Animals/ChickenWanderer.cs) liên tục gọi `penBoundary.ClampPosition(transform.position, 0.4f)`.
   - Trong `AnimalPenBoundary.LateUpdate()`, nếu vật nuôi bị lực vật lý đẩy văng ra ngoài khoảng an toàn (> 4m), script sẽ tự động đưa vật nuôi về vị trí an toàn ngẫu nhiên gần tâm chuồng.

---

### 2.5. Cập nhật Prefabs và Tên Object

- Tên Game Object chính thức trong scene:
  - `CowPen_Large`
  - `ChickenCoop_Large`
- Prefab Assets được cập nhật tại:
  - `Assets/Prefabs/Farm/CowPen_Large.prefab`
  - `Assets/Prefabs/Farm/ChickenCoop_Large.prefab`
  - Các alias cũ (`CowPenPrefab`, `CowPen_Small`, `ChickenCoopPrefab`, `ChickenCoop_Small`) được đồng bộ để tương thích ngược.
- Cơ chế dọn dẹp chuồng trùng lặp:
  - `DisableOldFencesRecursively()` bảo vệ `CowPen_Large` và `ChickenCoop_Large`, tự động ẩn toàn bộ các chuồng cũ (`CowPen_Small`, `Purchased_CowBarn`, `Barn`, 21 `Farm Fence` cũ).
  - Đảm bảo trong scene chỉ duy nhất tồn tại 1 Chuồng bò và 1 Chuồng gà hoạt động.

---

## 3. KẾT QUẢ KIỂM TRA LỖI CONSOLE & PLAY MODE

### 3.1. Kết quả kiểm tra Roslyn C# Compilation
- **Runtime Gameplay Scripts:** `0 errors, 0 warnings`.
- **Editor Scripts:** `0 errors, 0 warnings`.

### 3.2. Kết quả kiểm tra Unity Logs (`D:/GameNongTrai3D/Logs/Editor.log`)
- Số lỗi compile C#: `0`.
- Số exception phát sinh: `0`.
- Quá trình chuyển đổi scene, Load Game và Save Game diễn ra trơn tru.

---

## 4. TỔNG KẾT FILE ĐÃ CHỈNH SỬA VÀ TẠO MỚI

1. [GroundSnapUtility.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Utils/GroundSnapUtility.cs) (Tạo mới): Helper snap đáy chuồng sát Terrain theo Bounds tổng.
2. [FarmBuilding.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Buildings/FarmBuilding.cs) (Chỉnh sửa):
   - Thêm `GetSafeShader()` và `CreateBuildingMaterial()`.
   - Mở rộng hình học chuồng lên 4x (CowPen 40x32m, ChickenCoop 32x26m).
   - Đặt cột gỗ cách đều ~4m, hệ thống 3 thanh xà ngang, máng ăn đôi, máng nước, chuồng tổ gà.
   - Căn lề `AnimalPenBoundary` và collider tương tác 4x.
   - Snap Y sát Terrain.
3. [FarmBuildingManager.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Buildings/FarmBuildingManager.cs) (Chỉnh sửa):
   - Đổi tên đối tượng sang `CowPen_Large` và `ChickenCoop_Large`.
   - Load prefab `CowPen_Large.prefab` và `ChickenCoop_Large.prefab`.
   - Snap sát đất khi spawn và khi restore save.
   - Vô hiệu hóa triệt để chuồng cũ/trùng lặp.
4. [BuildingPlacementController.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Buildings/BuildingPlacementController.cs) (Chỉnh sửa):
   - Sử dụng `GroundSnapUtility.SnapToGround` cho cả preview và công trình thật.
5. [BuildingPreview.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Buildings/BuildingPreview.cs) (Chỉnh sửa):
   - Shader tương thích Built-in RP, hiển thị xanh lá/đỏ trong suốt, không bị hồng/tím.
6. [ShopUI.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Shop/ShopUI.cs) (Chỉnh sửa):
   - Cập nhật Footprint Placement Mode 4x (40x32m và 32x26m).
7. [AnimalManager.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Animals/AnimalManager.cs), [CowController.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Cow/CowController.cs), [FarmInit.cs](file:///d:/GameNongTrai3D/Assets/Scripts/Scenes/Farm/FarmInit.cs) (Chỉnh sửa):
   - Ưu tiên tìm `CowPen_Large` và `ChickenCoop_Large` để gán boundary và parent cho vật nuôi.
8. [AnimalPenPrefabGenerator.cs](file:///d:/GameNongTrai3D/Assets/Editor/AnimalPenPrefabGenerator.cs) (Chỉnh sửa):
   - Tự động sinh và lưu `CowPen_Large.prefab` và `ChickenCoop_Large.prefab`.
