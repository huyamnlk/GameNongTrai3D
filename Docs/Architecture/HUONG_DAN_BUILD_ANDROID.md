# HƯỚNG DẪN BUILD FILE APK CHO THIẾT BỊ ANDROID

Tài liệu này hướng dẫn chi tiết cách xuất bản (Build) trò chơi thành tệp tin cài đặt **.apk** để cài đặt và chơi trực tiếp trên điện thoại hoặc máy tính bảng Android.

---

## 1. Chuẩn bị môi trường Unity cho Android

1. Mở **Unity Hub** $\rightarrow$ chọn mục **Installs** ở thanh menu bên trái.
2. Tìm phiên bản Unity đang sử dụng (ví dụ `Unity 6000.6.5f1`) $\rightarrow$ nhấp vào biểu tượng bánh răng cưa $\rightarrow$ chọn **Add modules**.
3. Tích chọn:
   - **Android Build Support**
   - **Android SDK & NDK Tools**
   - **OpenJDK**
4. Nhấn **Install** và chờ Unity Hub tải và cài đặt xong các công cụ bổ trợ này.

---

## 2. Thiết lập cấu hình dự án (Project Settings)

Mở dự án `GameNongTrai3D` trong Unity Editor:

1. Vào thanh menu trên cùng: **Edit** $\rightarrow$ **Project Settings...**
2. Chọn mục **Player** $\rightarrow$ chọn tab có biểu tượng Android (con robot màu xanh):
   - **Company Name**: Đặt tên nhóm/tác giả (ví dụ: `DoAnNongTrai`).
   - **Product Name**: `GameNongTrai3D` (hoặc `HayDay2D`).
   - **Default Icon**: Có thể chọn icon tùy ý hoặc để mặc định.
3. Mở rộng mục **Resolution and Presentation**:
   - **Default Orientation**: Chọn **Portrait** (để game luôn giữ màn hình dọc khi xoay máy).
   - Tắt các lựa chọn Landscape (Landscape Left, Landscape Right).
4. Mở rộng mục **Other Settings**:
   - **Package Name**: Đặt định danh duy nhất, ví dụ: `com.doan.nongtrai2d`.
   - **Minimum API Level**: Chọn `Android 7.0 'Nougat' (API Level 24)` hoặc cao hơn để tương thích tốt với hầu hết thiết bị hiện nay.
   - **Target API Level**: Chọn `Automatic (highest installed)`.
   - **Scripting Backend**: Chọn **IL2CPP** (hoặc **Mono** nếu muốn build nhanh cho máy ảo).
   - **Target Architectures**:
     - Nếu chọn Mono: Tích **ARMv7**.
     - Nếu chọn IL2CPP: Tích cả **ARMv7** và **ARM64** (khuyến nghị cho thiết bị thật).

---

## 3. Các bước tiến hành Build APK

1. Vào thanh menu: **File** $\rightarrow$ **Build Settings...** (hoặc nhấn tổ hợp phím `Ctrl + Shift + B`).
2. Trong danh sách **Platform**:
   - Nhấp chọn **Android**.
   - Nếu chưa kích hoạt, nhấn nút **Switch Platform** ở góc dưới bên phải (chờ Unity chuyển đổi nền tảng một lát).
3. Đảm bảo scene `Assets/Scenes/MainScene.unity` đã được tích dấu chọn trong ô **Scenes In Build**. Nếu chưa có, nhấn nút **Add Open Scenes**.
4. Chọn loại bản dựng:
   - Nếu muốn xuất file `.apk` cài trực tiếp: Bỏ chọn ô *Build App Bundle (Google Play)*.
   - Nếu muốn kiểm tra debug: Có thể tích ô *Development Build*.
5. Nhấn nút **Build**:
   - Chọn thư mục lưu file và đặt tên tệp, ví dụ: `D:\GameNongTrai3D\GameNongTrai3D_v1.0.apk`.
   - Nhấn **Save**.
6. Quá trình biên dịch sẽ diễn ra trong 2 - 5 phút tùy cấu hình máy tính. Khi hoàn thành, Unity sẽ tự động mở thư mục chứa file APK đã tạo.

---

## 4. Cài đặt và chạy thử trên điện thoại thật

1. **Bật chế độ cài đặt từ nguồn không xác định** trên điện thoại:
   - Vào Cài đặt máy $\rightarrow$ Bảo mật $\rightarrow$ Cho phép cài đặt ứng dụng từ trình duyệt hoặc Quản lý tệp tin.
2. **Chuyển file APK sang điện thoại**:
   - Cắm cáp USB chép file sang bộ nhớ điện thoại, hoặc gửi qua Zalo, Google Drive, Telegram.
3. **Cài đặt**:
   - Mở ứng dụng Quản lý tệp (File Manager) trên điện thoại $\rightarrow$ nhấn vào file `GameNongTrai3D_v1.0.apk` $\rightarrow$ chọn **Cài đặt** (Install).
4. **Mở game và trải nghiệm**:
   - Trò chơi tự động khởi chạy ở chế độ dọc.
   - Thao tác cảm ứng mượt mà: chạm để chọn hạt giống/cho ăn/thu hoạch, vuốt để di chuyển trang trại, phóng to thu nhỏ bằng 2 ngón tay.
