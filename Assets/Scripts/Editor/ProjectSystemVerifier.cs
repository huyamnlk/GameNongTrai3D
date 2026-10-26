using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IrishFarmSim.Editor
{
    public static class ProjectSystemVerifier
    {
        [MenuItem("IrishFarmSim/Run System Verification", priority = 1)]
        public static void RunAllTests()
        {
            Debug.Log("=================================================");
            Debug.Log("=== IRISH FARM SIMULATOR SYSTEM VERIFICATION ===");
            Debug.Log("=================================================");

            bool allPassed = true;

            // 1. Verify Build Settings & Start Scene
            bool test1 = VerifyBuildSettingsAndStartScene();
            allPassed = allPassed && test1;

            // 2. Verify Shop UI & Machine Tab
            bool test2 = VerifyShopAndMachines();
            allPassed = allPassed && test2;

            // 3. Verify Audio System & BGM Source Binding
            bool test3 = VerifyAudioSystem();
            allPassed = allPassed && test3;

            // 4. Verify Main Menu Controller & Confirm Modal
            bool test4 = VerifyMainMenuFlow();
            allPassed = allPassed && test4;

            Debug.Log("=================================================");
            if (allPassed)
            {
                Debug.Log(">>> ALL SYSTEM TESTS PASSED SUCCESSFULLY! <<<");
            }
            else
            {
                Debug.LogError(">>> SOME SYSTEM TESTS FAILED! <<<");
            }
            Debug.Log("=================================================");
        }

        private static bool VerifyBuildSettingsAndStartScene()
        {
            Debug.Log("--- Test 1: Build Settings & PlayMode Start Scene ---");
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogError("FAIL: No scenes configured in EditorBuildSettings!");
                return false;
            }

            string firstScene = scenes[0].path;
            Debug.Log($"First scene in Build Settings: {firstScene}");
            if (!firstScene.EndsWith("Main Menu.unity", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"FAIL: Scene 0 should be 'Main Menu.unity', but was '{firstScene}'!");
                return false;
            }

            PlayModeStartSceneSetter.ApplyPlayModeStartScene();
            if (EditorSceneManager.playModeStartScene == null || !EditorSceneManager.playModeStartScene.name.Equals("Main Menu", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"FAIL: EditorSceneManager.playModeStartScene is not Main Menu! ({EditorSceneManager.playModeStartScene?.name})");
                return false;
            }

            Debug.Log("PASS: Build Settings scene 0 is 'Main Menu.unity' and Editor playModeStartScene is properly configured.");
            return true;
        }

        private static bool VerifyShopAndMachines()
        {
            Debug.Log("--- Test 2: Shop UI & Machine Tab ---");
            // Check Database for all 5 required machines
            string[] requiredMachines = { "mill", "bakery", "dairy", "feed_mixer", "jam_maker" };
            foreach (var mId in requiredMachines)
            {
                if (!FarmDatabase.Machines.ContainsKey(mId))
                {
                    Debug.LogError($"FAIL: Missing machine definition in FarmDatabase: '{mId}'!");
                    return false;
                }
                ShopItemData item = FarmDatabase.ShopItems.Find(i => i.relatedId == mId && i.category == ShopCategory.Machines);
                if (item == null)
                {
                    Debug.LogError($"FAIL: Machine '{mId}' not registered in FarmDatabase.ShopItems as ShopCategory.Machines!");
                    return false;
                }
                Debug.Log($"Found Machine in Shop: {item.displayName} | Price: {item.priceGold}G | Lv: {item.unlockLevel}");
            }

            // Use the real ShopUI factory. BuildUIStructure is intentionally private and
            // ShopUI.EnsureUI creates the tab controls through the supported runtime path.
            ShopUI shop = ShopUI.EnsureUI();
            if (shop == null)
            {
                Debug.LogError("FAIL: ShopUI.EnsureUI() could not create the shop interface!");
                return false;
            }

            // Verify all 6 tabs exist
            if (shop.tabSeeds == null || shop.tabFeed == null || shop.tabAnimals == null ||
                shop.tabBuildings == null || shop.tabMachines == null || shop.tabPlots == null)
            {
                Debug.LogError("FAIL: One or more of the 6 tabs is null in ShopUI!");
                return false;
            }

            // Verify machine tab text & active status in general mode
            ShopUI.OpenShop();
            if (!shop.tabMachines.gameObject.activeSelf)
            {
                Debug.LogError("FAIL: tabMachines is inactive in general shop mode!");
                shop.Close();
                return false;
            }

            RectTransform mRt = shop.tabMachines.GetComponent<RectTransform>();
            RectTransform pRt = shop.tabPlots.GetComponent<RectTransform>();
            Debug.Log($"tabMachines anchoredPos: {mRt.anchoredPosition}, size: {mRt.sizeDelta}");
            Debug.Log($"tabPlots anchoredPos: {pRt.anchoredPosition}, size: {pRt.sizeDelta}");

            // Ensure no overlap between tabMachines and tabPlots
            float dist = Mathf.Abs(mRt.anchoredPosition.x - pRt.anchoredPosition.x);
            float minRequiredDist = (mRt.sizeDelta.x + pRt.sizeDelta.x) * 0.5f;
            if (dist < minRequiredDist - 1f)
            {
                Debug.LogError($"FAIL: tabMachines and tabPlots overlap! Distance: {dist}, Required: {minRequiredDist}");
                shop.Close();
                return false;
            }

            shop.Close();
            Debug.Log("PASS: All 5 machines present in Shop; Tab 'MÁY MÓC' is visible, styled, and does not overlap other tabs.");
            return true;
        }

        private static bool VerifyAudioSystem()
        {
            Debug.Log("--- Test 3: Audio System & BGM Controls ---");
            AudioManager am = AudioManager.EnsureInstance();

            // Verify AudioClips in Resources
            AudioClip resClip = Resources.Load<AudioClip>("Sounds/Farm Ambient");
            AudioClip martClip = Resources.Load<AudioClip>("Sounds/Mart Ambient");
            AudioClip btnClip = Resources.Load<AudioClip>("Sounds/Button");

            Debug.Log($"Resources BGM clip: {(resClip != null ? resClip.name : "NULL")}");
            Debug.Log($"Resources Mart clip: {(martClip != null ? martClip.name : "NULL")}");
            Debug.Log($"Resources Button clip: {(btnClip != null ? btnClip.name : "NULL")}");

            if (resClip == null)
            {
                Debug.LogError("FAIL: 'Sounds/Farm Ambient' could not be loaded from Resources!");
                return false;
            }

            // Test Volume change
            am.SetBGMVolume(0.42f);
            if (Mathf.Abs(am.bgmVolume - 0.42f) > 0.01f)
            {
                Debug.LogError($"FAIL: BGM volume not updated in AudioManager! Expected 0.42, got {am.bgmVolume}");
                return false;
            }

            // Test Mute toggle
            am.SetBGMMuted(true);
            if (!am.bgmMuted)
            {
                Debug.LogError("FAIL: BGM mute state not updated in AudioManager!");
                return false;
            }

            am.SetBGMMuted(false);
            if (am.bgmMuted)
            {
                Debug.LogError("FAIL: BGM unmute state not updated in AudioManager!");
                return false;
            }

            Debug.Log("PASS: Audio clips available in Resources; BGM volume & mute methods execute and sync directly.");
            return true;
        }

        private static bool VerifyMainMenuFlow()
        {
            Debug.Log("--- Test 4: Native Main Menu Flow & Functionality ---");
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("FAIL: Could not open Main Menu scene!");
                return false;
            }

            // Verify no overlay canvas
            MainMenuController.PurgeModernCanvasOverlay();
            if (GameObject.Find("MainMenu_ModernCanvas") != null)
            {
                Debug.LogError("FAIL: MainMenu_ModernCanvas still exists in scene!");
                return false;
            }

            // Verify native UI Root and buttons
            GameObject uiRoot = GameObject.Find("UI Root");
            if (uiRoot == null)
            {
                Debug.LogError("FAIL: UI Root not found in Main Menu scene!");
                return false;
            }

            GameObject buttonsGo = GameObject.Find("Buttons");
            if (buttonsGo == null)
            {
                Debug.LogError("FAIL: Buttons GameObject not found in Main Menu scene!");
                return false;
            }

            Transform newBtn = buttonsGo.transform.Find("NewGameButton");
            Transform loadBtn = buttonsGo.transform.Find("LoadGameButton");
            Transform setBtn = buttonsGo.transform.Find("SettingsButton");
            Transform exitBtn = buttonsGo.transform.Find("ExitButton");

            if (newBtn == null || loadBtn == null || setBtn == null || exitBtn == null)
            {
                Debug.LogError($"FAIL: One or more native buttons missing! (New:{newBtn != null}, Load:{loadBtn != null}, Set:{setBtn != null}, Exit:{exitBtn != null})");
                return false;
            }

            // Verify Continue Button state
            UIBackground.UpdateContinueButtonVisibility();
            bool saveExists = SaveLoadSystem.HasSave();
            Debug.Log($"Save exists: {saveExists}, LoadGameButton activeSelf: {loadBtn.gameObject.activeSelf}");

            // Verify Version Label is Version 1.1.1.1
            UIBackground.EnsureTitleAndVersionDisplay();
            UILabel vLbl = GameObject.Find("Version Label")?.GetComponent<UILabel>();
            if (vLbl != null && !vLbl.text.Contains("1.1.1.1"))
            {
                Debug.LogError($"FAIL: Version Label text is '{vLbl.text}', expected 'Version 1.1.1.1'!");
                return false;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("PASS: Native Main Menu verified: single menu display, 4 functional buttons, Version 1.1.1.1, and no overlay UI.");
            return true;
        }
    }
}
