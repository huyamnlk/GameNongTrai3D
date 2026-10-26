using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Ensures all cows in the scene have AnimalCareController components,
    /// sets up a Chicken Coop for eggs production, and handles animal fast-forward debug.
    /// </summary>
    public class AnimalManager : MonoBehaviour
    {
        private static AnimalManager _instance;
        public static AnimalManager Instance
        {
            get
            {
                if (_instance == null) EnsureInstance();
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoInitialize()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Farm", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureInstance();
            }
        }

        public static AnimalManager EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            AnimalManager existing = UnityEngine.Object.FindFirstObjectByType<AnimalManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            if (!Application.isPlaying)
            {
                return null;
            }

            GameObject go = new GameObject("AnimalManager");
            _instance = go.AddComponent<AnimalManager>();
            return _instance;
        }

        public List<AnimalCareController> animals = new List<AnimalCareController>();
        private bool _hasInitialized = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (gameObject.name == "AnimalManager" && transform.childCount == 0)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Destroy(this);
                }
                return;
            }

            _instance = this;
            PurgeShowcaseAndSpriteArtifacts();
        }

        void Start()
        {
            PurgeShowcaseAndSpriteArtifacts();
            if (SaveLoadSystem.HasSave())
            {
                SyncExistingAnimalsList();
                return;
            }
            if (!_hasInitialized)
            {
                InitializeAnimals();
            }
        }

        /// <summary>
        /// Cleans up any leftover 2D sprite showcase objects or SpriteRenderers on 3D animals.
        /// </summary>
        public static void PurgeShowcaseAndSpriteArtifacts()
        {
            string[] showcaseNames = new string[]
            {
                "=== FARM_ART_SHOWCASE ===",
                "=== FARM_ART_PREVIEW ===",
                "ShowcaseContainer"
            };

            foreach (var sName in showcaseNames)
            {
                GameObject found = GameObject.Find(sName);
                while (found != null)
                {
                    Debug.Log($"[AnimalManager] Removing obsolete art showcase container: {found.name}");
                    if (Application.isPlaying)
                    {
                        found.SetActive(false);
                        found.name = "_ PurgedShowcase";
                        Destroy(found);
                        break;
                    }
                    else
                    {
                        DestroyImmediate(found);
                        found = GameObject.Find(sName);
                    }
                }
            }

            // Also remove any stray Showcase_Animal_* or Showcase_Crop_* objects anywhere in the scene
            Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null || t.gameObject == null) continue;
                if (t.name.StartsWith("Showcase_Animal_", System.StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith("Showcase_Crop_", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (Application.isPlaying)
                        Destroy(t.gameObject);
                    else
                        DestroyImmediate(t.gameObject);
                }
            }

            // Ensure no 3D animal has a conflicting SpriteRenderer attached
            AnimalCareController[] allAnimals = FindObjectsByType<AnimalCareController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var acc in allAnimals)
            {
                if (acc == null) continue;
                SpriteRenderer sr = acc.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (Application.isPlaying) Destroy(sr);
                    else DestroyImmediate(sr);
                }
            }
        }

        public void SyncExistingAnimalsList()
        {
            PurgeShowcaseAndSpriteArtifacts();
            animals.RemoveAll(a => a == null || !a || !a.gameObject);
            AnimalCareController[] accs = FindObjectsByType<AnimalCareController>(FindObjectsSortMode.None);
            foreach (var a in accs)
            {
                if (a != null)
                {
                    AnimalScaleConfig.ApplyScale(a.gameObject, a.animalType);
                    GroundSnapUtility.SnapToGroundExact(a.gameObject);
                    if (!animals.Contains(a))
                    {
                        animals.Add(a);
                    }
                }
            }
        }

        void Update()
        {
            // Debug shortcut: Press F to fast-forward all animal timers
            if (Application.isEditor && Input.GetKeyDown(KeyCode.F))
            {
                FastForwardAllAnimals();
            }
        }

        public void FastForwardAllAnimals()
        {
            int count = 0;
            foreach (var a in animals)
            {
                if (a != null && !a.isProductReady)
                {
                    a.InstantProduce();
                    count++;
                }
            }
            Debug.Log($"[AnimalManager] Fast-forwarded {count} animals to ready-for-harvest!");
        }

        public void InitializeAnimals()
        {
            _hasInitialized = true;
            PurgeShowcaseAndSpriteArtifacts();

            if (SaveLoadSystem.HasSave())
            {
                SyncExistingAnimalsList();
                return;
            }
            animals.Clear();

            // 1. Setup Cow Pen (Max 3 Cows)
            bool cowPenPurchased = FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased("cow_barn");
            GameObject cowPen = GameObject.Find("CowPen_Small") ?? GameObject.Find("CowPen_Large") ?? GameObject.Find("Purchased_CowBarn") ?? GameObject.Find("CowPenPrefab");
            FarmBuilding cowBarn = cowPen != null ? cowPen.GetComponent<FarmBuilding>() : (FarmBuildingManager.Instance != null ? FarmBuildingManager.Instance.GetBuilding("cow_barn") : null);
            AnimalPenBoundary cowPenBoundary = cowPen != null ? (cowPen.GetComponent<AnimalPenBoundary>() ?? cowPen.GetComponentInChildren<AnimalPenBoundary>(true)) : null;

            int cowCount = 0;
            const int maxCows = 3;
            CowController[] sceneCows = FindObjectsByType<CowController>(FindObjectsSortMode.None);
            foreach (var cow in sceneCows)
            {
                if (cow == null) continue;

                // If Cow Pen is not purchased, do not keep roaming stray cows
                if (!cowPenPurchased)
                {
                    Debug.Log($"[AnimalManager] Cow pen not built yet. Despawning stray cow '{cow.name}'.");
                    Destroy(cow.gameObject);
                    continue;
                }

                // If already reached max 3 cows, despawn excess
                if (cowCount >= maxCows)
                {
                    Debug.Log($"[AnimalManager] Cow pen reached max capacity ({maxCows}). Despawning excess cow '{cow.name}'.");
                    Destroy(cow.gameObject);
                    continue;
                }

                // Parent under CowPen
                if (cowPen != null && cow.transform.parent != cowPen.transform)
                {
                    cow.transform.SetParent(cowPen.transform, true);
                }

                AnimalScaleConfig.ApplyScale(cow.gameObject, AnimalType.Cow);
                GroundSnapUtility.SnapToGroundExact(cow.gameObject);

                // Link Pen Boundary
                if (cowPenBoundary != null)
                {
                    cow.penBoundary = cowPenBoundary;
                    cowPenBoundary.RegisterAnimal(cow.gameObject);
                    cow.transform.position = cowPenBoundary.ClampPosition(cow.transform.position, 0.65f);
                }

                // Ensure Collider
                if (cow.GetComponent<Collider>() == null)
                {
                    BoxCollider bc = cow.gameObject.AddComponent<BoxCollider>();
                    bc.size = new Vector3(1.2f, 1.4f, 2.2f);
                    bc.center = new Vector3(0f, 0.7f, 0f);
                }

                // Ensure AnimalCareController
                AnimalCareController acc = cow.GetComponent<AnimalCareController>();
                if (acc == null)
                {
                    acc = cow.gameObject.AddComponent<AnimalCareController>();
                    acc.animalType = AnimalType.Cow;
                    acc.productItemId = "milk";
                    acc.productName = "Sữa bò tươi (Milk)";
                    acc.productAmount = 2;
                    acc.productionDuration = 25f;
                    acc.hunger = 70f;
                    acc.happiness = 60f;
                }

                if (!animals.Contains(acc))
                {
                    animals.Add(acc);
                    cowCount++;
                    if (cowBarn != null && !cowBarn.occupants.Contains(acc))
                    {
                        cowBarn.occupants.Add(acc);
                    }
                }
            }
            if (cowBarn != null) cowBarn.RefreshVisuals();

            // 2. Setup or locate Chicken Coop (Max 5 Chickens)
            int chickenCount = SetupChickenCoop();

            // 3. Setup or locate Pig Pen (Max 4 Pigs)
            int pigCount = SetupPigPen();

            // 4. Setup or locate Horse Stable (Max 4 Horses)
            int horseCount = SetupHorseStable();

            // 5. Prune any stale destroyed references
            animals.RemoveAll(a => a == null || !a || !a.gameObject);

            Debug.Log($"[AnimalManager] Initialized {animals.Count} farm animals (Cows: {cowCount}/{maxCows}, Chickens: {chickenCount}/5, Pigs: {pigCount}/4, Horses: {horseCount}/4).");
        }

        private int SetupChickenCoop()
        {
            bool coopPurchased = FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased("chicken_coop");
            GameObject coop = GameObject.Find("ChickenCoop_Small") ?? GameObject.Find("ChickenCoop_Large") ?? GameObject.Find("Purchased_ChickenCoop") ?? GameObject.Find("ChickenCoop");
            FarmBuilding coopBuilding = coop != null ? coop.GetComponent<FarmBuilding>() : (FarmBuildingManager.Instance != null ? FarmBuildingManager.Instance.GetBuilding("chicken_coop") : null);
            AnimalPenBoundary coopBoundary = coop != null ? (coop.GetComponent<AnimalPenBoundary>() ?? coop.GetComponentInChildren<AnimalPenBoundary>(true)) : null;

            int count = 0;
            const int maxChickens = 3;

            // Find all chickens in scene
            List<GameObject> allChickenGos = new List<GameObject>();
            foreach (var cw in FindObjectsByType<ChickenWanderer>(FindObjectsSortMode.None))
            {
                if (cw != null && !allChickenGos.Contains(cw.gameObject)) allChickenGos.Add(cw.gameObject);
            }
            foreach (var acc in FindObjectsByType<AnimalCareController>(FindObjectsSortMode.None))
            {
                if (acc != null && acc.animalType == AnimalType.Chicken && !allChickenGos.Contains(acc.gameObject))
                {
                    allChickenGos.Add(acc.gameObject);
                }
            }

            foreach (var chGo in allChickenGos)
            {
                if (chGo == null) continue;

                if (!coopPurchased || coop == null)
                {
                    Debug.Log($"[AnimalManager] Chicken coop not built yet. Despawning stray chicken '{chGo.name}'.");
                    Destroy(chGo);
                    continue;
                }

                if (count >= maxChickens)
                {
                    Debug.Log($"[AnimalManager] Chicken coop reached max capacity ({maxChickens}). Despawning excess chicken '{chGo.name}'.");
                    Destroy(chGo);
                    continue;
                }

                // Parent under coop
                if (chGo.transform.parent != coop.transform)
                {
                    chGo.transform.SetParent(coop.transform, true);
                }

                AnimalScaleConfig.ApplyScale(chGo, AnimalType.Chicken);
                GroundSnapUtility.SnapToGroundExact(chGo);

                if (coopBoundary != null)
                {
                    ChickenWanderer cw = chGo.GetComponent<ChickenWanderer>();
                    if (cw != null) cw.penBoundary = coopBoundary;
                    coopBoundary.RegisterAnimal(chGo);
                    chGo.transform.position = coopBoundary.ClampPosition(chGo.transform.position, 0.55f);
                }

                AnimalCareController acc = chGo.GetComponent<AnimalCareController>();
                if (acc == null)
                {
                    acc = chGo.AddComponent<AnimalCareController>();
                    acc.animalType = AnimalType.Chicken;
                    acc.productItemId = "egg";
                    acc.productName = "Trứng gà tươi (Egg)";
                    acc.productAmount = 3;
                    acc.productionDuration = 18f;
                    acc.hunger = 70f;
                    acc.happiness = 65f;
                }

                if (!animals.Contains(acc))
                {
                    animals.Add(acc);
                    count++;
                    if (coopBuilding != null && !coopBuilding.occupants.Contains(acc))
                    {
                        coopBuilding.occupants.Add(acc);
                    }
                }
            }

            if (coopBuilding != null) coopBuilding.RefreshVisuals();
            return count;
        }

        private int SetupPigPen()
        {
            bool penPurchased = FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased("pig_pen");
            GameObject pen = GameObject.Find("PigPen_Small") ?? GameObject.Find("PigPen_Large") ?? GameObject.Find("Purchased_PigPen") ?? GameObject.Find("PigPen");
            FarmBuilding penBuilding = pen != null ? pen.GetComponent<FarmBuilding>() : (FarmBuildingManager.Instance != null ? FarmBuildingManager.Instance.GetBuilding("pig_pen") : null);
            AnimalPenBoundary penBoundary = pen != null ? (pen.GetComponent<AnimalPenBoundary>() ?? pen.GetComponentInChildren<AnimalPenBoundary>(true)) : null;

            int count = 0;
            const int maxPigs = 3;

            // Find all pigs in scene
            List<GameObject> allPigGos = new List<GameObject>();
            foreach (var pw in FindObjectsByType<PigWanderer>(FindObjectsSortMode.None))
            {
                if (pw != null && !allPigGos.Contains(pw.gameObject)) allPigGos.Add(pw.gameObject);
            }
            foreach (var acc in FindObjectsByType<AnimalCareController>(FindObjectsSortMode.None))
            {
                if (acc != null && acc.animalType == AnimalType.Pig && !allPigGos.Contains(acc.gameObject))
                {
                    allPigGos.Add(acc.gameObject);
                }
            }

            foreach (var pGo in allPigGos)
            {
                if (pGo == null) continue;

                if (!penPurchased || pen == null)
                {
                    Debug.Log($"[AnimalManager] Pig pen not built yet. Despawning stray pig '{pGo.name}'.");
                    Destroy(pGo);
                    continue;
                }

                if (count >= maxPigs)
                {
                    Debug.Log($"[AnimalManager] Pig pen reached max capacity ({maxPigs}). Despawning excess pig '{pGo.name}'.");
                    Destroy(pGo);
                    continue;
                }

                // Parent under pen
                if (pGo.transform.parent != pen.transform)
                {
                    pGo.transform.SetParent(pen.transform, true);
                }

                AnimalScaleConfig.ApplyScale(pGo, AnimalType.Pig);
                GroundSnapUtility.SnapToGroundExact(pGo);

                if (penBoundary != null)
                {
                    PigWanderer pw = pGo.GetComponent<PigWanderer>();
                    if (pw != null) pw.penBoundary = penBoundary;
                    penBoundary.RegisterAnimal(pGo);
                    pGo.transform.position = penBoundary.ClampPosition(pGo.transform.position, 0.75f);
                }

                AnimalCareController acc = pGo.GetComponent<AnimalCareController>();
                if (acc == null)
                {
                    acc = pGo.AddComponent<AnimalCareController>();
                    acc.animalType = AnimalType.Pig;
                    acc.productItemId = "bacon";
                    acc.productName = "Thịt xông khói (Bacon)";
                    acc.productAmount = 2;
                    acc.productionDuration = 30f;
                    acc.hunger = 70f;
                    acc.happiness = 65f;
                }

                if (!animals.Contains(acc))
                {
                    animals.Add(acc);
                    count++;
                    if (penBuilding != null && !penBuilding.occupants.Contains(acc))
                    {
                        penBuilding.occupants.Add(acc);
                    }
                }
            }

            if (penBuilding != null) penBuilding.RefreshVisuals();
            return count;
        }

        private int SetupHorseStable()
        {
            bool stablePurchased = FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased("horse_stable");
            GameObject stable = GameObject.Find("HorseStable_Small") ?? GameObject.Find("HorseStablePrefab") ?? GameObject.Find("Purchased_HorseStable") ?? GameObject.Find("HorseStable");
            FarmBuilding stableBuilding = stable != null ? stable.GetComponent<FarmBuilding>() : (FarmBuildingManager.Instance != null ? FarmBuildingManager.Instance.GetBuilding("horse_stable") : null);
            AnimalPenBoundary stableBoundary = stable != null ? (stable.GetComponent<AnimalPenBoundary>() ?? stable.GetComponentInChildren<AnimalPenBoundary>(true)) : null;

            int count = 0;
            const int maxHorses = 3;

            // Find all horses in scene
            List<GameObject> allHorseGos = new List<GameObject>();
            foreach (var hw in FindObjectsByType<HorseWanderer>(FindObjectsSortMode.None))
            {
                if (hw != null && !allHorseGos.Contains(hw.gameObject)) allHorseGos.Add(hw.gameObject);
            }
            foreach (var acc in FindObjectsByType<AnimalCareController>(FindObjectsSortMode.None))
            {
                if (acc != null && acc.animalType == AnimalType.Horse && !allHorseGos.Contains(acc.gameObject))
                {
                    allHorseGos.Add(acc.gameObject);
                }
            }

            foreach (var hGo in allHorseGos)
            {
                if (hGo == null) continue;

                if (!stablePurchased || stable == null)
                {
                    Debug.Log($"[AnimalManager] Horse stable not built yet. Despawning stray horse '{hGo.name}'.");
                    Destroy(hGo);
                    continue;
                }

                if (count >= maxHorses)
                {
                    Debug.Log($"[AnimalManager] Horse stable reached max capacity ({maxHorses}). Despawning excess horse '{hGo.name}'.");
                    Destroy(hGo);
                    continue;
                }

                // Parent under stable
                if (hGo.transform.parent != stable.transform)
                {
                    hGo.transform.SetParent(stable.transform, true);
                }

                AnimalScaleConfig.ApplyScale(hGo, AnimalType.Horse);
                GroundSnapUtility.SnapToGroundExact(hGo);

                if (stableBoundary != null)
                {
                    HorseWanderer hw = hGo.GetComponent<HorseWanderer>();
                    if (hw != null) hw.penBoundary = stableBoundary;
                    stableBoundary.RegisterAnimal(hGo);
                    hGo.transform.position = stableBoundary.ClampPosition(hGo.transform.position, 1.05f);
                }

                AnimalCareController acc = hGo.GetComponent<AnimalCareController>();
                if (acc == null)
                {
                    acc = hGo.AddComponent<AnimalCareController>();
                    acc.animalType = AnimalType.Horse;
                    acc.productItemId = "leather";
                    acc.productName = "Da thuộc (Leather)";
                    acc.productAmount = 1;
                    acc.productionDuration = 35f;
                    acc.hunger = 70f;
                    acc.happiness = 70f;
                }

                if (!animals.Contains(acc))
                {
                    animals.Add(acc);
                    count++;
                    if (stableBuilding != null && !stableBuilding.occupants.Contains(acc))
                    {
                        stableBuilding.occupants.Add(acc);
                    }
                }
            }

            if (stableBuilding != null) stableBuilding.RefreshVisuals();
            return count;
        }
    }
}
