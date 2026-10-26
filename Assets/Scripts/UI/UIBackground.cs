using UnityEngine;
using System.Collections;
using System;

namespace IrishFarmSim
{
	public class UIBackground : MonoBehaviour 
	{
		private static UITexture backgroundDark;
		private static UITexture loadingTexture;
		private static UITexture nogameTexture;
		private static UITexture logo;
		private static UITexture background;
		private static UITexture buttons;
		private static UITexture nameLabel;
		private static UISprite inputField;
		private static UILabel inputFieldLabel;

		private static string noSaveNoticeMessage = "";
		private static float noSaveNoticeTimer = 0f;

		// New Game Confirmation Modal state
		public static bool isShowingConfirm = false;
		private static Action onConfirmNewGameAction = null;

		private static T SafeFindComponent<T>(string name) where T : Component
		{
			GameObject go = GameObject.Find(name);
			return go != null ? go.GetComponent<T>() : null;
		}

		void Awake()
		{
			// Purge any residual modern overlay canvas if present in scene
			GameObject modernCanvas = GameObject.Find("MainMenu_ModernCanvas");
			if (modernCanvas != null)
			{
				Destroy(modernCanvas);
			}
		}

		void Start()
		{
			backgroundDark = SafeFindComponent<UITexture>("Background Dark");
			loadingTexture = SafeFindComponent<UITexture>("Loading");
			nogameTexture = SafeFindComponent<UITexture>("No Game");
			background = SafeFindComponent<UITexture>("Background");
			buttons = SafeFindComponent<UITexture>("Buttons");

			string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			if (levelName.Equals("Main Menu")) 
			{
				logo = SafeFindComponent<UITexture>("Logo");
				UpdateContinueButtonVisibility();
				EnsureTitleAndVersionDisplay();
				EnsureMainMenuBackground();
			}

			if (levelName.Equals("Player Name")) 
			{
				nameLabel = SafeFindComponent<UITexture>("Name Label");
				GameObject inputGo = GameObject.Find("Player Name Input");
				if (inputGo != null)
				{
					inputField = inputGo.GetComponent<UISprite>();
					inputFieldLabel = inputGo.GetComponentInChildren<UILabel>();
				}
			}

			if (backgroundDark != null) backgroundDark.enabled = false;
			if (loadingTexture != null) loadingTexture.enabled = false;
			if (nogameTexture != null) nogameTexture.enabled = false;
		}

		void Update()
		{
			if (noSaveNoticeTimer > 0f)
			{
				noSaveNoticeTimer -= Time.deltaTime;
				if (noSaveNoticeTimer <= 0f)
				{
					noSaveNoticeMessage = "";
					if (!isShowingConfirm)
					{
						if (backgroundDark != null) backgroundDark.enabled = false;
						if (logo != null) logo.enabled = true;
						if (buttons != null) buttons.enabled = true;
						UpdateContinueButtonVisibility();
					}
				}
			}
		}

		public static void EnsureTitleAndVersionDisplay()
		{
			// 1. Update Version Label to Version 1.1.1.1
			UILabel[] allLabels = Resources.FindObjectsOfTypeAll<UILabel>();
			foreach (var lbl in allLabels)
			{
				if (lbl != null && (lbl.name.Contains("Version") || (lbl.text != null && lbl.text.Contains("0.6.5"))))
				{
					lbl.text = "Version 1.1.1.1";
				}
			}

			// 2. Add or update "NHÓM 1" subtitle label attached to Logo
			GameObject logoGo = GameObject.Find("Logo");
			if (logoGo != null)
			{
				Transform existingSub = logoGo.transform.Find("Subtitle_Nhom1") ?? logoGo.transform.Find("Title_NongTrai3D") ?? logoGo.transform.Find("Title_NongTrai2D");
				UILabel subLbl = null;
				if (existingSub == null)
				{
					GameObject tGo = new GameObject("Subtitle_Nhom1");
					tGo.transform.SetParent(logoGo.transform, false);
					tGo.transform.localPosition = new Vector3(0f, -54f, 0f);
					tGo.layer = logoGo.layer;
					subLbl = tGo.AddComponent<UILabel>();
				}
				else
				{
					existingSub.name = "Subtitle_Nhom1";
					existingSub.localPosition = new Vector3(0f, -54f, 0f);
					subLbl = existingSub.GetComponent<UILabel>();
				}

				if (subLbl != null)
				{
					subLbl.text = "NHÓM 1";
					subLbl.fontSize = 32;
					subLbl.fontStyle = FontStyle.Bold;
					subLbl.color = new Color(1.0f, 0.88f, 0.22f); // Warm yellow
					subLbl.effectStyle = UILabel.Effect.Outline;
					subLbl.effectColor = new Color(0.18f, 0.10f, 0.04f, 0.95f);
					subLbl.effectDistance = new Vector2(2f, 2f);
					subLbl.alignment = NGUIText.Alignment.Center;
					subLbl.overflowMethod = UILabel.Overflow.ResizeFreely;

					Font farmFont = Resources.Load<Font>("Fonts/FarmFont_Bold") ?? Resources.Load<Font>("FarmFont_Bold");
					if (farmFont != null)
					{
						subLbl.trueTypeFont = farmFont;
						subLbl.bitmapFont = null;
					}
					else
					{
						UILabel sample = GameObject.Find("Version Label")?.GetComponent<UILabel>();
						if (sample != null)
						{
							subLbl.trueTypeFont = sample.trueTypeFont;
							subLbl.bitmapFont = sample.bitmapFont;
						}
					}
				}
			}

			EnsureMainMenuBackground();
		}

		public static void EnsureMainMenuBackground()
		{
			if (background == null) background = SafeFindComponent<UITexture>("Background");
			if (background != null)
			{
				Texture newBg = Resources.Load<Texture>("main_menu_farm_bg")
					?? Resources.Load<Texture>("Navigation Resources/Background/main_menu_farm_bg");
#if UNITY_EDITOR
				if (newBg == null)
				{
					newBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>("Assets/Navigation Resources/Background/main_menu_farm_bg.png")
						?? UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>("Assets/Resources/main_menu_farm_bg.png");
				}
#endif
				if (newBg != null)
				{
					background.mainTexture = newBg;
				}

				// Remove any blocking collider on Background
				Collider col = background.GetComponent<Collider>();
				if (col != null)
				{
					if (Application.isPlaying) Destroy(col);
					else DestroyImmediate(col);
				}

				// Place behind buttons (depth -10)
				background.depth = -10;

				// Adjust dimensions to fit/cover 16:9 and different aspect ratios without distortion
				UIRoot root = FindAnyObjectByType<UIRoot>();
				int screenH = (root != null) ? root.activeHeight : 720;
				float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
				float bgAspect = 1920f / 1080f; // 1.7778

				int targetW, targetH;
				if (screenAspect > bgAspect)
				{
					targetW = Mathf.CeilToInt(screenH * screenAspect);
					targetH = Mathf.CeilToInt(targetW / bgAspect);
				}
				else
				{
					targetH = screenH;
					targetW = Mathf.CeilToInt(screenH * bgAspect);
				}

				background.width = Mathf.Max(targetW, 1920);
				background.height = Mathf.Max(targetH, 1080);
				background.keepAspectRatio = UIWidget.AspectRatioSource.Free;
				background.transform.localPosition = Vector3.zero;
			}
		}

		public static void UpdateContinueButtonVisibility()
		{
			bool hasSave = SaveLoadSystem.HasSave();
			GameObject loadBtn = GameObject.Find("LoadGameButton");
			if (loadBtn == null)
			{
				var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
				foreach (var t in allTransforms)
				{
					if (t != null && t.name == "LoadGameButton" && t.gameObject.scene.isLoaded)
					{
						loadBtn = t.gameObject;
						break;
					}
				}
			}

			if (loadBtn != null)
			{
				loadBtn.SetActive(hasSave);
				Collider col = loadBtn.GetComponent<Collider>();
				if (col != null) col.enabled = hasSave;
			}
		}

		public static void ShowNoSaveNotice(string message)
		{
			noSaveNoticeMessage = message;
			noSaveNoticeTimer = 3.0f;
			if (backgroundDark != null) backgroundDark.enabled = true;
			if (logo != null) logo.enabled = false;
			if (buttons != null) buttons.enabled = false;
		}

		public static void ShowNewGameConfirm(Action onConfirmed)
		{
			isShowingConfirm = true;
			onConfirmNewGameAction = onConfirmed;
			BackgroundDark(true);
			Logo(false);
			Buttons(false);
		}

		void OnGUI()
		{
			// 1. New Game Confirmation Modal Dialog
			if (isShowingConfirm)
			{
				float w = Mathf.Min(560f, Screen.width * 0.9f);
				float h = 260f;
				float x = (Screen.width - w) * 0.5f;
				float y = (Screen.height - h) * 0.5f;

				GUI.Box(new Rect(x, y, w, h), "");
				GUI.Box(new Rect(x, y, w, h), "");

				GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
				titleStyle.fontSize = 21;
				titleStyle.fontStyle = FontStyle.Bold;
				titleStyle.alignment = TextAnchor.MiddleCenter;
				titleStyle.normal.textColor = new Color(1.0f, 0.82f, 0.25f);

				GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
				bodyStyle.fontSize = 15;
				bodyStyle.alignment = TextAnchor.MiddleCenter;
				bodyStyle.wordWrap = true;
				bodyStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f);

				GUI.Label(new Rect(x + 10f, y + 18f, w - 20f, 36f), "⚠️ XÁC NHẬN CHƠI MỚI / CONFIRM NEW GAME", titleStyle);
				GUI.Label(new Rect(x + 25f, y + 65f, w - 50f, 90f), 
					"Bạn đã có dữ liệu trang trại được lưu trước đó.\nBắt đầu chơi mới sẽ ghi đè và xóa toàn bộ tiến trình cũ.\nBạn có chắc chắn muốn tạo nông trại mới không?", bodyStyle);

				float btnW = (w - 70f) * 0.5f;
				float btnH = 48f;
				float btnY = y + h - 68f;

				// Confirm Button (Green)
				Color oldColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(0.20f, 0.65f, 0.30f, 1f);
				if (GUI.Button(new Rect(x + 25f, btnY, btnW, btnH), "✔  ĐỒNG Ý (TẠO MỚI)"))
				{
					isShowingConfirm = false;
					GUI.backgroundColor = oldColor;
					if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
					onConfirmNewGameAction?.Invoke();
					return;
				}

				// Cancel Button (Gray/Red)
				GUI.backgroundColor = new Color(0.55f, 0.35f, 0.35f, 1f);
				if (GUI.Button(new Rect(x + 45f + btnW, btnY, btnW, btnH), "✖  HỦY BỎ"))
				{
					isShowingConfirm = false;
					GUI.backgroundColor = oldColor;
					if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
					BackgroundDark(false);
					Logo(true);
					Buttons(true);
					UpdateContinueButtonVisibility();
					return;
				}
				GUI.backgroundColor = oldColor;
				return;
			}

			// 2. Simple Notice Popup (e.g. no save or editor exit)
			if (!string.IsNullOrEmpty(noSaveNoticeMessage))
			{
				float w = Mathf.Min(600f, Screen.width * 0.85f);
				float h = 160f;
				float x = (Screen.width - w) * 0.5f;
				float y = (Screen.height - h) * 0.5f;

				GUI.Box(new Rect(x, y, w, h), "");
				GUI.Box(new Rect(x, y, w, h), "");

				GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
				titleStyle.fontSize = 20;
				titleStyle.fontStyle = FontStyle.Bold;
				titleStyle.alignment = TextAnchor.MiddleCenter;
				titleStyle.normal.textColor = new Color(1.0f, 0.85f, 0.25f);

				GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
				bodyStyle.fontSize = 16;
				bodyStyle.alignment = TextAnchor.MiddleCenter;
				bodyStyle.normal.textColor = Color.white;

				GUI.Label(new Rect(x, y + 20f, w, 40f), "THÔNG BÁO / NOTICE", titleStyle);
				GUI.Label(new Rect(x + 20f, y + 65f, w - 40f, 75f), noSaveNoticeMessage, bodyStyle);
			}
		}

		public static void BackgroundDark(bool value)
		{
			if (backgroundDark != null) backgroundDark.enabled = value;
		}

		public static void LoadingTexture(bool value)
		{
			if (loadingTexture != null) loadingTexture.enabled = value;
		}

		public static void NogameTexture(bool value)
		{
			if (nogameTexture != null) nogameTexture.enabled = value;
		}

		public static void Logo(bool value)
		{
			if (logo != null) logo.enabled = value;
		}

		public static void Background(bool value)
		{
			if (background != null) background.enabled = value;
		}

		public static void Buttons(bool value)
		{
			if (buttons != null) buttons.enabled = value;
		}

		public static void NameLabel(bool value)
		{
			if (nameLabel != null) nameLabel.enabled = value;
		}

		public static void InputField(bool value)
		{
			if (inputField != null) inputField.enabled = value;
			if (inputFieldLabel != null) inputFieldLabel.enabled = value;
		}
	}
}