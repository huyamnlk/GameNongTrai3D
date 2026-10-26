using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Handles persistent saving and loading of Level, XP, Inventory, Farm Plots,
    /// and Animals state using JSON format with PlayerPrefs redundancy.
    /// </summary>
    public class SaveLoadSystem : MonoBehaviour
    {
        public static SaveLoadSystem Instance { get; private set; }

        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;

        public static string SaveFilePath
        {
            get
            {
                string p = Path.Combine(Application.persistentDataPath, "farm_save.json");
                if (File.Exists(p)) return p;
                string localLowFallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "DoAnNongTrai", "HayDay2D", "farm_save.json");
                if (File.Exists(localLowFallback)) return localLowFallback;
                string projectFallback = Path.Combine(Application.dataPath, "..", "farm_save_backup.json");
                if (File.Exists(projectFallback)) return Path.GetFullPath(projectFallback);
                return p;
            }
        }
        private const string PREFS_KEY = "FARM_JSON_SAVE";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                InternalSave();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                InternalLoad();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                FarmEditModeController.EnsureInstance().ToggleEditMode();
            }
        }

        void OnGUI()
        {
            // Interactive Save, Load, and Edit Mode buttons on screen
            float btnW = 85f;
            float btnH = 34f;
            float startX = Screen.width - 285f;
            float startY = Screen.height - 45f;

            GUI.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            if (GUI.Button(new Rect(startX, startY, btnW, btnH), FarmEditModeController.IsEditModeActive ? "✓ EDIT" : "🛠 EDIT"))
            {
                FarmEditModeController.EnsureInstance().ToggleEditMode();
            }
            if (GUI.Button(new Rect(startX + 95f, startY, btnW, btnH), "💾 SAVE"))
            {
                InternalSave();
            }
            if (GUI.Button(new Rect(startX + 190f, startY, btnW, btnH), "📂 LOAD"))
            {
                InternalLoad();
            }
        }

        public static bool HasSave()
        {
            try
            {
                if (File.Exists(SaveFilePath)) return true;
                if (PlayerPrefs.HasKey(PREFS_KEY) && !string.IsNullOrEmpty(PlayerPrefs.GetString(PREFS_KEY))) return true;
                if (File.Exists(Path.Combine(Application.persistentDataPath, "player.dat"))) return true;
                if (File.Exists(Path.Combine(Application.persistentDataPath, "cows.dat"))) return true;
            }
            catch {}
            return false;
        }

        public static void InitNewGameDefaults()
        {
            try
            {
                FarmSaveData defaultData = new FarmSaveData();
                defaultData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                defaultData.level = 1;
                defaultData.currentXP = 0;
                defaultData.gold = 100;
                defaultData.storageCapacity = 50;

                // 3 seeds for each plant type
                defaultData.inventory.Add(new InventoryItemSaveData { id = "WheatSeed", quantity = 3 });
                defaultData.inventory.Add(new InventoryItemSaveData { id = "CornSeed", quantity = 3 });
                defaultData.inventory.Add(new InventoryItemSaveData { id = "CarrotSeed", quantity = 3 });
                defaultData.inventory.Add(new InventoryItemSaveData { id = "HaySeed", quantity = 3 });

                // Initial cattle feed
                defaultData.inventory.Add(new InventoryItemSaveData { id = "Grain", quantity = 10 });
                defaultData.inventory.Add(new InventoryItemSaveData { id = "Hay", quantity = 10 });
                defaultData.inventory.Add(new InventoryItemSaveData { id = "Pellets", quantity = 5 });

                string json = JsonUtility.ToJson(defaultData, true);
                File.WriteAllText(SaveFilePath, json);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveLoadSystem] Initialized new game default save successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] Error initializing new game: {ex.Message}");
            }
        }

        public static void SaveGame()
        {
            EnsureInstance();
            if (Instance != null) Instance.InternalSave();
        }

        public static void LoadGame()
        {
            EnsureInstance();
            if (Instance != null) Instance.InternalLoad();
        }

        public void InternalSave()
        {
            try
            {
                FarmSaveData data = new FarmSaveData();
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // 1. Save Progression
                if (PlayerProgression.Instance != null)
                {
                    data.level = PlayerProgression.Instance.Level;
                    data.currentXP = PlayerProgression.Instance.CurrentXP;
                    data.gold = PlayerProgression.Instance.Gold;
                }

                // 2. Save Inventory & Storage Capacity
                if (InventorySystem.Instance != null)
                {
                    data.storageCapacity = InventorySystem.Instance.maxStorageCapacity;
                    foreach (var item in InventorySystem.Instance.GetAllItems())
                    {
                        data.inventory.Add(new InventoryItemSaveData
                        {
                            id = item.id,
                            quantity = item.quantity
                        });
                    }
                }

                // 2.5 Save Buildings & Machines
                if (FarmBuildingManager.Instance != null)
                {
                    data.purchasedBuildings = FarmBuildingManager.Instance.GetPurchasedBuildingIds();
                    data.purchasedBuildings.RemoveAll(id => id.Equals("machine_shed", StringComparison.OrdinalIgnoreCase));
                    foreach (var kvp in FarmBuildingManager.Instance.buildings)
                    {
                        FarmBuilding fb = kvp.Value;
                        if (FarmBuilding.IsValidBuilding(fb) && fb.isPurchased && !fb.buildingId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase))
                        {
                            Vector3 pos = fb.transform.position;
                            Vector3 scl = fb.transform.localScale;
                            data.buildingsData.Add(new BuildingSaveData
                            {
                                buildingId = fb.buildingId,
                                buildingType = fb.buildingType,
                                posX = pos.x,
                                posY = pos.y,
                                posZ = pos.z,
                                rotY = fb.transform.eulerAngles.y,
                                scaleX = scl.x,
                                scaleY = scl.y,
                                scaleZ = scl.z,
                                state = fb.isPurchased ? "Purchased" : "Normal",
                                isPurchased = fb.isPurchased
                            });
                        }
                    }
                }

                if (MachineManager.Instance != null)
                {
                    MachineManager.Instance.PruneAndScanSceneMachines();
                    foreach (var kvp in MachineManager.Instance.machines)
                    {
                        ProductionMachine pm = kvp.Value;
                        if (pm != null)
                        {
                            Vector3 mPos = pm.transform.position;
                            Vector3 mScl = pm.transform.localScale;
                            data.machines.Add(new MachineSaveData
                            {
                                machineId = pm.machineId,
                                state = pm.state.ToString(),
                                currentTimer = pm.currentTimer,
                                pendingOutputAmount = pm.pendingOutputAmount,
                                isPurchased = pm.isPurchased,
                                posX = mPos.x,
                                posY = mPos.y,
                                posZ = mPos.z,
                                rotY = pm.transform.eulerAngles.y,
                                scaleX = mScl.x,
                                scaleY = mScl.y,
                                scaleZ = mScl.z,
                                hasCustomPosition = true,
                                recipeId = pm.currentRecipe != null ? pm.currentRecipe.recipeId : ""
                            });
                        }
                    }
                }

                // 2.7 Save farm pets (dogs & cats)
                if (PetManager.Instance != null)
                {
                    data.pets = PetManager.Instance.CaptureSaveData();
                }

                // 3. Save Farm Plots
                if (PlayerProgression.Instance != null)
                {
                    data.unlockedPlotIds = new List<string>(PlayerProgression.Instance.unlockedPlotIds);
                }
                CropPlot[] plots = FindObjectsByType<CropPlot>();
                foreach (var p in plots)
                {
                    Vector3 pPos = p.transform.position;
                    data.plots.Add(new PlotSaveData
                    {
                        plotName = p.gameObject.name,
                        state = p.state.ToString(),
                        cropId = p.currentCrop != null ? p.currentCrop.cropId : "",
                        growthTimer = p.growthTimer,
                        posX = pPos.x,
                        posY = pPos.y,
                        posZ = pPos.z,
                        rotY = p.transform.eulerAngles.y,
                        hasCustomPosition = true,
                        isUnlocked = p.isUnlocked,
                        unlockLevel = p.unlockLevel
                    });
                }

                // 4. Save Animals
                AnimalManager.PurgeShowcaseAndSpriteArtifacts();
                AnimalCareController[] animals = FindObjectsByType<AnimalCareController>();
                Dictionary<string, int> savedPerBuilding = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in animals)
                {
                    if (a == null || !a) continue;
                    if (a.gameObject.name.StartsWith("Showcase_")) continue;

                    FarmBuilding parentBuilding = a.GetComponentInParent<FarmBuilding>();
                    string bId = parentBuilding != null ? parentBuilding.buildingId : "";
                    if (string.IsNullOrEmpty(bId) ||
                        (a.animalType == AnimalType.Chicken && bId != "chicken_coop") ||
                        (a.animalType == AnimalType.Pig && bId != "pig_pen") ||
                        (a.animalType == AnimalType.Horse && bId != "horse_stable") ||
                        (a.animalType == AnimalType.Cow && bId != "cow_barn"))
                    {
                        if (a.animalType == AnimalType.Chicken) bId = "chicken_coop";
                        else if (a.animalType == AnimalType.Pig) bId = "pig_pen";
                        else if (a.animalType == AnimalType.Horse) bId = "horse_stable";
                        else bId = "cow_barn";
                    }

                    int maxCap = (bId == "cow_barn") ? 3 : (bId == "chicken_coop" ? 5 : 4);
                    if (FarmBuildingManager.Instance != null)
                    {
                        FarmBuilding bObj = FarmBuildingManager.Instance.GetBuilding(bId);
                        if (bObj != null && bObj.capacity > 0) maxCap = bObj.capacity;
                    }

                    savedPerBuilding.TryGetValue(bId, out int countSoFar);
                    if (countSoFar >= maxCap) continue;
                    savedPerBuilding[bId] = countSoFar + 1;

                    string cleanName = a.animalName;
                    if (string.IsNullOrEmpty(cleanName) || cleanName.Trim() == "Bò")
                    {
                        cleanName = $"Bò sữa (Cow) #{countSoFar + 1}";
                        a.animalName = cleanName;
                    }

                    Vector3 aPos = a.transform.position;
                    data.animals.Add(new AnimalSaveData
                    {
                        animalName = cleanName,
                        animalType = a.animalType.ToString(),
                        parentBuildingId = bId,
                        hunger = a.hunger,
                        happiness = a.happiness,
                        affection = a.affection,
                        productTimer = a.productTimer,
                        isProductReady = a.isProductReady,
                        posX = aPos.x,
                        posY = aPos.y,
                        posZ = aPos.z,
                        rotY = a.transform.eulerAngles.y,
                        hasCustomPosition = true
                    });
                }

                // 5. Save Moved Decor, Fences & Farm Objects (Requirement C & Map Objects)
                MovableFarmObject[] allMovables = FindObjectsByType<MovableFarmObject>(FindObjectsInactive.Include);
                foreach (var m in allMovables)
                {
                    if (m == null || string.IsNullOrEmpty(m.objectId)) continue;
                    Vector3 pos = m.transform.position;
                    Vector3 scl = m.transform.localScale;
                    bool del = m.isDeleted;
                    if (m.objectType != MovableType.Machine && !m.gameObject.activeSelf) del = true;
                    if (del && !data.deletedObjectIds.Contains(m.objectId))
                    {
                        data.deletedObjectIds.Add(m.objectId);
                    }
                    data.movedObjects.Add(new MovableObjectSaveData
                    {
                        objectId = m.objectId,
                        objectType = m.objectType.ToString(),
                        posX = pos.x,
                        posY = pos.y,
                        posZ = pos.z,
                        rotY = m.transform.eulerAngles.y,
                        scaleX = scl.x,
                        scaleY = scl.y,
                        scaleZ = scl.z,
                        parentId = m.transform.parent != null ? m.transform.parent.name : "",
                        isDeleted = del
                    });
                }

                // 6. Save Weather & Day Progression
                if (WeatherSystem.Instance != null)
                {
                    data.currentDay = WeatherSystem.Instance.CurrentDay;
                    data.currentWeather = WeatherSystem.Instance.CurrentWeather.ToString();
                    data.currentSeason = WeatherSystem.Instance.CurrentSeason.ToString();
                    data.isManualWeather = WeatherSystem.Instance.isManualWeather;
                    data.manualWeather = WeatherSystem.Instance.manualWeather.ToString();
                }

                // 7. Save Daily Quests
                if (QuestSystem.Instance != null)
                {
                    data.totalCompletedQuestsCount = QuestSystem.Instance.totalCompletedQuestsCount;
                    foreach (var q in QuestSystem.Instance.activeQuests)
                    {
                        if (q == null) continue;
                        data.dailyQuests.Add(new QuestSaveData
                        {
                            questId = q.questId,
                            title = q.title,
                            description = q.description,
                            type = q.type.ToString(),
                            targetCount = q.targetCount,
                            currentCount = q.currentCount,
                            rewardGold = q.rewardGold,
                            rewardXP = q.rewardXP,
                            isClaimed = q.isClaimed
                        });
                    }
                }

                // 8. Save Achievements
                if (AchievementSystem.Instance != null)
                {
                    data.totalCropsHarvested = AchievementSystem.Instance.totalCropsHarvested;
                    data.totalDecorationsMovedOrPlaced = AchievementSystem.Instance.totalDecorationsMovedOrPlaced;
                    foreach (var ach in AchievementSystem.Instance.achievements)
                    {
                        if (ach == null) continue;
                        data.achievements.Add(new AchievementSaveData
                        {
                            id = ach.id,
                            title = ach.title,
                            description = ach.description,
                            targetCount = ach.targetCount,
                            currentCount = ach.currentCount,
                            rewardGold = ach.rewardGold,
                            rewardXP = ach.rewardXP,
                            isClaimed = ach.isClaimed
                        });
                    }
                }

                // Create backup before overwriting (Requirement C)
                try
                {
                    if (File.Exists(SaveFilePath))
                    {
                        string dir = Path.GetDirectoryName(SaveFilePath);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            File.Copy(SaveFilePath, Path.Combine(dir, "farm_save_backup_prev.json"), true);
                        }
                    }
                }
                catch {}

                string json = JsonUtility.ToJson(data, true);

                // Write to file
                File.WriteAllText(SaveFilePath, json);
                try
                {
                    string backupPath = Path.Combine(Application.dataPath, "..", "farm_save_backup.json");
                    File.WriteAllText(backupPath, json);
                }
                catch {}

                // Also write to PlayerPrefs as reliable cross-platform redundancy
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();

                Debug.Log($"[SaveLoadSystem] Game saved successfully to: {SaveFilePath}");
                OnGameSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to save game: {ex.Message}");
            }
        }

        public void InternalLoad()
        {
            try
            {
                AnimalManager.PurgeShowcaseAndSpriteArtifacts();

                string path = SaveFilePath;
                string json = "";

                if (File.Exists(path))
                {
                    Debug.Log("[SaveLoad] Existing save preserved");
                    Debug.Log($"[SaveLoad] Loading existing save: {path}");
                    json = File.ReadAllText(path);
                }
                else if (PlayerPrefs.HasKey(PREFS_KEY))
                {
                    Debug.Log("[SaveLoad] Existing save preserved");
                    Debug.Log($"[SaveLoad] Loading existing save: (PlayerPrefs)");
                    json = PlayerPrefs.GetString(PREFS_KEY);
                }
                else
                {
                    Debug.Log("[SaveLoad] No save found, creating new game");
                    InitNewGameDefaults();
                    return;
                }

                if (string.IsNullOrEmpty(json))
                {
                    Debug.Log("[SaveLoad] No save found, creating new game");
                    InitNewGameDefaults();
                    return;
                }

                FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[SaveLoadSystem] Failed to parse save data JSON!");
                    Debug.Log("[SaveLoad] No save found, creating new game");
                    InitNewGameDefaults();
                    return;
                }

                // 1. Restore Progression
                if (PlayerProgression.Instance != null)
                {
                    PlayerProgression.Instance.SetProgression(data.level, data.currentXP, data.gold);
                }

                // 2. Restore Inventory & Capacity
                if (InventorySystem.Instance != null)
                {
                    if (data.storageCapacity > 0)
                    {
                        InventorySystem.Instance.SetStorageCapacity(data.storageCapacity);
                    }
                    InventorySystem.Instance.ClearAll();
                    foreach (var itemData in data.inventory)
                    {
                        InventorySystem.Instance.AddItem(itemData.id, itemData.quantity);
                    }
                }

                // 2.5 Restore Buildings & Machines
                if (data.purchasedBuildings != null)
                {
                    data.purchasedBuildings.RemoveAll(id => id.Equals("machine_shed", StringComparison.OrdinalIgnoreCase));
                }
                if (data.buildingsData != null)
                {
                    data.buildingsData.RemoveAll(b => b != null && b.buildingId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase));
                }

                // Clean up any stray machine_shed objects in current scene
                FarmBuilding[] sceneBuildings = FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var fb in sceneBuildings)
                {
                    if (fb != null && (fb.buildingId == "machine_shed" || fb.buildingType == "machine_shed" ||
                        fb.name.ToLower().Contains("machine_shed") || fb.name.ToLower().Contains("machineshed")))
                    {
                        Debug.Log($"[SaveLoadSystem] Purging deleted machine_shed object: {fb.gameObject.name}");
                        SafeDestroy(fb.gameObject);
                    }
                }

                // Check if building records are missing and attempt recovery from backup files (Requirement 6)
                bool hasBuildingRecords = (data.purchasedBuildings != null && data.purchasedBuildings.Count > 0) ||
                                          (data.buildingsData != null && data.buildingsData.Count > 0);
                if (!hasBuildingRecords)
                {
                    Debug.LogWarning($"[SaveLoad] Current save from '{path}' contains 0 building records. Checking backup files...");
                    string[] backupCandidates = new string[]
                    {
                        Path.Combine(Application.dataPath, "..", "farm_save_backup.json"),
                        Path.Combine(Application.dataPath, "..", "farm_save_backup.json.safecopy"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "DoAnNongTrai", "HayDay2D", "farm_save.json")
                    };

                    foreach (var bPath in backupCandidates)
                    {
                        if (File.Exists(bPath))
                        {
                            try
                            {
                                string bJson = File.ReadAllText(bPath);
                                FarmSaveData backupData = JsonUtility.FromJson<FarmSaveData>(bJson);
                                if (backupData != null && ((backupData.purchasedBuildings != null && backupData.purchasedBuildings.Count > 0) ||
                                                          (backupData.buildingsData != null && backupData.buildingsData.Count > 0)))
                                {
                                    data.purchasedBuildings = backupData.purchasedBuildings;
                                    data.buildingsData = backupData.buildingsData;
                                    if ((data.animals == null || data.animals.Count == 0) && backupData.animals != null && backupData.animals.Count > 0)
                                    {
                                        data.animals = backupData.animals;
                                    }
                                    Debug.Log($"[SaveLoad] Successfully recovered building records from backup: {bPath}");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[SaveLoad] Failed reading backup '{bPath}': {ex.Message}");
                            }
                        }
                    }

                    if ((data.purchasedBuildings == null || data.purchasedBuildings.Count == 0) && (data.buildingsData == null || data.buildingsData.Count == 0))
                    {
                        Debug.LogWarning($"[SaveLoad] Could not recover buildings automatically. Active save path: '{path}', missing fields: purchasedBuildings, buildingsData.");
                    }
                }

                // Compute unique building records count
                HashSet<string> uniqueBuildingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (data.purchasedBuildings != null)
                {
                    foreach (var bId in data.purchasedBuildings)
                    {
                        if (!string.IsNullOrEmpty(bId) && !bId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase))
                            uniqueBuildingIds.Add(bId);
                    }
                }
                if (data.buildingsData != null)
                {
                    foreach (var b in data.buildingsData)
                    {
                        if (b != null && !string.IsNullOrEmpty(b.buildingId) && !b.buildingId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase))
                            uniqueBuildingIds.Add(b.buildingId);
                    }
                }
                int buildingRecordsCount = uniqueBuildingIds.Count;
                Debug.Log($"[SaveLoad] Building records loaded: {buildingRecordsCount}");
                Debug.Log($"[SaveLoad] Saved buildings found: {buildingRecordsCount}");

                FarmBuildingManager.EnsureInstance();
                if (FarmBuildingManager.Instance != null && (data.purchasedBuildings != null || data.buildingsData != null))
                {
                    FarmBuildingManager.Instance.RestorePurchasedBuildingsWithData(data.purchasedBuildings, data.buildingsData);
                }

                // 2.6 Restore Machines (respawns purchased machines that are not in the scene yet)
                if (MachineManager.Instance != null && data.machines != null && data.machines.Count > 0)
                {
                    List<string> savedPurchasedMachineIds = new List<string>();
                    foreach (var mSave in data.machines)
                    {
                        if (mSave == null || string.IsNullOrEmpty(mSave.machineId)) continue;
                        if (mSave.isPurchased) savedPurchasedMachineIds.Add(mSave.machineId);
                    }

                    MachineManager.Instance.RemoveMachinesNotInSave(savedPurchasedMachineIds);

                    foreach (var mSave in data.machines)
                    {
                        MachineManager.Instance.RestoreMachine(mSave);
                    }
                    MachineManager.Instance.PruneAndScanSceneMachines();
                }

                // 2.7 Restore farm pets (dogs & cats)
                PetManager.EnsureInstance();
                if (PetManager.Instance != null)
                {
                    PetManager.Instance.RestoreFromSaveData(data.pets);
                }

                // 3. Restore Farm Plots
                if (PlayerProgression.Instance != null && data.unlockedPlotIds != null && data.unlockedPlotIds.Count > 0)
                {
                    foreach (var uId in data.unlockedPlotIds)
                    {
                        PlayerProgression.Instance.UnlockPlot(uId);
                    }
                }

                CropPlot[] scenePlots = FindObjectsByType<CropPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Dictionary<string, CropPlot> plotMap = new Dictionary<string, CropPlot>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in scenePlots)
                {
                    if (p == null) continue;
                    string pName = string.IsNullOrEmpty(p.plotId) ? p.gameObject.name : p.plotId;
                    plotMap[pName] = p;
                }

                // If any saved plots exist in data, spawn any missing ones at their saved positions
                if (data.plots != null && CropManager.Instance != null)
                {
                    foreach (var ps in data.plots)
                    {
                        if (string.IsNullOrEmpty(ps.plotName)) continue;
                        if (!plotMap.ContainsKey(ps.plotName))
                        {
                            Vector3 pSpawnPos = new Vector3(ps.posX, ps.posY, ps.posZ);
                            Quaternion pSpawnRot = Quaternion.Euler(0f, ps.rotY, 0f);
                            if (pSpawnPos.sqrMagnitude < 0.01f)
                            {
                                CropPlot spawned = CropManager.Instance.SpawnSinglePlot(ps.plotName);
                                if (spawned != null) plotMap[ps.plotName] = spawned;
                            }
                            else
                            {
                                CropPlot spawned = CropManager.Instance.SpawnPlotAtPosition(ps.plotName, pSpawnPos, pSpawnRot);
                                if (spawned != null) plotMap[ps.plotName] = spawned;
                            }
                        }
                    }
                }

                // Also ensure any unlocked plots not yet in plotMap are spawned
                if (CropManager.Instance != null && data.unlockedPlotIds != null)
                {
                    foreach (var uId in data.unlockedPlotIds)
                    {
                        if (!plotMap.ContainsKey(uId))
                        {
                            CropPlot spawned = CropManager.Instance.SpawnSinglePlot(uId);
                            if (spawned != null) plotMap[uId] = spawned;
                        }
                    }
                }

                // Purge any scene plots that are not unlocked
                var allCurrentPlots = new List<CropPlot>(plotMap.Values);
                foreach (var p in allCurrentPlots)
                {
                    if (p == null) continue;
                    string pName = string.IsNullOrEmpty(p.plotId) ? p.gameObject.name : p.plotId;
                    bool isUnlocked = false;
                    if (FarmDatabase.Plots.TryGetValue(pName, out var pDb)) isUnlocked = pDb.isUnlockedByDefault;
                    if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(pName)) isUnlocked = true;

                    if (!isUnlocked)
                    {
                        plotMap.Remove(pName);
                        SafeDestroy(p.gameObject);
                    }
                }

                if (data.plots != null)
                {
                    foreach (var ps in data.plots)
                    {
                        if (plotMap.TryGetValue(ps.plotName, out CropPlot plot) && plot != null)
                        {
                            plot.isUnlocked = ps.isUnlocked;
                            if (ps.unlockLevel > 0) plot.unlockLevel = ps.unlockLevel;

                            if (Enum.TryParse(ps.state, out PlotState stateVal))
                            {
                                plot.state = stateVal;
                            }
                            else if (!plot.isUnlocked)
                            {
                                plot.state = PlotState.Locked;
                            }

                            if (!string.IsNullOrEmpty(ps.cropId))
                            {
                                plot.currentCrop = CropData.GetCrop(ps.cropId);
                            }
                            else
                            {
                                plot.currentCrop = null;
                            }

                            plot.growthTimer = ps.growthTimer;

                            if (ps.hasCustomPosition && (ps.posX != 0f || ps.posZ != 0f))
                            {
                                Vector3 pTargetPos = new Vector3(ps.posX, ps.posY, ps.posZ);
                                plot.transform.position = pTargetPos;
                                plot.transform.rotation = Quaternion.Euler(0f, ps.rotY, 0f);
                                GroundSnapUtility.SnapObjectToGround(plot.gameObject);
                            }

                            plot.UpdateVisuals();
                        }
                    }
                }

                // 4. Restore Animals (1-to-1 matching per AnimalType, strict capacity enforcement, cleanup of excess/duplicates)
                int animalCount = 0;
                AnimalCareController[] existingAnimals = FindObjectsByType<AnimalCareController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                List<AnimalCareController> unmatchedSceneAnimals = new List<AnimalCareController>();
                foreach (var a in existingAnimals)
                {
                    if (a != null && a)
                    {
                        unmatchedSceneAnimals.Add(a);
                    }
                }

                Dictionary<string, int> restoredPerBuilding = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (data.animals != null)
                {
                    foreach (var asave in data.animals)
                    {
                        if (asave == null) continue;

                        string targetBuildingId = asave.parentBuildingId;
                        AnimalType targetType = AnimalType.Cow;
                        if (Enum.TryParse(asave.animalType, true, out AnimalType parsedType))
                        {
                            targetType = parsedType;
                        }
                        else if (targetBuildingId == "chicken_coop") targetType = AnimalType.Chicken;
                        else if (targetBuildingId == "pig_pen") targetType = AnimalType.Pig;
                        else if (targetBuildingId == "horse_stable") targetType = AnimalType.Horse;

                        if (targetType == AnimalType.Chicken) targetBuildingId = "chicken_coop";
                        else if (targetType == AnimalType.Pig) targetBuildingId = "pig_pen";
                        else if (targetType == AnimalType.Horse) targetBuildingId = "horse_stable";
                        else targetBuildingId = "cow_barn";

                        FarmBuilding b = FarmBuildingManager.Instance != null ? FarmBuildingManager.Instance.GetBuilding(targetBuildingId) : null;
                        int maxCap = (b != null && b.capacity > 0) ? b.capacity :
                                     (targetBuildingId == "cow_barn" ? 3 : (targetBuildingId == "chicken_coop" ? 5 : 4));

                        restoredPerBuilding.TryGetValue(targetBuildingId, out int currentCountInPen);
                        if (currentCountInPen >= maxCap)
                        {
                            // Skip excess saved animals beyond pen capacity
                            continue;
                        }

                        int indexInPen = currentCountInPen + 1;
                        string normalizedName = asave.animalName;
                        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Trim() == "Bò" || normalizedName == "Bò sữa")
                        {
                            normalizedName = (targetType == AnimalType.Cow) ? $"Bò sữa (Cow) #{indexInPen}" :
                                             (targetType == AnimalType.Chicken ? $"Gà mái (Chicken) #{indexInPen}" :
                                             (targetType == AnimalType.Pig ? $"Heo hồng (Pig) #{indexInPen}" : $"Ngựa nâu (Horse) #{indexInPen}"));
                            asave.animalName = normalizedName;
                        }

                        // Find 1-to-1 match from existing scene animals of the same type
                        AnimalCareController matchedAcc = null;
                        for (int i = 0; i < unmatchedSceneAnimals.Count; i++)
                        {
                            var cand = unmatchedSceneAnimals[i];
                            if (cand != null && cand.animalType == targetType && cand.animalName == normalizedName)
                            {
                                matchedAcc = cand;
                                unmatchedSceneAnimals.RemoveAt(i);
                                break;
                            }
                        }
                        if (matchedAcc == null)
                        {
                            for (int i = 0; i < unmatchedSceneAnimals.Count; i++)
                            {
                                var cand = unmatchedSceneAnimals[i];
                                if (cand != null && cand.animalType == targetType)
                                {
                                    matchedAcc = cand;
                                    unmatchedSceneAnimals.RemoveAt(i);
                                    break;
                                }
                            }
                        }

                        if (matchedAcc == null && b != null && b.isPurchased && FarmBuildingManager.Instance != null)
                        {
                            matchedAcc = FarmBuildingManager.Instance.SpawnAnimalForBuilding(b, deductGold: false, awardXP: false);
                        }

                        if (matchedAcc != null && matchedAcc)
                        {
                            restoredPerBuilding[targetBuildingId] = indexInPen;
                            matchedAcc.animalName = normalizedName;
                            matchedAcc.hunger = asave.hunger;
                            matchedAcc.happiness = asave.happiness;
                            matchedAcc.affection = (asave.affection > 0f) ? asave.affection : 50f;
                            matchedAcc.productTimer = asave.productTimer;
                            matchedAcc.isProductReady = asave.isProductReady;
                            AnimalScaleConfig.ApplyScale(matchedAcc.gameObject, matchedAcc.animalType);
                            GroundSnapUtility.SnapToGroundExact(matchedAcc.gameObject);

                            CowController cc = matchedAcc.GetComponent<CowController>();
                            if (cc != null)
                            {
                                if (cc.cow == null)
                                {
                                    cc.cow = new Cow(normalizedName, 4,
                                        string.IsNullOrEmpty(matchedAcc.breed) ? "Holstein Friesian" : matchedAcc.breed,
                                        Mathf.RoundToInt(asave.happiness / 10f), 60, false, true, 250, "Farm");
                                }
                                else
                                {
                                    cc.cow.name = normalizedName;
                                    cc.cow.currScene = "Farm";
                                }
                            }

                            if (b != null)
                            {
                                Transform parentRoot = b.AnimalsRoot != null ? b.AnimalsRoot : b.transform;
                                if (matchedAcc.transform.parent != parentRoot)
                                {
                                    matchedAcc.transform.SetParent(parentRoot, true);
                                }

                                AnimalPenBoundary pen = b.GetComponent<AnimalPenBoundary>() ?? b.GetComponentInChildren<AnimalPenBoundary>(true);
                                if (pen != null)
                                {
                                    pen.RegisterAnimal(matchedAcc.gameObject);
                                    if (asave.hasCustomPosition && (asave.posX != 0f || asave.posZ != 0f))
                                    {
                                        Vector3 savedWorldPos = new Vector3(asave.posX, asave.posY, asave.posZ);
                                        if (pen.IsInside(savedWorldPos, 0.3f))
                                        {
                                            matchedAcc.transform.position = pen.ClampPosition(savedWorldPos, 0.55f);
                                            matchedAcc.transform.rotation = Quaternion.Euler(0f, asave.rotY, 0f);
                                        }
                                        else
                                        {
                                            matchedAcc.transform.position = pen.GetRandomPositionInside(0.65f);
                                        }
                                    }
                                    else if (!pen.IsInside(matchedAcc.transform.position, 0.3f))
                                    {
                                        matchedAcc.transform.position = pen.GetRandomPositionInside(0.65f);
                                    }
                                    else
                                    {
                                        matchedAcc.transform.position = pen.ClampPosition(matchedAcc.transform.position, 0.55f);
                                    }
                                }
                            }

                            animalCount++;
                        }
                    }
                }

                // Destroy any excess / duplicate / unmatched scene animals that were not part of the save
                for (int i = 0; i < unmatchedSceneAnimals.Count; i++)
                {
                    if (unmatchedSceneAnimals[i] != null && unmatchedSceneAnimals[i].gameObject != null)
                    {
                        Debug.Log($"[SaveLoadSystem] Removing excess/unmatched scene animal: {unmatchedSceneAnimals[i].gameObject.name}");
                        SafeDestroy(unmatchedSceneAnimals[i].gameObject);
                    }
                }

                if (AnimalManager.EnsureInstance() != null)
                {
                    AnimalManager.Instance.SyncExistingAnimalsList();
                }
                Debug.Log($"[SaveLoad] Restored animals: {animalCount}");

                // 5. Restore Moved Objects, Fences, Custom Decor & Map Objects (Requirement C & Map Objects)
                MovableFarmObject.ScanAndRegisterAllMapObjects();

                Transform[] allSceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
                Dictionary<string, Transform> transformLookup = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in allSceneTransforms)
                {
                    if (t == null) continue;
                    MovableFarmObject mfo = t.GetComponent<MovableFarmObject>();
                    if (mfo != null && !string.IsNullOrEmpty(mfo.objectId) && !transformLookup.ContainsKey(mfo.objectId))
                    {
                        transformLookup[mfo.objectId] = t;
                    }
                    if (t.parent != null)
                    {
                        string pPath = t.parent.name + "/" + t.name;
                        if (!transformLookup.ContainsKey(pPath)) transformLookup[pPath] = t;
                    }
                    if (!transformLookup.ContainsKey(t.name))
                    {
                        transformLookup[t.name] = t;
                    }
                }

                // Process deleted objects first
                if (data.deletedObjectIds != null && data.deletedObjectIds.Count > 0)
                {
                    foreach (var delId in data.deletedObjectIds)
                    {
                        if (string.IsNullOrEmpty(delId)) continue;
                        Transform targetT = null;
                        if (!transformLookup.TryGetValue(delId, out targetT) || targetT == null)
                        {
                            GameObject foundGo = GameObject.Find(delId);
                            if (foundGo != null) targetT = foundGo.transform;
                            else if (delId.Contains("/"))
                            {
                                string[] parts = delId.Split('/');
                                GameObject pGo = GameObject.Find(parts[0]);
                                if (pGo != null)
                                {
                                    Transform ch = pGo.transform.Find(parts[1]);
                                    if (ch != null) targetT = ch;
                                }
                            }
                        }
                        if (targetT != null)
                        {
                            ProductionMachine pm = targetT.GetComponent<ProductionMachine>();
                            if (pm != null && pm.isPurchased)
                            {
                                continue;
                            }
                            MovableFarmObject m = targetT.GetComponent<MovableFarmObject>() ?? targetT.gameObject.AddComponent<MovableFarmObject>();
                            m.isDeleted = true;
                            targetT.gameObject.SetActive(false);
                        }
                    }
                }

                if (data.movedObjects != null && data.movedObjects.Count > 0)
                {
                    int restoredMovedCount = 0;
                    foreach (var mo in data.movedObjects)
                    {
                        if (string.IsNullOrEmpty(mo.objectId)) continue;
                        Transform targetT = null;

                        if (!transformLookup.TryGetValue(mo.objectId, out targetT) || targetT == null)
                        {
                            GameObject foundGo = GameObject.Find(mo.objectId);
                            if (foundGo != null) targetT = foundGo.transform;
                            else if (mo.objectId.Contains("/"))
                            {
                                string[] parts = mo.objectId.Split('/');
                                GameObject pGo = GameObject.Find(parts[0]);
                                if (pGo != null)
                                {
                                    Transform ch = pGo.transform.Find(parts[1]);
                                    if (ch != null) targetT = ch;
                                }
                            }
                        }

                        if (targetT != null)
                        {
                            MovableFarmObject movable = targetT.GetComponent<MovableFarmObject>();
                            if (movable == null) movable = targetT.gameObject.AddComponent<MovableFarmObject>();
                            movable.objectId = mo.objectId;
                            movable.AutoDetectProperties();

                            ProductionMachine pm = targetT.GetComponent<ProductionMachine>();
                            if (mo.isDeleted && (pm == null || !pm.isPurchased))
                            {
                                movable.isDeleted = true;
                                targetT.gameObject.SetActive(false);
                                continue;
                            }

                            targetT.gameObject.SetActive(true);
                            movable.isDeleted = false;

                            Vector3 targetPos = new Vector3(mo.posX, mo.posY, mo.posZ);
                            if (Terrain.activeTerrain != null && targetPos.y == 0f)
                            {
                                targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.transform.position.y;
                            }
                            targetT.position = targetPos;
                            targetT.rotation = Quaternion.Euler(0f, mo.rotY, 0f);

                            if (mo.scaleX > 0.001f && mo.scaleY > 0.001f && mo.scaleZ > 0.001f)
                            {
                                targetT.localScale = new Vector3(mo.scaleX, mo.scaleY, mo.scaleZ);
                            }

                            // Ground-snap tractor, vehicles, pens, fences, straw, lumber, ladder, and all movables
                            string tName = targetT.name.ToLower();
                            if (tName.Contains("tractor") || tName.Contains("vehicle"))
                            {
                                GroundSnapUtility.SnapTractorToGroundExact(targetT.gameObject);
                            }
                            else
                            {
                                GroundSnapUtility.SnapObjectToGround(targetT.gameObject);
                            }

                            movable.EnsureCollider();
                            movable.RecordCurrentTransform();

                            // If animal pen, notify boundary
                            AnimalPenBoundary pen = targetT.GetComponentInChildren<AnimalPenBoundary>(true);
                            if (pen != null)
                            {
                                pen.NotifyPenMoved(Vector3.zero, Quaternion.identity);
                            }

                            restoredMovedCount++;
                        }
                    }
                    Debug.Log($"[SaveLoad] Restored {restoredMovedCount} moved objects / decor positions from save.");
                }

                // Enforce strictly one valid tractor, remove duplicates, and ground flush (Requirements B & C)
                FarmSceneInitializer.EnforceSingleValidTractor();

                // Re-align all fences, pens, and stone walls to terrain after loading game
                FenceGroundAligner.AlignAllInScene();

                // 6. Restore Weather & Day & Season
                if (WeatherSystem.Instance != null)
                {
                    WeatherSystem.Instance.LoadData(data.currentDay, data.currentWeather, data.currentSeason, data.isManualWeather, data.manualWeather);
                }

                // 7. Restore Daily Quests
                if (QuestSystem.Instance != null)
                {
                    QuestSystem.Instance.LoadData(data.dailyQuests, data.currentDay, data.totalCompletedQuestsCount);
                }

                // 8. Restore Achievements
                if (AchievementSystem.Instance != null)
                {
                    AchievementSystem.Instance.LoadData(data.achievements, data.totalCropsHarvested, data.totalDecorationsMovedOrPlaced);
                }

                Debug.Log($"[SaveLoadSystem] Game loaded successfully! (Saved at: {data.saveTimestamp})");
                OnGameLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to load game: {ex.Message}");
            }
        }

        public static SaveLoadSystem EnsureInstance()
        {
            if (Instance != null) return Instance;

            SaveLoadSystem found = FindAnyObjectByType<SaveLoadSystem>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("SaveLoadSystem");
            Instance = go.AddComponent<SaveLoadSystem>();
            return Instance;
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (obj is GameObject go)
            {
                if (go.CompareTag("MainCamera") || go.GetComponent<Camera>() != null || go.CompareTag("Player") || go.name == "Main Camera" || go.name == "Player" || go.name == "Game Objects" || go.name == "Farm" || go.name == "Environment")
                {
                    Debug.LogWarning($"[SaveLoadSystem.SafeDestroy] Refusing to destroy critical object: {go.name}");
                    return;
                }
            }
            if (obj is Camera || obj is CameraController)
            {
                Debug.LogWarning($"[SaveLoadSystem.SafeDestroy] Refusing to destroy camera component: {obj.name}");
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(obj);
#else
                DestroyImmediate(obj);
#endif
            }
        }
    }
}
