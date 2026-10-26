using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Central audio manager for Background Music (BGM) and Sound Effects (SFX).
    /// Prevents music duplication across scenes, persists volume/mute preferences via PlayerPrefs,
    /// and provides dedicated controls for settings UI.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PREF_BGM_VOL = "AUDIO_BGM_VOL";
        private const string PREF_SFX_VOL = "AUDIO_SFX_VOL";
        private const string PREF_BGM_MUTE = "AUDIO_BGM_MUTE";
        private const string PREF_SFX_MUTE = "AUDIO_SFX_MUTE";

        [Header("Audio Sources")]
        private AudioSource bgmSource;
        private AudioSource sfxSource;

        [Header("Settings Cache")]
        public float bgmVolume = 0.6f;
        public float sfxVolume = 0.8f;
        public bool bgmMuted = false;
        public bool sfxMuted = false;

        [Header("Audio Clips")]
        public AudioClip defaultBgmClip;
        public AudioClip martBgmClip;
        public AudioClip buttonClickClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static AudioManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            AudioManager found = FindAnyObjectByType<AudioManager>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("AudioManager");
            Instance = go.AddComponent<AudioManager>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying) DontDestroyOnLoad(gameObject);
                SetupAudioSources();
                LoadAudioPreferences();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f; // 2D Stereo
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D Stereo
            }

            // 1. Try loading from Resources (available on Android, Windows, and Editor)
            if (defaultBgmClip == null)
            {
                defaultBgmClip = Resources.Load<AudioClip>("Sounds/Farm Ambient");
            }
            if (martBgmClip == null)
            {
                martBgmClip = Resources.Load<AudioClip>("Sounds/Mart Ambient");
            }
            if (buttonClickClip == null)
            {
                buttonClickClip = Resources.Load<AudioClip>("Sounds/Button");
            }

            // 2. Editor fallback
#if UNITY_EDITOR
            if (defaultBgmClip == null)
                defaultBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Farm Ambient.mp3");
            if (martBgmClip == null)
                martBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Mart Ambient.mp3");
            if (buttonClickClip == null)
                buttonClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Button.wav");
#endif
        }

        private void LoadAudioPreferences()
        {
            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
            bgmMuted = PlayerPrefs.GetInt(PREF_BGM_MUTE, 0) == 1;
            sfxMuted = PlayerPrefs.GetInt(PREF_SFX_MUTE, 0) == 1;

            ApplyAudioSettings();
        }

        public void SaveAudioPreferences()
        {
            PlayerPrefs.SetFloat(PREF_BGM_VOL, bgmVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
            PlayerPrefs.SetInt(PREF_BGM_MUTE, bgmMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREF_SFX_MUTE, sfxMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyAudioSettings()
        {
            // 1. Update primary AudioManager BGM source directly and immediately
            if (bgmSource != null)
            {
                bgmSource.volume = bgmMuted ? 0f : bgmVolume;
                bgmSource.mute = bgmMuted;

                if (bgmMuted)
                {
                    if (bgmSource.isPlaying) bgmSource.Pause();
                }
                else
                {
                    if (!bgmSource.isPlaying && bgmSource.clip != null) bgmSource.Play();
                }
            }

            // 2. Safeguard & direct sync: Find any other active AudioSource in the scene playing ambient/looping music
            // (e.g. from Main Camera or imported scene prefabs) and ensure its volume & mute match immediately
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var src in allSources)
            {
                if (src == null || src == bgmSource || src == sfxSource) continue;

                bool isAmbientBgm = (src.clip != null && src.clip.name.Contains("Ambient"))
                                    || (src.loop && src.GetComponent<Camera>() != null);
                if (isAmbientBgm)
                {
                    src.volume = bgmMuted ? 0f : bgmVolume;
                    src.mute = bgmMuted;
                    if (bgmMuted && src.isPlaying)
                    {
                        src.Pause();
                    }
                    else if (!bgmMuted && !src.isPlaying && src.clip != null)
                    {
                        src.Play();
                    }
                }
            }

            if (sfxSource != null)
            {
                sfxSource.volume = sfxMuted ? 0f : sfxVolume;
                sfxSource.mute = sfxMuted;
            }

            // Sync with GameController legacy values if present
            if (GameController.Instance() != null)
            {
                GameController.Instance().musicLevel = bgmVolume;
                GameController.Instance().fxLevel = sfxVolume;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DeduplicateSceneAudioSources(scene.name);
            EnsureBgmPlaying(scene.name);
            ApplyAudioSettings();
        }

        /// <summary>
        /// Scans for extra BGM sources spawned by scene prefabs (e.g. Main Camera in Farm_GameObjects)
        /// and transfers their clip to AudioManager, disabling the redundant source to prevent overlap.
        /// </summary>
        public void DeduplicateSceneAudioSources(string sceneName = null)
        {
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var src in allSources)
            {
                if (src == null || src == bgmSource || src == sfxSource) continue;

                bool isBgmOnCamera = src.GetComponent<Camera>() != null && src.loop;
                bool isAmbientClip = src.clip != null && src.clip.name.Contains("Ambient");

                if (isBgmOnCamera || isAmbientClip)
                {
                    if (defaultBgmClip == null && src.clip != null)
                    {
                        defaultBgmClip = src.clip;
                    }

                    // Stop and disable redundant audio source to avoid double music
                    src.Stop();
                    src.enabled = false;
                    Debug.Log($"[AudioManager] Deduplicated external BGM AudioSource on '{src.gameObject.name}'. Centralizing into AudioManager.");
                }
            }
        }

        public void EnsureBgmPlaying(string sceneName = null)
        {
            if (bgmSource == null) SetupAudioSources();

            string currentScene = !string.IsNullOrEmpty(sceneName) ? sceneName : SceneManager.GetActiveScene().name;
            AudioClip targetClip = defaultBgmClip;

            if (currentScene.Equals("Mart", System.StringComparison.OrdinalIgnoreCase) && martBgmClip != null)
            {
                targetClip = martBgmClip;
            }

            if (targetClip != null)
            {
                if (bgmSource.clip != targetClip)
                {
                    bgmSource.clip = targetClip;
                    if (!bgmMuted)
                    {
                        bgmSource.volume = bgmVolume;
                        bgmSource.Play();
                    }
                }
                else if (!bgmSource.isPlaying && !bgmMuted)
                {
                    bgmSource.volume = bgmVolume;
                    bgmSource.Play();
                }
            }
        }

        public bool HasValidBgmClip()
        {
            return (bgmSource != null && bgmSource.clip != null) || defaultBgmClip != null || martBgmClip != null;
        }

        public string CurrentClipName
        {
            get
            {
                if (bgmSource != null && bgmSource.clip != null) return bgmSource.clip.name;
                if (defaultBgmClip != null) return defaultBgmClip.name;
                return "None";
            }
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
            SaveAudioPreferences();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
            SaveAudioPreferences();
        }

        public void SetBGMMuted(bool mute)
        {
            bgmMuted = mute;
            ApplyAudioSettings();
            SaveAudioPreferences();
        }

        public void SetSFXMuted(bool mute)
        {
            sfxMuted = mute;
            ApplyAudioSettings();
            SaveAudioPreferences();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (sfxMuted || clip == null) return;
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
            }
        }

        public void PlayButtonClick()
        {
            if (buttonClickClip != null)
            {
                PlaySFX(buttonClickClip, 0.7f);
            }
        }
    }
}
