using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Core Player Progression system managing Level, Current XP, Gold/Cash, and Level thresholds.
    /// Supports automatic level-ups while preserving leftover XP,
    /// and dispatches events for UI updates and floating popups.
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        public static PlayerProgression Instance { get; private set; }

        public static event Action<int, int, int> OnXPChanged; // (currentXP, xpToNextLevel, currentLevel)
        public static event Action<int> OnLevelUp;              // (newLevel)
        public static event Action<int, string> OnXPAwarded;    // (amount, sourceDescription)
        public static event Action<int> OnGoldChanged;          // (currentGold)

        [Header("Progression Data")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int gold = 100;
        [SerializeField] private int baseXPPerLevel = 50;

        public HashSet<string> unlockedPlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> unlockedCropIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> unlockedMachineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int Level => level;
        public int CurrentLevel => level;
        public int CurrentXP => currentXP;
        public int Gold => gold;
        public int XPToNextLevel => CalculateXPRequiredForLevel(level);

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SyncUnlockedItems();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SyncUnlockedItems()
        {
            // 1. Initial 6 plots unlocked by default
            for (int i = 1; i <= 6; i++)
            {
                unlockedPlotIds.Add($"CropPlot_{i:D2}");
            }

            // 2. Unlock crops based on level
            foreach (var crop in CropData.GetAllCrops())
            {
                if (crop != null && level >= crop.unlockLevel)
                {
                    unlockedCropIds.Add(crop.cropId);
                }
            }

            // 3. Unlock machines based on level
            if (FarmDatabase.Machines != null)
            {
                foreach (var kv in FarmDatabase.Machines)
                {
                    if (kv.Value != null && level >= kv.Value.unlockLevel)
                    {
                        unlockedMachineIds.Add(kv.Key);
                    }
                }
            }
        }

        public bool IsPlotUnlocked(string plotId)
        {
            if (string.IsNullOrEmpty(plotId)) return true;
            if (unlockedPlotIds.Contains(plotId)) return true;
            if (FarmDatabase.Plots != null && FarmDatabase.Plots.TryGetValue(plotId, out var pd))
            {
                if (pd.isUnlockedByDefault)
                {
                    unlockedPlotIds.Add(plotId);
                    return true;
                }
            }
            return false;
        }

        public bool UnlockPlot(string plotId)
        {
            if (string.IsNullOrEmpty(plotId)) return false;
            unlockedPlotIds.Add(plotId);
            return true;
        }

        public bool IsCropUnlocked(string cropId)
        {
            if (string.IsNullOrEmpty(cropId)) return true;
            CropData c = CropData.GetCrop(cropId);
            if (c == null) return true;
            return level >= c.unlockLevel || unlockedCropIds.Contains(c.cropId);
        }

        public bool IsMachineUnlocked(string machineId)
        {
            if (string.IsNullOrEmpty(machineId)) return true;
            if (FarmDatabase.Machines != null && FarmDatabase.Machines.TryGetValue(machineId, out var md))
            {
                return level >= md.unlockLevel || unlockedMachineIds.Contains(machineId);
            }
            return true;
        }

        public static int CalculateXPRequiredForLevel(int lvl)
        {
            return Mathf.Max(50, lvl * 50);
        }

        public static void AddXP(int amount, string source = "")
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.InternalAddXP(amount, source);
            }
        }

        public void InternalAddXP(int amount, string source)
        {
            if (amount <= 0) return;

            currentXP += amount;
            Debug.Log($"[PlayerProgression] +{amount} XP ({source}). Tổng XP: {currentXP}/{XPToNextLevel}");

            OnXPAwarded?.Invoke(amount, source);

            while (currentXP >= XPToNextLevel)
            {
                int req = XPToNextLevel;
                currentXP -= req;
                level++;
                SyncUnlockedItems();
                Debug.Log($"[PlayerProgression] CHÚC MỪNG LÊN CẤP! Level mới: {level}");
                OnLevelUp?.Invoke(level);
            }

            OnXPChanged?.Invoke(currentXP, XPToNextLevel, level);
        }

        public static void AddGold(int amount, string source = "")
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.InternalAddGold(amount, source);
            }
        }

        public void InternalAddGold(int amount, string source)
        {
            if (amount <= 0) return;
            gold += amount;
            Debug.Log($"[PlayerProgression] +{amount} Gold ({source}). Tổng Gold: {gold}");
            OnGoldChanged?.Invoke(gold);
        }

        public static bool TrySpendGold(int amount)
        {
            EnsureInstance();
            if (Instance == null) return false;
            if (Instance.gold < amount) return false;
            Instance.gold -= amount;
            OnGoldChanged?.Invoke(Instance.gold);
            return true;
        }

        public void SetProgression(int newLevel, int newXP, int newGold = -1)
        {
            level = Mathf.Max(1, newLevel);
            currentXP = Mathf.Max(0, newXP);
            if (newGold >= 0) gold = newGold;
            SyncUnlockedItems();
            OnXPChanged?.Invoke(currentXP, XPToNextLevel, level);
            OnGoldChanged?.Invoke(gold);
        }

        public static PlayerProgression EnsureInstance()
        {
            if (Instance != null) return Instance;

            PlayerProgression found = FindAnyObjectByType<PlayerProgression>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("PlayerProgression");
            Instance = go.AddComponent<PlayerProgression>();
            return Instance;
        }
    }
}
