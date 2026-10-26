using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    public enum SeasonType
    {
        Spring, // Mùa Xuân (Xanh tươi, hoa nở, ấm áp)
        Summer, // Mùa Hạ (Nắng vàng rực rỡ, cây cối phát triển mạnh)
        Autumn, // Mùa Thu (Lá vàng cam hổ phách, gió mát dịu)
        Winter  // Mùa Đông (Sương giá băng tuyết, trời lạnh mát)
    }

    public enum WeatherType
    {
        Sunny,  // Nắng ấm
        Rainy,  // Mưa (+50% tốc độ cây trồng)
        Cloudy, // Nhiều mây
        Snowy   // Tuyết rơi (Mùa đông)
    }

    /// <summary>
    /// Manages the 4 Seasons (Xuân, Hạ, Thu, Đông) and Dynamic/Manual Weather system.
    /// Includes real-time seasonal environmental lighting, mobile-optimized rain & snow particles,
    /// ambient weather audio, and manual override options for player settings.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public static event Action<int, WeatherType> OnDayOrWeatherChanged;
        public static event Action<SeasonType> OnSeasonChanged;

        public const int DAYS_PER_SEASON = 7; // 7 in-game days per season

        [Header("Day & Season State")]
        public int currentDay = 1;
        public WeatherType currentWeather = WeatherType.Sunny;
        public float dayDurationSeconds = 180f; // 3 minutes per in-game day
        public float dayTimer = 0f;

        [Header("Manual Weather Override")]
        public bool isManualWeather = false;
        public WeatherType manualWeather = WeatherType.Sunny;

        [Header("Rain & Snow Visuals (Optimized for Mobile)")]
        private GameObject rainEffectContainer;
        private ParticleSystem rainParticleSystem;
        private AudioSource rainAudioSource;

        private GameObject snowEffectContainer;
        private ParticleSystem snowParticleSystem;
        private AudioSource snowAudioSource;

        private Light farmSunLight;
        private bool originalFogEnabled = false;

        public static float GrowthSpeedMultiplier => (Instance != null && Instance.currentWeather == WeatherType.Rainy) ? 1.5f : 1.0f;
        public static bool IsRaining => Instance != null && Instance.currentWeather == WeatherType.Rainy;
        public static bool IsSnowing => Instance != null && Instance.currentWeather == WeatherType.Snowy;

        public int CurrentDay => currentDay;
        public WeatherType CurrentWeather => currentWeather;
        public SeasonType CurrentSeason => (SeasonType)(((currentDay - 1) / DAYS_PER_SEASON) % 4);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static WeatherSystem EnsureInstance()
        {
            if (Instance != null) return Instance;

            WeatherSystem found = FindAnyObjectByType<WeatherSystem>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("WeatherSystem");
            Instance = go.AddComponent<WeatherSystem>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying) DontDestroyOnLoad(gameObject);
                originalFogEnabled = RenderSettings.fog;
                SetupWeatherVisuals();
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSceneSun();
            ApplySeasonAndWeatherVisuals();
        }

        void Start()
        {
            FindSceneSun();
            ApplySeasonAndWeatherVisuals();
            OnDayOrWeatherChanged?.Invoke(currentDay, currentWeather);
            OnSeasonChanged?.Invoke(CurrentSeason);
        }

        void Update()
        {
            dayTimer += Time.deltaTime;
            if (dayTimer >= dayDurationSeconds)
            {
                AdvanceDay();
            }

            // Keep particle systems centered smoothly above camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 aboveCam = cam.transform.position + Vector3.up * 8f;
                if (rainEffectContainer != null && rainEffectContainer.activeSelf)
                {
                    rainEffectContainer.transform.position = aboveCam;
                }
                if (snowEffectContainer != null && snowEffectContainer.activeSelf)
                {
                    snowEffectContainer.transform.position = aboveCam;
                }
            }
        }

        public void AdvanceDay()
        {
            dayTimer = 0f;
            SeasonType prevSeason = CurrentSeason;
            currentDay++;
            SeasonType newSeason = CurrentSeason;

            if (isManualWeather)
            {
                currentWeather = manualWeather;
            }
            else
            {
                currentWeather = RollWeatherForSeason(newSeason);
            }

            ApplySeasonAndWeatherVisuals();

            string seasonName = GetSeasonDisplayName(newSeason);
            string weatherName = GetWeatherDisplayName(currentWeather);
            string manualTag = isManualWeather ? " [Thủ công]" : "";

            string toast = $"Bắt đầu Ngày {currentDay} ({seasonName})!\nThời tiết: {weatherName}{manualTag}";
            if (newSeason != prevSeason)
            {
                toast = $"🌾 CHUYỂN MÙA: Chào mừng {seasonName.ToUpper()}!\n" + toast;
            }

            Debug.Log($"[WeatherSystem] {toast}");
            CropSelectionUI.ShowToast(toast);

            OnDayOrWeatherChanged?.Invoke(currentDay, currentWeather);
            if (newSeason != prevSeason)
            {
                OnSeasonChanged?.Invoke(newSeason);
            }

            // Auto-save on new day
            SaveLoadSystem.SaveGame();
        }

        private WeatherType RollWeatherForSeason(SeasonType season)
        {
            float roll = UnityEngine.Random.value;
            switch (season)
            {
                case SeasonType.Winter:
                    // Winter: 35% Sunny (crisp cold), 40% Snowy, 15% Cloudy, 10% Rainy
                    if (roll < 0.35f) return WeatherType.Sunny;
                    if (roll < 0.75f) return WeatherType.Snowy;
                    if (roll < 0.90f) return WeatherType.Cloudy;
                    return WeatherType.Rainy;

                case SeasonType.Summer:
                    // Summer: 65% Sunny, 25% Rainy, 10% Cloudy
                    if (roll < 0.65f) return WeatherType.Sunny;
                    if (roll < 0.90f) return WeatherType.Rainy;
                    return WeatherType.Cloudy;

                case SeasonType.Autumn:
                    // Autumn: 45% Sunny, 35% Rainy, 20% Cloudy
                    if (roll < 0.45f) return WeatherType.Sunny;
                    if (roll < 0.80f) return WeatherType.Rainy;
                    return WeatherType.Cloudy;

                case SeasonType.Spring:
                default:
                    // Spring: 55% Sunny, 30% Rainy, 15% Cloudy
                    if (roll < 0.55f) return WeatherType.Sunny;
                    if (roll < 0.85f) return WeatherType.Rainy;
                    return WeatherType.Cloudy;
            }
        }

        public void SetWeather(WeatherType weather)
        {
            currentWeather = weather;
            ApplySeasonAndWeatherVisuals();
            OnDayOrWeatherChanged?.Invoke(currentDay, currentWeather);
        }

        /// <summary>
        /// Allows player to manually lock weather (e.g. Sunny or Rainy) from settings.
        /// </summary>
        public void SetManualWeather(WeatherType weather, bool manual)
        {
            isManualWeather = manual;
            manualWeather = weather;
            if (manual)
            {
                currentWeather = weather;
            }
            else
            {
                currentWeather = RollWeatherForSeason(CurrentSeason);
            }

            ApplySeasonAndWeatherVisuals();
            OnDayOrWeatherChanged?.Invoke(currentDay, currentWeather);

            string status = manual
                ? $"Đã chọn thủ công: {GetWeatherDisplayName(currentWeather)}"
                : $"Đã bật thời tiết tự động theo mùa: {GetWeatherDisplayName(currentWeather)}";
            CropSelectionUI.ShowToast(status);
            SaveLoadSystem.SaveGame();
        }

        public void LoadData(int day, string weatherStr, string seasonStr = "", bool manual = false, string manWeatherStr = "")
        {
            currentDay = Mathf.Max(1, day);
            isManualWeather = manual;

            if (!string.IsNullOrEmpty(manWeatherStr) && Enum.TryParse(manWeatherStr, true, out WeatherType parsedMan))
            {
                manualWeather = parsedMan;
            }

            if (!string.IsNullOrEmpty(weatherStr) && Enum.TryParse(weatherStr, true, out WeatherType parsed))
            {
                currentWeather = parsed;
            }
            else
            {
                currentWeather = WeatherType.Sunny;
            }

            ApplySeasonAndWeatherVisuals();
            OnDayOrWeatherChanged?.Invoke(currentDay, currentWeather);
            OnSeasonChanged?.Invoke(CurrentSeason);
        }

        public void LoadData(int day, string weatherStr)
        {
            LoadData(day, weatherStr, "", false, "");
        }

        private void FindSceneSun()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l != null && l.type == LightType.Directional)
                {
                    farmSunLight = l;
                    break;
                }
            }
        }

        public void ApplySeasonAndWeatherVisuals()
        {
            SeasonType season = CurrentSeason;

            // 1. Season Lighting & Atmosphere
            if (farmSunLight == null) FindSceneSun();

            Color sunColor = new Color(1.0f, 0.98f, 0.92f);
            float sunIntensity = 1.05f;
            Color ambientColor = new Color(0.55f, 0.58f, 0.52f);

            switch (season)
            {
                case SeasonType.Spring:
                    sunColor = new Color(1.0f, 0.98f, 0.92f);
                    sunIntensity = 1.05f;
                    ambientColor = new Color(0.55f, 0.58f, 0.52f);
                    RenderSettings.fog = false;
                    break;

                case SeasonType.Summer:
                    sunColor = new Color(1.0f, 0.95f, 0.82f);
                    sunIntensity = 1.20f; // Vibrant bright sunlight
                    ambientColor = new Color(0.60f, 0.58f, 0.48f);
                    RenderSettings.fog = false;
                    break;

                case SeasonType.Autumn:
                    sunColor = new Color(1.0f, 0.86f, 0.70f); // Warm golden amber
                    sunIntensity = 0.98f;
                    ambientColor = new Color(0.58f, 0.50f, 0.42f);
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Exponential;
                    RenderSettings.fogColor = new Color(0.88f, 0.80f, 0.70f);
                    RenderSettings.fogDensity = 0.0015f;
                    break;

                case SeasonType.Winter:
                    sunColor = new Color(0.86f, 0.92f, 1.0f); // Crisp cool blue tint
                    sunIntensity = 0.88f;
                    ambientColor = new Color(0.50f, 0.56f, 0.65f);
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Exponential;
                    RenderSettings.fogColor = new Color(0.82f, 0.88f, 0.96f);
                    RenderSettings.fogDensity = 0.0025f;
                    break;
            }

            // Adjust lighting if cloudy/rainy/snowy
            if (currentWeather == WeatherType.Cloudy)
            {
                sunIntensity *= 0.75f;
                ambientColor *= 0.88f;
            }
            else if (currentWeather == WeatherType.Rainy)
            {
                sunIntensity *= 0.65f;
                sunColor = Color.Lerp(sunColor, new Color(0.7f, 0.75f, 0.85f), 0.4f);
                ambientColor = new Color(0.45f, 0.48f, 0.52f);
            }
            else if (currentWeather == WeatherType.Snowy)
            {
                sunIntensity *= 0.70f;
                sunColor = Color.Lerp(sunColor, new Color(0.85f, 0.92f, 1.0f), 0.5f);
                ambientColor = new Color(0.55f, 0.60f, 0.68f);
            }

            if (farmSunLight != null)
            {
                farmSunLight.color = sunColor;
                farmSunLight.intensity = sunIntensity;
            }
            RenderSettings.ambientLight = ambientColor;

            // 2. Weather Particles & Sound
            bool showRain = (currentWeather == WeatherType.Rainy);
            if (rainEffectContainer != null)
            {
                rainEffectContainer.SetActive(showRain);
                if (rainAudioSource != null)
                {
                    if (showRain) rainAudioSource.Play();
                    else rainAudioSource.Stop();
                }
            }

            bool showSnow = (currentWeather == WeatherType.Snowy);
            if (snowEffectContainer != null)
            {
                snowEffectContainer.SetActive(showSnow);
                if (snowAudioSource != null)
                {
                    if (showSnow) snowAudioSource.Play();
                    else snowAudioSource.Stop();
                }
            }
        }

        public static string GetSeasonDisplayName(SeasonType season)
        {
            switch (season)
            {
                case SeasonType.Spring: return "🌸 Mùa Xuân (Spring)";
                case SeasonType.Summer: return "☀️ Mùa Hạ (Summer)";
                case SeasonType.Autumn: return "🍂 Mùa Thu (Autumn)";
                case SeasonType.Winter: return "❄️ Mùa Đông (Winter)";
                default: return "🌸 Mùa Xuân";
            }
        }

        public static string GetWeatherDisplayName(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Rainy: return "🌧️ Mưa (Cây lớn +50%)";
                case WeatherType.Snowy: return "❄️ Tuyết rơi";
                case WeatherType.Cloudy: return "⛅ Nhiều mây";
                case WeatherType.Sunny:
                default:
                    return "☀️ Nắng ấm";
            }
        }

        private void SetupWeatherVisuals()
        {
            SetupRainVisuals();
            SetupSnowVisuals();
        }

        private void SetupRainVisuals()
        {
            if (rainEffectContainer != null) return;

            rainEffectContainer = new GameObject("RainEffect_MobileOptimized");
            rainEffectContainer.transform.SetParent(transform, false);

            rainParticleSystem = rainEffectContainer.AddComponent<ParticleSystem>();
            var main = rainParticleSystem.main;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 16f;
            main.startSize = 0.08f;
            main.maxParticles = 120; // Lightweight for Android mobile performance
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.7f, 0.85f, 1f, 0.55f);

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = 80;

            var shape = rainParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 1f, 20f);

            var vel = rainParticleSystem.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(-18f);

            ParticleSystemRenderer rend = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.75f, 0.88f, 1f, 0.55f);
            rend.material = mat;

            rainAudioSource = rainEffectContainer.AddComponent<AudioSource>();
            rainAudioSource.loop = true;
            rainAudioSource.volume = 0.22f;
            rainAudioSource.playOnAwake = false;
            rainAudioSource.clip = CreateRainSoundClip();

            rainEffectContainer.SetActive(false);
        }

        private void SetupSnowVisuals()
        {
            if (snowEffectContainer != null) return;

            snowEffectContainer = new GameObject("SnowEffect_MobileOptimized");
            snowEffectContainer.transform.SetParent(transform, false);

            snowParticleSystem = snowEffectContainer.AddComponent<ParticleSystem>();
            var main = snowParticleSystem.main;
            main.loop = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 2.5f;
            main.startSize = 0.14f;
            main.maxParticles = 80; // Very lightweight for Android
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.95f, 0.97f, 1.0f, 0.80f);

            var emission = snowParticleSystem.emission;
            emission.rateOverTime = 25;

            var shape = snowParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(22f, 1f, 22f);

            var vel = snowParticleSystem.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(-3.5f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            ParticleSystemRenderer rend = snowParticleSystem.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.96f, 0.98f, 1.0f, 0.85f);
            rend.material = mat;

            snowAudioSource = snowEffectContainer.AddComponent<AudioSource>();
            snowAudioSource.loop = true;
            snowAudioSource.volume = 0.16f;
            snowAudioSource.playOnAwake = false;
            snowAudioSource.clip = CreateWindSoundClip();

            snowEffectContainer.SetActive(false);
        }

        private AudioClip CreateRainSoundClip()
        {
            int sampleRate = 22050;
            float duration = 2.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float last = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float white = (UnityEngine.Random.value * 2f - 1f) * 0.12f;
                last = Mathf.Lerp(last, white, 0.25f);
                samples[i] = last;
            }

            AudioClip clip = AudioClip.Create("SoftRainAmbient", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateWindSoundClip()
        {
            int sampleRate = 22050;
            float duration = 2.5f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float phase = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                phase += 0.005f;
                float whisper = Mathf.Sin(phase * 4f) * 0.05f + (UnityEngine.Random.value * 2f - 1f) * 0.03f;
                samples[i] = whisper;
            }

            AudioClip clip = AudioClip.Create("SoftWinterWindAmbient", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
