using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Displays Player Level, XP Bar, Gold/Cash, and floating '+XP/+Gold' popups.
    /// Fully responsive for Landscape 1920x1080.
    /// </summary>
    public class ProgressionUI : MonoBehaviour
    {
        public static ProgressionUI Instance { get; private set; }

        [Header("UI References")]
        public Text textLevel;
        public Text textXPProgress;
        public Image imageXPBarFill;
        public Text textGold;
        public Text textFloatingPopup;
        public GameObject levelUpBanner;
        public Text textLevelUp;
        public Text textWeatherDay;
        private GameObject weatherModal;
        private GameObject settingsModal;

        private Coroutine popupCoroutine;
        private Coroutine levelUpCoroutine;

        void Awake()
        {
            if (Instance == null) Instance = this;

            PlayerProgression.OnXPChanged += UpdateXPDisplay;
            PlayerProgression.OnGoldChanged += UpdateGoldDisplay;
            PlayerProgression.OnXPAwarded += ShowXPAwardedPopup;
            PlayerProgression.OnLevelUp += ShowLevelUpBanner;
            WeatherSystem.OnDayOrWeatherChanged += UpdateWeatherDayDisplay;
        }

        void Start()
        {
            if (PlayerProgression.Instance != null)
            {
                UpdateXPDisplay(PlayerProgression.Instance.CurrentXP, PlayerProgression.Instance.XPToNextLevel, PlayerProgression.Instance.Level);
                UpdateGoldDisplay(PlayerProgression.Instance.Gold);
            }
            else
            {
                UpdateXPDisplay(0, 50, 1);
                UpdateGoldDisplay(100);
            }

            if (WeatherSystem.Instance != null)
            {
                UpdateWeatherDayDisplay(WeatherSystem.Instance.CurrentDay, WeatherSystem.Instance.CurrentWeather);
            }
            else
            {
                UpdateWeatherDayDisplay(1, WeatherType.Sunny);
            }

            if (textFloatingPopup != null) textFloatingPopup.gameObject.SetActive(false);
            if (levelUpBanner != null) levelUpBanner.SetActive(false);

            FarmUITheme.StyleAllTextUnder(transform);
        }

        void OnDestroy()
        {
            PlayerProgression.OnXPChanged -= UpdateXPDisplay;
            PlayerProgression.OnGoldChanged -= UpdateGoldDisplay;
            PlayerProgression.OnXPAwarded -= ShowXPAwardedPopup;
            PlayerProgression.OnLevelUp -= ShowLevelUpBanner;
            WeatherSystem.OnDayOrWeatherChanged -= UpdateWeatherDayDisplay;
            if (Instance == this) Instance = null;
        }

        public void UpdateWeatherDayDisplay(int day, WeatherType weather)
        {
            if (textWeatherDay != null)
            {
                SeasonType season = WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentSeason : SeasonType.Spring;
                string sIcon = "🌸";
                string sName = "XUÂN";
                switch (season)
                {
                    case SeasonType.Summer: sIcon = "☀️"; sName = "HẠ"; break;
                    case SeasonType.Autumn: sIcon = "🍂"; sName = "THU"; break;
                    case SeasonType.Winter: sIcon = "❄️"; sName = "ĐÔNG"; break;
                }

                string wIcon = "☀️";
                string wName = "Nắng";
                if (weather == WeatherType.Rainy) { wIcon = "🌧️"; wName = "Mưa"; }
                else if (weather == WeatherType.Snowy) { wIcon = "❄️"; wName = "Tuyết"; }
                else if (weather == WeatherType.Cloudy) { wIcon = "☁️"; wName = "Mây"; }

                string manualIndicator = (WeatherSystem.Instance != null && WeatherSystem.Instance.isManualWeather) ? " [T.Công]" : "";
                textWeatherDay.text = $"{sIcon} {sName} • NGÀY {day}\n{wIcon} {wName}{manualIndicator}";
                textWeatherDay.font = FarmUITheme.BoldFont;
                textWeatherDay.fontSize = 15;
                textWeatherDay.fontStyle = FontStyle.Bold;
                FarmUITheme.AddShadow(textWeatherDay, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));
                FarmUITheme.EnsureBestFit(textWeatherDay, 12, 16);
            }
        }

        public void UpdateXPDisplay(int current, int required, int lvl)
        {
            if (textLevel != null)
            {
                FarmUITheme.ApplyBadge(textLevel, 18);
                textLevel.text = "CẤP " + lvl;
            }

            if (textXPProgress != null)
            {
                FarmUITheme.ApplyBody(textXPProgress, 14, false);
                textXPProgress.text = $"{current} / {required} XP";
            }

            if (imageXPBarFill != null)
            {
                float ratio = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
                imageXPBarFill.fillAmount = ratio;
            }
        }

        public void UpdateGoldDisplay(int currentGold)
        {
            if (textGold != null)
            {
                FarmUITheme.ApplyBadge(textGold, 20);
                textGold.color = FarmUITheme.GoldYellow;
                textGold.text = $"{currentGold:N0} VÀNG";
            }
        }

        public void ShowXPAwardedPopup(int amount, string source)
        {
            if (textFloatingPopup == null) return;

            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            popupCoroutine = StartCoroutine(AnimateXPPopup(amount, source));
        }

        private IEnumerator AnimateXPPopup(int amount, string source)
        {
            textFloatingPopup.gameObject.SetActive(true);
            FarmUITheme.ApplyBadge(textFloatingPopup, 20);
            textFloatingPopup.color = FarmUITheme.XPGreen;
            textFloatingPopup.text = $"+{amount} XP" + (string.IsNullOrEmpty(source) ? "" : $" ({source})");

            RectTransform rt = textFloatingPopup.rectTransform;
            Vector2 startPos = new Vector2(0f, -140f);
            Vector2 endPos = new Vector2(0f, -90f);

            float elapsed = 0f;
            float duration = 1.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            textFloatingPopup.gameObject.SetActive(false);
        }

        public void ShowLevelUpBanner(int newLevel)
        {
            if (levelUpBanner == null) return;

            if (levelUpCoroutine != null) StopCoroutine(levelUpCoroutine);
            levelUpCoroutine = StartCoroutine(AnimateLevelUpBanner(newLevel));
        }

        private IEnumerator AnimateLevelUpBanner(int newLevel)
        {
            levelUpBanner.SetActive(true);
            if (textLevelUp != null)
            {
                FarmUITheme.ApplyTitle(textLevelUp, 32);
                textLevelUp.color = FarmUITheme.GoldYellow;
                textLevelUp.text = $"★ ĐÃ LÊN CẤP {newLevel}! ★\n(Mở khóa thêm ô đất, cây trồng và máy móc mới)";
            }

            yield return new WaitForSeconds(3.5f);
            levelUpBanner.SetActive(false);
        }

        public static ProgressionUI EnsureUI()
        {
            if (Instance != null) return Instance;

            ProgressionUI found = FindAnyObjectByType<ProgressionUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject canvasGo = new GameObject("ProgressionCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // XP Widget Root Container (Top-Left)
            GameObject widget = new GameObject("XPWidget");
            widget.transform.SetParent(canvasGo.transform, false);
            RectTransform wrt = widget.AddComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0f, 1f);
            wrt.anchorMax = new Vector2(0f, 1f);
            wrt.pivot = new Vector2(0f, 1f);
            wrt.anchoredPosition = new Vector2(25f, -20f);
            wrt.sizeDelta = new Vector2(360f, 65f);

            Image wbg = widget.AddComponent<Image>();
            wbg.color = new Color(0.20f, 0.26f, 0.35f, 0.92f);
            wbg.raycastTarget = false;

            // Level Badge
            GameObject badgeGo = new GameObject("LevelBadge");
            badgeGo.transform.SetParent(widget.transform, false);
            RectTransform brt = badgeGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(8f, 0f);
            brt.sizeDelta = new Vector2(85f, -12f);

            Image bimg = badgeGo.AddComponent<Image>();
            bimg.color = new Color(0.95f, 0.65f, 0.15f);
            bimg.raycastTarget = false;

            GameObject badgeTextGo = new GameObject("Text");
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            RectTransform btrt = badgeTextGo.AddComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.sizeDelta = Vector2.zero;

            Text lvlText = badgeTextGo.AddComponent<Text>();
            lvlText.text = "CẤP 1";
            lvlText.font = FarmUITheme.BoldFont;
            lvlText.fontSize = 18;
            lvlText.fontStyle = FontStyle.Bold;
            lvlText.alignment = TextAnchor.MiddleCenter;
            lvlText.color = Color.white;
            lvlText.raycastTarget = false;
            FarmUITheme.AddShadow(lvlText, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // XP Bar Background
            GameObject barBgGo = new GameObject("XPBarBg");
            barBgGo.transform.SetParent(widget.transform, false);
            RectTransform barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0f, 0.2f);
            barBgRt.anchorMax = new Vector2(1f, 0.8f);
            barBgRt.pivot = new Vector2(0f, 0.5f);
            barBgRt.anchoredPosition = new Vector2(100f, 0f);
            barBgRt.sizeDelta = new Vector2(-115f, 0f);

            Image barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.16f, 0.20f, 0.26f);
            barBgImg.raycastTarget = false;

            // XP Bar Fill
            GameObject barFillGo = new GameObject("XPBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            RectTransform barFillRt = barFillGo.AddComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.sizeDelta = Vector2.zero;

            Image barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.color = new Color(0.25f, 0.85f, 0.45f);
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillAmount = 0.2f;
            barFillImg.raycastTarget = false;

            // XP Text Info
            GameObject xpTextGo = new GameObject("XPText");
            xpTextGo.transform.SetParent(barBgGo.transform, false);
            RectTransform xptRt = xpTextGo.AddComponent<RectTransform>();
            xptRt.anchorMin = Vector2.zero;
            xptRt.anchorMax = Vector2.one;
            xptRt.sizeDelta = Vector2.zero;

            Text xpTxt = xpTextGo.AddComponent<Text>();
            xpTxt.text = "0 / 50 XP";
            xpTxt.font = FarmUITheme.BoldFont;
            xpTxt.fontSize = 15;
            xpTxt.fontStyle = FontStyle.Bold;
            xpTxt.alignment = TextAnchor.MiddleCenter;
            xpTxt.color = Color.white;
            xpTxt.raycastTarget = false;
            FarmUITheme.AddShadow(xpTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // Gold Widget (Right next to XP widget)
            GameObject goldGo = new GameObject("GoldWidget");
            goldGo.transform.SetParent(canvasGo.transform, false);
            RectTransform grt = goldGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 1f);
            grt.anchorMax = new Vector2(0f, 1f);
            grt.pivot = new Vector2(0f, 1f);
            grt.anchoredPosition = new Vector2(400f, -20f);
            grt.sizeDelta = new Vector2(160f, 65f);

            Image gbg = goldGo.AddComponent<Image>();
            gbg.color = new Color(0.24f, 0.22f, 0.14f, 0.92f);
            gbg.raycastTarget = false;

            // Gold Label
            GameObject gLabelGo = new GameObject("GoldText");
            gLabelGo.transform.SetParent(goldGo.transform, false);
            RectTransform glrt = gLabelGo.AddComponent<RectTransform>();
            glrt.anchorMin = Vector2.zero;
            glrt.anchorMax = Vector2.one;
            glrt.sizeDelta = Vector2.zero;

            Text gTxt = gLabelGo.AddComponent<Text>();
            gTxt.text = "100 G";
            gTxt.font = FarmUITheme.BoldFont;
            gTxt.fontSize = 20;
            gTxt.fontStyle = FontStyle.Bold;
            gTxt.alignment = TextAnchor.MiddleCenter;
            gTxt.color = new Color(1.0f, 0.88f, 0.2f); // Golden yellow
            gTxt.raycastTarget = false;
            FarmUITheme.AddShadow(gTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Shop Button (Next to Gold widget)
            GameObject shopBtnGo = new GameObject("HUD_ShopButton");
            shopBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform sbrt = shopBtnGo.AddComponent<RectTransform>();
            sbrt.anchorMin = new Vector2(0f, 1f);
            sbrt.anchorMax = new Vector2(0f, 1f);
            sbrt.pivot = new Vector2(0f, 1f);
            sbrt.anchoredPosition = new Vector2(575f, -20f);
            sbrt.sizeDelta = new Vector2(130f, 65f);

            Image sImg = shopBtnGo.AddComponent<Image>();
            sImg.color = new Color(0.22f, 0.65f, 0.35f, 0.95f);
            sImg.raycastTarget = true;

            Button sBtn = shopBtnGo.AddComponent<Button>();
            sBtn.onClick.AddListener(() =>
            {
                ShopUI.OpenShop();
            });

            GameObject sTxtGo = new GameObject("Text");
            sTxtGo.transform.SetParent(shopBtnGo.transform, false);
            RectTransform strt = sTxtGo.AddComponent<RectTransform>();
            strt.anchorMin = Vector2.zero;
            strt.anchorMax = Vector2.one;
            strt.sizeDelta = Vector2.zero;

            Text stxt = sTxtGo.AddComponent<Text>();
            stxt.text = "SHOP\nCỬA HÀNG";
            stxt.font = FarmUITheme.BoldFont;
            stxt.fontSize = 15;
            stxt.fontStyle = FontStyle.Bold;
            stxt.alignment = TextAnchor.MiddleCenter;
            stxt.color = Color.white;
            stxt.raycastTarget = false;
            FarmUITheme.AddShadow(stxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Storage Button
            GameObject storBtnGo = new GameObject("HUD_StorageButton");
            storBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform storrt = storBtnGo.AddComponent<RectTransform>();
            storrt.anchorMin = new Vector2(0f, 1f);
            storrt.anchorMax = new Vector2(0f, 1f);
            storrt.pivot = new Vector2(0f, 1f);
            storrt.anchoredPosition = new Vector2(720f, -20f);
            storrt.sizeDelta = new Vector2(130f, 65f);

            Image storImg = storBtnGo.AddComponent<Image>();
            storImg.color = new Color(0.28f, 0.45f, 0.65f, 0.95f);
            storImg.raycastTarget = true;

            Button storBtn = storBtnGo.AddComponent<Button>();
            storBtn.onClick.AddListener(() =>
            {
                StorageUI sui = StorageUI.EnsureUI();
                if (sui != null)
                {
                    sui.Toggle();
                }
            });

            GameObject storTxtGo = new GameObject("Text");
            storTxtGo.transform.SetParent(storBtnGo.transform, false);
            RectTransform stortrt = storTxtGo.AddComponent<RectTransform>();
            stortrt.anchorMin = Vector2.zero;
            stortrt.anchorMax = Vector2.one;
            stortrt.sizeDelta = Vector2.zero;

            Text stortxt = storTxtGo.AddComponent<Text>();
            stortxt.text = "KHO ĐỒ\nSTORAGE";
            stortxt.font = FarmUITheme.BoldFont;
            stortxt.fontSize = 15;
            stortxt.fontStyle = FontStyle.Bold;
            stortxt.alignment = TextAnchor.MiddleCenter;
            stortxt.color = Color.white;
            stortxt.raycastTarget = false;
            FarmUITheme.AddShadow(stortxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Save Button
            GameObject saveBtnGo = new GameObject("HUD_SaveButton");
            saveBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform savert = saveBtnGo.AddComponent<RectTransform>();
            savert.anchorMin = new Vector2(0f, 1f);
            savert.anchorMax = new Vector2(0f, 1f);
            savert.pivot = new Vector2(0f, 1f);
            savert.anchoredPosition = new Vector2(865f, -20f);
            savert.sizeDelta = new Vector2(105f, 65f);

            Image saveImg = saveBtnGo.AddComponent<Image>();
            saveImg.color = new Color(0.20f, 0.55f, 0.75f, 0.95f);
            saveImg.raycastTarget = true;

            Button saveBtn = saveBtnGo.AddComponent<Button>();
            saveBtn.onClick.AddListener(() =>
            {
                SaveLoadSystem.SaveGame();
                CropSelectionUI.ShowToast("Đã lưu tiến trình nông trại thành công!");
            });

            GameObject saveTxtGo = new GameObject("Text");
            saveTxtGo.transform.SetParent(saveBtnGo.transform, false);
            RectTransform savetrt = saveTxtGo.AddComponent<RectTransform>();
            savetrt.anchorMin = Vector2.zero;
            savetrt.anchorMax = Vector2.one;
            savetrt.sizeDelta = Vector2.zero;

            Text savetxt = saveTxtGo.AddComponent<Text>();
            savetxt.text = "LƯU\nSAVE";
            savetxt.font = FarmUITheme.BoldFont;
            savetxt.fontSize = 15;
            savetxt.fontStyle = FontStyle.Bold;
            savetxt.alignment = TextAnchor.MiddleCenter;
            savetxt.color = Color.white;
            savetxt.raycastTarget = false;
            FarmUITheme.AddShadow(savetxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Load Button
            GameObject loadBtnGo = new GameObject("HUD_LoadButton");
            loadBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform loadrt = loadBtnGo.AddComponent<RectTransform>();
            loadrt.anchorMin = new Vector2(0f, 1f);
            loadrt.anchorMax = new Vector2(0f, 1f);
            loadrt.pivot = new Vector2(0f, 1f);
            loadrt.anchoredPosition = new Vector2(980f, -20f);
            loadrt.sizeDelta = new Vector2(105f, 65f);

            Image loadImg = loadBtnGo.AddComponent<Image>();
            loadImg.color = new Color(0.60f, 0.44f, 0.26f, 0.95f);
            loadImg.raycastTarget = true;

            Button loadBtn = loadBtnGo.AddComponent<Button>();
            loadBtn.onClick.AddListener(() =>
            {
                SaveLoadSystem.LoadGame();
                CropSelectionUI.ShowToast("Đã tải lại dữ liệu lưu thành công!");
            });

            GameObject loadTxtGo = new GameObject("Text");
            loadTxtGo.transform.SetParent(loadBtnGo.transform, false);
            RectTransform loadtrt = loadTxtGo.AddComponent<RectTransform>();
            loadtrt.anchorMin = Vector2.zero;
            loadtrt.anchorMax = Vector2.one;
            loadtrt.sizeDelta = Vector2.zero;

            Text loadtxt = loadTxtGo.AddComponent<Text>();
            loadtxt.text = "TẢI\nLOAD";
            loadtxt.font = FarmUITheme.BoldFont;
            loadtxt.fontSize = 15;
            loadtxt.fontStyle = FontStyle.Bold;
            loadtxt.alignment = TextAnchor.MiddleCenter;
            loadtxt.color = Color.white;
            loadtxt.raycastTarget = false;
            FarmUITheme.AddShadow(loadtxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // Requirement A: Purge any redundant Edit buttons (keep exclusively bottom-right EDIT button)
            GameObject strayEdit = GameObject.Find("HUD_EditFarmButton");
            if (strayEdit != null) Destroy(strayEdit);

            // HUD Weather & Day Widget (x = 1095f, sizeDelta = 165f, 65f)
            GameObject weatherGo = new GameObject("HUD_WeatherWidget");
            weatherGo.transform.SetParent(canvasGo.transform, false);
            RectTransform wtr = weatherGo.AddComponent<RectTransform>();
            wtr.anchorMin = new Vector2(0f, 1f);
            wtr.anchorMax = new Vector2(0f, 1f);
            wtr.pivot = new Vector2(0f, 1f);
            wtr.anchoredPosition = new Vector2(1095f, -20f);
            wtr.sizeDelta = new Vector2(165f, 65f);

            Image wImg = weatherGo.AddComponent<Image>();
            wImg.color = new Color(0.20f, 0.32f, 0.45f, 0.95f);
            wImg.raycastTarget = true;
            FarmUITheme.AddOutline(weatherGo, new Color(0.35f, 0.50f, 0.72f, 0.85f), new Vector2(1.5f, -1.5f));

            Button wBtn = weatherGo.AddComponent<Button>();
            wBtn.onClick.AddListener(() =>
            {
                if (Instance != null) Instance.OpenWeatherModal();
            });

            GameObject wTxtGo = new GameObject("Text");
            wTxtGo.transform.SetParent(weatherGo.transform, false);
            RectTransform wtrt = wTxtGo.AddComponent<RectTransform>();
            wtrt.anchorMin = Vector2.zero;
            wtrt.anchorMax = Vector2.one;
            wtrt.sizeDelta = Vector2.zero;

            Text wtxt = wTxtGo.AddComponent<Text>();
            wtxt.text = "NGÀY 1\n☀️ Nắng";
            wtxt.font = FarmUITheme.BoldFont;
            wtxt.fontSize = 15;
            wtxt.fontStyle = FontStyle.Bold;
            wtxt.alignment = TextAnchor.MiddleCenter;
            wtxt.color = Color.white;
            wtxt.raycastTarget = false;
            FarmUITheme.AddShadow(wtxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Quest Button (x = 1270f, sizeDelta = 125f, 65f)
            GameObject questBtnGo = new GameObject("HUD_QuestButton");
            questBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform qrt = questBtnGo.AddComponent<RectTransform>();
            qrt.anchorMin = new Vector2(0f, 1f);
            qrt.anchorMax = new Vector2(0f, 1f);
            qrt.pivot = new Vector2(0f, 1f);
            qrt.anchoredPosition = new Vector2(1270f, -20f);
            qrt.sizeDelta = new Vector2(125f, 65f);

            Image qImg = questBtnGo.AddComponent<Image>();
            qImg.color = new Color(0.85f, 0.52f, 0.15f, 0.95f);
            qImg.raycastTarget = true;
            FarmUITheme.AddOutline(questBtnGo, new Color(0.95f, 0.65f, 0.25f, 0.85f), new Vector2(1.5f, -1.5f));

            Button qBtn = questBtnGo.AddComponent<Button>();
            qBtn.onClick.AddListener(() =>
            {
                QuestUI qui = QuestUI.EnsureUI();
                if (qui != null) qui.Toggle();
            });

            GameObject qTxtGo = new GameObject("Text");
            qTxtGo.transform.SetParent(questBtnGo.transform, false);
            RectTransform qtrt = qTxtGo.AddComponent<RectTransform>();
            qtrt.anchorMin = Vector2.zero;
            qtrt.anchorMax = Vector2.one;
            qtrt.sizeDelta = Vector2.zero;

            Text qtxt = qTxtGo.AddComponent<Text>();
            qtxt.text = "NHIỆM VỤ\nQUESTS";
            qtxt.font = FarmUITheme.BoldFont;
            qtxt.fontSize = 15;
            qtxt.fontStyle = FontStyle.Bold;
            qtxt.alignment = TextAnchor.MiddleCenter;
            qtxt.color = Color.white;
            qtxt.raycastTarget = false;
            FarmUITheme.AddShadow(qtxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Achievement Button (x = 1405f, sizeDelta = 135f, 65f)
            GameObject achBtnGo = new GameObject("HUD_AchievementButton");
            achBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform achrt = achBtnGo.AddComponent<RectTransform>();
            achrt.anchorMin = new Vector2(0f, 1f);
            achrt.anchorMax = new Vector2(0f, 1f);
            achrt.pivot = new Vector2(0f, 1f);
            achrt.anchoredPosition = new Vector2(1405f, -20f);
            achrt.sizeDelta = new Vector2(135f, 65f);

            Image achImg = achBtnGo.AddComponent<Image>();
            achImg.color = new Color(0.55f, 0.25f, 0.70f, 0.95f);
            achImg.raycastTarget = true;
            FarmUITheme.AddOutline(achBtnGo, new Color(0.70f, 0.35f, 0.85f, 0.85f), new Vector2(1.5f, -1.5f));

            Button achBtn = achBtnGo.AddComponent<Button>();
            achBtn.onClick.AddListener(() =>
            {
                AchievementUI aui = AchievementUI.EnsureUI();
                if (aui != null) aui.Toggle();
            });

            GameObject achTxtGo = new GameObject("Text");
            achTxtGo.transform.SetParent(achBtnGo.transform, false);
            RectTransform achtrt = achTxtGo.AddComponent<RectTransform>();
            achtrt.anchorMin = Vector2.zero;
            achtrt.anchorMax = Vector2.one;
            achtrt.sizeDelta = Vector2.zero;

            Text achtxt = achTxtGo.AddComponent<Text>();
            achtxt.text = "THÀNH TỰU\nACHIEVE";
            achtxt.font = FarmUITheme.BoldFont;
            achtxt.fontSize = 15;
            achtxt.fontStyle = FontStyle.Bold;
            achtxt.alignment = TextAnchor.MiddleCenter;
            achtxt.color = Color.white;
            achtxt.raycastTarget = false;
            FarmUITheme.AddShadow(achtxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // HUD Audio Settings Button (x = 1550f, sizeDelta = 120f, 65f)
            GameObject settingsBtnGo = new GameObject("HUD_SettingsButton");
            settingsBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform srt = settingsBtnGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(1550f, -20f);
            srt.sizeDelta = new Vector2(120f, 65f);

            Image setImg = settingsBtnGo.AddComponent<Image>();
            setImg.color = new Color(0.35f, 0.40f, 0.50f, 0.95f);
            setImg.raycastTarget = true;
            FarmUITheme.AddOutline(settingsBtnGo, new Color(0.50f, 0.55f, 0.65f, 0.85f), new Vector2(1.5f, -1.5f));

            Button setBtn = settingsBtnGo.AddComponent<Button>();
            setBtn.onClick.AddListener(() =>
            {
                if (Instance != null) Instance.OpenSettingsModal();
            });

            GameObject setTxtGo = new GameObject("Text");
            setTxtGo.transform.SetParent(settingsBtnGo.transform, false);
            RectTransform setRt = setTxtGo.AddComponent<RectTransform>();
            setRt.anchorMin = Vector2.zero;
            setRt.anchorMax = Vector2.one;
            setRt.sizeDelta = Vector2.zero;

            Text setTxt = setTxtGo.AddComponent<Text>();
            setTxt.text = "CÀI ĐẶT\nAUDIO";
            setTxt.font = FarmUITheme.BoldFont;
            setTxt.fontSize = 15;
            setTxt.fontStyle = FontStyle.Bold;
            setTxt.alignment = TextAnchor.MiddleCenter;
            setTxt.color = Color.white;
            setTxt.raycastTarget = false;
            FarmUITheme.AddShadow(setTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // Floating Popup Text (+XP)
            GameObject floatGo = new GameObject("FloatingXP");
            floatGo.transform.SetParent(canvasGo.transform, false);
            RectTransform frt = floatGo.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 1f);
            frt.anchorMax = new Vector2(0f, 1f);
            frt.pivot = new Vector2(0f, 1f);
            frt.anchoredPosition = new Vector2(25f, -90f);
            frt.sizeDelta = new Vector2(300f, 40f);

            Text floatTxt = floatGo.AddComponent<Text>();
            floatTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            floatTxt.fontSize = 20;
            floatTxt.fontStyle = FontStyle.Bold;
            floatTxt.alignment = TextAnchor.MiddleLeft;
            floatTxt.color = new Color(1f, 0.9f, 0.2f);
            floatTxt.raycastTarget = false;
            floatGo.SetActive(false);

            // Level Up Banner (Center Screen)
            GameObject bannerGo = new GameObject("LevelUpBanner");
            bannerGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bnrt = bannerGo.AddComponent<RectTransform>();
            bnrt.anchorMin = new Vector2(0.5f, 0.5f);
            bnrt.anchorMax = new Vector2(0.5f, 0.5f);
            bnrt.pivot = new Vector2(0.5f, 0.5f);
            bnrt.anchoredPosition = Vector2.zero;
            bnrt.sizeDelta = new Vector2(500f, 180f);

            Image bnImg = bannerGo.AddComponent<Image>();
            bnImg.color = new Color(0.1f, 0.2f, 0.1f, 0.95f);
            bnImg.raycastTarget = false;

            GameObject bnTextGo = new GameObject("BannerText");
            bnTextGo.transform.SetParent(bannerGo.transform, false);
            RectTransform bntRt = bnTextGo.AddComponent<RectTransform>();
            bntRt.anchorMin = Vector2.zero;
            bntRt.anchorMax = Vector2.one;
            bntRt.sizeDelta = Vector2.zero;

            Text bnTxt = bnTextGo.AddComponent<Text>();
            bnTxt.text = "LÊN CẤP!\nLEVEL UP!";
            bnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bnTxt.fontSize = 32;
            bnTxt.fontStyle = FontStyle.Bold;
            bnTxt.alignment = TextAnchor.MiddleCenter;
            bnTxt.color = new Color(1f, 0.95f, 0.3f);
            bnTxt.raycastTarget = false;
            bannerGo.SetActive(false);

            ProgressionUI ui = canvasGo.AddComponent<ProgressionUI>();
            ui.textLevel = lvlText;
            ui.textXPProgress = xpTxt;
            ui.imageXPBarFill = barFillImg;
            ui.textGold = gTxt;
            ui.textWeatherDay = wtxt;
            ui.textFloatingPopup = floatTxt;
            ui.levelUpBanner = bannerGo;
            ui.textLevelUp = bnTxt;

            FarmUITheme.StyleAllTextUnder(canvasGo.transform);

            Instance = ui;
            return ui;
        }

        public void OpenWeatherModal()
        {
            if (weatherModal != null)
            {
                weatherModal.SetActive(true);
                return;
            }

            // Build Weather & Season modal panel
            weatherModal = new GameObject("Weather_Selection_Modal");
            weatherModal.transform.SetParent(transform, false);

            RectTransform mrt = weatherModal.AddComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(540f, 440f);

            Image bg = weatherModal.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.14f, 0.20f, 0.98f);
            FarmUITheme.AddOutline(weatherModal, new Color(0.30f, 0.45f, 0.65f, 0.85f), new Vector2(2f, 2f));

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(weatherModal.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -14f);
            trt.sizeDelta = new Vector2(0f, 42f);

            Text tTxt = titleGo.AddComponent<Text>();
            tTxt.text = "CÀI ĐẶT THỜI TIẾT & BỐN MÙA";
            tTxt.font = FarmUITheme.BoldFont;
            tTxt.fontSize = 22;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(1f, 0.88f, 0.25f);
            FarmUITheme.AddShadow(tTxt, FarmUITheme.ColorShadowDark, new Vector2(1.5f, -1.5f));

            // Subtitle
            GameObject subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(weatherModal.transform, false);
            RectTransform srt = subGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -54f);
            srt.sizeDelta = new Vector2(0f, 28f);

            Text subTxt = subGo.AddComponent<Text>();
            string sName = WeatherSystem.Instance != null ? WeatherSystem.GetSeasonDisplayName(WeatherSystem.Instance.CurrentSeason) : "Mùa Xuân";
            subTxt.text = $"Hiện tại: {sName} • Ngày {(WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentDay : 1)}";
            subTxt.font = FarmUITheme.BoldFont;
            subTxt.fontSize = 16;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.88f, 0.94f, 1f);
            FarmUITheme.AddShadow(subTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            // Options Buttons
            CreateWeatherBtn(weatherModal.transform, -92f, "🔄 Tự động (Theo chu kỳ 4 Mùa & Tự nhiên)", new Color(0.25f, 0.40f, 0.60f), () =>
            {
                if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetManualWeather(WeatherType.Sunny, false);
                CloseWeatherModal();
            });

            CreateWeatherBtn(weatherModal.transform, -148f, "☀️ Thủ công: Nắng ấm (Sunny Day)", new Color(0.70f, 0.50f, 0.15f), () =>
            {
                if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetManualWeather(WeatherType.Sunny, true);
                CloseWeatherModal();
            });

            CreateWeatherBtn(weatherModal.transform, -204f, "🌧️ Thủ công: Mưa rào (Rainy - Cây lớn +50%)", new Color(0.20f, 0.48f, 0.52f), () =>
            {
                if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetManualWeather(WeatherType.Rainy, true);
                CloseWeatherModal();
            });

            CreateWeatherBtn(weatherModal.transform, -260f, "❄️ Thủ công: Tuyết rơi (Snowy Day)", new Color(0.35f, 0.55f, 0.70f), () =>
            {
                if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetManualWeather(WeatherType.Snowy, true);
                CloseWeatherModal();
            });

            CreateWeatherBtn(weatherModal.transform, -316f, "⏭️ Sang Ngày Tiếp Theo (Advance Day)", new Color(0.25f, 0.55f, 0.30f), () =>
            {
                if (WeatherSystem.Instance != null) WeatherSystem.Instance.AdvanceDay();
                CloseWeatherModal();
            });

            // Close button
            CreateWeatherBtn(weatherModal.transform, -374f, "❌ Đóng (Close)", new Color(0.45f, 0.45f, 0.45f), () =>
            {
                CloseWeatherModal();
            });

            FarmUITheme.StyleAllTextUnder(weatherModal.transform);
        }

        private void CreateWeatherBtn(Transform parent, float yPos, string label, Color bgColor, UnityEngine.Events.UnityAction action)
        {
            GameObject btnGo = new GameObject("Btn_" + label);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(490f, 44f);

            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;
            FarmUITheme.AddOutline(btnGo, new Color(1f, 1f, 1f, 0.25f), new Vector2(1f, -1f));

            Button btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = FarmUITheme.BoldFont;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            FarmUITheme.AddShadow(txt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));
        }

        public void CloseWeatherModal()
        {
            if (weatherModal != null) weatherModal.SetActive(false);
        }

        public void OpenSettingsModal()
        {
            if (settingsModal != null)
            {
                settingsModal.SetActive(true);
                return;
            }

            // Create Audio Settings modal inside Farm UI
            settingsModal = new GameObject("Farm_AudioSettings_Modal");
            settingsModal.transform.SetParent(transform, false);

            RectTransform smrt = settingsModal.AddComponent<RectTransform>();
            smrt.anchorMin = new Vector2(0.5f, 0.5f);
            smrt.anchorMax = new Vector2(0.5f, 0.5f);
            smrt.pivot = new Vector2(0.5f, 0.5f);
            smrt.sizeDelta = new Vector2(540f, 500f);

            Image smImg = settingsModal.AddComponent<Image>();
            smImg.color = new Color(0.10f, 0.14f, 0.20f, 0.98f);
            FarmUITheme.AddOutline(settingsModal, new Color(0.30f, 0.45f, 0.65f, 0.85f), new Vector2(2f, 2f));

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(settingsModal.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -16f);
            trt.sizeDelta = new Vector2(0f, 40f);

            Text tTxt = titleGo.AddComponent<Text>();
            tTxt.text = "⚙️ CÀI ĐẶT ÂM THANH TRANG TRẠI";
            tTxt.font = FarmUITheme.BoldFont;
            tTxt.fontSize = 20;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(1.0f, 0.90f, 0.35f);
            FarmUITheme.AddShadow(tTxt, FarmUITheme.ColorShadowDark, new Vector2(1.5f, -1.5f));

            AudioManager am = AudioManager.EnsureInstance();

            // Status label for BGM
            GameObject bStatusGo = new GameObject("Text_BgmStatus");
            bStatusGo.transform.SetParent(settingsModal.transform, false);
            RectTransform bsrt = bStatusGo.AddComponent<RectTransform>();
            bsrt.anchorMin = new Vector2(0.5f, 1f);
            bsrt.anchorMax = new Vector2(0.5f, 1f);
            bsrt.pivot = new Vector2(0.5f, 1f);
            bsrt.anchoredPosition = new Vector2(0f, -175f);
            bsrt.sizeDelta = new Vector2(480f, 26f);

            Text bStatusTxt = bStatusGo.AddComponent<Text>();
            bStatusTxt.font = FarmUITheme.BoldFont;
            bStatusTxt.fontSize = 14;
            bStatusTxt.alignment = TextAnchor.MiddleCenter;
            bStatusTxt.supportRichText = true;
            FarmUITheme.AddShadow(bStatusTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            System.Action updateBgmText = () =>
            {
                if (bStatusTxt == null) return;
                if (am.HasValidBgmClip())
                {
                    if (am.bgmMuted)
                        bStatusTxt.text = $"Trạng thái: <color=#ffaa55>Tắt tiếng (Muted)</color> • {am.CurrentClipName}";
                    else
                        bStatusTxt.text = $"Trạng thái: <color=#55ff88>Đang phát ({Mathf.RoundToInt(am.bgmVolume * 100)}%)</color> • {am.CurrentClipName}";
                }
                else
                {
                    bStatusTxt.text = "<color=#ff5555>Không tìm thấy file nhạc nền!</color>";
                }
            };

            // Toggle BGM
            CreateSettingsToggle(settingsModal.transform, -75f, "Bật Nhạc Nền (BGM)", !am.bgmMuted, (val) =>
            {
                am.SetBGMMuted(!val);
                updateBgmText();
            });

            // Slider BGM
            CreateSettingsSlider(settingsModal.transform, -130f, "Âm lượng Nhạc nền:", am.bgmVolume, (val) =>
            {
                am.SetBGMVolume(val);
                updateBgmText();
            });

            // Toggle SFX
            CreateSettingsToggle(settingsModal.transform, -215f, "Bật Hiệu Ứng Âm Thanh (SFX)", !am.sfxMuted, (val) =>
            {
                am.SetSFXMuted(!val);
            });

            // Slider SFX
            CreateSettingsSlider(settingsModal.transform, -270f, "Âm lượng Hiệu ứng:", am.sfxVolume, (val) =>
            {
                am.SetSFXVolume(val);
            });

            updateBgmText();

            // Close Button
            GameObject closeGo = new GameObject("Btn_Close");
            closeGo.transform.SetParent(settingsModal.transform, false);
            RectTransform crt = closeGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 25f);
            crt.sizeDelta = new Vector2(260f, 48f);

            Image cImg = closeGo.AddComponent<Image>();
            cImg.color = new Color(0.25f, 0.55f, 0.35f, 0.95f);
            FarmUITheme.AddOutline(closeGo, new Color(0.35f, 0.75f, 0.45f, 0.85f), new Vector2(1.5f, -1.5f));

            Button cBtn = closeGo.AddComponent<Button>();
            cBtn.onClick.AddListener(() =>
            {
                am.PlayButtonClick();
                am.SaveAudioPreferences();
                CloseSettingsModal();
            });

            GameObject cTxtGo = new GameObject("Text");
            cTxtGo.transform.SetParent(closeGo.transform, false);
            RectTransform ctrt = cTxtGo.AddComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;

            Text cTxt = cTxtGo.AddComponent<Text>();
            cTxt.text = "✔  LƯU & ĐÓNG";
            cTxt.font = FarmUITheme.BoldFont;
            cTxt.fontSize = 17;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            FarmUITheme.AddShadow(cTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            FarmUITheme.StyleAllTextUnder(settingsModal.transform);
        }

        private void CreateSettingsToggle(Transform parent, float yPos, string label, bool isOn, UnityEngine.Events.UnityAction<bool> onToggle)
        {
            GameObject togGo = new GameObject("Toggle_" + label);
            togGo.transform.SetParent(parent, false);

            RectTransform trt = togGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, yPos);
            trt.sizeDelta = new Vector2(440f, 32f);

            Toggle toggle = togGo.AddComponent<Toggle>();
            toggle.isOn = isOn;

            GameObject boxGo = new GameObject("Background");
            boxGo.transform.SetParent(togGo.transform, false);
            RectTransform brt = boxGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(8f, 0f);
            brt.sizeDelta = new Vector2(26f, 26f);

            Image bImg = boxGo.AddComponent<Image>();
            bImg.color = new Color(0.25f, 0.30f, 0.38f);
            toggle.targetGraphic = bImg;

            GameObject checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(boxGo.transform, false);
            RectTransform chrt = checkGo.AddComponent<RectTransform>();
            chrt.anchorMin = new Vector2(0.2f, 0.2f);
            chrt.anchorMax = new Vector2(0.8f, 0.8f);
            chrt.sizeDelta = Vector2.zero;

            Image chImg = checkGo.AddComponent<Image>();
            chImg.color = new Color(0.40f, 0.85f, 0.40f);
            toggle.graphic = chImg;

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(togGo.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(50f, 0f);
            lrt.offsetMax = Vector2.zero;

            Text lTxt = lblGo.AddComponent<Text>();
            lTxt.text = label;
            lTxt.font = FarmUITheme.BoldFont;
            lTxt.fontSize = 15;
            lTxt.fontStyle = FontStyle.Bold;
            lTxt.alignment = TextAnchor.MiddleLeft;
            lTxt.color = Color.white;
            FarmUITheme.AddShadow(lTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));

            toggle.onValueChanged.AddListener(onToggle);
        }

        private void CreateSettingsSlider(Transform parent, float yPos, string label, float initVal, UnityEngine.Events.UnityAction<float> onChange)
        {
            GameObject container = new GameObject("SliderGroup_" + label);
            container.transform.SetParent(parent, false);

            RectTransform grt = container.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 1f);
            grt.anchorMax = new Vector2(0.5f, 1f);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.anchoredPosition = new Vector2(0f, yPos);
            grt.sizeDelta = new Vector2(440f, 44f);

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(container.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0.45f, 1f);
            lrt.sizeDelta = Vector2.zero;

            Text lTxt = lblGo.AddComponent<Text>();
            lTxt.text = label;
            lTxt.font = FarmUITheme.BoldFont;
            lTxt.fontSize = 15;
            lTxt.fontStyle = FontStyle.Bold;
            lTxt.alignment = TextAnchor.MiddleLeft;
            lTxt.color = new Color(0.85f, 0.90f, 0.95f);
            FarmUITheme.AddShadow(lTxt, FarmUITheme.ColorShadowDark, new Vector2(1.2f, -1.2f));
            lTxt.fontSize = 13;
            lTxt.alignment = TextAnchor.MiddleLeft;
            lTxt.color = new Color(0.85f, 0.90f, 0.95f);

            GameObject slGo = new GameObject("Slider");
            slGo.transform.SetParent(container.transform, false);
            RectTransform srt = slGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.45f, 0.15f);
            srt.anchorMax = new Vector2(1f, 0.85f);
            srt.sizeDelta = Vector2.zero;

            Slider slider = slGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initVal;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(slGo.transform, false);
            RectTransform bgrt = bg.AddComponent<RectTransform>();
            bgrt.anchorMin = new Vector2(0f, 0.25f);
            bgrt.anchorMax = new Vector2(1f, 0.75f);
            bgrt.sizeDelta = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.20f, 0.24f, 0.30f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slGo.transform, false);
            RectTransform fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = new Vector2(0f, 0.25f);
            fart.anchorMax = new Vector2(1f, 0.75f);
            fart.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform frt = fill.AddComponent<RectTransform>();
            frt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.75f, 0.45f);
            slider.fillRect = frt;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(slGo.transform, false);
            RectTransform hart = handleArea.AddComponent<RectTransform>();
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hrt = handle.AddComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(22f, 0f);
            Image hImg = handle.AddComponent<Image>();
            hImg.color = Color.white;
            slider.handleRect = hrt;
            slider.targetGraphic = hImg;

            slider.onValueChanged.AddListener(onChange);
        }

        public void CloseSettingsModal()
        {
            if (settingsModal != null) settingsModal.SetActive(false);
        }
    }
}
