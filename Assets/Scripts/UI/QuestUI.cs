using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Modern uGUI Daily Quest Board displaying active missions, progress bars,
    /// Gold/XP rewards, and reward claiming buttons in Vietnamese.
    /// Responsive for Landscape 1920x1080.
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance { get; private set; }

        public GameObject panelRoot;
        public CanvasGroup canvasGroup;
        public Transform questListContent;
        public Text textTitle;
        public Text textDaySubtitle;
        public Button buttonClose;
        public ScrollRect scrollRect;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public static QuestUI EnsureUI()
        {
            if (Instance != null) return Instance;

            QuestUI found = FindAnyObjectByType<QuestUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("QuestUICanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 138;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Instance = canvasGo.AddComponent<QuestUI>();
            Instance.BuildUIStructure(canvasGo);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            QuestSystem.OnQuestsUpdated += RefreshUI;
        }

        void OnDestroy()
        {
            QuestSystem.OnQuestsUpdated -= RefreshUI;
            if (Instance == this) Instance = null;
        }

        private void BuildUIStructure(GameObject canvasGo)
        {
            panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(canvasGo.transform, false);

            RectTransform pr = panelRoot.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(1000f, 620f);

            Image bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            // Header
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panelRoot.transform, false);
            RectTransform hr = headerGo.AddComponent<RectTransform>();
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.anchoredPosition = Vector2.zero;
            hr.sizeDelta = new Vector2(0f, 85f);

            Image headerBg = headerGo.AddComponent<Image>();
            headerBg.color = new Color(0.18f, 0.24f, 0.32f, 1f);

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.45f);
            trt.anchorMax = new Vector2(0.8f, 1f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(30f, 0f);
            trt.sizeDelta = Vector2.zero;

            textTitle = titleGo.AddComponent<Text>();
            textTitle.text = "NHIỆM VỤ HẰNG NGÀY";
            textTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textTitle.fontSize = 24;
            textTitle.fontStyle = FontStyle.Bold;
            textTitle.color = new Color(1f, 0.88f, 0.25f);
            textTitle.alignment = TextAnchor.MiddleLeft;

            // Day Subtitle
            GameObject subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(headerGo.transform, false);
            RectTransform srt = subGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0.8f, 0.45f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.anchoredPosition = new Vector2(30f, 0f);
            srt.sizeDelta = Vector2.zero;

            textDaySubtitle = subGo.AddComponent<Text>();
            textDaySubtitle.text = "Hoàn thành nhiệm vụ trước khi sang ngày mới để nhận nhiều Vàng và EXP!";
            textDaySubtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textDaySubtitle.fontSize = 15;
            textDaySubtitle.color = new Color(0.80f, 0.90f, 1f);
            textDaySubtitle.alignment = TextAnchor.MiddleLeft;

            // Close Button
            GameObject closeGo = new GameObject("BtnClose");
            closeGo.transform.SetParent(headerGo.transform, false);
            RectTransform crt = closeGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 0.5f);
            crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(1f, 0.5f);
            crt.anchoredPosition = new Vector2(-20f, 0f);
            crt.sizeDelta = new Vector2(50f, 50f);

            Image cImg = closeGo.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.25f, 0.25f);
            buttonClose = closeGo.AddComponent<Button>();
            buttonClose.onClick.AddListener(Close);

            GameObject cTxtGo = new GameObject("Text");
            cTxtGo.transform.SetParent(closeGo.transform, false);
            RectTransform ctrt = cTxtGo.AddComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;
            Text cTxt = cTxtGo.AddComponent<Text>();
            cTxt.text = "✕";
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cTxt.fontSize = 22;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;

            // Scroll View for Quests
            GameObject scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panelRoot.transform, false);
            RectTransform svrt = scrollGo.AddComponent<RectTransform>();
            svrt.anchorMin = new Vector2(0f, 0f);
            svrt.anchorMax = new Vector2(1f, 1f);
            svrt.offsetMin = new Vector2(25f, 25f);
            svrt.offsetMax = new Vector2(-25f, -95f);

            ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 30f;
            scrollRect = sr;

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
            sr.viewport = vprt;

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            RectTransform cr = contentGo.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(0f, 500f);

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.content = cr;
            questListContent = contentGo.transform;

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
            handleImg.color = new Color(0.35f, 0.55f, 0.75f, 0.85f);

            Scrollbar sbComp = sbGo.AddComponent<Scrollbar>();
            sbComp.direction = Scrollbar.Direction.BottomToTop;
            sbComp.handleRect = handleRect;

            sr.verticalScrollbar = sbComp;

            SetWindowState(false);
        }

        public void ResetScrollPosition()
        {
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Open()
        {
            if (ShopUI.Instance != null) ShopUI.Instance.Close();
            if (StorageUI.Instance != null) StorageUI.Instance.Close();
            if (AnimalUI.Instance != null) AnimalUI.Instance.Close();
            if (CropSelectionUI.Instance != null) CropSelectionUI.Instance.Close();
            if (AchievementUI.Instance != null) AchievementUI.Instance.Close();

            SetWindowState(true);
            RefreshUI();
            ResetScrollPosition();
        }

        public void Close()
        {
            SetWindowState(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void SetWindowState(bool active)
        {
            if (panelRoot != null) panelRoot.SetActive(active);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
        }

        public void RefreshUI()
        {
            if (!IsOpen || questListContent == null) return;

            int currentDay = WeatherSystem.Instance != null ? WeatherSystem.Instance.currentDay : 1;
            if (textDaySubtitle != null)
            {
                string weatherName = WeatherSystem.Instance != null ? WeatherSystem.GetWeatherDisplayName(WeatherSystem.Instance.currentWeather) : "Nắng ấm";
                textDaySubtitle.text = $"Ngày {currentDay}  •  {weatherName}  •  Hoàn thành để nhận Vàng và EXP!";
            }

            // Clear old items
            for (int i = questListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(questListContent.GetChild(i).gameObject);
            }

            List<QuestData> quests = QuestSystem.Instance != null ? QuestSystem.Instance.activeQuests : new List<QuestData>();

            foreach (var q in quests)
            {
                CreateQuestCard(q);
            }

            if (panelRoot != null)
            {
                FarmUITheme.StyleAllTextUnder(panelRoot.transform);
            }
        }

        private void CreateQuestCard(QuestData q)
        {
            GameObject cardGo = new GameObject("QuestCard_" + q.questId);
            cardGo.transform.SetParent(questListContent, false);

            LayoutElement le = cardGo.AddComponent<LayoutElement>();
            le.minHeight = 88f;
            le.preferredHeight = 88f;

            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.color = q.isClaimed ? new Color(0.18f, 0.22f, 0.28f, 0.8f) :
                           (q.IsCompleted ? new Color(0.16f, 0.32f, 0.24f, 0.95f) : new Color(0.18f, 0.24f, 0.34f, 0.95f));

            // Title & Description
            GameObject infoGo = new GameObject("Info");
            infoGo.transform.SetParent(cardGo.transform, false);
            RectTransform irt = infoGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(0.55f, 1f);
            irt.offsetMin = new Vector2(16f, 8f);
            irt.offsetMax = new Vector2(-8f, -8f);

            Text titleTxt = infoGo.AddComponent<Text>();
            titleTxt.text = $"{q.title}\n<color=#B0D4F0><size=13>{q.description}</size></color>";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 17;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.supportRichText = true;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = q.IsCompleted ? new Color(1f, 0.92f, 0.4f) : Color.white;

            // Progress Bar & Text
            GameObject progGo = new GameObject("ProgressContainer");
            progGo.transform.SetParent(cardGo.transform, false);
            RectTransform prt = progGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.55f, 0.25f);
            prt.anchorMax = new Vector2(0.78f, 0.75f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            GameObject barBgGo = new GameObject("BarBg");
            barBgGo.transform.SetParent(progGo.transform, false);
            RectTransform bbrt = barBgGo.AddComponent<RectTransform>();
            bbrt.anchorMin = new Vector2(0f, 0f);
            bbrt.anchorMax = new Vector2(1f, 0.5f);
            bbrt.offsetMin = Vector2.zero;
            bbrt.offsetMax = Vector2.zero;
            barBgGo.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f);

            GameObject barFillGo = new GameObject("BarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            RectTransform bfrt = barFillGo.AddComponent<RectTransform>();
            bfrt.anchorMin = Vector2.zero;
            bfrt.anchorMax = Vector2.one;
            bfrt.offsetMin = Vector2.zero;
            bfrt.offsetMax = Vector2.zero;
            Image barFill = barFillGo.AddComponent<Image>();
            barFill.color = new Color(0.25f, 0.85f, 0.45f);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = q.ProgressRatio;

            GameObject pTxtGo = new GameObject("ProgressText");
            pTxtGo.transform.SetParent(progGo.transform, false);
            RectTransform ptrt = pTxtGo.AddComponent<RectTransform>();
            ptrt.anchorMin = new Vector2(0f, 0.5f);
            ptrt.anchorMax = new Vector2(1f, 1f);
            ptrt.offsetMin = Vector2.zero;
            ptrt.offsetMax = Vector2.zero;
            Text pTxt = pTxtGo.AddComponent<Text>();
            pTxt.text = $"Tiến độ: {q.currentCount}/{q.targetCount}";
            pTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pTxt.fontSize = 14;
            pTxt.fontStyle = FontStyle.Bold;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = Color.white;

            // Action / Reward Button
            GameObject btnGo = new GameObject("BtnClaim");
            btnGo.transform.SetParent(cardGo.transform, false);
            RectTransform brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.80f, 0.18f);
            brt.anchorMax = new Vector2(0.98f, 0.82f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            Image btnImg = btnGo.AddComponent<Image>();
            Button btn = btnGo.AddComponent<Button>();

            GameObject bTxtGo = new GameObject("Text");
            bTxtGo.transform.SetParent(btnGo.transform, false);
            RectTransform btrt = bTxtGo.AddComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = Vector2.zero;
            btrt.offsetMax = Vector2.zero;
            Text btnTxt = bTxtGo.AddComponent<Text>();
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 14;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;

            if (q.isClaimed)
            {
                btnImg.color = new Color(0.35f, 0.40f, 0.45f);
                btnTxt.text = "ĐÃ NHẬN";
                btnTxt.color = new Color(0.75f, 0.75f, 0.75f);
                btn.interactable = false;
            }
            else if (q.IsCompleted)
            {
                btnImg.color = new Color(0.20f, 0.75f, 0.35f);
                btnTxt.text = $"NHẬN\n+{q.rewardGold}G / +{q.rewardXP}XP";
                btnTxt.color = Color.white;
                btn.interactable = true;
                string qId = q.questId;
                btn.onClick.AddListener(() =>
                {
                    QuestSystem.Instance?.ClaimReward(qId);
                });
            }
            else
            {
                btnImg.color = new Color(0.26f, 0.30f, 0.38f);
                btnTxt.text = $"+{q.rewardGold}G • +{q.rewardXP}XP\n(Chưa xong)";
                btnTxt.color = new Color(0.8f, 0.85f, 0.9f);
                btn.interactable = false;
            }
        }
    }
}
