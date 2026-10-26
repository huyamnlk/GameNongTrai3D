using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace IrishFarmSim
{
    /// <summary>
    /// Master coordinator for Main Menu lifecycle.
    /// Operates directly on the native NGUI UI Root (Background, Logo, Buttons, Version Label)
    /// without spawning duplicate overlay canvases.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (SceneManager.GetActiveScene().name.Equals("Main Menu", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureInstance();
            }
        }

        public static MainMenuController EnsureInstance()
        {
            // Remove any legacy modern canvas overlays that might have been saved in scene
            PurgeModernCanvasOverlay();

            if (Instance != null)
            {
                Instance.RefreshSaveState();
                return Instance;
            }

            MainMenuController found = FindAnyObjectByType<MainMenuController>();
            if (found != null)
            {
                Instance = found;
                Instance.RefreshSaveState();
                return found;
            }

            GameObject go = new GameObject("MainMenuController");
            Instance = go.AddComponent<MainMenuController>();
            Instance.RefreshSaveState();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            PurgeModernCanvasOverlay();
        }

        void Start()
        {
            PurgeModernCanvasOverlay();
            RefreshSaveState();
            AudioManager.EnsureInstance().EnsureBgmPlaying();
            UIBackground.EnsureTitleAndVersionDisplay();
        }

        public static void PurgeModernCanvasOverlay()
        {
            GameObject modernCanvas = GameObject.Find("MainMenu_ModernCanvas");
            if (modernCanvas != null)
            {
                Debug.Log("[MainMenuController] Purged duplicate MainMenu_ModernCanvas overlay.");
                if (Application.isPlaying)
                    Destroy(modernCanvas);
                else
                    DestroyImmediate(modernCanvas);
            }
        }

        public void RefreshSaveState()
        {
            UIBackground.UpdateContinueButtonVisibility();
        }

        public void OnClickNewGame()
        {
            if (SaveLoadSystem.HasSave())
            {
                UIBackground.ShowNewGameConfirm(() => StartConfirmedNewGame());
            }
            else
            {
                StartConfirmedNewGame();
            }
        }

        public void StartConfirmedNewGame()
        {
            Debug.Log("[MainMenuController] Player confirmed New Game. Initializing clean farm defaults...");
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
            SceneManager.LoadScene("Farm");
        }

        public void OnClickContinue()
        {
            if (!SaveLoadSystem.HasSave())
            {
                UIBackground.ShowNoSaveNotice("Chưa có dữ liệu lưu!\nVui lòng chọn 'New Game' để bắt đầu trò chơi mới.");
                return;
            }

            Debug.Log("[MainMenuController] Player selected Continue. Loading saved farm...");
            if (GameController.Instance() != null)
            {
                GameController.Instance().newGame = false;
                GameController.Instance().loadPlayer = true;
            }

            UIBackground.BackgroundDark(true);
            UIBackground.LoadingTexture(true);
            UIBackground.Background(false);
            UIBackground.Logo(false);
            UIBackground.Buttons(false);
            SceneManager.LoadScene("Farm");
        }

        public void OnClickSettings()
        {
            Debug.Log("[MainMenuController] Opening native Settings scene...");
            SceneManager.LoadScene("Settings");
        }

        public void OnClickExit()
        {
            Debug.Log("[MainMenuController] Exiting game...");
#if UNITY_EDITOR
            Debug.Log("[MainMenuController] Application.Quit() called in Unity Editor.");
            UIBackground.ShowNoSaveNotice("Đang chạy trong Unity Editor!\nTrong bản build cài đặt thật, ứng dụng sẽ thoát.");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
