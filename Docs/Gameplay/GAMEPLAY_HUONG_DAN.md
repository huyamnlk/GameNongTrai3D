# Hướng dẫn gameplay – HayDay Custom

## Chạy game

1. Mở scene `Assets/Scenes/Main Menu.unity`.
2. Nhấn **Play**.
3. Chọn **New Game** hoặc bắt đầu game.
4. Khi vào trang trại, click vào vùng **Game** để Unity nhận chuột.

## Điều khiển

### Máy tính

- `W A S D` hoặc phím mũi tên: di chuyển nhân vật.
- Joystick tròn ở góc dưới bên trái: kéo để di chuyển.
- Click vào bò: mở tương tác/thông tin về bò.
- Click vào nút giao diện: thực hiện chức năng của nút.

### Điện thoại

- Kéo joystick cảm ứng ở góc dưới bên trái để di chuyển.
- Chạm vào bò hoặc vật thể có thể tương tác.
- Chạm nút `X` để đóng cửa sổ đang mở.

## Cửa sổ Supplies (Kho thức ăn)

Cửa sổ **Supplies** đã được nâng cấp hoàn toàn sang hệ thống Unity UI (uGUI) hiện đại, tương thích 100% chuột và cảm ứng màn hình:

- **Nút mở nhanh trên HUD:** Bạn có thể nhấn nút `SUPPLIES` màu xanh ở góc trên bên phải màn hình bất kỳ lúc nào để mở/đóng kho.
- **Grain (Ngũ cốc / Thóc):** Nhấp/chạm vào ô Grain để xem số lượng và thông tin.
- **Hay (Cỏ khô):** Nhấp/chạm vào ô Hay để xem số lượng và thông tin.
- **Pellets (Thức ăn viên / Cám viên):** Nhấp/chạm vào ô Pellets để xem số lượng và thông tin.
- **Tính năng cho bò ăn:** Khi đang click chọn một con bò và mở Supplies, nhấn vào bất kỳ loại thức ăn nào sẽ tự động cho con bò đó ăn (giảm 1 đơn vị thức ăn, tăng chỉ số Sức khỏe và Hạnh phúc cho bò)!
- **Nút `X` đóng cửa sổ:** Nút `X` màu đỏ ở góc trên bên phải cửa sổ Supplies có hiệu ứng hover/press, nhấp vào sẽ đóng ngay cửa sổ và mở lại quyền điều khiển.
- **Chống chặn điều khiển:** Khi cửa sổ đóng, hệ thống hoàn toàn không chặn joystick hay camera. Khi cửa sổ mở, các cú click vào bảng UI sẽ không bị nhận nhầm là click di chuyển xuống mặt đất.

## Tương tác với bò

Khi click/chạm vào bò, hệ thống `FarmInteractionController` sẽ nhận diện con bò. Console có thể hiện log dạng:

```text
[FarmInteractionController] Interacted with cow: ...
```

Đây là log thông báo hệ thống đã nhận click, không phải lỗi. Sau khi chọn bò, bạn có thể mở cửa sổ Supplies để cho bò ăn ngay lập tức.

## Khi click không hoạt động

1. Kiểm tra nút **Play** đang chạy.
2. Click một lần vào vùng Game để lấy focus.
3. Nếu Supplies đang mở, nhấn nút `X` để đóng.
4. Không click vào vùng Console hoặc Hierarchy vì các cửa sổ đó sẽ nhận chuột thay cho Game.
5. Kiểm tra Console: log `Dynamic mobile joystick created successfully` nghĩa là joystick đã được tạo.
6. Lỗi `Tag: Terrain is not defined` đã được khắc phục hoàn toàn trên cả script `ClickToMove` và `TagManager`.

## Vòng lặp gameplay hiện tại

1. Di chuyển quanh trang trại bằng `WASD`, chuột click-to-move, hoặc Virtual Joystick.
2. Tìm và click vào đàn bò để tương tác/xem chỉ số.
3. Nhấn nút `SUPPLIES` trên HUD hoặc đi vào vùng trigger nông trại để mở kho thức ăn.
4. Chạm vào Grain / Hay / Pellets để cho bò ăn và nâng cao hạnh phúc cho đàn bò.
5. Di chuyển tới khu vực chợ (Mart) để giao dịch và mua bán gia súc.

## Lưu ý phát triển

Hệ thống điều khiển và UI trong `Farm.unity` hiện đã chạy hoàn toàn trên chuẩn Unity 6 (uGUI, EventSystem, CharacterController, CameraFollow mượt mà). Không còn lỗi compile đỏ hay lỗi tag chưa định nghĩa. Thích hợp cho cả trải nghiệm bàn phím PC và touch trên thiết bị Android.
