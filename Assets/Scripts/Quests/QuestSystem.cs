using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    public enum QuestType
    {
        HarvestCrops,      // Thu hoạch cây trồng
        FeedAnimals,       // Cho vật nuôi ăn
        CollectProducts,   // Thu thập sản phẩm từ vật nuôi
        SellItems,         // Bán nông sản
        BuyShopItems       // Mua vật phẩm trong shop
    }

    [Serializable]
    public class QuestData
    {
        public string questId;
        public string title;
        public string description;
        public QuestType type;
        public int targetCount;
        public int currentCount;
        public int rewardGold;
        public int rewardXP;
        public bool isClaimed;

        public bool IsCompleted => currentCount >= targetCount;
        public float ProgressRatio => targetCount > 0 ? Mathf.Clamp01((float)currentCount / targetCount) : 0f;
    }

    /// <summary>
    /// Master Daily Quest system managing 5 daily farm missions in Vietnamese.
    /// Resets missions on every new in-game day, awards Gold and XP,
    /// triggers completion toasts, and integrates with Save/Load.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        public static event Action OnQuestsUpdated;
        public static event Action<QuestData> OnQuestCompleted;

        [Header("Active Daily Quests")]
        public List<QuestData> activeQuests = new List<QuestData>();
        public int lastGeneratedDay = -1;
        public int totalCompletedQuestsCount = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static QuestSystem EnsureInstance()
        {
            if (Instance != null) return Instance;

            QuestSystem found = FindAnyObjectByType<QuestSystem>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("QuestSystem");
            Instance = go.AddComponent<QuestSystem>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SubscribeEvents();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            int currentDay = WeatherSystem.Instance != null ? WeatherSystem.Instance.currentDay : 1;
            if (activeQuests.Count == 0 || lastGeneratedDay != currentDay)
            {
                GenerateDailyQuests(currentDay);
            }
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void SubscribeEvents()
        {
            CropPlot.OnCropHarvested += HandleCropHarvested;
            AnimalCareController.OnAnimalFed += HandleAnimalFed;
            AnimalCareController.OnProductCollected += HandleProductCollected;
            ProductionMachine.OnProductCollected += HandleMachineProductCollected;
            StorageUI.OnItemSold += HandleItemSold;
            ShopUI.OnItemPurchased += HandleItemPurchased;
            WeatherSystem.OnDayOrWeatherChanged += HandleDayChanged;
        }

        private void UnsubscribeEvents()
        {
            CropPlot.OnCropHarvested -= HandleCropHarvested;
            AnimalCareController.OnAnimalFed -= HandleAnimalFed;
            AnimalCareController.OnProductCollected -= HandleProductCollected;
            ProductionMachine.OnProductCollected -= HandleMachineProductCollected;
            StorageUI.OnItemSold -= HandleItemSold;
            ShopUI.OnItemPurchased -= HandleItemPurchased;
            WeatherSystem.OnDayOrWeatherChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int newDay, WeatherType weather)
        {
            if (newDay != lastGeneratedDay)
            {
                GenerateDailyQuests(newDay);
            }
        }

        public void GenerateDailyQuests(int day)
        {
            lastGeneratedDay = day;
            activeQuests.Clear();

            // 1. Thu hoạch cây trồng
            activeQuests.Add(new QuestData
            {
                questId = $"quest_harvest_{day}",
                title = "Mùa Vàng Thu Hoạch",
                description = "Thu hoạch nông sản từ các ô đất trồng",
                type = QuestType.HarvestCrops,
                targetCount = 5,
                currentCount = 0,
                rewardGold = 100,
                rewardXP = 45,
                isClaimed = false
            });

            // 2. Cho vật nuôi ăn
            activeQuests.Add(new QuestData
            {
                questId = $"quest_feed_{day}",
                title = "Bữa Ăn No Nê",
                description = "Cho bò, gà, heo hoặc ngựa ăn bằng cỏ/ngô/cám",
                type = QuestType.FeedAnimals,
                targetCount = 3,
                currentCount = 0,
                rewardGold = 80,
                rewardXP = 35,
                isClaimed = false
            });

            // 3. Thu thập sản phẩm từ vật nuôi
            activeQuests.Add(new QuestData
            {
                questId = $"quest_collect_{day}",
                title = "Thu Gom Nông Phẩm",
                description = "Thu hoạch sữa bò tươi, trứng gà, thịt xông khói hoặc da thuộc",
                type = QuestType.CollectProducts,
                targetCount = 2,
                currentCount = 0,
                rewardGold = 120,
                rewardXP = 50,
                isClaimed = false
            });

            // 4. Bán nông sản
            activeQuests.Add(new QuestData
            {
                questId = $"quest_sell_{day}",
                title = "Giao Thương Nông Sản",
                description = "Mở Kho đồ và bán nông sản lấy tiền vàng",
                type = QuestType.SellItems,
                targetCount = 2,
                currentCount = 0,
                rewardGold = 90,
                rewardXP = 40,
                isClaimed = false
            });

            // 5. Mua vật phẩm trong shop
            activeQuests.Add(new QuestData
            {
                questId = $"quest_shop_{day}",
                title = "Khách Hàng Thân Thiết",
                description = "Ghé Cửa hàng và mua hạt giống, thức ăn hoặc nông cụ",
                type = QuestType.BuyShopItems,
                targetCount = 1,
                currentCount = 0,
                rewardGold = 70,
                rewardXP = 30,
                isClaimed = false
            });

            Debug.Log($"[QuestSystem] Đã tạo 5 nhiệm vụ hằng ngày cho Ngày {day}!");
            OnQuestsUpdated?.Invoke();
        }

        public void IncrementProgress(QuestType type, int amount = 1)
        {
            bool anyUpdated = false;
            foreach (var q in activeQuests)
            {
                if (q.type == type && !q.IsCompleted)
                {
                    q.currentCount = Mathf.Min(q.targetCount, q.currentCount + amount);
                    anyUpdated = true;

                    if (q.IsCompleted)
                    {
                        totalCompletedQuestsCount++;
                        Debug.Log($"[QuestSystem] 🎉 Hoàn thành nhiệm vụ: {q.title}!");
                        CropSelectionUI.ShowToast($"🎉 ĐÃ HOÀN THÀNH NHIỆM VỤ!\n★ {q.title} ★\n(Mở Bảng Nhiệm Vụ để nhận thưởng)");
                        OnQuestCompleted?.Invoke(q);
                    }
                }
            }

            if (anyUpdated)
            {
                OnQuestsUpdated?.Invoke();
            }
        }

        private void HandleCropHarvested(string cropName, int xp)
        {
            IncrementProgress(QuestType.HarvestCrops, 1);
        }

        private void HandleAnimalFed(AnimalCareController animal, string feedId)
        {
            IncrementProgress(QuestType.FeedAnimals, 1);
        }

        private void HandleProductCollected(AnimalCareController animal, string productId, int amount)
        {
            IncrementProgress(QuestType.CollectProducts, 1);
        }

        private void HandleMachineProductCollected(ProductionMachine machine, string productId, int amount)
        {
            IncrementProgress(QuestType.CollectProducts, 1);
        }

        private void HandleItemSold(string itemId, int quantity, int totalGold)
        {
            IncrementProgress(QuestType.SellItems, 1);
        }

        private void HandleItemPurchased(ShopItemData item)
        {
            IncrementProgress(QuestType.BuyShopItems, 1);
        }

        public bool ClaimReward(string questId)
        {
            QuestData q = activeQuests.Find(x => x.questId == questId);
            if (q == null) return false;
            if (!q.IsCompleted)
            {
                CropSelectionUI.ShowToast("Nhiệm vụ chưa hoàn thành!");
                return false;
            }
            if (q.isClaimed)
            {
                CropSelectionUI.ShowToast("Phần thưởng này đã được nhận rồi!");
                return false;
            }

            q.isClaimed = true;
            PlayerProgression.AddGold(q.rewardGold, $"Thưởng nhiệm vụ: {q.title}");
            PlayerProgression.AddXP(q.rewardXP, $"Thưởng nhiệm vụ: {q.title}");

            CropSelectionUI.ShowToast($"Đã nhận thưởng nhiệm vụ:\n+{q.rewardGold} Vàng  ★  +{q.rewardXP} XP");
            Debug.Log($"[QuestSystem] Claimed reward for {q.title}: +{q.rewardGold} Gold, +{q.rewardXP} XP.");

            OnQuestsUpdated?.Invoke();
            SaveLoadSystem.SaveGame();
            return true;
        }

        public void LoadData(List<QuestSaveData> savedList, int day, int totalCompleted)
        {
            totalCompletedQuestsCount = totalCompleted;
            lastGeneratedDay = day;

            if (savedList != null && savedList.Count > 0)
            {
                activeQuests.Clear();
                foreach (var s in savedList)
                {
                    QuestType t = QuestType.HarvestCrops;
                    Enum.TryParse(s.type, true, out t);
                    activeQuests.Add(new QuestData
                    {
                        questId = s.questId,
                        title = s.title,
                        description = s.description,
                        type = t,
                        targetCount = s.targetCount,
                        currentCount = s.currentCount,
                        rewardGold = s.rewardGold,
                        rewardXP = s.rewardXP,
                        isClaimed = s.isClaimed
                    });
                }
                OnQuestsUpdated?.Invoke();
            }
            else
            {
                GenerateDailyQuests(day);
            }
        }
    }
}
