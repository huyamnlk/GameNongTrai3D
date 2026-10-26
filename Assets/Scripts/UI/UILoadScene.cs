using UnityEngine;
using System.Collections;
using System.IO;

namespace IrishFarmSim
{
	public class UILoadScene : MonoBehaviour 
	{
		public string levelName;
		public bool loadPlayer;
		public bool newGame;

		void FixedUpdate() 
		{
			if (Input.GetKeyDown (KeyCode.Escape)) 
			{
				if(Application.loadedLevelName.Equals("Main Menu"))
				{
					StartCoroutine(Load("Quit"));
				}
				else
				{
					StartCoroutine(Load("Main Menu"));
				}
			}
		}

		public void OnClick()
		{
			StartCoroutine(Load(levelName));
		}

		private IEnumerator Load(string levelName) 
		{
			yield return new WaitForSeconds(0.5f);

			if (!string.IsNullOrEmpty(levelName))
			{
				if(levelName.Equals("Quit"))
				{
#if UNITY_EDITOR
					Debug.Log("[MainMenu] Thoát game (Đang chạy trong Unity Editor - dừng Play Mode).");
					UIBackground.ShowNoSaveNotice("Đang chạy trong Unity Editor!\nTrong bản build cài đặt thật, ứng dụng sẽ thoát.");
					UnityEditor.EditorApplication.isPlaying = false;
#else
					Application.Quit();
#endif
				}
				else if(newGame || levelName.Equals("Player Name") || (levelName.Equals("Farm") && !loadPlayer))
				{
					// Chơi mới: Nếu đã có dữ liệu lưu thì hiển thị xác nhận trước khi ghi đè
					if (SaveLoadSystem.HasSave())
					{
						UIBackground.ShowNewGameConfirm(() => ExecuteStartNewGame());
					}
					else
					{
						ExecuteStartNewGame();
					}
				}
				else if(levelName.Equals("Farm"))
				{
					if(loadPlayer)
					{
						if (SaveLoadSystem.HasSave())
						{
							GameController.Instance().loadPlayer = true;
							GameController.Instance().newGame = false;
							UIBackground.BackgroundDark(true);
							UIBackground.LoadingTexture(true);
							UIBackground.Background(false);
							UIBackground.Logo(false);
							UIBackground.Buttons(false);
							StartCoroutine(DelayLoadScene(1, "Farm"));
						}
						else
						{
							UIBackground.ShowNoSaveNotice("Chưa có dữ liệu lưu!\nVui lòng chọn 'New Game' để bắt đầu trò chơi mới.");
						}
					}
				}
				else
				{
					NGUITools.Broadcast("End");
					UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
				}
			}
		}

		private void ExecuteStartNewGame()
		{
			SaveLoadSystem.InitNewGameDefaults();
			if (GameController.Instance() != null)
			{
				GameController.Instance().newGame = true;
				GameController.Instance().loadPlayer = false;
				if (GameController.Instance().player == null)
				{
					GameController.Instance().player = new Farmer("Farmer", 25000, 0, 0, 0, 0, GameController.Instance().gameDifficulty, GameController.Instance().fxLevel);
				}
			}
			UIBackground.BackgroundDark(true);
			UIBackground.LoadingTexture(true);
			UIBackground.Background(false);
			UIBackground.Logo(false);
			UIBackground.Buttons(false);
			StartCoroutine(DelayLoadScene(1, "Farm"));
		}

		private IEnumerator DelayLoadScene(int seconds, string levelName) 
		{
			yield return new WaitForSeconds(seconds);
			UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
		}
	}
}