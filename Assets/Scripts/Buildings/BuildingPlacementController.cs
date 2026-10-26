using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IrishFarmSim
{
    public enum PlacementCategory
    {
        Building,
        Machine,
        Storage,
        Plot
    }

    /// <summary>
    /// Core Placement Mode controller for placing Farm Buildings, Machines, and Storage.
    /// Handles camera raycasting to terrain, ghost preview following, visual validity checking,
    /// touch/mouse confirmation, and cancellation.
    /// </summary>
    public class BuildingPlacementController : MonoBehaviour
    {
        public static BuildingPlacementController Instance { get; private set; }

        [Header("State")]
        public bool isPlacing = false;
        public bool IsPlacing => isPlacing;

        private PlacementCategory currentCategory;
        private string currentItemId;
        private string currentDisplayName;
        private int currentPrice;
        private GameObject currentPrefab;
        private Vector3 currentFootprint;

        private BuildingPreview activePreview;
        public BuildingPreview currentPreview => activePreview;
        public FarmBuilding selectedBuilding = null;
        private Camera targetCamera;
        private float previewBottomOffset = 0f;
        private float currentRotationY = 0f;

        [Header("UI Overlay")]
        private GameObject placementOverlayCanvas;
        private Text textTitle;
        private Text textInstruction;
        private Button btnRotateLeft;
        private Button btnRotateRight;
        private Button btnConfirm;
        private Button btnCancel;

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        public void ClearPlacementPreview()
        {
            if (activePreview != null)
            {
                if (activePreview.gameObject != null)
                {
                    activePreview.gameObject.SetActive(false);
                    SafeDestroy(activePreview.gameObject);
                }
                activePreview = null;
            }

            // Clean up any lingering ghost objects by name or layer (both active and inactive)
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in allTransforms)
            {
                if (t != null && t.gameObject != null && t.gameObject.scene.isLoaded)
                {
                    if (t.name.StartsWith("PlacementGhost") || t.gameObject.layer == LayerMask.NameToLayer("PlacementGhost"))
                    {
                        t.gameObject.SetActive(false);
                        SafeDestroy(t.gameObject);
                    }
                }
            }
        }

        public void ClearBuildingHighlight()
        {
            if (selectedBuilding != null)
            {
                selectedBuilding.ClearHighlight();
                selectedBuilding = null;
            }

            var allBuildings = FindObjectsByType<FarmBuilding>(FindObjectsSortMode.None);
            foreach (var b in allBuildings)
            {
                if (b != null)
                {
                    b.ClearHighlight();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static BuildingPlacementController EnsureInstance()
        {
            if (Instance != null) return Instance;

            BuildingPlacementController found = FindAnyObjectByType<BuildingPlacementController>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("BuildingPlacementController");
            Instance = go.AddComponent<BuildingPlacementController>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }

            CreatePlacementUI();
        }

        void Start()
        {
            targetCamera = Camera.main;
        }

        private void CreatePlacementUI()
        {
            if (placementOverlayCanvas != null) return;

            placementOverlayCanvas = new GameObject("PlacementOverlayCanvas");
            placementOverlayCanvas.transform.SetParent(transform, false);

            Canvas canvas = placementOverlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 98; // High priority above HUD

            CanvasScaler scaler = placementOverlayCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup cg = placementOverlayCanvas.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;

            placementOverlayCanvas.AddComponent<GraphicRaycaster>();

            // Top Banner Panel
            GameObject bannerGo = new GameObject("TopBanner");
            bannerGo.transform.SetParent(placementOverlayCanvas.transform, false);
            RectTransform brt = bannerGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -20f);
            brt.sizeDelta = new Vector2(750f, 95f);

            Image bImg = bannerGo.AddComponent<Image>();
            bImg.color = new Color(0.1f, 0.14f, 0.18f, 0.92f);

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(bannerGo.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = Vector2.zero;

            textTitle = titleGo.AddComponent<Text>();
            textTitle.text = "CHẾ ĐỘ ĐẶT CÔNG TRÌNH";
            textTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textTitle.fontSize = 22;
            textTitle.fontStyle = FontStyle.Bold;
            textTitle.alignment = TextAnchor.MiddleCenter;
            textTitle.color = new Color(1f, 0.85f, 0.3f);

            // Instruction
            GameObject instGo = new GameObject("Instruction");
            instGo.transform.SetParent(bannerGo.transform, false);
            RectTransform irt = instGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(1f, 0.55f);
            irt.sizeDelta = Vector2.zero;

            textInstruction = instGo.AddComponent<Text>();
            textInstruction.text = "[Chuột trái / Nút ĐẶT NGAY]: Đặt vật thể  |  [Phím R, Q/E / Nút XOAY]: Xoay 45°  |  [Chuột phải / Nút HỦY]: Hủy bỏ";
            textInstruction.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textInstruction.fontSize = 15;
            textInstruction.alignment = TextAnchor.MiddleCenter;
            textInstruction.color = Color.white;

            // Bottom Buttons Panel
            GameObject bottomPanel = new GameObject("BottomButtons");
            bottomPanel.transform.SetParent(placementOverlayCanvas.transform, false);
            RectTransform prt = bottomPanel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0f);
            prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 40f);
            prt.sizeDelta = new Vector2(740f, 65f);

            // Rotate Left Button (XOAY TRÁI)
            btnRotateLeft = CreateOverlayButton(bottomPanel.transform, "⟲ XOAY TRÁI", new Vector2(-270f, 0f), new Vector2(160f, 55f), new Color(0.24f, 0.44f, 0.65f));
            btnRotateLeft.onClick.AddListener(() => RotatePreview(-45f));

            // Rotate Right Button (XOAY PHẢI)
            btnRotateRight = CreateOverlayButton(bottomPanel.transform, "⟳ XOAY PHẢI", new Vector2(-95f, 0f), new Vector2(160f, 55f), new Color(0.24f, 0.44f, 0.65f));
            btnRotateRight.onClick.AddListener(() => RotatePreview(45f));

            // Confirm Button (ĐẶT NGAY)
            btnConfirm = CreateOverlayButton(bottomPanel.transform, "ĐẶT NGAY", new Vector2(90f, 0f), new Vector2(170f, 55f), new Color(0.2f, 0.7f, 0.3f));
            btnConfirm.onClick.AddListener(ConfirmPlacement);

            // Cancel Button (HỦY BỎ)
            btnCancel = CreateOverlayButton(bottomPanel.transform, "HỦY BỎ", new Vector2(250f, 0f), new Vector2(130f, 55f), new Color(0.85f, 0.25f, 0.2f));
            btnCancel.onClick.AddListener(CancelPlacement);

            placementOverlayCanvas.SetActive(false);
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        private Button CreateOverlayButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
        {
            GameObject bGo = new GameObject("Btn_" + label);
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = bGo.AddComponent<Image>();
            img.color = color;

            Button btn = bGo.AddComponent<Button>();

            GameObject tGo = new GameObject("Text");
            tGo.transform.SetParent(bGo.transform, false);
            RectTransform trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = tGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        public void StartPlacement(PlacementCategory category, string itemId, string displayName, int price, Vector3 footprint, GameObject prefab)
        {
            if (itemId == "machine_shed")
            {
                CropSelectionUI.ShowToast("Công trình Xưởng chế biến đã bị loại bỏ.");
                return;
            }

            if (isPlacing) CancelPlacement();

            currentCategory = category;
            currentItemId = itemId;
            currentDisplayName = displayName;
            currentPrice = price;
            currentPrefab = prefab;
            currentFootprint = footprint;
            currentRotationY = 0f;
            targetCamera = Camera.main;

            // Instantiate ghost preview
            GameObject ghostGo = null;
            if (prefab != null)
            {
                try { ghostGo = Instantiate(prefab); }
                catch { ghostGo = null; }
            }

            if (ghostGo == null)
            {
                ghostGo = new GameObject("PlacementGhost_" + itemId);
            }
            ghostGo.name = "PlacementGhost_" + itemId;
            ghostGo.transform.localScale = (category == PlacementCategory.Machine) ? (Vector3.one * 1.2f) : Vector3.one;

            if (category == PlacementCategory.Plot)
            {
                GameObject soilGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                soilGo.name = "Soil";
                soilGo.transform.SetParent(ghostGo.transform, false);
                soilGo.transform.localPosition = new Vector3(0f, 0.07f, 0f);
                soilGo.transform.localScale = new Vector3(2.55f, 0.14f, 2.55f);
                Collider sc = soilGo.GetComponent<Collider>();
                if (sc != null) SafeDestroy(sc);
                Renderer sr = soilGo.GetComponent<Renderer>();
                if (sr != null)
                {
                    sr.sharedMaterial = FarmVisualFactory.GetSharedMaterial("PlotSoilMoist", new Color(0.32f, 0.21f, 0.12f), 0.16f);
                }
            }

            // Strip interactive and billboard scripts from ghost
            foreach (var fbComp in ghostGo.GetComponentsInChildren<FarmBuilding>(true)) SafeDestroy(fbComp);
            foreach (var pmComp in ghostGo.GetComponentsInChildren<ProductionMachine>(true)) SafeDestroy(pmComp);
            foreach (var apb in ghostGo.GetComponentsInChildren<AnimalPenBoundary>(true)) SafeDestroy(apb);
            foreach (var acc in ghostGo.GetComponentsInChildren<AnimalCareController>(true)) SafeDestroy(acc);
            foreach (var cc in ghostGo.GetComponentsInChildren<CowController>(true)) SafeDestroy(cc);
            foreach (var cw in ghostGo.GetComponentsInChildren<ChickenWanderer>(true)) SafeDestroy(cw);
            foreach (var pw in ghostGo.GetComponentsInChildren<PigWanderer>(true)) SafeDestroy(pw);
            foreach (var hw in ghostGo.GetComponentsInChildren<HorseWanderer>(true)) SafeDestroy(hw);
            foreach (var mwa in ghostGo.GetComponentsInChildren<MachineWorkAnimator>(true)) SafeDestroy(mwa);
            foreach (var pet in ghostGo.GetComponentsInChildren<PetWanderer>(true)) SafeDestroy(pet);

            Transform bb = ghostGo.transform.Find("Billboard");
            if (bb != null) SafeDestroy(bb.gameObject);
            Transform fx = ghostGo.transform.Find("WorkFx");
            if (fx != null) SafeDestroy(fx.gameObject);

            // Phase 3: Set layer to PlacementGhost (or Ignore Raycast fallback)
            int ghostLayer = LayerMask.NameToLayer("PlacementGhost");
            if (ghostLayer < 0) ghostLayer = 2; // Ignore Raycast
            SetLayerRecursively(ghostGo, ghostLayer);

            // Phase 3: Disable and destroy all Colliders and Rigidbodies on ghost
            foreach (var c in ghostGo.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
                SafeDestroy(c);
            }
            foreach (var rb in ghostGo.GetComponentsInChildren<Rigidbody>(true))
            {
                SafeDestroy(rb);
            }

            // Check for obsolete oversized models or model mismatch on ghost
            bool isGhostOversized = false;
            foreach (var r in ghostGo.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.gameObject.name != "Billboard")
                {
                    if (r.bounds.size.x > 15f || r.bounds.size.z > 15f || r.transform.localScale.x > 15f || r.transform.localScale.z > 15f)
                    {
                        isGhostOversized = true;
                        break;
                    }
                }
            }

            bool isGhostMismatched = false;
            if (itemId == "chicken_coop" && (ghostGo.transform.Find("ProceduralModel/FeedingTrough") != null || ghostGo.transform.Find("ProceduralModel/MudWallow") != null || ghostGo.transform.Find("ProceduralModel/HorseStallShed") != null || ghostGo.transform.Find("ProceduralModel/WorkshopBody") != null)) isGhostMismatched = true;
            if (itemId == "cow_barn" && (ghostGo.transform.Find("ProceduralModel/NestingRoost") != null || ghostGo.transform.Find("ProceduralModel/MudWallow") != null || ghostGo.transform.Find("ProceduralModel/HorseStallShed") != null || ghostGo.transform.Find("ProceduralModel/WorkshopBody") != null)) isGhostMismatched = true;
            if (itemId == "pig_pen" && (ghostGo.transform.Find("ProceduralModel/FeedingTrough") != null || ghostGo.transform.Find("ProceduralModel/NestingRoost") != null || ghostGo.transform.Find("ProceduralModel/HorseStallShed") != null || ghostGo.transform.Find("ProceduralModel/WorkshopBody") != null)) isGhostMismatched = true;
            if (itemId == "horse_stable" && (ghostGo.transform.Find("ProceduralModel/FeedingTrough") != null || ghostGo.transform.Find("ProceduralModel/NestingRoost") != null || ghostGo.transform.Find("ProceduralModel/MudWallow") != null || ghostGo.transform.Find("ProceduralModel/WorkshopBody") != null)) isGhostMismatched = true;
            if (itemId == "machine_shed" && (ghostGo.transform.Find("ProceduralModel/FeedingTrough") != null || ghostGo.transform.Find("ProceduralModel/PenBedding") != null || ghostGo.transform.Find("ProceduralModel/NestingRoost") != null || ghostGo.transform.Find("ProceduralModel/MudWallow") != null || ghostGo.transform.Find("ProceduralModel/HorseStallShed") != null || ghostGo.transform.Find("CowPenBounds") != null)) isGhostMismatched = true;
            if (itemId == "storage" && (ghostGo.transform.Find("ProceduralModel/FeedingTrough") != null || ghostGo.transform.Find("ProceduralModel/PenBedding") != null || ghostGo.transform.Find("CowPenBounds") != null)) isGhostMismatched = true;

            if (isGhostOversized || isGhostMismatched)
            {
                for (int i = ghostGo.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = ghostGo.transform.GetChild(i);
                    if (child.name != "Billboard" && child.name != "AnimalsContainer")
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
            }

            // Ensure preview has visible 3D geometry
            if (ghostGo.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                if (category == PlacementCategory.Machine)
                {
                    MachineManager.CreateProceduralMachine(ghostGo, itemId, displayName);
                }
                else
                {
                    FarmBuilding.CreateProceduralBuilding(ghostGo, itemId, displayName);
                }

                // Re-apply ghost layer and remove colliders from newly generated procedural model
                SetLayerRecursively(ghostGo, ghostLayer);
                foreach (var c in ghostGo.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                    SafeDestroy(c);
                }
            }

            // Phase 3: Calculate vertical offset so ghost sits flush on ground without repeating bounds calc each frame
            previewBottomOffset = GroundSnapUtility.GetRendererBottomOffset(ghostGo);

            // Set initial position in front of camera / player on Terrain (NOT at 0,0,0)
            Vector3 initialPos = Vector3.zero;
            if (Camera.main != null)
            {
                Ray camRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                int mask = ~((1 << 2) | (1 << 5) | (ghostLayer >= 0 ? (1 << ghostLayer) : 0));
                if (Physics.Raycast(camRay, out RaycastHit initHit, 500f, mask))
                {
                    initialPos = initHit.point;
                }
            }
            if (initialPos == Vector3.zero)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) initialPos = player.transform.position + player.transform.forward * 8f;
                else initialPos = new Vector3(85f, 10f, 240f);
            }
            if (Terrain.activeTerrain != null)
            {
                initialPos.y = Terrain.activeTerrain.SampleHeight(initialPos) + Terrain.activeTerrain.transform.position.y;
            }
            initialPos.y += previewBottomOffset;
            ghostGo.transform.position = initialPos;

            activePreview = ghostGo.AddComponent<BuildingPreview>();
            activePreview.Initialize(footprint);
            activePreview.CheckPlacementValidity(ghostGo.transform.position);

            isPlacing = true;

            // Enable overlay UI
            if (placementOverlayCanvas != null)
            {
                placementOverlayCanvas.SetActive(true);
                CanvasGroup cg = placementOverlayCanvas.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                }
                textTitle.text = $"CHẾ ĐỘ ĐẶT: {displayName.ToUpper()}";
                textInstruction.text = $"[Chuột trái / Nút ĐẶT NGAY]: Đặt công trình  |  [Chuột phải / Nút HỦY]: Hủy bỏ  (Giá: {price} G)";
            }

            CropSelectionUI.ShowToast($"Bắt đầu đặt {displayName}! Di chuyển chuột/chạm để chọn vị trí.");
        }

        public static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
            }
        }

        public void RotatePreview(float delta)
        {
            if (!isPlacing || activePreview == null) return;
            currentRotationY = (currentRotationY + delta) % 360f;
            if (currentRotationY < 0f) currentRotationY += 360f;

            Quaternion rot = Quaternion.Euler(0f, currentRotationY, 0f);
            activePreview.transform.rotation = rot;

            float groundedY = GroundSnapUtility.CalculateGroundedPivotY(activePreview.gameObject, activePreview.transform.position, rot);
            activePreview.transform.position = new Vector3(activePreview.transform.position.x, groundedY, activePreview.transform.position.z);
            activePreview.CheckPlacementValidity(activePreview.transform.position);
        }

        void Update()
        {
            if (!isPlacing || activePreview == null) return;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            // Rotation key shortcuts: R or E to rotate right (+45°), Q to rotate left (-45°)
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.E))
            {
                RotatePreview(45f);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                RotatePreview(-45f);
            }

            // Mouse wheel scroll to rotate
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.05f)
            {
                RotatePreview(scroll > 0 ? 45f : -45f);
            }

            // Raycast from cursor down to Terrain, strictly excluding PlacementGhost layer
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            int ghostLayer = LayerMask.NameToLayer("PlacementGhost");
            int layerMask = ~((1 << 2) | (1 << 5) | (ghostLayer >= 0 ? (1 << ghostLayer) : 0));

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, layerMask);
            RaycastHit? groundHit = null;

            // Sort by hit distance
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (activePreview != null && (h.transform.root == activePreview.transform.root || h.transform.IsChildOf(activePreview.transform)))
                {
                    continue; // Skip preview itself
                }

                if (h.collider is TerrainCollider ||
                    h.collider.CompareTag("Terrain") ||
                    (h.collider.gameObject != null && h.collider.gameObject.name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    h.normal.y > 0.5f)
                {
                    groundHit = h;
                    break;
                }
            }

            if (groundHit.HasValue)
            {
                Vector3 hitPoint = groundHit.Value.point;
                Quaternion rot = Quaternion.Euler(0f, currentRotationY, 0f);
                activePreview.transform.rotation = rot;

                float groundedY = GroundSnapUtility.CalculateGroundedPivotY(activePreview.gameObject, hitPoint, rot);
                Vector3 targetPos = new Vector3(hitPoint.x, groundedY, hitPoint.z);
                activePreview.transform.position = targetPos;

                bool valid = activePreview.CheckPlacementValidity(targetPos);
                if (btnConfirm != null) btnConfirm.interactable = valid;
            }

            // 2. Cancellation: Right-click or Escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
                return;
            }

            // 3. Confirmation: Left-click on valid ground
            if (Input.GetMouseButtonDown(0))
            {
                // Prevent placing if clicking UI buttons
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (activePreview.isValidPosition)
                {
                    ConfirmPlacement();
                }
                else
                {
                    CropSelectionUI.ShowToast("Vị trí không hợp lệ! Hãy di chuyển đến bãi đất trống phẳng.");
                }
            }
        }

        public void ConfirmPlacement()
        {
            if (!isPlacing || activePreview == null) return;

            if (!activePreview.isValidPosition)
            {
                CropSelectionUI.ShowToast("Vị trí không hợp lệ! Không thể đặt công trình tại đây.");
                return;
            }

            // 1. Check Gold & Spend
            if (PlayerProgression.Instance == null || !PlayerProgression.TrySpendGold(currentPrice))
            {
                CropSelectionUI.ShowToast("Không đủ Gold để hoàn tất xây dựng!");
                CancelPlacement();
                return;
            }

            Vector3 finalPos = activePreview != null ? activePreview.transform.position : Vector3.zero;
            Quaternion finalRot = activePreview != null ? activePreview.transform.rotation : Quaternion.identity;

            // 2. Immediately deactivate and destroy Preview Ghost and clear any selection highlights
            ClearPlacementPreview();
            ClearBuildingHighlight();

            isPlacing = false;
            selectedBuilding = null;

            if (placementOverlayCanvas != null)
            {
                placementOverlayCanvas.SetActive(false);
                CanvasGroup cg = placementOverlayCanvas.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
            }

            // 3. Spawn real permanent entity
            GameObject spawnedGo = null;
            if (currentCategory == PlacementCategory.Building || currentCategory == PlacementCategory.Storage)
            {
                if (FarmBuildingManager.Instance != null)
                {
                    FarmBuilding fb = FarmBuildingManager.Instance.SpawnPurchasedBuildingAtPosition(currentItemId, finalPos, finalRot);
                    if (fb != null) spawnedGo = fb.gameObject;
                }
            }
            else if (currentCategory == PlacementCategory.Machine)
            {
                if (MachineManager.Instance != null)
                {
                    ProductionMachine pm = MachineManager.Instance.SpawnPurchasedMachineAtPosition(currentItemId, finalPos, finalRot);
                    if (pm != null) spawnedGo = pm.gameObject;
                }
            }
            else if (currentCategory == PlacementCategory.Plot)
            {
                if (CropManager.Instance != null)
                {
                    CropPlot plot = CropManager.Instance.SpawnPlotAtPosition(currentItemId, finalPos, finalRot);
                    if (plot != null) spawnedGo = plot.gameObject;
                }
                if (PlayerProgression.Instance != null)
                {
                    PlayerProgression.Instance.UnlockPlot(currentItemId);
                }
                PlayerProgression.AddXP(15, $"Mở rộng {currentDisplayName}");
                CropSelectionUI.ShowToast($"Đã mở rộng thành công: {currentDisplayName}!\n+15 XP");
                SaveLoadSystem.SaveGame();
            }

            // Clean up any residual preview/highlight components, restore original materials, and snap to ground
            if (spawnedGo != null)
            {
                CleanUpBuildingVisuals(spawnedGo);
                GroundSnapUtility.SnapBuildingToGround(spawnedGo);

                Renderer[] rends = spawnedGo.GetComponentsInChildren<Renderer>(true);
                Collider[] cols = spawnedGo.GetComponentsInChildren<Collider>(true);
                FarmBuilding placedFb = spawnedGo.GetComponent<FarmBuilding>();
                string bType = placedFb != null ? placedFb.buildingType : (currentCategory == PlacementCategory.Machine ? "machine" : "building");

                Debug.Log($"[BuildingPlacementController] Placement Verification:\n" +
                          $"buildingType: {bType}\n" +
                          $"prefabName: {currentItemId}\n" +
                          $"instanceName: {spawnedGo.name}\n" +
                          $"activeSelf: {spawnedGo.activeSelf}\n" +
                          $"position: {spawnedGo.transform.position}\n" +
                          $"localScale: {spawnedGo.transform.localScale}\n" +
                          $"rendererCount: {rends.Length}\n" +
                          $"colliderCount: {cols.Length}");
            }

            // Award XP
            PlayerProgression.AddXP(30, $"Xây dựng {currentDisplayName}");
            CropSelectionUI.ShowToast($"Đã xây dựng thành công: {currentDisplayName}!\n+30 XP");
            Debug.Log($"[BuildingPlacementController] Successfully placed {currentDisplayName} at {finalPos}.");

            if (ShopUI.Instance != null) ShopUI.Instance.RefreshDisplay();
            SaveLoadSystem.SaveGame();
        }

        public void CancelPlacement()
        {
            ClearPlacementPreview();
            ClearBuildingHighlight();

            isPlacing = false;
            selectedBuilding = null;

            if (placementOverlayCanvas != null)
            {
                placementOverlayCanvas.SetActive(false);
                CanvasGroup cg = placementOverlayCanvas.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
            }

            CropSelectionUI.ShowToast("Đã hủy bỏ chế độ đặt công trình.");
            Debug.Log("[BuildingPlacementController] Placement cancelled. No Gold deducted.");
        }

        private void CleanUpBuildingVisuals(GameObject go)
        {
            if (go == null) return;

            // Remove any BuildingPreview components
            var bps = go.GetComponentsInChildren<BuildingPreview>(true);
            foreach (var bp in bps) SafeDestroy(bp);

            // Remove any LineRenderer or Outline/Selection components if any
            var lrs = go.GetComponentsInChildren<LineRenderer>(true);
            foreach (var lr in lrs) SafeDestroy(lr);

            var allComps = go.GetComponentsInChildren<Component>(true);
            foreach (var c in allComps)
            {
                if (c == null) continue;
                string cName = c.GetType().Name.ToLower();
                if (cName.Contains("outline") || cName.Contains("selection") || cName.Contains("highlight"))
                {
                    SafeDestroy(c);
                }
            }

            // Ensure layer is Default (0) and not PlacementGhost
            SetLayerRecursively(go, 0);

            // Ensure normal building visuals and clean highlights
            FarmBuilding fb = go.GetComponent<FarmBuilding>();
            if (fb != null)
            {
                fb.EnforceTypeAndCapacity();
                fb.ClearHighlight();
                fb.RestoreOriginalMaterials();
                fb.EnsureVisualStructure();
                fb.RefreshVisuals();
            }
        }
    }
}
