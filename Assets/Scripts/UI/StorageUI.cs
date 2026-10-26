using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// UI Window for Storage / Barn inventory. Displays items in a clean grid,
    /// allows selling items with quantity controls (+, -, Sell 1, Sell All),
    /// warns when Storage Full, and provides touch and mouse responsiveness.
    /// </summary>
    public class StorageUI : MonoBehaviour
    {
        public static StorageUI Instance { get; private set; }
        public static event System.Action<string, int, int> OnItemSold; // (itemId, quantity, totalGold)

        [Header("UI Panels")]
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Button buttonClose;

        [Header("Text & Indicators")]
        public Text textCapacity;
        public Text textWarning;
        public Transform itemsGridContent;

        [Header("Sell Modal")]
        public GameObject sellModalPanel;
        public Image imageSellItemIcon;
        public Text textSellItemTitle;
        public Text textSellItemInfo;
        public Text textSellAmount;
        public Text textSellTotalGold;
        public Button buttonSellMinus;
        public Button buttonSellPlus;
        public Button buttonSellOne;
        public Button buttonSellAll;
        public Button buttonConfirmSell;
        public Button buttonCancelSell;

        [Header("Building Actions")]
        public Button buttonSellStorage;

        [Header("State")]
        public bool isOpen = false;
        public bool IsOpen => isOpen;
        public FarmBuilding selectedStorage;
        public string currentFilter = "ALL";

        private InventoryItemData currentSellingItem;
        private int currentSellAmount = 1;

        public static StorageUI EnsureUI() => EnsureStorageUIInScene();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            RemoveRedundantTopRightStorageButton();
            InitButtonListeners();
            SetWindowState(false);
        }

        void Start()
        {
            InitButtonListeners();
            SetWindowState(false);
            InventorySystem.OnInventoryChanged += RefreshUI;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            InventorySystem.OnInventoryChanged -= RefreshUI;
        }

        public void Open()
        {
            // Close other UI panels to prevent overlap
            if (ShopUI.Instance != null) ShopUI.Instance.Close();
            if (MachineUI.Instance != null) MachineUI.Instance.Close();
            if (BuildingUI.Instance != null) BuildingUI.Instance.Close();

            EnforcePanelConstraints();
            InitButtonListeners();
            isOpen = true;
            SetWindowState(true);
            CloseSellModal();
            RefreshUI();
            ResetScrollPosition();
            Debug.Log("[StorageUI] Opened");
        }

        public void OnCloseButtonClicked()
        {
            Debug.Log("[StorageUI] Close button clicked");
            CloseStorage();
        }

        public void CloseStorage()
        {
            isOpen = false;
            selectedStorage = null;
            CloseSellModal();
            SetWindowState(false);
            Debug.Log("[StorageUI] Closed");
        }

        public void Close()
        {
            CloseStorage();
        }

        public void Toggle()
        {
            if (isOpen) CloseStorage();
            else Open();
        }

        public void InitButtonListeners()
        {
            if (panelRoot != null)
            {
                if (buttonClose == null)
                {
                    Transform found = panelRoot.transform.Find("Header/CloseButton");
                    if (found == null) found = panelRoot.transform.Find("Header/ButtonClose");
                    if (found == null) found = panelRoot.transform.Find("CloseButton");
                    if (found == null) found = panelRoot.transform.Find("ButtonClose");
                    if (found != null) buttonClose = found.GetComponent<Button>();
                }
            }

            if (buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners();
                buttonClose.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        public void EnforcePanelConstraints()
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c == null && panelRoot != null) c = panelRoot.GetComponentInParent<Canvas>();
            if (c != null)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            if (panelRoot != null)
            {
                RectTransform pr = panelRoot.GetComponent<RectTransform>();
                if (pr != null)
                {
                    pr.anchorMin = new Vector2(0.5f, 0.5f);
                    pr.anchorMax = new Vector2(0.5f, 0.5f);
                    pr.pivot = new Vector2(0.5f, 0.5f);
                    pr.anchoredPosition = new Vector2(0f, -25f);

                    // Constrain within 80% screen width (1536) and 70% screen height (756)
                    float targetWidth = Mathf.Min(1040f, 1920f * 0.8f);
                    float targetHeight = Mathf.Min(620f, 1080f * 0.7f);
                    pr.sizeDelta = new Vector2(targetWidth, targetHeight);
                }
            }
        }

        public void ResetScrollPosition()
        {
            if (panelRoot != null)
            {
                ScrollRect panelScrollRect = panelRoot.GetComponentInChildren<ScrollRect>(true);
                if (panelScrollRect != null)
                {
                    panelScrollRect.verticalNormalizedPosition = 1f;
                }
            }
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

        public void RefreshUI()
        {
            if (!isOpen || InventorySystem.Instance == null) return;

            int total = InventorySystem.Instance.GetTotalItemCount();
            int max = InventorySystem.Instance.maxStorageCapacity;

            if (textCapacity != null)
            {
                textCapacity.text = $"Storage / Sức chứa: {total} / {max}";
            }

            if (textWarning != null)
            {
                if (total >= max)
                {
                    textWarning.gameObject.SetActive(true);
                    textWarning.text = "Storage Full! Kho đã đầy, hãy nâng cấp hoặc bán bớt nông sản.";
                    textWarning.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    textWarning.gameObject.SetActive(false);
                }
            }

            // Sell Storage Building button visibility
            if (buttonSellStorage != null)
            {
                buttonSellStorage.gameObject.SetActive(selectedStorage != null);
            }

            // Populate Item Cards in Grid
            if (itemsGridContent != null)
            {
                // Clear existing child cards safely
                foreach (Transform child in itemsGridContent)
                {
                    SafeDestroy(child.gameObject);
                }

                List<InventoryItemData> allItems = InventorySystem.Instance.GetAllItems();
                foreach (var item in allItems)
                {
                    if (currentFilter != "ALL")
                    {
                        if (currentFilter == "Seed" && item.itemType != ItemType.Seed) continue;
                        if (currentFilter == "Crop" && item.itemType != ItemType.Crop) continue;
                        if (currentFilter == "AnimalProduct" && item.itemType != ItemType.AnimalProduct) continue;
                        if (currentFilter == "Feed" && item.itemType != ItemType.Feed) continue;
                        if (currentFilter == "Material" && item.itemType != ItemType.Material) continue;
                        if (currentFilter == "Decoration" && item.itemType != ItemType.Decoration) continue;
                    }
                    CreateItemCard(item);
                }
            }

            // If sell modal is open, refresh its limits
            if (currentSellingItem != null)
            {
                var refreshed = InventorySystem.Instance.GetItem(currentSellingItem.id);
                if (refreshed == null || refreshed.quantity <= 0)
                {
                    CloseSellModal();
                }
                else
                {
                    currentSellingItem = refreshed;
                    UpdateSellModalDisplay();
                }
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void CreateItemCard(InventoryItemData item)
        {
            GameObject cardGo = new GameObject("Card_" + item.id);
            cardGo.transform.SetParent(itemsGridContent, false);

            RectTransform cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(230f, 150f);

            Image bg = cardGo.AddComponent<Image>();
            bg.color = GetCardColor(item.itemType);
            bg.raycastTarget = true;

            int unitPrice = FarmDatabase.GetItemSellPrice(item.id);

            // Icon Frame (Square background container on left)
            GameObject iconFrame = new GameObject("IconFrame");
            iconFrame.transform.SetParent(cardGo.transform, false);
            RectTransform iconFrameRect = iconFrame.AddComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0.04f, 0.32f);
            iconFrameRect.anchorMax = new Vector2(0.35f, 0.94f);
            iconFrameRect.anchoredPosition = Vector2.zero;
            iconFrameRect.sizeDelta = Vector2.zero;
            Image frameImg = iconFrame.AddComponent<Image>();
            frameImg.color = new Color(0f, 0f, 0f, 0.35f);

            // Item Icon (Crisp, preserved aspect ratio)
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(iconFrame.transform, false);
            RectTransform iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.08f, 0.08f);
            iconRect.anchorMax = new Vector2(0.92f, 0.92f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = FarmIconService.GetItemIcon(item.id, item.itemType);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Category Badge (Top-right)
            GameObject badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(cardGo.transform, false);
            RectTransform badgeRect = badgeGo.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.38f, 0.76f);
            badgeRect.anchorMax = new Vector2(0.98f, 0.96f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = Vector2.zero;

            Text badgeTxt = badgeGo.AddComponent<Text>();
            badgeTxt.text = $"[{GetItemTypeName(item.itemType)}]";
            badgeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            badgeTxt.fontSize = 11;
            badgeTxt.fontStyle = FontStyle.Bold;
            badgeTxt.alignment = TextAnchor.MiddleLeft;
            badgeTxt.color = new Color(0.9f, 0.95f, 1f, 0.85f);

            // Name Text (Middle-right)
            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(cardGo.transform, false);
            RectTransform nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.38f, 0.50f);
            nameRect.anchorMax = new Vector2(0.98f, 0.76f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = Vector2.zero;

            Text nameTxt = nameGo.AddComponent<Text>();
            nameTxt.text = item.displayName;
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = 13;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.color = Color.white;

            // Info (Quantity & Unit Price, Bottom-right of top section)
            GameObject infoGo = new GameObject("Info");
            infoGo.transform.SetParent(cardGo.transform, false);
            RectTransform infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.38f, 0.28f);
            infoRect.anchorMax = new Vector2(0.98f, 0.50f);
            infoRect.anchoredPosition = Vector2.zero;
            infoRect.sizeDelta = Vector2.zero;

            Text infoTxt = infoGo.AddComponent<Text>();
            infoTxt.text = $"x{item.quantity}  •  {unitPrice} G";
            infoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoTxt.fontSize = 12;
            infoTxt.fontStyle = FontStyle.Bold;
            infoTxt.alignment = TextAnchor.MiddleLeft;
            infoTxt.color = new Color(1f, 0.9f, 0.4f, 1f);

            // Row for BÁN, DÙNG, VỨT
            // 1. Sell Button (Left)
            GameObject btnSellGo = new GameObject("BtnSell");
            btnSellGo.transform.SetParent(cardGo.transform, false);
            RectTransform btnSellRect = btnSellGo.AddComponent<RectTransform>();
            btnSellRect.anchorMin = new Vector2(0.04f, 0.04f);
            btnSellRect.anchorMax = new Vector2(0.35f, 0.26f);
            btnSellRect.sizeDelta = Vector2.zero;
            Image sellImg = btnSellGo.AddComponent<Image>();
            sellImg.color = (item.quantity > 0) ? new Color(0.85f, 0.55f, 0.15f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            Button btnSell = btnSellGo.AddComponent<Button>();
            btnSell.interactable = (item.quantity > 0);
            CreateButtonText(btnSellGo.transform, "BÁN", 11);
            if (item.quantity > 0) btnSell.onClick.AddListener(() => OpenSellModal(item));

            // 2. Use Button (Center)
            GameObject btnUseGo = new GameObject("BtnUse");
            btnUseGo.transform.SetParent(cardGo.transform, false);
            RectTransform btnUseRect = btnUseGo.AddComponent<RectTransform>();
            btnUseRect.anchorMin = new Vector2(0.37f, 0.04f);
            btnUseRect.anchorMax = new Vector2(0.68f, 0.26f);
            btnUseRect.sizeDelta = Vector2.zero;
            Image useImg = btnUseGo.AddComponent<Image>();
            useImg.color = (item.quantity > 0) ? new Color(0.18f, 0.65f, 0.38f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            Button btnUse = btnUseGo.AddComponent<Button>();
            btnUse.interactable = (item.quantity > 0);
            CreateButtonText(btnUseGo.transform, "DÙNG", 11);
            if (item.quantity > 0) btnUse.onClick.AddListener(() => UseItem(item));

            // 3. Discard Button (Right)
            GameObject btnDiscGo = new GameObject("BtnDiscard");
            btnDiscGo.transform.SetParent(cardGo.transform, false);
            RectTransform btnDiscRect = btnDiscGo.AddComponent<RectTransform>();
            btnDiscRect.anchorMin = new Vector2(0.70f, 0.04f);
            btnDiscRect.anchorMax = new Vector2(0.96f, 0.26f);
            btnDiscRect.sizeDelta = Vector2.zero;
            Image discImg = btnDiscGo.AddComponent<Image>();
            discImg.color = (item.quantity > 0) ? new Color(0.65f, 0.25f, 0.25f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            Button btnDisc = btnDiscGo.AddComponent<Button>();
            btnDisc.interactable = (item.quantity > 0);
            CreateButtonText(btnDiscGo.transform, "VỨT", 11);
            if (item.quantity > 0) btnDisc.onClick.AddListener(() => DiscardItem(item));
        }

        private static void CreateButtonText(Transform parent, string text, int fontSize)
        {
            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(parent, false);
            RectTransform tr = txtGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        public void DiscardItem(InventoryItemData item)
        {
            if (item == null || item.quantity <= 0) return;
            string dName = item.displayName;
            if (InventorySystem.Instance.RemoveItem(item.id, 1))
            {
                CropSelectionUI.ShowToast($"Đã vứt bỏ 1 {dName}");
                RefreshUI();
            }
        }

        public void UseItem(InventoryItemData item)
        {
            if (item == null || item.quantity <= 0) return;
            switch (item.itemType)
            {
                case ItemType.Seed:
                    CloseStorage();
                    CropSelectionUI.ShowToast($"Đang cầm hạt giống: {item.displayName}. Chạm ô đất để gieo!");
                    break;
                case ItemType.Feed:
                    CloseStorage();
                    CropSelectionUI.ShowToast($"Đang cầm thức ăn: {item.displayName}. Hãy đến chuồng và bấm CHO ĂN!");
                    break;
                case ItemType.Decoration:
                case ItemType.Material:
                    CloseStorage();
                    if (FarmEditModeController.Instance != null)
                    {
                        FarmEditModeController.Instance.EnterEditMode();
                        CropSelectionUI.ShowToast($"Mở chế độ xây dựng để sắp xếp {item.displayName}!");
                    }
                    break;
                default:
                    CropSelectionUI.ShowToast($"Vật phẩm: {item.displayName} (x{item.quantity})");
                    break;
            }
        }

        public void SetFilter(string filter)
        {
            currentFilter = filter;
            RefreshUI();
        }

        private string GetItemTypeName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Seed: return "HẠT GIỐNG";
                case ItemType.Crop: return "NÔNG SẢN";
                case ItemType.AnimalProduct: return "SP VẬT NUÔI";
                case ItemType.Feed: return "THỨC ĂN";
                case ItemType.Material: return "VẬT LIỆU";
                case ItemType.Decoration: return "TRANG TRÍ";
                default: return "VẬT PHẨM";
            }
        }

        private Color GetCardColor(ItemType type)
        {
            switch (type)
            {
                case ItemType.Seed: return new Color(0.24f, 0.38f, 0.28f, 0.95f);
                case ItemType.Crop: return new Color(0.22f, 0.44f, 0.32f, 0.95f);
                case ItemType.AnimalProduct: return new Color(0.40f, 0.28f, 0.45f, 0.95f);
                case ItemType.Feed: return new Color(0.45f, 0.35f, 0.20f, 0.95f);
                case ItemType.Material: return new Color(0.38f, 0.30f, 0.24f, 0.95f);
                case ItemType.Decoration: return new Color(0.24f, 0.36f, 0.48f, 0.95f);
                default: return new Color(0.28f, 0.34f, 0.42f, 0.95f);
            }
        }

        public void OpenSellModal(InventoryItemData item)
        {
            if (item == null || item.quantity <= 0) return;
            currentSellingItem = item;
            currentSellAmount = 1;
            UpdateSellModalDisplay();
            if (sellModalPanel != null)
            {
                sellModalPanel.SetActive(true);
                sellModalPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseSellModal()
        {
            currentSellingItem = null;
            if (sellModalPanel != null) sellModalPanel.SetActive(false);
        }

        private void UpdateSellModalDisplay()
        {
            if (currentSellingItem == null) return;
            int unitPrice = FarmDatabase.GetItemSellPrice(currentSellingItem.id);
            currentSellAmount = Mathf.Clamp(currentSellAmount, 1, currentSellingItem.quantity);

            if (textSellItemTitle != null)
            {
                textSellItemTitle.text = $"BÁN: {currentSellingItem.displayName.ToUpper()}";
            }
            if (imageSellItemIcon != null)
            {
                imageSellItemIcon.sprite = FarmIconService.GetItemIcon(currentSellingItem.id, currentSellingItem.itemType);
                imageSellItemIcon.gameObject.SetActive(imageSellItemIcon.sprite != null);
            }
            if (textSellItemInfo != null)
            {
                textSellItemInfo.text = $"Đơn giá: {unitPrice} Gold / cái   |   Số lượng trong kho: {currentSellingItem.quantity}";
            }
            if (textSellAmount != null)
            {
                textSellAmount.text = currentSellAmount.ToString();
            }
            if (textSellTotalGold != null)
            {
                int totalGold = currentSellAmount * unitPrice;
                textSellTotalGold.text = $"Tổng nhận được: +{totalGold} Gold";
            }
            if (buttonSellMinus != null) buttonSellMinus.interactable = currentSellAmount > 1;
            if (buttonSellPlus != null) buttonSellPlus.interactable = currentSellAmount < currentSellingItem.quantity;
        }

        public void AdjustSellAmount(int delta)
        {
            if (currentSellingItem == null) return;
            currentSellAmount = Mathf.Clamp(currentSellAmount + delta, 1, currentSellingItem.quantity);
            UpdateSellModalDisplay();
        }

        public void SetSellAmount(int amount)
        {
            if (currentSellingItem == null) return;
            currentSellAmount = Mathf.Clamp(amount, 1, currentSellingItem.quantity);
            UpdateSellModalDisplay();
        }

        public void ExecuteSell()
        {
            if (currentSellingItem == null || InventorySystem.Instance == null) return;
            int unitPrice = FarmDatabase.GetItemSellPrice(currentSellingItem.id);
            int sellCount = currentSellAmount;
            if (sellCount > currentSellingItem.quantity) sellCount = currentSellingItem.quantity;
            if (sellCount <= 0) return;

            int totalGold = sellCount * unitPrice;
            string dName = currentSellingItem.displayName;
            string itemId = currentSellingItem.id;

            bool removed = InventorySystem.Instance.RemoveItem(itemId, sellCount);
            if (removed)
            {
                PlayerProgression.AddGold(totalGold, $"Bán {dName} x{sellCount}");
                CropSelectionUI.ShowToast($"Đã bán {dName} x{sellCount}\n+{totalGold} Gold");
                OnItemSold?.Invoke(itemId, sellCount, totalGold);
                SaveLoadSystem.SaveGame();
            }

            CloseSellModal();
            RefreshUI();
        }

        public void SellStorageBuilding()
        {
            if (selectedStorage == null) return;
            int refund = FarmDatabase.GetBuildingRefundGold("storage");
            if (FarmBuildingManager.Instance != null)
            {
                FarmBuildingManager.Instance.RemoveBuilding(selectedStorage);
                PlayerProgression.AddGold(refund, "Bán Kho đồ");
                CropSelectionUI.ShowToast($"Đã bán thành công Kho nông trại!\n+{refund} Gold");
                selectedStorage = null;
                CloseStorage();
            }
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

        /// <summary>
        /// Ensures StorageUI canvas is created in scene with standard responsive layout and ScrollView.
        /// </summary>
        public static StorageUI EnsureStorageUIInScene()
        {
            StorageUI[] founds = FindObjectsByType<StorageUI>(FindObjectsSortMode.None);
            if (founds != null && founds.Length > 0)
            {
                // Check if existing founds already has the standard ScrollView hierarchy
                bool hasValidScrollView = false;
                if (founds[0].panelRoot != null)
                {
                    ScrollRect existingScrollRect = founds[0].panelRoot.GetComponentInChildren<ScrollRect>(true);
                    if (existingScrollRect != null && existingScrollRect.content != null && existingScrollRect.viewport != null)
                    {
                        hasValidScrollView = true;
                    }
                }

                if (hasValidScrollView)
                {
                    Instance = founds[0];
                    for (int i = 1; i < founds.Length; i++)
                    {
                        if (founds[i] != null && founds[i].gameObject != null)
                        {
                            SafeDestroy(founds[i].gameObject);
                        }
                    }
                    Instance.InitButtonListeners();
                    Instance.EnforcePanelConstraints();
                    return Instance;
                }
                else
                {
                    // Clean up outdated StorageUI instances so we rebuild the standard one
                    for (int i = 0; i < founds.Length; i++)
                    {
                        if (founds[i] != null && founds[i].gameObject != null)
                        {
                            SafeDestroy(founds[i].gameObject);
                        }
                    }
                }
            }

            // 1. Create Canvas
            GameObject canvasGo = new GameObject("StorageCanvas");
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 140;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 2. StoragePanel (Centered, constrained <= 80% width [1536], <= 70% height [756])
            GameObject panelGo = new GameObject("StoragePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = panelGo.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.anchoredPosition = new Vector2(0f, -25f);
            pr.sizeDelta = new Vector2(1040f, 620f);

            Image panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.22f, 0.28f, 0.36f, 0.98f);
            panelBg.raycastTarget = true;

            CanvasGroup cg = panelGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            panelGo.SetActive(false);

            // ── Header (Title & CloseButton) ──
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panelGo.transform, false);
            RectTransform hr = headerGo.AddComponent<RectTransform>();
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.anchoredPosition = new Vector2(0f, 0f);
            hr.sizeDelta = new Vector2(0f, 60f);

            Image headerBg = headerGo.AddComponent<Image>();
            headerBg.color = new Color(0.17f, 0.22f, 0.29f, 1f);
            headerBg.raycastTarget = false;

            // Header -> Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);
            RectTransform tr = titleGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = new Vector2(-120f, 0f);

            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = "BARN STORAGE  /  KHO NÔNG TRẠI";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.35f, 1f);
            titleTxt.raycastTarget = false;

            // Header -> CloseButton ("X")
            GameObject closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(headerGo.transform, false);
            RectTransform cr = closeGo.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(1f, 0.5f);
            cr.anchorMax = new Vector2(1f, 0.5f);
            cr.pivot = new Vector2(1f, 0.5f);
            cr.anchoredPosition = new Vector2(-12f, 0f);
            cr.sizeDelta = new Vector2(44f, 44f);

            Image ci = closeGo.AddComponent<Image>();
            ci.color = new Color(0.85f, 0.22f, 0.22f, 1f);
            ci.raycastTarget = true;

            Button cb = closeGo.AddComponent<Button>();

            GameObject xTxtGo = new GameObject("Text");
            xTxtGo.transform.SetParent(closeGo.transform, false);
            RectTransform xtr = xTxtGo.AddComponent<RectTransform>();
            xtr.anchorMin = Vector2.zero;
            xtr.anchorMax = Vector2.one;
            xtr.sizeDelta = Vector2.zero;
            Text xt = xTxtGo.AddComponent<Text>();
            xt.text = "✕";
            xt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xt.fontSize = 24;
            xt.fontStyle = FontStyle.Bold;
            xt.alignment = TextAnchor.MiddleCenter;
            xt.color = Color.white;
            xt.raycastTarget = false;

            // ── SubHeader (Capacity & Warning) ──
            GameObject subHeaderGo = new GameObject("SubHeader");
            subHeaderGo.transform.SetParent(panelGo.transform, false);
            RectTransform subR = subHeaderGo.AddComponent<RectTransform>();
            subR.anchorMin = new Vector2(0f, 1f);
            subR.anchorMax = new Vector2(1f, 1f);
            subR.pivot = new Vector2(0.5f, 1f);
            subR.anchoredPosition = new Vector2(0f, -60f);
            subR.sizeDelta = new Vector2(0f, 36f);

            Image subBg = subHeaderGo.AddComponent<Image>();
            subBg.color = new Color(0.19f, 0.24f, 0.32f, 0.95f);
            subBg.raycastTarget = false;

            // Capacity Text
            GameObject capGo = new GameObject("CapacityText");
            capGo.transform.SetParent(subHeaderGo.transform, false);
            RectTransform capR = capGo.AddComponent<RectTransform>();
            capR.anchorMin = new Vector2(0f, 0f);
            capR.anchorMax = new Vector2(0.5f, 1f);
            capR.pivot = new Vector2(0f, 0.5f);
            capR.anchoredPosition = new Vector2(20f, 0f);
            capR.sizeDelta = new Vector2(-20f, 0f);

            Text capTxt = capGo.AddComponent<Text>();
            capTxt.text = "Sức chứa kho: 0 / 150 Ô";
            capTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            capTxt.fontSize = 16;
            capTxt.fontStyle = FontStyle.Bold;
            capTxt.alignment = TextAnchor.MiddleLeft;
            capTxt.color = new Color(0.85f, 0.92f, 1f, 0.95f);
            capTxt.raycastTarget = false;

            // Warning Text
            GameObject warnGo = new GameObject("WarningText");
            warnGo.transform.SetParent(subHeaderGo.transform, false);
            RectTransform warnR = warnGo.AddComponent<RectTransform>();
            warnR.anchorMin = new Vector2(0.5f, 0f);
            warnR.anchorMax = new Vector2(1f, 1f);
            warnR.pivot = new Vector2(1f, 0.5f);
            warnR.anchoredPosition = new Vector2(-20f, 0f);
            warnR.sizeDelta = new Vector2(-20f, 0f);

            Text warnTxt = warnGo.AddComponent<Text>();
            warnTxt.text = "";
            warnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warnTxt.fontSize = 15;
            warnTxt.fontStyle = FontStyle.Bold;
            warnTxt.alignment = TextAnchor.MiddleRight;
            warnTxt.color = new Color(1f, 0.35f, 0.35f, 1f);
            warnTxt.raycastTarget = false;

            // ── Category Tabs Bar ──
            GameObject tabsGo = new GameObject("CategoryTabs");
            tabsGo.transform.SetParent(panelGo.transform, false);
            RectTransform tabsR = tabsGo.AddComponent<RectTransform>();
            tabsR.anchorMin = new Vector2(0f, 1f);
            tabsR.anchorMax = new Vector2(1f, 1f);
            tabsR.pivot = new Vector2(0.5f, 1f);
            tabsR.anchoredPosition = new Vector2(0f, -96f);
            tabsR.sizeDelta = new Vector2(0f, 38f);

            HorizontalLayoutGroup hlg = tabsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(20, 20, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            string[] filterKeys = new string[] { "ALL", "Seed", "Crop", "AnimalProduct", "Feed", "Material", "Decoration" };
            string[] filterLabels = new string[] { "TẤT CẢ", "HẠT GIỐNG", "NÔNG SẢN", "SP VẬT NUÔI", "THỨC ĂN", "VẬT LIỆU", "TRANG TRÍ" };

            for (int i = 0; i < filterKeys.Length; i++)
            {
                CreateCategoryTab(tabsGo.transform, filterKeys[i], filterLabels[i]);
            }

            // ── Scroll View ──
            GameObject scrollViewGo = new GameObject("Scroll View");
            scrollViewGo.transform.SetParent(panelGo.transform, false);
            RectTransform svr = scrollViewGo.AddComponent<RectTransform>();
            svr.anchorMin = new Vector2(0f, 0f);
            svr.anchorMax = new Vector2(1f, 1f);
            svr.pivot = new Vector2(0.5f, 0.5f);
            // Margin offsets: Left=20, Bottom=65, Right=-20, Top=-140 (clearing header, subheader, tabs)
            svr.offsetMin = new Vector2(20f, 65f);
            svr.offsetMax = new Vector2(-20f, -140f);

            ScrollRect storageScrollRect = scrollViewGo.AddComponent<ScrollRect>();
            storageScrollRect.horizontal = false;
            storageScrollRect.vertical = true;
            storageScrollRect.movementType = ScrollRect.MovementType.Clamped;
            storageScrollRect.inertia = true;
            storageScrollRect.decelerationRate = 0.135f;
            storageScrollRect.scrollSensitivity = 28f;

            // Scroll View -> Viewport
            GameObject viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform vpr = viewportGo.AddComponent<RectTransform>();
            vpr.anchorMin = Vector2.zero;
            vpr.anchorMax = Vector2.one;
            vpr.pivot = new Vector2(0f, 1f);
            vpr.offsetMin = Vector2.zero;
            vpr.offsetMax = new Vector2(-18f, 0f); // 18px clearance for vertical scrollbar

            Image vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = Color.clear;
            viewportGo.AddComponent<RectMask2D>();

            // Viewport -> Content
            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform ctr = contentGo.AddComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0f, 1f);
            ctr.anchorMax = new Vector2(1f, 1f);
            ctr.pivot = new Vector2(0.5f, 1f);
            ctr.anchoredPosition = Vector2.zero;
            ctr.sizeDelta = new Vector2(0f, 300f);

            GridLayoutGroup glg = contentGo.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(230f, 150f);
            glg.spacing = new Vector2(16f, 16f);
            glg.padding = new RectOffset(15, 15, 12, 12);
            glg.childAlignment = TextAnchor.UpperCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Scroll View -> Scrollbar Vertical
            GameObject scrollbarGo = new GameObject("Scrollbar Vertical");
            scrollbarGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform sbr = scrollbarGo.AddComponent<RectTransform>();
            sbr.anchorMin = new Vector2(1f, 0f);
            sbr.anchorMax = new Vector2(1f, 1f);
            sbr.pivot = new Vector2(1f, 1f);
            sbr.anchoredPosition = Vector2.zero;
            sbr.sizeDelta = new Vector2(14f, 0f);

            Image sbTrackImg = scrollbarGo.AddComponent<Image>();
            sbTrackImg.color = new Color(0.08f, 0.11f, 0.16f, 0.7f);

            GameObject slidingAreaGo = new GameObject("Sliding Area");
            slidingAreaGo.transform.SetParent(scrollbarGo.transform, false);
            RectTransform saRect = slidingAreaGo.AddComponent<RectTransform>();
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.sizeDelta = Vector2.zero;

            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(slidingAreaGo.transform, false);
            RectTransform handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;

            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.35f, 0.5f, 0.7f, 0.85f);

            Scrollbar sbComp = scrollbarGo.AddComponent<Scrollbar>();
            sbComp.direction = Scrollbar.Direction.BottomToTop;
            sbComp.handleRect = handleRect;
            sbComp.targetGraphic = handleImg;

            storageScrollRect.viewport = vpr;
            storageScrollRect.content = ctr;
            storageScrollRect.verticalScrollbar = sbComp;
            storageScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // ── BottomBar (Upgrade & Sell Building buttons) ──
            GameObject bottomGo = new GameObject("BottomBar");
            bottomGo.transform.SetParent(panelGo.transform, false);
            RectTransform btr = bottomGo.AddComponent<RectTransform>();
            btr.anchorMin = new Vector2(0f, 0f);
            btr.anchorMax = new Vector2(1f, 0f);
            btr.pivot = new Vector2(0.5f, 0f);
            btr.anchoredPosition = Vector2.zero;
            btr.sizeDelta = new Vector2(0f, 60f);

            Image bottomBg = bottomGo.AddComponent<Image>();
            bottomBg.color = new Color(0.12f, 0.16f, 0.22f, 1f);
            bottomBg.raycastTarget = false;

            // Upgrade Storage Button
            GameObject upgGo = new GameObject("ButtonUpgrade");
            upgGo.transform.SetParent(bottomGo.transform, false);
            RectTransform upgR = upgGo.AddComponent<RectTransform>();
            upgR.anchorMin = new Vector2(0.5f, 0.5f);
            upgR.anchorMax = new Vector2(0.5f, 0.5f);
            upgR.pivot = new Vector2(0.5f, 0.5f);
            upgR.anchoredPosition = new Vector2(-145f, 0f);
            upgR.sizeDelta = new Vector2(265f, 40f);

            Image upgImg = upgGo.AddComponent<Image>();
            upgImg.color = new Color(0.2f, 0.62f, 0.32f, 1f);
            upgImg.raycastTarget = true;

            Button upgBtn = upgGo.AddComponent<Button>();
            upgBtn.onClick.AddListener(() =>
            {
                if (Instance != null && Instance.isOpen && InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.UpgradeCapacity(50, 100);
                }
            });

            GameObject upgTxtGo = new GameObject("Text");
            upgTxtGo.transform.SetParent(upgGo.transform, false);
            RectTransform utr = upgTxtGo.AddComponent<RectTransform>();
            utr.anchorMin = Vector2.zero;
            utr.anchorMax = Vector2.one;
            utr.sizeDelta = Vector2.zero;
            Text ut = upgTxtGo.AddComponent<Text>();
            ut.text = "NÂNG CẤP KHO (+50 Ô - 100 G)";
            ut.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ut.fontSize = 14;
            ut.fontStyle = FontStyle.Bold;
            ut.alignment = TextAnchor.MiddleCenter;
            ut.color = Color.white;
            ut.raycastTarget = false;

            // Sell Storage Building Button
            GameObject sellBldgGo = new GameObject("ButtonSellStorage");
            sellBldgGo.transform.SetParent(bottomGo.transform, false);
            RectTransform sbR = sellBldgGo.AddComponent<RectTransform>();
            sbR.anchorMin = new Vector2(0.5f, 0.5f);
            sbR.anchorMax = new Vector2(0.5f, 0.5f);
            sbR.pivot = new Vector2(0.5f, 0.5f);
            sbR.anchoredPosition = new Vector2(145f, 0f);
            sbR.sizeDelta = new Vector2(250f, 40f);

            Image sbImg = sellBldgGo.AddComponent<Image>();
            sbImg.color = new Color(0.75f, 0.25f, 0.2f, 1f);
            sbImg.raycastTarget = true;

            Button sbBtn = sellBldgGo.AddComponent<Button>();
            sbBtn.onClick.AddListener(() =>
            {
                if (Instance != null) Instance.SellStorageBuilding();
            });

            GameObject sbTxtGo = new GameObject("Text");
            sbTxtGo.transform.SetParent(sellBldgGo.transform, false);
            RectTransform sbTr = sbTxtGo.AddComponent<RectTransform>();
            sbTr.anchorMin = Vector2.zero;
            sbTr.anchorMax = Vector2.one;
            sbTr.sizeDelta = Vector2.zero;
            Text sbt = sbTxtGo.AddComponent<Text>();
            sbt.text = "BÁN KHO NÔNG TRẠI (+70 G)";
            sbt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sbt.fontSize = 14;
            sbt.fontStyle = FontStyle.Bold;
            sbt.alignment = TextAnchor.MiddleCenter;
            sbt.color = Color.white;
            sbt.raycastTarget = false;
            sellBldgGo.SetActive(false);

            // ── Sell Modal Popup ──
            GameObject smGo = new GameObject("SellModalPanel");
            smGo.transform.SetParent(panelGo.transform, false);
            RectTransform smR = smGo.AddComponent<RectTransform>();
            smR.anchorMin = new Vector2(0.5f, 0.5f);
            smR.anchorMax = new Vector2(0.5f, 0.5f);
            smR.pivot = new Vector2(0.5f, 0.5f);
            smR.anchoredPosition = Vector2.zero;
            smR.sizeDelta = new Vector2(560f, 360f);

            Image smBg = smGo.AddComponent<Image>();
            smBg.color = new Color(0.22f, 0.28f, 0.36f, 0.98f);
            smBg.raycastTarget = true;

            // Sell Modal Title
            GameObject smTitleGo = new GameObject("ModalTitle");
            smTitleGo.transform.SetParent(smGo.transform, false);
            RectTransform smtR = smTitleGo.AddComponent<RectTransform>();
            smtR.anchorMin = new Vector2(0f, 1f);
            smtR.anchorMax = new Vector2(1f, 1f);
            smtR.pivot = new Vector2(0.5f, 1f);
            smtR.anchoredPosition = new Vector2(0f, -15f);
            smtR.sizeDelta = new Vector2(-30f, 35f);

            Text smTitleTxt = smTitleGo.AddComponent<Text>();
            smTitleTxt.text = "BÁN NÔNG SẢN / VẬT PHẨM";
            smTitleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            smTitleTxt.fontSize = 20;
            smTitleTxt.fontStyle = FontStyle.Bold;
            smTitleTxt.alignment = TextAnchor.MiddleCenter;
            smTitleTxt.color = new Color(1f, 0.85f, 0.35f);

            // Item Icon in Sell Modal
            GameObject smIconGo = new GameObject("ModalIcon");
            smIconGo.transform.SetParent(smGo.transform, false);
            RectTransform smIconR = smIconGo.AddComponent<RectTransform>();
            smIconR.anchorMin = new Vector2(0.5f, 1f);
            smIconR.anchorMax = new Vector2(0.5f, 1f);
            smIconR.pivot = new Vector2(0.5f, 1f);
            smIconR.anchoredPosition = new Vector2(0f, -48f);
            smIconR.sizeDelta = new Vector2(48f, 48f);
            Image smIconImg = smIconGo.AddComponent<Image>();
            smIconImg.preserveAspect = true;

            // Sell Modal Info
            GameObject smInfoGo = new GameObject("ModalInfo");
            smInfoGo.transform.SetParent(smGo.transform, false);
            RectTransform smiR = smInfoGo.AddComponent<RectTransform>();
            smiR.anchorMin = new Vector2(0f, 1f);
            smiR.anchorMax = new Vector2(1f, 1f);
            smiR.pivot = new Vector2(0.5f, 1f);
            smiR.anchoredPosition = new Vector2(0f, -100f);
            smiR.sizeDelta = new Vector2(-30f, 30f);

            Text smInfoTxt = smInfoGo.AddComponent<Text>();
            smInfoTxt.text = "Đơn giá & Số lượng trong kho";
            smInfoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            smInfoTxt.fontSize = 15;
            smInfoTxt.alignment = TextAnchor.MiddleCenter;
            smInfoTxt.color = new Color(0.85f, 0.95f, 1f);

            // Quantity selector row: [-]  [amount]  [+]
            GameObject rowGo = new GameObject("SelectorRow");
            rowGo.transform.SetParent(smGo.transform, false);
            RectTransform rowR = rowGo.AddComponent<RectTransform>();
            rowR.anchorMin = new Vector2(0.5f, 0.65f);
            rowR.anchorMax = new Vector2(0.5f, 0.65f);
            rowR.pivot = new Vector2(0.5f, 0.5f);
            rowR.anchoredPosition = Vector2.zero;
            rowR.sizeDelta = new Vector2(240f, 50f);

            // Minus Button
            GameObject minusGo = new GameObject("BtnMinus");
            minusGo.transform.SetParent(rowGo.transform, false);
            RectTransform mR = minusGo.AddComponent<RectTransform>();
            mR.anchoredPosition = new Vector2(-80f, 0f);
            mR.sizeDelta = new Vector2(50f, 45f);
            Image mImg = minusGo.AddComponent<Image>();
            mImg.color = new Color(0.25f, 0.35f, 0.45f);
            Button minusBtn = minusGo.AddComponent<Button>();

            GameObject mTxtGo = new GameObject("Text");
            mTxtGo.transform.SetParent(minusGo.transform, false);
            RectTransform mtr = mTxtGo.AddComponent<RectTransform>();
            mtr.anchorMin = Vector2.zero;
            mtr.anchorMax = Vector2.one;
            mtr.sizeDelta = Vector2.zero;
            Text mt = mTxtGo.AddComponent<Text>();
            mt.text = "-";
            mt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mt.fontSize = 26;
            mt.fontStyle = FontStyle.Bold;
            mt.alignment = TextAnchor.MiddleCenter;
            mt.color = Color.white;

            // Amount Text
            GameObject amtGo = new GameObject("TextAmount");
            amtGo.transform.SetParent(rowGo.transform, false);
            RectTransform amtR = amtGo.AddComponent<RectTransform>();
            amtR.anchoredPosition = Vector2.zero;
            amtR.sizeDelta = new Vector2(90f, 45f);
            Text amtTxt = amtGo.AddComponent<Text>();
            amtTxt.text = "1";
            amtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            amtTxt.fontSize = 24;
            amtTxt.fontStyle = FontStyle.Bold;
            amtTxt.alignment = TextAnchor.MiddleCenter;
            amtTxt.color = new Color(1f, 0.95f, 0.4f);

            // Plus Button
            GameObject plusGo = new GameObject("BtnPlus");
            plusGo.transform.SetParent(rowGo.transform, false);
            RectTransform pR = plusGo.AddComponent<RectTransform>();
            pR.anchoredPosition = new Vector2(80f, 0f);
            pR.sizeDelta = new Vector2(50f, 45f);
            Image pImg = plusGo.AddComponent<Image>();
            pImg.color = new Color(0.25f, 0.35f, 0.45f);
            Button plusBtn = plusGo.AddComponent<Button>();

            GameObject pTxtGo = new GameObject("Text");
            pTxtGo.transform.SetParent(plusGo.transform, false);
            RectTransform ptr = pTxtGo.AddComponent<RectTransform>();
            ptr.anchorMin = Vector2.zero;
            ptr.anchorMax = Vector2.one;
            ptr.sizeDelta = Vector2.zero;
            Text pt = pTxtGo.AddComponent<Text>();
            pt.text = "+";
            pt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pt.fontSize = 24;
            pt.fontStyle = FontStyle.Bold;
            pt.alignment = TextAnchor.MiddleCenter;
            pt.color = Color.white;

            // Quick buttons row: [Bán 1] [Bán tất cả]
            GameObject quickGo = new GameObject("QuickRow");
            quickGo.transform.SetParent(smGo.transform, false);
            RectTransform qR = quickGo.AddComponent<RectTransform>();
            qR.anchorMin = new Vector2(0.5f, 0.46f);
            qR.anchorMax = new Vector2(0.5f, 0.46f);
            qR.pivot = new Vector2(0.5f, 0.5f);
            qR.anchoredPosition = Vector2.zero;
            qR.sizeDelta = new Vector2(280f, 40f);

            GameObject b1Go = new GameObject("BtnOne");
            b1Go.transform.SetParent(quickGo.transform, false);
            RectTransform b1R = b1Go.AddComponent<RectTransform>();
            b1R.anchoredPosition = new Vector2(-75f, 0f);
            b1R.sizeDelta = new Vector2(120f, 36f);
            Image b1Img = b1Go.AddComponent<Image>();
            b1Img.color = new Color(0.2f, 0.45f, 0.65f);
            Button b1Btn = b1Go.AddComponent<Button>();

            GameObject b1TxtGo = new GameObject("Text");
            b1TxtGo.transform.SetParent(b1Go.transform, false);
            RectTransform b1tr = b1TxtGo.AddComponent<RectTransform>();
            b1tr.anchorMin = Vector2.zero;
            b1tr.anchorMax = Vector2.one;
            b1tr.sizeDelta = Vector2.zero;
            Text b1t = b1TxtGo.AddComponent<Text>();
            b1t.text = "Bán 1";
            b1t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            b1t.fontSize = 15;
            b1t.fontStyle = FontStyle.Bold;
            b1t.alignment = TextAnchor.MiddleCenter;
            b1t.color = Color.white;

            GameObject bAllGo = new GameObject("BtnAll");
            bAllGo.transform.SetParent(quickGo.transform, false);
            RectTransform bAllR = bAllGo.AddComponent<RectTransform>();
            bAllR.anchoredPosition = new Vector2(75f, 0f);
            bAllR.sizeDelta = new Vector2(130f, 36f);
            Image bAllImg = bAllGo.AddComponent<Image>();
            bAllImg.color = new Color(0.2f, 0.45f, 0.65f);
            Button bAllBtn = bAllGo.AddComponent<Button>();

            GameObject bAllTxtGo = new GameObject("Text");
            bAllTxtGo.transform.SetParent(bAllGo.transform, false);
            RectTransform bAllTr = bAllTxtGo.AddComponent<RectTransform>();
            bAllTr.anchorMin = Vector2.zero;
            bAllTr.anchorMax = Vector2.one;
            bAllTr.sizeDelta = Vector2.zero;
            Text bAllT = bAllTxtGo.AddComponent<Text>();
            bAllT.text = "Bán tất cả";
            bAllT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bAllT.fontSize = 15;
            bAllT.fontStyle = FontStyle.Bold;
            bAllT.alignment = TextAnchor.MiddleCenter;
            bAllT.color = Color.white;

            // Total Gold text
            GameObject totGo = new GameObject("TotalGold");
            totGo.transform.SetParent(smGo.transform, false);
            RectTransform totR = totGo.AddComponent<RectTransform>();
            totR.anchorMin = new Vector2(0f, 0.28f);
            totR.anchorMax = new Vector2(1f, 0.28f);
            totR.pivot = new Vector2(0.5f, 0.5f);
            totR.anchoredPosition = Vector2.zero;
            totR.sizeDelta = new Vector2(-30f, 30f);

            Text totTxt = totGo.AddComponent<Text>();
            totTxt.text = "Tổng nhận được: +0 Gold";
            totTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            totTxt.fontSize = 18;
            totTxt.fontStyle = FontStyle.Bold;
            totTxt.alignment = TextAnchor.MiddleCenter;
            totTxt.color = new Color(1f, 0.85f, 0.3f);

            // Confirm & Cancel Buttons
            GameObject cRowGo = new GameObject("ConfirmRow");
            cRowGo.transform.SetParent(smGo.transform, false);
            RectTransform cRowR = cRowGo.AddComponent<RectTransform>();
            cRowR.anchorMin = new Vector2(0.5f, 0.12f);
            cRowR.anchorMax = new Vector2(0.5f, 0.12f);
            cRowR.pivot = new Vector2(0.5f, 0.5f);
            cRowR.anchoredPosition = Vector2.zero;
            cRowR.sizeDelta = new Vector2(340f, 45f);

            GameObject cfGo = new GameObject("BtnConfirm");
            cfGo.transform.SetParent(cRowGo.transform, false);
            RectTransform cfR = cfGo.AddComponent<RectTransform>();
            cfR.anchoredPosition = new Vector2(-75f, 0f);
            cfR.sizeDelta = new Vector2(170f, 42f);
            Image cfImg = cfGo.AddComponent<Image>();
            cfImg.color = new Color(0.2f, 0.65f, 0.3f);
            Button cfBtn = cfGo.AddComponent<Button>();

            GameObject cfTxtGo = new GameObject("Text");
            cfTxtGo.transform.SetParent(cfGo.transform, false);
            RectTransform cftr = cfTxtGo.AddComponent<RectTransform>();
            cftr.anchorMin = Vector2.zero;
            cftr.anchorMax = Vector2.one;
            cftr.sizeDelta = Vector2.zero;
            Text cft = cfTxtGo.AddComponent<Text>();
            cft.text = "XÁC NHẬN BÁN";
            cft.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cft.fontSize = 15;
            cft.fontStyle = FontStyle.Bold;
            cft.alignment = TextAnchor.MiddleCenter;
            cft.color = Color.white;

            GameObject ccGo = new GameObject("BtnCancel");
            ccGo.transform.SetParent(cRowGo.transform, false);
            RectTransform ccR = ccGo.AddComponent<RectTransform>();
            ccR.anchoredPosition = new Vector2(100f, 0f);
            ccR.sizeDelta = new Vector2(120f, 42f);
            Image ccImg = ccGo.AddComponent<Image>();
            ccImg.color = new Color(0.6f, 0.25f, 0.25f);
            Button ccBtn = ccGo.AddComponent<Button>();

            GameObject ccTxtGo = new GameObject("Text");
            ccTxtGo.transform.SetParent(ccGo.transform, false);
            RectTransform cctr = ccTxtGo.AddComponent<RectTransform>();
            cctr.anchorMin = Vector2.zero;
            cctr.anchorMax = Vector2.one;
            cctr.sizeDelta = Vector2.zero;
            Text cct = ccTxtGo.AddComponent<Text>();
            cct.text = "HỦY";
            cct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cct.fontSize = 15;
            cct.fontStyle = FontStyle.Bold;
            cct.alignment = TextAnchor.MiddleCenter;
            cct.color = Color.white;

            smGo.SetActive(false);

            // Bring Close Button to front
            closeGo.transform.SetAsLastSibling();

            StorageUI sui = canvasGo.AddComponent<StorageUI>();
            sui.panelRoot = panelGo;
            sui.canvasGroup = cg;
            sui.buttonClose = cb;
            sui.textCapacity = capTxt;
            sui.textWarning = warnTxt;
            sui.itemsGridContent = ctr;

            // Wire sell modal
            sui.sellModalPanel = smGo;
            sui.imageSellItemIcon = smIconImg;
            sui.textSellItemTitle = smTitleTxt;
            sui.textSellItemInfo = smInfoTxt;
            sui.textSellAmount = amtTxt;
            sui.textSellTotalGold = totTxt;
            sui.buttonSellMinus = minusBtn;
            sui.buttonSellPlus = plusBtn;
            sui.buttonSellOne = b1Btn;
            sui.buttonSellAll = bAllBtn;
            sui.buttonConfirmSell = cfBtn;
            sui.buttonCancelSell = ccBtn;
            sui.buttonSellStorage = sbBtn;

            minusBtn.onClick.AddListener(() => sui.AdjustSellAmount(-1));
            plusBtn.onClick.AddListener(() => sui.AdjustSellAmount(1));
            b1Btn.onClick.AddListener(() => sui.SetSellAmount(1));
            bAllBtn.onClick.AddListener(() => {
                if (sui.currentSellingItem != null) sui.SetSellAmount(sui.currentSellingItem.quantity);
            });
            cfBtn.onClick.AddListener(sui.ExecuteSell);
            ccBtn.onClick.AddListener(sui.CloseSellModal);

            sui.InitButtonListeners();
            sui.CloseStorage();

            // Purge any redundant top-right corner STORAGE button if present
            RemoveRedundantTopRightStorageButton();

            Instance = sui;
            Debug.Log("[StorageUI] Standard responsive Storage UI Canvas with ScrollView created successfully.");
            return sui;
        }

        private static void CreateCategoryTab(Transform parent, string filterKey, string label)
        {
            GameObject tabGo = new GameObject("Tab_" + filterKey);
            tabGo.transform.SetParent(parent, false);

            Image img = tabGo.AddComponent<Image>();
            img.color = new Color(0.20f, 0.26f, 0.35f, 0.95f);

            Button btn = tabGo.AddComponent<Button>();

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(tabGo.transform, false);
            RectTransform tr = txtGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.9f, 0.95f, 1f, 1f);

            btn.onClick.AddListener(() =>
            {
                if (Instance != null)
                {
                    Instance.SetFilter(filterKey);
                }
            });
        }

        /// <summary>
        /// Finds and removes any redundant top-right corner storage buttons so only the main menu bar button remains.
        /// </summary>
        public static void RemoveRedundantTopRightStorageButton()
        {
            GameObject[] btns = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] != null && btns[i].name == "HUD_StorageButton")
                {
                    RectTransform rt = btns[i].GetComponent<RectTransform>();
                    // Redundant top-right button was anchored to top-right corner (anchorMin.x > 0.5f)
                    if (rt != null && rt.anchorMin.x > 0.5f)
                    {
                        SafeDestroy(btns[i]);
                    }
                }
            }
        }
    }
}
