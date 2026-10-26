using System;
using UnityEngine;

namespace IrishFarmSim
{
    public enum PlotState
    {
        Locked,
        Empty,
        Planted,
        Growing,
        ReadyToHarvest
    }

    /// <summary>
    /// Manages an individual farm plot in the scene. Handles state transitions,
    /// level unlocking, 5-stage 3D crop visuals (1. Seed, 2. Sprout, 3. Young, 4. Mature, 5. Harvest-ready),
    /// gentle wind sway, planting, instant maturity (debug F1), and harvesting.
    /// </summary>
    [SelectionBase]
    public class CropPlot : MonoBehaviour
    {
        public static event Action<string, int> OnCropHarvested;
        public static event Action<string> OnCropPlanted;

        public static bool ShowDebugCropLabels = false;

        [Header("Plot Status")]
        public PlotState state = PlotState.Empty;
        public CropData currentCrop;
        public float growthTimer = 0f;

        [Header("Unlock & Progression")]
        public string plotId = "";
        public int unlockLevel = 1;
        public int unlockCost = 0;
        public bool isUnlocked = true;
        [SerializeField] private GameObject lockBadgeGo;
        [SerializeField] private TextMesh lockBadgeText;

        [Header("Visual Components")]
        [SerializeField] private Transform soilTransform;
        [SerializeField] private Transform cropVisualContainer;
        [SerializeField] private Renderer soilRenderer;
        [SerializeField] private TextMesh statusLabel;

        private string builtCropId = "";
        private readonly GameObject[] stageModels = new GameObject[5];
        private int currentStageIndex = -1;

        public static readonly string[] StageDisplayNames =
        {
            "Seed",
            "Sprout",
            "Young",
            "Mature",
            "Harvest-ready"
        };

        public int CurrentStageIndex => currentStageIndex;
        public string CurrentStageName => (currentStageIndex >= 0 && currentStageIndex < StageDisplayNames.Length)
            ? StageDisplayNames[currentStageIndex]
            : "None";

        public float GrowthProgress
        {
            get
            {
                if (state == PlotState.ReadyToHarvest) return 1f;
                if (currentCrop == null || currentCrop.growthTime <= 0) return 0f;
                return Mathf.Clamp01(growthTimer / currentCrop.growthTime);
            }
        }

        void OnEnable()
        {
            PlayerProgression.OnLevelUp += OnPlayerLevelUp;
        }

        void OnDisable()
        {
            PlayerProgression.OnLevelUp -= OnPlayerLevelUp;
        }

        private void OnPlayerLevelUp(int newLevel)
        {
            if (state == PlotState.Locked || !isUnlocked)
            {
                UpdateVisuals();
            }
        }

        void Awake()
        {
            SetupVisualComponents();
            UpdateVisuals();
        }

        void Start()
        {
            GroundSnapUtility.SnapToGround(gameObject, false);
            SetupVisualComponents();
            UpdateVisuals();
        }

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.H))
            {
                ShowDebugCropLabels = !ShowDebugCropLabels;
                UpdateVisuals();
            }

            if ((state == PlotState.Growing || state == PlotState.Planted) && currentCrop != null)
            {
                if (state == PlotState.Planted) state = PlotState.Growing;

                float speedMultiplier = WeatherSystem.GrowthSpeedMultiplier;
                growthTimer += Time.deltaTime * speedMultiplier;
                UpdatePlantStageAndScale();

                if (growthTimer >= currentCrop.growthTime)
                {
                    growthTimer = currentCrop.growthTime;
                    state = PlotState.ReadyToHarvest;
                    UpdateVisuals();
                    Debug.Log($"[CropPlot] {gameObject.name}: Crop '{currentCrop.displayName}' reached Stage 5 (Harvest-ready)!");
                }
            }
        }

        void LateUpdate()
        {
            if (statusLabel != null && statusLabel.gameObject.activeSelf && Camera.main != null)
            {
                statusLabel.transform.rotation = Camera.main.transform.rotation;
            }
            if (lockBadgeGo != null && lockBadgeGo.activeSelf && Camera.main != null)
            {
                lockBadgeGo.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void OnMouseDown()
        {
            Interact();
        }

        /// <summary>
        /// Public entry point for Player click/interaction.
        /// </summary>
        public void Interact()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            // Handle locked plot interaction
            if (state == PlotState.Locked || !isUnlocked)
            {
                int playerLvl = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;
                if (playerLvl < unlockLevel)
                {
                    Debug.LogWarning($"[CropPlot] {gameObject.name}: Locked! Requires Level {unlockLevel}.");
                    CropSelectionUI.ShowToast($"Yêu cầu Cấp {unlockLevel} mới có thể mở khóa ô này!");
                    return;
                }

                if (unlockCost > 0)
                {
                    if (PlayerProgression.Instance == null || PlayerProgression.Instance.Gold < unlockCost)
                    {
                        CropSelectionUI.ShowToast($"Không đủ Vàng để mở khóa ({unlockCost} G)!");
                        return;
                    }

                    if (PlayerProgression.TrySpendGold(unlockCost))
                    {
                        UnlockThisPlot();
                        CropSelectionUI.ShowToast($"Đã mở khóa {gameObject.name} thành công!\n-{unlockCost} G");
                    }
                }
                else
                {
                    UnlockThisPlot();
                    CropSelectionUI.ShowToast($"Đã mở khóa {gameObject.name} thành công!");
                }
                return;
            }

            if (state == PlotState.ReadyToHarvest)
            {
                Harvest();
            }
            else if (state == PlotState.Empty || state == PlotState.Planted)
            {
                CropSelectionUI.OpenMenu(this);
            }
            else if (state == PlotState.Growing && currentCrop != null)
            {
                int percent = (int)(GrowthProgress * 100f);
                int secLeft = Mathf.CeilToInt(currentCrop.growthTime - growthTimer);
                Debug.Log($"[CropPlot] {gameObject.name}: '{currentCrop.displayName}' [{CurrentStageName}] đang lớn: {percent}% (còn {secLeft}s). Nhấn F1 để chín ngay.");
                if (CropPlotUI.Instance != null)
                {
                    CropPlotUI.ShowHarvestToast($"[{CurrentStageName}] {currentCrop.displayName}: {percent}% (còn {secLeft}s). Nhấn F1 để chín nhanh!");
                }
            }
        }

        public void UnlockThisPlot()
        {
            isUnlocked = true;
            state = PlotState.Empty;
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.UnlockPlot(gameObject.name);
            }
            UpdateVisuals();
            SaveLoadSystem.SaveGame();
        }

        public void OnPlotClicked() => Interact();

        /// <summary>
        /// Plants a crop on this plot given its string ID.
        /// </summary>
        public bool PlantCrop(string cropId)
        {
            if (state == PlotState.Locked || !isUnlocked)
            {
                Debug.LogWarning($"[CropPlot] {gameObject.name}: Cannot plant on locked plot (Level {unlockLevel})!");
                CropSelectionUI.ShowToast($"Ô đất đang khóa (Cấp {unlockLevel})!");
                return false;
            }

            if (state == PlotState.Growing || state == PlotState.ReadyToHarvest)
            {
                Debug.LogWarning($"[CropPlot] {gameObject.name}: Cannot plant on plot in state '{state}'!");
                return false;
            }

            CropData crop = CropData.GetCrop(cropId);
            if (crop == null)
            {
                Debug.LogError($"[CropPlot] Unknown crop ID: '{cropId}'");
                return false;
            }

            if (InventorySystem.Instance != null)
            {
                if (InventorySystem.Instance.GetItemCount(crop.seedItemId) <= 0)
                {
                    Debug.LogWarning($"[CropPlot] Cannot plant '{crop.displayName}': Not enough seeds ({crop.seedItemId} = 0).");
                    CropSelectionUI.ShowToast("Not enough seeds");
                    return false;
                }
                InventorySystem.Instance.RemoveItem(crop.seedItemId, 1);
            }

            currentCrop = crop;
            growthTimer = 0f;
            state = PlotState.Growing;

            SetupVisualComponents();
            EnsureCropStageModelsBuilt(crop.cropId);
            UpdateVisuals();

            PlayerProgression.AddXP(crop.plantXp, $"Trồng {crop.displayName}");
            Debug.Log($"[CropPlot] {gameObject.name}: Planted '{crop.displayName}'. Stage=1 (Seed). +{crop.plantXp} XP.");

            OnCropPlanted?.Invoke(crop.displayName);
            return true;
        }

        public bool Plant(CropData crop)
        {
            if (crop == null) return false;
            return PlantCrop(crop.cropId);
        }

        /// <summary>
        /// Instantly forces this crop to mature (ready to harvest).
        /// </summary>
        public void ForceMature()
        {
            if ((state == PlotState.Growing || state == PlotState.Planted) && currentCrop != null)
            {
                growthTimer = currentCrop.growthTime;
                state = PlotState.ReadyToHarvest;
                UpdateVisuals();
                Debug.Log($"[CropPlot] {gameObject.name}: ForceMature called. '{currentCrop.displayName}' is now ReadyToHarvest (Stage 5)!");
            }
        }

        public void InstantMature() => ForceMature();

        /// <summary>
        /// Harvests the mature crop from this plot.
        /// </summary>
        public bool Harvest()
        {
            if (state != PlotState.ReadyToHarvest || currentCrop == null)
            {
                Debug.LogWarning($"[CropPlot] {gameObject.name}: Cannot harvest. Plot not ReadyToHarvest!");
                return false;
            }

            if (InventorySystem.Instance != null && InventorySystem.Instance.IsFull(currentCrop.harvestAmount))
            {
                Debug.LogWarning("[CropPlot] Cannot harvest: Storage Full!");
                if (CropPlotUI.Instance != null)
                {
                    CropPlotUI.ShowHarvestToast("Storage Full! Không thể thu hoạch thêm.");
                }
                return false;
            }

            int amount = currentCrop.harvestAmount;
            int xp = currentCrop.harvestXp;
            int gold = currentCrop.harvestGold;
            string cName = currentCrop.displayName;
            string cropId = currentCrop.cropId;
            string harvestItemId = currentCrop.harvestItemId;

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItem(harvestItemId, amount);

                if (cropId.Equals("wheat", StringComparison.OrdinalIgnoreCase))
                {
                    InventorySystem.Instance.AddItem("Grain", amount);
                }
            }

            string shortName = harvestItemId;

            Debug.Log($"[CropPlot] Harvested {shortName} +{amount}");
            PlayerProgression.AddXP(xp, $"Thu hoạch {shortName}");
            PlayerProgression.AddGold(gold, $"Thu hoạch {shortName}");
            Debug.Log($"[Economy] +{gold} Gold");

            CropSelectionUI.ShowToast($"Harvested {shortName} +{amount}\n+{xp} XP\n+{gold} Gold");

            OnCropHarvested?.Invoke(shortName, xp);

            currentCrop = null;
            growthTimer = 0f;
            state = PlotState.Empty;
            currentStageIndex = -1;
            UpdateVisuals();

            Debug.Log($"[CropPlot] {gameObject.name} returned to state Empty. Ready for replanting.");
            return true;
        }

        public void SetupVisualComponents()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
            col.size = new Vector3(2.6f, 1.2f, 2.6f);
            col.center = new Vector3(0f, 0.6f, 0f);

            // Create or verify Soil visual (flush on ground, height 0.14m so top surface is at y = 0.14m)
            soilTransform = transform.Find("Soil");
            if (soilTransform == null)
            {
                GameObject soilGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                soilGo.name = "Soil";
                soilGo.transform.SetParent(transform, false);
                Collider sc = soilGo.GetComponent<Collider>();
                if (sc != null) SafeDestroy(sc);
                soilTransform = soilGo.transform;
            }
            soilTransform.localPosition = new Vector3(0f, 0.07f, 0f);
            soilTransform.localScale = new Vector3(2.55f, 0.14f, 2.55f);

            // Add subtle tilled soil furrows on top of Soil if not yet present
            if (soilTransform.Find("Furrow_0") == null)
            {
                Material furrowMat = FarmVisualFactory.GetSharedMaterial("PlotFurrow", new Color(0.34f, 0.23f, 0.14f), 0.08f);
                for (int f = 0; f < 4; f++)
                {
                    float z = Mathf.Lerp(-0.36f, 0.36f, f / 3f);
                    FarmVisualFactory.CreatePart(PrimitiveType.Cube, soilTransform, $"Furrow_{f}",
                        new Vector3(0f, 0.52f, z), new Vector3(0.90f, 0.12f, 0.09f), furrowMat, default, false);
                }
            }

            soilRenderer = soilTransform.GetComponent<Renderer>();
            if (soilRenderer != null)
            {
                soilRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilDry", new Color(0.46f, 0.33f, 0.20f), 0.08f);
            }

            // Create or verify Crops container flush on top of Soil (y = 0.14f)
            cropVisualContainer = transform.Find("Crops");
            if (cropVisualContainer == null)
            {
                GameObject cropsGo = new GameObject("Crops");
                cropsGo.transform.SetParent(transform, false);
                cropVisualContainer = cropsGo.transform;
            }
            cropVisualContainer.localPosition = new Vector3(0f, 0.14f, 0f);
            cropVisualContainer.localRotation = Quaternion.identity;
            cropVisualContainer.localScale = Vector3.one;

            // Remove any leftover BlobShadow discs on CropPlot
            Transform shadowOnPlot = transform.Find("BlobShadow");
            if (shadowOnPlot != null) SafeDestroy(shadowOnPlot.gameObject);
            Transform shadowInCrops = cropVisualContainer.Find("BlobShadow");
            if (shadowInCrops != null) SafeDestroy(shadowInCrops.gameObject);

            // Remove legacy Stalk cylinders if present
            Transform oldStalk = cropVisualContainer.Find("Stalk");
            if (oldStalk != null)
            {
                FarmVisualFactory.ClearVisualChildren(cropVisualContainer);
                builtCropId = "";
            }

            // Remove any leftover 2D CropSpriteRenderer child
            Transform spriteT = transform.Find("CropSpriteRenderer");
            if (spriteT != null)
            {
                SafeDestroy(spriteT.gameObject);
            }

            Transform labelT = transform.Find("StatusLabel");
            if (labelT == null)
            {
                GameObject labelGo = new GameObject("StatusLabel");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
                statusLabel = labelGo.AddComponent<TextMesh>();
                statusLabel.characterSize = 0.13f;
                statusLabel.fontSize = 28;
                statusLabel.alignment = TextAlignment.Center;
                statusLabel.anchor = TextAnchor.MiddleCenter;
                statusLabel.color = Color.white;
                statusLabel.font = FarmUITheme.BoldFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                MeshRenderer mr = labelGo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = statusLabel.font.material;
                labelGo.AddComponent<BillboardFaceCamera>();
                labelGo.SetActive(false);
            }
            else
            {
                labelT.localPosition = new Vector3(0f, 1.85f, 0f);
                statusLabel = labelT.GetComponent<TextMesh>();
            }

            // Remove legacy LockBadge if present (all unbought plots are purchased exclusively through Shop)
            Transform lockT = transform.Find("LockBadge");
            if (lockT != null)
            {
                SafeDestroy(lockT.gameObject);
                lockBadgeGo = null;
                lockBadgeText = null;
            }
        }

        private void EnsureCropStageModelsBuilt(string cropId)
        {
            if (cropVisualContainer == null) return;
            string normId = string.IsNullOrEmpty(cropId) ? "wheat" : cropId.Trim().ToLowerInvariant();
            if (normId == "grass" || normId == "feed") normId = "hay";

            // Check if already built for this cropId
            if (builtCropId == normId && stageModels[0] != null && stageModels[4] != null)
            {
                return;
            }

            // Also check if existing children already match Stage_0_Seed .. Stage_4_HarvestReady for the same crop
            if (builtCropId == normId)
            {
                bool allFound = true;
                for (int i = 0; i < 5; i++)
                {
                    Transform t = cropVisualContainer.Find(FarmVisualFactory.StageNames[i]);
                    if (t != null) stageModels[i] = t.gameObject;
                    else allFound = false;
                }
                if (allFound) return;
            }

            GameObject[] built = FarmVisualFactory.BuildCropAllStages(cropVisualContainer, normId);
            if (built != null && built.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    stageModels[i] = built[i];
                }
                builtCropId = normId;
                currentStageIndex = -1;
            }
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        public void UpdateVisuals()
        {
            SetupVisualComponents();

            int currentLevel = (PlayerProgression.Instance != null) ? PlayerProgression.Instance.Level : 1;
            if (state == PlotState.Locked || !isUnlocked)
            {
                if (cropVisualContainer != null) cropVisualContainer.gameObject.SetActive(false);
                if (statusLabel != null) statusLabel.gameObject.SetActive(false);
                currentStageIndex = -1;

                if (soilRenderer != null)
                {
                    soilRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilLocked", new Color(0.38f, 0.35f, 0.32f), 0.05f);
                }
                return;
            }

            if (state == PlotState.Empty || currentCrop == null)
            {
                if (cropVisualContainer != null) cropVisualContainer.gameObject.SetActive(false);
                currentStageIndex = -1;

                if (statusLabel != null)
                {
                    if (ShowDebugCropLabels)
                    {
                        statusLabel.gameObject.SetActive(true);
                        statusLabel.text = $"[{gameObject.name}]\nTrống: Chạm để trồng";
                        statusLabel.color = new Color(0.8f, 1.0f, 0.8f);
                    }
                    else
                    {
                        statusLabel.gameObject.SetActive(false);
                    }
                }
                if (soilRenderer != null)
                {
                    soilRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilDry", new Color(0.46f, 0.33f, 0.20f), 0.08f);
                }
            }
            else if (state == PlotState.Growing || state == PlotState.Planted)
            {
                EnsureCropStageModelsBuilt(currentCrop.cropId);
                if (cropVisualContainer != null)
                {
                    cropVisualContainer.gameObject.SetActive(true);
                }
                if (soilRenderer != null)
                {
                    // Moist dark soil when watered/growing
                    soilRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilMoist", new Color(0.32f, 0.21f, 0.12f), 0.16f);
                }
                UpdatePlantStageAndScale();
            }
            else if (state == PlotState.ReadyToHarvest)
            {
                EnsureCropStageModelsBuilt(currentCrop.cropId);
                if (cropVisualContainer != null)
                {
                    cropVisualContainer.gameObject.SetActive(true);
                }
                ActivateStageModel(4, 1f);

                if (statusLabel != null)
                {
                    statusLabel.gameObject.SetActive(true);
                    statusLabel.text = "★ Thu hoạch";
                    statusLabel.color = new Color(1.0f, 0.9f, 0.2f);
                }
                if (soilRenderer != null)
                {
                    soilRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilMoist", new Color(0.35f, 0.23f, 0.14f), 0.14f);
                }
            }
        }

        private void UpdatePlantStageAndScale()
        {
            if (cropVisualContainer == null || currentCrop == null) return;

            EnsureCropStageModelsBuilt(currentCrop.cropId);
            float p = GrowthProgress;
            int targetStage = FarmVisualFactory.GetCropStageIndex(p, state == PlotState.ReadyToHarvest);
            ActivateStageModel(targetStage, p);

            if (statusLabel != null)
            {
                if (ShowDebugCropLabels)
                {
                    statusLabel.gameObject.SetActive(true);
                    int secLeft = Mathf.CeilToInt(currentCrop.growthTime - growthTimer);
                    statusLabel.text = $"{currentCrop.displayName} [{CurrentStageName}]\n{(int)(p * 100)}% ({secLeft}s)";
                    statusLabel.color = Color.white;
                }
                else
                {
                    statusLabel.gameObject.SetActive(false);
                }
            }
        }

        private void ActivateStageModel(int stageIdx, float totalProgress)
        {
            stageIdx = Mathf.Clamp(stageIdx, 0, 4);
            currentStageIndex = stageIdx;

            for (int i = 0; i < 5; i++)
            {
                if (stageModels[i] == null) continue;
                bool active = (i == stageIdx);
                if (stageModels[i].activeSelf != active)
                {
                    stageModels[i].SetActive(active);
                }
                if (active)
                {
                    // Gentle progressive scale within the stage while keeping base flush at y=0
                    float subScale = (stageIdx == 0) ? 1f : Mathf.Lerp(0.92f, 1.04f, totalProgress);
                    CropScaleConfig.ApplyScale(stageModels[i].transform, currentCrop != null ? currentCrop.cropId : null, stageIdx, subScale);
                }
            }
        }
    }
}
