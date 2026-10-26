using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Manages physical farm buildings and animal assignments.
    /// Handles purchasing, permanent scene spawning, persistence, and safe reference lifecycle.
    /// </summary>
    public class FarmBuildingManager : MonoBehaviour
    {
        public static FarmBuildingManager Instance { get; private set; }

        public Dictionary<string, FarmBuilding> buildings = new Dictionary<string, FarmBuilding>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> purchasedBuildingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static FarmBuildingManager EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.PruneAndScanSceneBuildings();
                return Instance;
            }

            FarmBuildingManager found = FindAnyObjectByType<FarmBuildingManager>();
            if (found != null)
            {
                Instance = found;
                Instance.PruneAndScanSceneBuildings();
                return found;
            }

            GameObject go = new GameObject("FarmBuildingManager");
            Instance = go.AddComponent<FarmBuildingManager>();
            Instance.PruneAndScanSceneBuildings();
            return Instance;
        }

        private static bool oldBuildingsDisabled = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }

            if (!oldBuildingsDisabled) DisableOldGiantBuildingsAndFences();
            PruneAndScanSceneBuildings();
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        void Start()
        {
            if (!oldBuildingsDisabled) DisableOldGiantBuildingsAndFences();
            PruneAndScanSceneBuildings();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!oldBuildingsDisabled) DisableOldGiantBuildingsAndFences();
            PruneAndScanSceneBuildings();
        }

        /// <summary>
        /// Deactivates the old giant barn and 21 giant fences from Farm.prefab so only the new compact pens exist.
        /// Guaranteed to run only once per scene session.
        /// </summary>
        public static void DisableOldGiantBuildingsAndFences()
        {
            if (oldBuildingsDisabled) return;
            oldBuildingsDisabled = true;

            try
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (!activeScene.isLoaded) return;

                GameObject[] roots = activeScene.GetRootGameObjects();
                foreach (var r in roots)
                {
                    DisableOldFencesRecursively(r.transform);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FarmBuildingManager] DisableOldGiantBuildingsAndFences notice: {ex.Message}");
            }
        }

        private static void DisableOldFencesRecursively(Transform t)
        {
            if (t == null) return;
            string n = t.name;

            // Only deactivate machine_shed if found
            if (n.Equals("machine_shed", StringComparison.OrdinalIgnoreCase) ||
                (n.ToLower().Contains("machine") && n.ToLower().Contains("shed") && !n.ToLower().Contains("cow") && !n.ToLower().Contains("chicken") && !n.ToLower().Contains("pig") && !n.ToLower().Contains("horse")))
            {
                t.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                DisableOldFencesRecursively(t.GetChild(i));
            }
        }

        /// <summary>
        /// Cleans up any destroyed Unity object references and re-registers any physical buildings found in the scene.
        /// </summary>
        public void PruneAndScanSceneBuildings()
        {
            // 1. Remove truly destroyed references (do not prune just because object is inactive or initializing)
            List<string> staleKeys = new List<string>();
            foreach (var kvp in buildings)
            {
                if (kvp.Value == null || !kvp.Value || kvp.Value.gameObject == null)
                {
                    staleKeys.Add(kvp.Key);
                }
            }
            foreach (var key in staleKeys)
            {
                buildings.Remove(key);
            }

            // 2. Discover all FarmBuilding instances in current scene (including inactive)
            FarmBuilding[] sceneBuildings = FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fb in sceneBuildings)
            {
                if (fb != null && (fb.buildingId == "machine_shed" || fb.buildingType == "machine_shed" ||
                    fb.name.ToLower().Contains("machine_shed") || fb.name.ToLower().Contains("machineshed")))
                {
                    Debug.Log($"[FarmBuildingManager] Removing deleted machine_shed building: {fb.gameObject.name}");
                    SafeDestroy(fb.gameObject);
                    continue;
                }

                if (fb != null && fb.gameObject != null && !string.IsNullOrEmpty(fb.buildingId))
                {
                    buildings[fb.buildingId] = fb;
                    if (fb.isPurchased)
                    {
                        purchasedBuildingIds.Add(fb.buildingId);
                    }
                }
            }
            purchasedBuildingIds.Remove("machine_shed");

            // 3. Re-link existing cows to Cow Barn if Cow Barn is active
            if (buildings.TryGetValue("cow_barn", out FarmBuilding cowBarn) && FarmBuilding.IsValidBuilding(cowBarn) && cowBarn.isPurchased)
            {
                CowController[] cows = FindObjectsByType<CowController>();
                foreach (var cow in cows)
                {
                    if (cow == null) continue;
                    AnimalCareController acc = cow.GetComponent<AnimalCareController>();
                    if (acc != null && acc.animalType == AnimalType.Cow)
                    {
                        cowBarn.AddAnimal(acc);
                    }
                }
            }

            // 4. Re-link existing chickens to Chicken Coop if Chicken Coop is active
            if (buildings.TryGetValue("chicken_coop", out FarmBuilding chickenCoop) && FarmBuilding.IsValidBuilding(chickenCoop) && chickenCoop.isPurchased)
            {
                ChickenWanderer[] chickens = FindObjectsByType<ChickenWanderer>();
                foreach (var chk in chickens)
                {
                    if (chk == null) continue;
                    AnimalCareController acc = chk.GetComponent<AnimalCareController>();
                    if (acc != null && acc.animalType == AnimalType.Chicken)
                    {
                        chickenCoop.AddAnimal(acc);
                    }
                }
            }

            // 5. Re-link existing pigs to Pig Pen if Pig Pen is active
            if (buildings.TryGetValue("pig_pen", out FarmBuilding pigPen) && FarmBuilding.IsValidBuilding(pigPen) && pigPen.isPurchased)
            {
                AnimalCareController[] allAnimals = FindObjectsByType<AnimalCareController>();
                foreach (var acc in allAnimals)
                {
                    if (acc != null && acc.animalType == AnimalType.Pig)
                    {
                        pigPen.AddAnimal(acc);
                    }
                }
            }

            // 6. Re-link existing horses to Horse Stable if Horse Stable is active
            if (buildings.TryGetValue("horse_stable", out FarmBuilding horseStable) && FarmBuilding.IsValidBuilding(horseStable) && horseStable.isPurchased)
            {
                AnimalCareController[] allAnimals = FindObjectsByType<AnimalCareController>();
                foreach (var acc in allAnimals)
                {
                    if (acc != null && acc.animalType == AnimalType.Horse)
                    {
                        horseStable.AddAnimal(acc);
                    }
                }
            }
        }

        public FarmBuilding GetBuilding(string buildingId)
        {
            PruneAndScanSceneBuildings();
            if (buildings.TryGetValue(buildingId, out FarmBuilding fb) && FarmBuilding.IsValidBuilding(fb))
            {
                return fb;
            }
            return null;
        }

        public bool IsBuildingPurchased(string buildingId)
        {
            PruneAndScanSceneBuildings();
            if (buildings.TryGetValue(buildingId, out FarmBuilding fb) && FarmBuilding.IsValidBuilding(fb))
            {
                return fb.isPurchased;
            }
            return purchasedBuildingIds.Contains(buildingId);
        }

        public bool HasCapacityForAnimal(string requiredBarnType)
        {
            FarmBuilding barn = GetBuilding(requiredBarnType);
            if (barn != null && barn.isPurchased)
            {
                return barn.CanAddAnimal();
            }
            return false;
        }

        public static GameObject LoadBuildingPrefab(string buildingId, out string resolvedPath)
        {
            resolvedPath = "";
            switch (buildingId.ToLower())
            {
                case "cow_barn":
                    resolvedPath = "Assets/Prefabs/Farm/CowPen_Small.prefab";
                    break;
                case "chicken_coop":
                    resolvedPath = "Assets/Prefabs/Farm/ChickenCoop_Small.prefab";
                    break;
                case "pig_pen":
                    resolvedPath = "Assets/Prefabs/Farm/PigPen_Small.prefab";
                    break;
                case "horse_stable":
                    resolvedPath = "Assets/Prefabs/Farm/HorseStable_Small.prefab";
                    break;
                case "storage":
                    resolvedPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House1.prefab";
                    break;
            }

            if (buildingId.ToLower() == "machine_shed") return null;

            GameObject loaded = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                if (loaded == null && buildingId.ToLower() == "cow_barn")
                {
                    resolvedPath = "Assets/Prefabs/Farm/CowPenPrefab.prefab";
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    if (loaded == null)
                    {
                        resolvedPath = "Assets/Prefabs/Farm/CowPen_Large.prefab";
                        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    }
                }
                else if (loaded == null && buildingId.ToLower() == "chicken_coop")
                {
                    resolvedPath = "Assets/Prefabs/Farm/ChickenCoopPrefab.prefab";
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    if (loaded == null)
                    {
                        resolvedPath = "Assets/Prefabs/Farm/ChickenCoop_Large.prefab";
                        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    }
                }
                else if (loaded == null && buildingId.ToLower() == "pig_pen")
                {
                    resolvedPath = "Assets/Prefabs/Farm/PigPen_Small.prefab";
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    if (loaded == null)
                    {
                        resolvedPath = "Assets/Prefabs/Farm/PigPen_Large.prefab";
                        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                    }
                }
                else if (loaded == null && buildingId.ToLower() == "horse_stable")
                {
                    resolvedPath = "Assets/Prefabs/Farm/HorseStablePrefab.prefab";
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                }
                else if (loaded == null && buildingId.ToLower() == "storage")
                {
                    resolvedPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House1.prefab";
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                }
            }
#endif
            if (loaded == null)
            {
                if (buildingId.ToLower() == "cow_barn")
                {
                    resolvedPath = "Resources/CowPen_Small";
                    loaded = Resources.Load<GameObject>("CowPen_Small") ?? Resources.Load<GameObject>("CowPenPrefab") ?? Resources.Load<GameObject>("CowPen_Large");
                }
                else if (buildingId.ToLower() == "chicken_coop")
                {
                    resolvedPath = "Resources/ChickenCoop_Small";
                    loaded = Resources.Load<GameObject>("ChickenCoop_Small") ?? Resources.Load<GameObject>("ChickenCoopPrefab") ?? Resources.Load<GameObject>("ChickenCoop_Large");
                }
                else if (buildingId.ToLower() == "pig_pen")
                {
                    resolvedPath = "Resources/PigPenPrefab";
                    loaded = Resources.Load<GameObject>("PigPenPrefab") ?? Resources.Load<GameObject>("PigPen_Small") ?? Resources.Load<GameObject>("PigPen_Large");
                }
                else if (buildingId.ToLower() == "horse_stable")
                {
                    resolvedPath = "Resources/HorseStable_Small";
                    loaded = Resources.Load<GameObject>("HorseStable_Small") ?? Resources.Load<GameObject>("HorseStablePrefab");
                }
                else if (buildingId.ToLower() == "storage")
                {
                    resolvedPath = "Resources/House1";
                    loaded = Resources.Load<GameObject>("House1") ?? Resources.Load<GameObject>("Storage");
                }
                else
                {
                    resolvedPath = "Resources/" + buildingId;
                    loaded = Resources.Load<GameObject>(buildingId);
                }
            }
            return loaded;
        }

        public static GameObject LoadBuildingPrefab(string buildingId)
        {
            return LoadBuildingPrefab(buildingId, out _);
        }

        public bool BuyBuilding(string buildingId)
        {
            PruneAndScanSceneBuildings();

            if (buildingId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase))
            {
                CropSelectionUI.ShowToast("Công trình Xưởng chế biến đã bị loại bỏ.");
                return false;
            }

            if (!FarmDatabase.Buildings.TryGetValue(buildingId, out BuildingData data))
            {
                Debug.LogError($"[FarmBuildingManager] Unknown building ID in database: {buildingId}");
                return false;
            }

            // 1. Check if already purchased
            if (IsBuildingPurchased(buildingId))
            {
                CropSelectionUI.ShowToast("Công trình này đã được xây dựng rồi!");
                return false;
            }

            // 2. Check Level
            int currentLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;
            if (currentLevel < data.unlockLevel)
            {
                CropSelectionUI.ShowToast($"Yêu cầu Level {data.unlockLevel} mới có thể mở khóa!");
                return false;
            }

            // 3. Check Gold
            if (PlayerProgression.Instance == null || PlayerProgression.Instance.Gold < data.priceGold)
            {
                CropSelectionUI.ShowToast("Not enough Gold!");
                return false;
            }

            // 4. Check Prefab
            GameObject prefab = LoadBuildingPrefab(buildingId);
            if (prefab == null && buildingId != "pig_pen" && buildingId != "horse_stable" && buildingId != "machine_shed")
            {
                Debug.LogError($"[FarmBuildingManager] Building prefab missing for '{buildingId}'!");
                CropSelectionUI.ShowToast("Building prefab missing!");
                return false;
            }

            // 5. Deduct Gold
            if (!PlayerProgression.TrySpendGold(data.priceGold))
            {
                CropSelectionUI.ShowToast("Not enough Gold!");
                return false;
            }

            // 6. Spawn physical building GameObject in Farm
            Vector3 spawnPos = data.defaultPosition;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
            }

            FarmBuilding fb = SpawnPurchasedBuildingAtPosition(buildingId, spawnPos);
            if (fb != null)
            {
                PlayerProgression.AddXP(25, $"Xây dựng {data.displayName}");
                CropSelectionUI.ShowToast($"Đã xây dựng thành công: {data.displayName}!\n+25 XP");
                SaveLoadSystem.SaveGame();
                return true;
            }
            return false;
        }

        public FarmBuilding SpawnPurchasedBuildingAtPosition(string buildingId, Vector3 spawnPos, Quaternion spawnRot = default, Vector3 spawnScale = default, bool isFromSave = false)
        {
            PruneAndScanSceneBuildings();

            if (!FarmDatabase.Buildings.TryGetValue(buildingId, out BuildingData data))
            {
                Debug.LogError($"[FarmBuildingManager] Unknown building ID in database: {buildingId}");
                return null;
            }

            // Prevent duplicate creation of the same building
            if (buildings.TryGetValue(buildingId, out FarmBuilding existingFb) && existingFb != null && existingFb.gameObject != null)
            {
                Debug.LogWarning($"[FarmBuildingManager] Building '{buildingId}' already exists in scene. Using existing.");
                return existingFb;
            }

            if (buildingId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string clearName = (buildingId == "cow_barn") ? "CowPen_Small" :
                               ((buildingId == "chicken_coop") ? "ChickenCoop_Small" :
                               ("Purchased_" + (buildingId == "pig_pen" ? "PigPen" :
                               (buildingId == "horse_stable" ? "HorseStable" : "Storage"))));

            // Only raycast to terrain if spawnPos is 0 / uninitialized
            if (spawnPos.sqrMagnitude < 0.01f)
            {
                Ray groundRay = new Ray(spawnPos + Vector3.up * 50f, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit hit, 200f))
                {
                    spawnPos.y = hit.point.y;
                }
                else
                {
                    Terrain terrain = Terrain.activeTerrain;
                    if (terrain != null)
                    {
                        spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                    }
                }
            }

            if (spawnRot == default) spawnRot = Quaternion.identity;
            if (spawnScale == default || spawnScale == Vector3.zero) spawnScale = Vector3.one;

            GameObject prefab = LoadBuildingPrefab(buildingId, out string resolvedPrefabPath);
            GameObject buildingGo = null;
            if (prefab != null)
            {
                try
                {
                    buildingGo = Instantiate(prefab, spawnPos, spawnRot);
                    Debug.Log($"[FarmBuildingManager] Spawned prefab: {resolvedPrefabPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FarmBuildingManager] Exception instantiating prefab for {buildingId}: {ex.Message}");
                    buildingGo = null;
                }
            }

            if (buildingGo == null)
            {
                buildingGo = new GameObject(clearName);
                buildingGo.transform.position = spawnPos;
                buildingGo.transform.rotation = spawnRot;
                FarmBuilding.CreateProceduralBuilding(buildingGo, buildingId, data.displayName);
                Debug.Log($"[FarmBuildingManager] Spawned procedural fallback for: {buildingId}");
            }

            buildingGo.name = clearName;
            buildingGo.transform.position = spawnPos;
            buildingGo.transform.rotation = spawnRot;
            buildingGo.transform.localScale = spawnScale;

            // Ensure parented under Farm for hierarchy: Farm/CowPen_Small
            GameObject farmRoot = GameObject.Find("Farm");
            if (farmRoot == null)
            {
                farmRoot = new GameObject("Farm");
            }
            buildingGo.transform.SetParent(farmRoot.transform, true);

            FarmBuilding fb = buildingGo.GetComponent<FarmBuilding>();
            if (fb == null)
            {
                fb = buildingGo.AddComponent<FarmBuilding>();
            }
            fb.SetupBuilding(data, true);

            // Re-align pen posts, rails, colliders and floor flush with terrain
            GroundSnapUtility.SnapToGroundExact(buildingGo);
            FenceGroundAligner.AlignObject(buildingGo);

            buildings[buildingId] = fb;
            purchasedBuildingIds.Add(buildingId);

            if (buildingId == "storage" && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.UpgradeCapacity(50, 0);
            }

            Debug.Log($"[FarmBuildingManager] Successfully spawned {clearName} at {buildingGo.transform.position}.");
            return fb;
        }

        public FarmBuilding SpawnPurchasedBuildingAtPosition(string buildingId, Vector3 spawnPos)
        {
            return SpawnPurchasedBuildingAtPosition(buildingId, spawnPos, Quaternion.identity, Vector3.one, false);
        }

        public bool BuyAndSpawnAnimalForBuilding(FarmBuilding barn)
        {
            if (!FarmBuilding.IsValidBuilding(barn))
            {
                CropSelectionUI.ShowToast("Chuồng trại không hợp lệ!");
                return false;
            }

            if (!barn.isPurchased)
            {
                CropSelectionUI.ShowToast("Bạn cần mua chuồng này trước!");
                return false;
            }

            if (!barn.CanAddAnimal())
            {
                CropSelectionUI.ShowToast($"Chuồng đã đầy: {barn.GetOccupantCount()}/{barn.capacity}");
                return false;
            }

            AnimalCareController acc = SpawnAnimalForBuilding(barn, deductGold: true, awardXP: true);
            return acc != null;
        }

        public AnimalCareController SpawnAnimalForBuilding(FarmBuilding barn, bool deductGold = false, bool awardXP = false)
        {
            if (!FarmBuilding.IsValidBuilding(barn)) return null;
            if (!barn.CanAddAnimal())
            {
                Debug.LogWarning($"[FarmBuildingManager] Cannot spawn animal: '{barn.name}' is at capacity ({barn.GetOccupantCount()}/{barn.capacity}).");
                return null;
            }

            string animalId = "cow";
            if (barn.allowedAnimalType == AnimalType.Pig || barn.buildingTypeEnum == BuildingType.PigPen || barn.buildingType == "pig_pen" || barn.buildingId == "pig_pen" || barn.name.ToLower().Contains("pig"))
            {
                animalId = "pig";
            }
            else if (barn.allowedAnimalType == AnimalType.Chicken || barn.buildingTypeEnum == BuildingType.ChickenCoop || barn.buildingType == "chicken_coop" || barn.buildingId == "chicken_coop" || barn.name.ToLower().Contains("chicken"))
            {
                animalId = "chicken";
            }
            else if (barn.allowedAnimalType == AnimalType.Horse || barn.buildingTypeEnum == BuildingType.HorseStable || barn.buildingType == "horse_stable" || barn.buildingId == "horse_stable" || barn.name.ToLower().Contains("horse"))
            {
                animalId = "horse";
            }
            else
            {
                animalId = "cow";
            }

            if (!FarmDatabase.Animals.TryGetValue(animalId, out AnimalData aData))
            {
                Debug.LogError($"[FarmBuildingManager] Animal data not found for {animalId}");
                return null;
            }

            if (deductGold)
            {
                int currentLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;
                if (currentLevel < aData.unlockLevel)
                {
                    CropSelectionUI.ShowToast($"Yêu cầu Level {aData.unlockLevel} mới có thể mua {aData.displayName}!");
                    return null;
                }

                if (PlayerProgression.Instance == null || PlayerProgression.Instance.Gold < aData.priceGold)
                {
                    CropSelectionUI.ShowToast("Not enough Gold!");
                    return null;
                }
            }

            // Spawn animal inside pen bounds for full visibility and strict containment
            AnimalPenBoundary boundary = barn.GetComponent<AnimalPenBoundary>() ?? barn.GetComponentInChildren<AnimalPenBoundary>();
            Vector3 spawnPos = Vector3.zero;
            if (boundary != null)
            {
                spawnPos = boundary.GetRandomPositionInside(0.65f);
            }
            if (spawnPos == Vector3.zero || float.IsNaN(spawnPos.x))
            {
                Vector3 barnCenter = barn.transform.position;
                Vector3 offset = barn.transform.right * UnityEngine.Random.Range(-1.5f, 1.5f) +
                                 barn.transform.forward * UnityEngine.Random.Range(-1.5f, 1.5f);
                spawnPos = barnCenter + offset;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
            }

            GameObject animalGo = null;
            string selectedCowBreed = "Holstein Friesian";
            if (animalId == "cow")
            {
                string[] cowBreeds = new string[] { "Holstein Friesian", "Angus", "Hereford", "Brangus", "Charolais", "Shorthorn" };
                int breedIdx = barn.GetOccupantCount() % cowBreeds.Length;
                selectedCowBreed = cowBreeds[breedIdx];
                GameObject cowPrefab = Resources.Load<GameObject>(selectedCowBreed) ?? Resources.Load<GameObject>("Angus");
#if UNITY_EDITOR
                if (cowPrefab == null)
                {
                    cowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/{selectedCowBreed}.prefab") ??
                                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Angus.prefab") ??
                                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Animals/Cow.prefab");
                }
#endif
                if (cowPrefab != null)
                {
                    animalGo = Instantiate(cowPrefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                    FarmVisualFactory.EnhanceCowVisuals(animalGo);
                }
            }
            else
            {
                string prefabCapName = char.ToUpperInvariant(animalId[0]) + animalId.Substring(1).ToLowerInvariant();
                GameObject dedicatedPrefab = Resources.Load<GameObject>($"Animals/{prefabCapName}");
#if UNITY_EDITOR
                if (dedicatedPrefab == null)
                {
                    dedicatedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Animals/{prefabCapName}.prefab");
                }
#endif
                if (dedicatedPrefab != null)
                {
                    animalGo = Instantiate(dedicatedPrefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                }
            }

            if (animalGo == null)
            {
                animalGo = new GameObject();
                animalGo.transform.position = spawnPos;
                animalGo.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                CreateAnimalMesh(animalGo, animalId);
            }

            Renderer animalRenderer = animalGo.GetComponentInChildren<Renderer>();
            if (animalRenderer == null)
            {
                Debug.LogError($"[AnimalPurchase] Spawn failed: {animalId} has no active Renderer!");
                CropSelectionUI.ShowToast($"Lỗi: Không tìm thấy Renderer cho {aData.displayName}!");
                SafeDestroy(animalGo);
                return null;
            }
            animalRenderer.enabled = true;

            if (deductGold)
            {
                if (!PlayerProgression.TrySpendGold(aData.priceGold))
                {
                    CropSelectionUI.ShowToast("Not enough Gold!");
                    SafeDestroy(animalGo);
                    return null;
                }
            }

            int nextIdx = barn.GetOccupantCount() + 1;
            animalGo.name = $"{aData.displayName}_{nextIdx}";
            animalGo.transform.SetParent(barn.AnimalsRoot, true);
            AnimalScaleConfig.ApplyScale(animalGo, animalId);
            GroundSnapUtility.SnapToGroundExact(animalGo);

            // Add Collider if missing
            Collider col = animalGo.GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = animalGo.AddComponent<SphereCollider>();
                sc.radius = (animalId == "chicken") ? 0.6f : (animalId == "pig" ? 0.9f : (animalId == "horse" ? 1.35f : 1.2f));
                sc.center = new Vector3(0f, (animalId == "chicken") ? 0.6f : (animalId == "pig" ? 0.6f : (animalId == "horse" ? 1.15f : 0.8f)), 0f);
            }

            // Confine animal behavior strictly within pen boundary
            if (animalId == "chicken")
            {
                ChickenWanderer cw = animalGo.GetComponent<ChickenWanderer>() ?? animalGo.AddComponent<ChickenWanderer>();
                cw.penBoundary = boundary;
            }
            else if (animalId == "pig")
            {
                PigWanderer pw = animalGo.GetComponent<PigWanderer>() ?? animalGo.AddComponent<PigWanderer>();
                pw.penBoundary = boundary;
            }
            else if (animalId == "horse")
            {
                HorseWanderer hw = animalGo.GetComponent<HorseWanderer>() ?? animalGo.AddComponent<HorseWanderer>();
                hw.penBoundary = boundary;
            }
            else if (animalId == "cow")
            {
                CowController cc = animalGo.GetComponent<CowController>();
                if (cc != null)
                {
                    cc.penBoundary = boundary;
                    if (cc.cow == null || string.IsNullOrEmpty(cc.cow.breed))
                    {
                        cc.cow = new Cow($"{aData.displayName} #{nextIdx}", 4, selectedCowBreed, 8, 60, true, true, 250, "Farm");
                    }
                    else
                    {
                        cc.cow.name = $"{aData.displayName} #{nextIdx}";
                        cc.cow.currScene = "Farm";
                    }
                    cc.cow.cowController = cc;
                }
            }

            if (boundary != null)
            {
                boundary.RegisterAnimal(animalGo);
            }

            // Configure AnimalCareController
            AnimalCareController acc = animalGo.GetComponent<AnimalCareController>();
            if (acc == null) acc = animalGo.AddComponent<AnimalCareController>();

            acc.animalType = (animalId == "chicken") ? AnimalType.Chicken :
                             (animalId == "pig" ? AnimalType.Pig :
                             (animalId == "horse" ? AnimalType.Horse : AnimalType.Cow));
            acc.animalName = $"{aData.displayName} #{nextIdx}";
            if (animalId == "cow") acc.breed = selectedCowBreed;
            acc.productItemId = aData.productItemId;
            acc.productName = aData.productName;
            acc.productAmount = aData.productAmount;
            acc.productionDuration = aData.productionDuration;
            acc.hunger = 80f;
            acc.happiness = 75f;
            acc.feedXP = aData.careXP;
            acc.collectXP = aData.harvestXP;

            barn.AddAnimal(acc);

            if (AnimalManager.Instance != null && !AnimalManager.Instance.animals.Contains(acc))
            {
                AnimalManager.Instance.animals.Add(acc);
            }

            if (awardXP)
            {
                PlayerProgression.AddXP(aData.careXP, $"Mua {aData.displayName}");
                CropSelectionUI.ShowToast($"Đã mua thành công 1 {aData.displayName}!\n+{aData.careXP} XP");
            }

            Debug.Log($"[AnimalPurchase] {acc.animalName} spawned in {barn.name}");

            barn.RefreshVisuals();
            if (BuildingUI.Instance != null) BuildingUI.Instance.RefreshDisplay();
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();

            if (deductGold) SaveLoadSystem.SaveGame();
            return acc;
        }

        public bool SpawnAnimalInBarn(string animalId)
        {
            if (!FarmDatabase.Animals.TryGetValue(animalId, out AnimalData aData))
            {
                Debug.LogError($"[FarmBuildingManager] Unknown animal: {animalId}");
                return false;
            }

            FarmBuilding barn = GetBuilding(aData.requiredBarnType);
            if (barn == null || !barn.isPurchased)
            {
                if (animalId == "cow" || aData.requiredBarnType == "cow_barn")
                    CropSelectionUI.ShowToast("Bạn cần xây Chuồng bò trước");
                else if (animalId == "chicken" || aData.requiredBarnType == "chicken_coop")
                    CropSelectionUI.ShowToast("Bạn cần xây Chuồng gà trước");
                else
                    CropSelectionUI.ShowToast($"Bạn cần xây {FarmDatabase.Buildings[aData.requiredBarnType].displayName} trước");
                return false;
            }

            return BuyAndSpawnAnimalForBuilding(barn);
        }

        private void CreateAnimalMesh(GameObject go, string animalId)
        {
            if (animalId == "chicken")
            {
                FarmVisualFactory.BuildChickenVisuals(go);
                AnimalScaleConfig.ApplyScale(go, animalId);
            }
            else if (animalId == "pig")
            {
                FarmVisualFactory.BuildPigVisuals(go);
                AnimalScaleConfig.ApplyScale(go, animalId);
            }
            else if (animalId == "horse")
            {
                FarmVisualFactory.BuildHorseVisuals(go);
                AnimalScaleConfig.ApplyScale(go, animalId);
            }
            else
            {
                FarmVisualFactory.EnhanceCowVisuals(go);
                AnimalScaleConfig.ApplyScale(go, animalId);
            }
        }

        public List<string> GetPurchasedBuildingIds()
        {
            PruneAndScanSceneBuildings();
            return new List<string>(purchasedBuildingIds);
        }

        public void RestorePurchasedBuildings(List<string> purchasedList)
        {
            RestorePurchasedBuildingsWithData(purchasedList, null);
        }

        public void RestorePurchasedBuildingsWithData(List<string> purchasedList, List<BuildingSaveData> buildingsData)
        {
            Dictionary<string, BuildingSaveData> savedMap = new Dictionary<string, BuildingSaveData>(StringComparer.OrdinalIgnoreCase);
            if (buildingsData != null)
            {
                foreach (var b in buildingsData)
                {
                    if (b != null && !string.IsNullOrEmpty(b.buildingId)) savedMap[b.buildingId] = b;
                }
            }

            List<string> allToRestore = new List<string>();
            if (purchasedList != null)
            {
                foreach (var id in purchasedList)
                {
                    if (!string.IsNullOrEmpty(id) && !allToRestore.Exists(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                        allToRestore.Add(id);
                }
            }
            foreach (var kvp in savedMap)
            {
                if (!allToRestore.Exists(x => x.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    allToRestore.Add(kvp.Key);
                }
            }

            int restoredCount = 0;
            foreach (var bId in allToRestore)
            {
                if (bId.Equals("machine_shed", StringComparison.OrdinalIgnoreCase)) continue;
                purchasedBuildingIds.Add(bId);

                Debug.Log($"[FarmBuildingManager] Restoring building: {bId}");

                Vector3 targetPos = Vector3.zero;
                Quaternion targetRot = Quaternion.identity;
                Vector3 targetScale = Vector3.one;
                bool hasTargetPos = false;

                if (savedMap.TryGetValue(bId, out BuildingSaveData bData))
                {
                    targetPos = new Vector3(bData.posX, bData.posY, bData.posZ);
                    targetRot = Quaternion.Euler(0f, bData.rotY, 0f);
                    if (bData.scaleX > 0.01f)
                    {
                        targetScale = new Vector3(bData.scaleX, bData.scaleY, bData.scaleZ);
                    }
                    hasTargetPos = true;
                }
                else if (FarmDatabase.Buildings.TryGetValue(bId, out BuildingData defaultData))
                {
                    targetPos = defaultData.defaultPosition;
                    hasTargetPos = true;
                }

                FarmBuilding targetBuilding = null;
                if (buildings.TryGetValue(bId, out FarmBuilding existingFb) && existingFb != null && existingFb.gameObject != null)
                {
                    targetBuilding = existingFb;
                }
                else
                {
                    try
                    {
                        targetBuilding = SpawnPurchasedBuildingAtPosition(bId, targetPos, targetRot, targetScale, isFromSave: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FarmBuildingManager] Failed to restore: {bId} - {ex.Message}");
                    }
                }

                if (targetBuilding != null)
                {
                    targetBuilding.isPurchased = true;
                    if (!targetBuilding.gameObject.activeSelf) targetBuilding.gameObject.SetActive(true);

                    if (hasTargetPos && targetPos.sqrMagnitude > 0.01f)
                    {
                        targetBuilding.transform.position = targetPos;
                    }
                    targetBuilding.transform.rotation = targetRot;
                    if (targetScale != Vector3.zero) targetBuilding.transform.localScale = targetScale;

                    buildings[bId] = targetBuilding;
                    restoredCount++;
                }
                else
                {
                    Debug.LogError($"[FarmBuildingManager] Failed to restore: {bId} - Spawning returned null");
                }
            }

            int restoredFences = 0;
            foreach (var kvp in buildings)
            {
                if (kvp.Value != null)
                {
                    Renderer[] rArr = kvp.Value.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rArr)
                    {
                        if (r != null && r.gameObject.activeInHierarchy && (r.gameObject.name.ToLower().Contains("fence") || r.gameObject.name.ToLower().Contains("rail") || r.gameObject.name.ToLower().Contains("post")))
                        {
                            restoredFences++;
                        }
                    }
                }
            }
            var allSceneTransforms = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allSceneTransforms)
            {
                if (t != null && t.gameObject.activeInHierarchy && (t.name.StartsWith("Farm Fence") || t.name.StartsWith("Farm_fence")))
                {
                    restoredFences++;
                }
            }

            Debug.Log($"[FarmBuildingManager] Total buildings restored: {restoredCount}");
            Debug.Log($"[FarmBuildingManager] Restored buildings: {restoredCount}");
            Debug.Log($"[FarmBuildingManager] Restored fences: {restoredFences}");
        }

        public void ResetPurchasedBuildings()
        {
            foreach (var kvp in buildings)
            {
                if (FarmBuilding.IsValidBuilding(kvp.Value))
                {
                    SafeDestroy(kvp.Value.gameObject);
                }
            }
            buildings.Clear();
            purchasedBuildingIds.Clear();
            Debug.Log("[FarmBuildingManager] Reset all purchased buildings.");
            CropSelectionUI.ShowToast("[Debug F3] Đã reset toàn bộ chuồng trại đã mua!");
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
            SaveLoadSystem.SaveGame();
        }

        public bool RemoveBuilding(FarmBuilding building)
        {
            if (building == null) return false;
            string foundKey = null;
            foreach (var kvp in buildings)
            {
                if (kvp.Value == building)
                {
                    foundKey = kvp.Key;
                    break;
                }
            }
            if (foundKey != null)
            {
                buildings.Remove(foundKey);
            }
            purchasedBuildingIds.Remove(building.buildingId);
            SafeDestroy(building.gameObject);
            PruneAndScanSceneBuildings();
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
            SaveLoadSystem.SaveGame();
            return true;
        }

        public void VerifyAllBuildingReferences()
        {
            PruneAndScanSceneBuildings();
            int validCount = 0;
            int totalOccupants = 0;

            foreach (var kvp in buildings)
            {
                if (FarmBuilding.IsValidBuilding(kvp.Value))
                {
                    validCount++;
                    totalOccupants += kvp.Value.GetOccupantCount();
                    Debug.Log($"[Verification] Building '{kvp.Key}': Status=VALID, Purchased={kvp.Value.isPurchased}, Occupants={kvp.Value.GetOccupantCount()}/{kvp.Value.capacity}");
                }
                else
                {
                    Debug.LogWarning($"[Verification] Building '{kvp.Key}': Reference is DESTROYED or INVALID!");
                }
            }

            Debug.Log($"[FarmBuildingManager] Verification Complete: {validCount} valid buildings, {totalOccupants} total animals.");
            CropSelectionUI.ShowToast($"[F4 Verify] {validCount} chuồng hợp lệ, {totalOccupants} vật nuôi đang hoạt động!");
        }
    }
}
