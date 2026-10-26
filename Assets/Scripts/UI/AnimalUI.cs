using System;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Modern uGUI window for Animal care: feeding, viewing hunger/happiness stats,
    /// collecting milk/eggs with XP rewards, and selling animals.
    /// Responsive for Landscape 1920x1080.
    /// </summary>
    public class AnimalUI : MonoBehaviour
    {
        public static AnimalUI Instance { get; private set; }

        [Header("UI Panels")]
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Button buttonClose;

        [Header("Animal Info Labels")]
        public Text textTitle;
        public Text textStats;
        public Image barHungerFill;
        public Image barHappinessFill;
        public Image barAffectionFill;
        public Text textProductStatus;

        [Header("Action Buttons")]
        public Button buttonPet;
        public Button buttonFeedHay;
        public Text textHayStock;

        public Button buttonFeedGrain;
        public Text textGrainStock;

        public Button buttonFeedPellets;
        public Text textPelletStock;

        public Button buttonCollectProduct;
        public Text textCollectPrompt;

        public Button buttonSellAnimal;

        [Header("Confirm Sell Modal")]
        public GameObject confirmSellPanel;
        public Text textConfirmSellPrompt;
        public Button buttonConfirmSellYes;
        public Button buttonConfirmSellNo;

        private AnimalCareController activeAnimal;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            if (Instance == null) Instance = this;

            if (buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners();
                buttonClose.onClick.AddListener(Close);
            }
            SetupActionButtons();
            SetWindowState(false);
        }

        void Start()
        {
            SetWindowState(false);
        }

        void Update()
        {
            if (activeAnimal != null && panelRoot != null && panelRoot.activeSelf)
            {
                RefreshDisplay();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void OpenForAnimal(AnimalCareController animal)
        {
            if (animal == null) return;
            EnsureUI();
            if (Instance != null)
            {
                Instance.activeAnimal = animal;
                Debug.Log($"[AnimalCareUI] Opened for animal: {animal.animalName}");
                Instance.Open();
            }
        }

        public void Open()
        {
            CloseSellPrompt();
            SetWindowState(true);
            RefreshDisplay();
        }

        public void Close()
        {
            activeAnimal = null;
            CloseSellPrompt();
            AnimalCareController.DeselectAll();
            SetWindowState(false);
            Debug.Log("[AnimalCareUI] Closed");
        }

        public void CloseAnimalCare()
        {
            Close();
        }

        private void SetWindowState(bool active)
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

        public void SetupActionButtons()
        {
            if (buttonFeedHay != null)
            {
                buttonFeedHay.onClick.RemoveAllListeners();
                buttonFeedHay.onClick.AddListener(() => TryFeed("feed_hay"));
            }

            if (buttonFeedGrain != null)
            {
                buttonFeedGrain.onClick.RemoveAllListeners();
                buttonFeedGrain.onClick.AddListener(() => TryFeed("feed_grain"));
            }

            if (buttonFeedPellets != null)
            {
                buttonFeedPellets.onClick.RemoveAllListeners();
                buttonFeedPellets.onClick.AddListener(() => TryFeed("feed_pellets"));
            }

            if (buttonCollectProduct != null)
            {
                buttonCollectProduct.onClick.RemoveAllListeners();
                buttonCollectProduct.onClick.AddListener(TryCollect);
            }

            if (buttonSellAnimal != null)
            {
                buttonSellAnimal.onClick.RemoveAllListeners();
                buttonSellAnimal.onClick.AddListener(PromptSellAnimal);
            }

            if (buttonConfirmSellYes != null)
            {
                buttonConfirmSellYes.onClick.RemoveAllListeners();
                buttonConfirmSellYes.onClick.AddListener(ExecuteSellAnimal);
            }

            if (buttonConfirmSellNo != null)
            {
                buttonConfirmSellNo.onClick.RemoveAllListeners();
                buttonConfirmSellNo.onClick.AddListener(CloseSellPrompt);
            }

            if (buttonPet != null)
            {
                buttonPet.onClick.RemoveAllListeners();
                buttonPet.onClick.AddListener(() =>
                {
                    if (activeAnimal != null)
                    {
                        activeAnimal.Pet();
                        RefreshDisplay();
                    }
                });
            }
        }

        private void TryFeed(string feedItemId)
        {
            if (activeAnimal == null) return;
            bool fed = activeAnimal.Feed(feedItemId);
            if (!fed)
            {
                Debug.LogWarning("[AnimalUI] Không thể cho ăn (thiếu thức ăn trong kho).");
            }
            RefreshDisplay();
        }

        private void TryCollect()
        {
            if (activeAnimal == null) return;
            bool collected = activeAnimal.CollectProduct();
            if (collected)
            {
                RefreshDisplay();
            }
        }

        public void PromptSellAnimal()
        {
            if (activeAnimal == null) return;

            if (BuildingPlacementController.Instance != null && BuildingPlacementController.Instance.isPlacing)
            {
                CropSelectionUI.ShowToast("Không thể bán vật nuôi khi đang đặt công trình!");
                return;
            }

            string typeKey = activeAnimal.animalType.ToString().ToLower();
            int sellGold = FarmDatabase.GetAnimalSellPrice(typeKey);

            if (textConfirmSellPrompt != null)
            {
                textConfirmSellPrompt.text = $"Bạn có chắc muốn bán con vật này không?\n{activeAnimal.animalName} (+{sellGold} Gold)";
            }

            if (confirmSellPanel != null)
            {
                confirmSellPanel.SetActive(true);
                confirmSellPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseSellPrompt()
        {
            if (confirmSellPanel != null) confirmSellPanel.SetActive(false);
        }

        public void ExecuteSellAnimal()
        {
            if (activeAnimal == null || !activeAnimal.gameObject)
            {
                CloseSellPrompt();
                Close();
                return;
            }

            if (BuildingPlacementController.Instance != null && BuildingPlacementController.Instance.isPlacing)
            {
                CropSelectionUI.ShowToast("Không thể bán vật nuôi khi đang đặt công trình!");
                CloseSellPrompt();
                return;
            }

            string typeKey = activeAnimal.animalType.ToString().ToLower();
            int sellGold = FarmDatabase.GetAnimalSellPrice(typeKey);
            string animalName = activeAnimal.animalName;
            GameObject animalGo = activeAnimal.gameObject;

            // 1. Remove from parent FarmBuilding
            FarmBuilding parentBarn = activeAnimal.GetComponentInParent<FarmBuilding>();
            if (parentBarn != null)
            {
                parentBarn.RemoveAnimal(activeAnimal);
            }

            // 2. Remove from AnimalManager
            if (AnimalManager.Instance != null)
            {
                AnimalManager.Instance.animals.Remove(activeAnimal);
            }

            // 3. Unregister from pen boundary
            AnimalPenBoundary boundary = activeAnimal.GetComponentInParent<AnimalPenBoundary>();
            if (boundary == null)
            {
                CowController cc = activeAnimal.GetComponent<CowController>();
                if (cc != null) boundary = cc.penBoundary;
            }
            if (boundary != null)
            {
                boundary.UnregisterAnimal(animalGo);
            }

            // 4. Add Gold to Player
            PlayerProgression.AddGold(sellGold, $"Bán vật nuôi {animalName}");

            // 5. Safely Destroy animal GameObject
            SafeDestroy(animalGo);

            // 6. Save game data
            SaveLoadSystem.SaveGame();

            // 7. Show toast feedback
            CropSelectionUI.ShowToast($"Đã bán thành công {animalName}!\n+{sellGold} Gold");

            // 8. Refresh building UI if open and close Animal UI
            if (BuildingUI.Instance != null && BuildingUI.Instance.panelRoot != null && BuildingUI.Instance.panelRoot.activeSelf)
            {
                BuildingUI.Instance.RefreshDisplay();
            }

            CloseSellPrompt();
            Close();
        }

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

        public void RefreshDisplay()
        {
            if (activeAnimal == null) return;

            if (textTitle != null)
            {
                textTitle.text = $"CHĂM SÓC: {activeAnimal.animalName.ToUpper()}";
            }

            if (textStats != null)
            {
                textStats.text = $"Giống: {activeAnimal.breed}   |   Độ no: {(int)activeAnimal.hunger}%   |   Vui vẻ: {(int)activeAnimal.happiness}%   |   Tình cảm: {(int)activeAnimal.affection}%";
            }

            if (barHungerFill != null)
            {
                barHungerFill.fillAmount = Mathf.Clamp01(activeAnimal.hunger / 100f);
            }

            if (barHappinessFill != null)
            {
                barHappinessFill.fillAmount = Mathf.Clamp01(activeAnimal.happiness / 100f);
            }

            if (barAffectionFill != null)
            {
                barAffectionFill.fillAmount = Mathf.Clamp01(activeAnimal.affection / 100f);
            }

            // Production status
            if (textProductStatus != null)
            {
                if (activeAnimal.isProductReady)
                {
                    textProductStatus.text = $"★ {activeAnimal.productName.ToUpper()} ĐÃ SẴN SÀNG! ★";
                    textProductStatus.color = new Color(1f, 0.9f, 0.2f);
                }
                else
                {
                    int p = (int)(Mathf.Clamp01(activeAnimal.productTimer / activeAnimal.productionDuration) * 100f);
                    int sec = Mathf.CeilToInt(activeAnimal.productionDuration - activeAnimal.productTimer);
                    textProductStatus.text = $"Đang sản xuất {activeAnimal.productName}: {p}% (còn {sec}s)";
                    textProductStatus.color = Color.white;
                }
            }

            // Collect button interactability
            if (buttonCollectProduct != null)
            {
                buttonCollectProduct.interactable = activeAnimal.isProductReady;
            }

            if (textCollectPrompt != null)
            {
                if (activeAnimal.isProductReady)
                {
                    textCollectPrompt.text = $"THU HOẠCH {activeAnimal.productName.ToUpper()} (+{activeAnimal.productAmount})";
                }
                else
                {
                    textCollectPrompt.text = "CHƯA SẴN SÀNG ĐỂ THU HOẠCH";
                }
            }

            // Update Sell Button label
            if (buttonSellAnimal != null)
            {
                string typeKey = activeAnimal.animalType.ToString().ToLower();
                int sellGold = FarmDatabase.GetAnimalSellPrice(typeKey);
                Text sTxt = buttonSellAnimal.GetComponentInChildren<Text>();
                if (sTxt != null) sTxt.text = $"BÁN VẬT NUÔI (+{sellGold} G)";
            }

            // Refresh feed inventory stock
            if (InventorySystem.Instance != null)
            {
                int hayCount = InventorySystem.Instance.GetItemCount("hay") + InventorySystem.Instance.GetItemCount("feed_hay");
                int grainCount = InventorySystem.Instance.GetItemCount("grain") + InventorySystem.Instance.GetItemCount("feed_grain");
                int pelletCount = InventorySystem.Instance.GetItemCount("pellet") + InventorySystem.Instance.GetItemCount("feed_pellets");

                if (textHayStock != null) textHayStock.text = "Cỏ khô: x" + hayCount;
                if (textGrainStock != null) textGrainStock.text = "Ngô hạt: x" + grainCount;
                if (textPelletStock != null) textPelletStock.text = "Viên ăn: x" + pelletCount;
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        public static AnimalUI EnsureUI()
        {
            if (Instance != null) return Instance;

            AnimalUI found = FindAnyObjectByType<AnimalUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("AnimalUICanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Main Panel
            GameObject panel = new GameObject("AnimalCarePanel");
            panel.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(760f, 440f);

            Image pbg = panel.AddComponent<Image>();
            pbg.color = new Color(0.12f, 0.16f, 0.22f, 0.96f);
            pbg.raycastTarget = true;

            CanvasGroup cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            panel.SetActive(false);

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            RectTransform tr = titleGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -15f);
            tr.sizeDelta = new Vector2(-40f, 40f);

            Text tt = titleGo.AddComponent<Text>();
            tt.text = "CHĂM SÓC VẬT NUÔI / ANIMAL CARE";
            tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 22;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = new Color(0.95f, 0.85f, 0.4f);

            // Close button "X"
            GameObject cbGo = new GameObject("ButtonClose");
            cbGo.transform.SetParent(panel.transform, false);
            RectTransform cbr = cbGo.AddComponent<RectTransform>();
            cbr.anchorMin = new Vector2(1f, 1f);
            cbr.anchorMax = new Vector2(1f, 1f);
            cbr.pivot = new Vector2(1f, 1f);
            cbr.anchoredPosition = new Vector2(-15f, -15f);
            cbr.sizeDelta = new Vector2(40f, 40f);

            Image cbImg = cbGo.AddComponent<Image>();
            cbImg.color = new Color(0.85f, 0.25f, 0.2f);
            Button closeBtn = cbGo.AddComponent<Button>();

            GameObject cbTxt = new GameObject("Text");
            cbTxt.transform.SetParent(cbGo.transform, false);
            RectTransform cbtR = cbTxt.AddComponent<RectTransform>();
            cbtR.anchorMin = Vector2.zero;
            cbtR.anchorMax = Vector2.one;
            cbtR.sizeDelta = Vector2.zero;
            Text cbt = cbTxt.AddComponent<Text>();
            cbt.text = "X";
            cbt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cbt.fontSize = 20;
            cbt.fontStyle = FontStyle.Bold;
            cbt.alignment = TextAnchor.MiddleCenter;
            cbt.color = Color.white;

            // Stats Text
            GameObject statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(panel.transform, false);
            RectTransform sr = statsGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 1f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.pivot = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0f, -55f);
            sr.sizeDelta = new Vector2(-40f, 25f);

            Text st = statsGo.AddComponent<Text>();
            st.text = "Giống: Angus   |   Độ no: 100%   |   Vui vẻ: 100%";
            st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            st.fontSize = 15;
            st.alignment = TextAnchor.MiddleCenter;
            st.color = new Color(0.85f, 0.95f, 1f);

            // Hunger Bar
            Image hungerFill = CreateStatBar(panel.transform, "HungerBar", "ĐỘ NO BỤNG", new Vector2(-240f, -95f), new Color(0.9f, 0.55f, 0.1f));

            // Happiness Bar
            Image happinessFill = CreateStatBar(panel.transform, "HappinessBar", "ĐỘ VUI VẺ", new Vector2(0f, -95f), new Color(0.2f, 0.75f, 0.4f));

            // Affection Bar
            Image affectionFill = CreateStatBar(panel.transform, "AffectionBar", "TÌNH CẢM", new Vector2(240f, -95f), new Color(0.95f, 0.35f, 0.65f));

            // Production Status Text
            GameObject prodGo = new GameObject("ProductStatus");
            prodGo.transform.SetParent(panel.transform, false);
            RectTransform prt = prodGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, -15f);
            prt.sizeDelta = new Vector2(-40f, 30f);

            Text pst = prodGo.AddComponent<Text>();
            pst.text = "Đang sản xuất...";
            pst.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pst.fontSize = 16;
            pst.fontStyle = FontStyle.Bold;
            pst.alignment = TextAnchor.MiddleCenter;
            pst.color = Color.white;

            // Feed Buttons (Hay, Grain, Pellets) & Pet Button
            Button bHay; Text tHay;
            CreateFeedBtn(panel.transform, "FeedHay", "Cho ăn Cỏ khô\n(+2 XP)", new Vector2(-270f, -235f), new Color(0.35f, 0.5f, 0.2f), out bHay, out tHay);

            Button bGrain; Text tGrain;
            CreateFeedBtn(panel.transform, "FeedGrain", "Cho ăn Ngô hạt\n(+3 XP)", new Vector2(-90f, -235f), new Color(0.6f, 0.5f, 0.15f), out bGrain, out tGrain);

            Button bPellet; Text tPellet;
            CreateFeedBtn(panel.transform, "FeedPellet", "Cho ăn Cám viên\n(+3 XP)", new Vector2(90f, -235f), new Color(0.3f, 0.4f, 0.6f), out bPellet, out tPellet);

            Button bPet; Text tPet;
            CreateFeedBtn(panel.transform, "PetAnimal", "❤️ Vuốt ve\n(+2 XP)", new Vector2(270f, -235f), new Color(0.85f, 0.35f, 0.65f), out bPet, out tPet);

            // Harvest Button (Bottom Left)
            GameObject collGo = new GameObject("ButtonCollect");
            collGo.transform.SetParent(panel.transform, false);
            RectTransform colRt = collGo.AddComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0f);
            colRt.anchorMax = new Vector2(0.5f, 0f);
            colRt.pivot = new Vector2(0.5f, 0f);
            colRt.anchoredPosition = new Vector2(-110f, 25f);
            colRt.sizeDelta = new Vector2(380f, 60f);

            Image colImg = collGo.AddComponent<Image>();
            colImg.color = new Color(0.18f, 0.65f, 0.35f);
            Button colBtn = collGo.AddComponent<Button>();

            GameObject colTxtGo = new GameObject("Text");
            colTxtGo.transform.SetParent(collGo.transform, false);
            RectTransform ctrt = colTxtGo.AddComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;

            Text colTxt = colTxtGo.AddComponent<Text>();
            colTxt.text = "THU HOẠCH SẢN PHẨM (+15 XP)";
            colTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            colTxt.fontSize = 18;
            colTxt.fontStyle = FontStyle.Bold;
            colTxt.alignment = TextAnchor.MiddleCenter;
            colTxt.color = Color.white;

            // Sell Animal Button (Bottom Right)
            GameObject sellGo = new GameObject("ButtonSellAnimal");
            sellGo.transform.SetParent(panel.transform, false);
            RectTransform sellRt = sellGo.AddComponent<RectTransform>();
            sellRt.anchorMin = new Vector2(0.5f, 0f);
            sellRt.anchorMax = new Vector2(0.5f, 0f);
            sellRt.pivot = new Vector2(0.5f, 0f);
            sellRt.anchoredPosition = new Vector2(195f, 25f);
            sellRt.sizeDelta = new Vector2(210f, 60f);

            Image sellImg = sellGo.AddComponent<Image>();
            sellImg.color = new Color(0.85f, 0.3f, 0.2f);
            Button sellBtn = sellGo.AddComponent<Button>();

            GameObject sellTxtGo = new GameObject("Text");
            sellTxtGo.transform.SetParent(sellGo.transform, false);
            RectTransform sellTxtRt = sellTxtGo.AddComponent<RectTransform>();
            sellTxtRt.anchorMin = Vector2.zero;
            sellTxtRt.anchorMax = Vector2.one;
            sellTxtRt.sizeDelta = Vector2.zero;

            Text sellTxt = sellTxtGo.AddComponent<Text>();
            sellTxt.text = "BÁN VẬT NUÔI";
            sellTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sellTxt.fontSize = 16;
            sellTxt.fontStyle = FontStyle.Bold;
            sellTxt.alignment = TextAnchor.MiddleCenter;
            sellTxt.color = Color.white;

            // Confirm Sell Modal (Overlay)
            GameObject confPanel = new GameObject("ConfirmSellModal");
            confPanel.transform.SetParent(panel.transform, false);
            RectTransform confR = confPanel.AddComponent<RectTransform>();
            confR.anchorMin = new Vector2(0.5f, 0.5f);
            confR.anchorMax = new Vector2(0.5f, 0.5f);
            confR.pivot = new Vector2(0.5f, 0.5f);
            confR.anchoredPosition = Vector2.zero;
            confR.sizeDelta = new Vector2(500f, 260f);

            Image confBg = confPanel.AddComponent<Image>();
            confBg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            confBg.raycastTarget = true;

            GameObject confTitleGo = new GameObject("Title");
            confTitleGo.transform.SetParent(confPanel.transform, false);
            RectTransform ctitR = confTitleGo.AddComponent<RectTransform>();
            ctitR.anchorMin = new Vector2(0f, 1f);
            ctitR.anchorMax = new Vector2(1f, 1f);
            ctitR.pivot = new Vector2(0.5f, 1f);
            ctitR.anchoredPosition = new Vector2(0f, -20f);
            ctitR.sizeDelta = new Vector2(-30f, 35f);

            Text ctitTxt = confTitleGo.AddComponent<Text>();
            ctitTxt.text = "XÁC NHẬN BÁN VẬT NUÔI";
            ctitTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctitTxt.fontSize = 20;
            ctitTxt.fontStyle = FontStyle.Bold;
            ctitTxt.alignment = TextAnchor.MiddleCenter;
            ctitTxt.color = new Color(1f, 0.85f, 0.35f);

            GameObject confPrmGo = new GameObject("Prompt");
            confPrmGo.transform.SetParent(confPanel.transform, false);
            RectTransform cpR = confPrmGo.AddComponent<RectTransform>();
            cpR.anchorMin = new Vector2(0f, 0.45f);
            cpR.anchorMax = new Vector2(1f, 0.8f);
            cpR.sizeDelta = Vector2.zero;

            Text cpTxt = confPrmGo.AddComponent<Text>();
            cpTxt.text = "Bạn có chắc muốn bán con vật này không?";
            cpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cpTxt.fontSize = 16;
            cpTxt.alignment = TextAnchor.MiddleCenter;
            cpTxt.color = Color.white;

            // Buttons: [XÁC NHẬN BÁN] [HỦY]
            GameObject cyGo = new GameObject("BtnYes");
            cyGo.transform.SetParent(confPanel.transform, false);
            RectTransform cyR = cyGo.AddComponent<RectTransform>();
            cyR.anchoredPosition = new Vector2(-80f, -80f);
            cyR.sizeDelta = new Vector2(160f, 45f);
            Image cyImg = cyGo.AddComponent<Image>();
            cyImg.color = new Color(0.2f, 0.65f, 0.3f);
            Button cyBtn = cyGo.AddComponent<Button>();

            GameObject cyTxtGo = new GameObject("Text");
            cyTxtGo.transform.SetParent(cyGo.transform, false);
            RectTransform cytr = cyTxtGo.AddComponent<RectTransform>();
            cytr.anchorMin = Vector2.zero;
            cytr.anchorMax = Vector2.one;
            cytr.sizeDelta = Vector2.zero;
            Text cyt = cyTxtGo.AddComponent<Text>();
            cyt.text = "XÁC NHẬN";
            cyt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cyt.fontSize = 16;
            cyt.fontStyle = FontStyle.Bold;
            cyt.alignment = TextAnchor.MiddleCenter;
            cyt.color = Color.white;

            GameObject cnGo = new GameObject("BtnNo");
            cnGo.transform.SetParent(confPanel.transform, false);
            RectTransform cnR = cnGo.AddComponent<RectTransform>();
            cnR.anchoredPosition = new Vector2(90f, -80f);
            cnR.sizeDelta = new Vector2(120f, 45f);
            Image cnImg = cnGo.AddComponent<Image>();
            cnImg.color = new Color(0.6f, 0.25f, 0.25f);
            Button cnBtn = cnGo.AddComponent<Button>();

            GameObject cnTxtGo = new GameObject("Text");
            cnTxtGo.transform.SetParent(cnGo.transform, false);
            RectTransform cntr = cnTxtGo.AddComponent<RectTransform>();
            cntr.anchorMin = Vector2.zero;
            cntr.anchorMax = Vector2.one;
            cntr.sizeDelta = Vector2.zero;
            Text cnt = cnTxtGo.AddComponent<Text>();
            cnt.text = "HỦY";
            cnt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cnt.fontSize = 16;
            cnt.fontStyle = FontStyle.Bold;
            cnt.alignment = TextAnchor.MiddleCenter;
            cnt.color = Color.white;

            confPanel.SetActive(false);

            AnimalUI ui = canvasGo.AddComponent<AnimalUI>();
            ui.panelRoot = panel;
            ui.canvasGroup = cg;
            ui.buttonClose = closeBtn;
            ui.textTitle = tt;
            ui.textStats = st;
            ui.barHungerFill = hungerFill;
            ui.barHappinessFill = happinessFill;
            ui.barAffectionFill = affectionFill;
            ui.textProductStatus = pst;
            ui.buttonPet = bPet;
            ui.buttonFeedHay = bHay;
            ui.textHayStock = tHay;
            ui.buttonFeedGrain = bGrain;
            ui.textGrainStock = tGrain;
            ui.buttonFeedPellets = bPellet;
            ui.textPelletStock = tPellet;
            ui.buttonCollectProduct = colBtn;
            ui.textCollectPrompt = colTxt;
            ui.buttonSellAnimal = sellBtn;

            ui.confirmSellPanel = confPanel;
            ui.textConfirmSellPrompt = cpTxt;
            ui.buttonConfirmSellYes = cyBtn;
            ui.buttonConfirmSellNo = cnBtn;

            Instance = ui;
            ui.SetupActionButtons();
            if (ui.buttonClose != null)
            {
                ui.buttonClose.onClick.RemoveAllListeners();
                ui.buttonClose.onClick.AddListener(ui.Close);
            }
            ui.Close();
            return ui;
        }

        private static Image CreateStatBar(Transform parent, string name, string label, Vector2 pos, Color fillColor)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);
            RectTransform crt = container.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = pos;
            crt.sizeDelta = new Vector2(280f, 32f);

            // Label
            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(container.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 2f);
            lrt.sizeDelta = new Vector2(0f, 18f);
            Text lt = lblGo.AddComponent<Text>();
            lt.text = label;
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.fontSize = 13;
            lt.alignment = TextAnchor.MiddleLeft;
            lt.color = Color.white;

            // Bar background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.22f, 0.25f);

            // Bar fill
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(container.transform, false);
            RectTransform frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.sizeDelta = Vector2.zero;

            Image fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0.5f;

            return fill;
        }

        private static void CreateFeedBtn(Transform parent, string name, string label, Vector2 pos, Color btnColor, out Button outBtn, out Text outStock)
        {
            GameObject bGo = new GameObject(name);
            bGo.transform.SetParent(parent, false);
            RectTransform brt = bGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(195f, 75f);

            Image img = bGo.AddComponent<Image>();
            img.color = btnColor;
            outBtn = bGo.AddComponent<Button>();

            // Title
            GameObject titleGo = new GameObject("Text");
            titleGo.transform.SetParent(bGo.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.35f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = Vector2.zero;
            Text tt = titleGo.AddComponent<Text>();
            tt.text = label;
            tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 14;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = Color.white;

            // Stock
            GameObject stockGo = new GameObject("Stock");
            stockGo.transform.SetParent(bGo.transform, false);
            RectTransform srt = stockGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.35f);
            srt.sizeDelta = Vector2.zero;
            outStock = stockGo.AddComponent<Text>();
            outStock.text = "x0";
            outStock.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            outStock.fontSize = 13;
            outStock.alignment = TextAnchor.MiddleCenter;
            outStock.color = new Color(0.9f, 1f, 0.8f);
        }
    }
}
