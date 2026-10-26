using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Runtime and Editor scene initializer for Farm.unity.
    /// Automatically guarantees all required components (Player, Camera, Terrain, Mobile Joystick, EventSystem)
    /// are properly configured and wired on scene load.
    /// </summary>
    [ExecuteAlways]
    public class FarmSceneInitializer : MonoBehaviour
    {
        private static int _lastPlayModeInitFrame = -999;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            _lastPlayModeInitFrame = -999;
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnBootLoaded()
        {
            CheckAndInit();
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndInit(scene.name);
        }

        private static void CheckAndInit(string sceneName = null)
        {
            string name = !string.IsNullOrEmpty(sceneName) ? sceneName : SceneManager.GetActiveScene().name;
            if (name.Equals("Farm", System.StringComparison.OrdinalIgnoreCase))
            {
                InitializeFarmScene();
            }
        }

        void Start()
        {
            InitializeFarmScene();
        }

        void LateUpdate()
        {
            // Fail-safe: If at any point during gameplay all cameras are missing or disabled, recover immediately
            if (Camera.main == null && Object.FindFirstObjectByType<Camera>() == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player == null) player = GameObject.FindGameObjectWithTag("Player");
                EnsureMainCamera(player);
            }

            // Continuous AudioListener deduplication
            DeduplicateAudioListeners();
        }

        public static void InitializeFarmScene()
        {
            AnimalManager.PurgeShowcaseAndSpriteArtifacts();

            if (Application.isPlaying)
            {
                if (Mathf.Abs(Time.frameCount - _lastPlayModeInitFrame) <= 2)
                {
                    return;
                }
                _lastPlayModeInitFrame = Time.frameCount;
            }

            Debug.Log("[FarmSceneInitializer] Initializing Farm scene components and controls...");

            // 0. Ensure core scene hierarchy groups exist (Restore from Resources if missing)
            EnsureCoreSceneGroups();

            // 1. Verify and configure Terrain
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
                if (tc == null)
                {
                    tc = terrain.gameObject.AddComponent<TerrainCollider>();
                    tc.terrainData = terrain.terrainData;
                    Debug.Log("[FarmSceneInitializer] Added missing TerrainCollider to active terrain.");
                }
                else if (tc.terrainData == null && terrain.terrainData != null)
                {
                    tc.terrainData = terrain.terrainData;
                }

                // Explicitly guarantee tag and layer on Main Terrain
                try
                {
                    terrain.gameObject.tag = "Terrain";
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[FarmSceneInitializer] Could not tag terrain: " + e.Message);
                }
            }

            // Always apply lighting and ground theme
            FarmLightingOptimizer.EnsureOptimizer();

            // 2. Verify and configure Player
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null) player = playerByTag;
            }

            if (player != null)
            {
                // Ensure CharacterController
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = player.AddComponent<CharacterController>();
                    Debug.Log("[FarmSceneInitializer] Added CharacterController to Player.");
                }

                // Properly account for Player transform lossyScale (Player scale is approx (10, 10, 10))
                float scaleY = player.transform.lossyScale.y;
                if (scaleY <= 0.001f) scaleY = 1f;

                cc.radius = 0.32f / scaleY;
                cc.height = 1.65f / scaleY;
                cc.center = new Vector3(0f, 0.85f / scaleY, 0f);
                cc.stepOffset = 0.30f / scaleY;
                cc.skinWidth = 0.02f / scaleY;
                cc.slopeLimit = 50f;
                cc.minMoveDistance = 0.0001f;

                // Requirement A: Single physics system only. Destroy conflicting Rigidbody on Player.
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    SafeDestroy(rb);
                    Debug.Log("[FarmSceneInitializer] Destroyed conflicting Rigidbody on Player.");
                }

                // Disable any extra non-character colliders so they don't fight CharacterController
                Collider[] extraCols = player.GetComponents<Collider>();
                foreach (var col in extraCols)
                {
                    if (!(col is CharacterController))
                    {
                        col.enabled = false;
                    }
                }

                // Requirement C: Destroy any AudioListener on Player and its children
                AudioListener[] pListeners = player.GetComponentsInChildren<AudioListener>(true);
                foreach (var al in pListeners)
                {
                    Debug.Log($"[FarmSceneInitializer] Destroyed AudioListener on Player ('{al.gameObject.name}'). AudioListener belongs exclusively on Main Camera.");
                    SafeDestroy(al);
                }

                // Check and remove any dummy speaker visual or marker objects under Player
                Transform[] playerChildren = player.GetComponentsInChildren<Transform>(true);
                foreach (var pt in playerChildren)
                {
                    if (pt == null || pt == player.transform) continue;
                    string n = pt.name.ToLower();
                    if (n.Contains("speaker") || n.Contains("loudspeaker") || n.Contains("soundicon") || n.Contains("audiolistener"))
                    {
                        Debug.Log($"[FarmSceneInitializer] Destroyed speaker visual object '{pt.name}' on Player.");
                        SafeDestroy(pt.gameObject);
                    }
                }

                // Ensure Movement component
                Movement movement = player.GetComponent<Movement>();
                if (movement == null)
                {
                    movement = player.AddComponent<Movement>();
                    Debug.Log("[FarmSceneInitializer] Added Movement to Player.");
                }

                // Ensure ClickToMove
                ClickToMove ctm = player.GetComponent<ClickToMove>();
                if (ctm == null)
                {
                    ctm = player.AddComponent<ClickToMove>();
                }

                // Ensure FarmInteractionController
                FarmInteractionController fic = player.GetComponent<FarmInteractionController>();
                if (fic == null)
                {
                    fic = player.AddComponent<FarmInteractionController>();
                }

                // Ensure AudioSource
                AudioSource audio = player.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = player.AddComponent<AudioSource>();
                    audio.playOnAwake = false;
                }

                // Ensure Player is on top of terrain
                if (terrain != null)
                {
                    float terrainHeight = terrain.SampleHeight(player.transform.position) + terrain.transform.position.y;
                    if (player.transform.position.y < terrainHeight)
                    {
                        Vector3 p = player.transform.position;
                        p.y = terrainHeight + 0.1f;
                        player.transform.position = p;
                        Debug.Log($"[FarmSceneInitializer] Adjusted Player position above terrain to Y={p.y}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[FarmSceneInitializer] Player GameObject not found in Farm scene!");
            }

            // 3. Verify and configure Main Camera (Guaranteed active, enabled, Display 1, tag MainCamera)
            Camera activeCam = EnsureMainCamera(player);

            // 4. Verify and configure strictly ONE EventSystem
            EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem activeEventSystem = null;

            if (allEventSystems == null || allEventSystems.Length == 0)
            {
                GameObject esGo = new GameObject("EventSystem");
                activeEventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Debug.Log("[FarmSceneInitializer] Created EventSystem with StandaloneInputModule.");
            }
            else
            {
                activeEventSystem = allEventSystems[0];
                for (int i = 1; i < allEventSystems.Length; i++)
                {
                    Debug.LogWarning($"[FarmSceneInitializer] Destroying duplicate EventSystem on '{allEventSystems[i].gameObject.name}'.");
                    SafeDestroy(allEventSystems[i].gameObject);
                }
            }

            if (activeEventSystem != null)
            {
                StandaloneInputModule sim = activeEventSystem.GetComponent<StandaloneInputModule>();
                if (sim == null)
                {
                    sim = activeEventSystem.gameObject.AddComponent<StandaloneInputModule>();
                }
                // Clean up any extraneous or conflicting input modules
                BaseInputModule[] allModules = activeEventSystem.GetComponents<BaseInputModule>();
                foreach (var mod in allModules)
                {
                    if (mod != sim)
                    {
                        SafeDestroy(mod);
                    }
                }
            }

            // 5. Ensure Mobile Virtual Joystick
            MobileJoystick.EnsureJoystickInScene();

            // 6. Clean up legacy Supplies Window & Rotating Cow trigger
            SuppliesUI.EnsureSuppliesUIInScene();
            GameObject oldSupplies = GameObject.Find("Supplies");
            if (oldSupplies != null)
            {
                Debug.Log("[FarmSceneInitializer] Destroying legacy rotating cow trigger ('Supplies').");
                SafeDestroy(oldSupplies);
            }
            Supplies[] allSuppliesComps = FindObjectsByType<Supplies>(FindObjectsSortMode.None);
            foreach (var sc in allSuppliesComps)
            {
                if (sc != null && sc.gameObject != null)
                {
                    SafeDestroy(sc.gameObject);
                }
            }

            // Also check for any standalone obsolete MartTransition and FarmTransition trigger objects in Farm
            MartTransition[] martTransitions = FindObjectsByType<MartTransition>(FindObjectsSortMode.None);
            foreach (var mt in martTransitions)
            {
                if (mt != null && mt.gameObject != null)
                {
                    Debug.Log("[FarmSceneInitializer] Destroying legacy MartTransition trigger.");
                    SafeDestroy(mt.gameObject);
                }
            }
            FarmTransition[] legacyFarmTransitions = FindObjectsByType<FarmTransition>(FindObjectsSortMode.None);
            foreach (var ft in legacyFarmTransitions)
            {
                if (ft != null && ft.gameObject != null)
                {
                    Debug.Log($"[FarmSceneInitializer] Destroying legacy FarmTransition trigger ('{ft.gameObject.name}').");
                    SafeDestroy(ft.gameObject);
                }
            }

            if (Application.isPlaying)
            {
                // 7. Ensure Inventory & Storage System
                InventorySystem.EnsureInstance();
                StorageUI.EnsureStorageUIInScene();

                // 8. Ensure Farming Plots & Crop Planting UI
                CropManager.EnsureInstance();
                CropPlotUI.EnsureUI();

                // 9. Ensure Animal Care & Product System
                AnimalManager.EnsureInstance();
                AnimalUI.EnsureUI();

                // 10. Ensure Player Progression (Level & XP)
                PlayerProgression.EnsureInstance();
                ProgressionUI.EnsureUI();

                // 10.5 Ensure Buildings & Machines & Pets Managers
                FarmBuildingManager.EnsureInstance();
                MachineManager machineManager = MachineManager.EnsureInstance();
                PetManager petManager = PetManager.EnsureInstance();

                // 10.6 Pre-register all map props (Straw, Ladder, Lumber, Fences, Decor) with stable IDs
                MovableFarmObject.ScanAndRegisterAllMapObjects();

                // 11. Ensure Save / Load System
                SaveLoadSystem.EnsureInstance();
                bool shouldLoadSave = false;
                if (GameController.Instance() != null)
                {
                    shouldLoadSave = GameController.Instance().loadPlayer && !GameController.Instance().newGame;
                }
                else
                {
                    shouldLoadSave = SaveLoadSystem.HasSave();
                }

                if (shouldLoadSave && SaveLoadSystem.HasSave())
                {
                    Debug.Log("[FarmSceneInitializer] Continuing saved game as requested by player selection.");
                    SaveLoadSystem.LoadGame();
                }
                else
                {
                    Debug.Log("[FarmSceneInitializer] Starting fresh new game defaults as requested by player selection.");
                    SaveLoadSystem.InitNewGameDefaults();
                    if (GameController.Instance() != null)
                    {
                        GameController.Instance().newGame = false;
                        GameController.Instance().loadPlayer = false;
                    }
                }

                // Production machines must be purchased by player in Shop (no free/sample starter machines).
                // Pets are initialized naturally without duplicates.
                if (PetManager.Instance != null)
                    PetManager.Instance.EnsureDefaultPets();

                // 12. Ensure Farm Edit Mode Controller
                FarmEditModeController.EnsureInstance();
            }

            // Requirement A: Purge redundant top-left Edit buttons ("CHỈNH SỬA (B)")
            GameObject strayHud = GameObject.Find("FarmEditModeHUDCanvas");
            if (strayHud != null) SafeDestroy(strayHud);
            GameObject strayBtn = GameObject.Find("Btn_🛠 CHỈNH SỬA (B)");
            if (strayBtn != null) SafeDestroy(strayBtn);
            GameObject strayEditBtn = GameObject.Find("HUD_EditFarmButton");
            if (strayEditBtn != null) SafeDestroy(strayEditBtn);

            // 13. Enforce single valid tractor and ground it flush (Requirements B & C)
            EnforceSingleValidTractor();

            // 14. Optimize Stone Wall colliders (keep LOD0 collider active, disable duplicate LOD1 collider and resolve narrow pinch traps)
            OptimizeStoneWallColliders(player);

            // 15. Align all fences, pens, and stone walls flush with terrain
            int alignedFences = FenceGroundAligner.AlignAllInScene();
            if (alignedFences > 0)
            {
                Debug.Log($"[FarmSceneInitializer] Ground-aligned {alignedFences} fence/wall segments and animal pens to terrain.");
            }

            // 16. Clean pen colliders, ensure wide entrance gates, and register interior props
            FarmBuilding[] allBuildings = Object.FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allBuildings)
            {
                if (b != null)
                {
                    b.SetupInteractionCollider();
                    b.CleanRedundantPenColliders();
                }
            }

            Debug.Log("[FarmSceneInitializer] All Farm scene components and gameplay systems initialized successfully.");
        }

        /// <summary>
        /// Inspects stone wall segments under Farm/Walls:
        /// - Keeps LOD0 BoxCollider enabled so stone walls remain solid physical boundaries.
        /// - Disables redundant LOD1/LOD2 colliders (e.g. 'stone_wall_lg_noVine_LOD1') on the same LODGroup that cause double-collider snagging.
        /// - Deduplicates overlapping wall segments placed at identical coordinates.
        /// - Ensures nearby straw props ('Straw 7', 'Straw 9', 'Straw 10') leave enough clearance (> 1.1m) from 'stone_wall_lg_noVine 27' so Player never gets wedged.
        /// </summary>
        public static void OptimizeStoneWallColliders(GameObject player = null)
        {
            LODGroup[] lodGroups = Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Vector3> registeredWallPositions = new List<Vector3>();
            int disabledLod1Colliders = 0;
            int deduplicatedOverlapWalls = 0;

            foreach (var lg in lodGroups)
            {
                if (lg == null || lg.gameObject == null) continue;
                string gName = lg.gameObject.name.ToLower();
                if (!gName.Contains("stone_wall") && !gName.Contains("wall")) continue;

                // Check if another stone wall segment is already at the exact same position (< 0.25m)
                Vector3 pos = lg.transform.position;
                bool isExactDuplicateWall = false;
                for (int i = 0; i < registeredWallPositions.Count; i++)
                {
                    if (Vector3.Distance(pos, registeredWallPositions[i]) < 0.25f)
                    {
                        isExactDuplicateWall = true;
                        break;
                    }
                }

                if (isExactDuplicateWall)
                {
                    Collider[] dupCols = lg.GetComponentsInChildren<Collider>(true);
                    foreach (var dc in dupCols)
                    {
                        if (dc != null && dc.enabled)
                        {
                            dc.enabled = false;
                            deduplicatedOverlapWalls++;
                        }
                    }
                    continue;
                }

                registeredWallPositions.Add(pos);

                // Keep LOD0 collider enabled, disable duplicate LOD1/LOD2 colliders on the same wall
                Collider lod0Collider = null;
                Transform[] children = lg.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child == null || child == lg.transform) continue;
                    if (child.name.EndsWith("_LOD0", System.StringComparison.OrdinalIgnoreCase))
                    {
                        lod0Collider = child.GetComponent<Collider>();
                        if (lod0Collider != null)
                        {
                            lod0Collider.enabled = true;
                        }
                    }
                }

                foreach (var child in children)
                {
                    if (child == null || child == lg.transform) continue;
                    if (child.name.EndsWith("_LOD1", System.StringComparison.OrdinalIgnoreCase) ||
                        child.name.EndsWith("_LOD2", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Collider lodCol = child.GetComponent<Collider>();
                        if (lodCol != null && lodCol.enabled && lod0Collider != null)
                        {
                            lodCol.enabled = false;
                            disabledLod1Colliders++;
                        }
                    }
                }
            }

            // Ensure Straw 7 / Straw 9 / Straw 10 next to stone_wall_lg_noVine 27 (X ~ 81.0, Z ~ 273) don't form a 0.61m V-trap for Player (0.64m diameter)
            GameObject farmRoot = GameObject.Find("Farm");
            if (farmRoot != null)
            {
                string[] pinchStraws = new string[] { "Straw 7", "Straw 9", "Straw 10" };
                foreach (var sName in pinchStraws)
                {
                    Transform strawT = farmRoot.transform.Find(sName);
                    if (strawT != null && strawT.position.x > 81.2f && strawT.position.x < 82.6f && strawT.position.z > 271f && strawT.position.z < 276f)
                    {
                        Vector3 sp = strawT.position;
                        sp.x = Mathf.Max(sp.x, 83.2f);
                        strawT.position = sp;
                    }
                }
            }

            if (disabledLod1Colliders > 0 || deduplicatedOverlapWalls > 0)
            {
                Debug.Log($"[FarmSceneInitializer] Stone wall colliders optimized: kept LOD0 colliders active, disabled {disabledLod1Colliders} duplicate LOD1 colliders and {deduplicatedOverlapWalls} stacked duplicate wall colliders.");
            }
        }

        private static void EnsureCoreSceneGroups()
        {
            // Check Game Objects (Terrain, Camera, Player, Light)
            if (Terrain.activeTerrain == null && GameObject.Find("Game Objects") == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Farm_GameObjects");
                if (prefab != null)
                {
                    GameObject go = Object.Instantiate(prefab);
                    go.name = "Game Objects";
                    go.transform.position = new Vector3(202.7074f, 20.94772f, 295.5883f);
                    go.transform.rotation = Quaternion.identity;
                    Debug.Log("[FarmSceneInitializer] Instantiated 'Farm_GameObjects' from Resources.");
                }
            }

            // Check Farm layout
            if (GameObject.Find("Farm") == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Farm_Layout");
                if (prefab != null)
                {
                    GameObject go = Object.Instantiate(prefab);
                    go.name = "Farm";
                    go.transform.position = new Vector3(87.37000f, 24.12000f, 241.3800f);
                    go.transform.rotation = new Quaternion(0f, 0.7162433f, 0f, 0.6978508f);
                    Debug.Log("[FarmSceneInitializer] Instantiated 'Farm_Layout' from Resources.");
                }
            }

            // Check Environment
            if (GameObject.Find("Environment") == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Farm_Environment");
                if (prefab != null)
                {
                    GameObject go = Object.Instantiate(prefab);
                    go.name = "Environment";
                    go.transform.position = new Vector3(151.5f, 23.58396f, 231.28f);
                    go.transform.rotation = Quaternion.identity;
                    Debug.Log("[FarmSceneInitializer] Instantiated 'Farm_Environment' from Resources.");
                }
            }

            // Check UI
            if (GameObject.Find("UI") == null && Object.FindFirstObjectByType<Canvas>() == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Farm_UI");
                if (prefab != null)
                {
                    GameObject go = Object.Instantiate(prefab);
                    go.name = "UI";
                    go.transform.position = new Vector3(88.80164f, 27.87320f, 235.8676f);
                    go.transform.rotation = Quaternion.identity;
                    Debug.Log("[FarmSceneInitializer] Instantiated 'Farm_UI' from Resources.");
                }
            }
        }

        public static Camera EnsureMainCamera(GameObject player)
        {
            // Find existing camera (including inactive)
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            if (cam == null)
            {
                GameObject camByTag = GameObject.FindGameObjectWithTag("MainCamera");
                if (camByTag != null) cam = camByTag.GetComponent<Camera>();
            }

            if (cam == null)
            {
                GameObject camByName = GameObject.Find("Main Camera");
                if (camByName != null) cam = camByName.GetComponent<Camera>();
            }

            // If still no camera, create one safely
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                try { camGo.tag = "MainCamera"; } catch {}

                if (player != null)
                {
                    camGo.transform.position = player.transform.position + new Vector3(0f, 6.5f, -9.5f);
                    camGo.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
                }
                else
                {
                    camGo.transform.position = new Vector3(100f, 20f, 90f);
                    camGo.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
                }
                Debug.Log("[FarmSceneInitializer] Created fallback Main Camera at " + camGo.transform.position);
            }

            // Guarantee GameObject is active and Camera component is enabled
            if (!cam.gameObject.activeSelf)
            {
                cam.gameObject.SetActive(true);
                Debug.Log("[FarmSceneInitializer] Activated Main Camera GameObject.");
            }
            if (!cam.enabled)
            {
                cam.enabled = true;
                Debug.Log("[FarmSceneInitializer] Enabled Camera component.");
            }

            // Guarantee Camera properties
            cam.targetDisplay = 0; // Display 1
            cam.cullingMask = -1;  // Render Everything
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.65f, 0.75f, 0.85f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = 60f;

            try
            {
                cam.gameObject.tag = "MainCamera";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[FarmSceneInitializer] Could not tag camera: " + ex.Message);
            }

            // Ensure strictly ONE active AudioListener in scene (assigned to Main Camera)
            DeduplicateAudioListeners(cam.gameObject);

            // Deduplicate camera BGM AudioSource into AudioManager so volume/mute controls work 100%
            AudioSource camAudio = cam.GetComponent<AudioSource>();
            if (camAudio != null && (camAudio.loop || (camAudio.clip != null && camAudio.clip.name.Contains("Ambient"))))
            {
                if (AudioManager.Instance != null && AudioManager.Instance.defaultBgmClip == null && camAudio.clip != null)
                {
                    AudioManager.Instance.defaultBgmClip = camAudio.clip;
                }
                camAudio.Stop();
                camAudio.enabled = false;
            }

            // Configure CameraController
            CameraController cc = cam.GetComponent<CameraController>();
            if (cc == null)
            {
                cc = cam.gameObject.AddComponent<CameraController>();
                Debug.Log("[FarmSceneInitializer] Added CameraController to Main Camera.");
            }
            cc.enabled = true;

            if (player != null)
            {
                cc.target = player;
                cc.followTarget = true;
                if (cc.difVec.sqrMagnitude < 1f)
                {
                    cc.difVec = new Vector3(0f, 6.5f, -9.5f);
                }

                Movement movement = player.GetComponent<Movement>();
                if (movement != null)
                {
                    movement.cameraController = cc;
                }
            }

            return cam;
        }

        public static void DeduplicateAudioListeners(GameObject preferredOwner = null)
        {
            // Requirement C: Preferred owner must always be Main Camera, never Player
            if (preferredOwner == null)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
                if (cam != null) preferredOwner = cam.gameObject;
            }

            AudioListener[] allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allListeners == null || allListeners.Length == 0)
            {
                if (preferredOwner != null)
                {
                    preferredOwner.AddComponent<AudioListener>();
                    Debug.Log("[FarmSceneInitializer] Added initial AudioListener to " + preferredOwner.name);
                }
                return;
            }

            AudioListener primary = null;

            // 1. Preferred owner or Main Camera (strictly non-Player)
            foreach (var l in allListeners)
            {
                if (l != null && IsCameraObject(l.gameObject))
                {
                    primary = l;
                    break;
                }
            }

            // 2. Camera component owner
            if (primary == null)
            {
                foreach (var l in allListeners)
                {
                    if (l != null && l.GetComponent<Camera>() != null)
                    {
                        primary = l;
                        break;
                    }
                }
            }

            // 3. Fallback: pick any listener that is NOT on or under Player
            if (primary == null)
            {
                foreach (var l in allListeners)
                {
                    if (l != null && !IsPlayerObject(l.gameObject))
                    {
                        primary = l;
                        break;
                    }
                }
            }

            // If still no valid primary, attach to Camera
            if (primary == null && preferredOwner != null)
            {
                primary = preferredOwner.AddComponent<AudioListener>();
            }

            for (int i = 0; i < allListeners.Length; i++)
            {
                AudioListener l = allListeners[i];
                if (l == null) continue;

                // Explicitly purge any listener on Player
                if (IsPlayerObject(l.gameObject))
                {
                    l.enabled = false;
                    Debug.Log($"[FarmSceneInitializer] Removed unwanted AudioListener on Player object '{l.gameObject.name}'.");
                    SafeDestroy(l);
                    continue;
                }

                if (l == primary)
                {
                    if (!l.enabled) l.enabled = true;
                    if (!l.gameObject.activeSelf) l.gameObject.SetActive(true);
                }
                else
                {
                    l.enabled = false;
                    Debug.Log($"[FarmSceneInitializer] Removed duplicate AudioListener on '{l.gameObject.name}'.");
                    SafeDestroy(l);
                }
            }
        }

        private static bool IsCameraObject(GameObject go)
        {
            if (go == null) return false;
            return go.CompareTag("MainCamera") || go.name == "Main Camera" || go.GetComponent<Camera>() != null;
        }

        private static bool IsPlayerObject(GameObject go)
        {
            if (go == null) return false;
            Transform root = go.transform.root;
            return go.CompareTag("Player") || go.name == "Player" || (root != null && (root.CompareTag("Player") || root.name == "Player"));
        }

        /// <summary>
        /// Requirements B & C:
        /// 1. Finds all tractors/tractor models in the scene.
        /// 2. Logs [Tractor] Existing tractors found: <count>
        /// 3. Identifies the single valid tractor (preserves save data if exists, otherwise default farm tractor).
        /// 4. Removes duplicate/floating tractors and logs [Tractor] Duplicate tractor removed: <name>
        /// 5. Logs [Tractor] Restored tractor: <name>
        /// 6. Snaps tires/wheels flush to ground without hardcoded Y values.
        /// </summary>
        public static void EnforceSingleValidTractor()
        {
            List<GameObject> tractorCandidates = new List<GameObject>();

            // 1. Look for legacy floating Transition trigger objects (which carry floating Car7A tractor model)
            FarmTransition[] transitions = Object.FindObjectsByType<FarmTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tr in transitions)
            {
                if (tr != null && tr.gameObject != null && !tractorCandidates.Contains(tr.gameObject))
                {
                    tractorCandidates.Add(tr.gameObject);
                }
            }

            // 2. Scan all scene transforms for tractor roots or Car7A tractor models
            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                string lower = t.name.ToLower();
                bool isTractor = lower.StartsWith("tractor") || lower == "farm tractor" || lower == "purchased_tractor";
                if (!isTractor)
                {
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.ToLower().Contains("car7a"))
                    {
                        isTractor = true;
                    }
                }

                if (isTractor)
                {
                    GameObject vRoot = GetVehicleRoot(t.gameObject);
                    if (vRoot != null && vRoot.name != "Farm" && vRoot.name != "Environment" && vRoot.name != "UI" && !tractorCandidates.Contains(vRoot))
                    {
                        tractorCandidates.Add(vRoot);
                    }
                }
            }

            // Requirement B.6: Log existing count
            Debug.Log($"[Tractor] Existing tractors found: {tractorCandidates.Count}");

            if (tractorCandidates.Count == 0)
            {
                Debug.LogWarning("[Tractor] No tractor found in scene.");
                return;
            }

            // 3. Determine the single valid tractor:
            // Prefer the legitimate Farm tractor ("Tractor 1" under Farm), never the floating Transition object
            GameObject validTractor = null;

            // Check if save data specifies a tractor
            if (SaveLoadSystem.HasSave())
            {
                validTractor = tractorCandidates.Find(g => g != null && g.name == "Tractor 1" && g.transform.parent != null && g.transform.parent.name == "Farm");
                if (validTractor == null)
                {
                    validTractor = tractorCandidates.Find(g => g != null && g.name.IndexOf("tractor", System.StringComparison.OrdinalIgnoreCase) >= 0 && g.name != "Transition");
                }
            }

            // Default farm tractor
            if (validTractor == null)
            {
                validTractor = tractorCandidates.Find(g => g != null && g.name == "Tractor 1");
            }
            if (validTractor == null)
            {
                validTractor = tractorCandidates.Find(g => g != null && g.name != "Transition" && g.GetComponent<FarmTransition>() == null);
            }
            if (validTractor == null && tractorCandidates.Count > 0)
            {
                validTractor = tractorCandidates[0];
            }

            // 4. Remove all duplicate / floating tractors
            foreach (var t in tractorCandidates)
            {
                if (t != null && t != validTractor)
                {
                    Debug.Log($"[Tractor] Duplicate tractor removed: {t.name}");
                    SafeDestroy(t);
                }
            }

            // 5. Restore and snap the single valid tractor
            if (validTractor != null)
            {
                if (!validTractor.activeSelf) validTractor.SetActive(true);

                MovableFarmObject movable = validTractor.GetComponent<MovableFarmObject>();
                if (movable == null) movable = validTractor.AddComponent<MovableFarmObject>();
                movable.objectId = "Tractor 1";
                movable.objectType = MovableType.Decoration;
                movable.displayName = "Máy cày (Tractor)";

                // Requirement C: Ground wheels flush to ground surface
                float finalY = GroundSnapUtility.SnapTractorToGroundExact(validTractor);
                movable.RecordCurrentTransform();

                Debug.Log($"[Tractor] Restored tractor: {validTractor.name}");
            }
        }

        private static GameObject GetVehicleRoot(GameObject go)
        {
            if (go == null) return null;
            Transform curr = go.transform;
            Transform root = curr;
            while (curr != null && curr.parent != null)
            {
                string pName = curr.parent.name;
                if (pName == "Farm" || pName == "Environment" || pName == "UI" || pName == "Game Objects" || curr.parent.parent == null)
                {
                    break;
                }
                curr = curr.parent;
                root = curr;
            }
            return root.gameObject;
        }

        /// <summary>
        /// Alias for EnforceSingleValidTractor to support legacy callers.
        /// </summary>
        public static void SnapAllTractorsAndVehiclesToGround()
        {
            EnforceSingleValidTractor();
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;

            // Absolute protection against destroying Camera, Player, or critical root objects
            if (obj is GameObject go)
            {
                if (go.CompareTag("MainCamera") || go.GetComponent<Camera>() != null || 
                    go.CompareTag("Player") || go.name == "Main Camera" || 
                    go.name == "Player" || go.name == "Game Objects" || 
                    go.name == "Farm" || go.name == "Environment")
                {
                    Debug.LogWarning($"[FarmSceneInitializer.SafeDestroy] Refusing to destroy critical object: {go.name}");
                    return;
                }
            }
            if (obj is Camera || obj is CameraController)
            {
                Debug.LogWarning($"[FarmSceneInitializer.SafeDestroy] Refusing to destroy camera component: {obj.name}");
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(obj);
#else
                DestroyImmediate(obj);
#endif
            }
        }
    }
}
