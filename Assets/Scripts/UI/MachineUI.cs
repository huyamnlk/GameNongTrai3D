using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// UI Dialog for operating Production Machines (Mill, Bakery, Dairy, Feed Mixer, Jam Maker).
    /// Features interactive recipe cards, real-time inventory checking with high-contrast color status,
    /// production progress tracking, Start/Collect/Cancel buttons, and machine resale.
    /// </summary>
    public class MachineUI : MonoBehaviour
    {
        public static MachineUI Instance { get; private set; }

        [Header("UI Panels")]
        public GameObject scrimGo;
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Text textTitle;
        public Text textRecipeInfo;
        public Text textIngredients;
        public Image imageSelectedProductIcon;
        public Text textInstructions;
        public Slider sliderProgress;
        public Text textProgress;
        public Button buttonStart;
        public Text textButtonStart;
        public Button buttonCollect;
        public Button buttonCancel;
        public Button buttonClose;
        public Button buttonSellMachine;
        public Button buttonPrevRecipe;
        public Button buttonNextRecipe;

        [Header("Recipe Cards Area")]
        public Transform recipeCardsContainer;
        private List<GameObject> activeRecipeCards = new List<GameObject>();

        [Header("Confirm Sell Modal")]
        private GameObject confirmSellModal;
        private Text confirmSellText;
        private Button btnConfirmSellYes;
        private Button btnConfirmSellNo;

        private ProductionMachine activeMachine;

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        public static MachineUI EnsureUI()
        {
            if (Instance != null) return Instance;

            MachineUI found = FindAnyObjectByType<MachineUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("MachineUICanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Instance = canvasGo.AddComponent<MachineUI>();
            Instance.BuildUIStructure(canvasGo);
            return Instance;
        }

        private void BuildUIStructure(GameObject canvasGo)
        {
            // Fullscreen backdrop scrim (clicking outside closes the window)
            scrimGo = new GameObject("Scrim");
            scrimGo.transform.SetParent(canvasGo.transform, false);
            RectTransform scrimRt = scrimGo.AddComponent<RectTransform>();
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.sizeDelta = Vector2.zero;
            Image sImg = scrimGo.AddComponent<Image>();
            sImg.color = new Color(0f, 0f, 0f, 0.65f);
            Button sBtn = scrimGo.AddComponent<Button>();
            sBtn.onClick.AddListener(Close);

            panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = panelRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(780f, 680f);

            Image bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.20f, 0.27f, 0.98f);
            Outline panelOutline = panelRoot.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.30f, 0.40f, 0.55f, 0.8f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            // Title Header
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelRoot.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -14f);
            trt.sizeDelta = new Vector2(-160f, 44f);

            textTitle = titleGo.AddComponent<Text>();
            textTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textTitle.fontSize = 24;
            textTitle.fontStyle = FontStyle.Bold;
            textTitle.alignment = TextAnchor.MiddleCenter;
            textTitle.color = new Color(1f, 0.85f, 0.3f);
            textTitle.raycastTarget = false;

            // Close 'X' Button anchored securely at top-right
            buttonClose = CreateSimpleButton(panelRoot.transform, "X", Vector2.zero, new Vector2(48f, 48f), new Color(0.85f, 0.24f, 0.24f));
            RectTransform cRt = buttonClose.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-12f, -12f);
            buttonClose.onClick.AddListener(Close);
            buttonClose.transform.SetAsLastSibling();

            // Recipe Section Header
            GameObject headerGo = new GameObject("RecipeSectionHeader");
            headerGo.transform.SetParent(panelRoot.transform, false);
            RectTransform hrt = headerGo.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -58f);
            hrt.sizeDelta = new Vector2(-40f, 26f);

            Text headerTxt = headerGo.AddComponent<Text>();
            headerTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerTxt.fontSize = 14;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleLeft;
            headerTxt.color = new Color(0.72f, 0.84f, 0.98f);
            headerTxt.text = "CÁC CÔNG THỨC SẢN XUẤT (CHẠM ĐỂ CHỌN):";

            // Recipe Cards Scroll View (supports horizontal scroll on mobile Android if multiple recipes)
            GameObject scrollAreaGo = new GameObject("RecipeCardsScrollView");
            scrollAreaGo.transform.SetParent(panelRoot.transform, false);
            RectTransform srt = scrollAreaGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -86f);
            srt.sizeDelta = new Vector2(-40f, 155f);

            ScrollRect scrollRect = scrollAreaGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 20f;

            GameObject viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollAreaGo.transform, false);
            RectTransform vprt = viewportGo.AddComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.sizeDelta = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            scrollRect.viewport = vprt;

            GameObject cardsContainerGo = new GameObject("RecipeCardsContainer");
            cardsContainerGo.transform.SetParent(viewportGo.transform, false);
            RectTransform ccrt = cardsContainerGo.AddComponent<RectTransform>();
            ccrt.anchorMin = new Vector2(0f, 0f);
            ccrt.anchorMax = new Vector2(0f, 1f);
            ccrt.pivot = new Vector2(0f, 0.5f);
            ccrt.anchoredPosition = Vector2.zero;
            ccrt.sizeDelta = new Vector2(0f, 0f);

            HorizontalLayoutGroup hlg = cardsContainerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            scrollRect.content = ccrt;
            recipeCardsContainer = cardsContainerGo.transform;

            // Selected Recipe Details Panel
            GameObject detailsPanel = new GameObject("SelectedDetailsPanel");
            detailsPanel.transform.SetParent(panelRoot.transform, false);
            RectTransform dprt = detailsPanel.AddComponent<RectTransform>();
            dprt.anchorMin = new Vector2(0f, 1f);
            dprt.anchorMax = new Vector2(1f, 1f);
            dprt.pivot = new Vector2(0.5f, 1f);
            dprt.anchoredPosition = new Vector2(0f, -250f);
            dprt.sizeDelta = new Vector2(-40f, 126f);

            Image dpImg = detailsPanel.AddComponent<Image>();
            dpImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            Outline dpOutline = detailsPanel.AddComponent<Outline>();
            dpOutline.effectColor = new Color(0.24f, 0.32f, 0.44f, 0.8f);
            dpOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Icon Frame inside Details Panel (Left side preview)
            GameObject selIconFrame = new GameObject("SelectedIconFrame");
            selIconFrame.transform.SetParent(detailsPanel.transform, false);
            RectTransform sifrt = selIconFrame.AddComponent<RectTransform>();
            sifrt.anchorMin = new Vector2(0f, 0.5f);
            sifrt.anchorMax = new Vector2(0f, 0.5f);
            sifrt.pivot = new Vector2(0f, 0.5f);
            sifrt.anchoredPosition = new Vector2(14f, 0f);
            sifrt.sizeDelta = new Vector2(86f, 86f);

            Image sifBg = selIconFrame.AddComponent<Image>();
            sifBg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            Outline sifOutline = selIconFrame.AddComponent<Outline>();
            sifOutline.effectColor = new Color(0.35f, 0.50f, 0.70f, 0.7f);
            sifOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject selIconGo = new GameObject("SelectedProductIcon");
            selIconGo.transform.SetParent(selIconFrame.transform, false);
            RectTransform sirt = selIconGo.AddComponent<RectTransform>();
            sirt.anchorMin = Vector2.zero;
            sirt.anchorMax = Vector2.one;
            sirt.sizeDelta = new Vector2(-8f, -8f);
            imageSelectedProductIcon = selIconGo.AddComponent<Image>();
            imageSelectedProductIcon.preserveAspect = true;

            // Recipe Info Text inside Details Panel
            GameObject rGo = new GameObject("RecipeInfo");
            rGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform rrt = rGo.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0f, 0.52f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0f, 0.5f);
            rrt.anchoredPosition = new Vector2(114f, 0f);
            rrt.sizeDelta = new Vector2(-128f, 0f);

            textRecipeInfo = rGo.AddComponent<Text>();
            textRecipeInfo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textRecipeInfo.fontSize = 15;
            textRecipeInfo.alignment = TextAnchor.MiddleLeft;
            textRecipeInfo.color = Color.white;

            // Ingredients Text inside Details Panel
            GameObject ingGo = new GameObject("Ingredients");
            ingGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform ingrt = ingGo.AddComponent<RectTransform>();
            ingrt.anchorMin = new Vector2(0f, 0f);
            ingrt.anchorMax = new Vector2(1f, 0.52f);
            ingrt.pivot = new Vector2(0f, 0.5f);
            ingrt.anchoredPosition = new Vector2(114f, 0f);
            ingrt.sizeDelta = new Vector2(-128f, 0f);

            textIngredients = ingGo.AddComponent<Text>();
            textIngredients.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textIngredients.fontSize = 14;
            textIngredients.alignment = TextAnchor.MiddleLeft;
            textIngredients.color = new Color(0.85f, 0.95f, 1f);

            // Instructions text
            GameObject guideGo = new GameObject("Instructions");
            guideGo.transform.SetParent(panelRoot.transform, false);
            RectTransform grt = guideGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 1f);
            grt.anchorMax = new Vector2(1f, 1f);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.anchoredPosition = new Vector2(0f, -384f);
            grt.sizeDelta = new Vector2(-40f, 26f);

            textInstructions = guideGo.AddComponent<Text>();
            textInstructions.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textInstructions.fontSize = 13;
            textInstructions.fontStyle = FontStyle.Italic;
            textInstructions.alignment = TextAnchor.MiddleCenter;
            textInstructions.color = new Color(0.72f, 0.86f, 0.98f, 0.9f);
            textInstructions.text = "💡 Chạm vào thẻ món ăn bên trên để đổi công thức ➔ Đủ nguyên liệu ➔ [Bắt đầu sản xuất].";

            // Status Text
            GameObject ptGo = new GameObject("ProgressText");
            ptGo.transform.SetParent(panelRoot.transform, false);
            RectTransform ptrt = ptGo.AddComponent<RectTransform>();
            ptrt.anchorMin = new Vector2(0.05f, 1f);
            ptrt.anchorMax = new Vector2(0.95f, 1f);
            ptrt.pivot = new Vector2(0.5f, 1f);
            ptrt.anchoredPosition = new Vector2(0f, -412f);
            ptrt.sizeDelta = new Vector2(0f, 26f);

            textProgress = ptGo.AddComponent<Text>();
            textProgress.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textProgress.fontSize = 15;
            textProgress.fontStyle = FontStyle.Bold;
            textProgress.alignment = TextAnchor.MiddleCenter;
            textProgress.color = Color.yellow;

            // Progress Slider
            GameObject slGo = new GameObject("ProgressSlider");
            slGo.transform.SetParent(panelRoot.transform, false);
            RectTransform slrt = slGo.AddComponent<RectTransform>();
            slrt.anchorMin = new Vector2(0.06f, 1f);
            slrt.anchorMax = new Vector2(0.94f, 1f);
            slrt.pivot = new Vector2(0.5f, 1f);
            slrt.anchoredPosition = new Vector2(0f, -444f);
            slrt.sizeDelta = new Vector2(0f, 24f);

            sliderProgress = slGo.AddComponent<Slider>();
            sliderProgress.minValue = 0f;
            sliderProgress.maxValue = 1f;

            // Background of slider
            GameObject sbgGo = new GameObject("Background");
            sbgGo.transform.SetParent(slGo.transform, false);
            RectTransform sbgrt = sbgGo.AddComponent<RectTransform>();
            sbgrt.anchorMin = Vector2.zero;
            sbgrt.anchorMax = Vector2.one;
            sbgrt.sizeDelta = Vector2.zero;
            Image sbgImg = sbgGo.AddComponent<Image>();
            sbgImg.color = new Color(0.12f, 0.16f, 0.22f);

            // Fill Area of slider
            GameObject faGo = new GameObject("Fill Area");
            faGo.transform.SetParent(slGo.transform, false);
            RectTransform farrt = faGo.AddComponent<RectTransform>();
            farrt.anchorMin = Vector2.zero;
            farrt.anchorMax = Vector2.one;
            farrt.sizeDelta = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(faGo.transform, false);
            RectTransform fillrt = fillGo.AddComponent<RectTransform>();
            fillrt.anchorMin = Vector2.zero;
            fillrt.anchorMax = Vector2.one;
            fillrt.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.85f, 0.45f);
            sliderProgress.fillRect = fillrt;

            // Bottom Action Buttons Panel
            buttonStart = CreateSimpleButton(panelRoot.transform, "BẮT ĐẦU SẢN XUẤT", new Vector2(-135f, -172f), new Vector2(230f, 52f), new Color(0.22f, 0.72f, 0.38f));
            textButtonStart = buttonStart.GetComponentInChildren<Text>();
            buttonStart.onClick.AddListener(OnStartClicked);

            buttonCollect = CreateSimpleButton(panelRoot.transform, "★ THU HOẠCH SẢN PHẨM", new Vector2(0f, -172f), new Vector2(270f, 52f), new Color(1.0f, 0.72f, 0.15f));
            buttonCollect.onClick.AddListener(OnCollectClicked);

            buttonCancel = CreateSimpleButton(panelRoot.transform, "HỦY BỎ", new Vector2(135f, -172f), new Vector2(180f, 52f), new Color(0.85f, 0.30f, 0.25f));
            buttonCancel.onClick.AddListener(OnCancelClicked);

            buttonSellMachine = CreateSimpleButton(panelRoot.transform, "BÁN MÁY NÀY", new Vector2(0f, -242f), new Vector2(220f, 40f), new Color(0.75f, 0.28f, 0.26f));
            buttonSellMachine.onClick.AddListener(OnSellMachineClicked);

            BuildConfirmSellModal(canvasGo);
            Close();
        }

        private void BuildConfirmSellModal(GameObject canvasGo)
        {
            confirmSellModal = new GameObject("ConfirmSellModal");
            confirmSellModal.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = confirmSellModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image darkBg = confirmSellModal.AddComponent<Image>();
            darkBg.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject box = new GameObject("Box");
            box.transform.SetParent(confirmSellModal.transform, false);
            RectTransform brt = box.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(440f, 240f);

            Image bImg = box.AddComponent<Image>();
            bImg.color = new Color(0.24f, 0.30f, 0.38f, 0.98f);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(box.transform, false);
            RectTransform trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.45f);
            trt.anchorMax = new Vector2(0.95f, 0.95f);
            trt.sizeDelta = Vector2.zero;

            confirmSellText = txtGo.AddComponent<Text>();
            confirmSellText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            confirmSellText.fontSize = 18;
            confirmSellText.alignment = TextAnchor.MiddleCenter;
            confirmSellText.color = Color.white;
            confirmSellText.text = "Bạn có chắc muốn bán máy này không?";

            btnConfirmSellYes = CreateSimpleButton(box.transform, "XÁC NHẬN BÁN", new Vector2(-100f, -60f), new Vector2(160f, 45f), new Color(0.85f, 0.25f, 0.25f));
            btnConfirmSellYes.onClick.AddListener(ExecuteSellMachine);

            btnConfirmSellNo = CreateSimpleButton(box.transform, "HỦY", new Vector2(100f, -60f), new Vector2(140f, 45f), new Color(0.4f, 0.45f, 0.5f));
            btnConfirmSellNo.onClick.AddListener(() => { if (confirmSellModal != null) confirmSellModal.SetActive(false); });

            confirmSellModal.SetActive(false);
        }

        private Button CreateSimpleButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
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
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }

        void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                // Support PC Escape key and Android Back button
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                    return;
                }

                if (activeMachine != null)
                {
                    if (activeMachine.state == MachineState.Producing)
                    {
                        UpdateProducingProgress();
                    }
                    else if (activeMachine.state == MachineState.Completed && buttonCollect != null && !buttonCollect.gameObject.activeSelf)
                    {
                        RefreshDisplay();
                    }
                }
            }
        }

        private void UpdateProducingProgress()
        {
            if (activeMachine == null) return;

            if (activeMachine.state == MachineState.Completed)
            {
                RefreshDisplay();
                return;
            }

            float dur = Mathf.Max(0.1f, activeMachine.totalDuration);
            float prog = Mathf.Clamp01(activeMachine.currentTimer / dur);
            if (sliderProgress != null) sliderProgress.value = prog;
            int secLeft = Mathf.CeilToInt(Mathf.Max(0f, activeMachine.totalDuration - activeMachine.currentTimer));
            if (textProgress != null)
            {
                textProgress.text = $"⚙ Đang sản xuất: {(int)(prog * 100f)}% (Còn {secLeft} giây...)";
                textProgress.color = Color.yellow;
            }
        }

        public static void OpenMachineShop()
        {
            ShopUI.OpenMachineShop();
        }

        public static void OpenMachine(ProductionMachine machine)
        {
            EnsureUI();
            if (Instance == null || machine == null) return;

            // Close other UI panels to prevent overlap
            if (ShopUI.Instance != null) ShopUI.Instance.Close();
            if (BuildingUI.Instance != null) BuildingUI.Instance.Close();
            if (StorageUI.Instance != null) StorageUI.Instance.Close();

            Instance.activeMachine = machine;
            if (Instance.scrimGo != null) Instance.scrimGo.SetActive(true);
            Instance.panelRoot.SetActive(true);
            Instance.canvasGroup.alpha = 1f;
            Instance.canvasGroup.interactable = true;
            Instance.canvasGroup.blocksRaycasts = true;
            if (Instance.buttonClose != null) Instance.buttonClose.transform.SetAsLastSibling();
            Instance.RefreshDisplay();
            Debug.Log($"[MachineUI] Opened production machine: {machine.machineId} ({machine.displayName})");
        }

        public void Close()
        {
            activeMachine = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            if (scrimGo != null) scrimGo.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (confirmSellModal != null) confirmSellModal.SetActive(false);
        }

        public void RefreshDisplay()
        {
            if (activeMachine == null || activeMachine.currentRecipe == null) return;

            ProductionRecipeData r = activeMachine.currentRecipe;
            textTitle.text = activeMachine.displayName.ToUpper();
            FarmUITheme.ApplyTitle(textTitle, 22);

            // 1. Build / Update Recipe Cards
            UpdateRecipeCards();

            // 2. Selected Recipe Overview
            string outName = InventorySystem.GetDefaultItemName(InventorySystem.NormalizeId(r.outputItemId));
            if (imageSelectedProductIcon != null)
            {
                imageSelectedProductIcon.sprite = FarmIconService.GetItemIcon(r.outputItemId);
                imageSelectedProductIcon.preserveAspect = true;
            }
            textRecipeInfo.text = $"<b>{r.displayName}</b>\n➔ Sản phẩm: <color=#00FF99><b>+{r.outputAmount} {outName}</b></color>  |  Thời gian: <b>{r.productionTime}s</b>  |  Kinh nghiệm: <b>+{r.productionXP} XP</b>";
            FarmUITheme.ApplyBody(textRecipeInfo, 14, true);

            // 3. Ingredients Breakdown with Clear High-Contrast Status
            string ingStr = "<b>Nguyên liệu cần có:</b>";
            var ingredients = r.GetIngredients();
            bool hasAll = true;
            foreach (var ing in ingredients)
            {
                int have = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(ing.Key) : 0;
                bool enough = have >= ing.Value;
                if (!enough) hasAll = false;
                string ingName = InventorySystem.GetDefaultItemName(InventorySystem.NormalizeId(ing.Key));
                string statusTag = enough ? "<color=#2ECC71>✔ ĐỦ</color>" : $"<color=#FF4D4D>✖ THIẾU {ing.Value - have}</color>";
                ingStr += $"\n• {ingName}: <b>{have}/{ing.Value}</b>  {statusTag}";
            }
            textIngredients.text = ingStr;

            // 4. Machine State, Progress & Button controls
            if (activeMachine.state == MachineState.Idle)
            {
                sliderProgress.value = 0f;
                if (hasAll)
                {
                    textProgress.text = "Trạng thái: SẴN SÀNG SẢN XUẤT";
                    textProgress.color = new Color(0.3f, 1f, 0.4f);
                    buttonStart.interactable = true;
                    if (textButtonStart != null) textButtonStart.text = "BẮT ĐẦU SẢN XUẤT";
                    Image bImg = buttonStart.GetComponent<Image>();
                    if (bImg != null) bImg.color = new Color(0.22f, 0.72f, 0.38f);
                }
                else
                {
                    textProgress.text = $"⚠ Thiếu nguyên liệu: {activeMachine.GetMissingIngredientText()}";
                    textProgress.color = new Color(1f, 0.45f, 0.45f);
                    buttonStart.interactable = false;
                    if (textButtonStart != null) textButtonStart.text = "THIẾU NGUYÊN LIỆU";
                    Image bImg = buttonStart.GetComponent<Image>();
                    if (bImg != null) bImg.color = new Color(0.40f, 0.45f, 0.50f);
                }

                buttonStart.gameObject.SetActive(true);
                buttonCollect.gameObject.SetActive(false);
                buttonCancel.gameObject.SetActive(false);
            }
            else if (activeMachine.state == MachineState.Producing)
            {
                float prog = activeMachine.currentTimer / Mathf.Max(0.1f, activeMachine.totalDuration);
                sliderProgress.value = prog;
                int secLeft = Mathf.CeilToInt(activeMachine.totalDuration - activeMachine.currentTimer);
                textProgress.text = $"⚙ Đang sản xuất: {(int)(prog * 100f)}% (Còn {secLeft} giây...)";
                textProgress.color = Color.yellow;
                buttonStart.gameObject.SetActive(false);
                buttonCollect.gameObject.SetActive(false);
                buttonCancel.gameObject.SetActive(true);
            }
            else if (activeMachine.state == MachineState.Completed)
            {
                sliderProgress.value = 1f;
                textProgress.text = $"★ ĐÃ HOÀN THÀNH: Nhận +{activeMachine.pendingOutputAmount} {outName}!";
                textProgress.color = new Color(0.2f, 1f, 0.4f);
                buttonStart.gameObject.SetActive(false);
                buttonCollect.gameObject.SetActive(true);
                buttonCancel.gameObject.SetActive(false);
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void UpdateRecipeCards()
        {
            if (activeMachine == null || activeMachine.availableRecipes == null || recipeCardsContainer == null) return;

            var recipes = activeMachine.availableRecipes;
            int count = recipes.Count;

            // Ensure card pool matches recipe count
            while (activeRecipeCards.Count < count)
            {
                GameObject card = CreateRecipeCardTemplate();
                activeRecipeCards.Add(card);
            }
            for (int i = 0; i < activeRecipeCards.Count; i++)
            {
                activeRecipeCards[i].SetActive(i < count);
            }

            // Calculate card width and layout alignment
            float availableWidth = 730f;
            float totalSpacing = 14f * Mathf.Max(0, count - 1);
            float cardWidth = Mathf.Clamp((availableWidth - totalSpacing) / Mathf.Max(1, count), 215f, 235f);

            // Adjust container width for scroll vs centered
            float requiredTotalWidth = (cardWidth * count) + totalSpacing + 12f;
            RectTransform ccrt = recipeCardsContainer as RectTransform;
            HorizontalLayoutGroup hlg = recipeCardsContainer.GetComponent<HorizontalLayoutGroup>();
            if (ccrt != null && hlg != null)
            {
                if (requiredTotalWidth <= availableWidth)
                {
                    ccrt.sizeDelta = new Vector2(availableWidth, 0f);
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                }
                else
                {
                    ccrt.sizeDelta = new Vector2(requiredTotalWidth, 0f);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                }
            }

            for (int i = 0; i < count; i++)
            {
                ProductionRecipeData recipe = recipes[i];
                GameObject cardGo = activeRecipeCards[i];
                RectTransform crt = cardGo.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(cardWidth, 146f);

                bool isSelected = (activeMachine.currentRecipe != null && activeMachine.currentRecipe.recipeId == recipe.recipeId);

                // Check ingredient sufficiency for this recipe
                bool enoughIngs = true;
                foreach (var ing in recipe.GetIngredients())
                {
                    int have = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(ing.Key) : 0;
                    if (have < ing.Value) enoughIngs = false;
                }

                Image cardBg = cardGo.GetComponent<Image>();
                Outline cardOutline = cardGo.GetComponent<Outline>();
                if (cardOutline == null) cardOutline = cardGo.AddComponent<Outline>();

                Outline ifOutline = cardGo.transform.Find("IconFrame")?.GetComponent<Outline>();

                if (isSelected)
                {
                    cardBg.color = new Color(0.18f, 0.32f, 0.50f, 0.98f);
                    cardOutline.enabled = true;
                    cardOutline.effectColor = new Color(1.0f, 0.85f, 0.25f, 1f);
                    cardOutline.effectDistance = new Vector2(3f, -3f);
                    if (ifOutline != null) ifOutline.effectColor = new Color(1.0f, 0.85f, 0.25f, 0.9f);
                }
                else
                {
                    cardBg.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
                    cardOutline.enabled = true;
                    cardOutline.effectColor = new Color(0.28f, 0.36f, 0.46f, 0.7f);
                    cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
                    if (ifOutline != null) ifOutline.effectColor = new Color(0.30f, 0.40f, 0.50f, 0.5f);
                }

                // 1. Header badge
                Text txtBadge = cardGo.transform.Find("Badge")?.GetComponent<Text>();
                if (txtBadge != null)
                {
                    txtBadge.text = isSelected ? "★ ĐANG CHỌN" : "CHẠM ĐỂ CHỌN";
                    txtBadge.color = isSelected ? new Color(1f, 0.85f, 0.25f) : new Color(0.6f, 0.72f, 0.85f);
                }

                // 2. Product Icon
                Image iconImg = cardGo.transform.Find("IconFrame/ProductIcon")?.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = FarmIconService.GetItemIcon(recipe.outputItemId);
                    iconImg.preserveAspect = true;
                }

                // 3. Recipe Name
                Text txtTitle = cardGo.transform.Find("Title")?.GetComponent<Text>();
                if (txtTitle != null)
                {
                    txtTitle.text = recipe.displayName;
                    txtTitle.color = isSelected ? Color.white : new Color(0.85f, 0.92f, 1f);
                }

                // 4. Output
                Text txtOutput = cardGo.transform.Find("Output")?.GetComponent<Text>();
                if (txtOutput != null)
                {
                    string outShort = InventorySystem.GetDefaultItemName(InventorySystem.NormalizeId(recipe.outputItemId));
                    txtOutput.text = $"➔ +{recipe.outputAmount} {outShort}";
                    txtOutput.color = enoughIngs ? new Color(0.35f, 1f, 0.55f) : new Color(0.95f, 0.75f, 0.35f);
                }

                // 5. Time
                Text txtTime = cardGo.transform.Find("Time")?.GetComponent<Text>();
                if (txtTime != null)
                {
                    txtTime.text = $"⏱ {recipe.productionTime}s";
                }

                // 6. Ingredients Status
                Text txtStatus = cardGo.transform.Find("Status")?.GetComponent<Text>();
                if (txtStatus != null)
                {
                    txtStatus.text = enoughIngs ? "<color=#2ECC71>✔ Đủ nguyên liệu</color>" : "<color=#FF6B6B>✖ Thiếu nguyên liệu</color>";
                }

                // Click event
                Button cardBtn = cardGo.GetComponent<Button>();
                cardBtn.onClick.RemoveAllListeners();
                string rId = recipe.recipeId;
                cardBtn.onClick.AddListener(() =>
                {
                    if (activeMachine != null)
                    {
                        if (activeMachine.CanChangeRecipe)
                        {
                            activeMachine.SelectRecipe(rId);
                            RefreshDisplay();
                        }
                        else
                        {
                            CropSelectionUI.ShowToast("Máy đang sản xuất, không thể đổi công thức lúc này!");
                        }
                    }
                });
            }
        }

        private GameObject CreateRecipeCardTemplate()
        {
            GameObject cardGo = new GameObject("RecipeCard");
            cardGo.transform.SetParent(recipeCardsContainer, false);

            RectTransform rt = cardGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(230f, 146f);

            Image img = cardGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            Button btn = cardGo.AddComponent<Button>();

            // 1. Badge (top)
            GameObject badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(cardGo.transform, false);
            RectTransform brt = badgeGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -6f);
            brt.sizeDelta = new Vector2(-16f, 20f);
            Text bTxt = badgeGo.AddComponent<Text>();
            bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bTxt.fontSize = 11;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;

            // 2. Icon Frame & Product Icon (Middle Left)
            GameObject iconFrameGo = new GameObject("IconFrame");
            iconFrameGo.transform.SetParent(cardGo.transform, false);
            RectTransform ifrt = iconFrameGo.AddComponent<RectTransform>();
            ifrt.anchorMin = new Vector2(0f, 0.5f);
            ifrt.anchorMax = new Vector2(0f, 0.5f);
            ifrt.pivot = new Vector2(0f, 0.5f);
            ifrt.anchoredPosition = new Vector2(10f, 2f);
            ifrt.sizeDelta = new Vector2(56f, 56f);

            Image ifBg = iconFrameGo.AddComponent<Image>();
            ifBg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            Outline ifOutline = iconFrameGo.AddComponent<Outline>();
            ifOutline.effectColor = new Color(0.30f, 0.40f, 0.50f, 0.5f);
            ifOutline.effectDistance = new Vector2(1f, -1f);

            GameObject iconGo = new GameObject("ProductIcon");
            iconGo.transform.SetParent(iconFrameGo.transform, false);
            RectTransform irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.sizeDelta = new Vector2(-6f, -6f);
            Image pIcon = iconGo.AddComponent<Image>();
            pIcon.preserveAspect = true;

            // 3. Middle Right Content (Title, Output, Time)
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(72f, 18f);
            trt.sizeDelta = new Vector2(-80f, 22f);
            Text tTxt = titleGo.AddComponent<Text>();
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 13;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleLeft;

            GameObject outGo = new GameObject("Output");
            outGo.transform.SetParent(cardGo.transform, false);
            RectTransform ort = outGo.AddComponent<RectTransform>();
            ort.anchorMin = new Vector2(0f, 0.5f);
            ort.anchorMax = new Vector2(1f, 0.5f);
            ort.pivot = new Vector2(0f, 0.5f);
            ort.anchoredPosition = new Vector2(72f, -1f);
            ort.sizeDelta = new Vector2(-80f, 18f);
            Text oTxt = outGo.AddComponent<Text>();
            oTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            oTxt.fontSize = 12;
            oTxt.fontStyle = FontStyle.Bold;
            oTxt.alignment = TextAnchor.MiddleLeft;

            GameObject timeGo = new GameObject("Time");
            timeGo.transform.SetParent(cardGo.transform, false);
            RectTransform timert = timeGo.AddComponent<RectTransform>();
            timert.anchorMin = new Vector2(0f, 0.5f);
            timert.anchorMax = new Vector2(1f, 0.5f);
            timert.pivot = new Vector2(0f, 0.5f);
            timert.anchoredPosition = new Vector2(72f, -18f);
            timert.sizeDelta = new Vector2(-80f, 16f);
            Text timeTxt = timeGo.AddComponent<Text>();
            timeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeTxt.fontSize = 11;
            timeTxt.alignment = TextAnchor.MiddleLeft;
            timeTxt.color = new Color(0.72f, 0.82f, 0.92f);

            // 4. Status (bottom)
            GameObject statGo = new GameObject("Status");
            statGo.transform.SetParent(cardGo.transform, false);
            RectTransform srt = statGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 6f);
            srt.sizeDelta = new Vector2(-16f, 22f);
            Text sTxt = statGo.AddComponent<Text>();
            sTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sTxt.fontSize = 12;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;

            return cardGo;
        }

        public void OnChangeRecipeClicked(int direction)
        {
            if (activeMachine != null && activeMachine.CanChangeRecipe)
            {
                activeMachine.CycleRecipe(direction);
                RefreshDisplay();
            }
        }

        private void OnStartClicked()
        {
            if (activeMachine != null)
            {
                activeMachine.StartProduction();
                RefreshDisplay();
            }
        }

        private void OnCollectClicked()
        {
            if (activeMachine != null)
            {
                if (activeMachine.CollectProduct())
                {
                    Close();
                }
            }
        }

        private void OnCancelClicked()
        {
            if (activeMachine != null)
            {
                activeMachine.CancelProduction();
                RefreshDisplay();
            }
        }

        private void OnSellMachineClicked()
        {
            if (activeMachine == null) return;

            if (activeMachine.state == MachineState.Producing)
            {
                CropSelectionUI.ShowToast("Máy đang hoạt động, hãy chờ sản xuất xong hoặc hủy trước khi bán!");
                return;
            }

            int refundGold = FarmDatabase.GetBuildingRefundGold(activeMachine.machineId);
            if (confirmSellModal != null && confirmSellText != null)
            {
                confirmSellText.text = $"Bạn có chắc muốn bán máy này không?\n\n<color=#FFD700>{activeMachine.displayName}</color>\nNhận lại: <color=#00FF88>+{refundGold} Gold</color>";
                confirmSellModal.SetActive(true);
            }
            else
            {
                ExecuteSellMachine();
            }
        }

        private void ExecuteSellMachine()
        {
            if (activeMachine == null) return;

            string mName = activeMachine.displayName;
            int refundGold = FarmDatabase.GetBuildingRefundGold(activeMachine.machineId);

            if (MachineManager.Instance != null && MachineManager.Instance.RemoveMachine(activeMachine))
            {
                PlayerProgression.AddGold(refundGold, $"Bán {mName}");

                CropSelectionUI.ShowToast($"Đã bán {mName}!\n+{refundGold} Gold");
                SaveLoadSystem.SaveGame();
                Close();
            }
            else
            {
                CropSelectionUI.ShowToast($"Không thể bán {mName}!");
                if (confirmSellModal != null) confirmSellModal.SetActive(false);
            }
        }
    }
}
