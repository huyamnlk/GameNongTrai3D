# HỆ THỐNG KHO LƯU TRỮ, TIẾN TRÌNH & LƯU GAME (INVENTORY, PROGRESSION & SAVE/LOAD)

Tài liệu chi tiết về hệ thống Kho đồ (`InventorySystem`), Tiến trình người chơi (`PlayerProgression`) và Hệ thống lưu trữ dữ liệu (`SaveLoadSystem`) trong dự án **GameNongTrai3D**.

---

## 1. Hệ thống Kho lưu trữ (Inventory System)

### Kiến trúc Singleton & Tồn tại qua Scene
- Script: `InventorySystem.cs` kế thừa `MonoBehaviour` với thuộc tính `DontDestroyOnLoad(gameObject)`.
- Khi chuyển đổi giữa scene `Farm` và các scene khác (ví dụ: `Mart`, `MainMenu`), dữ liệu vật phẩm trong kho **hoàn toàn được giữ nguyên**.
- Đồng bộ hai chiều với đối tượng `GameController.Instance().player`: số lượng ngũ cốc (`grain`), cỏ khô (`hay`) và cám viên (`pellet`) luôn được cập nhật tương thích.

### Quy định về Sức chứa và Cảnh báo đầy kho
- Sức chứa tối đa mặc định: **150 đơn vị** (`maxStorageCapacity = 150`).
- Khi tổng số lượng vật phẩm trong kho cộng với số lượng sắp thêm vượt quá 150:
  - Hàm `IsFull()` trả về `true`.
  - Trên màn hình hiển thị thông báo đỏ cảnh báo: **`STORAGE FULL / KHO ĐÃ ĐẦY!`**.
  - Hành động thu hoạch cây hoặc thu hoạch sản phẩm vật nuôi sẽ bị chặn cho đến khi người chơi dùng bớt vật phẩm.

### Danh mục Item được hỗ trợ
| ID vật phẩm | Tên hiển thị | Nhóm loại | Số lượng khởi đầu | Mô tả công dụng |
| :--- | :--- | :--- | :--- | :--- |
| `seed_wheat` | Wheat Seed / Hạt Lúa mì | Seed | 10 | Dùng để gieo trồng Lúa mì |
| `seed_corn` | Corn Seed / Hạt Ngô | Seed | 8 | Dùng để gieo trồng Bắp Ngô |
| `seed_carrot` | Carrot Seed / Hạt Cà rốt | Seed | 6 | Dùng để gieo trồng Cà rốt |
| `seed_hay` | Hay Grass Seed / Hạt Cỏ | Seed | 10 | Dùng để gieo trồng Cỏ khô |
| `wheat` | Wheat / Lúa mì | Crop | 0 | Thu hoạch từ ô lúa mì |
| `corn` | Corn / Bắp Ngô | Crop | 0 | Thu hoạch từ ô ngô |
| `carrot` | Carrot / Cà rốt | Crop | 0 | Thu hoạch từ ô cà rốt |
| `grain` / `feed_grain` | Grain / Ngũ cốc | Feed | 10 | Cho bò và gà ăn |
| `hay` / `feed_hay` | Hay / Cỏ khô | Feed | 20 | Cho bò ăn |
| `pellet` / `feed_pellets` | Pellets / Cám viên | Feed | 5 | Dinh dưỡng cao cho vật nuôi |
| `milk` | Fresh Milk / Sữa bò tươi | AnimalProduct | 0 | Sản phẩm thu hoạch từ Bò |
| `egg` | Fresh Eggs / Trứng gà tươi | AnimalProduct | 0 | Sản phẩm thu hoạch từ Chuồng gà |

### Cách mở và sử dụng Kho
1. Bấm nút **`STORAGE`** ở góc trên bên phải màn hình.
2. Bảng kho mở ra hiển thị danh sách các món đồ dưới dạng lưới thẻ màu sinh động (Xanh lá: Hạt giống, Xanh đậm: Nông sản, Tím: Sản phẩm động vật, Nâu: Thức ăn chăn nuôi).
3. Hiển thị thông số dung lượng thực tế: ví dụ `Dung lượng: 69 / 150`.
4. Đóng kho bằng nút **`X`** màu đỏ ở góc trên bên phải cửa sổ kho.

---

## 2. Hệ thống Cấp độ & Kinh nghiệm (Player Progression)

### Cơ chế tính điểm XP và Cấp độ
- Script trung tâm: `PlayerProgression.cs` (Singleton, `DontDestroyOnLoad`).
- **Cấp độ khởi đầu**: Level 1.
- **Công thức tính XP cần cho cấp tiếp theo**:
  $$\text{XP Cần thiết} = \text{Level} \times 50$$
  - Level 1 $\rightarrow$ 2: Cần 50 XP
  - Level 2 $\rightarrow$ 3: Cần 100 XP
  - Level 3 $\rightarrow$ 4: Cần 150 XP
  - ...

### Cơ chế bảo toàn XP dư khi Lên cấp (Overflow Preservation)
- Khi nhận lượng XP lớn đưa tổng XP vượt qua ngưỡng yêu cầu:
  - Cấp độ tự động tăng thêm 1 (`level++`).
  - Lượng XP dư thừa được **giữ lại nguyên vẹn** và tính tiếp vào cấp độ mới:
    $$\text{XP Mới} = \text{XP Hiện Tại} - \text{XP Yêu Cầu}$$
  - Nếu lượng XP quá lớn đủ lên nhiều cấp, vòng lặp tự động xử lý lên nhiều cấp liên tiếp mà không bị mất điểm.

### Bảng cấu hình Điểm XP hành động
| Hành động trong game | Lượng XP thưởng | Popup thông báo hiển thị |
| :--- | :--- | :--- |
| **Gieo hạt bất kỳ** | **+2 XP** | `+2 XP (Trồng Lúa mì / Ngô...)` |
| **Thu hoạch Lúa mì (Wheat)** | **+10 XP** | `+10 XP (Thu hoạch Lúa mì)` |
| **Thu hoạch Bắp Ngô (Corn)** | **+12 XP** | `+12 XP (Thu hoạch Bắp Ngô)` |
| **Thu hoạch Cà rốt (Carrot)** | **+10 XP** | `+10 XP (Thu hoạch Cà rốt)` |
| **Thu hoạch Cỏ khô (Hay)** | **+8 XP** | `+8 XP (Thu hoạch Cỏ khô)` |
| **Cho vật nuôi ăn (Bò / Gà)** | **+3 XP** | `+3 XP (Cho Bò ăn Cỏ khô...)` |
| **Thu hoạch Sữa bò (Milk)** | **+15 XP** | `+15 XP (Thu hoạch Sữa bò tươi)` |
| **Thu hoạch Trứng gà (Egg)** | **+15 XP** | `+15 XP (Thu hoạch Trứng gà tươi)` |

### Hiển thị giao diện Tiến trình (`ProgressionUI`)
- **Level Badge**: Huy hiệu màu vàng cam hiển thị `CẤP 1`, `CẤP 2`...
- **XP Progress Bar**: Thanh tiến trình ngang màu xanh lá mượt mà với số liệu trực tiếp: ví dụ `24 / 50 XP`.
- **Floating Popup Animation**: Khi nhận XP, một thông báo vàng phát sáng trượt nhẹ lên trên và mờ dần trong 1.6 giây.
- **Level Up Banner**: Khi lên cấp, một banner thông báo lớn xuất hiện giữa màn hình: `★ LÊN CẤP 2! ★ Chúc mừng bạn!`.

---

## 3. Hệ thống Lưu và Tải Game (Save & Load System)

### Cơ chế lưu trữ an toàn (Safe Serialization)
- Script: `SaveLoadSystem.cs` kết hợp file `SaveData.cs`.
- **Không lưu trực tiếp tham chiếu Unity Object** (tránh lỗi null reference hoặc dangling pointer khi chuyển scene).
- Dữ liệu được trích xuất thành định dạng cấu trúc thuần túy:
  - Cấp độ (`level`) & Điểm kinh nghiệm (`currentXP`);
  - Danh sách ID vật phẩm và số lượng trong kho;
  - Trạng thái từng ô đất (`plotName`, `state`, `cropId`, `growthTimer`);
  - Trạng thái từng vật nuôi (`animalName`, `hunger`, `happiness`, `productTimer`, `isProductReady`);
  - Dấu mốc thời gian lưu (`saveTimestamp`).
- Sử dụng **2 cơ chế lưu trữ song song**:
  1. Ghi file JSON chuẩn tại đường dẫn: `Application.persistentDataPath/farm_save.json`.
  2. Ghi chuỗi JSON vào `PlayerPrefs` (`key: FARM_JSON_SAVE`) để đảm bảo khả năng tương thích tuyệt đối trên mọi nền tảng (Windows Editor, Android, WebGL).

### Cách sử dụng nút Save / Load trên màn hình
- **Nút `SAVE`** (màu xanh dương ở góc trên bên phải):
  - Nhấp vào nút `SAVE` bất kỳ lúc nào để lưu toàn bộ dữ liệu nông trại.
  - Màn hình sẽ hiện thông báo popup: `Đã Lưu Dữ Liệu!`.
- **Nút `LOAD`** (màu xám xanh ở cạnh nút SAVE):
  - Nhấp vào nút `LOAD` để khôi phục lại trạng thái đã lưu trước đó.
  - Cấp độ, thanh XP, kho đồ, cây trồng và trạng thái vật nuôi sẽ lập tức được cập nhật đồng bộ.
  - Màn hình hiện thông báo popup: `Đã Tải Dữ Liệu!`.
