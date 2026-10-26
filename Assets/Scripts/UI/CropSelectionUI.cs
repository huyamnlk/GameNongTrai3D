using System;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// UI Window allowing the player to select and plant crops into an active CropPlot.
    /// Strictly stays closed at scene start until a valid CropPlot is clicked.
    /// </summary>
    public class CropSelectionUI : MonoBehaviour
    {
        public static CropSelectionUI Instance { get; private set; }

        public static float IgnoreClicksUntilTime { get; private set; } = 0f;

        [Header("Panels")]
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Button buttonClose;
        public ScrollRect scrollRect;
        public Transform cardsContent;

        [Header("Seed Count Labels")]
        public Text textWheatSeed;
        public Text textCornSeed;
        public Text textCarrotSeed;
        public Text textPotatoSeed;
        public Text textTomatoSeed;
        public Text textPumpkinSeed;
        public Text textStrawberrySeed;
        public Text textCabbageSeed;
        public Text textWatermelonSeed;
        public Text textHaySeed;

        [Header("Buttons")]
        public Button buttonPlantWheat;
        public Button buttonPlantCorn;
        public Button buttonPlantCarrot;
        public Button buttonPlantPotato;
        public Button buttonPlantTomato;
        public Button buttonPlantPumpkin;
        public Button buttonPlantStrawberry;
        public Button buttonPlantCabbage;
        public Button buttonPlantWatermelon;
        public Button buttonPlantHay;

        [Header("Prompt Text")]
        public Text textPrompt;

        private CropPlot activePlot;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf && (canvasGroup == null || canvasGroup.alpha > 0.01f);

        void Awake()
        {
            if (Instance == null) Instance = this;
            UpgradeLayoutIfNeeded();
            SetupListeners();
            SetWindowState(false);
        }

        void Start()
        {
            // Explicitly guarantee it is closed on game start
            SetWindowState(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void UpgradeLayoutIfNeeded()
        {
            if (panelRoot == null) return;

            RectTransform pr = panelRoot.GetComponent<RectTransform>();
            if (pr != null)
            {
                pr.anchorMin = new Vector2(0.5f, 0.5f);
                pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.sizeDelta = new Vector2(1040f, 600f);
            }

            // Ensure ScrollView structure exists under panelRoot
            SetupScrollViewStructure(panelRoot.transform);

            // Ensure all 10 cards exist inside cardsContent
            EnsureAllTenCardsExist();
        }

        private void SetupScrollViewStructure(Transform panelTransform)
        {
            Transform existingSv = panelTransform.Find("CropScrollView");
            if (existingSv != null)
            {
                scrollRect = existingSv.GetComponent<ScrollRect>();
                Transform vp = existingSv.Find("Viewport");
                if (vp != null)
                {
                    cardsContent = vp.Find("Content");
                }
            }

            if (scrollRect != null && cardsContent != null)
            {
                // Already configured, ensure proper styling
                return;
            }

            // Header Title (anchored fixed at top)
            Transform titleTr = panelTransform.Find("Title");
            if (titleTr != null)
            {
                RectTransform tr = titleTr.GetComponent<RectTransform>();
                if (tr != null)
                {
                    tr.anchorMin = new Vector2(0f, 1f);
                    tr.anchorMax = new Vector2(1f, 1f);
                    tr.pivot = new Vector2(0.5f, 1f);
                    tr.anchoredPosition = new Vector2(0f, -12f);
                    tr.sizeDelta = new Vector2(-120f, 44f);
                }
            }

            // Button Close (anchored fixed at top right)
            Transform closeTr = panelTransform.Find("ButtonClose");
            if (closeTr != null)
            {
                RectTransform cbr = closeTr.GetComponent<RectTransform>();
                if (cbr != null)
                {
                    cbr.anchorMin = new Vector2(1f, 1f);
                    cbr.anchorMax = new Vector2(1f, 1f);
                    cbr.pivot = new Vector2(1f, 1f);
                    cbr.anchoredPosition = new Vector2(-15f, -12f);
                    cbr.sizeDelta = new Vector2(44f, 44f);
                }
                buttonClose = closeTr.GetComponent<Button>();
            }

            // 1. Scroll View Container
            GameObject svGo = new GameObject("CropScrollView");
            svGo.transform.SetParent(panelTransform, false);
            RectTransform svr = svGo.AddComponent<RectTransform>();
            svr.anchorMin = new Vector2(0f, 0f);
            svr.anchorMax = new Vector2(1f, 1f);
            svr.pivot = new Vector2(0.5f, 0.5f);
            svr.offsetMin = new Vector2(20f, 16f);
            svr.offsetMax = new Vector2(-20f, -68f);

            scrollRect = svGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 32f;

            // 2. Viewport
            GameObject vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(svGo.transform, false);
            RectTransform vpr = vpGo.AddComponent<RectTransform>();
            vpr.anchorMin = Vector2.zero;
            vpr.anchorMax = Vector2.one;
            vpr.pivot = new Vector2(0f, 1f);
            vpr.offsetMin = Vector2.zero;
            vpr.offsetMax = new Vector2(-16f, 0f); // 16px clearance for scrollbar

            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            vpGo.AddComponent<RectMask2D>();

            // 3. Content
            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            RectTransform ctr = contentGo.AddComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0f, 1f);
            ctr.anchorMax = new Vector2(1f, 1f);
            ctr.pivot = new Vector2(0.5f, 1f);
            ctr.anchoredPosition = Vector2.zero;
            ctr.sizeDelta = new Vector2(0f, 600f);

            GridLayoutGroup glg = contentGo.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(230f, 235f);
            glg.spacing = new Vector2(16f, 16f);
            glg.padding = new RectOffset(8, 8, 12, 16);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = vpr;
            scrollRect.content = ctr;
            cardsContent = contentGo.transform;

            // 4. Scrollbar Vertical
            GameObject sbGo = new GameObject("Scrollbar Vertical");
            sbGo.transform.SetParent(svGo.transform, false);
            RectTransform sbr = sbGo.AddComponent<RectTransform>();
            sbr.anchorMin = new Vector2(1f, 0f);
            sbr.anchorMax = new Vector2(1f, 1f);
            sbr.pivot = new Vector2(1f, 1f);
            sbr.anchoredPosition = Vector2.zero;
            sbr.sizeDelta = new Vector2(12f, 0f);

            Image sbTrackImg = sbGo.AddComponent<Image>();
            sbTrackImg.color = new Color(0.08f, 0.12f, 0.08f, 0.65f);

            GameObject saGo = new GameObject("Sliding Area");
            saGo.transform.SetParent(sbGo.transform, false);
            RectTransform saRect = saGo.AddComponent<RectTransform>();
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.sizeDelta = Vector2.zero;

            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(saGo.transform, false);
            RectTransform handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;

            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.35f, 0.65f, 0.42f, 0.85f);

            Scrollbar sbComp = sbGo.AddComponent<Scrollbar>();
            sbComp.direction = Scrollbar.Direction.BottomToTop;
            sbComp.handleRect = handleRect;

            scrollRect.verticalScrollbar = sbComp;

            // Move any cards currently hanging under panelTransform into cardsContent
            string[] cardNames = new string[] {
                "Card_Wheat", "Card_Corn", "Card_Carrot", "Card_Potato", "Card_Tomato",
                "Card_Pumpkin", "Card_Strawberry", "Card_Cabbage", "Card_Watermelon", "Card_Hay"
            };
            foreach (string cName in cardNames)
            {
                Transform cardTr = panelTransform.Find(cName);
                if (cardTr != null)
                {
                    cardTr.SetParent(cardsContent, false);
                    NormalizeCardLayout(cardTr);
                }
            }
        }

        private void EnsureAllTenCardsExist()
        {
            if (cardsContent == null) return;

            EnsureCropCard("Wheat", "LÚA MÌ\n(Wheat)", "30s", "+3", "+10 XP", "+15 Vàng",
                           new Color(0.95f, 0.85f, 0.25f), out buttonPlantWheat, out textWheatSeed);
            EnsureCropCard("Corn", "BẮP NGÔ\n(Corn)", "45s", "+3", "+15 XP", "+25 Vàng",
                           new Color(0.45f, 0.88f, 0.30f), out buttonPlantCorn, out textCornSeed);
            EnsureCropCard("Carrot", "CÀ RỐT\n(Carrot)", "20s", "+2", "+8 XP", "+12 Vàng",
                           new Color(1.0f, 0.55f, 0.15f), out buttonPlantCarrot, out textCarrotSeed);
            EnsureCropCard("Potato", "KHOAI TÂY\n(Potato)", "25s", "+3", "+10 XP", "+16 Vàng",
                           new Color(0.85f, 0.72f, 0.45f), out buttonPlantPotato, out textPotatoSeed);
            EnsureCropCard("Tomato", "CÀ CHUA\n(Tomato)", "35s", "+3", "+12 XP", "+20 Vàng",
                           new Color(0.95f, 0.35f, 0.30f), out buttonPlantTomato, out textTomatoSeed);
            EnsureCropCard("Pumpkin", "BÍ NGÔ\n(Pumpkin)", "50s", "+2", "+18 XP", "+30 Vàng",
                           new Color(0.96f, 0.52f, 0.14f), out buttonPlantPumpkin, out textPumpkinSeed);
            EnsureCropCard("Strawberry", "DÂU TÂY\n(Strawberry)", "30s", "+4", "+14 XP", "+22 Vàng",
                           new Color(0.95f, 0.30f, 0.42f), out buttonPlantStrawberry, out textStrawberrySeed);
            EnsureCropCard("Cabbage", "BẮP CẢI\n(Cabbage)", "28s", "+2", "+11 XP", "+18 Vàng",
                           new Color(0.50f, 0.88f, 0.48f), out buttonPlantCabbage, out textCabbageSeed);
            EnsureCropCard("Watermelon", "DƯA HẤU\n(Watermelon)", "55s", "+2", "+20 XP", "+35 Vàng",
                           new Color(0.32f, 0.80f, 0.38f), out buttonPlantWatermelon, out textWatermelonSeed);
            EnsureCropCard("Hay", "CỎ CHĂN NUÔI\n(Hay / Grass)", "25s", "+3", "+10 XP", "+15 Vàng",
                           new Color(0.68f, 0.88f, 0.45f), out buttonPlantHay, out textHaySeed);
        }

        private void EnsureCropCard(string id, string title, string timeStr, string yieldStr, string xpStr, string goldStr,
                                    Color themeColor, out Button outBtn, out Text outSeedText)
        {
            Transform existing = cardsContent.Find("Card_" + id);
            if (existing != null)
            {
                NormalizeCardLayout(existing);
                outBtn = existing.Find("BtnPlant")?.GetComponent<Button>();
                outSeedText = existing.Find("SeedCount")?.GetComponent<Text>();
                return;
            }

            CreateCropCard(cardsContent, id, title, timeStr, yieldStr, xpStr, goldStr,
                           Vector2.zero, themeColor, out outBtn, out outSeedText);
        }

        private static void NormalizeCardLayout(Transform cardTr)
        {
            if (cardTr == null) return;
            RectTransform crt = cardTr.GetComponent<RectTransform>();
            if (crt != null)
            {
                crt.sizeDelta = new Vector2(230f, 235f);
            }

            Transform titleTr = cardTr.Find("Title");
            if (titleTr != null)
            {
                RectTransform tr = titleTr.GetComponent<RectTransform>();
                if (tr != null)
                {
                    tr.anchorMin = new Vector2(0.03f, 0.74f);
                    tr.anchorMax = new Vector2(0.97f, 0.98f);
                    tr.sizeDelta = Vector2.zero;
                }
            }

            Transform infoTr = cardTr.Find("Info");
            if (infoTr != null)
            {
                RectTransform ir = infoTr.GetComponent<RectTransform>();
                if (ir != null)
                {
                    ir.anchorMin = new Vector2(0.04f, 0.39f);
                    ir.anchorMax = new Vector2(0.96f, 0.74f);
                    ir.sizeDelta = Vector2.zero;
                }
            }

            Transform seedTr = cardTr.Find("SeedCount");
            if (seedTr != null)
            {
                RectTransform sr = seedTr.GetComponent<RectTransform>();
                if (sr != null)
                {
                    sr.anchorMin = new Vector2(0.04f, 0.22f);
                    sr.anchorMax = new Vector2(0.96f, 0.39f);
                    sr.sizeDelta = Vector2.zero;
                }
            }

            Transform btnTr = cardTr.Find("BtnPlant");
            if (btnTr != null)
            {
                RectTransform br = btnTr.GetComponent<RectTransform>();
                if (br != null)
                {
                    br.anchorMin = new Vector2(0.07f, 0.04f);
                    br.anchorMax = new Vector2(0.93f, 0.21f);
                    br.sizeDelta = Vector2.zero;
                }
            }
        }

        private void BindPlantButton(Button btn, string cropId)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => TryPlant(CropData.GetCrop(cropId)));
        }

        public void SetupListeners()
        {
            UpgradeLayoutIfNeeded();
            if (buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners();
                buttonClose.onClick.AddListener(Close);
            }

            BindPlantButton(buttonPlantWheat, "wheat");
            BindPlantButton(buttonPlantCorn, "corn");
            BindPlantButton(buttonPlantCarrot, "carrot");
            BindPlantButton(buttonPlantPotato, "potato");
            BindPlantButton(buttonPlantTomato, "tomato");
            BindPlantButton(buttonPlantPumpkin, "pumpkin");
            BindPlantButton(buttonPlantStrawberry, "strawberry");
            BindPlantButton(buttonPlantCabbage, "cabbage");
            BindPlantButton(buttonPlantWatermelon, "watermelon");
            BindPlantButton(buttonPlantHay, "hay");
        }

        public static void OpenMenu(CropPlot plot)
        {
            EnsureUI();
            if (Instance == null || plot == null) return;

            Instance.activePlot = plot;
            Instance.Open();
            Debug.Log($"[CropSelectionUI] Selected plot: {plot.gameObject.name}");
        }

        public void Open()
        {
            // Close other UI panels to prevent overlap
            if (ShopUI.Instance != null && ShopUI.Instance.panelRoot != null && ShopUI.Instance.panelRoot.activeSelf) ShopUI.Instance.Close();
            if (StorageUI.Instance != null && StorageUI.Instance.IsOpen) StorageUI.Instance.CloseStorage();
            if (AnimalUI.Instance != null && AnimalUI.Instance.IsOpen) AnimalUI.Instance.Close();
            if (QuestUI.Instance != null && QuestUI.Instance.IsOpen) QuestUI.Instance.Close();
            if (AchievementUI.Instance != null && AchievementUI.Instance.IsOpen) AchievementUI.Instance.Close();

            UpgradeLayoutIfNeeded();
            SetWindowState(true);
            RefreshDisplay();
            ResetScrollPosition();
            Debug.Log("[CropSelectionUI] Opened");
        }

        public void ResetScrollPosition()
        {
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            activePlot = null;
            SetWindowState(false);
            IgnoreClicksUntilTime = Time.unscaledTime + 0.35f;
            Debug.Log("[CropSelectionUI] Closed");
        }

        public void SetWindowState(bool active)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }
        }

        public void RefreshDisplay()
        {
            if (InventorySystem.Instance == null) return;

            int currentLevel = (PlayerProgression.Instance != null) ? PlayerProgression.Instance.Level : 1;

            UpdateCropCardState("wheat", buttonPlantWheat, textWheatSeed, "WheatSeed", currentLevel);
            UpdateCropCardState("corn", buttonPlantCorn, textCornSeed, "CornSeed", currentLevel);
            UpdateCropCardState("carrot", buttonPlantCarrot, textCarrotSeed, "CarrotSeed", currentLevel);
            UpdateCropCardState("potato", buttonPlantPotato, textPotatoSeed, "PotatoSeed", currentLevel);
            UpdateCropCardState("tomato", buttonPlantTomato, textTomatoSeed, "TomatoSeed", currentLevel);
            UpdateCropCardState("pumpkin", buttonPlantPumpkin, textPumpkinSeed, "PumpkinSeed", currentLevel);
            UpdateCropCardState("strawberry", buttonPlantStrawberry, textStrawberrySeed, "StrawberrySeed", currentLevel);
            UpdateCropCardState("cabbage", buttonPlantCabbage, textCabbageSeed, "CabbageSeed", currentLevel);
            UpdateCropCardState("watermelon", buttonPlantWatermelon, textWatermelonSeed, "WatermelonSeed", currentLevel);
            UpdateCropCardState("hay", buttonPlantHay, textHaySeed, "HaySeed", currentLevel);

            if (textPrompt != null)
            {
                FarmUITheme.ApplyTitle(textPrompt, 22);
                textPrompt.text = "CHỌN CÂY TRỒNG (10 LOẠI - 5 GIAI ĐOẠN - CẤP 1 ĐẾN 5)";
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void UpdateCropCardState(string cropId, Button btn, Text seedText, string seedItemId, int currentLevel)
        {
            CropData crop = CropData.GetCrop(cropId);
            if (crop == null) return;

            int seedCount = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(seedItemId) : 0;
            bool isUnlocked = (PlayerProgression.Instance != null) ? PlayerProgression.Instance.IsCropUnlocked(cropId) : (currentLevel >= crop.unlockLevel);

            if (seedText != null)
            {
                if (!isUnlocked)
                {
                    seedText.text = $"Khóa (Cấp {crop.unlockLevel})";
                    seedText.color = FarmUITheme.LockedOrange;
                }
                else
                {
                    seedText.text = $"Hạt giống: {seedCount}";
                    seedText.color = seedCount > 0 ? FarmUITheme.GoldYellow : FarmUITheme.TextDim;
                }
            }

            if (btn != null)
            {
                Text btnText = btn.GetComponentInChildren<Text>();
                Image btnImg = btn.GetComponent<Image>();

                if (!isUnlocked)
                {
                    btn.interactable = false;
                    if (btnText != null)
                    {
                        btnText.text = $"CẤP {crop.unlockLevel}";
                        btnText.color = Color.white;
                    }
                    if (btnImg != null) btnImg.color = new Color(0.45f, 0.35f, 0.25f, 0.85f);
                }
                else
                {
                    bool canPlant = (seedCount > 0);
                    btn.interactable = canPlant;
                    if (btnText != null)
                    {
                        btnText.text = canPlant ? "TRỒNG CÂY" : "HẾT HẠT";
                        btnText.color = Color.white;
                    }
                    if (btnImg != null)
                    {
                        btnImg.color = canPlant ? FarmUITheme.GreenPrimary : new Color(0.35f, 0.35f, 0.35f, 0.85f);
                    }
                }
            }
        }

        private void TryPlant(CropData crop)
        {
            if (activePlot == null || crop == null) return;

            if (PlayerProgression.Instance != null && !PlayerProgression.Instance.IsCropUnlocked(crop.cropId))
            {
                ShowToast($"Chưa mở khóa! Cần đạt Cấp {crop.unlockLevel}.");
                return;
            }

            if (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(crop.seedItemId, 1))
            {
                Debug.LogWarning($"[CropSelectionUI] Not enough seeds for {crop.displayName}!");
                ShowToast($"Hết hạt giống ({crop.displayName})! Hãy mua ở Cửa hàng.");
                if (textPrompt != null)
                {
                    textPrompt.text = $"Hết hạt giống ({crop.displayName})!";
                }
                return;
            }

            // Let activePlot.PlantCrop deduct exactly 1 seed and plant
            if (activePlot.PlantCrop(crop.cropId))
            {
                int remaining = InventorySystem.Instance.GetItemCount(crop.seedItemId);
                Debug.Log($"[CropSelectionUI] Planted {crop.displayName}. Seeds left: {remaining}");
                Close();
            }
        }

        private static GameObject activeToastGo;

        public static void ShowToast(string message)
        {
            EnsureUI();
            if (Instance == null) return;

            if (activeToastGo != null) Destroy(activeToastGo);

            Canvas canvas = Instance.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            activeToastGo = new GameObject("CropToast");
            activeToastGo.transform.SetParent(canvas.transform, false);

            RectTransform rt = activeToastGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.78f);
            rt.anchorMax = new Vector2(0.5f, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500f, 70f);

            Image img = activeToastGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.12f, 0.95f);
            FarmUITheme.AddOutline(activeToastGo, new Color(0.95f, 0.8f, 0.2f, 0.85f), new Vector2(2f, 2f));

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(activeToastGo.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = textGo.AddComponent<Text>();
            txt.text = message;
            txt.font = FarmUITheme.BoldFont;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 0.95f, 0.3f);
            FarmUITheme.AddShadow(txt, FarmUITheme.ColorShadowDark, new Vector2(1.5f, -1.5f));

            Destroy(activeToastGo, 3.5f);
        }

        public static CropSelectionUI EnsureUI()
        {
            if (Instance != null) return Instance;

            CropSelectionUI found = FindAnyObjectByType<CropSelectionUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("CropSelectionCanvas");
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 145;

            CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel Root
            GameObject panel = new GameObject("CropSelectionPanel");
            panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(1040f, 600f);

            Image pbg = panel.AddComponent<Image>();
            pbg.color = new Color(0.11f, 0.16f, 0.13f, 0.98f);
            pbg.raycastTarget = true;

            CanvasGroup cg = panel.AddComponent<CanvasGroup>();

            // Header Title (Fixed at top of panel, outside ScrollView)
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            RectTransform tr = titleGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -12f);
            tr.sizeDelta = new Vector2(-120f, 44f);

            Text tt = titleGo.AddComponent<Text>();
            tt.text = "CHỌN CÂY TRỒNG / PLANT CROPS (10 LOẠI - 5 GIAI ĐOẠN)";
            tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 21;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = new Color(0.88f, 1f, 0.65f);

            // Close button "X" (Fixed at top-right of panel, outside ScrollView)
            GameObject cbGo = new GameObject("ButtonClose");
            cbGo.transform.SetParent(panel.transform, false);
            RectTransform cbr = cbGo.AddComponent<RectTransform>();
            cbr.anchorMin = new Vector2(1f, 1f);
            cbr.anchorMax = new Vector2(1f, 1f);
            cbr.pivot = new Vector2(1f, 1f);
            cbr.anchoredPosition = new Vector2(-15f, -12f);
            cbr.sizeDelta = new Vector2(44f, 44f);

            Image cbi = cbGo.AddComponent<Image>();
            cbi.color = new Color(0.85f, 0.22f, 0.2f);
            Button closeBtn = cbGo.AddComponent<Button>();

            GameObject xtGo = new GameObject("Text");
            xtGo.transform.SetParent(cbGo.transform, false);
            RectTransform xtr = xtGo.AddComponent<RectTransform>();
            xtr.anchorMin = Vector2.zero;
            xtr.anchorMax = Vector2.one;
            xtr.sizeDelta = Vector2.zero;
            Text xt = xtGo.AddComponent<Text>();
            xt.text = "✕";
            xt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xt.fontSize = 24;
            xt.fontStyle = FontStyle.Bold;
            xt.alignment = TextAnchor.MiddleCenter;
            xt.color = Color.white;

            CropSelectionUI ui = canvasGo.AddComponent<CropSelectionUI>();
            ui.panelRoot = panel;
            ui.canvasGroup = cg;
            ui.buttonClose = closeBtn;
            ui.textPrompt = tt;

            // Build ScrollView and 10 cards
            ui.SetupScrollViewStructure(panel.transform);
            ui.EnsureAllTenCardsExist();

            // Wire all button listeners
            ui.SetupListeners();

            // Set inactive initially!
            ui.SetWindowState(false);

            Instance = ui;
            return ui;
        }

        private static void CreateCropCard(Transform parent, string id, string title, string timeStr,
                                           string yieldStr, string xpStr, string goldStr,
                                           Vector2 pos, Color themeColor, out Button outBtn, out Text outSeedText)
        {
            GameObject cardGo = new GameObject("Card_" + id);
            cardGo.transform.SetParent(parent, false);

            RectTransform r = cardGo.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(230f, 235f);

            Image img = cardGo.AddComponent<Image>();
            img.color = new Color(0.16f, 0.22f, 0.17f, 0.98f);
            img.raycastTarget = true;

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform tr = titleGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.03f, 0.74f);
            tr.anchorMax = new Vector2(0.97f, 0.98f);
            tr.sizeDelta = Vector2.zero;
            Text txtTitle = titleGo.AddComponent<Text>();
            txtTitle.text = title;
            txtTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTitle.fontSize = 14;
            txtTitle.fontStyle = FontStyle.Bold;
            txtTitle.alignment = TextAnchor.MiddleCenter;
            txtTitle.color = themeColor;

            // Info (Time, Yield, XP, Gold)
            GameObject infoGo = new GameObject("Info");
            infoGo.transform.SetParent(cardGo.transform, false);
            RectTransform ir = infoGo.AddComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.04f, 0.39f);
            ir.anchorMax = new Vector2(0.96f, 0.74f);
            ir.sizeDelta = Vector2.zero;
            Text txtInfo = infoGo.AddComponent<Text>();
            txtInfo.text = $"Thời gian: {timeStr}\nThu hoạch: {yieldStr}\n{xpStr} | {goldStr}";
            txtInfo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtInfo.fontSize = 12;
            txtInfo.lineSpacing = 1.15f;
            txtInfo.alignment = TextAnchor.MiddleCenter;
            txtInfo.color = new Color(0.90f, 0.95f, 0.90f);

            // Seed Count
            GameObject seedGo = new GameObject("SeedCount");
            seedGo.transform.SetParent(cardGo.transform, false);
            RectTransform sr = seedGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.04f, 0.22f);
            sr.anchorMax = new Vector2(0.96f, 0.39f);
            sr.sizeDelta = Vector2.zero;
            outSeedText = seedGo.AddComponent<Text>();
            outSeedText.text = "Hạt: 3";
            outSeedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            outSeedText.fontSize = 13;
            outSeedText.fontStyle = FontStyle.Bold;
            outSeedText.alignment = TextAnchor.MiddleCenter;
            outSeedText.color = new Color(1f, 0.95f, 0.4f);

            // Plant Button (Spacious 40px height, 10px from bottom edge, never cut off)
            GameObject btnGo = new GameObject("BtnPlant");
            btnGo.transform.SetParent(cardGo.transform, false);
            RectTransform br = btnGo.AddComponent<RectTransform>();
            br.anchorMin = new Vector2(0.07f, 0.04f);
            br.anchorMax = new Vector2(0.93f, 0.21f);
            br.sizeDelta = Vector2.zero;

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.22f, 0.58f, 0.22f);
            btnImg.raycastTarget = true;
            outBtn = btnGo.AddComponent<Button>();

            GameObject btnTxtGo = new GameObject("Text");
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            RectTransform btr = btnTxtGo.AddComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.sizeDelta = Vector2.zero;
            Text btnTxt = btnTxtGo.AddComponent<Text>();
            btnTxt.text = "TRỒNG CÂY";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 13;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
        }
    }
}
