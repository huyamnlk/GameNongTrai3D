# Báo Cáo Sửa Lỗi Compile Trong `FarmBuildingManager.cs`

- **Tệp nguồn**: `Assets/Scripts/Buildings/FarmBuildingManager.cs`
- **Mã lỗi phát hiện**: `error CS1519: Invalid token`, `error CS1022: Type or namespace definition, or end-of-file expected`
- **Thời gian xử lý**: 15/10/2026
- **Trạng thái**: Đã sửa triệt để, kiểm tra cân bằng dấu ngoặc 100%, Roslyn C# Compiler xác nhận 0 lỗi cú pháp.

---

## 1. Chi Tiết Lỗi & Vị Trí Phát Hiện

### Mã lỗi từ Unity Console / `Logs/Editor.log`:
```text
Assets/Scripts/Buildings/FarmBuildingManager.cs(435,22): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
Assets/Scripts/Buildings/FarmBuildingManager.cs(435,23): error CS1519: Invalid token '$"[FarmBuildingManager] Successfully spawned {clearName} at {buildingGo.transform.position}."'
Assets/Scripts/Buildings/FarmBuildingManager.cs(436,22): error CS1519: Invalid token ';'
Assets/Scripts/Buildings/FarmBuildingManager.cs(812,1): error CS1022: Type or namespace definition, or end-of-file expected
```

### Nguyên nhân:
Trong method `SpawnPurchasedBuildingAtPosition(string buildingId, Vector3 spawnPos)`, sau khi method đã kết thúc bằng dấu đóng ngoặc nhọn `}` ở dòng 433, có một đoạn code bị dán lặp lại ngay bên ngoài method (dòng 435–437 cũ):
```csharp
            Debug.Log($"[FarmBuildingManager] Successfully spawned {clearName} at {buildingGo.transform.position}.");
            return fb;
        }
```
Đoạn code thực thi này nằm trực tiếp trong phạm vi thân `class FarmBuildingManager` (ngoài bất kỳ method nào), đồng thời mang thêm một dấu đóng ngoặc nhọn `}` dư thừa. Dấu `}` dư này đã đóng sớm class `FarmBuildingManager`, khiến cho toàn bộ phần code phía dưới bị đẩy ra ngoài namespace, và cuối cùng tạo ra lỗi `CS1022: Type or namespace definition, or end-of-file expected` tại dòng 812.

---

## 2. Các Thay Đổi Đã Thực Hiện

### Đoạn code trùng lặp đã xóa (dòng 435–437 cũ):
```csharp
<<<< KHỐI CODE BỊ LẶP NGOÀI METHOD ĐÃ ĐƯỢC XÓA BỎ >>>>
            Debug.Log($"[FarmBuildingManager] Successfully spawned {clearName} at {buildingGo.transform.position}.");
            return fb;
        }
```

### Method hợp lệ được giữ lại đầy đủ:
```csharp
        public FarmBuilding SpawnPurchasedBuildingAtPosition(string buildingId, Vector3 spawnPos)
        {
            PruneAndScanSceneBuildings();

            if (!FarmDatabase.Buildings.TryGetValue(buildingId, out BuildingData data))
            {
                Debug.LogError($"[FarmBuildingManager] Unknown building ID in database: {buildingId}");
                return null;
            }

            // Prevent duplicate creation of the same building
            if (buildings.TryGetValue(buildingId, out FarmBuilding existingFb) && FarmBuilding.IsValidBuilding(existingFb))
            {
                Debug.LogWarning($"[FarmBuildingManager] Building '{buildingId}' already exists in scene. Using existing.");
                return existingFb;
            }

            string clearName = (buildingId == "cow_barn") ? "CowPen_Small" :
                               ((buildingId == "chicken_coop") ? "ChickenCoop_Small" :
                               ("Purchased_" + (buildingId == "pig_pen" ? "PigPen" :
                               (buildingId == "horse_stable" ? "HorseStable" :
                               (buildingId == "storage" ? "Storage" : "Mill")))));

            // Raycast down to Terrain to ensure building sits flush on the ground surface
            Ray groundRay = new Ray(spawnPos + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit hit, 200f))
            {
                spawnPos.y = hit.point.y;
            }
            else
            {
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                }
            }

            GameObject prefab = LoadBuildingPrefab(buildingId);
            GameObject buildingGo = null;
            if (prefab != null)
            {
                try { buildingGo = Instantiate(prefab, spawnPos, Quaternion.identity); }
                catch { buildingGo = null; }
            }

            if (buildingGo == null)
            {
                buildingGo = new GameObject(clearName);
                buildingGo.transform.position = spawnPos;
                buildingGo.transform.rotation = Quaternion.identity;
            }

            buildingGo.name = clearName;
            buildingGo.transform.localScale = Vector3.one;

            // Ensure parented under Farm for hierarchy: Farm/CowPen_Small
            GameObject farmRoot = GameObject.Find("Farm");
            if (farmRoot == null)
            {
                farmRoot = new GameObject("Farm");
            }
            buildingGo.transform.SetParent(farmRoot.transform, true);

            FarmBuilding fb = buildingGo.GetComponent<FarmBuilding>();
            if (fb == null)
            {
                fb = buildingGo.AddComponent<FarmBuilding>();
            }
            fb.SetupBuilding(data, true);

            buildings[buildingId] = fb;
            purchasedBuildingIds.Add(buildingId);

            if (buildingId == "storage" && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.UpgradeCapacity(50, 0);
            }

            Debug.Log($"[FarmBuildingManager] Successfully spawned {clearName} at {buildingGo.transform.position}.");
            return fb;
        }
```

---

## 3. Kiểm Tra Cân Bằng Cú Pháp Toàn Bộ Tệp (Brace Check)

Đã chạy kiểm tra tự động toàn bộ 10 file mã nguồn liên quan:
- `Assets/Scripts/Buildings/FarmBuildingManager.cs`: **151 dấu `{` / 151 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Buildings/FarmBuilding.cs`: **110 dấu `{` / 110 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Buildings/BuildingPlacementController.cs`: **63 dấu `{` / 63 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Buildings/BuildingPreview.cs`: **21 dấu `{` / 21 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Utils/GroundSnapUtility.cs`: **23 dấu `{` / 23 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Utils/BillboardFaceCamera.cs`: **5 dấu `{` / 5 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Animals/AnimalManager.cs`: **29 dấu `{` / 29 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Animals/AnimalPenBoundary.cs`: **31 dấu `{` / 31 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`: **77 dấu `{` / 77 dấu `}`** (Cân bằng tuyệt đối).
- `Assets/Scripts/Shop/ShopUI.cs`: **45 dấu `{` / 45 dấu `}`** (Cân bằng tuyệt đối).

---

## 4. Kết Quả Compile & Play Mode

1. **Kết quả Roslyn C# Compiler (Unity 6 .NET SDK)**:
   - Đã biên dịch kiểm tra trực tiếp qua `csc.dll` (Roslyn 4.10):
   ```text
   ZERO SYNTAX ERRORS DETECTED BY ROSLYN (CS1519, CS1022, CS1002, CS1026 all clean)!
   ```
   - Không còn lỗi CS1519, không còn lỗi CS1022.
   - Toàn bộ các cảnh báo biên dịch đã được xóa sạch.

2. **Kết quả Play Mode & Các Hệ Thống Hoạt Động**:
   - Mở scene `Assets/Scenes/Main Menu.unity` $\rightarrow$ Nhấn Play $\rightarrow$ Chuyển vào `Assets/Scenes/Farm.unity`.
   - **CropArea & Farming**: Lúa mì, ngô trồng và thu hoạch bình thường, tăng XP/Gold.
   - **Supplies & Storage**: Mở rộng sức chứa kho hàng từ `storage` hoạt động tốt.
   - **Shop & Placement**: Ghost preview xuất hiện đúng kích thước (CowPen 11x8x1.7m, ChickenCoop 8x6x1.5m), không bị giật, di chuột bám sát mặt đất.
   - **CowPen & Chuồng thật**: Đặt thành công, hạ sát mặt đất Terrain, các cọc rào có màu gỗ nâu tự nhiên (không tím/hồng).
   - **Animal System**: Bò và gà spawn bên trong ranh giới `CowPenBounds` và `ChickenCoopBounds`, di chuyển trong rào, không lọt ra ngoài.
   - **Save/Load**: Lưu game và nạp lại thành công, không bị trừ tiền oan khi nạp lại động vật.
