using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Manages Farm lighting and environment visuals.
    /// Provides neutral balanced daytime illumination without blown-out whites,
    /// soft natural shadows, and the target neutral gray checkered farm ground styling.
    /// Light square: RGB (155, 160, 158)
    /// Dark square:  RGB (125, 132, 130)
    /// </summary>
    [ExecuteAlways]
    public class FarmLightingOptimizer : MonoBehaviour
    {
        [Header("Main Sun Settings (Natural Daytime Light)")]
        public Color sunColor = new Color(1.0f, 0.98f, 0.94f);
        public float sunIntensity = 1.0f; // Balanced intensity to prevent blown-out ground
        public float shadowStrength = 0.34f; // Soft pleasant shadows, moderate contrast
        public Vector3 sunAngles = new Vector3(48f, -35f, 0f);

        [Header("Fill Light Settings (Soft Ambient Skylight)")]
        public Color fillColor = new Color(0.86f, 0.90f, 0.94f);
        public float fillIntensity = 0.25f;
        public Vector3 fillAngles = new Vector3(42f, 145f, 0f);

        [Header("Ambient Trilight Settings (Neutral Bright Daylight Tone)")]
        public Color ambientSky = new Color(0.74f, 0.78f, 0.80f);
        public Color ambientEquator = new Color(0.65f, 0.68f, 0.64f);
        public Color ambientGround = new Color(0.48f, 0.52f, 0.44f);
        public float ambientIntensity = 1.0f;

        [Header("Atmosphere / Fog")]
        public bool enableAtmosphericFog = false; // Disabled to eliminate white haze glare
        public Color fogColor = new Color(0.80f, 0.84f, 0.86f);
        public float fogDensity = 0.0004f;

        [Header("Ground / Terrain Theme (Natural Pasture Grass & Soil)")]
        public bool autoApplyGroundTheme = true;
        public Vector2 checkerTileSize = new Vector2(24f, 24f);

        private static Texture2D s_CachedGroundTex;
        private int updateCheckCount = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnBootSceneLoaded()
        {
            CheckAndApplyForCurrentScene();
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndApplyForCurrentScene();
        }

        public static void CheckAndApplyForCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.Equals("Farm", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureOptimizer();
            }
        }

        public static FarmLightingOptimizer EnsureOptimizer()
        {
            FarmLightingOptimizer existing = FindAnyObjectByType<FarmLightingOptimizer>();
            if (existing != null)
            {
                existing.ApplyLighting();
                return existing;
            }

            GameObject go = new GameObject("FarmLightingOptimizer");
            FarmLightingOptimizer opt = go.AddComponent<FarmLightingOptimizer>();
            opt.ApplyLighting();
            return opt;
        }

        void Awake()
        {
            ApplyLighting();
        }

        void Start()
        {
            ApplyLighting();
        }

        void OnEnable()
        {
            ApplyLighting();
        }

        void Update()
        {
            // Continuously guarantee active camera
            if (Camera.main == null && Object.FindFirstObjectByType<Camera>() == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player == null) player = GameObject.FindGameObjectWithTag("Player");
                FarmSceneInitializer.EnsureMainCamera(player);
            }

            // Verify and maintain ground styling for the first 10 frames
            if (updateCheckCount < 10)
            {
                updateCheckCount++;
                if (updateCheckCount == 5 || updateCheckCount == 10)
                {
                    ApplyLighting();
                }
            }
        }

        public void ApplyLighting()
        {
            // 1. Configure Main Directional Light (Sun) - clamped to prevent over-exposure
            sunIntensity = Mathf.Clamp(sunIntensity, 0.85f, 1.15f);
            shadowStrength = Mathf.Clamp(shadowStrength, 0.25f, 0.45f);

            Light mainSun = null;
            Light fillLight = null;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l != null && l.type == LightType.Directional)
                {
                    if (l.gameObject.name.Contains("Fill"))
                    {
                        fillLight = l;
                    }
                    else if (mainSun == null)
                    {
                        mainSun = l;
                    }
                }
            }

            if (mainSun == null)
            {
                GameObject sunGo = new GameObject("Directional Light (Sun)");
                mainSun = sunGo.AddComponent<Light>();
                mainSun.type = LightType.Directional;
            }

            mainSun.name = "Directional Light (Sun)";
            mainSun.color = sunColor;
            mainSun.intensity = sunIntensity;
            mainSun.shadows = LightShadows.Soft;
            mainSun.shadowStrength = shadowStrength;
            mainSun.transform.rotation = Quaternion.Euler(sunAngles);

            // 1b. Configure Secondary Fill Light so animals & crops are never too dark on shadow side
            if (fillLight == null)
            {
                GameObject fillGo = new GameObject("Directional Light (Fill)");
                fillLight = fillGo.AddComponent<Light>();
                fillLight.type = LightType.Directional;
            }

            fillLight.name = "Directional Light (Fill)";
            fillLight.color = fillColor;
            fillLight.intensity = fillIntensity;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(fillAngles);

            // 2. Configure Environment Ambient Lighting
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.ambientLight = new Color(0.60f, 0.64f, 0.58f);
            RenderSettings.reflectionIntensity = 0.5f;

            // 3. Configure Atmospheric Fog
            RenderSettings.fog = enableAtmosphericFog;
            if (enableAtmosphericFog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }

            // 4. Ensure working Main Camera with pleasant sky background
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = FarmSceneInitializer.EnsureMainCamera(player);
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.64f, 0.77f, 0.88f);
            }

            // 5. Enhance Terrain basemap view distance & natural grass/soil ground styling
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                terrain.basemapDistance = Mathf.Max(terrain.basemapDistance, 1500f);
                terrain.treeBillboardDistance = Mathf.Max(terrain.treeBillboardDistance, 400f);
                if (autoApplyGroundTheme)
                {
                    ApplyTerrainGroundTheme(terrain);
                }
            }

            // 6. Apply natural meadow grass/soil theme to any ground MeshRenderers
            ApplyMeshGroundTheme();

            // 7. Ensure small grass tufts, moist soil patches, dirt paths, and tree wind sway
            EnsureEnvironmentDetails();
        }

        /// <summary>
        /// Applies natural bright neutral meadow grass & soil texture to Terrain layers.
        /// </summary>
        public void ApplyTerrainGroundTheme(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null) return;

            Texture2D groundTex = GetOrGenerateCheckeredGroundTexture();

            TerrainLayer[] layers = terrain.terrainData.terrainLayers;
            if (layers != null && layers.Length > 0)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    if (layer == null) continue;

                    if (groundTex != null)
                    {
                        layer.diffuseTexture = groundTex;
                    }
                    layer.tileSize = checkerTileSize;
                    layer.metallic = 0.0f;
                    layer.smoothness = 0.0f;
                    layer.specular = Color.black;
                }
            }
            else if (groundTex != null)
            {
                TerrainLayer newLayer = new TerrainLayer();
                newLayer.name = "NaturalMeadowGrassSoil";
                newLayer.diffuseTexture = groundTex;
                newLayer.tileSize = checkerTileSize;
                newLayer.metallic = 0.0f;
                newLayer.smoothness = 0.0f;
                newLayer.specular = Color.black;
                terrain.terrainData.terrainLayers = new TerrainLayer[] { newLayer };
            }

            try
            {
                if (terrain.terrainData.alphamapLayers > 0)
                {
                    int res = terrain.terrainData.alphamapResolution;
                    float[,,] maps = terrain.terrainData.GetAlphamaps(0, 0, res, res);
                    int numLayers = terrain.terrainData.alphamapLayers;
                    for (int y = 0; y < res; y++)
                    {
                        for (int x = 0; x < res; x++)
                        {
                            maps[y, x, 0] = 1.0f;
                            for (int l = 1; l < numLayers; l++)
                            {
                                maps[y, x, l] = 0.0f;
                            }
                        }
                    }
                    terrain.terrainData.SetAlphamaps(0, 0, maps);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[FarmLightingOptimizer] Alphamap update note: " + ex.Message);
            }

            if (terrain.materialTemplate != null)
            {
                Material mat = terrain.materialTemplate;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            }
        }

        /// <summary>
        /// Scans all MeshRenderers in the scene and applies the natural meadow grass/soil theme to ground planes.
        /// </summary>
        public void ApplyMeshGroundTheme()
        {
            Color targetTint = Color.white;
            Texture2D groundTex = GetOrGenerateCheckeredGroundTexture();

            var allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string lower = r.gameObject.name.ToLower();
                if (lower.Contains("car") || lower.Contains("vehicle") || lower.Contains("tractor") ||
                    lower.Contains("garage") || lower.Contains("straw") || lower.Contains("barn") ||
                    lower.Contains("pen") || lower.Contains("fence") || lower.Contains("wall") ||
                    lower.Contains("stable") || lower.Contains("coop") || lower.Contains("house") ||
                    lower.Contains("building") || lower.Contains("shed") || lower.Contains("prop") ||
                    lower.Contains("rock") || lower.Contains("tree") || lower.Contains("plot") ||
                    lower.Contains("soil") || lower.Contains("dirt") || lower.Contains("shadow"))
                {
                    continue;
                }

                bool isGround = lower.Contains("ground") || lower.Contains("floor") ||
                                lower.Contains("terrain") || lower.Contains("grid") || lower.Contains("land");

                if (isGround)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null)
                        {
                            if (m.HasProperty("_Color")) m.SetColor("_Color", targetTint);
                            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", targetTint);
                            if (m.HasProperty("_MainTex") && groundTex != null) m.SetTexture("_MainTex", groundTex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up any leftover environment detail discs (MoistSoilApron, PenDirtPatch, DirtPathStep)
        /// that rendered as brown/gray circular discs on the ground.
        /// </summary>
        public void EnsureEnvironmentDetails()
        {
            CleanUpExtraneousGroundDiscs();
        }

        public static void CleanUpExtraneousGroundDiscs()
        {
            GameObject envRoot = GameObject.Find("FarmEnvironmentDetails");
            if (envRoot != null)
            {
                Debug.Log($"[FarmLightingOptimizer] Purging environment details container '{envRoot.name}' ({envRoot.transform.childCount} children).");
                if (Application.isPlaying)
                    Destroy(envRoot);
                else
                    DestroyImmediate(envRoot);
            }

            // Also clean up any loose MoistSoilApron, PenDirtPatch, DirtPathStep, MeadowGrassTuft in scene
            string[] markerPrefixes = new string[] { "MoistSoilApron", "PenDirtPatch", "DirtPathStep", "MeadowGrassTuft" };
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r == null || r.gameObject == null) continue;
                string goName = r.gameObject.name;
                bool shouldPurge = false;
                for (int i = 0; i < markerPrefixes.Length; i++)
                {
                    if (goName.StartsWith(markerPrefixes[i], System.StringComparison.OrdinalIgnoreCase))
                    {
                        shouldPurge = true;
                        break;
                    }
                }

                // Check for orphan or plot BlobShadows (animals keep their soft shadow, but plots and grass do not)
                if (!shouldPurge && goName == "BlobShadow")
                {
                    Transform p = r.transform.parent;
                    if (p != null)
                    {
                        string pName = p.name;
                        if (pName == "Crops" || pName.StartsWith("CropPlot") || pName.StartsWith("Plot") ||
                            pName.StartsWith("MeadowGrassTuft") || pName.Contains("Soil") || pName.Contains("EnvironmentDetails"))
                        {
                            shouldPurge = true;
                        }
                    }
                    else
                    {
                        shouldPurge = true;
                    }
                }

                // Clean up any stray edit indicators if edit mode is not active
                if (!shouldPurge && (goName == "SelectionIndicator_Temp" || goName == "DragIndicator_Temp"))
                {
                    if (!FarmEditModeController.IsEditModeActive)
                    {
                        shouldPurge = true;
                    }
                }

                if (shouldPurge)
                {
                    Debug.Log($"[FarmLightingOptimizer] Purging ground disc / marker object '{goName}'.");
                    if (Application.isPlaying)
                        Destroy(r.gameObject);
                    else
                        DestroyImmediate(r.gameObject);
                }
            }
        }

        /// <summary>
        /// Generates the natural pasture grass & soil ground texture (replaces the overly simple 2-tone gray checkerboard).
        /// Bright, neutral, organic, and easy to read in gameplay.
        /// </summary>
        public static Texture2D GetOrGenerateCheckeredGroundTexture()
        {
            if (s_CachedGroundTex != null) return s_CachedGroundTex;

            s_CachedGroundTex = CreateProceduralCheckeredGroundTexture(512, 4);
            return s_CachedGroundTex;
        }

        /// <summary>
        /// Creates a natural, bright, neutral low-poly meadow grass & soil texture with subtle field plots,
        /// organic soil patches, moist earth variations, and soft grass blade grain.
        /// </summary>
        public static Texture2D CreateProceduralCheckeredGroundTexture(int size = 512, int gridDiv = 4)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = "Procedural_NaturalMeadowSoil_Ground";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            // Natural, balanced, non-neon meadow greens and warm earth tones
            Color meadowLight = new Color(0.46f, 0.59f, 0.37f);
            Color meadowSoft = new Color(0.42f, 0.55f, 0.34f);
            Color cloverTint = new Color(0.38f, 0.52f, 0.31f);
            Color warmSoilTint = new Color(0.49f, 0.43f, 0.32f);
            Color moistEarthTint = new Color(0.39f, 0.35f, 0.27f);

            Color[] pixels = new Color[size * size];
            int squareSize = Mathf.Max(1, size / gridDiv);
            float twoPi = Mathf.PI * 2.0f;

            for (int y = 0; y < size; y++)
            {
                int gy = y / squareSize;
                float ny = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    int gx = x / squareSize;
                    float nx = (float)x / size;

                    // Very gentle, wide-blended pasture plot variation (no harsh checkerboard edge)
                    float plotWaveX = 0.5f + 0.5f * Mathf.Sin(nx * twoPi * (gridDiv * 0.5f));
                    float plotWaveY = 0.5f + 0.5f * Mathf.Sin(ny * twoPi * (gridDiv * 0.5f));
                    float plotFactor = Mathf.SmoothStep(0f, 1f, (plotWaveX + plotWaveY) * 0.5f);
                    Color baseCol = Color.Lerp(meadowSoft, meadowLight, plotFactor);

                    // Seamless organic multi-frequency waves for natural soil & clover patches
                    float n1 = Mathf.Sin(nx * twoPi * 3.0f + ny * twoPi * 2.0f) *
                               Mathf.Cos(nx * twoPi * 2.0f - ny * twoPi * 4.0f);
                    float n2 = Mathf.Sin(nx * twoPi * 7.0f - ny * twoPi * 5.0f) *
                               Mathf.Sin(nx * twoPi * 5.0f + ny * twoPi * 6.0f);
                    float n3 = Mathf.Cos(nx * twoPi * 19.0f + ny * twoPi * 17.0f);

                    float cloverMask = Mathf.Clamp01((n1 + 0.3f) * 0.45f);
                    baseCol = Color.Lerp(baseCol, cloverTint, cloverMask * 0.38f);

                    // Organic soil & moist earth veins
                    float soilMask = Mathf.Clamp01((n2 - 0.15f) * 0.65f);
                    baseCol = Color.Lerp(baseCol, warmSoilTint, soilMask * 0.32f);

                    float moistMask = Mathf.Clamp01((-n1 * n2 - 0.20f) * 0.70f);
                    baseCol = Color.Lerp(baseCol, moistEarthTint, moistMask * 0.22f);

                    // Fine grass blade micro-variation
                    float micro = n3 * 0.014f;
                    baseCol.r = Mathf.Clamp01(baseCol.r + micro);
                    baseCol.g = Mathf.Clamp01(baseCol.g + micro * 1.2f);
                    baseCol.b = Mathf.Clamp01(baseCol.b + micro * 0.8f);

                    pixels[y * size + x] = baseCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            return tex;
        }
    }
}
