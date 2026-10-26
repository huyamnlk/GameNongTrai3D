using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    [System.Serializable]
    public class CropData
    {
        [Header("Identifiers")]
        public string cropId;
        public string displayName;
        public string seedItemId;
        public string harvestItemId;

        [Header("Growth & Yield")]
        public float growthTime; // in seconds
        public int harvestAmount;
        public int plantXp;
        public int harvestXp;
        public int harvestGold;

        [Header("Progression & Economy")]
        public int unlockLevel = 1;
        public int seedPrice = 10;

        [Header("Visual Config")]
        public Color plantColor;
        public Color ripeColor;
        public GameObject plantedVisual;
        public GameObject matureVisual;

        // Compatibility aliases
        public string cropName => displayName;
        public float growthDuration => growthTime;
        public int plantXP => plantXp;
        public int harvestXP => harvestXp;

        public CropData() { }

        public CropData(string cropId, string displayName, string seedItemId, string harvestItemId,
                        float growthTime, int harvestAmount, int plantXp, int harvestXp, int harvestGold,
                        Color plantColor, Color ripeColor, int unlockLevel = 1, int seedPrice = 10)
        {
            this.cropId = cropId;
            this.displayName = displayName;
            this.seedItemId = seedItemId;
            this.harvestItemId = harvestItemId;
            this.growthTime = growthTime;
            this.harvestAmount = harvestAmount;
            this.plantXp = plantXp;
            this.harvestXp = harvestXp;
            this.harvestGold = harvestGold;
            this.plantColor = plantColor;
            this.ripeColor = ripeColor;
            this.unlockLevel = unlockLevel;
            this.seedPrice = seedPrice;
        }

        private static Dictionary<string, CropData> registry;

        public static CropData GetCrop(string cropId)
        {
            EnsureRegistry();
            if (string.IsNullOrEmpty(cropId)) return registry["wheat"];
            string key = cropId.ToLower().Trim();
            if (key == "grass" || key == "feed" || key == "feed_crop") key = "hay";
            if (registry.TryGetValue(key, out var data)) return data;
            foreach (var kv in registry)
            {
                if (kv.Value.seedItemId.Equals(cropId, StringComparison.OrdinalIgnoreCase) ||
                    kv.Value.harvestItemId.Equals(cropId, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return registry["wheat"];
        }

        public static IEnumerable<CropData> GetAllCrops()
        {
            EnsureRegistry();
            return registry.Values;
        }

        private static void EnsureRegistry()
        {
            if (registry != null) return;
            registry = new Dictionary<string, CropData>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "wheat",
                    new CropData(
                        "wheat",
                        "Wheat / Lúa mì",
                        "WheatSeed",
                        "Wheat",
                        30f, 3, 2, 10, 15,
                        new Color(0.38f, 0.68f, 0.24f),
                        new Color(0.92f, 0.76f, 0.22f),
                        1, 10
                    )
                },
                {
                    "corn",
                    new CropData(
                        "corn",
                        "Corn / Ngô",
                        "CornSeed",
                        "Corn",
                        45f, 3, 2, 12, 25,
                        new Color(0.28f, 0.64f, 0.22f),
                        new Color(0.94f, 0.78f, 0.18f),
                        2, 15
                    )
                },
                {
                    "carrot",
                    new CropData(
                        "carrot",
                        "Carrot / Cà rốt",
                        "CarrotSeed",
                        "Carrot",
                        20f, 2, 2, 8, 12,
                        new Color(0.32f, 0.68f, 0.24f),
                        new Color(0.95f, 0.45f, 0.12f),
                        1, 8
                    )
                },
                {
                    "potato",
                    new CropData(
                        "potato",
                        "Potato / Khoai tây",
                        "PotatoSeed",
                        "Potato",
                        28f, 3, 2, 10, 16,
                        new Color(0.30f, 0.62f, 0.22f),
                        new Color(0.78f, 0.60f, 0.34f),
                        2, 12
                    )
                },
                {
                    "tomato",
                    new CropData(
                        "tomato",
                        "Tomato / Cà chua",
                        "TomatoSeed",
                        "Tomato",
                        35f, 3, 3, 12, 22,
                        new Color(0.26f, 0.58f, 0.20f),
                        new Color(0.88f, 0.18f, 0.14f),
                        3, 20
                    )
                },
                {
                    "pumpkin",
                    new CropData(
                        "pumpkin",
                        "Pumpkin / Bí ngô",
                        "PumpkinSeed",
                        "Pumpkin",
                        50f, 2, 3, 15, 32,
                        new Color(0.30f, 0.58f, 0.20f),
                        new Color(0.94f, 0.47f, 0.10f),
                        3, 25
                    )
                },
                {
                    "strawberry",
                    new CropData(
                        "strawberry",
                        "Strawberry / Dâu tây",
                        "StrawberrySeed",
                        "Strawberry",
                        32f, 4, 3, 12, 24,
                        new Color(0.28f, 0.62f, 0.22f),
                        new Color(0.88f, 0.15f, 0.20f),
                        4, 25
                    )
                },
                {
                    "cabbage",
                    new CropData(
                        "cabbage",
                        "Cabbage / Bắp cải",
                        "CabbageSeed",
                        "Cabbage",
                        26f, 2, 2, 10, 18,
                        new Color(0.36f, 0.72f, 0.30f),
                        new Color(0.56f, 0.82f, 0.40f),
                        4, 18
                    )
                },
                {
                    "watermelon",
                    new CropData(
                        "watermelon",
                        "Watermelon / Dưa hấu",
                        "WatermelonSeed",
                        "Watermelon",
                        55f, 2, 4, 16, 36,
                        new Color(0.22f, 0.56f, 0.20f),
                        new Color(0.16f, 0.48f, 0.20f),
                        5, 35
                    )
                },
                {
                    "hay",
                    new CropData(
                        "hay",
                        "Grass / Cỏ chăn nuôi",
                        "HaySeed",
                        "Hay",
                        25f, 3, 2, 10, 15,
                        new Color(0.38f, 0.72f, 0.28f),
                        new Color(0.62f, 0.76f, 0.32f),
                        2, 10
                    )
                }
            };
        }

        public static CropData CreateWheat() => GetCrop("wheat");
        public static CropData CreateCorn() => GetCrop("corn");
        public static CropData CreateCarrot() => GetCrop("carrot");
        public static CropData CreatePotato() => GetCrop("potato");
        public static CropData CreateTomato() => GetCrop("tomato");
        public static CropData CreatePumpkin() => GetCrop("pumpkin");
        public static CropData CreateStrawberry() => GetCrop("strawberry");
        public static CropData CreateCabbage() => GetCrop("cabbage");
        public static CropData CreateWatermelon() => GetCrop("watermelon");
        public static CropData CreateHay() => GetCrop("hay");
    }
}
