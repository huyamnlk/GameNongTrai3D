using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Management UI for Farm Buildings (Cow Barn, Chicken Coop, Pig Pen).
    /// Displays animal roster, hunger/happiness levels, Feed All, Collect All, and Buy Animal actions.
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        public static BuildingUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Text textTitle;
        public Text textOccupancy;
        public Text textAnimalList;
        public Button buttonFeedAll;
        public Button buttonCollectAll;
        public Button buttonBuyAnimal;
        public Button buttonSellBuilding;
        public Button buttonClose;

        [Header("Confirm Sell Modal")]
        public GameObject confirmSellModal;
        public Text textConfirmPrompt;
        public Button buttonConfirmSellYes;
        public Button buttonConfirmSellNo;

        private FarmBuilding activeBuilding;

        public static BuildingUI EnsureUI()
        {
            if (Instance != null) return Instance;

            BuildingUI found = FindAnyObjectByType<BuildingUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("BuildingUICanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Instance = canvasGo.AddComponent<BuildingUI>();
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
            rt.sizeDelta = new Vector2(740f, 520f);

            Image bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.28f, 0.36f, 0.98f);

            canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            // Dedicated Top Header Bar (Height: 60px)
            GameObject headerBar = new GameObject("HeaderBar");
            headerBar.transform.SetParent(panelRoot.transform, false);
            RectTransform hrt = headerBar.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(0f, 60f);

            Image hbg = headerBar.AddComponent<Image>();
            hbg.color = new Color(0.17f, 0.22f, 0.29f, 0.98f);

            // Left-aligned Title (Width restricted so it never reaches the right-side buttons)
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerBar.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.offsetMin = new Vector2(25f, 0f);
            trt.offsetMax = new Vector2(-235f, 0f); // 235px clear margin for Sell and Close buttons

            textTitle = titleGo.AddComponent<Text>();
            textTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textTitle.fontSize = 20;
            textTitle.fontStyle = FontStyle.Bold;
            textTitle.alignment = TextAnchor.MiddleLeft;
            textTitle.color = new Color(1f, 0.85f, 0.3f);

            Outline titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Button Sell Building (Right side of HeaderBar, left of Close button)
            buttonSellBuilding = CreateHeaderButton(headerBar.transform, "BÁN CHUỒNG", new Vector2(-65f, 0f), new Vector2(145f, 38f), new Color(0.82f, 0.28f, 0.22f));
            buttonSellBuilding.onClick.AddListener(OnSellBuildingClicked);

            // Button Close "X" (Top right corner of HeaderBar)
            buttonClose = CreateHeaderButton(headerBar.transform, "X", new Vector2(-15f, 0f), new Vector2(40f, 38f), new Color(0.85f, 0.22f, 0.2f));
            buttonClose.onClick.AddListener(Close);

            // Occupancy Info Bar below Header
            GameObject occGo = new GameObject("Occupancy");
            occGo.transform.SetParent(panelRoot.transform, false);
            RectTransform occrt = occGo.AddComponent<RectTransform>();
            occrt.anchorMin = new Vector2(0f, 1f);
            occrt.anchorMax = new Vector2(1f, 1f);
            occrt.pivot = new Vector2(0.5f, 1f);
            occrt.anchoredPosition = new Vector2(0f, -72f);
            occrt.sizeDelta = new Vector2(-50f, 28f);

            textOccupancy = occGo.AddComponent<Text>();
            textOccupancy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textOccupancy.fontSize = 16;
            textOccupancy.alignment = TextAnchor.MiddleCenter;
            textOccupancy.color = new Color(0.85f, 0.95f, 1f);

            // Animal List Container with ScrollRect
            GameObject listGo = new GameObject("AnimalList");
            listGo.transform.SetParent(panelRoot.transform, false);
            RectTransform listrt = listGo.AddComponent<RectTransform>();
            listrt.anchorMin = new Vector2(0.04f, 0.20f);
            listrt.anchorMax = new Vector2(0.96f, 0.82f);
            listrt.sizeDelta = Vector2.zero;

            Image listBg = listGo.AddComponent<Image>();
            listBg.color = new Color(0.16f, 0.20f, 0.27f, 0.90f);

            ScrollRect listScroll = listGo.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listScroll.vertical = true;
            listScroll.movementType = ScrollRect.MovementType.Clamped;
            listScroll.inertia = true;
            listScroll.decelerationRate = 0.135f;
            listScroll.scrollSensitivity = 28f;

            GameObject vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(listGo.transform, false);
            RectTransform vprt = vpGo.AddComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.offsetMin = new Vector2(10f, 10f);
            vprt.offsetMax = new Vector2(-10f, -10f);

            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            vpGo.AddComponent<RectMask2D>();
            listScroll.viewport = vprt;

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(vpGo.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.sizeDelta = new Vector2(0f, 200f);

            textAnimalList = textGo.AddComponent<Text>();
            if (textAnimalList != null)
            {
                textAnimalList.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textAnimalList.fontSize = 16;
                textAnimalList.alignment = TextAnchor.UpperLeft;
                textAnimalList.color = Color.white;
            }

            ContentSizeFitter textCsf = textGo.AddComponent<ContentSizeFitter>();
            textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            listScroll.content = textRt;

            // Bottom Action Row
            buttonFeedAll = CreateBottomButton(panelRoot.transform, "CHO ĂN TẤT CẢ", new Vector2(-225f, 40f), new Vector2(195f, 48f), new Color(0.25f, 0.72f, 0.38f));
            buttonFeedAll.onClick.AddListener(OnFeedAllClicked);

            buttonCollectAll = CreateBottomButton(panelRoot.transform, "THU HOẠCH SẢN PHẨM", new Vector2(0f, 40f), new Vector2(210f, 48f), new Color(1.0f, 0.70f, 0.15f));
            buttonCollectAll.onClick.AddListener(OnCollectAllClicked);

            buttonBuyAnimal = CreateBottomButton(panelRoot.transform, "+ MUA VẬT NUÔI", new Vector2(225f, 40f), new Vector2(195f, 48f), new Color(0.25f, 0.60f, 0.95f));
            buttonBuyAnimal.onClick.AddListener(OnBuyAnimalClicked);

            CreateConfirmSellModal(panelRoot.transform);

            Close();
        }

        private Button CreateHeaderButton(Transform parent, string label, Vector2 posFromRight, Vector2 size, Color color)
        {
            GameObject bGo = new GameObject("Btn_" + label);
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = posFromRight;
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
            txt.fontSize = (label == "X") ? 18 : 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private Button CreateBottomButton(Transform parent, string label, Vector2 posFromBottom, Vector2 size, Color color)
        {
            GameObject bGo = new GameObject("Btn_" + label);
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = posFromBottom;
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
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        public static void OpenBuilding(FarmBuilding building)
        {
            EnsureUI();
            if (Instance == null) return;
            if (!FarmBuilding.IsValidBuilding(building)) return;

            // Close other UI panels to prevent overlap
            if (ShopUI.Instance != null) ShopUI.Instance.Close();
            if (MachineUI.Instance != null) MachineUI.Instance.Close();
            if (StorageUI.Instance != null) StorageUI.Instance.Close();

            Instance.activeBuilding = building;
            Instance.panelRoot.SetActive(true);
            Instance.canvasGroup.alpha = 1f;
            Instance.canvasGroup.interactable = true;
            Instance.canvasGroup.blocksRaycasts = true;
            Instance.RefreshDisplay();
        }

        public void Close()
        {
            activeBuilding = null;
            CloseConfirmModal();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (BuildingPlacementController.Instance != null)
            {
                BuildingPlacementController.Instance.ClearBuildingHighlight();
            }
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }

                    if (Camera.main != null)
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                        {
                            FarmBuilding hitBuilding = hit.collider.GetComponentInParent<FarmBuilding>();
                            if (hitBuilding != null && hitBuilding == activeBuilding)
                            {
                                return;
                            }
                        }
                    }

                    Close();
                }
            }
        }

        public void RefreshDisplay()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding))
            {
                Close();
                return;
            }

            string typeName = (activeBuilding.buildingTypeEnum == BuildingType.CowBarn || activeBuilding.buildingType == "cow_barn" || activeBuilding.buildingId == "cow_barn") ? "COW BARN / CHUỒNG BÒ" :
                              (activeBuilding.buildingTypeEnum == BuildingType.ChickenCoop || activeBuilding.buildingType == "chicken_coop" || activeBuilding.buildingId == "chicken_coop") ? "CHICKEN COOP / CHUỒNG GÀ" :
                              (activeBuilding.buildingTypeEnum == BuildingType.PigPen || activeBuilding.buildingType == "pig_pen" || activeBuilding.buildingId == "pig_pen") ? "PIG PEN / CHUỒNG LỢN" :
                              (activeBuilding.buildingTypeEnum == BuildingType.HorseStable || activeBuilding.buildingType == "horse_stable" || activeBuilding.buildingId == "horse_stable") ? "HORSE STABLE / CHUỒNG NGỰA" :
                              activeBuilding.displayName.ToUpper();
            if (textTitle != null) textTitle.text = typeName;

            int count = activeBuilding.GetOccupantCount();
            int maxCap = activeBuilding.capacity;
            string allowedAnimalStr = (activeBuilding.allowedAnimalType == AnimalType.Cow) ? "Cow / Bò" :
                                      (activeBuilding.allowedAnimalType == AnimalType.Chicken) ? "Chicken / Gà" :
                                      (activeBuilding.allowedAnimalType == AnimalType.Pig) ? "Pig / Lợn" :
                                      (activeBuilding.allowedAnimalType == AnimalType.Horse) ? "Horse / Ngựa" :
                                      activeBuilding.allowedAnimalType.ToString();

            if (textOccupancy != null)
            {
                textOccupancy.text = $"Số lượng: {count}/{maxCap}  |  Vật nuôi cho phép: {allowedAnimalStr}";
            }

            // Update Buy Animal button label & state
            if (buttonBuyAnimal != null)
            {
                Text buyBtnText = buttonBuyAnimal.GetComponentInChildren<Text>();
                string animalLabel = "+ MUA BÒ (100 Gold)";
                if (activeBuilding.allowedAnimalType == AnimalType.Chicken || activeBuilding.buildingTypeEnum == BuildingType.ChickenCoop || activeBuilding.buildingType == "chicken_coop")
                    animalLabel = "+ MUA GÀ (60 Gold)";
                else if (activeBuilding.allowedAnimalType == AnimalType.Pig || activeBuilding.buildingTypeEnum == BuildingType.PigPen || activeBuilding.buildingType == "pig_pen")
                    animalLabel = "+ MUA LỢN (150 Gold)";
                else if (activeBuilding.allowedAnimalType == AnimalType.Horse || activeBuilding.buildingTypeEnum == BuildingType.HorseStable || activeBuilding.buildingType == "horse_stable")
                    animalLabel = "+ MUA NGỰA (300 Gold)";

                if (count >= maxCap)
                {
                    if (buyBtnText != null) buyBtnText.text = $"CHUỒNG ĐÃ ĐẦY ({count}/{maxCap})";
                    buttonBuyAnimal.interactable = false;
                }
                else
                {
                    if (buyBtnText != null) buyBtnText.text = animalLabel;
                    buttonBuyAnimal.interactable = true;
                }
            }

            if (buttonSellBuilding != null)
            {
                int refund = FarmDatabase.GetBuildingRefundGold(activeBuilding.buildingId);
                Text sbText = buttonSellBuilding.GetComponentInChildren<Text>();
                if (sbText != null) sbText.text = $"BÁN (+{refund} G)";
            }

            if (textAnimalList != null)
            {
                if (count == 0)
                {
                    textAnimalList.text = "   Chuồng đang trống! Bấm nút bên dưới để mua và đón vật nuôi vào chuồng.";
                }
                else
                {
                    string info = "";
                    for (int i = 0; i < activeBuilding.occupants.Count; i++)
                    {
                        AnimalCareController a = activeBuilding.occupants[i];
                        if (a == null || !a) continue;
                        string status = a.isProductReady ? $"★ {a.productName} SẴN SÀNG!" : $"Đang sản xuất ({Mathf.CeilToInt(a.productionDuration - a.productTimer)}s)";
                        info += $" • {a.animalName}: No bụng {(int)a.hunger}% | Vui vẻ {(int)a.happiness}% | {status}\n";
                    }
                    textAnimalList.text = info;
                }
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void OnFeedAllClicked()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding) || activeBuilding.occupants.Count == 0)
            {
                CropSelectionUI.ShowToast("Không có vật nuôi nào trong chuồng!");
                return;
            }

            int fedCount = 0;
            string feedId = (activeBuilding.buildingType == "chicken_coop") ? "grain" : "hay";

            foreach (var a in activeBuilding.occupants)
            {
                if (a != null && a && a.hunger < 90f)
                {
                    if (a.Feed(feedId)) fedCount++;
                }
            }

            if (fedCount > 0)
            {
                CropSelectionUI.ShowToast($"Đã cho {fedCount} vật nuôi ăn {feedId}!");
            }
            else
            {
                CropSelectionUI.ShowToast("Tất cả vật nuôi đã no bụng hoặc kho không đủ thức ăn!");
            }
            RefreshDisplay();
        }

        private void OnCollectAllClicked()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding)) return;

            int collected = 0;
            foreach (var a in activeBuilding.occupants)
            {
                if (a != null && a && a.isProductReady)
                {
                    if (a.CollectProduct()) collected++;
                }
            }

            if (collected > 0)
            {
                CropSelectionUI.ShowToast($"Đã thu hoạch thành công sản phẩm từ {collected} vật nuôi!");
            }
            else
            {
                CropSelectionUI.ShowToast("Chưa có sản phẩm nào sẵn sàng thu hoạch!");
            }
            RefreshDisplay();
        }

        private void OnBuyAnimalClicked()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding)) return;

            if (FarmBuildingManager.Instance != null)
            {
                FarmBuildingManager.Instance.BuyAndSpawnAnimalForBuilding(activeBuilding);
            }
            RefreshDisplay();
        }

        private void OnSellBuildingClicked()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding)) return;

            if (activeBuilding.GetOccupantCount() > 0)
            {
                CropSelectionUI.ShowToast("Hãy chuyển hoặc bán hết vật nuôi trước khi bán công trình!");
                return;
            }

            int refund = FarmDatabase.GetBuildingRefundGold(activeBuilding.buildingId);
            if (textConfirmPrompt != null)
            {
                textConfirmPrompt.text = $"Bạn có chắc muốn bán công trình này không?\n{activeBuilding.displayName}\n(Hoàn lại: +{refund} Gold)";
            }

            if (confirmSellModal != null)
            {
                confirmSellModal.SetActive(true);
                confirmSellModal.transform.SetAsLastSibling();
            }
        }

        private void ExecuteSellBuilding()
        {
            if (!FarmBuilding.IsValidBuilding(activeBuilding))
            {
                Close();
                return;
            }

            if (activeBuilding.GetOccupantCount() > 0)
            {
                CropSelectionUI.ShowToast("Hãy chuyển hoặc bán hết vật nuôi trước khi bán công trình!");
                CloseConfirmModal();
                return;
            }

            int refund = FarmDatabase.GetBuildingRefundGold(activeBuilding.buildingId);
            string dName = activeBuilding.displayName;

            if (FarmBuildingManager.Instance != null)
            {
                FarmBuildingManager.Instance.RemoveBuilding(activeBuilding);
            }

            PlayerProgression.AddGold(refund, $"Bán công trình {dName}");
            CropSelectionUI.ShowToast($"Đã bán thành công {dName}!\n+{refund} Gold");
            SaveLoadSystem.SaveGame();

            CloseConfirmModal();
            Close();
        }

        public void CloseConfirmModal()
        {
            if (confirmSellModal != null) confirmSellModal.SetActive(false);
        }

        private void CreateConfirmSellModal(Transform parent)
        {
            confirmSellModal = new GameObject("ConfirmSellModal");
            confirmSellModal.transform.SetParent(parent, false);

            RectTransform rt = confirmSellModal.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(500f, 260f);

            Image img = confirmSellModal.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            img.raycastTarget = true;

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(confirmSellModal.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f);
            trt.sizeDelta = new Vector2(-30f, 35f);

            Text tt = titleGo.AddComponent<Text>();
            tt.text = "XÁC NHẬN BÁN CÔNG TRÌNH";
            tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 20;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = new Color(1f, 0.85f, 0.35f);

            // Prompt
            GameObject pGo = new GameObject("Prompt");
            pGo.transform.SetParent(confirmSellModal.transform, false);
            RectTransform prt = pGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.45f);
            prt.anchorMax = new Vector2(1f, 0.8f);
            prt.sizeDelta = Vector2.zero;

            textConfirmPrompt = pGo.AddComponent<Text>();
            textConfirmPrompt.text = "Bạn có chắc muốn bán công trình này không?";
            textConfirmPrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textConfirmPrompt.fontSize = 16;
            textConfirmPrompt.alignment = TextAnchor.MiddleCenter;
            textConfirmPrompt.color = Color.white;

            // Buttons: [XÁC NHẬN BÁN] [HỦY]
            buttonConfirmSellYes = CreateSimpleButton(confirmSellModal.transform, "XÁC NHẬN", new Vector2(-80f, -70f), new Vector2(150f, 45f), new Color(0.2f, 0.65f, 0.3f));
            buttonConfirmSellYes.onClick.AddListener(ExecuteSellBuilding);

            buttonConfirmSellNo = CreateSimpleButton(confirmSellModal.transform, "HỦY", new Vector2(90f, -70f), new Vector2(120f, 45f), new Color(0.6f, 0.25f, 0.25f));
            buttonConfirmSellNo.onClick.AddListener(CloseConfirmModal);

            confirmSellModal.SetActive(false);
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}
