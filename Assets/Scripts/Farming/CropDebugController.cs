using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Debug controller for fast gameplay testing during Play Mode in Unity Editor.
    /// F1: Complete timer for all crops (ForceMature all).
    /// F2: Reset inventory seeds to 3 for each crop type.
    /// F3: Add +100 XP to player progression.
    /// F4: Add +100 Gold to player and ready animal products.
    /// </summary>
    public class CropDebugController : MonoBehaviour
    {
        public static CropDebugController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static CropDebugController EnsureInstance()
        {
            if (Instance != null) return Instance;

            CropDebugController found = FindAnyObjectByType<CropDebugController>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("CropDebugController");
            Instance = go.AddComponent<CropDebugController>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;

            // F1: +1000 Gold
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TriggerF1AddGold();
            }

            // F2: Tăng Level (+1 Level)
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TriggerF2LevelUp();
            }

            // F3: Mở khóa vật nuôi / shop
            if (Input.GetKeyDown(KeyCode.F3))
            {
                TriggerF3UnlockAnimals();
            }

            // F4: Reset công trình test (Barns, Storage, Machines)
            if (Input.GetKeyDown(KeyCode.F4))
            {
                TriggerF4ResetTestBuildings();
            }

            // F5: Hoàn tất timer máy móc & sản xuất
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TriggerF5CompleteMachineTimers();
            }
#endif
        }

        public void TriggerF1AddGold()
        {
            PlayerProgression.AddGold(1000, "Debug F1 Key");
            Debug.Log("[CropDebugController F1] +1000 Gold awarded!");
            CropSelectionUI.ShowToast("[F1 Debug] +1000 Gold!");
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
        }

        public void TriggerF2LevelUp()
        {
            if (PlayerProgression.Instance != null)
            {
                int nextLvl = PlayerProgression.Instance.Level + 1;
                PlayerProgression.Instance.SetProgression(nextLvl, 0, PlayerProgression.Instance.Gold);
                Debug.Log($"[CropDebugController F2] Level increased to {nextLvl}!");
                CropSelectionUI.ShowToast($"[F2 Debug] Tăng Cấp độ: Đạt Level {nextLvl}!");
            }
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
        }

        public void TriggerF3UnlockAnimals()
        {
            ShopUI.DebugUnlockAll = !ShopUI.DebugUnlockAll;
            string status = ShopUI.DebugUnlockAll ? "ĐÃ MỞ KHÓA VẬT NUÔI & CÔNG TRÌNH" : "ÁP DỤNG LẠI LEVEL KHÓA SHOP";
            Debug.Log($"[CropDebugController F3] Animal/Shop unlock toggle: {status}");
            CropSelectionUI.ShowToast($"[F3 Debug] {status}!");
            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
        }

        public void TriggerF4ResetTestBuildings()
        {
            if (FarmBuildingManager.Instance != null)
            {
                FarmBuildingManager.Instance.ResetPurchasedBuildings();
            }
            if (MachineManager.Instance != null)
            {
                MachineManager.Instance.ResetPurchasedMachines();
            }
            Debug.Log("[CropDebugController F4] Reset all test buildings and machines.");
            CropSelectionUI.ShowToast("[F4 Debug] Đã reset toàn bộ công trình và máy móc test!");
        }

        public void TriggerF5CompleteMachineTimers()
        {
            // 1. Fast forward all machines
            if (MachineManager.Instance != null)
            {
                MachineManager.Instance.FastForwardAllMachines();
            }

            // 2. Force mature all crops
            CropPlot[] plots = FindObjectsByType<CropPlot>();
            int cropCount = 0;
            foreach (var p in plots)
            {
                if (p != null && (p.state == PlotState.Growing || p.state == PlotState.Planted))
                {
                    p.ForceMature();
                    cropCount++;
                }
            }

            // 3. Fast forward all animals
            if (AnimalManager.Instance != null)
            {
                AnimalManager.Instance.FastForwardAllAnimals();
            }

            Debug.Log($"[CropDebugController F5] Fast-forwarded all machine, crop ({cropCount}), and animal timers!");
            CropSelectionUI.ShowToast($"[F5 Debug] Hoàn tất timer máy móc, cây trồng ({cropCount}) và vật nuôi!");
        }

        public void TriggerF4CompleteAllTimers()
        {
            // 1. Force mature all crops
            CropPlot[] plots = FindObjectsByType<CropPlot>();
            int cropCount = 0;
            foreach (var p in plots)
            {
                if (p != null && (p.state == PlotState.Growing || p.state == PlotState.Planted))
                {
                    p.ForceMature();
                    cropCount++;
                }
            }

            // 2. Fast forward all machines
            if (MachineManager.Instance != null)
            {
                MachineManager.Instance.FastForwardAllMachines();
            }

            // 3. Fast forward all animals
            if (AnimalManager.Instance != null)
            {
                AnimalManager.Instance.FastForwardAllAnimals();
            }

            Debug.Log($"[CropDebugController F4] Fast-forwarded all production timers ({cropCount} crops, all machines & animals)!");
            CropSelectionUI.ShowToast($"[F4 Debug] Hoàn tất toàn bộ timer sản xuất: Cây trồng ({cropCount}), Máy móc và Vật nuôi!");
        }

        public void TriggerF1ForceMatureAll() => TriggerF4CompleteAllTimers();

        public void TriggerF5ResetTestData()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.ResetSeedsToThree();
            }
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.SetProgression(1, 0, 500);
            }
            ShopUI.DebugUnlockAll = false;
            Debug.Log("[CropDebugController F5] Reset all test data (Seeds=3, Level=1, XP=0, Gold=500).");
            CropSelectionUI.ShowToast("[F5 Debug] Đã reset toàn bộ dữ liệu test về mặc định!");
        }
    }
}
