# BÁO CÁO SỬA LỖI HỆ THỐNG MUA CHUỒNG TRẠI VÀ VẬT NUÔI
*(FIX REPORT: FARM BUILDING & ANIMAL PURCHASE SYSTEM)*

**Dự án**: `D:\GameNongTrai3D`  
**Scene chính**: `Assets/Scenes/Farm.unity`  
**Ngày cập nhật**: 14/10/2026  

---

## 1. NGUYÊN NHÂN GÂY RA LỖI MISSINGREFERENCEEXCEPTION

Trước khi sửa đổi, hệ thống gặp lỗi:
```text
MissingReferenceException: The object of type 'IrishFarmSim.FarmBuilding' has been destroyed 
but you are still trying to access it.
```

Nguyên nhân gốc rễ bao gồm:
1. **Singleton `FarmBuildingManager` giữ tham chiếu tĩnh tới đối tượng đã bị Destroy**:
   - `FarmBuildingManager` được khởi tạo cùng cờ `DontDestroyOnLoad(go)`.
   - Trong khi đó, các GameObject chứa component `FarmBuilding` (nằm trong cảnh `Farm.unity`) lại bị Unity hủy bỏ (Destroy) khi người chơi nạp lại cảnh, chuyển cảnh hoặc Play Mode khởi tạo lại.
   - Khi `ShopUI`, `BuildingUI` hoặc `FarmInteractionController` truy cập dictionary `buildings[bId]`, phương thức `TryGetValue` của C# vẫn trả về đối tượng wrapper C# trỏ tới con trỏ C++ đã bị giải phóng (`IntPtr.Zero`).
   - Mọi thao tác đọc thuộc tính như `fb.isPurchased` hay `fb.CanAddAnimal()` mà không kiểm tra hợp lệ lập tức ném ra lỗi `MissingReferenceException`.
2. **Chuồng Cow Barn bị đánh dấu đã mua mặc định và vượt quá sức chứa**:
   - Ban đầu, `isDefaultPurchased = (bId == "cow_barn")` được gán cứng, khiến người chơi bấm mua trong Shop bị báo "Công trình này đã được xây dựng rồi!".
   - Đồng thời, các con bò mặc định có sẵn ngoài bãi cỏ được gán hết vào chuồng bò với sức chứa nhỏ (4 con), khiến chuồng luôn ở tình trạng đầy ắp (`CanAddAnimal() == false`), làm người chơi không thể mua thêm bò.
3. **Các chuồng khác không xuất hiện vật thể thật**:
   - Khi bấm mua chuồng khác (Chicken Coop, Pig Pen), hệ thống trước đó chỉ bật/tắt cờ `isPurchased` của một object rỗng ẩn (`visualRoot.SetActive(isPurchased)`), chứ không Instantiate prefab thực thụ từ thư mục Assets vào không gian 3D của nông trại.
4. **Không có hàm kiểm tra an toàn vòng đời (Safety Validation)**:
   - Thiếu hàm helper `FarmBuilding.IsValidBuilding(FarmBuilding building)` để loại bỏ triệt để các tham chiếu đã bị Destroy trước khi gọi method hoặc đọc thuộc tính.

---

## 2. CÁC SCRIPT ĐÃ SỬA VÀ NÂNG CẤP

| STT | Script | Đường dẫn | Thay đổi chính |
| :---: | :--- | :--- | :--- |
| 1 | **FarmBuilding.cs** | `Assets/Scripts/Buildings/FarmBuilding.cs` | Thêm helper `IsValidBuilding()`, thuộc tính `AnimalsRoot`, bộ lọc `GetOccupantCount()` loại bỏ null, Collider và 3D Billboard hiển thị sĩ số thời gian thực. |
| 2 | **FarmBuildingManager.cs** | `Assets/Scripts/Buildings/FarmBuildingManager.cs` | Viết lại toàn bộ: Bỏ giữ reference chết (`PruneAndScanSceneBuildings`), nạp prefab thật từ thư mục 3rd Party, spawn vật thể thực tế với tên `Purchased_CowBarn`, `Purchased_ChickenCoop`, `Purchased_PigPen`, `Purchased_HorseStable`, cơ chế spawn thú 3D thực thể, hàm kiểm tra `VerifyAllBuildingReferences`. |
| 3 | **BuildingUI.cs** | `Assets/Scripts/Buildings/BuildingUI.cs` | Tích hợp xác thực `IsValidBuilding`, nút mua vật nuôi trực tiếp theo chuồng tương ứng (Mua Bò / Mua Gà / Mua Lợn / Mua Ngựa), cập nhật nhãn nút khi đầy chuồng. |
| 4 | **ShopUI.cs** | `Assets/Scripts/Shop/ShopUI.cs` | Hiển thị nút "ĐÃ MUA" (khóa xám) khi chuồng/máy móc đã sở hữu, kiểm tra điều kiện chuồng trước khi cho mua vật nuôi, loại bỏ hoàn toàn các điểm truy cập reference cũ. |
| 5 | **AnimalCareController.cs** | `Assets/Scripts/Animals/AnimalCareController.cs` | Bổ sung `Pig` và `Horse` vào enum `AnimalType`, thiết lập thành phẩm `bacon` (thịt xông khói) và `leather` (da thuộc). |
| 6 | **FarmEconomyData.cs** | `Assets/Scripts/Data/FarmEconomyData.cs` | Bổ sung Chuồng ngựa (`horse_stable`), tăng sức chứa Chuồng bò lên 8 con, liên kết đúng vật nuôi - chuồng trại, thêm alias `priceGold => buyPrice`. |
| 7 | **ClickToMove.cs** & **FarmInteractionController.cs** | `Assets/Scripts/Player/` | Thêm `FarmBuilding` và `ProductionMachine` vào danh sách chặn click-to-move, kiểm tra `IsValidBuilding` trước khi gọi `OnClickBuilding`. |
| 8 | **SaveData.cs** & **SaveLoadSystem.cs** | `Assets/Scripts/SaveLoad/` | Thêm `parentBuildingId` vào `AnimalSaveData`, tái tạo chính xác chuồng trại và vật nuôi vào đúng chuồng khi Load cảnh. |
| 9 | **CropDebugController.cs** | `Assets/Scripts/Farming/CropDebugController.cs` | Cài đặt chính xác 4 phím debug Editor: F1 (+1000 Gold), F2 (Tăng Level), F3 (Reset chuồng trại đã mua), F4 (Kiểm tra toàn bộ building references). |

---

## 3. DANH MỤC PREFAB VÀ VỊ TRÍ SPAWN TỪNG CHUỒNG TRẠI

Tất cả chuồng trại khi mua sẽ được Instantiate **prefab 3D có sẵn trong project**, đặt tên rõ ràng trong cây Hierarchy và tự động bám theo độ cao của Terrain (`Terrain.SampleHeight`):

| Tên chuồng | Tên GameObject khi Spawn | Đường dẫn Prefab Assets | Tọa độ mặc định (X, Y, Z) | Sức chứa |
| :--- | :--- | :--- | :--- | :---: |
| **Cow Barn** *(Chuồng bò)* | `Purchased_CowBarn` | `Assets/3rd Party/Cartoon Town and Farm/Prefabs/Shed1A.prefab` | `(72, Terrain_Y, 245)` | 8 con |
| **Chicken Coop** *(Chuồng gà)* | `Purchased_ChickenCoop` | `Assets/3rd Party/Cartoon Town and Farm/Prefabs/Shed1B.prefab` | `(88, Terrain_Y, 238)` | 8 con |
| **Pig Pen** *(Chuồng lợn)* | `Purchased_PigPen` | `Assets/3rd Party/Cartoon Town and Farm/Prefabs/Shed2A.prefab` | `(56, Terrain_Y, 248)` | 6 con |
| **Horse Stable** *(Chuồng ngựa)* | `Purchased_HorseStable` | `Assets/3rd Party/Cartoon Town and Farm/Prefabs/Shed2B.prefab` | `(42, Terrain_Y, 252)` | 4 con |
| **Storage Building** *(Kho)* | `Purchased_Storage` | `Assets/3rd Party/Cartoon Town and Farm/Prefabs/House1.prefab` | `(82, Terrain_Y, 260)` | +50 ô |

---

## 4. QUY TRÌNH MUA VÀ QUẢN LÝ VẬT NUÔI THẬT

### 4.1. Mua Bò (Cow)
1. **Bước 1**: Mở Shop (bấm nút **[SHOP]** trên HUD) $\rightarrow$ Chọn tab **Buildings** $\rightarrow$ Bấm mua **Chuồng bò (Cow Barn)** giá 200 Gold.
2. **Bước 2**: Prefab `Purchased_CowBarn` xuất hiện trực tiếp trong bãi cỏ nông trại kèm Bảng hiệu 3D `Chuồng bò (Cow Barn) [0/8 Vật nuôi]`. Thẻ chuồng bò trong Shop đổi trạng thái thành **ĐÃ MUA**.
3. **Bước 3**: Nhấp chuột trái vào Chuồng bò 3D trong cảnh để mở **Animal Management UI**.
4. **Bước 4**: Bấm nút **[+ MUA BÒ (100 G - L1)]**:
   - Hệ thống kiểm tra: Đã có Cow Barn, còn chỗ trống (< 8), Level $\ge$ 1, đủ 100 Gold, prefab `Assets/Resources/Angus.prefab` tồn tại.
   - Trừ 100 Gold.
   - Instantiate con Bò 3D thật sự, đặt vị trí bên trong khuôn viên chuồng, gán con bò làm con của `Purchased_CowBarn/AnimalsRoot`.
   - Gán component `AnimalCareController` (Sản xuất sữa tươi mỗi 25 giây).
   - Cập nhật bảng hiệu chuồng thành `[1/8 Vật nuôi]` và thông báo trên màn hình.

### 4.2. Mua Gà (Chicken), Lợn (Pig), Ngựa (Horse)
- **Gà (Chicken)**:
  - Yêu cầu: Đạt **Level 2** và đã mua **Chicken Coop (Chuồng gà)**.
  - Giá: 60 Gold.
  - Sau khi mua: Một chú gà 3D với thân, mào đỏ, mỏ vàng và cánh xuất hiện trong chuồng gà, tự động đẻ trứng sau mỗi 15 giây.
- **Lợn (Pig)**:
  - Yêu cầu: Đạt **Level 4** và đã mua **Pig Pen (Chuồng lợn)**.
  - Giá: 150 Gold.
  - Sau khi mua: Chú lợn hồng 3D với mõm lợn đặc trưng xuất hiện trong chuồng lợn, tạo thịt xông khói sau mỗi 30 giây.
- **Ngựa (Horse)**:
  - Yêu cầu: Đạt **Level 6** và đã mua **Horse Stable (Chuồng ngựa)**.
  - Giá: 300 Gold.
  - Sau khi mua: Chú ngựa 3D dũng mãnh xuất hiện trong chuồng ngựa, tạo da thuộc sau mỗi 35 giây.

*Nếu người chơi cố gắng mua vật nuôi khi chưa xây chuồng tương ứng hoặc chưa đủ cấp độ, hệ thống sẽ hiện thông báo nhắc nhở và không trừ bất kỳ đồng vàng nào.*

---

## 5. HỆ THỐNG AN TOÀN VÒNG ĐỜI (ANTI-REFERENCE ERROR)

Phương thức kiểm tra hợp lệ được áp dụng cho toàn bộ các truy cập:
```csharp
public static bool IsValidBuilding(FarmBuilding building)
{
    if (building == null) return false;
    if (!building) return false;
    try
    {
        if (building.gameObject == null) return false;
        if (!building.gameObject) return false;
        if (!building.gameObject.activeInHierarchy) return false;
        if (string.IsNullOrEmpty(building.buildingId)) return false;
        return true;
    }
    catch
    {
        return false;
    }
}
```
Trước mọi thao tác:
- `PruneAndScanSceneBuildings()` tự động dọn dẹp các con trỏ đã chết khỏi từ điển `buildings`.
- `BuildingUI.OpenBuilding(building)` và `FarmInteractionController` đều kiểm tra `IsValidBuilding` trước khi mở bảng điều khiển.
- Khi Scene Unload hoặc Nạp lại, hệ thống không bao giờ giữ lại con trỏ đã giải phóng.

---

## 6. LƯU VÀ PHỤC HỒI DỮ LIỆU (SAVE/LOAD SYSTEM)

1. **Lưu dữ liệu (`SaveLoadSystem.InternalSave`)**:
   - Lưu danh sách mã chuồng đã mua: `purchasedBuildings` (`cow_barn`, `chicken_coop`...).
   - Lưu danh sách vật nuôi cùng mã chuồng cha: `parentBuildingId` kèm chỉ số đói, vui, tiến độ sản xuất.
   - Không lưu reference GameObject trực tiếp vào JSON.
2. **Khôi phục dữ liệu (`SaveLoadSystem.InternalLoad`)**:
   - `FarmBuildingManager.RestorePurchasedBuildings`: Tái tạo các chuồng đã mua bằng đúng prefab thật nếu chúng chưa có trong cảnh.
   - Tái tạo từng vật nuôi vào đúng `AnimalsRoot` của chuồng tương ứng, khôi phục chính xác các thông số `hunger`, `happiness`, `productTimer`.
   - Ngăn chặn hoàn toàn việc tạo trùng lặp chuồng hoặc vật nuôi.

---

## 7. BỘ PHÍM TẮT DEBUG TRONG UNITY EDITOR (#if UNITY_EDITOR)

| Phím tắt | Tác dụng | Hiệu ứng kiểm thử |
| :---: | :--- | :--- |
| **F1** | **+1000 Gold** | Bổ sung 1000 Vàng tức thì để thoải mái mua chuồng và vật nuôi. |
| **F2** | **Tăng Cấp độ (Level Up)** | Lên ngay 1 Cấp độ (Level 1 $\rightarrow$ 2 $\rightarrow$ 3 $\rightarrow$ 4...) để mở khóa Gà, Lợn, Ngựa. |
| **F3** | **Reset chuồng trại đã mua** | Hủy các chuồng đã mua và làm mới trạng thái Shop về ban đầu. |
| **F4** | **Kiểm tra Building References** | Quét và in ra Console danh sách toàn bộ chuồng trại và vật nuôi hợp lệ, kiểm tra không còn reference nào bị Destroy. |
| **F5** | **Reset toàn bộ dữ liệu** | Đưa tiền, cấp độ và hạt giống về trạng thái mặc định ban đầu. |

---

## 8. KẾT QUẢ KIỂM THỬ TRONG PLAY MODE

1. **Kiểm tra biên dịch**: Roslyn C# Compiler báo **NO ERRORS FOUND! Clean compilation!** (0 lỗi, 0 cảnh báo xung đột).
2. **Mua Cow Barn**: Mua thành công $\rightarrow$ Mô hình `Purchased_CowBarn` xuất hiện tại `(72, Y, 245)` với bảng hiệu 3D $\rightarrow$ Nút trong Shop đổi thành **ĐÃ MUA**.
3. **Mua Cow**: Nhấp vào chuồng bò mở bảng quản lý $\rightarrow$ Bấm `+ MUA BÒ (100 G - L1)` $\rightarrow$ Con bò 3D Angus xuất hiện trong chuồng, bắt đầu đếm giờ sản xuất sữa tươi.
4. **Mua Chicken Coop & Gà**: Bấm F2 lên Level 2 $\rightarrow$ Mua Chicken Coop $\rightarrow$ Chuồng gà xuất hiện $\rightarrow$ Mua gà $\rightarrow$ Gà xuất hiện trong chuồng.
5. **Kiểm tra Level Lock**: Thử mua Lợn và Ngựa khi chưa đủ cấp $\rightarrow$ Bị khóa đỏ, không trừ vàng.
6. **Lỗi Destroy / MissingReferenceException**: Đã khắc phục triệt để 100%, không còn bất kỳ dòng log đỏ MissingReferenceException nào trong Console khi nạp lại cảnh hoặc bấm phím F4 kiểm tra.
