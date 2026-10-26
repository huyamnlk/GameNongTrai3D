# HayDay Custom – GameNongTrai3D

Project này đã thay phần `Assets` bằng source từ [NatalieC001/HayDay-Project](https://github.com/NatalieC001/HayDay-Project). `ProjectSettings`, `Packages` và cache Unity của máy được giữ lại để project vẫn mở bằng Unity hiện tại.

## Cách cài và chạy

1. Cài **Unity 6.0.6f1** (hoặc mở bằng Unity Hub và để Unity tự nâng cấp project nếu cần).
2. Trong Unity Hub chọn **Add > Add project from disk** và trỏ tới thư mục `D:\GameNongTrai3D`.
3. Chờ Unity import toàn bộ asset. Lần đầu có thể mất vài phút.
4. Mở scene `Assets/Scenes/Main Menu.unity`.
5. Nhấn **Play** để chạy thử trên máy tính.

## Các scene chính

- `Main Menu.unity`: menu bắt đầu.
- `Player Name.unity`: nhập tên người chơi.
- `Farm.unity`: khu trang trại.
- `Mart.unity`: khu mua bán.
- `Settings.unity`: cài đặt.

## Build Android

1. Mở **File > Build Profiles** (hoặc **Build Settings** tùy phiên bản Unity).
2. Chọn **Android**, rồi **Switch Platform**.
3. Thêm các scene theo thứ tự: Main Menu, Player Name, Farm, Mart, Misc Menu, Settings.
4. Chọn **Build And Run** để cài lên điện thoại.
5. Nếu Unity báo thiếu Android module, mở Unity Hub > Installs > dấu ba chấm > Add modules và cài Android Build Support, SDK & NDK Tools, OpenJDK.

## Nơi chỉnh riêng cho bạn

- Gameplay và điều khiển: `Assets/Scripts/`
- Scene/trang trại: `Assets/Scenes/Farm.unity`
- Menu: `Assets/Scripts/Menus/` và các scene menu.
- Nhân vật và bò: `Assets/Scripts/Player/` và `Assets/Scripts/Cow/`.

## Backup

Assets trước khi thay được giữ tại `_GameNongTrai3D_Assets_Before_HayDay`. Không xóa thư mục này cho tới khi xác nhận bản mới chạy ổn.

## Lưu ý

Repo gốc là project Unity đời cũ và README gốc mô tả Android 4.0+. Một số warning/API cũ có thể xuất hiện khi Unity 6 import; cần xử lý tiếp nếu Console báo lỗi compile thực tế.
