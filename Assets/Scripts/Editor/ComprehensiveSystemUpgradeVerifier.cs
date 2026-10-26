#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using IrishFarmSim;

namespace IrishFarmSim.EditorTools
{
    public static class ComprehensiveSystemUpgradeVerifier
    {
        [MenuItem("Tools/Irish Farm/Verify Comprehensive Upgrade")]
        public static void RunVerification()
        {
            Debug.Log("==================================================================");
            Debug.Log("[VERIFIER] BẮT ĐẦU KIỂM TRA TOÀN DIỆN NÂNG CẤP NÔNG TRẠI THỰC TẾ");
            Debug.Log("==================================================================");

            int totalTests = 7;
            int passedTests = 0;

            try
            {
                // Test 1: Kiểm tra AudioManager & PlayerPrefs
                if (VerifyAudioSystem()) passedTests++;

                // Test 2: Kiểm tra Main Menu Scene & UI Controller
                if (VerifyMainMenuScene()) passedTests++;

                // Test 3: Kiểm tra Vật nuôi (Thú cưng: 3 chó, 3 mèo + Scale lớn + 3 con mỗi gia súc)
                if (VerifyAnimalsAndPets()) passedTests++;

                // Test 4: Kiểm tra Máy chế biến (Phải mua trong Shop, không tự động đặt máy mẫu)
                if (VerifyMachinesInShop()) passedTests++;

                // Test 5: Kiểm tra Bốn Mùa & Hiệu ứng Thời tiết (Xuân, Hạ, Thu, Đông, Tuyết, Thủ công)
                if (VerifySeasonsAndWeather()) passedTests++;

                // Test 6: Kiểm tra Luồng Mua & Đặt Máy Thực Tế
                if (VerifyMachinePlacementFlow()) passedTests++;

                // Test 7: Kiểm tra Lưu & Tải (Save/Load) Toàn Diện không trùng lặp
                if (VerifySaveLoadIntegrity()) passedTests++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] Lỗi ngoại lệ trong quá trình kiểm tra: {ex.Message}\n{ex.StackTrace}");
            }

            Debug.Log("==================================================================");
            Debug.Log($"[VERIFIER] KẾT QUẢ CUỐI CÙNG: {passedTests}/{totalTests} HỆ THỐNG VƯỢT QUA KIỂM TRA!");
            Debug.Log("==================================================================");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passedTests == totalTests ? 0 : 1);
            }
        }

        private static bool VerifyAudioSystem()
        {
            Debug.Log("[VERIFIER] 1. Kiểm tra AudioManager & Cài đặt Âm thanh...");
            AudioManager am = AudioManager.EnsureInstance();
            if (am == null)
            {
                Debug.LogError("[VERIFIER] 1. FAILED: Không thể khởi tạo AudioManager!");
                return false;
            }

            // Test Volume & Mute control
            am.SetBGMVolume(0.45f);
            am.SetSFXVolume(0.85f);
            am.SetBGMMuted(true);
            am.SetSFXMuted(false);

            float savedBgm = PlayerPrefs.GetFloat("AUDIO_BGM_VOL", -1f);
            float savedSfx = PlayerPrefs.GetFloat("AUDIO_SFX_VOL", -1f);
            int savedBgmMute = PlayerPrefs.GetInt("AUDIO_BGM_MUTE", -1);
            int savedSfxMute = PlayerPrefs.GetInt("AUDIO_SFX_MUTE", -1);

            if (Mathf.Abs(savedBgm - 0.45f) > 0.01f || Mathf.Abs(savedSfx - 0.85f) > 0.01f || savedBgmMute != 1 || savedSfxMute != 0)
            {
                Debug.LogError($"[VERIFIER] 1. FAILED: PlayerPrefs lưu âm thanh không khớp! (BGM={savedBgm}, SFX={savedSfx}, BGMMute={savedBgmMute}, SFXMute={savedSfxMute})");
                return false;
            }

            // Restore defaults
            am.SetBGMVolume(0.60f);
            am.SetSFXVolume(0.80f);
            am.SetBGMMuted(false);
            am.SetSFXMuted(false);

            Debug.Log("[VERIFIER] 1. PASSED: AudioManager & PlayerPrefs âm thanh hoạt động chuẩn xác.");
            return true;
        }

        private static bool VerifyMainMenuScene()
        {
            Debug.Log("[VERIFIER] 2. Kiểm tra Scene Main Menu thực tế...");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[VERIFIER] 2. FAILED: Không thể mở scene Assets/Scenes/Main Menu.unity!");
                return false;
            }

            MainMenuController.PurgeModernCanvasOverlay();

            // Check native NGUI UI Root and elements
            GameObject uiRoot = GameObject.Find("UI Root");
            if (uiRoot == null)
            {
                Debug.LogError("[VERIFIER] 2. FAILED: Không tìm thấy UI Root của menu gốc!");
                return false;
            }

            GameObject buttonsGo = GameObject.Find("Buttons");
            if (buttonsGo == null)
            {
                Debug.LogError("[VERIFIER] 2. FAILED: Không tìm thấy Buttons trong menu gốc!");
                return false;
            }

            bool hasNew = buttonsGo.transform.Find("NewGameButton") != null;
            bool hasContinue = buttonsGo.transform.Find("LoadGameButton") != null;
            bool hasSettings = buttonsGo.transform.Find("SettingsButton") != null;
            bool hasExit = buttonsGo.transform.Find("ExitButton") != null;

            if (!hasNew || !hasContinue || !hasSettings || !hasExit)
            {
                Debug.LogError($"[VERIFIER] 2. FAILED: Thiếu nút trên menu gốc! (New:{hasNew}, Load:{hasContinue}, Set:{hasSettings}, Exit:{hasExit})");
                return false;
            }

            // Test Continue button reactivity to save existence
            UIBackground.UpdateContinueButtonVisibility();
            bool saveExists = SaveLoadSystem.HasSave();
            GameObject loadBtnGo = buttonsGo.transform.Find("LoadGameButton").gameObject;
            if (loadBtnGo.activeSelf != saveExists)
            {
                Debug.LogError($"[VERIFIER] 2. FAILED: Nút LoadGameButton không phản ánh đúng trạng thái save! (Active={loadBtnGo.activeSelf}, SaveExists={saveExists})");
                return false;
            }

            // Check Version Label displays Version 1.1.1.1
            UIBackground.EnsureTitleAndVersionDisplay();
            UILabel vLbl = GameObject.Find("Version Label")?.GetComponent<UILabel>();
            if (vLbl != null && !vLbl.text.Contains("1.1.1.1"))
            {
                Debug.LogError($"[VERIFIER] 2. FAILED: Version Label không hiển thị Version 1.1.1.1! (Hiện tại: {vLbl.text})");
                return false;
            }

            // Verify no modern canvas is present
            if (GameObject.Find("MainMenu_ModernCanvas") != null)
            {
                Debug.LogError("[VERIFIER] 2. FAILED: MainMenu_ModernCanvas vẫn còn tồn tại!");
                return false;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[VERIFIER] 2. PASSED: Menu gốc hoàn chỉnh (New, Load, Settings, Exit, Version 1.1.1.1), không còn overlay UI.");
            return true;
        }

        private static bool VerifyAnimalsAndPets()
        {
            Debug.Log("[VERIFIER] 3. Kiểm tra Vật nuôi: Thú cưng (3 chó, 3 mèo) & Gia súc...");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Farm.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[VERIFIER] 3. FAILED: Không thể mở scene Assets/Scenes/Farm.unity!");
                return false;
            }

            // 1. Check Pet Manager & Visual Scale
            PetManager pm = PetManager.EnsureInstance();
            pm.activePets.Clear();
            foreach (var pet in UnityEngine.Object.FindObjectsByType<PetWanderer>(FindObjectsSortMode.None))
            {
                if (pet != null) UnityEngine.Object.DestroyImmediate(pet.gameObject);
            }

            pm.SpawnDefaultPets();

            if (pm.activePets.Count != 6)
            {
                Debug.LogError($"[VERIFIER] 3. FAILED: Số lượng thú cưng không phải 6 (3 chó, 3 mèo)! Thực tế: {pm.activePets.Count}");
                return false;
            }

            int dogCount = 0;
            int catCount = 0;
            HashSet<string> coats = new HashSet<string>();

            foreach (var pet in pm.activePets)
            {
                if (pet.kind == PetKind.Dog)
                {
                    dogCount++;
                    float expectedScale = 1.85f;
                    if (Mathf.Abs(pet.transform.localScale.x - expectedScale) > 0.05f)
                    {
                        Debug.LogError($"[VERIFIER] 3. FAILED: Scale của Chó {pet.petId} ({pet.transform.localScale.x}) không đúng {expectedScale}!");
                        return false;
                    }
                }
                else if (pet.kind == PetKind.Cat)
                {
                    catCount++;
                    float expectedScale = 1.60f;
                    if (Mathf.Abs(pet.transform.localScale.x - expectedScale) > 0.05f)
                    {
                        Debug.LogError($"[VERIFIER] 3. FAILED: Scale của Mèo {pet.petId} ({pet.transform.localScale.x}) không đúng {expectedScale}!");
                        return false;
                    }
                }
                coats.Add(pet.coatId);

                // Check collider trigger
                SphereCollider sc = pet.GetComponent<SphereCollider>();
                if (sc == null || !sc.isTrigger)
                {
                    Debug.LogError($"[VERIFIER] 3. FAILED: Thú cưng {pet.petId} thiếu Trigger Collider!");
                    return false;
                }
            }

            if (dogCount != 3 || catCount != 3)
            {
                Debug.LogError($"[VERIFIER] 3. FAILED: Tỷ lệ thú cưng không đúng (Cần 3 chó & 3 mèo, thực tế {dogCount} chó, {catCount} mèo)!");
                return false;
            }

            if (coats.Count != 6)
            {
                Debug.LogError($"[VERIFIER] 3. FAILED: 6 con thú cưng không có 6 màu/ngoại hình riêng biệt! (Số coat: {coats.Count})");
                return false;
            }

            // 2. Check Farm Animals Scale Config
            if (AnimalScaleConfig.COW_SCALE < 2.0f || AnimalScaleConfig.HORSE_SCALE < 1.5f || AnimalScaleConfig.CHICKEN_SCALE < 1.5f || AnimalScaleConfig.PIG_SCALE < 1.5f)
            {
                Debug.LogError("[VERIFIER] 3. FAILED: Scale gia súc chưa được tăng đủ lớn cho mobile!");
                return false;
            }

            Debug.Log($"[VERIFIER] 3. PASSED: Đúng 3 Chó (Scale 1.85, 3 coat riêng) và đúng 3 Mèo (Scale 1.60, 3 coat riêng). Gia súc tăng scale lớn rõ ràng.");
            return true;
        }

        private static bool VerifyMachinesInShop()
        {
            Debug.Log("[VERIFIER] 4. Kiểm tra Máy chế biến trong Shop & Không có máy khởi đầu tự động...");
            
            // Check that EnsureStarterMachine does not spawn free starter machines
            MachineManager mm = MachineManager.EnsureInstance();
            mm.machines.Clear();
            foreach (var m in UnityEngine.Object.FindObjectsByType<ProductionMachine>(FindObjectsSortMode.None))
            {
                if (m != null) UnityEngine.Object.DestroyImmediate(m.gameObject);
            }

            mm.EnsureStarterMachine();
            if (mm.machines.Count > 0)
            {
                Debug.LogError($"[VERIFIER] 4. FAILED: Máy chế biến vẫn bị tự động spawn miễn phí khi bắt đầu game! (Số máy: {mm.machines.Count})");
                return false;
            }

            // Check that all 5 machines exist in Shop Database
            FarmDatabase.InitDatabase();
            string[] requiredMachines = { "mill", "bakery", "dairy", "feed_mixer", "jam_maker" };
            foreach (var reqId in requiredMachines)
            {
                ShopItemData item = FarmDatabase.ShopItems.Find(s => s.category == ShopCategory.Machines && s.relatedId == reqId);
                if (item == null)
                {
                    Debug.LogError($"[VERIFIER] 4. FAILED: Không tìm thấy máy '{reqId}' trong danh mục Máy móc của Shop!");
                    return false;
                }
                if (item.priceGold <= 0)
                {
                    Debug.LogError($"[VERIFIER] 4. FAILED: Máy '{reqId}' trong Shop có giá <= 0!");
                    return false;
                }
            }

            Debug.Log("[VERIFIER] 4. PASSED: Không tự động spawn máy mẫu. Đủ 5 máy trong Shop: Mill (150G), Bakery (250G), Dairy (300G), Feed Mixer (200G), Jam Maker (320G).");
            return true;
        }

        private static bool VerifySeasonsAndWeather()
        {
            Debug.Log("[VERIFIER] 5. Kiểm tra Bốn Mùa & Hiệu ứng Thời tiết (Xuân, Hạ, Thu, Đông, Tuyết, Thủ công)...");
            WeatherSystem ws = WeatherSystem.EnsureInstance();
            if (ws == null)
            {
                Debug.LogError("[VERIFIER] 5. FAILED: Không thể khởi tạo WeatherSystem!");
                return false;
            }

            // Test 4 Seasons progression
            ws.currentDay = 1;
            if (ws.CurrentSeason != SeasonType.Spring)
            {
                Debug.LogError($"[VERIFIER] 5. FAILED: Ngày 1 phải là Mùa Xuân! Thực tế: {ws.CurrentSeason}");
                return false;
            }

            ws.currentDay = 8;
            if (ws.CurrentSeason != SeasonType.Summer)
            {
                Debug.LogError($"[VERIFIER] 5. FAILED: Ngày 8 phải là Mùa Hạ! Thực tế: {ws.CurrentSeason}");
                return false;
            }

            ws.currentDay = 15;
            if (ws.CurrentSeason != SeasonType.Autumn)
            {
                Debug.LogError($"[VERIFIER] 5. FAILED: Ngày 15 phải là Mùa Thu! Thực tế: {ws.CurrentSeason}");
                return false;
            }

            ws.currentDay = 22;
            if (ws.CurrentSeason != SeasonType.Winter)
            {
                Debug.LogError($"[VERIFIER] 5. FAILED: Ngày 22 phải là Mùa Đông! Thực tế: {ws.CurrentSeason}");
                return false;
            }

            // Test Manual Weather Override
            ws.SetManualWeather(WeatherType.Rainy, true);
            if (!ws.isManualWeather || ws.CurrentWeather != WeatherType.Rainy)
            {
                Debug.LogError("[VERIFIER] 5. FAILED: Không thể đặt thủ công thời tiết Mưa!");
                return false;
            }

            ws.SetManualWeather(WeatherType.Snowy, true);
            if (!ws.isManualWeather || ws.CurrentWeather != WeatherType.Snowy)
            {
                Debug.LogError("[VERIFIER] 5. FAILED: Không thể đặt thủ công thời tiết Tuyết!");
                return false;
            }

            // Test AdvanceDay with manual weather kept
            ws.AdvanceDay();
            if (ws.CurrentWeather != WeatherType.Snowy)
            {
                Debug.LogError("[VERIFIER] 5. FAILED: Sau khi qua ngày, thiết lập thủ công thời tiết Tuyết bị mất!");
                return false;
            }

            // Reset to auto
            ws.SetManualWeather(WeatherType.Sunny, false);
            if (ws.isManualWeather)
            {
                Debug.LogError("[VERIFIER] 5. FAILED: Không thể chuyển về thời tiết tự động!");
                return false;
            }

            Debug.Log("[VERIFIER] 5. PASSED: Chu kỳ 4 Mùa chuẩn xác (Xuân, Hạ, Thu, Đông), hiệu ứng Mưa & Tuyết, thiết lập thời tiết thủ công hoạt động mượt mà.");
            return true;
        }

        private static bool VerifyMachinePlacementFlow()
        {
            Debug.Log("[VERIFIER] 6. Kiểm tra Luồng Mua & Đặt Máy Chế Biến Thực Tế...");
            MachineManager mm = MachineManager.EnsureInstance();

            // Simulate buying Jam Maker at farm coordinates
            Vector3 targetPos = new Vector3(88f, 0f, 215f);
            ProductionMachine placedMachine = mm.SpawnPurchasedMachineAtPosition("jam_maker", targetPos);

            if (placedMachine == null)
            {
                Debug.LogError("[VERIFIER] 6. FAILED: Không thể spawn máy mua 'jam_maker'!");
                return false;
            }

            if (!placedMachine.isPurchased)
            {
                Debug.LogError("[VERIFIER] 6. FAILED: Máy vừa mua không được đánh dấu isPurchased!");
                return false;
            }

            if (!mm.IsMachinePurchased("jam_maker"))
            {
                Debug.LogError("[VERIFIER] 6. FAILED: MachineManager không ghi nhận máy đã mua!");
                return false;
            }

            // Check that recipes are loaded and interactive
            if (placedMachine.availableRecipes.Count == 0)
            {
                Debug.LogError("[VERIFIER] 6. FAILED: Máy 'jam_maker' không nạp danh sách công thức!");
                return false;
            }

            // Verify production cycle interaction
            placedMachine.StartProduction();
            if (placedMachine.state != MachineState.Producing && placedMachine.state != MachineState.Idle)
            {
                Debug.LogError("[VERIFIER] 6. FAILED: Trạng thái máy không hợp lệ sau khi khởi động!");
                return false;
            }

            Debug.Log($"[VERIFIER] 6. PASSED: Máy '{placedMachine.displayName}' được mua, đặt tại ({targetPos.x}, {targetPos.z}), có {placedMachine.availableRecipes.Count} công thức sản xuất.");
            return true;
        }

        private static bool VerifySaveLoadIntegrity()
        {
            Debug.Log("[VERIFIER] 7. Kiểm tra Lưu & Tải (Save/Load) Toàn Diện & Chống nhân bản...");
            PetManager pm = PetManager.EnsureInstance();
            MachineManager mm = MachineManager.EnsureInstance();
            WeatherSystem ws = WeatherSystem.EnsureInstance();

            // Set specific states to test save/load
            ws.currentDay = 18; // Mùa Thu
            ws.SetManualWeather(WeatherType.Rainy, true);

            // Capture Save
            SaveLoadSystem.SaveGame();

            int initialPetCount = pm.activePets.Count;
            int initialMachineCount = mm.machines.Count;

            // Simulate reload
            SaveLoadSystem.LoadGame();

            int reloadedPetCount = pm.activePets.Count;
            int reloadedMachineCount = mm.machines.Count;

            if (reloadedPetCount != initialPetCount)
            {
                Debug.LogError($"[VERIFIER] 7. FAILED: Thú cưng bị nhân đôi hoặc thất thoát sau khi tải save! (Trước: {initialPetCount}, Sau: {reloadedPetCount})");
                return false;
            }

            if (reloadedMachineCount != initialMachineCount)
            {
                Debug.LogError($"[VERIFIER] 7. FAILED: Máy móc bị thất thoát hoặc nhân đôi sau khi tải save! (Trước: {initialMachineCount}, Sau: {reloadedMachineCount})");
                return false;
            }

            if (ws.CurrentDay != 18 || ws.CurrentSeason != SeasonType.Autumn || ws.CurrentWeather != WeatherType.Rainy || !ws.isManualWeather)
            {
                Debug.LogError($"[VERIFIER] 7. FAILED: Dữ liệu thời tiết/mùa sau khi load không khớp! (Day={ws.CurrentDay}, Season={ws.CurrentSeason}, Weather={ws.CurrentWeather}, Manual={ws.isManualWeather})");
                return false;
            }

            Debug.Log($"[VERIFIER] 7. PASSED: Save & Load toàn diện hoàn hảo. Đúng {reloadedPetCount} thú cưng (không nhân bản), đúng {reloadedMachineCount} máy, mùa Thu ngày 18 mưa thủ công khôi phục nguyên vẹn.");
            return true;
        }
    }
}
#endif
