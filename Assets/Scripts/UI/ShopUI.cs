using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Master Farm Shop UI window allowing purchase of Seeds, Feeds, Animals, Buildings, and Machines.
    /// Checks player Level, Gold balance, building requirements, and updates state in real-time.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }
        public static event Action<ShopItemData> OnItemPurchased;

        public static bool DebugUnlockAll { get; set; } = false;

        [Header("UI Panels")]
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Text textTitle;
        public Text textGoldBalance;
        public Transform itemsContainer;
        public Button buttonClose;
        public ScrollRect scrollRect;

        [Header("Category Tabs")]
        public Button tabSeeds;
        public Button tabFeed;
        public Button tabAnimals;
        public Button tabBuildings;
        public Button tabMachines;
        public Button tabPlots;

        private ShopCategory currentCategory = ShopCategory.Seeds;
        private bool isMachineShopOnly = false;
        private List<GameObject> activeCards = new List<GameObject>();

        public static ShopUI EnsureUI()
        {
            if (Instance != null) return Instance;

            ShopUI found = FindAnyObjectByType<ShopUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("ShopUICanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 94;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Instance = canvasGo.AddComponent<ShopUI>();
            Instance.BuildUIStructure(canvasGo);
            return Instance;
        }

        private void BuildUIStructure(GameObject canvasGo)
        {
            panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = panelRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900f, 620f);

            Image bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.28f, 0.36f, 0.98f);

            canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelRoot.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.6f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(30f, -20f);
            trt.sizeDelta = new Vector2(0f, 45f);

            textTitle = titleGo.AddComponent<Text>();
            textTitle.text = "CỬA HÀNG TRANG TRẠI / FARM SHOP";
            textTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textTitle.fontSize = 24;
            textTitle.fontStyle = FontStyle.Bold;
            textTitle.alignment = TextAnchor.MiddleLeft;
            textTitle.color = new Color(1f, 0.85f, 0.25f);

            // Gold Balance
            GameObject goldGo = new GameObject("GoldBalance");
            goldGo.transform.SetParent(panelRoot.transform, false);
            RectTransform grt = goldGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.6f, 1f);
            grt.anchorMax = new Vector2(0.9f, 1f);
            grt.pivot = new Vector2(1f, 1f);
            grt.anchoredPosition = new Vector2(0f, -20f);
            grt.sizeDelta = new Vector2(0f, 45f);

            textGoldBalance = goldGo.AddComponent<Text>();
            textGoldBalance.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textGoldBalance.fontSize = 20;
            textGoldBalance.fontStyle = FontStyle.Bold;
            textGoldBalance.alignment = TextAnchor.MiddleRight;
            textGoldBalance.color = new Color(1f, 0.9f, 0.3f);

            // Category Tab Bar
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(panelRoot.transform, false);
            RectTransform tabrt = tabBar.AddComponent<RectTransform>();
            tabrt.anchorMin = new Vector2(0.03f, 1f);
            tabrt.anchorMax = new Vector2(0.97f, 1f);
            tabrt.pivot = new Vector2(0.5f, 1f);
            tabrt.anchoredPosition = new Vector2(0f, -75f);
            tabrt.sizeDelta = new Vector2(0f, 45f);

            tabSeeds = CreateTabButton(tabBar.transform, "HẠT GIỐNG", new Vector2(-365f, 0f), ShopCategory.Seeds);
            tabFeed = CreateTabButton(tabBar.transform, "THỨC ĂN", new Vector2(-219f, 0f), ShopCategory.AnimalFeed);
            tabAnimals = CreateTabButton(tabBar.transform, "VẬT NUÔI", new Vector2(-73f, 0f), ShopCategory.Animals);
            tabBuildings = CreateTabButton(tabBar.transform, "CHUỒNG TRẠI", new Vector2(73f, 0f), ShopCategory.Buildings);
            tabMachines = CreateTabButton(tabBar.transform, "MÁY MÓC", new Vector2(219f, 0f), ShopCategory.Machines);
            tabPlots = CreateTabButton(tabBar.transform, "ĐẤT TRỒNG", new Vector2(365f, 0f), ShopCategory.Plots);

            // Scroll View / Content Container
            GameObject scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panelRoot.transform, false);
            RectTransform srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.03f, 0.05f);
            srt.anchorMax = new Vector2(0.97f, 1f);
            srt.anchoredPosition = new Vector2(0f, -75f);
            srt.sizeDelta = new Vector2(0f, -90f);

            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;

            GameObject vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            RectTransform vprt = vpGo.AddComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.offsetMin = Vector2.zero;
            vprt.offsetMax = new Vector2(-16f, 0f);

            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            vpGo.AddComponent<RectMask2D>();

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            RectTransform crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 800f);

            GridLayoutGroup grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(400f, 120f);
            grid.spacing = new Vector2(20f, 15f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vprt;
            scrollRect.content = crt;
            itemsContainer = contentGo.transform;

            // Scrollbar Vertical
            GameObject sbGo = new GameObject("Scrollbar Vertical");
            sbGo.transform.SetParent(scrollGo.transform, false);
            RectTransform sbr = sbGo.AddComponent<RectTransform>();
            sbr.anchorMin = new Vector2(1f, 0f);
            sbr.anchorMax = new Vector2(1f, 1f);
            sbr.pivot = new Vector2(1f, 1f);
            sbr.anchoredPosition = Vector2.zero;
            sbr.sizeDelta = new Vector2(12f, 0f);

            Image sbTrackImg = sbGo.AddComponent<Image>();
            sbTrackImg.color = new Color(0.08f, 0.12f, 0.18f, 0.65f);

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
            handleImg.color = new Color(0.35f, 0.5f, 0.7f, 0.85f);

            Scrollbar sbComp = sbGo.AddComponent<Scrollbar>();
            sbComp.direction = Scrollbar.Direction.BottomToTop;
            sbComp.handleRect = handleRect;

            scrollRect.verticalScrollbar = sbComp;

            // Close Button
            buttonClose = CreateButton(panelRoot.transform, "X", new Vector2(415f, 275f), new Vector2(45f, 45f), new Color(0.85f, 0.2f, 0.2f));
            buttonClose.onClick.AddListener(Close);

            Close();
        }

        private Button CreateTabButton(Transform parent, string label, Vector2 pos, ShopCategory cat)
        {
            Button btn = CreateButton(parent, label, pos, new Vector2(136f, 40f), new Color(0.28f, 0.35f, 0.44f));
            Text t = btn.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.fontSize = 13;
            }
            btn.onClick.AddListener(() =>
            {
                currentCategory = cat;
                UpdateTabVisuals();
                RefreshDisplay();
                ResetScrollPosition();
            });
            return btn;
        }

        public void ResetScrollPosition()
        {
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
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
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        public static void OpenShop()
        {
            EnsureUI();
            if (Instance == null) return;

            // Close other UI panels to prevent overlap
            if (MachineUI.Instance != null) MachineUI.Instance.Close();
            if (BuildingUI.Instance != null) BuildingUI.Instance.Close();
            if (StorageUI.Instance != null) StorageUI.Instance.Close();

            if (Instance.isMachineShopOnly)
            {
                Instance.currentCategory = ShopCategory.Seeds;
            }
            Instance.isMachineShopOnly = false;

            if (Instance.textTitle != null)
            {
                Instance.textTitle.text = "CỬA HÀNG TRANG TRẠI / FARM SHOP";
            }

            Instance.ConfigureTabsForMode();

            Instance.panelRoot.SetActive(true);
            Instance.canvasGroup.alpha = 1f;
            Instance.canvasGroup.interactable = true;
            Instance.canvasGroup.blocksRaycasts = true;
            Instance.RefreshDisplay();
            Instance.ResetScrollPosition();
            Debug.Log("[ShopUI] Opened general shop");
        }

        public static void OpenMachineShop()
        {
            EnsureUI();
            if (Instance == null) return;

            // Close other UI panels to prevent overlap
            if (MachineUI.Instance != null) MachineUI.Instance.Close();
            if (BuildingUI.Instance != null) BuildingUI.Instance.Close();
            if (StorageUI.Instance != null) StorageUI.Instance.Close();

            Instance.isMachineShopOnly = true;
            Instance.currentCategory = ShopCategory.Machines;

            if (Instance.textTitle != null)
            {
                Instance.textTitle.text = "MÁY CHẾ BIẾN NÔNG TRẠI / MACHINES";
            }

            Instance.ConfigureTabsForMode();

            Instance.panelRoot.SetActive(true);
            Instance.canvasGroup.alpha = 1f;
            Instance.canvasGroup.interactable = true;
            Instance.canvasGroup.blocksRaycasts = true;
            Instance.RefreshDisplay();
            Instance.ResetScrollPosition();
            Debug.Log("[MachineShed] Opened machine shop");
        }

        public static void OpenCategory(ShopCategory cat)
        {
            EnsureUI();
            if (Instance == null) return;

            if (cat == ShopCategory.Machines)
            {
                OpenMachineShop();
            }
            else
            {
                Instance.currentCategory = cat;
                OpenShop();
            }
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            isMachineShopOnly = false;
            ConfigureTabsForMode();
            Debug.Log("[ShopUI] Closed Shop");
        }

        private void ConfigureTabsForMode()
        {
            if (tabSeeds == null || tabFeed == null || tabAnimals == null || tabBuildings == null || tabMachines == null || tabPlots == null) return;

            if (isMachineShopOnly)
            {
                tabSeeds.gameObject.SetActive(false);
                tabFeed.gameObject.SetActive(false);
                tabAnimals.gameObject.SetActive(false);
                tabBuildings.gameObject.SetActive(false);
                tabPlots.gameObject.SetActive(false);

                tabMachines.gameObject.SetActive(true);
                SetTabPosition(tabMachines, new Vector2(0f, 0f), new Vector2(260f, 40f));
            }
            else
            {
                tabSeeds.gameObject.SetActive(true);
                tabFeed.gameObject.SetActive(true);
                tabAnimals.gameObject.SetActive(true);
                tabBuildings.gameObject.SetActive(true);
                tabMachines.gameObject.SetActive(true);
                tabPlots.gameObject.SetActive(true);

                // All 6 tabs neatly spaced across tab bar: width 136f, gap 10f
                SetTabPosition(tabSeeds, new Vector2(-365f, 0f), new Vector2(136f, 40f));
                SetTabPosition(tabFeed, new Vector2(-219f, 0f), new Vector2(136f, 40f));
                SetTabPosition(tabAnimals, new Vector2(-73f, 0f), new Vector2(136f, 40f));
                SetTabPosition(tabBuildings, new Vector2(73f, 0f), new Vector2(136f, 40f));
                SetTabPosition(tabMachines, new Vector2(219f, 0f), new Vector2(136f, 40f));
                SetTabPosition(tabPlots, new Vector2(365f, 0f), new Vector2(136f, 40f));
            }

            UpdateTabVisuals();
        }

        private void SetTabPosition(Button btn, Vector2 pos, Vector2 size)
        {
            if (btn == null) return;
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
        }

        private void UpdateTabVisuals()
        {
            SetTabColor(tabSeeds, currentCategory == ShopCategory.Seeds);
            SetTabColor(tabFeed, currentCategory == ShopCategory.AnimalFeed);
            SetTabColor(tabAnimals, currentCategory == ShopCategory.Animals);
            SetTabColor(tabBuildings, currentCategory == ShopCategory.Buildings);
            SetTabColor(tabMachines, currentCategory == ShopCategory.Machines);
            SetTabColor(tabPlots, currentCategory == ShopCategory.Plots);
        }

        private void SetTabColor(Button btn, bool isSelected)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(0.24f, 0.62f, 0.95f) : new Color(0.28f, 0.35f, 0.44f);
            }
        }

        public void RefreshDisplay()
        {
            if (itemsContainer == null) return;

            int currentGold = PlayerProgression.Instance != null ? PlayerProgression.Instance.Gold : 0;
            int playerLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;

            if (textGoldBalance != null)
            {
                FarmUITheme.ApplyBadge(textGoldBalance, 20);
                textGoldBalance.color = FarmUITheme.GoldYellow;
                textGoldBalance.text = $"VÀNG: {currentGold:N0} G";
            }

            UpdateTabVisuals();

            // Clear old cards
            foreach (var card in activeCards)
            {
                if (card != null) Destroy(card);
            }
            activeCards.Clear();

            // Populate category items
            List<ShopItemData> list = FarmDatabase.ShopItems.FindAll(item => item.category == currentCategory);
            foreach (var item in list)
            {
                CreateItemCard(item, playerLevel, currentGold);
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void CreateItemCard(ShopItemData item, int playerLevel, int currentGold)
        {
            GameObject cardGo = new GameObject("Card_" + item.id);
            cardGo.transform.SetParent(itemsContainer, false);
            activeCards.Add(cardGo);

            RectTransform crt = cardGo.AddComponent<RectTransform>();
            crt.sizeDelta = new Vector2(400f, 120f);

            bool isLocked = !DebugUnlockAll && (playerLevel < item.unlockLevel);

            Image bg = cardGo.AddComponent<Image>();
            bg.color = isLocked ? new Color(0.24f, 0.28f, 0.32f, 0.94f) : new Color(0.30f, 0.38f, 0.48f, 0.96f);

            // Item Name
            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(cardGo.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(0.7f, 1f);
            nrt.pivot = new Vector2(0f, 1f);
            nrt.anchoredPosition = new Vector2(15f, -10f);
            nrt.sizeDelta = new Vector2(0f, 30f);

            Text nTxt = nameGo.AddComponent<Text>();
            nTxt.text = item.displayName;
            FarmUITheme.ApplyHeader(nTxt, 16);
            nTxt.color = isLocked ? FarmUITheme.LockedOrange : Color.white;

            // Description
            GameObject descGo = new GameObject("Desc");
            descGo.transform.SetParent(cardGo.transform, false);
            RectTransform drt = descGo.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0.3f);
            drt.anchorMax = new Vector2(0.7f, 0.75f);
            drt.pivot = new Vector2(0f, 0.5f);
            drt.anchoredPosition = new Vector2(15f, 0f);
            drt.sizeDelta = new Vector2(0f, 0f);

            Text dTxt = descGo.AddComponent<Text>();
            dTxt.text = isLocked ? $"[KHÓA - CẦN CẤP {item.unlockLevel}]" : item.description;
            FarmUITheme.ApplyBody(dTxt, 13, false);
            dTxt.color = isLocked ? new Color(0.95f, 0.45f, 0.25f) : new Color(0.85f, 0.90f, 0.95f);

            // Price & Unlock status (bottom)
            GameObject pGo = new GameObject("Price");
            pGo.transform.SetParent(cardGo.transform, false);
            RectTransform prt = pGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0.65f, 0.35f);
            prt.pivot = new Vector2(0f, 0f);
            prt.anchoredPosition = new Vector2(15f, 5f);
            prt.sizeDelta = new Vector2(0f, 0f);

            Text pTxt = pGo.AddComponent<Text>();
            pTxt.text = $"Giá: {item.priceGold:N0} Vàng  |  SL: +{item.quantityPerPurchase}";
            FarmUITheme.ApplyBadge(pTxt, 14);
            pTxt.color = FarmUITheme.GoldYellow;

            // Buy Button
            GameObject bGo = new GameObject("BtnBuy");
            bGo.transform.SetParent(cardGo.transform, false);
            RectTransform brt = bGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.72f, 0.2f);
            brt.anchorMax = new Vector2(0.96f, 0.8f);
            brt.sizeDelta = Vector2.zero;

            Image bImg = bGo.AddComponent<Image>();
            bImg.color = isLocked ? new Color(0.35f, 0.38f, 0.42f) : new Color(0.28f, 0.72f, 0.42f);

            Button buyBtn = bGo.AddComponent<Button>();
            buyBtn.interactable = !isLocked;

            GameObject btGo = new GameObject("Text");
            btGo.transform.SetParent(bGo.transform, false);
            RectTransform btrt = btGo.AddComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.sizeDelta = Vector2.zero;

            Text btTxt = btGo.AddComponent<Text>();
            btTxt.text = isLocked ? "KHÓA" : "MUA";
            btTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btTxt.fontSize = 16;
            btTxt.fontStyle = FontStyle.Bold;
            btTxt.alignment = TextAnchor.MiddleCenter;
            btTxt.color = Color.white;

            // Check if building, machine, or plot is already owned
            if (!isLocked && item.category == ShopCategory.Buildings)
            {
                if (FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased(item.relatedId))
                {
                    btTxt.text = "ĐÃ MUA";
                    buyBtn.interactable = false;
                    bImg.color = new Color(0.40f, 0.46f, 0.54f);
                }
            }
            else if (!isLocked && item.category == ShopCategory.Machines)
            {
                if (MachineManager.Instance != null && MachineManager.Instance.IsMachinePurchased(item.relatedId))
                {
                    btTxt.text = "ĐÃ MUA";
                    buyBtn.interactable = false;
                    bImg.color = new Color(0.40f, 0.46f, 0.54f);
                }
            }
            else if (!isLocked && item.category == ShopCategory.Plots)
            {
                if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(item.relatedId))
                {
                    btTxt.text = "ĐÃ MUA";
                    buyBtn.interactable = false;
                    bImg.color = new Color(0.40f, 0.46f, 0.54f);
                }
            }
            else if (isLocked && item.category == ShopCategory.Plots)
            {
                btTxt.text = "CHƯA ĐỦ CẤP";
                buyBtn.interactable = false;
                bImg.color = new Color(0.35f, 0.38f, 0.42f);
            }

            buyBtn.onClick.AddListener(() => OnBuyItemClicked(item));
        }

        private void OnBuyItemClicked(ShopItemData item)
        {
            int playerLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : 1;

            // 1. Check Level requirement
            if (!DebugUnlockAll && playerLevel < item.unlockLevel)
            {
                CropSelectionUI.ShowToast($"Unlocks at Level {item.unlockLevel}!");
                return;
            }

            // 2. Check Gold
            if (PlayerProgression.Instance == null || PlayerProgression.Instance.Gold < item.priceGold)
            {
                CropSelectionUI.ShowToast("Not enough Gold!");
                return;
            }

            // 3. Process Purchase based on category
            bool success = false;
            switch (item.category)
            {
                case ShopCategory.Seeds:
                case ShopCategory.AnimalFeed:
                    if (InventorySystem.Instance != null)
                    {
                        if (InventorySystem.Instance.IsFull(item.quantityPerPurchase))
                        {
                            CropSelectionUI.ShowToast("Storage Full! Không thể mua thêm.");
                            return;
                        }

                        if (PlayerProgression.TrySpendGold(item.priceGold))
                        {
                            InventorySystem.Instance.AddItem(item.relatedId, item.quantityPerPurchase);
                            success = true;
                        }
                    }
                    break;

                case ShopCategory.Animals:
                    if (FarmBuildingManager.Instance != null)
                    {
                        string barnType = FarmDatabase.Animals[item.relatedId].requiredBarnType;
                        bool hasBarn = FarmBuildingManager.Instance.IsBuildingPurchased(barnType);
                        if (!hasBarn)
                        {
                            if (item.relatedId == "cow" || barnType == "cow_barn")
                                CropSelectionUI.ShowToast("Bạn cần xây Chuồng bò trước");
                            else if (item.relatedId == "chicken" || barnType == "chicken_coop")
                                CropSelectionUI.ShowToast("Bạn cần xây Chuồng gà trước");
                            else if (item.relatedId == "pig" || barnType == "pig_pen")
                                CropSelectionUI.ShowToast("Bạn cần xây Chuồng lợn trước");
                            else if (item.relatedId == "horse" || barnType == "horse_stable")
                                CropSelectionUI.ShowToast("Bạn cần xây Chuồng ngựa trước");
                            else
                                CropSelectionUI.ShowToast($"Bạn cần xây {FarmDatabase.Buildings[barnType].displayName} trước");
                            return;
                        }

                        if (!FarmBuildingManager.Instance.HasCapacityForAnimal(barnType))
                        {
                            FarmBuilding b = FarmBuildingManager.Instance.GetBuilding(barnType);
                            int cur = b != null ? b.GetOccupantCount() : 0;
                            int cap = b != null ? b.capacity : (barnType == "cow_barn" ? 3 : (barnType == "pig_pen" ? 4 : (barnType == "horse_stable" ? 4 : 5)));
                            CropSelectionUI.ShowToast($"Chuồng đã đầy: {cur}/{cap}");
                            return;
                        }

                        // Spawn animal in barn (deducts gold inside FarmBuildingManager)
                        success = FarmBuildingManager.Instance.SpawnAnimalInBarn(item.relatedId);
                    }
                    break;

                case ShopCategory.Buildings:
                    if (FarmBuildingManager.Instance != null && FarmBuildingManager.Instance.IsBuildingPurchased(item.relatedId))
                    {
                        CropSelectionUI.ShowToast("Công trình này đã được xây dựng rồi!");
                        return;
                    }

                    GameObject bPrefab = FarmBuildingManager.LoadBuildingPrefab(item.relatedId);

                    Close();
                    PlacementCategory bCat = (item.relatedId == "storage") ? PlacementCategory.Storage : PlacementCategory.Building;
                    Vector3 bFootprint;
                    if (item.relatedId == "cow_barn") bFootprint = new Vector3(11f, 2.5f, 8f);
                    else if (item.relatedId == "chicken_coop") bFootprint = new Vector3(8f, 2.0f, 6f);
                    else if (item.relatedId == "pig_pen") bFootprint = new Vector3(9f, 2.2f, 7f);
                    else if (item.relatedId == "horse_stable") bFootprint = new Vector3(12f, 2.5f, 9f);
                    else bFootprint = new Vector3(8f, 5f, 8f);

                    BuildingPlacementController.EnsureInstance().StartPlacement(
                        bCat,
                        item.relatedId,
                        item.displayName,
                        item.priceGold,
                        bFootprint,
                        bPrefab
                    );
                    return;

                case ShopCategory.Machines:
                    if (MachineManager.Instance != null && MachineManager.Instance.IsMachinePurchased(item.relatedId))
                    {
                        CropSelectionUI.ShowToast("Máy móc này đã được xây dựng rồi!");
                        return;
                    }

                    if (PlayerProgression.Instance != null && PlayerProgression.Instance.Gold < item.priceGold)
                    {
                        CropSelectionUI.ShowToast($"Bạn không đủ vàng! Cần {item.priceGold:N0} Vàng.");
                        return;
                    }

                    GameObject mPrefab = MachineManager.LoadMachinePrefab(item.relatedId);

                    Close();
                    BuildingPlacementController.EnsureInstance().StartPlacement(
                        PlacementCategory.Machine,
                        item.relatedId,
                        item.displayName,
                        item.priceGold,
                        new Vector3(6f, 4f, 6f),
                        mPrefab
                    );
                    return;

                case ShopCategory.Plots:
                    if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(item.relatedId))
                    {
                        CropSelectionUI.ShowToast("Ô đất này đã được mua rồi!");
                        return;
                    }

                    Close();
                    BuildingPlacementController.EnsureInstance().StartPlacement(
                        PlacementCategory.Plot,
                        item.relatedId,
                        item.displayName,
                        item.priceGold,
                        new Vector3(3f, 1f, 3f),
                        null
                    );
                    return;
            }

            if (success)
            {
                CropSelectionUI.ShowToast($"Purchased successfully: {item.displayName}!");
                Debug.Log($"[ShopUI] Purchased {item.displayName} for {item.priceGold} Gold.");
                OnItemPurchased?.Invoke(item);
                RefreshDisplay();
                SaveLoadSystem.SaveGame();
            }
        }
    }
}
