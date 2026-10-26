#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IrishFarmSim.Editor
{
    /// <summary>
    /// Editor utility to generate and maintain CowPen_Small and ChickenCoop_Small prefab assets.
    /// CowPen: 11.0m x 8.0m, Fence Height 1.7m.
    /// ChickenCoop: 8.0m x 6.0m, Fence Height 1.5m.
    /// </summary>
    [InitializeOnLoad]
    public static class AnimalPenPrefabGenerator
    {
        static AnimalPenPrefabGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                GenerateAllPens(false);
            };
        }

        [MenuItem("Farm/Generate Animal Pen Prefabs")]
        public static void MenuGeneratePens()
        {
            GenerateAllPens(true);
        }

        public static void GenerateAllPens(bool forceOverwrite)
        {
            // 1. Generate CowPen_Small.prefab
            GeneratePenPrefab("cow_barn", "Cow Barn / Chuồng bò", "CowPen_Small", "Assets/Prefabs/Farm/CowPen_Small.prefab", forceOverwrite);

            // 2. Generate ChickenCoop_Small.prefab
            GeneratePenPrefab("chicken_coop", "Chicken Coop / Chuồng gà", "ChickenCoop_Small", "Assets/Prefabs/Farm/ChickenCoop_Small.prefab", forceOverwrite);

            // 3. Generate PigPen_Small.prefab
            GeneratePenPrefab("pig_pen", "Pig Pen / Chuồng lợn", "PigPen_Small", "Assets/Prefabs/Farm/PigPen_Small.prefab", forceOverwrite);

            // 4. Generate HorseStable_Small.prefab
            GeneratePenPrefab("horse_stable", "Horse Stable / Chuồng ngựa", "HorseStable_Small", "Assets/Prefabs/Farm/HorseStable_Small.prefab", forceOverwrite);
        }

        private static void GeneratePenPrefab(string bId, string dName, string rootName, string prefabPath, bool forceOverwrite = false)
        {
            if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            string folder = "Assets/Prefabs/Farm";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Farm");
            }

            GameObject root = new GameObject(rootName);
            // Build the fence pen model first so that Awake() on FarmBuilding recognizes the valid existing structure
            FarmBuilding.CreateEnclosedFencePen(root, bId, dName);

            FarmBuilding fb = root.AddComponent<FarmBuilding>();
            fb.buildingId = bId;
            fb.displayName = (bId == "chicken_coop") ? "Chicken Coop / Chuồng gà" :
                             (bId == "pig_pen" ? "Pig Pen / Chuồng lợn" :
                             (bId == "horse_stable" ? "Horse Stable / Chuồng ngựa" : "Cow Barn / Chuồng bò"));
            fb.capacity = (bId == "chicken_coop") ? 5 : (bId == "pig_pen" ? 4 : (bId == "horse_stable" ? 4 : 3));
            fb.allowedAnimalType = (bId == "chicken_coop") ? AnimalType.Chicken :
                                   (bId == "pig_pen" ? AnimalType.Pig : (bId == "horse_stable" ? AnimalType.Horse : AnimalType.Cow));
            fb.buildingType = bId;
            fb.buildingTypeEnum = (bId == "chicken_coop") ? BuildingType.ChickenCoop :
                                  (bId == "pig_pen" ? BuildingType.PigPen : (bId == "horse_stable" ? BuildingType.HorseStable : BuildingType.CowBarn));
            fb.isPurchased = true;

            fb.SetupInteractionCollider();
            fb.SetupBillboard();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AnimalPenPrefabGenerator] Created pen prefab at {prefabPath}");
        }
    }
}
#endif
