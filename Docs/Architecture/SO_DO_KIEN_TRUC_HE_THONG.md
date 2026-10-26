# SƠ ĐỒ KIẾN TRÚC HỆ THỐNG & VÒNG LẶP GAMEPLAY

Tài liệu này mô tả chi tiết kiến trúc phần mềm, mô hình phân tầng, mối quan hệ giữa các module quản lý và lưu đồ vòng lặp gameplay chính của dự án Game Nông Trại 2D.

---

## 1. Vòng lặp Gameplay chính (Core Gameplay Loop)

```
       +-------------------------------------------------------+
       |                                                       |
       v                                                       |
  [ 1. Mua hạt giống ] ---> Dùng GOLD mua hạt giống trong Shop/Popup
       |
       v
  [ 2. Trồng cây ]     ---> Chọn ô đất trống, trừ hạt giống / GOLD
       |
       v
  [ 3. Chờ sinh trưởng ] -> Cây lớn dần qua 4 giai đoạn (có Timer)
       |
       v
  [ 4. Thu hoạch ]     ---> Nhận nông sản thô + EXP (kiểm tra đầy kho)
       |
       +---------------------------------------+
       |                                       |
       v                                       v
[ 5A. Chế biến ]                        [ 5B. Bán nông sản thô ]
Nhà máy: Trứng + Lúa mì -> Bánh mì       Cửa hàng: Bán trực tiếp
Gà: Cho ăn Ngô -> Thu trứng                    |
       |                                       |
       v                                       |
[ 5C. Thu hoạch thành phẩm ]                   |
Nhận Bánh mì, Trứng vào kho                    |
       |                                       |
       +-------------------+-------------------+
                           |
                           v
                    [ 6. Bán hàng ]
                 Bán Bánh mì, Trứng, Nông sản
                           |
                           v
               [ 7. Nhận GOLD & EXP ]
             + Tích lũy tiền vàng cho người chơi
             + Tăng EXP -> Lên Cấp (Level Up)
             + Cập nhật tiến độ Nhiệm vụ (Quests)
                           |
                           v
            [ 8. Mở rộng Nông Trại & Nâng cấp ]
             + Mở khóa thêm ô đất mới (Khu đất mới)
             + Nâng cấp sức chứa Nhà kho (Storage)
             + Mở khóa giống cây trồng & công thức mới
                           |
                           +---------------------------+ (Lặp lại vòng lặp)
```

---

## 2. Sơ đồ kiến trúc phân tầng (Layered Architecture)

Kiến trúc game được tổ chức theo 4 tầng rõ ràng, đảm bảo tính đóng gói, dễ mở rộng và dễ bảo trì:

```
+-------------------------------------------------------------------------+
|                         TẦNG GIAO DIỆN & INPUT                          |
|  - UIManager (HUD, Popups chọn hạt giống, Kho, Shop, Nhà máy, Nhiệm vụ)  |
|  - CameraController (Pan & Pinch Zoom / Chuột)                         |
|  - InputHelper (Chống chạm xuyên qua UI xuống thế giới)                 |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
|                       TẦNG ĐIỀU PHỐI (MANAGERS)                         |
|                           [ GameManager ]                               |
|                               (Singleton)                               |
|    +--------------------+-------------------+--------------------+      |
|    | FarmManager        | CropManager       | AnimalManager      |      |
|    | ProductionManager  | InventoryManager  | ShopManager        |      |
|    | QuestManager       | LevelManager      | SaveManager        |      |
|    | AudioManager       |                   |                    |      |
|    +--------------------+-------------------+--------------------+      |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
|                      TẦNG THỰC THỂ THẾ GIỚI (WORLD)                     |
|  - FarmPlot (Ô đất canh tác)          - AnimalSlot (Chuồng nuôi gà/thú) |
|  - ProductionSlot (Dây chuyền nhà máy) - FarmArea (Khu đất mở khóa)     |
|  - BuildingInteract (Công trình chạm)  - FarmerController (Nông dân)    |
|  - BlinkEffect (Chỉ báo nhấp nháy)     - EffectSpawner (VFX hạt & chữ)  |
+-------------------------------------------------------------------------+
                                    |
                                    v
+-------------------------------------------------------------------------+
|                         TẦNG DỮ LIỆU & LƯU TRỮ                          |
|  - ScriptableObjects: CropData, AnimalData, RecipeData, QuestData, ...  |
|  - GameSaveData: Lưu trạng thái JSON vào persistentDataPath             |
|  - SpriteGenerator: Sinh procedural sprites không phụ thuộc asset ngoài|
+-------------------------------------------------------------------------+
```

---

## 3. Vai trò chi tiết của các Manager

1. **`GameManager`**:
   - Tâm điểm điều phối, lưu trữ lượng `gold` của người chơi.
   - Quản lý trạng thái Pause / Unpause.
   - Cung cấp hàm `ResetGame()` để chơi lại từ đầu.
2. **`CropManager`**:
   - Quản lý danh sách ô đất `farmPlots` và các loại hạt giống `availableCrops`.
   - Thực hiện logic trồng, trừ tiền giống, cập nhật sinh trưởng và thu hoạch.
   - Tính toán thời gian offline (`ApplyOfflineTime`) để cây tiếp tục lớn khi tắt game.
3. **`AnimalManager`**:
   - Quản lý chuồng gà / chuồng nuôi, trạng thái đói, cho ăn thức ăn tương ứng (ví dụ: Ngô) và thu hoạch trứng.
   - Ràng buộc loại chuồng phù hợp với từng loài động vật.
4. **`ProductionManager`**:
   - Quản lý dây chuyền sản xuất của nhà máy theo công thức (`RecipeData`).
   - Kiểm tra nguyên liệu đầu vào trong kho trước khi bắt đầu sản xuất.
5. **`InventoryManager`**:
   - Quản lý kho chứa với cơ chế giới hạn dung lượng (`maxCapacity`).
   - Kiểm tra chặt chẽ điều kiện đầy kho để cảnh báo người chơi nâng cấp kho hoặc bán bớt hàng.
6. **`ShopManager`**:
   - Định giá nông sản, thực hiện giao dịch bán hàng, quy đổi ra tiền vàng (GOLD) và điểm kinh nghiệm (EXP).
7. **`QuestManager`**:
   - Lưu trữ trạng thái tiến độ các nhiệm vụ mẫu (thu hoạch, bán hàng, chế biến), phát thưởng khi hoàn thành.
8. **`LevelManager`**:
   - Quản lý hệ thống điểm EXP và cấp độ (`currentLevel`).
   - Khi lên cấp mới sẽ phát sự kiện mở khóa nội dung và hiệu ứng pháo hoa.
9. **`SaveManager`**:
   - Đọc/ghi cấu trúc `GameSaveData` thành chuỗi JSON và lưu vào ổ đĩa.
   - Tính toán độ lệch thời gian thực (`DateTime.Now - lastSaveTime`) để mô phỏng sự phát triển của nông trại khi người chơi vắng mặt.
10. **`AudioManager`**:
    - Tự động sinh sóng âm toán học (sine wave/square wave) thành các đoạn SFX độc quyền, không phụ thuộc tài nguyên bên ngoài.
11. **`UIManager`**:
    - Quản lý toàn bộ giao diện Canvas UI di động (HUD, danh sách hạt giống, kho hàng, shop, công thức nhà máy, nhiệm vụ, cài đặt).
