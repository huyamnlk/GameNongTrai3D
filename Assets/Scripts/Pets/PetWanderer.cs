using UnityEngine;

namespace IrishFarmSim
{
    public enum PetState
    {
        Idle,
        Walking,
        Sitting,
        Exploring
    }

    /// <summary>
    /// AI controller for Dogs and Cats roaming freely on the farm.
    /// Handles procedural 4-leg walking animation, tail wagging / ear twitching,
    /// natural behaviors (sit, rest, explore barns/houses), ground-snapping,
    /// and obstacle/fence avoidance so pets never walk through objects or leave the farm.
    /// Does not block player clicks or farming interactions.
    /// </summary>
    public class PetWanderer : MonoBehaviour
    {
        [Header("Pet Profile")]
        public string petId = "dog_golden";
        public PetKind kind = PetKind.Dog;
        public string coatId = "golden";
        public string petDisplayName = "Chó Vàng (Lucky)";

        [Header("Movement Settings")]
        public float walkSpeed = 1.9f;
        public float turnSpeed = 7.0f;
        public float obstacleCheckDistance = 2.4f;

        [Header("Runtime State")]
        public PetState currentState = PetState.Idle;
        public float stateTimer = 0f;

        // Visual transforms
        private Transform bodyRoot;
        private Transform headPivot;
        private Transform earL;
        private Transform earR;
        private Transform tailPivot;
        private Transform tailPivot2;
        private Transform tailPivot3;
        private Transform legFL;
        private Transform legFR;
        private Transform legBL;
        private Transform legBR;

        private Vector3 bodyBaseLocalPos;
        private Quaternion bodyBaseLocalRot;
        private Quaternion tailBaseRot;
        private float animPhase = 0f;
        private float gaitPhase = 0f;
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 targetDestination;
        private bool destinationSet = false;

        // Farm bounds cache (minX, maxX, minZ, maxZ)
        private static Rect playableBounds = new Rect(28f, 175f, 185f, 125f);
        private static bool boundsCached = false;

        void Awake()
        {
            SetupCollision();
            ApplyScale();
        }

        void Start()
        {
            CacheFarmBounds();
            ApplyScale();
            RebuildVisuals();
            SnapToGroundExact();
            StartIdle();
        }

        private void SetupCollision()
        {
            // Pets have a lightweight trigger collider so raycasts from player clicks pass through or don't block
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = kind == PetKind.Dog ? 0.70f : 0.55f;
            sc.center = new Vector3(0f, kind == PetKind.Dog ? 0.55f : 0.42f, 0f);

            // Trigger collider so character movement and physical objects pass through freely
            // Layer 0 (Default) allows FarmInteractionController to detect pet clicks after crops & machines
            gameObject.layer = 0;
            foreach (Transform t in transform)
            {
                t.gameObject.layer = 0;
            }
        }

        public void ApplyScale()
        {
            float s = PetVisualFactory.GetScale(kind);
            transform.localScale = new Vector3(s, s, s);
        }

        public void SetupPet(string id, PetKind k, string coat, string displayName)
        {
            petId = id;
            kind = k;
            coatId = coat;
            petDisplayName = displayName;
            ApplyScale();
            RebuildVisuals();
            SetupCollision();
        }

        public void RebuildVisuals()
        {
            PetVisualFactory.Build(gameObject, kind, coatId);
            CacheRigTransforms();
        }

        private void CacheRigTransforms()
        {
            bodyRoot = transform.Find("PetBody");
            if (bodyRoot != null)
            {
                bodyBaseLocalPos = bodyRoot.localPosition;
                bodyBaseLocalRot = bodyRoot.localRotation;
                headPivot = bodyRoot.Find("PetHead");
                if (headPivot != null)
                {
                    earL = headPivot.Find("PetEar_L");
                    earR = headPivot.Find("PetEar_R");
                }
                tailPivot = bodyRoot.Find("PetTail");
                if (tailPivot != null)
                {
                    tailBaseRot = tailPivot.localRotation;
                    tailPivot2 = tailPivot.Find("PetTail2");
                    if (tailPivot2 != null)
                    {
                        tailPivot3 = tailPivot2.Find("PetTail3");
                    }
                }
            }

            legFL = transform.Find("PetLeg_FL");
            legFR = transform.Find("PetLeg_FR");
            legBL = transform.Find("PetLeg_BL");
            legBR = transform.Find("PetLeg_BR");
        }

        void Update()
        {
            if (bodyRoot == null)
            {
                RebuildVisuals();
            }

            animPhase += Time.deltaTime;
            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case PetState.Idle:
                    UpdateIdle();
                    break;
                case PetState.Sitting:
                    UpdateSitting();
                    break;
                case PetState.Walking:
                case PetState.Exploring:
                    UpdateWalking();
                    break;
            }

            SnapToGroundExact();
            AnimateRig();
        }

        private void UpdateIdle()
        {
            currentVelocity = Vector3.zero;
            if (stateTimer <= 0f)
            {
                float roll = Random.value;
                if (roll < 0.35f)
                {
                    StartSitting();
                }
                else if (roll < 0.75f)
                {
                    StartWandering();
                }
                else
                {
                    StartExploring();
                }
            }
        }

        private void UpdateSitting()
        {
            currentVelocity = Vector3.zero;
            if (stateTimer <= 0f)
            {
                if (Random.value < 0.6f)
                {
                    StartWandering();
                }
                else
                {
                    StartIdle();
                }
            }
        }

        private void UpdateWalking()
        {
            if (!destinationSet)
            {
                PickNewDestination();
            }

            Vector3 toTarget = targetDestination - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < 0.6f || stateTimer <= 0f)
            {
                destinationSet = false;
                if (Random.value < 0.45f) StartSitting();
                else StartIdle();
                return;
            }

            // Obstacle Avoidance: check forward ray
            Vector3 forward = transform.forward;
            if (Physics.Raycast(transform.position + Vector3.up * 0.4f, forward, out RaycastHit hit, obstacleCheckDistance))
            {
                // If it hits an obstacle (fences, building walls, water, rocks), steer away
                if (!hit.collider.isTrigger && !(hit.collider is TerrainCollider))
                {
                    // Steer perpendicular
                    Vector3 steer = Vector3.Cross(hit.normal, Vector3.up);
                    if (Vector3.Dot(steer, transform.right) < 0f) steer = -steer;
                    toTarget = (toTarget.normalized + steer * 1.5f).normalized;
                }
            }

            // Look towards direction
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }

            // Move forward
            float speed = walkSpeed * (currentState == PetState.Exploring ? 1.15f : 1.0f);
            Vector3 move = transform.forward * (speed * Time.deltaTime);

            // Clamp into playable bounds
            Vector3 nextPos = transform.position + move;
            nextPos.x = Mathf.Clamp(nextPos.x, playableBounds.xMin, playableBounds.xMax);
            nextPos.z = Mathf.Clamp(nextPos.z, playableBounds.yMin, playableBounds.yMax);

            transform.position = nextPos;
            currentVelocity = transform.forward * speed;
            gaitPhase += speed * (Mathf.PI * 2f / 0.55f) * Time.deltaTime;
        }

        public void StartIdle()
        {
            currentState = PetState.Idle;
            stateTimer = Random.Range(2.5f, 5.5f);
            destinationSet = false;
        }

        public void StartSitting()
        {
            currentState = PetState.Sitting;
            stateTimer = Random.Range(3.5f, 7.0f);
            destinationSet = false;
        }

        public void StartWandering()
        {
            currentState = PetState.Walking;
            stateTimer = Random.Range(4.0f, 8.5f);
            PickNewDestination();
        }

        public void StartExploring()
        {
            currentState = PetState.Exploring;
            stateTimer = Random.Range(5.0f, 10.0f);
            PickInterestPointDestination();
        }

        private void PickNewDestination()
        {
            destinationSet = true;
            // Pick a point 4 to 12 meters away in a random direction
            float dist = Random.Range(4.0f, 11.0f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Vector3 target = transform.position + offset;

            target.x = Mathf.Clamp(target.x, playableBounds.xMin + 3f, playableBounds.xMax - 3f);
            target.z = Mathf.Clamp(target.z, playableBounds.yMin + 3f, playableBounds.yMax - 3f);
            targetDestination = target;
        }

        private void PickInterestPointDestination()
        {
            destinationSet = true;
            // Explore near interesting locations: Barn, Coop, Machines, House, Tractor
            Vector3[] pointsOfInterest = new Vector3[]
            {
                new Vector3(72f, 0f, 245f),   // Cow Barn
                new Vector3(88f, 0f, 238f),   // Chicken Coop
                new Vector3(56f, 0f, 248f),   // Pig Pen
                new Vector3(104f, 0f, 235f),  // Mill
                new Vector3(112f, 0f, 245f),  // Bakery
                new Vector3(120f, 0f, 255f),  // Dairy
                new Vector3(128f, 0f, 240f),  // Jam Maker
                new Vector3(82f, 0f, 260f)    // Storage / House
            };

            Vector3 chosen = pointsOfInterest[Random.Range(0, pointsOfInterest.Length)];
            Vector3 jitter = new Vector3(Random.Range(-3.5f, 3.5f), 0f, Random.Range(-3.5f, 3.5f));
            Vector3 finalDest = chosen + jitter;

            finalDest.x = Mathf.Clamp(finalDest.x, playableBounds.xMin + 3f, playableBounds.xMax - 3f);
            finalDest.z = Mathf.Clamp(finalDest.z, playableBounds.yMin + 3f, playableBounds.yMax - 3f);
            targetDestination = finalDest;
        }

        private void SnapToGroundExact()
        {
            float groundY = GroundSnapUtility.GetGroundY(transform.position);
            Vector3 p = transform.position;
            if (Mathf.Abs(p.y - groundY) > 0.002f)
            {
                p.y = groundY;
                transform.position = p;
            }
        }

        private void CacheFarmBounds()
        {
            if (boundsCached) return;
            boundsCached = true;

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                Vector3 tp = terrain.transform.position;
                Vector3 ts = terrain.terrainData.size;
                // Leave a safe margin inside terrain boundaries
                playableBounds = new Rect(tp.x + 24f, tp.z + 160f, ts.x * 0.70f, ts.z * 0.45f);
            }
        }

        // ----------------------------------------------------------- Articulated Rig Animation
        private void AnimateRig()
        {
            float t = animPhase;
            bool isMoving = (currentState == PetState.Walking || currentState == PetState.Exploring) && currentVelocity.sqrMagnitude > 0.01f;
            bool isSitting = currentState == PetState.Sitting;

            // 1. Body Animation
            if (bodyRoot != null)
            {
                float targetPitch = 0f;
                float targetY = bodyBaseLocalPos.y;
                float roll = 0f;

                if (isSitting)
                {
                    // Body sits tilted back slightly, rear closer to ground
                    targetPitch = kind == PetKind.Dog ? -18f : -15f;
                    targetY = bodyBaseLocalPos.y * 0.72f;
                }
                else if (isMoving)
                {
                    float bob = Mathf.Abs(Mathf.Sin(gaitPhase * 2f)) * 0.02f;
                    targetY = bodyBaseLocalPos.y + bob;
                    roll = Mathf.Sin(gaitPhase) * 3.5f;
                }
                else
                {
                    // Gentle breathing
                    targetY = bodyBaseLocalPos.y + Mathf.Sin(t * 3.0f) * 0.004f;
                }

                bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, new Vector3(bodyBaseLocalPos.x, targetY, bodyBaseLocalPos.z), Time.deltaTime * 10f);
                bodyRoot.localRotation = Quaternion.Slerp(bodyRoot.localRotation, bodyBaseLocalRot * Quaternion.Euler(targetPitch, 0f, roll), Time.deltaTime * 10f);
            }

            // 2. Head Animation
            if (headPivot != null)
            {
                float headYaw = 0f;
                float headPitch = 0f;

                if (isSitting)
                {
                    // Look around curiously
                    headYaw = Mathf.Sin(t * 1.5f) * 18f;
                    headPitch = (kind == PetKind.Dog ? 12f : 8f) + Mathf.Sin(t * 2.8f) * 4f;
                }
                else if (isMoving)
                {
                    // Bob head along with walk cycle
                    headPitch = Mathf.Sin(gaitPhase * 2f) * 6f;
                }
                else
                {
                    // Idle subtle glance
                    headYaw = Mathf.Sin(t * 0.8f) * 14f;
                    headPitch = Mathf.Sin(t * 2.0f) * 3f;
                }

                headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, Quaternion.Euler(headPitch, headYaw, 0f), Time.deltaTime * 10f);
            }

            // 3. Tail Animation
            if (tailPivot != null)
            {
                float tailWag = 0f;
                float tailPitch = 0f;

                if (kind == PetKind.Dog)
                {
                    // Dogs wag tail cheerfully, especially when moving or sitting
                    float wagSpeed = isMoving ? 14f : (isSitting ? 12f : 6f);
                    float wagAmp = isMoving ? 28f : (isSitting ? 32f : 16f);
                    tailWag = Mathf.Sin(t * wagSpeed) * wagAmp;
                    tailPitch = isSitting ? 15f : 0f;
                    tailPivot.localRotation = tailBaseRot * Quaternion.Euler(tailPitch, tailWag, 0f);
                }
                else
                {
                    // Cats have graceful slow s-curve tail sways
                    float catSpeed = isMoving ? 5f : 3f;
                    float baseWag = Mathf.Sin(t * catSpeed) * 14f;
                    tailPivot.localRotation = tailBaseRot * Quaternion.Euler(0f, baseWag, 0f);
                    if (tailPivot2 != null)
                    {
                        tailPivot2.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * catSpeed - 0.6f) * 18f, 0f);
                    }
                    if (tailPivot3 != null)
                    {
                        tailPivot3.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * catSpeed - 1.2f) * 22f, 0f);
                    }
                }
            }

            // 4. Ears twitching occasionally
            if (earL != null && earR != null)
            {
                float twitch = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.8f)), 8f) * 10f;
                earL.localRotation = Quaternion.Euler(0f, 0f, 25f + twitch);
                earR.localRotation = Quaternion.Euler(0f, 0f, -25f - twitch);
            }

            // 5. 4-Leg Trot Walk Cycle (alternating diagonals: FL/BR vs FR/BL)
            if (legFL != null && legFR != null && legBL != null && legBR != null)
            {
                if (isMoving)
                {
                    float stride = 28f;
                    float swingDiag1 = Mathf.Sin(gaitPhase) * stride;
                    float swingDiag2 = -swingDiag1;

                    legFL.localRotation = Quaternion.Lerp(legFL.localRotation, Quaternion.Euler(swingDiag1, 0f, 0f), Time.deltaTime * 18f);
                    legBR.localRotation = Quaternion.Lerp(legBR.localRotation, Quaternion.Euler(swingDiag1, 0f, 0f), Time.deltaTime * 18f);

                    legFR.localRotation = Quaternion.Lerp(legFR.localRotation, Quaternion.Euler(swingDiag2, 0f, 0f), Time.deltaTime * 18f);
                    legBL.localRotation = Quaternion.Lerp(legBL.localRotation, Quaternion.Euler(swingDiag2, 0f, 0f), Time.deltaTime * 18f);
                }
                else if (isSitting)
                {
                    // Front legs straight down, back legs tucked underneath
                    legFL.localRotation = Quaternion.Lerp(legFL.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                    legFR.localRotation = Quaternion.Lerp(legFR.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                    legBL.localRotation = Quaternion.Lerp(legBL.localRotation, Quaternion.Euler(-55f, 0f, 0f), Time.deltaTime * 12f);
                    legBR.localRotation = Quaternion.Lerp(legBR.localRotation, Quaternion.Euler(-55f, 0f, 0f), Time.deltaTime * 12f);
                }
                else
                {
                    // Idle standing rest
                    legFL.localRotation = Quaternion.Lerp(legFL.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                    legFR.localRotation = Quaternion.Lerp(legFR.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                    legBL.localRotation = Quaternion.Lerp(legBL.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                    legBR.localRotation = Quaternion.Lerp(legBR.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                }
            }
        }

        // Friendly interaction: if user clicks pet directly
        public void Pet()
        {
            StartSitting();
            stateTimer = 4.0f;
            string sound = kind == PetKind.Dog ? "Gâu gâu! 🐶" : "Meo meo~ 🐱";
            CropSelectionUI.ShowToast($"{petDisplayName}: {sound}\nThú cưng rất vui vẻ!");
        }
    }
}
