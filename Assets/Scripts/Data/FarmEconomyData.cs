using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    public enum ShopCategory
    {
        Seeds,
        AnimalFeed,
        Animals,
        Buildings,
        Machines,
        Plots
    }

    [Serializable]
    public class ShopItemData
    {
        public string id;
        public string displayName;
        public ShopCategory category;
        public string description;
        public int priceGold;
        public int unlockLevel;
        public string unlockDesc;
        public string relatedId; // Target seed ID, animal ID, building ID, or machine ID
        public int quantityPerPurchase;

        public ShopItemData(string id, string displayName, ShopCategory category, string description, int priceGold, int unlockLevel, string relatedId, int quantityPerPurchase = 1)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.description = description;
            this.priceGold = priceGold;
            this.unlockLevel = unlockLevel;
            this.unlockDesc = unlockLevel > 1 ? $"Mở khóa ở Level {unlockLevel}" : "Đã mở khóa";
            this.relatedId = relatedId;
            this.quantityPerPurchase = quantityPerPurchase;
        }
    }

    [Serializable]
    public class AnimalData
    {
        public string animalId;
        public string displayName;
        public int buyPrice;
        public int priceGold => buyPrice;
        public int unlockLevel;
        public string requiredBarnType; // "cow_barn", "chicken_coop", "pig_pen", "horse_stable"
        public string preferredFeed;     // "grain", "hay", "pellet"
        public string productItemId;     // "milk", "egg", "bacon", "truffle"
        public string productName;
        public int productAmount;
        public float productionDuration; // Seconds
        public int careXP;
        public int harvestXP;

        public AnimalData(string animalId, string displayName, int buyPrice, int unlockLevel, string requiredBarnType, string preferredFeed, string productItemId, string productName, int productAmount, float productionDuration, int careXP, int harvestXP)
        {
            this.animalId = animalId;
            this.displayName = displayName;
            this.buyPrice = buyPrice;
            this.unlockLevel = unlockLevel;
            this.requiredBarnType = requiredBarnType;
            this.preferredFeed = preferredFeed;
            this.productItemId = productItemId;
            this.productName = productName;
            this.productAmount = productAmount;
            this.productionDuration = productionDuration;
            this.careXP = careXP;
            this.harvestXP = harvestXP;
        }
    }

    [Serializable]
    public class BuildingData
    {
        public string buildingId;
        public string displayName;
        public int priceGold;
        public int unlockLevel;
        public int capacity;
        public string buildingType; // "cow_barn", "chicken_coop", "pig_pen", "storage", "machine_shed"
        public Vector3 defaultPosition;
        public string description;

        public BuildingData(string buildingId, string displayName, int priceGold, int unlockLevel, int capacity, string buildingType, Vector3 defaultPosition, string description)
        {
            this.buildingId = buildingId;
            this.displayName = displayName;
            this.priceGold = priceGold;
            this.unlockLevel = unlockLevel;
            this.capacity = capacity;
            this.buildingType = buildingType;
            this.defaultPosition = defaultPosition;
            this.description = description;
        }
    }

    [Serializable]
    public class ProductionRecipeData
    {
        public string recipeId;
        public string displayName;
        public string inputItemId;
        public int inputAmount;
        public string secondaryInputId;
        public int secondaryInputAmount;
        public string outputItemId;
        public int outputAmount;
        public float productionTime;
        public int productionXP;
        public string tertiaryInputId;
        public int tertiaryInputAmount;

        public ProductionRecipeData(string recipeId, string displayName, string inputItemId, int inputAmount, string outputItemId, int outputAmount, float productionTime, int productionXP, string secondaryInputId = "", int secondaryInputAmount = 0, string tertiaryInputId = "", int tertiaryInputAmount = 0)
        {
            this.tertiaryInputId = tertiaryInputId;
            this.tertiaryInputAmount = tertiaryInputAmount;
            this.recipeId = recipeId;
            this.displayName = displayName;
            this.inputItemId = inputItemId;
            this.inputAmount = inputAmount;
            this.outputItemId = outputItemId;
            this.outputAmount = outputAmount;
            this.productionTime = productionTime;
            this.productionXP = productionXP;
            this.secondaryInputId = secondaryInputId;
            this.secondaryInputAmount = secondaryInputAmount;
        }

        /// <summary>
        /// Lists every required ingredient (id, amount) of this recipe, skipping empty slots.
        /// </summary>
        public List<KeyValuePair<string, int>> GetIngredients()
        {
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
            if (!string.IsNullOrEmpty(inputItemId) && inputAmount > 0)
                list.Add(new KeyValuePair<string, int>(inputItemId, inputAmount));
            if (!string.IsNullOrEmpty(secondaryInputId) && secondaryInputAmount > 0)
                list.Add(new KeyValuePair<string, int>(secondaryInputId, secondaryInputAmount));
            if (!string.IsNullOrEmpty(tertiaryInputId) && tertiaryInputAmount > 0)
                list.Add(new KeyValuePair<string, int>(tertiaryInputId, tertiaryInputAmount));
            return list;
        }
    }

    [Serializable]
    public class MachineData
    {
        public string machineId;
        public string displayName;
        public int priceGold;
        public int unlockLevel;
        public Vector3 defaultPosition;
        public string description;
        public ProductionRecipeData defaultRecipe;
        public List<ProductionRecipeData> recipes = new List<ProductionRecipeData>();

        public MachineData(string machineId, string displayName, int priceGold, int unlockLevel, Vector3 defaultPosition, string description, ProductionRecipeData defaultRecipe, params ProductionRecipeData[] extraRecipes)
        {
            this.machineId = machineId;
            this.displayName = displayName;
            this.priceGold = priceGold;
            this.unlockLevel = unlockLevel;
            this.defaultPosition = defaultPosition;
            this.description = description;
            this.defaultRecipe = defaultRecipe;
            recipes.Add(defaultRecipe);
            if (extraRecipes != null)
            {
                foreach (var r in extraRecipes)
                {
                    if (r != null) recipes.Add(r);
                }
            }
        }

        public ProductionRecipeData FindRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            return recipes.Find(r => r != null && string.Equals(r.recipeId, recipeId, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Serializable]
    public class PlotData
    {
        public string plotId;
        public string displayName;
        public int unlockLevel;
        public int unlockCost;
        public bool isUnlockedByDefault;
        public Vector2 gridOffset;

        public PlotData(string plotId, string displayName, int unlockLevel, int unlockCost, bool isUnlockedByDefault, Vector2 gridOffset)
        {
            this.plotId = plotId;
            this.displayName = displayName;
            this.unlockLevel = unlockLevel;
            this.unlockCost = unlockCost;
            this.isUnlockedByDefault = isUnlockedByDefault;
            this.gridOffset = gridOffset;
        }
    }

    /// <summary>
    /// Master database of all shop items, animals, buildings, machines, and recipes.
    /// </summary>
    public static class FarmDatabase
    {
        public static List<ShopItemData> ShopItems { get; private set; }
        public static Dictionary<string, AnimalData> Animals { get; private set; }
        public static Dictionary<string, BuildingData> Buildings { get; private set; }
        public static Dictionary<string, MachineData> Machines { get; private set; }
        public static Dictionary<string, PlotData> Plots { get; private set; }

        static FarmDatabase()
        {
            InitDatabase();
        }

        public static void InitDatabase()
        {
            // 1. Animals
            Animals = new Dictionary<string, AnimalData>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "cow", new AnimalData("cow", "Bò sữa (Cow)", 100, 1, "cow_barn", "hay", "milk", "Sữa tươi (Milk)", 2, 25f, 5, 15)
                },
                {
                    "chicken", new AnimalData("chicken", "Gà đẻ trứng (Chicken)", 60, 2, "chicken_coop", "grain", "egg", "Trứng gà (Egg)", 3, 15f, 4, 10)
                },
                {
                    "pig", new AnimalData("pig", "Lợn thịt (Pig)", 150, 4, "pig_pen", "pellet", "bacon", "Thịt xông khói (Bacon)", 2, 30f, 8, 20)
                },
                {
                    "horse", new AnimalData("horse", "Ngựa chiến (Horse)", 300, 6, "horse_stable", "hay", "leather", "Da thuộc (Leather)", 1, 35f, 12, 30)
                }
            };

            // 2. Buildings
            Buildings = new Dictionary<string, BuildingData>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "cow_barn", new BuildingData("cow_barn", "Cow Barn / Chuồng bò", 200, 1, 3, "cow_barn", new Vector3(72f, 0f, 245f), "Nơi nuôi dưỡng và vắt sữa đàn bò của nông trại. Sức chứa: 3 con bò.")
                },
                {
                    "chicken_coop", new BuildingData("chicken_coop", "Chicken Coop / Chuồng gà", 150, 2, 5, "chicken_coop", new Vector3(88f, 0f, 238f), "Chuồng nuôi gà ấm cúng, thu hoạch trứng mỗi ngày. Sức chứa: 5 con gà.")
                },
                {
                    "pig_pen", new BuildingData("pig_pen", "Pig Pen / Chuồng lợn", 250, 4, 4, "pig_pen", new Vector3(56f, 0f, 248f), "Khu vực nuôi lợn sạch sẽ, sản xuất thịt chất lượng cao. Sức chứa: 4 con lợn.")
                },
                {
                    "horse_stable", new BuildingData("horse_stable", "Horse Stable / Chuồng ngựa", 350, 6, 4, "horse_stable", new Vector3(42f, 0f, 252f), "Chuồng nuôi dưỡng đàn ngựa trang trại. Sức chứa: 4 con ngựa.")
                },
                {
                    "storage", new BuildingData("storage", "Kho mở rộng (Storage Building)", 100, 1, 50, "storage", new Vector3(82f, 0f, 260f), "Nâng cấp dung lượng kho thêm +50 ô chứa nguyên liệu và nông sản.")
                }
            };

            // 3. Machines & Recipes
            Machines = new Dictionary<string, MachineData>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "mill", new MachineData(
                        "mill",
                        "Cối xay bột (Mill)",
                        150,
                        1,
                        new Vector3(104f, 0f, 235f),
                        "Nghiền lúa mì thành bột mì mịn màng chất lượng cao.",
                        new ProductionRecipeData("wheat_to_flour", "Xay lúa mì thành bột", "wheat", 1, "flour", 1, 10f, 12)
                    )
                },
                {
                    "bakery", new MachineData(
                        "bakery",
                        "Lò làm bánh (Bakery)",
                        250,
                        2,
                        new Vector3(112f, 0f, 245f),
                        "Nướng bánh mì thơm ngon từ Bột mì và Trứng gà tươi.",
                        new ProductionRecipeData("bake_bread", "Nướng bánh mì", "flour", 1, "bread", 1, 15f, 25, "egg", 1),
                        new ProductionRecipeData("bake_cake", "Nướng bánh ngọt", "flour", 1, "cake", 1, 25f, 35, "egg", 1, "milk", 1)
                    )
                },
                {
                    "dairy", new MachineData(
                        "dairy",
                        "Máy làm phô mai (Dairy Machine)",
                        300,
                        3,
                        new Vector3(120f, 0f, 255f),
                        "Chế biến sữa bò tươi nguyên chất thành Phô mai hảo hạng.",
                        new ProductionRecipeData("milk_to_cheese", "Làm phô mai từ sữa", "milk", 1, "cheese", 1, 15f, 20)
                    )
                },
                {
                    "feed_mixer", new MachineData(
                        "feed_mixer",
                        "Máy trộn cám (Feed Mixer)",
                        200,
                        2,
                        new Vector3(96f, 0f, 255f),
                        "Trộn lúa mì hoặc cỏ khô thành cám viên cao cấp cho gia súc.",
                        new ProductionRecipeData("mix_pellets", "Trộn cám viên Pellets", "wheat", 1, "pellet", 2, 10f, 10),
                        new ProductionRecipeData("mix_pellets_corn", "Trộn cám từ ngô", "corn", 1, "pellet", 2, 10f, 10)
                    )
                },
                {
                    "jam_maker", new MachineData(
                        "jam_maker",
                        "Máy mứt & nước ép (Jam & Juice Maker)",
                        320,
                        3,
                        new Vector3(128f, 0f, 240f),
                        "Nấu dâu tây thành mứt, ép cà chua và dưa hấu thành nước ép tươi.",
                        new ProductionRecipeData("strawberry_jam", "Nấu mứt dâu tây", "strawberry", 3, "strawberry_jam", 1, 20f, 28),
                        new ProductionRecipeData("tomato_juice", "Ép nước cà chua", "tomato", 2, "tomato_juice", 2, 15f, 18),
                        new ProductionRecipeData("watermelon_juice", "Ép nước dưa hấu", "watermelon", 1, "watermelon_juice", 2, 18f, 22)
                    )
                }
            };

            // 4. Plots (26 plots across 5 unlock levels)
            Plots = new Dictionary<string, PlotData>(StringComparer.OrdinalIgnoreCase);
            float colSp = 3.2f;
            float rowSp = 3.2f;

            // Level 1: 6 initial plots (Row 0 & 1, 3 columns)
            Plots["CropPlot_01"] = new PlotData("CropPlot_01", "Ô đất 01", 1, 0, true,  new Vector2(-colSp, -rowSp * 0.5f));
            Plots["CropPlot_02"] = new PlotData("CropPlot_02", "Ô đất 02", 1, 0, true,  new Vector2( 0f,    -rowSp * 0.5f));
            Plots["CropPlot_03"] = new PlotData("CropPlot_03", "Ô đất 03", 1, 0, true,  new Vector2( colSp, -rowSp * 0.5f));
            Plots["CropPlot_04"] = new PlotData("CropPlot_04", "Ô đất 04", 1, 0, true,  new Vector2(-colSp,  rowSp * 0.5f));
            Plots["CropPlot_05"] = new PlotData("CropPlot_05", "Ô đất 05", 1, 0, true,  new Vector2( 0f,     rowSp * 0.5f));
            Plots["CropPlot_06"] = new PlotData("CropPlot_06", "Ô đất 06", 1, 0, true,  new Vector2( colSp,  rowSp * 0.5f));

            // Level 2: +4 plots (Row 2, 4 columns)
            Plots["CropPlot_07"] = new PlotData("CropPlot_07", "Ô đất 07", 2, 100, false, new Vector2(-colSp * 1.5f, rowSp * 1.5f));
            Plots["CropPlot_08"] = new PlotData("CropPlot_08", "Ô đất 08", 2, 100, false, new Vector2(-colSp * 0.5f, rowSp * 1.5f));
            Plots["CropPlot_09"] = new PlotData("CropPlot_09", "Ô đất 09", 2, 100, false, new Vector2( colSp * 0.5f, rowSp * 1.5f));
            Plots["CropPlot_10"] = new PlotData("CropPlot_10", "Ô đất 10", 2, 100, false, new Vector2( colSp * 1.5f, rowSp * 1.5f));

            // Level 3: +4 plots (Row 3, 4 columns)
            Plots["CropPlot_11"] = new PlotData("CropPlot_11", "Ô đất 11", 3, 200, false, new Vector2(-colSp * 1.5f, rowSp * 2.5f));
            Plots["CropPlot_12"] = new PlotData("CropPlot_12", "Ô đất 12", 3, 200, false, new Vector2(-colSp * 0.5f, rowSp * 2.5f));
            Plots["CropPlot_13"] = new PlotData("CropPlot_13", "Ô đất 13", 3, 200, false, new Vector2( colSp * 0.5f, rowSp * 2.5f));
            Plots["CropPlot_14"] = new PlotData("CropPlot_14", "Ô đất 14", 3, 200, false, new Vector2( colSp * 1.5f, rowSp * 2.5f));

            // Level 4: +6 plots (Row 4, 6 columns)
            Plots["CropPlot_15"] = new PlotData("CropPlot_15", "Ô đất 15", 4, 350, false, new Vector2(-colSp * 2.5f, rowSp * 3.5f));
            Plots["CropPlot_16"] = new PlotData("CropPlot_16", "Ô đất 16", 4, 350, false, new Vector2(-colSp * 1.5f, rowSp * 3.5f));
            Plots["CropPlot_17"] = new PlotData("CropPlot_17", "Ô đất 17", 4, 350, false, new Vector2(-colSp * 0.5f, rowSp * 3.5f));
            Plots["CropPlot_18"] = new PlotData("CropPlot_18", "Ô đất 18", 4, 350, false, new Vector2( colSp * 0.5f, rowSp * 3.5f));
            Plots["CropPlot_19"] = new PlotData("CropPlot_19", "Ô đất 19", 4, 350, false, new Vector2( colSp * 1.5f, rowSp * 3.5f));
            Plots["CropPlot_20"] = new PlotData("CropPlot_20", "Ô đất 20", 4, 350, false, new Vector2( colSp * 2.5f, rowSp * 3.5f));

            // Level 5: +6 plots (Row 5, 6 columns)
            Plots["CropPlot_21"] = new PlotData("CropPlot_21", "Ô đất 21", 5, 500, false, new Vector2(-colSp * 2.5f, rowSp * 4.5f));
            Plots["CropPlot_22"] = new PlotData("CropPlot_22", "Ô đất 22", 5, 500, false, new Vector2(-colSp * 1.5f, rowSp * 4.5f));
            Plots["CropPlot_23"] = new PlotData("CropPlot_23", "Ô đất 23", 5, 500, false, new Vector2(-colSp * 0.5f, rowSp * 4.5f));
            Plots["CropPlot_24"] = new PlotData("CropPlot_24", "Ô đất 24", 5, 500, false, new Vector2( colSp * 0.5f, rowSp * 4.5f));
            Plots["CropPlot_25"] = new PlotData("CropPlot_25", "Ô đất 25", 5, 500, false, new Vector2( colSp * 1.5f, rowSp * 4.5f));
            Plots["CropPlot_26"] = new PlotData("CropPlot_26", "Ô đất 26", 5, 500, false, new Vector2( colSp * 2.5f, rowSp * 4.5f));

            // 5. Shop Items
            ShopItems = new List<ShopItemData>
            {
                // SEEDS (All 10 Crops with distinct level progression)
                new ShopItemData("shop_seed_wheat", "Wheat Seed (Hạt Lúa mì)", ShopCategory.Seeds, "Lúa mì năng suất cao, thu hoạch sau 30s.", 10, 1, "WheatSeed", 3),
                new ShopItemData("shop_seed_carrot", "Carrot Seed (Hạt Cà rốt)", ShopCategory.Seeds, "Cà rốt giòn ngọt giàu vitamin, thu hoạch sau 20s.", 8, 1, "CarrotSeed", 3),
                new ShopItemData("shop_seed_corn", "Corn Seed (Hạt Ngô)", ShopCategory.Seeds, "Bắp ngô ngọt năng suất cao, thu hoạch sau 45s.", 15, 2, "CornSeed", 3),
                new ShopItemData("shop_seed_potato", "Potato Seed (Hạt Khoai tây)", ShopCategory.Seeds, "Khoai tây củ to vàng ươm, thu hoạch sau 28s.", 12, 2, "PotatoSeed", 3),
                new ShopItemData("shop_seed_hay", "Grass Seed (Hạt Cỏ chăn nuôi)", ShopCategory.Seeds, "Cỏ chăn nuôi làm thức ăn cho bò và ngựa, thu hoạch sau 25s.", 10, 2, "HaySeed", 3),
                new ShopItemData("shop_seed_tomato", "Tomato Seed (Hạt Cà chua)", ShopCategory.Seeds, "Cà chua leo giàn chín đỏ mọng, thu hoạch sau 35s.", 20, 3, "TomatoSeed", 3),
                new ShopItemData("shop_seed_pumpkin", "Pumpkin Seed (Hạt Bí ngô)", ShopCategory.Seeds, "Bí ngô quả lớn giá trị cao, thu hoạch sau 50s.", 25, 3, "PumpkinSeed", 3),
                new ShopItemData("shop_seed_strawberry", "Strawberry Seed (Hạt Dâu tây)", ShopCategory.Seeds, "Dâu tây đỏ mọng thơm ngọt, thu hoạch sau 32s.", 25, 4, "StrawberrySeed", 3),
                new ShopItemData("shop_seed_cabbage", "Cabbage Seed (Hạt Bắp cải)", ShopCategory.Seeds, "Bắp cải xanh tươi ngon, thu hoạch sau 26s.", 18, 4, "CabbageSeed", 3),
                new ShopItemData("shop_seed_watermelon", "Watermelon Seed (Hạt Dưa hấu)", ShopCategory.Seeds, "Dưa hấu quả to ngọt mát, thu hoạch sau 55s.", 35, 5, "WatermelonSeed", 3),

                // ANIMAL FEED
                new ShopItemData("shop_feed_grain", "Grain (Ngũ cốc / Thóc)", ShopCategory.AnimalFeed, "Thức ăn bổ dưỡng thiết yếu cho gà và gia súc.", 12, 1, "grain", 5),
                new ShopItemData("shop_feed_hay", "Hay (Cỏ khô)", ShopCategory.AnimalFeed, "Thức ăn thô xanh thiết yếu hàng ngày cho bò.", 8, 1, "hay", 5),
                new ShopItemData("shop_feed_pellet", "Pellets (Cám viên dinh dưỡng)", ShopCategory.AnimalFeed, "Thức ăn tăng trọng nhanh giúp vật nuôi sản xuất siêu tốc.", 15, 1, "pellet", 5),

                // ANIMALS
                new ShopItemData("shop_animal_cow", "Bò sữa (Cow)", ShopCategory.Animals, "Cho sữa tươi thơm ngon mỗi 25s. Yêu cầu Chuồng bò (Tối đa 3 con).", 100, 1, "cow", 1),
                new ShopItemData("shop_animal_chicken", "Gà mái (Chicken)", ShopCategory.Animals, "Đẻ trứng đều đặn mỗi 15s. Yêu cầu Chuồng gà (Tối đa 5 con).", 60, 2, "chicken", 1),
                new ShopItemData("shop_animal_pig", "Lợn (Pig)", ShopCategory.Animals, "Sản xuất thịt xông khói giá trị cao. Yêu cầu Chuồng lợn (Tối đa 4 con).", 150, 4, "pig", 1),
                new ShopItemData("shop_animal_horse", "Ngựa (Horse)", ShopCategory.Animals, "Ngựa trang trại uy dũng. Yêu cầu Chuồng ngựa (Tối đa 4 con).", 300, 6, "horse", 1),

                // BUILDINGS
                new ShopItemData("shop_building_cow_barn", "Cow Barn / Chuồng bò", ShopCategory.Buildings, "Chuồng bò tiêu chuẩn sức chứa tối đa 3 con bò.", 200, 1, "cow_barn", 1),
                new ShopItemData("shop_building_chicken_coop", "Chicken Coop / Chuồng gà", ShopCategory.Buildings, "Chuồng gà ấm áp sức chứa tối đa 5 con gà.", 150, 2, "chicken_coop", 1),
                new ShopItemData("shop_building_pig_pen", "Pig Pen / Chuồng lợn", ShopCategory.Buildings, "Chuồng lợn tiêu chuẩn sức chứa tối đa 4 con lợn.", 250, 4, "pig_pen", 1),
                new ShopItemData("shop_building_horse_stable", "Horse Stable / Chuồng ngựa", ShopCategory.Buildings, "Chuồng ngựa tiêu chuẩn sức chứa tối đa 4 con ngựa.", 350, 6, "horse_stable", 1),
                new ShopItemData("shop_building_storage", "Nâng cấp Kho (+50 Sức chứa)", ShopCategory.Buildings, "Gia tăng sức chứa kho đồ để lưu trữ nhiều nông sản hơn.", 100, 1, "storage", 1),

                // MACHINES
                new ShopItemData("shop_machine_mill", "Cối xay bột (Mill)", ShopCategory.Machines, "Nghiền Lúa mì thành Bột mì (Flour).", 150, 1, "mill", 1),
                new ShopItemData("shop_machine_bakery", "Lò làm bánh (Bakery)", ShopCategory.Machines, "Nướng Bột mì + Trứng thành Bánh mì, hoặc thêm Sữa để làm Bánh ngọt (Cake).", 250, 2, "bakery", 1),
                new ShopItemData("shop_machine_dairy", "Máy phô mai (Dairy Machine)", ShopCategory.Machines, "Chế biến Sữa tươi thành Phô mai (Cheese).", 300, 3, "dairy", 1),
                new ShopItemData("shop_machine_feed_mixer", "Máy trộn cám (Feed Mixer)", ShopCategory.Machines, "Trộn Lúa mì hoặc Ngô thành 2 túi Cám viên (Pellets).", 200, 2, "feed_mixer", 1),
                new ShopItemData("shop_machine_jam_maker", "Máy mứt & nước ép (Jam & Juice Maker)", ShopCategory.Machines, "Dâu tây thành Mứt; Cà chua, Dưa hấu thành Nước ép.", 320, 3, "jam_maker", 1),

                // EXPANSION PLOTS (CropPlot_07 to 26; 01 to 06 are default free unlocked)
                new ShopItemData("shop_plot_07", "Ô đất trồng #07", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 100, 2, "CropPlot_07", 1),
                new ShopItemData("shop_plot_08", "Ô đất trồng #08", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 100, 2, "CropPlot_08", 1),
                new ShopItemData("shop_plot_09", "Ô đất trồng #09", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 100, 2, "CropPlot_09", 1),
                new ShopItemData("shop_plot_10", "Ô đất trồng #10", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 100, 2, "CropPlot_10", 1),

                new ShopItemData("shop_plot_11", "Ô đất trồng #11", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 200, 3, "CropPlot_11", 1),
                new ShopItemData("shop_plot_12", "Ô đất trồng #12", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 200, 3, "CropPlot_12", 1),
                new ShopItemData("shop_plot_13", "Ô đất trồng #13", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 200, 3, "CropPlot_13", 1),
                new ShopItemData("shop_plot_14", "Ô đất trồng #14", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 200, 3, "CropPlot_14", 1),

                new ShopItemData("shop_plot_15", "Ô đất trồng #15", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_15", 1),
                new ShopItemData("shop_plot_16", "Ô đất trồng #16", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_16", 1),
                new ShopItemData("shop_plot_17", "Ô đất trồng #17", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_17", 1),
                new ShopItemData("shop_plot_18", "Ô đất trồng #18", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_18", 1),
                new ShopItemData("shop_plot_19", "Ô đất trồng #19", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_19", 1),
                new ShopItemData("shop_plot_20", "Ô đất trồng #20", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 350, 4, "CropPlot_20", 1),

                new ShopItemData("shop_plot_21", "Ô đất trồng #21", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_21", 1),
                new ShopItemData("shop_plot_22", "Ô đất trồng #22", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_22", 1),
                new ShopItemData("shop_plot_23", "Ô đất trồng #23", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_23", 1),
                new ShopItemData("shop_plot_24", "Ô đất trồng #24", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_24", 1),
                new ShopItemData("shop_plot_25", "Ô đất trồng #25", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_25", 1),
                new ShopItemData("shop_plot_26", "Ô đất trồng #26", ShopCategory.Plots, "Mở rộng diện tích canh tác thêm 1 ô đất mới.", 500, 5, "CropPlot_26", 1),
            };
        }

        public static Dictionary<string, int> ItemSellPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "wheat", 15 },
            { "corn", 18 },
            { "carrot", 12 },
            { "potato", 16 },
            { "tomato", 22 },
            { "pumpkin", 32 },
            { "strawberry", 24 },
            { "cabbage", 18 },
            { "watermelon", 36 },
            { "hay", 10 },
            { "feed_hay", 10 },
            { "grain", 8 },
            { "feed_grain", 8 },
            { "pellet", 10 },
            { "pellets", 10 },
            { "feed_pellets", 10 },
            { "milk", 35 },
            { "egg", 20 },
            { "eggs", 20 },
            { "bacon", 40 },
            { "leather", 50 },
            { "flour", 25 },
            { "bread", 45 },
            { "cheese", 60 },
            { "cake", 110 },
            { "strawberryjam", 110 },
            { "tomatojuice", 35 },
            { "watermelonjuice", 28 },
            { "wheat_seed", 5 },
            { "corn_seed", 8 },
            { "carrot_seed", 6 }
        };

        public static int GetItemSellPrice(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 10;
            if (ItemSellPrices.TryGetValue(itemId, out int price)) return price;
            return 10;
        }

        public static Dictionary<string, int> AnimalSellPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "cow", 100 },
            { "chicken", 50 },
            { "pig", 150 },
            { "horse", 300 }
        };

        public static int GetAnimalSellPrice(string animalType)
        {
            if (string.IsNullOrEmpty(animalType)) return 50;
            if (AnimalSellPrices.TryGetValue(animalType, out int price)) return price;
            return 50;
        }

        public static int GetBuildingRefundGold(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return 50;
            if (Buildings.TryGetValue(buildingId, out BuildingData data))
            {
                return Mathf.RoundToInt(data.priceGold * 0.7f); // Hoàn lại 70% giá mua
            }
            if (Machines.TryGetValue(buildingId, out MachineData mData))
            {
                return Mathf.RoundToInt(mData.priceGold * 0.7f);
            }
            return 50;
        }
    }
}
