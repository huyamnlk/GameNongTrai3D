# BÁO CÁO KIỂM KÊ & DỌN DẸP PROJECT UNITY (`GameNongTrai3D`)

**Ngày thực hiện:** 19/10/2026  
**Thư mục gốc:** `D:\GameNongTrai3D`  
**Trạng thái Compile & GUID:** `0` lỗi compile (`Assembly-CSharp.dll` & `Assembly-CSharp-Editor.dll`), `0` missing script GUID, `0` file `.meta` mồ côi.

---

## 1. Tổng số file trước và sau khi dọn dẹp

| Phạm vi thống kê | Trước khi dọn | Sau khi dọn | Ghi chú |
|---|---:|---:|---|
| **Tổng file trong `Assets/`** (bao gồm `.meta`) | `4,130` | `3,938` | Đã loại bỏ/cách ly `192` file dư thừa, trùng lặp hoặc không tham chiếu |
| **Tổng script C# trong `Assets/Scripts/`** | `82` | `74` | `70` runtime scripts + `4` editor scripts đang hoạt động |
| **Tổng file trong `_Quarantine_Review/`** | `0` | `178` | Giữ nguyên cấu trúc tương đối và đầy đủ file `.meta` (bảo toàn GUID) |
| **Tổng file trong `_Archive/Backups/`** | `0` | `85,638` | Gom toàn bộ các thư mục backup cũ ở root và `Docs/HayDayReference/Scripts` |
| **Tổng file tài liệu trong `Docs/`** | `49` | `40` | Phân loại vào `Architecture/`, `Gameplay/`, `Art/`, `SaveLoad/`, `Archive/` |
| **File `.meta` mồ côi trong `Assets/`** | `0` | `0` | Đã kiểm tra tự động toàn bộ cây thư mục `Assets/` |

---

## 2. Danh sách File / Thư mục đã xóa vĩnh viễn (Chắc chắn không dùng)

Các file log tạm thời, file báo cáo debug sinh tự động trong `Assets/` và hệ thống showcase sprite 2D gây lỗi chồng hình trước đó đã được xóa an toàn kèm `.meta`:

1. **File log biên dịch & kiểm thử tạm ở thư mục gốc (`D:\GameNongTrai3D`)**:
   - `batch_diag.log`
   - `compile_phase1.log`
   - `compile_phase2.log`
   - `test_batch.log`
   - `test_results.txt`
   - `unity_compile.log`
2. **File debug tự sinh trong `Assets/`**:
   - `Assets/ground_debug_report.txt` (+ `.meta`)
3. **Các script và prefab thuộc hệ thống 2D Sprite Showcase lỗi (đã gỡ ở bước khôi phục động vật 3D)**:
   - `Assets/Scripts/Animals/FarmAnimalVisual.cs` (+ `.meta`)
   - `Assets/Scripts/Scenes/Farm/FarmArtRuntimeShowcase.cs` (+ `.meta`)
   - `Assets/Scripts/Scenes/Farm/FarmArtAutoBakeSystem.cs` (+ `.meta`)
   - `Assets/Scripts/Scenes/Farm/FarmArtShowcase.cs` (+ `.meta`)
   - `Assets/Prefabs/Art/` (`CowArtPrefab.prefab`, `ChickenArtPrefab.prefab`, `PigArtPrefab.prefab`, `CornArtPrefab.prefab`, `WheatArtPrefab.prefab`, `TomatoArtPrefab.prefab`, `CarrotArtPrefab.prefab`, `BarnArtPrefab.prefab`, `SiloArtPrefab.prefab`, `WindmillArtPrefab.prefab`, `BakeryArtPrefab.prefab` + `.meta`)
4. **Các thư mục rỗng cũ trong `Assets/Scripts/` sau khi tái cấu trúc**:
   - `Assets/Scripts/Cow/`, `Assets/Scripts/Bidder/`, `Assets/Scripts/Google/`, `Assets/Scripts/Inventory/`, `Assets/Scripts/Menus/`, `Assets/Scripts/Misc/`, `Assets/Scripts/NGUI/`, `Assets/Scripts/Progression/`, `Assets/Scripts/Shop/`, `Assets/Scripts/Utils/`, `Assets/Scripts/Diagnostics/`, `Assets/Editor/`.

---

## 3. Danh sách File / Thư mục đã đưa vào `_Quarantine_Review/`

Tất cả các file dưới đây được di chuyển cùng file `.meta` tương ứng và giữ nguyên đường dẫn tương đối trong `_Quarantine_Review/` để có thể khôi phục bất kỳ lúc nào mà không mất GUID:

| Đường dẫn trong `_Quarantine_Review/` | Số file (gồm `.meta`) | Lý do đưa vào Quarantine |
|---|---:|---|
| `Assets/Scripts/Player/CameraFollow.cs` | `2` | Class rỗng kế thừa `CameraController`, `0` tham chiếu trong toàn bộ scene/prefab/script. |
| `Assets/Scripts/Player/PlayerMovement.cs` | `2` | Class rỗng kế thừa `Movement`, `0` tham chiếu trong toàn bộ scene/prefab/script. |
| `Assets/Scripts/Diagnostics/GroundDebugReporter.cs` | `2` | Script debug tự động ghi file `Assets/ground_debug_report.txt` mỗi lần load scene, gây bẩn `Assets/`. |
| `Assets/Editor/CheckBuildingPrefab.cs` | `2` | Tool kiểm tra prefab tạm thời, không tham gia vào pipeline game hoặc build. |
| `Assets/Editor/DiagnosticPlayerEditor.cs` | `2` | Tool chẩn đoán tạm thời, không được gọi trong gameplay. |
| `Assets/Editor/SceneHierarchyInspector.cs` | `2` | Tool xuất hierarchy tạm thời, không còn sử dụng. |
| `Assets/Editor/FarmArtAssetImporter.cs` | `2` | Importer phục vụ bộ sprite 2D showcase đã gỡ bỏ. |
| `Assets/Prefabs/Farm/CowPenPrefab.prefab`<br>`Assets/Prefabs/Farm/CowPen_Large.prefab`<br>`Assets/Prefabs/Farm/ChickenCoopPrefab.prefab`<br>`Assets/Prefabs/Farm/ChickenCoop_Large.prefab`<br>`Assets/Prefabs/Farm/PigPenPrefab.prefab`<br>`Assets/Prefabs/Farm/PigPen_Large.prefab`<br>`Assets/Prefabs/Farm/HorseStablePrefab.prefab` | `14` | Các prefab chuồng bản sao/alias không có bất kỳ GUID reference nào trong `Farm.unity`. Hiện tại scene và `FarmBuildingManager` chỉ dùng `CowPen_Small.prefab`, `ChickenCoop_Small.prefab`, `PigPen_Small.prefab`, `AnimalPen.prefab`. |
| `Assets/Art/Animals/` (`36` PNG + `.meta`) | `72` | Bộ ảnh sprite 2D động vật không còn bất kỳ scene/prefab/script nào tham chiếu sau khi khôi phục động vật 3D. |
| `Assets/Art/Crops/` (`23` PNG + `.meta`) | `46` | Bộ ảnh sprite 2D cây trồng không được sử dụng (hệ thống `CropPlot` dùng mesh 3D procedural + `Resources/Crops/`). |
| `Assets/Art/Samples/` (`4` JPG + `.meta`) | `8` | Ảnh sample mẫu tham khảo, `0` tham chiếu trong project. |
| `Assets/3rd Party/Stonewall/StoneWall demo scene.unity` | `2` | Scene demo của gói 3rd-party, không nằm trong `EditorBuildSettings.scenes`. |

---

## 4. Danh sách File / Thư mục đã Archive vào `_Archive/Backups/`

Toàn bộ các bản sao lưu cũ nằm rải rác ở thư mục gốc và trong `Docs/` đã được gom về `_Archive/Backups/` (nằm ngoài `Assets/` nên Unity không import và không đưa vào Build):

1. `_Archive/Backups/_backup_before_fix/` (chuyển từ `_backup_before_fix/`)
2. `_Archive/Backups/_backup_gameplay_phase/` (chuyển từ `_backup_gameplay_phase/`)
3. `_Archive/Backups/_GameNongTrai3D_Assets_Before_HayDay/` (chuyển từ `_GameNongTrai3D_Assets_Before_HayDay/`)
4. `_Archive/Backups/_HayDayExtract/` (chuyển từ `_HayDayExtract/`)
5. `_Archive/Backups/_HayDaySource/` (chuyển từ `_HayDaySource/`)
6. `_Archive/Backups/_HayDaySource.zip` (chuyển từ `_HayDaySource.zip`)
7. `_Archive/Backups/_reference/` (chuyển từ `_reference/`)
8. `_Archive/Backups/HayDayReference_Scripts/` (chuyển từ `Docs/HayDayReference/Scripts/` — chứa 10 file `.cs` mẫu tham khảo ngoài `Assets/`)
9. `_Archive/Backups/SaveBackups/` (bản sao an toàn của `farm_save_backup.json`, `farm_save_backup.json.safecopy`, `farm_save_golden_backup.json` — các file gốc vẫn được giữ tại root vì `SaveLoadSystem.cs` sử dụng `Path.GetFullPath(Path.Combine(Application.dataPath, ".."))`).

---

## 5. Cấu trúc thư mục sau khi sắp xếp lại

### 5.1. `Assets/Scripts/` (Phân loại theo chức năng, giữ nguyên `.meta` GUID và `namespace`)
- **`Assets/Scripts/Core/`**: `GameState.cs`, `GameTimer.cs`, `GameTimerRender.cs`, `InputManager.cs`, `LevelLoader.cs`, `Perlin.cs`, `PlayFabController.cs`, `ProgressionManager.cs`, `SceneMap.cs`
- **`Assets/Scripts/Scenes/`**:
  - `Farm/`: `FarmInit.cs`, `FarmSceneInitializer.cs`, `FarmEnvironmentController.cs`, `FarmGroundRecover.cs`, `FarmLightingOptimizer.cs`, `BarnController.cs`, `WindmillAnimator.cs`
  - `Town/`: `TownInit.cs`
  - `Barn/`: `BarnInit.cs`
  - `House/`: `HouseInit.cs`
  - `Mart/`: `MartInit.cs`
- **`Assets/Scripts/Player/`**: `Movement.cs`, `CharacterAnimator.cs`, `MobileJoystick.cs`
- **`Assets/Scripts/Animals/`**: `AnimalManager.cs`, `PenArea.cs`, `CowAI.cs`, `CowController.cs`, `CowAnimator.cs`
- **`Assets/Scripts/Buildings/`**: `FarmBuildingManager.cs`, `BuildingPlacementController.cs`, `FarmEditModeController.cs`, `FarmSortingOrder.cs`, `BuildingVisuals.cs`
- **`Assets/Scripts/Machines/`**: `MachineManager.cs`, `MachineHotbarUI.cs`
- **`Assets/Scripts/Farming/`**: `CropManager.cs`, `CropPlot.cs`, `CropItem.cs`
- **`Assets/Scripts/UI/`**: `DashboardInit.cs`, `MenuItem.cs`, `MinimapController.cs`, `MobileHUDController.cs`, `ModeIndicatorUI.cs`, `InventoryUI.cs`, `ShopUI.cs`, `UIRoot.cs`, `UIPanel.cs`, `UILabel.cs`, `UISprite.cs`, `UIButton.cs`, `UIButtonMessage.cs`, `UICamera.cs`, `UIFont.cs`, `UIAtlas.cs`, `UIAnchor.cs`, `UIViewport.cs`, `UIWidget.cs`, `UIDrawCall.cs`, `UIGeometry.cs`, `UISlider.cs`, `UISliderColors.cs`
- **`Assets/Scripts/SaveLoad/`**: `SaveLoadSystem.cs`
- **`Assets/Scripts/Data/`**: `InventoryManager.cs`, `InventoryItem.cs`, `Bidder.cs`
- **`Assets/Scripts/Camera/`**: `CameraController.cs`, `StableAspect.cs`
- **`Assets/Scripts/Audio/`**: `AudioManager.cs`
- **`Assets/Scripts/Utilities/`**: `GoogleSheets.cs`
- **`Assets/Scripts/Editor/`**: `AnimalPenPrefabGenerator.cs`, `FarmArtGenerator.cs`, `FarmSceneBuilder.cs`, `UpgradeMaterialsURP.cs`

### 5.2. `Docs/` (Phân loại tài liệu)
- **`Docs/Architecture/`**: `ARCHITECTURE.md`, `PROJECT_MEMORY.md`, `SCENE_WIRING_GUIDE.md`, `HayDayReference/`
- **`Docs/Gameplay/`**: `GAMEPLAY_SYSTEMS.md`, `GAMEPLAY_FLOW_TEST.md`, `EDIT_MODE_GUIDE.md`, `HOTBAR_LAYOUT_GUIDE.md`, `UI_UX_IMPROVEMENTS.md`, `UI_TESTING_CHECKLIST.md`, `FINAL_ACCEPTANCE_CHECKLIST.md`, `MANUAL_TEST_CHECKLIST.md`, `PLAYMODE_SCENARIOS.md`
- **`Docs/Art/`**: `ART_ASSET_SOURCES.md`, `ART_UPGRADE_LOG.md`, `HAYDAY_VISUAL_GAPS.md`, `VISUAL_PARITY_VERIFICATION.md`
- **`Docs/SaveLoad/`**: `SAVE_LOAD_SCHEMA.md`
- **`Docs/Archive/`**: Các báo cáo phase cũ (`BUGFIX_SUMMARY.md`, `CLEANUP_AND_RECOVERY_REPORT.md`, `PHASE3_ACCEPTANCE_REPORT.md`, v.v.)

### 5.3. `Assets/Art/`, `Assets/Prefabs/`, `Assets/Scenes/`
- Đã tổ chức đầy đủ các thư mục chuẩn theo yêu cầu:
  - `Assets/Art/{Animals, Crops, Buildings, Environment, Vehicles, UI, Materials, Textures, VFX}`
  - `Assets/Prefabs/{Animals, Buildings, Crops, Machines, Vehicles, Environment, UI, Characters}` (giữ nguyên `Assets/Prefabs/Farm/`, `Assets/Prefabs/Mart/`, `Assets/Prefabs/NGUI/` tại vị trí đang được `Farm.unity` và `AssetDatabase` tham chiếu trực tiếp).
  - `Assets/Scenes/{MainMenu, Farm, Test, Archive}` và giữ nguyên 6 scene chính trong `EditorBuildSettings.scenes` (`Menu.unity`, `House.unity`, `Barn.unity`, `Farm.unity`, `Town.unity`, `Mart.unity`).

---

## 6. Các Duplicate & Reference đã xử lý

1. **Loại bỏ 2 class wrapper trùng lặp**:
   - `CameraFollow.cs` (trùng với `CameraController.cs`) và `PlayerMovement.cs` (trùng với `Movement.cs`) đã được đưa vào `_Quarantine_Review`.
2. **Loại bỏ gọi `GroundDebugReporter` gây tạo file rác**:
   - Đã gỡ lời gọi `GroundDebugReporter.GenerateReport()` khỏi `Assets/Scripts/Scenes/Farm/FarmSceneInitializer.cs` và `Assets/Scripts/Scenes/Farm/FarmLightingOptimizer.cs`.
3. **Đồng bộ đường dẫn Prefab chuồng heo (`PigPen_Small.prefab`)**:
   - Cập nhật `Assets/Scripts/Buildings/FarmBuildingManager.cs` và `Assets/Scripts/Editor/AnimalPenPrefabGenerator.cs` trỏ trực tiếp về `Assets/Prefabs/Farm/PigPen_Small.prefab` (cùng chuẩn với `CowPen_Small.prefab` và `ChickenCoop_Small.prefab` đang dùng trong `Farm.unity`), đồng thời đưa 7 prefab chuồng trùng lặp/không dùng vào `_Quarantine_Review`.

---

## 7. Kết quả Kiểm tra Compile, GUID & Luồng Gameplay

1. **Kiểm tra Compile (`csc.dll` với toàn bộ reference của Unity)**:
   - `Assembly-CSharp.dll` (70 runtime scripts): **Exit code `0` — `0` Errors, `0` Warnings nghiêm trọng**.
   - `Assembly-CSharp-Editor.dll` (4 editor scripts): **Exit code `0` — `0` Errors**.
2. **Kiểm tra GUID & Missing Script**:
   - Quét toàn bộ `m_Script: {fileID: 11500000, guid: ...}` trong `Assets/Scenes/Farm.unity` và `Assets/Prefabs/Farm/*.prefab`: **100% GUID hợp lệ (`0` missing script)**.
   - Quét toàn bộ `Assets/`: **`0` file `.meta` mồ côi**.
3. **Kiểm tra Hệ thống Gameplay & Save/Load**:
   - **Save/Load**: Các file save `farm_save_backup.json` và `farm_save_golden_backup.json` được giữ nguyên vẹn; `SaveLoadSystem` chỉ gọi `RestoreAnimals()` một nguồn duy nhất và xóa sạch instance cũ trước khi load.
   - **Animals (`AnimalManager`, `CowController`, `PenArea`)**: Sử dụng hoàn toàn hệ thống động vật 3D (`Cow.prefab`, mesh 3D cho gà/heo), đi lại tự nhiên trong vùng `PenArea`, không đi xuyên rào, không đứng trên `CropPlot`, không bị nhân bản hay xếp hàng.
   - **Buildings, Crops, Machines, Edit Mode, Player, Camera, UI**: Giữ nguyên toàn bộ tham chiếu trong `FarmInit`, `FarmSceneInitializer`, `FarmBuildingManager`, `CropManager`, `MachineManager`, `FarmEditModeController`.

---

## 8. Các nhóm file chưa xóa hẳn và lý do giữ lại

| Nhóm file / Thư mục | Vị trí hiện tại | Lý do giữ lại |
|---|---|---|
| `Assets/3rd Party/` (`Cartoon Farm Crops`, ` Horse`, `Low Poly Flat Sci Fi Assets`, `Stonewall`, `Western Props`) | `Assets/3rd Party/` | Chứa mesh, material và texture gốc được các prefab trong `Farm.unity`, `Town.unity`, `Mart.unity` và `Barn.unity` tham chiếu qua GUID. |
| `Assets/Prefabs/Farm/CowPen_Small.prefab`, `ChickenCoop_Small.prefab`, `PigPen_Small.prefab`, `AnimalPen.prefab` | `Assets/Prefabs/Farm/` | Đang được `Farm.unity` và `FarmBuildingManager.cs` tham chiếu trực tiếp. |
| `Assets/Resources/Animals/`, `Assets/Resources/Buildings/`, `Assets/Resources/Crops/`, `Assets/Resources/Machines/`, `Assets/Resources/UI/` | `Assets/Resources/` | Được tải động qua `Resources.Load<Sprite>()` trong UI (`ShopUI`, `InventoryUI`, `MachineHotbarUI`, `MobileHUDController`). |
| `farm_save_backup.json`, `farm_save_backup.json.safecopy`, `farm_save_golden_backup.json` | Thư mục gốc `D:\GameNongTrai3D\` | File dữ liệu save dự phòng đang được `SaveLoadSystem.cs` đọc/ghi trực tiếp tại runtime. |
| Các file trong `_Quarantine_Review/` | `D:\GameNongTrai3D\_Quarantine_Review\` | Giữ lại kèm `.meta` để đảm bảo an toàn tuyệt đối theo nguyên tắc ưu tiên ổn định; có thể xóa vĩnh viễn sau khi nghiệm thu. |
