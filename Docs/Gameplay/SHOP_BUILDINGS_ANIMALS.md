# HỆ THỐNG CỬA HÀNG, CHUỒNG TRẠI VÀ VẬT NUÔI (FARM SHOP, BUILDINGS & ANIMALS)

Tài liệu hướng dẫn và đặc tả kỹ thuật chi tiết cho hệ thống Cửa hàng, Chuồng trại, Quản lý vật nuôi và Tương tác kho vật phẩm (Supplies) trong **GameNongTrai3D** (`Assets/Scenes/Farm.unity`).

---

## 1. TỔNG QUAN HỆ THỐNG

Hệ thống kết nối liền mạch giữa:
- **Cửa hàng (ShopUI)**: Trung tâm mua sắm toàn diện (Hạt giống, Thức ăn, Vật nuôi, Chuồng trại, Máy móc sản xuất).
- **Chuồng trại (FarmBuilding / FarmBuildingManager)**: Các công trình chăn nuôi thực thể xuất hiện trong nông trại, có sức chứa, bảng tên 3D Billboard, và mở menu quản lý khi nhấp chuột.
- **Vật nuôi (Animal / AnimalManager)**: Tự động gắn kết với chuồng tương ứng, quản lý trạng thái đói (Hunger), hạnh phúc (Happiness), thời gian tạo sản phẩm (Sữa bò, Trứng gà, Thịt heo...), và mô hình 3D trong chuồng.
- **Supplies tương tác chi tiết (SuppliesUI)**: Cho phép nhấp vào Grain, Hay, Pellets để mở bảng chi tiết sản phẩm, cho vật nuôi ăn trực tiếp (Feed), mua thêm (Buy) hoặc bán thu vàng (Sell).

---

## 2. CÁCH MỞ SHOP

1. **Trên giao diện (HUD)**:
   - Ở góc trên cùng bên trái màn hình (khu vực hiển thị Level, XP Bar, Gold), có 2 nút chức năng nổi bật:
     - Nút **[SHOP]** màu xanh lá / vàng kim: Nhấp vào để mở ngay Cửa hàng Nông trại.
     - Nút **[STORAGE]** màu xanh dương: Nhấp vào để kiểm tra Kho và Nâng cấp sức chứa.
2. **Khi đang xem chi tiết vật phẩm trong Supplies**:
   - Nhấp vào nút **"Buy More (Shop)"** để tự động chuyển đến Shop UI tại tab tương ứng.
3. **Đóng Shop**:
   - Nhấp nút **[X]** màu đỏ ở góc trên bên phải của bảng Shop để quay lại màn hình nông trại.

---

## 3. CẤU TRÚC 5 DANH MỤC CỬA HÀNG

Bảng Shop được chia thành 5 Tabs điều hướng rõ ràng:

1. **Seeds (Hạt giống)**:
   - *Wheat Seed* (Lúa mì): Giá 5 Gold, Mở khóa Level 1.
   - *Corn Seed* (Ngô): Giá 8 Gold, Mở khóa Level 1.
   - *Carrot Seed* (Cà rốt): Giá 10 Gold, Mở khóa Level 1.
   - *Hay Seed* (Cỏ khô): Giá 6 Gold, Mở khóa Level 1.
2. **Animal Feed (Thức ăn chăn nuôi)**:
   - *Grain* (Ngũ cốc): Giá 8 Gold, dùng cho Gà và Bò.
   - *Hay* (Cỏ khô chăn nuôi): Giá 10 Gold, dùng cho Bò.
   - *Pellets* (Cám viên tổng hợp): Giá 12 Gold, thức ăn giàu dinh dưỡng cho vật nuôi.
3. **Animals (Vật nuôi)**:
   - *Cow* (Bò sữa): Giá 150 Gold, Yêu cầu Level 1, Cần **Cow Barn**.
   - *Chicken* (Gà đẻ trứng): Giá 50 Gold, Yêu cầu **Level 2**, Cần **Chicken Coop**.
   - *Pig* (Heo): Giá 200 Gold, Yêu cầu **Level 4**, Cần **Pig Pen**.
   - *Horse* (Ngựa): Giá 400 Gold, Yêu cầu **Level 6**, Cần **Stable/Chuồng ngựa**.
4. **Buildings (Chuồng trại & Nhà kho)**:
   - *Chicken Coop* (Chuồng gà): Giá 200 Gold, Yêu cầu Level 2, Sức chứa: 6 con.
   - *Cow Barn* (Chuồng bò): Giá 300 Gold, Yêu cầu Level 1, Sức chứa: 6 con.
   - *Pig Pen* (Chuồng heo): Giá 400 Gold, Yêu cầu Level 4, Sức chứa: 6 con.
   - *Storage Building* (Nhà kho mở rộng): Giá 350 Gold, Yêu cầu Level 1, tăng 50 sức chứa kho.
5. **Machines (Máy móc sản xuất)**:
   - *Mill* (Cối xay bột): Giá 250 Gold, Yêu cầu Level 1.
   - *Bakery* (Lò nướng bánh): Giá 400 Gold, Yêu cầu Level 2.
   - *Dairy Machine* (Máy chế biến sữa/bơ/phô mai): Giá 350 Gold, Yêu cầu Level 2.
   - *Feed Mixer* (Máy trộn cám/thức ăn): Giá 200 Gold, Yêu cầu Level 1.

---

## 4. QUY TRÌNH MUA VÀ QUẢN LÝ CHUỒNG TRẠI

1. Người chơi mở Shop -> Chọn tab **Buildings**.
2. Hệ thống kiểm tra:
   - **Cấp độ (Level)**: Nếu chưa đủ level, hiển thị huy hiệu Khóa và dòng chữ *"Unlocks at Level X"*. Nút Buy bị vô hiệu hóa.
   - **Số dư Vàng (Gold)**: Nếu không đủ tiền, báo toast thông báo *"Not enough Gold"*.
3. Khi bấm **Buy**:
   - Trừ Gold trong tài khoản người chơi.
   - Tạo ra (spawn) thực thể công trình Chuồng trại (`FarmBuilding`) tại vị trí hợp lệ trong Nông trại.
   - Bật Bảng hiệu 3D (Billboard UI) lơ lửng trên nóc chuồng hiển thị tên chuồng và số lượng vật nuôi hiện có (ví dụ: `Cow Barn [0/6]`).
   - Cập nhật trạng thái chuồng trong hệ thống thành `Purchased`.
   - Hiển thị thông báo *"Purchased successfully"*.
4. **Tương tác trong Farm**:
   - Người chơi điều khiển nhân vật hoặc click chuột vào mô hình Chuồng trại trong không gian 3D.
   - Bảng **Animal Management UI** sẽ mở ra, cho phép:
     - Xem danh sách từng con vật đang sống trong chuồng kèm mức Độ đói (Hunger), Độ vui vẻ (Happiness) và thời gian ra sản phẩm.
     - Nút **[Feed All]**: Cho toàn bộ con vật trong chuồng ăn cùng lúc bằng thức ăn trong kho.
     - Nút **[Collect All]**: Thu hoạch toàn bộ sản phẩm đã sẵn sàng (Sữa bò, Trứng...).
     - Nút **[Buy Animal]**: Mua nhanh thêm vật nuôi vào chuồng này mà không cần mở Shop.

---

## 5. MỞ KHÓA VẬT NUÔI THEO LEVEL VÀ QUY TẮC MUA

### Điều kiện mua vật nuôi:
1. **Kiểm tra Chuồng (Barn Requirement)**: Không thể mua vật nuôi nếu người chơi chưa sở hữu chuồng trại tương ứng (ví dụ: mua Chicken phải có Chicken Coop). Báo thông báo: *"Bạn cần xây chuồng trước khi mua vật nuôi này!"*.
2. **Kiểm tra Sức chứa (Capacity)**: Chuồng phải còn chỗ trống (< 6 con). Nếu đã đạt 6/6, báo: *"Chuồng đã đầy sức chứa (6/6)!"*.
3. **Kiểm tra Cấp độ (Level)**:
   - Level 1: Mở khóa **Cow** (Bò sữa).
   - Level 2: Mở khóa **Chicken** (Gà).
   - Level 4: Mở khóa **Pig** (Heo).
   - Level 6: Mở khóa **Horse** (Ngựa).
   - Khi chưa đủ cấp: Nút mua hiển thị khóa đỏ, ghi *"Unlocks at Level X"*.
4. **Kiểm tra Vàng (Gold)**: Đủ số tiền niêm yết.

### Hành động sau khi mua thành công:
- Trừ Gold của người chơi.
- Tăng số lượng vật nuôi trong chuồng tương ứng.
- Tạo cá thể vật nuôi 3D xuất hiện trực tiếp trong khuôn viên chuồng trại.
- Cập nhật bảng hiệu 3D của chuồng trại (ví dụ: `Chicken Coop [1/6]`).
- Lưu lại trạng thái vào hệ thống Save/Load.

---

## 6. PHASE 1: CHỨC NĂNG CHI TIẾT SUPPLIES (GRAIN, HAY, PELLETS)

Khi người chơi bấm vào nút **Supplies** ở dưới màn hình:
- Bảng danh mục các loại thức ăn chăn nuôi hiện ra (Grain, Hay, Pellets) cùng số lượng tồn kho.
- Khi **nhấp vào bất kỳ ô nào (Grain / Hay / Pellets)**:
  - Hệ thống mở bảng **Item Detail Modal** nằm nổi ở giữa màn hình:
    1. **Tên vật phẩm**: Tiếng Việt + Tiếng Anh (ví dụ: *Grain (Ngũ cốc chăn nuôi)*).
    2. **Số lượng hiện có**: Đọc thời gian thực từ `InventorySystem` (ví dụ: `Số lượng: 5 cái`).
    3. **Công dụng**: Mô tả chi tiết vật nuôi có thể dùng (Grain: cho Gà và Bò ăn; Hay: cho Bò ăn; Pellets: thức ăn chất lượng cao).
    4. **Nút [Feed Cow] / [Feed Chicken]**:
       - Bấm nút này: Kiểm tra tồn kho > 0.
       - Trừ 1 đơn vị thức ăn tương ứng trong Kho.
       - Tăng Hunger (+35) và Happiness (+25) cho vật nuôi mục tiêu.
       - Cộng ngay **+15 XP** cho người chơi.
       - Hiển thị Toast thông báo: *"Đã cho [Vật nuôi] ăn! (+15 XP)"*.
       - Cập nhật số lượng trên ô Supplies ngay lập tức.
    5. **Nút [Buy More (Shop)]**: Đưa người chơi tới tab Thức ăn của Shop để mua thêm.
    6. **Nút [Sell for Gold]**: Bán bớt thức ăn nếu kho thừa, nhận lại Vàng tương ứng.
    7. **Nút [Close]**: Đóng bảng chi tiết.

---

## 7. CÁC PHÍM TẮT DEBUG TRONG EDITOR (#if UNITY_EDITOR)

Dành cho kiểm thử Play Mode nhanh chóng:
- **F1**: Cộng **+100 XP** cho người chơi (giúp lên cấp nhanh để test mở khóa).
- **F2**: Cộng **+1000 Gold** (giúp có vàng mua chuồng và vật nuôi).
- **F3**: Bật/tắt chế độ **Mở khóa toàn bộ Shop** (Debug unlock all items).
- **F4**: **Hoàn tất toàn bộ Timer sản xuất** (Cây trồng lập tức chín, máy móc hoàn thành sản phẩm, vật nuôi sẵn sàng cho thu hoạch).
- **F5**: **Reset dữ liệu test** về mặc định ban đầu (Hạt giống = 3, Cấp 1, 0 XP, 500 Gold).

---

## 8. DANH SÁCH FILE CODE LIÊN QUAN

- `Assets/Scripts/Data/FarmEconomyData.cs`: Định nghĩa cấu trúc dữ liệu `ShopItemData`, `AnimalData`, `BuildingData`, danh mục `FarmDatabase`.
- `Assets/Scripts/Shop/ShopUI.cs`: Giao diện Cửa hàng 5 tabs, quản lý mua bán, kiểm tra cấp độ và tiền.
- `Assets/Scripts/Buildings/FarmBuilding.cs`: Thực thể chuồng trại trên cảnh 3D, lưu danh sách vật nuôi, quản lý billboard 3D.
- `Assets/Scripts/Buildings/FarmBuildingManager.cs`: Singleton quản lý mua chuồng trại, phân phối vị trí, spawn động vật.
- `Assets/Scripts/Buildings/BuildingUI.cs`: Giao diện tương tác quản lý vật nuôi bên trong chuồng trại.
- `Assets/Scripts/Scenes/Farm/SuppliesUI.cs`: Nâng cấp giao diện Supplies với Item Detail Modal, thao tác Feed/Buy/Sell.
- `Assets/Scripts/Player/FarmInteractionController.cs`: Raycast nhận diện click vào Chuồng trại và Máy móc 3D.
- `Assets/Scripts/Farming/CropDebugController.cs`: Bộ điều khiển phím tắt F1-F5.

---

## 9. LƯU Ý KHI CHẠY & VẬN HÀNH

- Toàn bộ giao diện được sinh bằng mã C# an toàn, tự động liên kết với `EventSystem` và `GraphicRaycaster`, không phụ thuộc vào prefab bị missing.
- Khi các bảng UI (Shop, BuildingUI, SuppliesDetail) mở ra, hệ thống tự động khóa di chuyển của nhân vật (`ClickToMove.IsUIBlockingClick()`), ngăn ngừa việc bấm nhầm xuống mặt đất.
