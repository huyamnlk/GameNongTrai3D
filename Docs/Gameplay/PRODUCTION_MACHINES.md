# HỆ THỐNG MÁY MÓC SẢN XUẤT (PRODUCTION MACHINES)

Tài liệu hướng dẫn và đặc tả kỹ thuật chi tiết cho hệ thống Máy móc chế biến nông sản trong **GameNongTrai3D** (`Assets/Scenes/Farm.unity`).

---

## 1. TỔNG QUAN HỆ THỐNG

Hệ thống Máy móc sản xuất đóng vai trò hoàn thiện chuỗi giá trị nông nghiệp (Nông sản thô -> Chế biến sâu -> Thành phẩm cao cấp có giá trị cao).
- **Mô hình máy 3D thực thể (`ProductionMachine`)**: Đặt trực tiếp trong khuôn viên nông trại tại Khu vực chế biến (gần nhà xưởng/kho).
- **Bảng hiển thị 3D (Billboard UI)**: Nằm ngay trên nóc từng máy móc, hiển thị tên máy, trạng thái hoạt động (*Sẵn sàng*, *Đang sản xuất xx%*, *Đã hoàn tất - Nhấp để thu hoạch!*).
- **Giao diện điều khiển máy (`MachineUI`)**: Cung cấp giao diện trực quan hiển thị công thức chế biến, yêu cầu nguyên liệu, thời gian, thanh tiến trình (progress bar), cùng các nút điều khiển *Start*, *Collect*, *Cancel*.
- **Cơ chế an toàn tồn kho (Storage Safety Guarantee)**: Đảm bảo thành phẩm chế biến không bao giờ bị biến mất nếu kho nông sản đang đầy.

---

## 2. DANH MỤC 4 LOẠI MÁY MÓC VÀ CÔNG THỨC CHẾ BIẾN

| Tên máy | Mô hình / Vai trò | Giá mua | Level mở | Công thức chế biến (Input -> Output) | Thời gian | XP nhận |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Mill** *(Cối xay)* | Xay hạt lương thực thành bột mịn | 250 G | Level 1 | **1 Wheat** -> **1 Flour** | 15 giây | +12 XP |
| **Bakery** *(Lò nướng)* | Nướng bánh mì thơm ngon từ bột mì & trứng | 400 G | Level 2 | **1 Flour + 1 Egg** -> **1 Bread** | 25 giây | +25 XP |
| **Dairy Machine** *(Máy làm sữa/phô mai)* | Chế biến sữa tươi thành phô mai thơm béo | 350 G | Level 2 | **1 Milk** -> **1 Cheese** | 20 giây | +20 XP |
| **Feed Mixer** *(Máy trộn thức ăn)* | Nghiền ngô/lúa mì thành cám viên cao cấp | 200 G | Level 1 | **1 Wheat** (hoặc Corn) -> **2 Pellets** | 10 giây | +10 XP |

---

## 3. CÁCH MUA MÁY MÓC TỪ SHOP

1. Người chơi mở Shop (bấm nút **[SHOP]** trên thanh HUD).
2. Chuyển sang tab **Machines**.
3. Chọn máy móc cần mua:
   - Nếu chưa đạt Level quy định: Nút mua bị khóa đỏ, hiển thị *"Unlocks at Level X"*.
   - Nếu đạt Level nhưng thiếu vàng: Báo thông báo *"Not enough Gold"*.
   - Nếu đủ tiền và Level: Bấm **[Buy]**.
4. Tiền vàng bị trừ tương ứng, máy móc xuất hiện trong Farm tại vị trí chế biến được cấu hình sẵn trong `MachineManager`.
5. Bảng hiệu 3D xuất hiện trên nóc máy hiển thị trạng thái `IDLE (Sẵn sàng)`.

---

## 4. QUY TRÌNH VẬN HÀNH MÁY MÓC TRONG GAMEPLAY

1. **Mở máy**: Người chơi nhấp chuột trái hoặc chạm vào mô hình máy móc trong nông trại.
2. **Kiểm tra nguyên liệu**: Bảng **MachineUI** xuất hiện:
   - Hiển thị công thức chế biến.
   - Hiển thị số lượng nguyên liệu người chơi đang có trong Kho so với số lượng cần thiết (ví dụ: `Wheat: 3 / 1`).
   - Nếu thiếu nguyên liệu: Số lượng hiển thị màu đỏ, nút **[Bắt đầu sản xuất]** bị vô hiệu hóa.
3. **Bắt đầu sản xuất (Start)**:
   - Khi bấm **[Start]**:
     - Hệ thống lập tức trừ nguyên liệu trong kho (`InventorySystem`).
     - Trạng thái máy chuyển thành `Producing`.
     - Bộ đếm thời gian bắt đầu chạy và thanh tiến trình (Progress Bar) tăng dần đều.
     - Bảng hiệu 3D trên nóc máy cập nhật tỷ lệ phần trăm theo thời gian thực (ví dụ: `Mill: 45%`).
4. **Hủy tiến trình (Cancel)**:
   - Nếu người chơi muốn dừng máy giữa chừng, bấm **[Cancel]**: máy quay về trạng thái rảnh rỗi.
5. **Thu hoạch thành phẩm (Collect)**:
   - Khi đếm ngược hoàn tất, trạng thái máy chuyển thành `Completed`.
   - Nút **[Collect]** sáng xanh nổi bật trên giao diện, và bảng hiệu 3D trên máy nhấp nháy dòng chữ *"HOÀN THÀNH - NHẤP ĐỂ THU HOẠCH"*.
   - Người chơi bấm **[Collect]**:
     - Hệ thống kiểm tra sức chứa kho:
       - **Nếu kho còn chỗ**: Thành phẩm được chuyển vào Kho, cộng điểm kinh nghiệm (XP) cho người chơi, hiển thị thông báo thu hoạch thành công, máy quay về trạng thái rảnh rỗi.
       - **Nếu kho đã đầy**: Hiển thị cảnh báo *"Kho đã đầy! Vui lòng dọn dẹp hoặc nâng cấp kho trước khi nhận hàng"*. Thành phẩm **được lưu an toàn bên trong máy**, người chơi có thể bấm nhận lại bất kỳ lúc nào sau khi nâng cấp kho.

---

## 5. PHÍM TẮT DEBUG TĂNG TỐC SẢN XUẤT (#if UNITY_EDITOR)

- Nhấn phím **F4**: Hoàn tất ngay lập tức 100% thời gian sản xuất của toàn bộ máy móc đang chạy, cây trồng đang lớn và vật nuôi trong chuồng mà không cần chờ đợi.
- Nhấn phím **F2**: Cộng thêm 1000 Gold để mua toàn bộ 4 loại máy móc.

---

## 6. LƯU VÀ TẢI DỮ LIỆU MÁY MÓC (SAVE/LOAD SYSTEM)

Dữ liệu máy móc được lưu trữ theo cấu trúc JSON chuẩn qua `MachineSaveData`:
- `machineId`: Mã định danh máy (ví dụ: `machine_mill`).
- `isPurchased`: Trạng thái đã mua hay chưa.
- `state`: Trạng thái hiện tại (`Idle`, `Producing`, `Completed`).
- `currentRecipeId`: Công thức đang chế biến dở dang.
- `elapsedTime`: Thời gian đã chạy.
- `targetTime`: Tổng thời gian của chu kỳ sản xuất.

Dữ liệu không bao giờ lưu trực tiếp đối tượng tham chiếu của Unity, đảm bảo tương thích 100% khi chuyển Scene hoặc khởi động lại ứng dụng.

---

## 7. DANH SÁCH FILE LIÊN QUAN

- `Assets/Scripts/Machines/ProductionMachine.cs`: Logic máy móc, bộ đếm giờ, quản lý công thức và billboard 3D.
- `Assets/Scripts/Machines/MachineManager.cs`: Singleton quản lý mua, vị trí và chu kỳ của các máy.
- `Assets/Scripts/Machines/MachineUI.cs`: Giao diện tương tác chọn công thức, chạy thanh tiến trình và nhận đồ.
- `Assets/Scripts/Data/FarmEconomyData.cs`: Dữ liệu công thức chế biến `ProductionRecipeData`.
- `Assets/Scripts/Player/FarmInteractionController.cs`: Click raycast tương tác máy móc 3D.
- `Assets/Scripts/SaveLoad/SaveData.cs` & `SaveLoadSystem.cs`: Khả năng lưu trữ trạng thái sản xuất.
