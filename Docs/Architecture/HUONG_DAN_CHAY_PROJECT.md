# HƯỚNG DẪN MỞ VÀ CHẠY DỰ ÁN TRÊN UNITY EDITOR & PC

Tài liệu này hướng dẫn từng bước để giảng viên hoặc sinh viên có thể mở, chạy thử và kiểm tra mã nguồn của đồ án nông trại 2D trên máy tính.

---

## 1. Yêu cầu hệ thống
- **Hệ điều hành**: Windows 10/11 (hoặc macOS/Linux).
- **Phần mềm**: Unity Hub và Unity Editor (khuyến nghị phiên bản Unity 6, ví dụ `6000.6.5f1` hoặc Unity 2022.3 LTS trở lên).
- **Mã nguồn dự án**: Nằm tại thư mục `D:\GameNongTrai3D`.

---

## 2. Các bước mở dự án trong Unity Hub
1. Mở phần mềm **Unity Hub**.
2. Nhấn nút **Add** (hoặc mũi tên xổ xuống chọn **Add project from disk**).
3. Duyệt và chọn thư mục `D:\GameNongTrai3D`.
4. Tại cột Editor Version, chọn phiên bản Unity Editor đã cài đặt trên máy của bạn (ví dụ `Unity 6000.6.5f1`).
5. Nhấp chuột vào tên dự án `GameNongTrai3D` để mở.
6. Lần đầu mở, Unity sẽ nạp Package Manager và biên dịch mã nguồn C# (mất khoảng 1 - 2 phút).

---

## 3. Mở Scene và chạy thử Game
1. Trong cửa sổ **Project** ở góc dưới, mở thư mục:
   `Assets` $\rightarrow$ `Scenes` $\rightarrow$ nhấp đúp vào **`MainScene`**.
2. Trong cửa sổ **Game** (Game View):
   - Chọn chế độ hiển thị màn hình dọc để có trải nghiệm chuẩn Mobile:
     Nhấp vào ô chọn tỉ lệ khung hình (Aspect) $\rightarrow$ chọn **Portrait (9:16)** hoặc tạo tỉ lệ `1080 x 1920`.
3. Nhấn nút **Play** (biểu tượng tam giác $\blacktriangleright$) ở thanh công cụ trên cùng.
4. `GameInitializer` sẽ tự động dựng toàn bộ bản đồ nông trại, các ô đất, chuồng gà, công trình và toàn bộ giao diện điều khiển trong chưa đầy 1 giây.

---

## 4. Hướng dẫn thao tác khi chơi thử (PC & Mobile)

### A. Thao tác điều khiển Camera:
- **Di chuyển bản đồ (Pan)**:
  - Trên PC: Giữ **chuột phải** và kéo rê chuột theo hướng mong muốn.
  - Trên Mobile: Chạm 1 ngón tay vào khoảng đất trống và kéo.
- **Phóng to / Thu nhỏ (Zoom)**:
  - Trên PC: Lăn **con cuộn chuột** (Scroll Wheel).
  - Trên Mobile: Dùng 2 ngón tay chụm vào / mở rộng ra (Pinch Zoom).

### B. Vòng lặp chơi mẫu (Gameplay Flow):
1. **Trồng cây**:
   - Chạm (click chuột trái) vào một ô đất trống màu nâu.
   - Bảng **🌱 Chọn hạt giống** mở ra.
   - Chọn **Lúa mì** (giá 5 GOLD, sinh trưởng 30 giây) hoặc **Ngô** (8 GOLD, sinh trưởng 45 giây).
   - Nông dân sẽ tự động bước tới ô đất; ô đất hiển thị cây non và bộ đếm thời gian lùi dần.
2. **Thu hoạch**:
   - Khi hết thời gian, cây chuyển sang màu vàng tươi và xuất hiện chấm tròn nhấp nháy.
   - Click chuột trái vào cây để thu hoạch.
   - Chữ bay lên hiển thị số lượng nhận được và EXP; nông sản được tự động chuyển vào Nhà kho.
3. **Chăn nuôi gà**:
   - Click vào ô chuồng gà trống bên phải $\rightarrow$ mua Gà (50 GOLD).
   - Chú gà màu trắng xuất hiện với chấm đỏ báo đói.
   - Click vào chuồng để cho ăn (yêu cầu có 1 Ngô trong kho đã thu hoạch từ trước).
   - Đếm ngược 45 giây $\rightarrow$ chấm vàng báo sẵn sàng $\rightarrow$ click để thu hoạch Trứng (+1 Trứng, +5 EXP).
4. **Chế biến bánh mì tại Nhà máy**:
   - Click trực tiếp vào slot máy xay / nhà máy bên góc trên bên trái (hoặc click vào tòa nhà Nhà máy).
   - Bảng **🏭 Nhà máy chế biến** hiện lên $\rightarrow$ chọn công thức **Bánh mì** (yêu cầu 1 Trứng + 2 Lúa mì).
   - Chờ hoàn thành $\rightarrow$ click thu hoạch Bánh mì thơm ngon.
5. **Bán hàng tại Cửa hàng**:
   - Nhấn nút **🏪 Shop** ở thanh menu dưới cùng (hoặc click vào tòa nhà Cửa hàng trên bản đồ).
   - Danh sách nông sản bạn đang có trong kho sẽ hiển thị kèm giá bán.
   - Bấm **Bán 1** hoặc **Bán hết** $\rightarrow$ âm thanh keng keng vui tai phát ra, tiền vàng (GOLD) và EXP tăng lên rõ rệt.
6. **Nhận thưởng Nhiệm vụ**:
   - Nhấn nút **📋 NV** ở thanh menu dưới cùng.
   - Xem tiến độ: *Nông dân chăm chỉ* (thu hoạch lúa mì), *Thương nhân nông trại* (bán hàng), *Thợ làm bánh* (sản xuất).
   - Khi thanh tiến độ đầy, nút **Nhận** màu xanh lá sáng lên $\rightarrow$ bấm nhận thưởng lượng lớn GOLD và EXP.
7. **Mở rộng nông trại**:
   - Đạt đến Cấp 3 và tích lũy đủ 300 GOLD $\rightarrow$ click vào biển báo khóa ở khu đất phía dưới.
   - Bảng xác nhận mở khóa hiện ra $\rightarrow$ bấm **Mở khóa** $\rightarrow$ 4 ô đất mới tinh được kích hoạt để canh tác tiếp.
8. **Lưu game và Chơi lại từ đầu**:
   - Nhấn nút **⚙️ CĐ** (Cài đặt) $\rightarrow$ bấm **💾 Lưu game** để lưu ngay lập tức (game cũng tự động lưu định kỳ).
   - Muốn reset trải nghiệm lại từ con số 0: bấm **🔄 Chơi lại từ đầu**.
