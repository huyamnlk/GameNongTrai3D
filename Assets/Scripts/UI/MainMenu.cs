using UnityEngine;
using System.Collections;
using System.IO;

namespace IrishFarmSim
{
	public class MainMenu : MonoBehaviour
	{
		// GUI labels & text
		public Texture backgroundTexture;
		public Texture backgroundLoading;
		public Texture backgroundNoGame;
		public GUIStyle labelGameTitle;
		public GUIStyle buttonNewStyle;
		public GUIStyle buttonLoadStyle;
		public GUIStyle buttonExitStyle;
		public float buttonPadding = 0.35f;
		public AudioClip buttonSound;

		private bool isLoading = false;
		private Texture backgroundTemp;
		private string noSaveNoticeMessage = "";
		private float noSaveNoticeTimer = 0f;

		void Start()
		{
		}

		void Update()
		{
			if (noSaveNoticeTimer > 0f)
			{
				noSaveNoticeTimer -= Time.deltaTime;
				if (noSaveNoticeTimer <= 0f)
				{
					noSaveNoticeMessage = "";
				}
			}
		}

		void FixedUpdate() 
		{
			if (Input.GetKeyDown (KeyCode.Escape)) 
			{
				if (GetComponent<AudioSource>() != null && buttonSound != null) GetComponent<AudioSource>().PlayOneShot(buttonSound, 0.6f);
				StartCoroutine(WaitFor(10));	// Exit game
			}
		}

		void Awake()
		{
		}

		void OnGUI()
		{
			if (MainMenuController.Instance != null) return;

			if (backgroundTexture != null)
			{
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), backgroundTexture);
			}

			if (!isLoading && labelGameTitle != null)
			{
				GUI.Label (new Rect (Screen.width * .23f, Screen.height * (buttonPadding - .225f), Screen.width * .58f, Screen.height * .16f), "", labelGameTitle);
			}

			if (!isLoading && buttonNewStyle != null)
			{
				// New Game Button
				if (GUI.Button (new Rect (Screen.width * .28f, Screen.height * (buttonPadding * 1.05f), Screen.width * .45f, Screen.height * .15f), "", buttonNewStyle)) 
				{
					if (GetComponent<AudioSource>() != null && buttonSound != null) GetComponent<AudioSource>().PlayOneShot(buttonSound, 0.6f);
					SaveLoadSystem.InitNewGameDefaults();
					GameController.Instance().newGame = true;
					GameController.Instance().loadPlayer = false;
					if (GameController.Instance().player == null)
					{
						GameController.Instance().player = new Farmer("Farmer", 25000, 0, 0, 0, 0, GameController.Instance().gameDifficulty, GameController.Instance().fxLevel);
					}
					isLoading = true;
					if (backgroundLoading != null) backgroundTexture = backgroundLoading;
					UnityEngine.SceneManagement.SceneManager.LoadScene("Farm");
				}
			}

			// Requirement 8: Continue chỉ hiển thị khi có save hợp lệ
			bool hasSave = SaveLoadSystem.HasSave();
			if (!isLoading && hasSave && buttonLoadStyle != null)
			{
				// Continue / Load Game Button
				if (GUI.Button (new Rect (Screen.width * .28f, Screen.height * (buttonPadding * 1.92f), Screen.width * .45f, Screen.height * .15f), "", buttonLoadStyle)) 
				{	
					if (GetComponent<AudioSource>() != null && buttonSound != null) GetComponent<AudioSource>().PlayOneShot(buttonSound, 0.6f);

					GameController.Instance().loadPlayer = true;
					GameController.Instance().newGame = false;
					isLoading = true;
					if (backgroundLoading != null) backgroundTexture = backgroundLoading;
					UnityEngine.SceneManagement.SceneManager.LoadScene("Farm");
				}
			}

			if (!isLoading && buttonExitStyle != null)
			{
				// Exit Button
				if (GUI.Button (new Rect (Screen.width * .37f, Screen.height * (buttonPadding * 2.9f), Screen.width * .225f, Screen.height * .145f), "", buttonExitStyle)) 
				{
					if (GetComponent<AudioSource>() != null && buttonSound != null) GetComponent<AudioSource>().PlayOneShot(buttonSound, 0.6f);
					StartCoroutine(WaitFor(10));	// Exit game
				}
			}

			// Vietnamese Notice Popup when no save exists
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

		public void ShowNoSaveNotice(string message)
		{
			noSaveNoticeMessage = message;
			noSaveNoticeTimer = 3.0f;
		}

		private IEnumerator WaitFor(int level) 
		{
			yield return new WaitForSeconds(0.8f);
			
			if (level == 10) 
			{
				Application.Quit ();	
			} 
			else 
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene(level);
			}
		}
	}
}