using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IrishFarmSim;

namespace IrishFarmSim.EditorTools
{
    public static class FoodProcessingAndPetsVerifier
    {
        [MenuItem("Farm Tools/Verify Processing Machines and Pets")]
        public static void RunVerification()
        {
            Debug.Log("==================================================================");
            Debug.Log("[VERIFIER] KIỂM TRA KHU CHẾ BIẾN THỰC PHẨM & THÚ CƯNG TRANG TRẠI");
            Debug.Log("==================================================================");

            int passed = 0;
            int total = 6;

            // 1. Machine Database & Multi-recipe verification
            try
            {
                FarmDatabase.InitDatabase();
                bool hasMill = FarmDatabase.Machines.ContainsKey("mill");
                bool hasBakery = FarmDatabase.Machines.ContainsKey("bakery");
                bool hasDairy = FarmDatabase.Machines.ContainsKey("dairy");
                bool hasFeedMixer = FarmDatabase.Machines.ContainsKey("feed_mixer");
                bool hasJamMaker = FarmDatabase.Machines.ContainsKey("jam_maker");

                MachineData bakery = FarmDatabase.Machines["bakery"];
                bool bakeryHasBread = bakery.FindRecipe("bake_bread") != null;
                bool bakeryHasCake = bakery.FindRecipe("bake_cake") != null;

                MachineData dairy = FarmDatabase.Machines["dairy"];
                bool dairyHasCheese = dairy.FindRecipe("milk_to_cheese") != null;

                MachineData jam = FarmDatabase.Machines["jam_maker"];
                bool jamHasJam = jam.FindRecipe("strawberry_jam") != null;
                bool jamHasTomato = jam.FindRecipe("tomato_juice") != null;
                bool jamHasWatermelon = jam.FindRecipe("watermelon_juice") != null;

                bool allDbValid = hasMill && hasBakery && hasDairy && hasFeedMixer && hasJamMaker
                                  && bakeryHasBread && bakeryHasCake && dairyHasCheese
                                  && jamHasJam && jamHasTomato && jamHasWatermelon;

                Debug.Log($"[VERIFIER] 1. Cấu hình máy & Công thức: {(allDbValid ? "PASSED" : "FAILED")} " +
                          $"(5 máy: Mill/Bakery/Dairy/FeedMixer/JamMaker, Bánh ngọt: {bakeryHasCake}, Mứt/Nước ép: {jamHasJam && jamHasTomato})");
                if (allDbValid) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 1. Lỗi Cấu hình máy & Công thức: {ex.Message}");
            }

            // 2. Economy & Inventory integration
            try
            {
                string normCake = InventorySystem.NormalizeId("cake");
                string normJam = InventorySystem.NormalizeId("strawberry_jam");
                string normTomatoJ = InventorySystem.NormalizeId("tomato_juice");
                string normWatermelonJ = InventorySystem.NormalizeId("watermelon_juice");

                int priceCake = FarmDatabase.GetItemSellPrice("cake");
                int priceJam = FarmDatabase.GetItemSellPrice("strawberry_jam");
                int priceTomatoJ = FarmDatabase.GetItemSellPrice("tomato_juice");
                int priceWatermelonJ = FarmDatabase.GetItemSellPrice("watermelon_juice");

                bool allNormValid = normCake == "Cake" && normJam == "StrawberryJam"
                                    && normTomatoJ == "TomatoJuice" && normWatermelonJ == "WatermelonJuice";
                bool allPricesValid = priceCake > 0 && priceJam > 0 && priceTomatoJ > 0 && priceWatermelonJ > 0;

                bool econValid = allNormValid && allPricesValid;
                Debug.Log($"[VERIFIER] 2. Tích hợp Kho & Kinh tế (Bán hàng): {(econValid ? "PASSED" : "FAILED")} " +
                          $"(Giá Cake: {priceCake}G, Jam: {priceJam}G, TomatoJuice: {priceTomatoJ}G, MelonJuice: {priceWatermelonJ}G)");
                if (econValid) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 2. Lỗi Tích hợp Kho: {ex.Message}");
            }

            // 3. Machine Visuals & Animation Rig
            try
            {
                string[] testMachines = { "mill", "bakery", "dairy", "feed_mixer", "jam_maker" };
                bool allVisualsBuilt = true;

                foreach (var mId in testMachines)
                {
                    GameObject tempGo = new GameObject("TempMachine_" + mId);
                    MachineVisualFactory.Build(tempGo, mId);

                    Transform proc = tempGo.transform.Find(MachineVisualFactory.ModelRootName);
                    Renderer[] rends = tempGo.GetComponentsInChildren<Renderer>(true);
                    if (proc == null || rends.Length == 0)
                    {
                        allVisualsBuilt = false;
                        Debug.LogError($"[VERIFIER] Lỗi dựng mô hình máy {mId}!");
                    }
                    UnityEngine.Object.DestroyImmediate(tempGo);
                }

                Debug.Log($"[VERIFIER] 3. Mô hình 3D & Neo hiệu ứng máy móc: {(allVisualsBuilt ? "PASSED" : "FAILED")} (5 loại máy)");
                if (allVisualsBuilt) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 3. Lỗi Mô hình 3D máy móc: {ex.Message}");
            }

            // 4. Pet Visuals & Rigs
            try
            {
                bool dogGolden = VerifyPetModel(PetKind.Dog, "golden");
                bool dogSpotted = VerifyPetModel(PetKind.Dog, "spotted");
                bool catOrange = VerifyPetModel(PetKind.Cat, "orange");
                bool catGray = VerifyPetModel(PetKind.Cat, "gray");

                bool allPetsValid = dogGolden && dogSpotted && catOrange && catGray;
                Debug.Log($"[VERIFIER] 4. Mô hình 3D Chó & Mèo (4 loại ngoại hình): {(allPetsValid ? "PASSED" : "FAILED")}");
                if (allPetsValid) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 4. Lỗi Mô hình thú cưng: {ex.Message}");
            }

            // 5. Machine Production Logic Simulation
            try
            {
                InventorySystem inv = InventorySystem.EnsureInstance();
                inv.ClearAll();
                inv.AddItem("wheat", 10);
                inv.AddItem("egg", 10);
                inv.AddItem("flour", 10);
                inv.AddItem("milk", 10);

                GameObject tempM = new GameObject("TestBakery");
                ProductionMachine pm = tempM.AddComponent<ProductionMachine>();
                MachineData bData = FarmDatabase.Machines["bakery"];
                pm.SetupMachine(bData, true);

                // Select cake recipe
                pm.SelectRecipe("bake_cake");
                bool cakeSelected = pm.currentRecipe != null && pm.currentRecipe.recipeId == "bake_cake";

                // Start production
                int startFlour = inv.GetItemCount("flour");
                int startEgg = inv.GetItemCount("egg");
                int startMilk = inv.GetItemCount("milk");
                bool started = pm.StartProduction();

                int afterFlour = inv.GetItemCount("flour");
                int afterEgg = inv.GetItemCount("egg");
                int afterMilk = inv.GetItemCount("milk");

                bool ingredientsDeducted = (startFlour - afterFlour == 1) &&
                                          (startEgg - afterEgg == 1) &&
                                          (startMilk - afterMilk == 1);

                // Force complete
                pm.ForceComplete();
                bool completed = pm.state == MachineState.Completed;

                // Collect
                int startCake = inv.GetItemCount("cake");
                bool collected = pm.CollectProduct();
                int afterCake = inv.GetItemCount("cake");
                bool cakeReceived = (afterCake - startCake == 1);

                UnityEngine.Object.DestroyImmediate(tempM);

                bool simValid = cakeSelected && started && ingredientsDeducted && completed && collected && cakeReceived;
                Debug.Log($"[VERIFIER] 5. Quy trình Chế biến Thực phẩm (Bột + Trứng + Sữa ➔ Bánh ngọt): {(simValid ? "PASSED" : "FAILED")}");
                if (simValid) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 5. Lỗi Quy trình Chế biến: {ex.Message}");
            }

            // 6. Save / Load serialization check
            try
            {
                FarmSaveData testSave = new FarmSaveData();
                testSave.machines.Add(new MachineSaveData
                {
                    machineId = "jam_maker",
                    state = "Producing",
                    currentTimer = 5.5f,
                    recipeId = "strawberry_jam",
                    isPurchased = true,
                    posX = 128f,
                    posY = 0f,
                    posZ = 240f
                });

                testSave.pets.Add(new PetSaveData
                {
                    petId = "dog_golden",
                    petType = "Dog",
                    coatId = "golden",
                    posX = 82f,
                    posY = 0f,
                    posZ = 252f,
                    rotY = 45f
                });

                testSave.pets.Add(new PetSaveData
                {
                    petId = "cat_orange",
                    petType = "Cat",
                    coatId = "orange",
                    posX = 92f,
                    posY = 0f,
                    posZ = 236f,
                    rotY = 120f
                });

                string json = JsonUtility.ToJson(testSave, true);
                FarmSaveData loaded = JsonUtility.FromJson<FarmSaveData>(json);

                bool saveValid = loaded != null &&
                                 loaded.machines.Count == 1 &&
                                 loaded.machines[0].recipeId == "strawberry_jam" &&
                                 loaded.pets.Count == 2 &&
                                 loaded.pets[0].petId == "dog_golden" &&
                                 loaded.pets[1].petId == "cat_orange";

                Debug.Log($"[VERIFIER] 6. Lưu & Tải (Save/Load) Máy & Thú cưng: {(saveValid ? "PASSED" : "FAILED")}");
                if (saveValid) passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIER] 6. Lỗi Lưu & Tải: {ex.Message}");
            }

            Debug.Log("==================================================================");
            Debug.Log($"[VERIFIER] KẾT QUẢ: {passed}/{total} HỆ THỐNG ĐÃ VƯỢT QUA KIỂM TRA!");
            Debug.Log("==================================================================");
        }

        private static bool VerifyPetModel(PetKind kind, string coatId)
        {
            GameObject pet = new GameObject("TestPet");
            PetVisualFactory.Build(pet, kind, coatId);

            Transform body = pet.transform.Find("PetBody");
            Transform head = body != null ? body.Find("PetHead") : null;
            Transform tail = body != null ? body.Find("PetTail") : null;
            Transform legFL = pet.transform.Find("PetLeg_FL");
            Transform shadow = pet.transform.Find("BlobShadow");

            bool valid = body != null && head != null && tail != null && legFL != null && shadow != null;
            UnityEngine.Object.DestroyImmediate(pet);
            return valid;
        }
    }
}
