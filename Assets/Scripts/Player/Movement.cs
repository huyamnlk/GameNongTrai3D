using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Third Person Player/Third Person Controller")]
    public class Movement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float rotationDamping = 15f;
        public float Speed = 4.5f;
        public float gravity = 20f;
        public GameObject currentFocus;
        public AudioClip moveSound;
        public static bool freeRoam = true;

        private float moveSpeed;
        private float verticalVelocity = 0f;
        public CharacterController controller;
        public CameraController cameraController;
        public MobileJoystick mobileJoystick;
        public ClickToMove clickToMove;
        public FarmInteractionController interactionController;

        private Animator animator;
        private AudioSource audioSource;
        private VCAnalogJoystickBase joy;

        private float lastCollisionLogTime = 0f;
        private string lastHitColliderName = "";
        private float lastRecordedY = 0f;
        private float lastYLogTime = 0f;
        private float stuckTimer = 0f;
        private Vector3 lastStuckCheckPosition;
        private float lastStuckPositionSampleTime = 0f;

        void Start()
        {
            try
            {
                freeRoam = true;
                if (controller == null) controller = GetComponent<CharacterController>();

                // Configure CharacterController with proper scaling and stable ground parameters
                if (controller != null)
                {
                    float scaleY = transform.lossyScale.y;
                    if (scaleY <= 0.001f) scaleY = 1f;

                    controller.radius = 0.32f / scaleY;
                    controller.height = 1.65f / scaleY;
                    controller.center = new Vector3(0f, 0.85f / scaleY, 0f);
                    controller.stepOffset = 0.30f / scaleY;
                    controller.skinWidth = 0.02f / scaleY;
                    controller.slopeLimit = 50f;
                    controller.minMoveDistance = 0.0001f;
                }

                // Requirement A: Single physics system only. Destroy Rigidbody if present.
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Destroy(rb);
                    Debug.Log("[Movement] Destroyed conflicting Rigidbody on Player.");
                }

                // Disable any extra colliders on Player root so they don't fight CharacterController
                Collider[] extraCols = GetComponents<Collider>();
                foreach (var c in extraCols)
                {
                    if (!(c is CharacterController))
                    {
                        c.enabled = false;
                    }
                }

                // Requirement C: Remove any AudioListener on Player
                AudioListener al = GetComponent<AudioListener>();
                if (al != null)
                {
                    Destroy(al);
                    Debug.Log("[Movement] Removed AudioListener from Player (must reside solely on Main Camera).");
                }

                animator = GetComponent<Animator>();
                audioSource = GetComponent<AudioSource>();

                GameObject camGo = GameObject.FindGameObjectWithTag("MainCamera");
                if (camGo != null && cameraController == null)
                {
                    cameraController = camGo.GetComponent<CameraController>();
                }
                if (cameraController == null)
                {
                    cameraController = FindAnyObjectByType<CameraController>();
                }

                // Ensure ClickToMove component exists
                if (clickToMove == null) clickToMove = GetComponent<ClickToMove>();
                if (clickToMove == null)
                {
                    clickToMove = gameObject.AddComponent<ClickToMove>();
                }

                // Ensure FarmInteractionController exists
                if (interactionController == null) interactionController = GetComponent<FarmInteractionController>();
                if (interactionController == null)
                {
                    interactionController = gameObject.AddComponent<FarmInteractionController>();
                }

                // Ensure MobileJoystick exists in scene
                mobileJoystick = MobileJoystick.EnsureJoystickInScene();

                // Ensure stone wall duplicate LOD1 colliders are cleaned up
                FarmSceneInitializer.OptimizeStoneWallColliders(gameObject);

                // Initial one-time ground placement
                InitialGroundSnap();
                lastRecordedY = transform.position.y;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Movement] Initialization warning: " + e.Message);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null) return;
            if (hit.collider is TerrainCollider || hit.collider.CompareTag("Terrain")) return;
            if (hit.normal.y > 0.6f) return; // Walking smoothly on ground

            // If hitting a duplicate LOD1 stone wall collider while LOD0 collider exists on parent, disable the redundant LOD1 collider
            if (hit.collider.name.EndsWith("_LOD1", System.StringComparison.OrdinalIgnoreCase) && hit.collider.transform.parent != null)
            {
                Transform p = hit.collider.transform.parent;
                for (int i = 0; i < p.childCount; i++)
                {
                    Transform sibling = p.GetChild(i);
                    if (sibling != hit.collider.transform && sibling.name.EndsWith("_LOD0", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Collider lod0Col = sibling.GetComponent<Collider>();
                        if (lod0Col != null && lod0Col.enabled)
                        {
                            hit.collider.enabled = false;
                            return;
                        }
                    }
                }
            }

            // Requirement A.9: Debug log collision obstacles with throttling
            if (Time.time - lastCollisionLogTime > 2.0f)
            {
                lastCollisionLogTime = Time.time;
                lastHitColliderName = hit.collider.name;
                string rootName = hit.collider.transform.root != null ? hit.collider.transform.root.name : "None";
                Debug.Log($"[PlayerCollision] Blocked by: '{hit.collider.name}' (Parent: '{hit.collider.transform.parent?.name}', Root: '{rootName}') at {hit.point}");
            }
        }

        void Update()
        {
            moveSpeed = UpdateMovement();
            if (animator != null)
            {
                animator.SetFloat("Speed", moveSpeed, 0.1f, Time.deltaTime);
            }
        }

        void LateUpdate()
        {
            // Requirement A: Temporary debug log to identify any script or force altering Player Y unexpectedly
            if (controller != null && controller.isGrounded)
            {
                float deltaY = transform.position.y - lastRecordedY;
                if (Mathf.Abs(deltaY) > 0.15f && Time.time - lastYLogTime > 1.5f)
                {
                    Debug.Log($"[PlayerYMonitor] Player Y delta={deltaY:F3}m (Pos Y={transform.position.y:F3}, isGrounded={controller.isGrounded})");
                    lastYLogTime = Time.time;
                }
            }
            lastRecordedY = transform.position.y;
        }

        public float UpdateMovement()
        {
            if (controller == null) return 0f;
            if (FarmEditModeController.Instance != null && FarmEditModeController.Instance.IsEditMode) return 0f;

            // 1. Gather Input from Keyboard (WASD, Arrows)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Additional direct key fallbacks
            if (h == 0 && v == 0)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            }

            // 2. Gather Input from Mobile Joystick
            var activeJoy = mobileJoystick != null ? mobileJoystick : MobileJoystick.Instance;
            if (activeJoy != null && activeJoy.Direction.sqrMagnitude > 0.001f)
            {
                h = activeJoy.Horizontal;
                v = activeJoy.Vertical;
            }
            else
            {
                // Fallback to legacy VC joystick if present
                if (joy == null)
                {
                    try { joy = VCAnalogJoystickBase.GetInstance("stick"); } catch { }
                }
                if (joy != null && (Mathf.Abs(joy.AxisX) > 0.05f || Mathf.Abs(joy.AxisY) > 0.05f))
                {
                    h = joy.AxisX;
                    v = joy.AxisY;
                }
            }

            // Requirement A: Deadzone smoothing so input near zero does not induce oscillation
            if (Mathf.Abs(h) < 0.08f) h = 0f;
            if (Mathf.Abs(v) < 0.08f) v = 0f;

            Vector3 moveDirection = Vector3.zero;
            bool hasManualInput = Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f;

            if (hasManualInput)
            {
                // Cancel click-to-move when manual input is active
                if (clickToMove != null && clickToMove.HasTarget)
                {
                    clickToMove.CancelMove();
                }

                // Calculate camera-relative movement direction
                Transform camT = cameraController != null ? cameraController.transform : (Camera.main != null ? Camera.main.transform : null);
                if (camT != null)
                {
                    Vector3 camFwd = camT.forward;
                    Vector3 camRight = camT.right;
                    camFwd.y = 0;
                    camRight.y = 0;
                    camFwd.Normalize();
                    camRight.Normalize();

                    moveDirection = (camRight * h + camFwd * v);
                }
                else
                {
                    moveDirection = new Vector3(h, 0, v);
                }

                if (moveDirection.magnitude > 1f)
                {
                    moveDirection.Normalize();
                }
            }
            else if (clickToMove != null && clickToMove.HasTarget)
            {
                // 3. Click-to-Move direction
                moveDirection = clickToMove.GetMoveDirection(transform.position);
            }

            // Apply effective speed (prevent legacy 0.1f from making player appear stuck)
            float effectiveSpeed = Speed < 1.0f ? 4.5f : Speed;
            Vector3 horizontalVelocity = moveDirection * effectiveSpeed;

            // Apply Gravity and Grounding once, without per-frame teleporting
            if (controller.isGrounded)
            {
                verticalVelocity = -2.0f; // Firm downward grounding force
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
                if (verticalVelocity < -25f) verticalVelocity = -25f;
            }

            // Single movement execution pipeline through CharacterController.Move()
            Vector3 totalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(totalVelocity * Time.deltaTime);

            // Stuck detection & recovery check
            CheckAndRecoverStuck(hasManualInput);

            // Audio handling
            HandleFootstepAudio(moveDirection.sqrMagnitude > 0.01f);

            // Rotation handling
            if (freeRoam)
            {
                if (moveDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationDamping);
                }
            }
            else if (currentFocus != null)
            {
                Vector3 dir = (currentFocus.transform.position - transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * (rotationDamping / 3f));
                }
            }
            else
            {
                freeRoam = true;
            }

            // Out-of-bounds safety check: only recovers if player drastically falls through terrain
            RecoverIfFellThrough();

            return horizontalVelocity.magnitude;
        }

        private void HandleFootstepAudio(bool isMoving)
        {
            if (audioSource == null || moveSound == null) return;

            if (isMoving)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = moveSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource.isPlaying && audioSource.clip == moveSound)
                {
                    audioSource.Stop();
                }
            }
        }

        private void InitialGroundSnap()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                if (transform.position.y < terrainY || transform.position.y > terrainY + 2.0f)
                {
                    Vector3 p = transform.position;
                    p.y = terrainY + 0.05f;
                    transform.position = p;
                    verticalVelocity = 0f;
                }
            }
        }

        private void RecoverIfFellThrough()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                if (transform.position.y < terrainY - 4.0f)
                {
                    Vector3 p = transform.position;
                    p.y = terrainY + 0.15f;
                    transform.position = p;
                    verticalVelocity = 0f;
                    Debug.LogWarning($"[Movement] Recovered Player from falling below map (Position Y={p.y}).");
                }
            }
        }

        public void LookAt(GameObject target)
        {
            currentFocus = target;
            freeRoam = false;
        }

        private void CheckAndRecoverStuck(bool hasManualInput)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                UnstuckPlayer();
                return;
            }

            if (!hasManualInput || controller == null)
            {
                stuckTimer = 0f;
                return;
            }

            if (Time.time - lastStuckPositionSampleTime > 0.3f)
            {
                float horizontalDist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(lastStuckCheckPosition.x, lastStuckCheckPosition.z)
                );

                lastStuckCheckPosition = transform.position;
                lastStuckPositionSampleTime = Time.time;

                // Player is providing active movement input but barely moving horizontally while hitting side obstacles
                if (horizontalDist < 0.04f && (controller.collisionFlags & CollisionFlags.Sides) != 0)
                {
                    stuckTimer += 0.3f;
                    if (stuckTimer >= 1.5f)
                    {
                        UnstuckPlayer();
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = Mathf.Max(0f, stuckTimer - 0.2f);
                }
            }
        }

        /// <summary>
        /// Finds the nearest unobstructed ground point and repositions the player safely.
        /// </summary>
        public void UnstuckPlayer()
        {
            if (controller == null) return;

            Vector3 currentPos = transform.position;
            float r = controller.radius > 0.05f ? controller.radius : 0.32f;
            float h = controller.height > 0.5f ? controller.height : 1.65f;
            int mask = ~((1 << 2) | (1 << 5));

            // Concentric rings at 0.8m, 1.5m, 2.2m across 8 compass directions
            float[] radii = new float[] { 0.8f, 1.5f, 2.2f };
            Vector3 bestSafePos = Vector3.zero;
            bool foundSafe = false;

            for (int ring = 0; ring < radii.Length; ring++)
            {
                float radius = radii[ring];
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                    Vector3 candidate = currentPos + offset;

                    // Ground height
                    float groundY = candidate.y;
                    if (Terrain.activeTerrain != null)
                    {
                        groundY = Terrain.activeTerrain.SampleHeight(candidate) + Terrain.activeTerrain.transform.position.y;
                    }
                    else if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f, mask))
                    {
                        groundY = hit.point.y;
                    }

                    candidate.y = groundY + 0.05f;

                    // Check capsule clearance
                    Vector3 bottom = candidate + Vector3.up * (r + 0.02f);
                    Vector3 top = candidate + Vector3.up * (h - r - 0.02f);
                    if (!Physics.CheckCapsule(bottom, top, r * 0.9f, mask, QueryTriggerInteraction.Ignore))
                    {
                        bestSafePos = candidate;
                        foundSafe = true;
                        break;
                    }
                }
                if (foundSafe) break;
            }

            if (foundSafe)
            {
                controller.enabled = false;
                transform.position = bestSafePos;
                controller.enabled = true;
                verticalVelocity = 0f;
                Debug.Log($"[Movement] Unstuck player: repositioned from {currentPos} to free ground at {bestSafePos}");
                CropSelectionUI.ShowToast("Đã giải thoát nhân vật khỏi vị trí kẹt!");
            }
        }
    }
}