using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    [Serializable]
    public class AchievementData
    {
        public string id;
        public string title;
        public string description;
        public int targetCount;
        public int currentCount;
        public int rewardGold;
        public int rewardXP;
        public bool isClaimed;

        public bool IsAchieved => currentCount >= targetCount;
        public float ProgressRatio => targetCount > 0 ? Mathf.Clamp01((float)currentCount / targetCount) : 0f;
    }

    /// <summary>
    /// Master Achievement system managing Vietnamese farm milestones.
    /// Tracks crops harvested, animal species owned, total gold, quests completed, and player level.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        public static event Action OnAchievementsUpdated;
        public static event Action<AchievementData> OnAchievementUnlocked;

        public List<AchievementData> achievements = new List<AchievementData>();

        // Lifetime counters
        public int totalCropsHarvested = 0;
        public int totalDecorationsMovedOrPlaced = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static AchievementSystem EnsureInstance()
        {
            if (Instance != null) return Instance;

            AchievementSystem found = FindAnyObjectByType<AchievementSystem>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("AchievementSystem");
            Instance = go.AddComponent<AchievementSystem>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAchievements();
                SubscribeEvents();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            CheckAllAchievements();
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void SubscribeEvents()
        {
            CropPlot.OnCropHarvested += HandleCropHarvested;
            PlayerProgression.OnGoldChanged += HandleGoldChanged;
            PlayerProgression.OnLevelUp += HandleLevelUp;
            QuestSystem.OnQuestCompleted += HandleQuestCompleted;
        }

        private void UnsubscribeEvents()
        {
            CropPlot.OnCropHarvested -= HandleCropHarvested;
            PlayerProgression.OnGoldChanged -= HandleGoldChanged;
            PlayerProgression.OnLevelUp -= HandleLevelUp;
            QuestSystem.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void InitializeAchievements()
        {
            if (achievements.Count > 0) return;

            achievements.Add(new AchievementData
            {
                id = "ach_harvest_100",
                title = "Nông Dân Chăm Chỉ",
                description = "Thu hoạch 100 cây trồng từ các ô đất",
                targetCount = 100,
                currentCount = 0,
                rewardGold = 500,
                rewardXP = 200,
                isClaimed = false
            });

            achievements.Add(new AchievementData
            {
                id = "ach_animals_4",
                title = "Trại Chăn Nuôi Phong Phú",
                description = "Sở hữu đủ 4 loài vật nuôi (Bò, Gà, Heo, Ngựa)",
                targetCount = 4,
                currentCount = 0,
                rewardGold = 350,
                rewardXP = 150,
                isClaimed = false
            });

            achievements.Add(new AchievementData
            {
                id = "ach_gold_1000",
                title = "Triệu Phú Nông Trại",
                description = "Tích lũy đạt mốc 1.000 Vàng trong kho bạc",
                targetCount = 1000,
                currentCount = 0,
                rewardGold = 400,
                rewardXP = 200,
                isClaimed = false
            });

            achievements.Add(new AchievementData
            {
                id = "ach_quests_10",
                title = "Người Hoàn Thành Nhiệm Vụ",
                description = "Hoàn thành 10 nhiệm vụ hằng ngày",
                targetCount = 10,
                currentCount = 0,
                rewardGold = 350,
                rewardXP = 180,
                isClaimed = false
            });

            achievements.Add(new AchievementData
            {
                id = "ach_level_5",
                title = "Bậc Thầy Nông Nghiệp",
                description = "Đạt Cấp độ nông trại 5",
                targetCount = 5,
                currentCount = 1,
                rewardGold = 600,
                rewardXP = 300,
                isClaimed = false
            });

            achievements.Add(new AchievementData
            {
                id = "ach_decor_5",
                title = "Nhà Thiết Kế Nông Trại",
                description = "Chỉnh sửa hoặc đặt 5 vật trang trí/công trình",
                targetCount = 5,
                currentCount = 0,
                rewardGold = 250,
                rewardXP = 100,
                isClaimed = false
            });
        }

        public void CheckAllAchievements()
        {
            // Update Gold
            if (PlayerProgression.Instance != null)
            {
                UpdateProgress("ach_gold_1000", PlayerProgression.Instance.Gold);
                UpdateProgress("ach_level_5", PlayerProgression.Instance.Level);
            }

            // Update Animals count
            UpdateAnimalSpeciesCount();

            // Update Quests count
            if (QuestSystem.Instance != null)
            {
                UpdateProgress("ach_quests_10", QuestSystem.Instance.totalCompletedQuestsCount);
            }

            // Update Harvest count
            UpdateProgress("ach_harvest_100", totalCropsHarvested);

            // Update Decor count
            UpdateProgress("ach_decor_5", totalDecorationsMovedOrPlaced);
        }

        private void UpdateAnimalSpeciesCount()
        {
            HashSet<AnimalType> ownedSpecies = new HashSet<AnimalType>();
            AnimalCareController[] allAnimals = FindObjectsByType<AnimalCareController>(FindObjectsSortMode.None);
            foreach (var a in allAnimals)
            {
                if (a != null && a.gameObject.activeInHierarchy)
                {
                    ownedSpecies.Add(a.animalType);
                }
            }
            UpdateProgress("ach_animals_4", ownedSpecies.Count);
        }

        public void NotifyDecorationPlacedOrMoved()
        {
            totalDecorationsMovedOrPlaced++;
            UpdateProgress("ach_decor_5", totalDecorationsMovedOrPlaced);
        }

        private void HandleCropHarvested(string cropName, int xp)
        {
            totalCropsHarvested++;
            UpdateProgress("ach_harvest_100", totalCropsHarvested);
        }

        private void HandleGoldChanged(int currentGold)
        {
            UpdateProgress("ach_gold_1000", currentGold);
        }

        private void HandleLevelUp(int newLevel)
        {
            UpdateProgress("ach_level_5", newLevel);
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (QuestSystem.Instance != null)
            {
                UpdateProgress("ach_quests_10", QuestSystem.Instance.totalCompletedQuestsCount);
            }
        }

        public void UpdateProgress(string achievementId, int newCount)
        {
            AchievementData a = achievements.Find(x => x.id == achievementId);
            if (a == null) return;

            bool wasAchieved = a.IsAchieved;
            a.currentCount = Mathf.Max(a.currentCount, newCount);

            if (!wasAchieved && a.IsAchieved)
            {
                Debug.Log($"[AchievementSystem] 🏆 ĐÃ ĐẠT THÀNH TỰU: {a.title}!");
                CropSelectionUI.ShowToast($"🏆 ĐÃ ĐẠT THÀNH TỰU MỚI!\n★ {a.title} ★\n(Mở Bảng Thành Tựu để nhận thưởng)");
                OnAchievementUnlocked?.Invoke(a);
            }

            OnAchievementsUpdated?.Invoke();
        }

        public bool ClaimReward(string achievementId)
        {
            AchievementData a = achievements.Find(x => x.id == achievementId);
            if (a == null) return false;
            if (!a.IsAchieved)
            {
                CropSelectionUI.ShowToast("Thành tựu chưa hoàn thành!");
                return false;
            }
            if (a.isClaimed)
            {
                CropSelectionUI.ShowToast("Phần thưởng đã được nhận!");
                return false;
            }

            a.isClaimed = true;
            PlayerProgression.AddGold(a.rewardGold, $"Thành tựu: {a.title}");
            PlayerProgression.AddXP(a.rewardXP, $"Thành tựu: {a.title}");

            CropSelectionUI.ShowToast($"Đã nhận thưởng Thành tựu:\n+{a.rewardGold} Vàng  ★  +{a.rewardXP} XP");
            OnAchievementsUpdated?.Invoke();
            SaveLoadSystem.SaveGame();
            return true;
        }

        public void LoadData(List<AchievementSaveData> savedList, int cropsHarvested, int decorsPlaced)
        {
            totalCropsHarvested = cropsHarvested;
            totalDecorationsMovedOrPlaced = decorsPlaced;

            if (savedList != null && savedList.Count > 0)
            {
                foreach (var s in savedList)
                {
                    AchievementData a = achievements.Find(x => x.id == s.id);
                    if (a != null)
                    {
                        a.currentCount = Mathf.Max(a.currentCount, s.currentCount);
                        a.isClaimed = s.isClaimed;
                    }
                }
            }

            CheckAllAchievements();
        }
    }
}
