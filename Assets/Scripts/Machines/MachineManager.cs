using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Manages all processing machines in the farm scene, permanent placement spawning, purchase verification,
    /// and fast-forward debug.
    /// </summary>
    public class MachineManager : MonoBehaviour
    {
        public static MachineManager Instance { get; private set; }

        public Dictionary<string, ProductionMachine> machines = new Dictionary<string, ProductionMachine>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> purchasedMachineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static MachineManager EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.PruneAndScanSceneMachines();
                return Instance;
            }

            MachineManager found = FindAnyObjectByType<MachineManager>();
            if (found != null)
            {
                Instance = found;
                Instance.PruneAndScanSceneMachines();
                return found;
            }

            GameObject go = new GameObject("MachineManager");
            Instance = go.AddComponent<MachineManager>();
            Instance.PruneAndScanSceneMachines();
            return Instance;
        }

        /// <summary>
        /// Per requirements, processing machines must be purchased by player in Shop before appearing on farm.
        /// No automatic sample/free starter machines are spawned.
        /// </summary>
        public void EnsureStarterMachine()
        {
            // Intentionally no-op: all machines must be purchased in the Shop
            PruneAndScanSceneMachines();
        }

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

            PruneAndScanSceneMachines();
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
            PruneAndScanSceneMachines();
        }

        public static bool IsValidMachine(ProductionMachine pm)
        {
            if (pm == null) return false;
            if (!pm) return false;
            try
            {
                if (pm.gameObject == null) return false;
                if (!pm.gameObject) return false;
                if (!pm.gameObject.activeInHierarchy) return false;
                if (string.IsNullOrEmpty(pm.machineId)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void PruneAndScanSceneMachines()
        {
            // 1. Remove destroyed references
            List<string> staleKeys = new List<string>();
            foreach (var kvp in machines)
            {
                if (!IsValidMachine(kvp.Value))
                {
                    staleKeys.Add(kvp.Key);
                }
            }
            foreach (var key in staleKeys)
            {
                machines.Remove(key);
            }

            // 2. Discover all ProductionMachine instances in current scene
            ProductionMachine[] sceneMachines = FindObjectsByType<ProductionMachine>();
            foreach (var pm in sceneMachines)
            {
                if (IsValidMachine(pm))
                {
                    if (machines.TryGetValue(pm.machineId, out ProductionMachine existing) && existing != pm && IsValidMachine(existing))
                    {
                        // Duplicate detected! Keep the purchased / active one, destroy duplicate
                        if (pm.isPurchased && !existing.isPurchased)
                        {
                            machines[pm.machineId] = pm;
                            DestroyMachineObject(existing.gameObject);
                        }
                        else
                        {
                            DestroyMachineObject(pm.gameObject);
                            continue;
                        }
                    }
                    else
                    {
                        machines[pm.machineId] = pm;
                    }

                    if (pm.isPurchased)
                    {
                        purchasedMachineIds.Add(pm.machineId);
                    }
                }
            }
        }

        public bool IsMachinePurchased(string machineId)
        {
            PruneAndScanSceneMachines();
            if (machines.TryGetValue(machineId, out ProductionMachine pm) && IsValidMachine(pm))
            {
                return pm.isPurchased;
            }
            return purchasedMachineIds.Contains(machineId);
        }

        public static GameObject LoadMachinePrefab(string machineId)
        {
            string prefabPath = "";
            switch (machineId.ToLower())
            {
                case "mill":
                    prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Windmill1.prefab";
                    break;
                case "bakery":
                    prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House3.prefab";
                    break;
                case "dairy":
                    prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House4.prefab";
                    break;
                case "feed_mixer":
                    prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House5.prefab";
                    break;
            }

            GameObject loaded = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(prefabPath))
            {
                loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
#endif
            if (loaded == null)
            {
                loaded = Resources.Load<GameObject>(machineId);
            }
            return loaded;
        }

        public ProductionMachine SpawnPurchasedMachineAtPosition(string machineId, Vector3 spawnPos, Quaternion spawnRot = default)
        {
            PruneAndScanSceneMachines();

            if (!FarmDatabase.Machines.TryGetValue(machineId, out MachineData data))
            {
                Debug.LogError($"[MachineManager] Unknown machine ID: {machineId}");
                return null;
            }

            if (spawnRot == default) spawnRot = Quaternion.identity;

            // Prevent duplicate machine instances if machine already exists
            if (machines.TryGetValue(machineId, out ProductionMachine existingPm) && IsValidMachine(existingPm))
            {
                existingPm.gameObject.SetActive(true);
                existingPm.transform.position = spawnPos;
                existingPm.transform.rotation = spawnRot;
                existingPm.transform.localScale = Vector3.one * 1.2f;
                existingPm.SetupMachine(data, true);
                purchasedMachineIds.Add(machineId);
                return existingPm;
            }

            string clearName = "Purchased_" + (machineId == "mill" ? "Mill" :
                               (machineId == "bakery" ? "Bakery" :
                               (machineId == "dairy" ? "DairyMachine" :
                               (machineId == "jam_maker" ? "JamMaker" : "FeedMixer"))));

            GameObject machineGo = new GameObject(clearName);
            machineGo.transform.position = spawnPos;
            machineGo.transform.rotation = spawnRot;
            machineGo.transform.localScale = Vector3.one * 1.2f;

            ProductionMachine pm = machineGo.AddComponent<ProductionMachine>();
            pm.SetupMachine(data, true);

            machines[machineId] = pm;
            purchasedMachineIds.Add(machineId);

            Debug.Log($"[MachineManager] Successfully spawned {clearName} at {machineGo.transform.position}.");
            return pm;
        }

        public static void CreateProceduralMachine(GameObject root, string machineId, string displayName)
        {
            // Builds the machine-specific procedural model (mill, bakery, dairy, feed mixer, jam maker)
            MachineVisualFactory.Build(root, machineId);
        }

        public bool BuyMachine(string machineId)
        {
            PruneAndScanSceneMachines();

            if (!FarmDatabase.Machines.TryGetValue(machineId, out MachineData data))
            {
                Debug.LogWarning($"[MachineManager] Unknown machine ID: {machineId}");
                return false;
            }

            if (IsMachinePurchased(machineId))
            {
                CropSelectionUI.ShowToast("Máy này đã được mua rồi!");
                return false;
            }

            // 1. Check Level
            int currentLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;
            if (currentLevel < data.unlockLevel)
            {
                CropSelectionUI.ShowToast($"Yêu cầu Level {data.unlockLevel} mới có thể mở khóa!");
                return false;
            }

            // 2. Check Gold
            if (PlayerProgression.Instance == null || !PlayerProgression.TrySpendGold(data.priceGold))
            {
                CropSelectionUI.ShowToast("Không đủ Gold để mua máy!");
                return false;
            }

            // 3. Spawn machine
            Vector3 spawnPos = data.defaultPosition;
            ProductionMachine pm = SpawnPurchasedMachineAtPosition(machineId, spawnPos);
            if (pm != null)
            {
                PlayerProgression.AddXP(25, $"Mua {data.displayName}");
                CropSelectionUI.ShowToast($"Đã mua thành công: {data.displayName}!\n+25 XP");
                SaveLoadSystem.SaveGame();
                return true;
            }

            return false;
        }

        public void FastForwardAllMachines()
        {
            PruneAndScanSceneMachines();
            int count = 0;
            foreach (var kvp in machines)
            {
                ProductionMachine pm = kvp.Value;
                if (IsValidMachine(pm) && pm.state == MachineState.Producing)
                {
                    pm.ForceComplete();
                    count++;
                }
            }
            Debug.Log($"[MachineManager] Fast-forwarded {count} machines to complete!");
        }

        public void ResetPurchasedMachines()
        {
            foreach (var kvp in machines)
            {
                if (IsValidMachine(kvp.Value))
                {
                    DestroyMachineObject(kvp.Value.gameObject);
                }
            }
            machines.Clear();
            purchasedMachineIds.Clear();
            Debug.Log("[MachineManager] Reset all purchased machines.");
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
            SaveLoadSystem.SaveGame();
        }

        /// <summary>
        /// Restores a machine from save data. Purchased machines that are not in the scene yet
        /// (new session / reloaded scene) are respawned at their saved position, then their
        /// recipe, state, timer and pending output are applied.
        /// </summary>
        public ProductionMachine RestoreMachine(MachineSaveData s)
        {
            if (s == null || string.IsNullOrEmpty(s.machineId)) return null;
            if (!FarmDatabase.Machines.TryGetValue(s.machineId, out MachineData data)) return null;

            PruneAndScanSceneMachines();

            bool hasSavedPos = s.hasCustomPosition && (s.posX != 0f || s.posZ != 0f);
            Vector3 targetPos = hasSavedPos ? new Vector3(s.posX, s.posY, s.posZ) : data.defaultPosition;
            Quaternion targetRot = Quaternion.Euler(0f, s.rotY, 0f);
            Vector3 targetScale = (s.scaleX > 0.1f && s.scaleY > 0.1f && s.scaleZ > 0.1f)
                ? new Vector3(s.scaleX, s.scaleY, s.scaleZ)
                : Vector3.one * 1.2f;

            if (!machines.TryGetValue(s.machineId, out ProductionMachine pm) || !IsValidMachine(pm))
            {
                if (!s.isPurchased) return null;
                pm = SpawnPurchasedMachineAtPosition(s.machineId, targetPos, targetRot);
                if (pm == null) return null;
            }

            pm.gameObject.SetActive(true);
            pm.transform.localScale = targetScale;
            pm.isPurchased = s.isPurchased;
            if (s.isPurchased) purchasedMachineIds.Add(s.machineId);
            else purchasedMachineIds.Remove(s.machineId);

            pm.transform.position = targetPos;
            pm.transform.rotation = targetRot;

            // Rebuild visual to guarantee renderers exist, are scaled correctly and activated
            pm.Build3DVisual();

            // Snap flush to ground using exact renderer contact points
            GroundSnapUtility.SnapObjectToGround(pm.gameObject);

            // Ensure MovableFarmObject is present and synchronized
            MovableFarmObject m = pm.GetComponent<MovableFarmObject>() ?? pm.gameObject.AddComponent<MovableFarmObject>();
            m.isDeleted = false;
            m.AutoDetectProperties();
            m.RecordCurrentTransform();

            MachineState parsed = MachineState.Idle;
            Enum.TryParse(s.state, out parsed);
            pm.ApplySavedState(s.recipeId, parsed, s.currentTimer, s.pendingOutputAmount);
            return pm;
        }

        /// <summary>
        /// Removes scene machines that are not part of the loaded save (without refund or auto-save).
        /// </summary>
        public void RemoveMachinesNotInSave(ICollection<string> savedPurchasedIds)
        {
            if (savedPurchasedIds == null || savedPurchasedIds.Count == 0)
            {
                // Safety guard: If save has no machines recorded or empty list, do NOT delete all machines in scene!
                return;
            }

            PruneAndScanSceneMachines();
            List<ProductionMachine> toRemove = new List<ProductionMachine>();
            foreach (var kvp in machines)
            {
                if (IsValidMachine(kvp.Value) && !savedPurchasedIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Value);
                }
            }
            foreach (var pm in toRemove)
            {
                machines.Remove(pm.machineId);
                purchasedMachineIds.Remove(pm.machineId);
                DestroyMachineObject(pm.gameObject);
            }
        }

        public bool RemoveMachine(ProductionMachine pm)
        {
            if (pm == null) return false;
            string foundKey = null;
            foreach (var kvp in machines)
            {
                if (kvp.Value == pm)
                {
                    foundKey = kvp.Key;
                    break;
                }
            }
            if (foundKey != null)
            {
                machines.Remove(foundKey);
            }
            purchasedMachineIds.Remove(pm.machineId);
            DestroyMachineObject(pm.gameObject);
            PruneAndScanSceneMachines();
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
            SaveLoadSystem.SaveGame();
            return true;
        }

        /// <summary>
        /// Deactivates before destroying so a pending (end-of-frame) destroy can never be re-discovered
        /// as a valid purchased machine by a scene scan.
        /// </summary>
        private static void DestroyMachineObject(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            SafeDestroy(go);
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}
