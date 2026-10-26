using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Unified Inventory & Storage System managing player crops, seeds, animal feeds, and produce.
    /// Preserves state across scenes via DontDestroyOnLoad.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        public static event Action OnInventoryChanged;

        [Header("Storage Configuration")]
        public int maxStorageCapacity = 200;
        public int StorageCapacity => maxStorageCapacity;

        [Header("Inventory Items")]
        public List<InventoryItemData> items = new List<InventoryItemData>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultItems();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static InventorySystem EnsureInstance()
        {
            if (Instance != null) return Instance;

            InventorySystem found = FindAnyObjectByType<InventorySystem>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("InventorySystem");
            Instance = go.AddComponent<InventorySystem>();
            return Instance;
        }

        public static string NormalizeId(string rawId)
        {
            if (string.IsNullOrEmpty(rawId)) return "";
            string lower = rawId.ToLower().Trim();

            if (lower == "wheatseed" || lower == "seed_wheat") return "WheatSeed";
            if (lower == "cornseed" || lower == "seed_corn") return "CornSeed";
            if (lower == "carrotseed" || lower == "seed_carrot") return "CarrotSeed";
            if (lower == "potatoseed" || lower == "seed_potato") return "PotatoSeed";
            if (lower == "tomatoseed" || lower == "seed_tomato") return "TomatoSeed";
            if (lower == "pumpkinseed" || lower == "seed_pumpkin") return "PumpkinSeed";
            if (lower == "strawberryseed" || lower == "seed_strawberry") return "StrawberrySeed";
            if (lower == "cabbageseed" || lower == "seed_cabbage") return "CabbageSeed";
            if (lower == "watermelonseed" || lower == "seed_watermelon") return "WatermelonSeed";
            if (lower == "hayseed" || lower == "seed_hay" || lower == "grassseed" || lower == "seed_grass") return "HaySeed";

            if (lower == "wheat" || lower == "crop_wheat") return "Wheat";
            if (lower == "corn" || lower == "crop_corn") return "Corn";
            if (lower == "carrot" || lower == "crop_carrot") return "Carrot";
            if (lower == "potato" || lower == "crop_potato") return "Potato";
            if (lower == "tomato" || lower == "crop_tomato") return "Tomato";
            if (lower == "pumpkin" || lower == "crop_pumpkin") return "Pumpkin";
            if (lower == "strawberry" || lower == "crop_strawberry") return "Strawberry";
            if (lower == "cabbage" || lower == "crop_cabbage") return "Cabbage";
            if (lower == "watermelon" || lower == "crop_watermelon") return "Watermelon";
            if (lower == "hay" || lower == "grass") return "Hay";

            if (lower == "grain") return "Grain";
            if (lower == "pellet" || lower == "pellets") return "Pellet";
            if (lower == "milk") return "Milk";
            if (lower == "egg" || lower == "eggs") return "Eggs";
            if (lower == "flour") return "Flour";
            if (lower == "bread") return "Bread";
            if (lower == "cheese") return "Cheese";
            if (lower == "cake") return "Cake";
            if (lower == "strawberryjam" || lower == "strawberry_jam" || lower == "jam") return "StrawberryJam";
            if (lower == "tomatojuice" || lower == "tomato_juice") return "TomatoJuice";
            if (lower == "watermelonjuice" || lower == "watermelon_juice") return "WatermelonJuice";
            if (lower == "bacon") return "Bacon";
            if (lower == "leather") return "Leather";

            return rawId;
        }

        private void InitializeDefaultItems()
        {
            if (items.Count == 0)
            {
                // Starter Seeds for all 10 crops
                AddOrSetItem("WheatSeed", "Wheat Seed / Hạt Lúa mì", ItemType.Seed, 3);
                AddOrSetItem("CornSeed", "Corn Seed / Hạt Ngô", ItemType.Seed, 3);
                AddOrSetItem("CarrotSeed", "Carrot Seed / Hạt Cà rốt", ItemType.Seed, 3);
                AddOrSetItem("PotatoSeed", "Potato Seed / Hạt Khoai tây", ItemType.Seed, 3);
                AddOrSetItem("TomatoSeed", "Tomato Seed / Hạt Cà chua", ItemType.Seed, 3);
                AddOrSetItem("PumpkinSeed", "Pumpkin Seed / Hạt Bí ngô", ItemType.Seed, 3);
                AddOrSetItem("StrawberrySeed", "Strawberry Seed / Hạt Dâu tây", ItemType.Seed, 3);
                AddOrSetItem("CabbageSeed", "Cabbage Seed / Hạt Bắp cải", ItemType.Seed, 3);
                AddOrSetItem("WatermelonSeed", "Watermelon Seed / Hạt Dưa hấu", ItemType.Seed, 3);
                AddOrSetItem("HaySeed", "Grass Seed / Hạt Cỏ chăn nuôi", ItemType.Seed, 3);

                // Feeds
                AddOrSetItem("Grain", "Grain / Ngũ cốc", ItemType.Feed, 10);
                AddOrSetItem("Hay", "Hay / Cỏ khô", ItemType.Feed, 20);
                AddOrSetItem("Pellet", "Pellets / Cám viên", ItemType.Feed, 5);
            }
            else
            {
                EnsureSeedAtLeast("WheatSeed", 3);
                EnsureSeedAtLeast("CornSeed", 3);
                EnsureSeedAtLeast("CarrotSeed", 3);
                EnsureSeedAtLeast("PotatoSeed", 3);
                EnsureSeedAtLeast("TomatoSeed", 3);
                EnsureSeedAtLeast("PumpkinSeed", 3);
                EnsureSeedAtLeast("StrawberrySeed", 3);
                EnsureSeedAtLeast("CabbageSeed", 3);
                EnsureSeedAtLeast("WatermelonSeed", 3);
                EnsureSeedAtLeast("HaySeed", 3);
            }
        }

        private void EnsureSeedAtLeast(string id, int minCount)
        {
            string norm = NormalizeId(id);
            InventoryItemData item = items.Find(i => NormalizeId(i.id) == norm);
            if (item == null)
            {
                AddOrSetItem(norm, GetDefaultItemName(norm), ItemType.Seed, minCount);
            }
        }

        public void ResetSeedsToThree()
        {
            AddOrSetItem("WheatSeed", "Wheat Seed / Hạt Lúa mì", ItemType.Seed, 3);
            AddOrSetItem("CornSeed", "Corn Seed / Hạt Ngô", ItemType.Seed, 3);
            AddOrSetItem("CarrotSeed", "Carrot Seed / Hạt Cà rốt", ItemType.Seed, 3);
            AddOrSetItem("PotatoSeed", "Potato Seed / Hạt Khoai tây", ItemType.Seed, 3);
            AddOrSetItem("TomatoSeed", "Tomato Seed / Hạt Cà chua", ItemType.Seed, 3);
            AddOrSetItem("PumpkinSeed", "Pumpkin Seed / Hạt Bí ngô", ItemType.Seed, 3);
            AddOrSetItem("StrawberrySeed", "Strawberry Seed / Hạt Dâu tây", ItemType.Seed, 3);
            AddOrSetItem("CabbageSeed", "Cabbage Seed / Hạt Bắp cải", ItemType.Seed, 3);
            AddOrSetItem("WatermelonSeed", "Watermelon Seed / Hạt Dưa hấu", ItemType.Seed, 3);
            AddOrSetItem("HaySeed", "Grass Seed / Hạt Cỏ chăn nuôi", ItemType.Seed, 3);
            Debug.Log("[InventorySystem] Reset all 10 crop seed counts to 3.");
            OnInventoryChanged?.Invoke();
        }

        public int GetTotalItemCount()
        {
            int total = 0;
            foreach (var item in items)
            {
                total += item.quantity;
            }
            return total;
        }

        public bool IsFull(int countToAdd = 1)
        {
            return (GetTotalItemCount() + countToAdd) > maxStorageCapacity;
        }

        public bool UpgradeCapacity(int addedCapacity = 50, int costGold = 100)
        {
            if (costGold > 0)
            {
                if (PlayerProgression.Instance == null || !PlayerProgression.TrySpendGold(costGold))
                {
                    CropSelectionUI.ShowToast("Không đủ Gold để nâng cấp kho!");
                    return false;
                }
            }
            maxStorageCapacity += addedCapacity;
            CropSelectionUI.ShowToast($"Đã nâng cấp Kho! Sức chứa mới: {maxStorageCapacity} ô (+{addedCapacity})");
            Debug.Log($"[InventorySystem] Storage upgraded to {maxStorageCapacity} capacity.");
            OnInventoryChanged?.Invoke();
            SaveLoadSystem.SaveGame();
            return true;
        }

        public void SetStorageCapacity(int capacity)
        {
            maxStorageCapacity = Mathf.Max(50, capacity);
            OnInventoryChanged?.Invoke();
        }

        public bool AddItem(string id, string displayName, ItemType type, int count = 1)
        {
            if (count <= 0) return true;
            string normId = NormalizeId(id);

            if (IsFull(count))
            {
                Debug.LogWarning("[InventorySystem] Storage Full! Cannot add " + normId);
                return false;
            }

            InventoryItemData existing = items.Find(i => NormalizeId(i.id) == normId);
            if (existing != null)
            {
                existing.quantity += count;
            }
            else
            {
                items.Add(new InventoryItemData(normId, displayName, type, count));
            }

            SyncToGameController(normId);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool AddItem(string id, int count = 1)
        {
            string normId = NormalizeId(id);
            string defaultName = GetDefaultItemName(normId);
            ItemType defaultType = GetDefaultItemType(normId);
            return AddItem(normId, defaultName, defaultType, count);
        }

        public bool RemoveItem(string id, int count = 1)
        {
            if (count <= 0) return true;
            string normId = NormalizeId(id);

            InventoryItemData existing = items.Find(i => NormalizeId(i.id) == normId);
            if (existing == null || existing.quantity < count)
            {
                return false;
            }

            existing.quantity -= count;
            if (existing.quantity <= 0)
            {
                items.Remove(existing);
            }

            SyncToGameController(normId);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public InventoryItemData GetItem(string id)
        {
            string normId = NormalizeId(id);
            return items.Find(i => NormalizeId(i.id) == normId);
        }

        public int GetItemCount(string id)
        {
            string normId = NormalizeId(id);
            InventoryItemData item = items.Find(i => NormalizeId(i.id) == normId);
            return item != null ? item.quantity : 0;
        }

        public bool HasItem(string id, int requiredCount = 1)
        {
            return GetItemCount(id) >= requiredCount;
        }

        public void AddOrSetItem(string id, string displayName, ItemType type, int quantity)
        {
            string normId = NormalizeId(id);
            InventoryItemData existing = items.Find(i => NormalizeId(i.id) == normId);
            if (existing != null)
            {
                existing.quantity = quantity;
            }
            else
            {
                items.Add(new InventoryItemData(normId, displayName, type, quantity));
            }
            SyncToGameController(normId);
            OnInventoryChanged?.Invoke();
        }

        public List<InventoryItemData> GetAllItems()
        {
            return new List<InventoryItemData>(items);
        }

        public void ClearAll()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        private void SyncToGameController(string id)
        {
            string normId = NormalizeId(id);
            GameController gc = GameController.Instance();
            if (gc == null || gc.player == null) return;

            if (normId.Equals("Grain", StringComparison.OrdinalIgnoreCase))
                gc.player.grain = GetItemCount("Grain");
            else if (normId.Equals("Hay", StringComparison.OrdinalIgnoreCase))
                gc.player.hay = GetItemCount("Hay");
            else if (normId.Equals("Pellet", StringComparison.OrdinalIgnoreCase))
                gc.player.pellet = GetItemCount("Pellet");
        }

        public static string GetDefaultItemName(string normId)
        {
            switch (normId)
            {
                case "WheatSeed": return "Wheat Seed / Hạt Lúa mì";
                case "CornSeed": return "Corn Seed / Hạt Ngô";
                case "CarrotSeed": return "Carrot Seed / Hạt Cà rốt";
                case "PotatoSeed": return "Potato Seed / Hạt Khoai tây";
                case "TomatoSeed": return "Tomato Seed / Hạt Cà chua";
                case "PumpkinSeed": return "Pumpkin Seed / Hạt Bí ngô";
                case "StrawberrySeed": return "Strawberry Seed / Hạt Dâu tây";
                case "CabbageSeed": return "Cabbage Seed / Hạt Bắp cải";
                case "WatermelonSeed": return "Watermelon Seed / Hạt Dưa hấu";
                case "HaySeed": return "Grass Seed / Hạt Cỏ chăn nuôi";
                case "Wheat": return "Wheat / Lúa mì";
                case "Corn": return "Corn / Ngô";
                case "Carrot": return "Carrot / Cà rốt";
                case "Potato": return "Potato / Khoai tây";
                case "Tomato": return "Tomato / Cà chua";
                case "Pumpkin": return "Pumpkin / Bí ngô";
                case "Strawberry": return "Strawberry / Dâu tây";
                case "Cabbage": return "Cabbage / Bắp cải";
                case "Watermelon": return "Watermelon / Dưa hấu";
                case "Hay": return "Hay / Cỏ khô";
                case "Grain": return "Grain / Ngũ cốc";
                case "Pellet": return "Pellets / Cám viên";
                case "Milk": return "Milk / Sữa bò";
                case "Eggs": return "Eggs / Trứng gà";
                case "Flour": return "Flour / Bột mì";
                case "Bread": return "Bread / Bánh mì";
                case "Cheese": return "Cheese / Phô mai";
                case "Cake": return "Cake / Bánh ngọt";
                case "StrawberryJam": return "Strawberry Jam / Mứt dâu tây";
                case "TomatoJuice": return "Tomato Juice / Nước ép cà chua";
                case "WatermelonJuice": return "Watermelon Juice / Nước ép dưa hấu";
                case "Bacon": return "Bacon / Thịt xông khói";
                case "Leather": return "Leather / Da thuộc";
                default: return normId;
            }
        }

        private ItemType GetDefaultItemType(string normId)
        {
            if (normId.EndsWith("Seed", StringComparison.OrdinalIgnoreCase)) return ItemType.Seed;
            if (normId == "Wheat" || normId == "Corn" || normId == "Carrot" ||
                normId == "Potato" || normId == "Tomato" || normId == "Pumpkin" ||
                normId == "Strawberry" || normId == "Cabbage" || normId == "Watermelon")
                return ItemType.Crop;
            if (normId == "Hay" || normId == "Grain" || normId == "Pellet") return ItemType.Feed;
            if (normId == "Milk" || normId == "Eggs" || normId == "Bacon" || normId == "Leather") return ItemType.AnimalProduct;
            if (normId == "Flour" || normId == "Bread" || normId == "Cheese" ||
                normId == "Cake" || normId == "StrawberryJam" || normId == "TomatoJuice" || normId == "WatermelonJuice") return ItemType.Crop;
            return ItemType.Misc;
        }
    }
}
