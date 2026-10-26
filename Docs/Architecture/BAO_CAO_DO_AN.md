# ĐỒ ÁN: XÂY DỰNG GAME NÔNG TRẠI 2D TRÊN THIẾT BỊ DI ĐỘNG (LẤY CẢM HỨNG TỪ HAY DAY)

## 1. Giới thiệu đề tài
- **Tên đề tài**: Xây dựng game nông trại 2D trên thiết bị di động lấy cảm hứng từ Hay Day.
- **Mục tiêu**: Tạo phiên bản đồ án hoàn chỉnh, tập trung vào vòng lặp gameplay chính:
  $$\text{Trồng cây} \rightarrow \text{Sinh trưởng} \rightarrow \text{Thu hoạch} \rightarrow \text{Chế biến} \rightarrow \text{Bán hàng} \rightarrow \text{Nhận GOLD \& EXP} \rightarrow \text{Mở rộng đất} \rightarrow \text{Trồng tiếp}$$
- **Nền tảng mục tiêu**: Mobile (Android/iOS) thiết kế theo chiều dọc (Portrait), đồng thời hỗ trợ chơi mượt mà trên PC bằng chuột.
- **Công nghệ**: Unity 2D (Unity 6 / 2022+), ngôn ngữ C#, UGUI + TextMeshPro, hệ thống dữ liệu kiểu ScriptableObject, lưu game định dạng JSON.
- **Điểm nổi bật**: Toàn bộ đồ họa, âm thanh hiệu ứng (SFX sóng âm) và giao diện được khởi tạo tự động bằng mã nguồn (`GameInitializer`, `SpriteGenerator`, `AudioManager`), hoàn toàn không vi phạm bản quyền và sẵn sàng thay thế bằng sprite/audio ngoài bất kỳ lúc nào.

---

## 2. Cấu trúc thư mục dự án

```
D:\GameNongTrai3D\
├── Assets\
│   ├── Scenes\
│   │   └── MainScene.unity           # Scene chính (chứa Camera, EventSystem, GameInitializer)
│   ├── Scripts\
│   │   ├── Data\                     # Lớp dữ liệu và cấu trúc ScriptableObject
│   │   │   ├── CropData.cs           # Cấu hình 5 loại cây trồng
│   │   │   ├── AnimalData.cs         # Cấu hình 3 loại vật nuôi (Gà, Bò, Heo)
│   │   │   ├── RecipeData.cs         # Cấu hình công thức chế biến
│   │   │   ├── QuestData.cs          # Cấu hình nhiệm vụ
│   │   │   ├── LevelData.cs          # Bảng cấp độ & EXP yêu cầu
│   │   │   └── GameSaveData.cs       # Cấu trúc JSON serialize để lưu game
│   │   ├── Gameplay\                 # Logic tương tác trên bản đồ thế giới
│   │   │   ├── FarmPlot.cs           # Ô đất trồng cây (4 giai đoạn lớn, timer, thu hoạch)
│   │   │   ├── AnimalSlot.cs         # Chuồng nuôi (cho ăn, sản xuất trứng/sữa, thu hoạch)
│   │   │   ├── ProductionSlot.cs     # Dây chuyền sản xuất bánh mì trong nhà máy
│   │   │   ├── FarmArea.cs           # Khu vực đất có thể mở khóa bằng GOLD + Level
│   │   │   ├── BuildingInteract.cs   # Chạm vào Nhà kho / Shop / Nhà máy trên map
│   │   │   ├── FarmerController.cs   # Nông dân tự động đi dạo & đi tới ô đất khi thao tác
│   │   │   ├── CameraController.cs   # Điều khiển kéo pan & pinch zoom (Touch + Mouse)
│   │   │   └── GameInitializer.cs    # Tự động dựng toàn bộ Scene, Map, UI và Managers
│   │   ├── Managers\                 # Hệ thống quản lý lõi (Singleton GameManager)
│   │   │   ├── GameManager.cs        # Điều phối trung tâm, kinh tế GOLD, reset game
│   │   │   ├── FarmManager.cs        # Quản lý đất đai và đăng ký ô đất mở khóa
│   │   │   ├── CropManager.cs        # Trồng cây, thu hoạch, offline progress
│   │   │   ├── AnimalManager.cs      # Mua vật nuôi, cho ăn, thu trứng, offline progress
│   │   │   ├── ProductionManager.cs  # Chế biến công thức, sản xuất, offline progress
│   │   │   ├── InventoryManager.cs   # Sức chứa kho, kiểm tra đầy kho, nâng cấp kho
│   │   │   ├── ShopManager.cs        # Bán nông sản lấy GOLD + EXP
│   │   │   ├── QuestManager.cs       # Theo dõi 3 nhiệm vụ mẫu và phát thưởng
│   │   │   ├── LevelManager.cs       # EXP, thăng cấp, mở khóa nội dung
│   │   │   ├── SaveManager.cs        # Lưu/tải JSON cục bộ, tính thời gian offline
│   │   │   ├── AudioManager.cs       # Tổng hợp âm thanh SFX tự động bằng code (không bản quyền)
│   │   │   └── UIManager.cs          # HUD dọc, popups, scrollview, thông báo
│   │   └── Utils\                    # Tiện ích bổ trợ
│   │       ├── SpriteGenerator.cs    # Sinh texture/sprite procedural (cây, thú, nhà, UI)
│   │       ├── BlinkEffect.cs        # Hiệu ứng nhấp nháy cho chỉ báo sẵn sàng
│   │       ├── EffectSpawner.cs      # Chữ bay (+EXP, +GOLD), nảy pop, bụi hạt particle
│   │       └── InputHelper.cs        # Chống chạm xuyên qua UI xuống bản đồ
│   └── Resources\                    # Thư mục chứa tài nguyên runtime (nếu mở rộng)
├── Packages\
│   └── manifest.json                 # UGUI, TextMeshPro, 2D Physics, Audio
├── ProjectSettings\                  # Cấu hình dự án Unity
│   ├── ProjectVersion.txt
│   └── EditorBuildSettings.asset
└── Docs\                             # Tài liệu đồ án
    ├── BAO_CAO_DO_AN.md              # Báo cáo chi tiết đồ án
    ├── HUONG_DAN_CHAY_PROJECT.md     # Hướng dẫn mở và chạy trên Unity Editor / PC
    ├── HUONG_DAN_BUILD_ANDROID.md    # Hướng dẫn build file APK Android
    └── SO_DO_KIEN_TRUC_HE_THONG.md   # Sơ đồ kiến trúc & vòng lặp gameplay
```

---

## 3. Danh sách các tính năng hoàn thành

| STT | Tính năng | Trạng thái | Mô tả chi tiết |
|---|---|:---:|---|
| 1 | **Bản đồ nông trại 2D** | Hoàn thành | Lưới ô đất, nhà kho, cửa hàng, nhà máy, chuồng gà, khu đất mở khóa, cây xanh trang trí |
| 2 | **Hệ thống trồng cây (5 loại)** | Hoàn thành | Lúa mì (30s), Ngô (45s), Cà rốt (40s), Cà chua (50s), Khoai tây (60s). Đầy đủ 4 giai đoạn sinh trưởng, timer đếm ngược, thu hoạch |
| 3 | **Hệ thống chăn nuôi** | Hoàn thành | Triển khai hoàn chỉnh Gà (ăn 1 Ngô $\rightarrow$ đẻ 1 Trứng sau 45s); Chuẩn bị cấu hình mở rộng cho Bò (sữa) và Heo (thịt); ràng buộc loại chuồng phù hợp |
| 4 | **Hệ thống chế biến** | Hoàn thành | Công thức Nhà máy: 1 Trứng + 2 Lúa mì $\rightarrow$ 1 Bánh mì (60s). Hiển thị tiến độ và nhận sản phẩm vào kho |
| 5 | **Hệ thống kho chứa** | Hoàn thành | Quản lý dung lượng, kiểm tra đầy kho (ngăn trồng/thu hoạch), thông báo tiếng Việt, nút nâng cấp sức chứa bằng GOLD |
| 6 | **Cửa hàng và bán hàng** | Hoàn thành | Bán nông sản (Lúa mì, Ngô, Cà rốt, Cà chua, Khoai tây, Trứng, Bánh mì...), có nút "Bán 1" và "Bán hết", cộng GOLD + EXP |
| 7 | **Hệ thống GOLD & EXP / Level** | Hoàn thành | HUD trên cùng: thanh EXP, cấp độ, số GOLD; hiệu ứng lên level mở khóa tính năng |
| 8 | **Hệ thống nhiệm vụ** | Hoàn thành | 3 nhiệm vụ: Thu hoạch 20 lúa mì, Bán 10 cà chua, Sản xuất 3 bánh mì. Thanh tiến độ trực quan, nút nhận thưởng |
| 9 | **Mở khóa đất đai** | Hoàn thành | Khu đất mới yêu cầu Level 3 + 300 GOLD; overlay khóa với thông tin rõ ràng; mở khóa lập tức kích hoạt thêm 4 ô đất mới |
| 10 | **Lưu & tải game + Offline** | Hoàn thành | Lưu định dạng JSON vào `persistentDataPath`, tự động lưu mỗi 60s và khi thoát; tính thời gian offline để cây/thú tiếp tục sinh trưởng |
| 11 | **Giao diện di động (Portrait)** | Hoàn thành | Tỉ lệ 9:16 dọc điện thoại, thanh trạng thái trên cùng, 4 nút chức năng lớn dưới đáy, cửa sổ pop-up bo góc thân thiện |
| 12 | **Điều khiển kép (Touch & PC)** | Hoàn thành | Chạm cảm ứng 1 ngón / kéo chuột phải để di chuyển camera; pinch zoom 2 ngón / con cuộn chuột để phóng to/thu nhỏ |
| 13 | **Hiệu ứng đồ họa (VFX)** | Hoàn thành | Chữ nổi bay lên (+2 Lúa mì, +EXP, +GOLD), nảy pop khi chạm, hạt bụi tung tóe khi trồng/thu hoạch, chỉ báo nhấp nháy |
| 14 | **Âm thanh hiệu ứng (SFX)** | Hoàn thành | Tự tổng hợp bằng code: tiếng click, trồng cây, thu hoạch, nhận tiền vàng, thăng cấp, còi báo lỗi, âm thanh nhà máy |
| 15 | **Nhân vật nông dân** | Hoàn thành | Đi dạo quanh trang trại, tự động bước tới vị trí ô đất / chuồng khi người chơi thao tác |

---

## 4. Kiểm chứng tính đúng đắn kỹ thuật
Dự án đã được biên dịch thử nghiệm trực tiếp qua Unity Editor 6000.6.5f1 batchmode:
- Mã thoát: `0` (Success).
- Trạng thái biên dịch C#: Không có lỗi cú pháp, không có lỗi thiếu namespace, không có xung đột reference.
- Scene chính `Assets/Scenes/MainScene.unity` đã được gắn `GameInitializer` và cấu hình trong `ProjectSettings/EditorBuildSettings.asset`.
