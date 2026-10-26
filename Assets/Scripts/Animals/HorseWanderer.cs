using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Realistic articulated behaviour & animation controller for Horses inside HorseStable paddock.
    /// Supports:
    /// - Idle breathing (chest expansion, gentle body rise, independent ear twitches, multi-segment tail swish)
    /// - Turning in place / lead-turn with neck & head looking into the turn and stepping feet
    /// - Walk (4-beat equine gait strictly synchronized to actual ground speed -> zero foot sliding)
    /// - Run / Trot (for longer wander segments inside the pen)
    /// - Grazing (neck lowers to ground, head/jaw chewing motion, ear & tail movement)
    /// - Animator Controller & BlendTree parameter synchronization ("Speed", "IsGrazing", "IsTurning", "TurnAngle", "State")
    /// - Strict confinement inside HorseStable via AnimalPenBoundary + wall margin avoidance
    /// </summary>
    public class HorseWanderer : MonoBehaviour
    {
        public enum HorseState
        {
            Idle = 0,
            Turning = 1,
            Walking = 2,
            Running = 3,
            Grazing = 4
        }

        public AnimalPenBoundary penBoundary;

        [Header("Movement")]
        public float moveSpeed = 1.15f;
        public float runSpeed = 1.85f;
        public float turnSpeed = 5.5f;
        public float idleMinDuration = 2.5f;
        public float idleMaxDuration = 5.5f;
        public float grazeMinDuration = 3.5f;
        public float grazeMaxDuration = 6.5f;
        [Range(0f, 1f)] public float grazeChance = 0.45f;
        [Range(0f, 1f)] public float runChance = 0.25f;

        [SerializeField] private HorseState currentState = HorseState.Idle;
        private float stateTimer = 0f;
        private Vector3 targetDestination;
        private float animOffset;
        private float gaitPhase;
        private float currentSpeed;
        private float signedTurnAngle;

        private Animator animator;

        // Articulated rig transforms
        private Transform bodyRoot;
        private Transform chestPart;
        private Transform neckPivot;
        private Transform headPivot;
        private Transform jawPart;
        private Transform earL;
        private Transform earR;
        private Transform tailRoot;
        private Transform tailMid;
        private Transform tailTip;

        // 4 Articulated Legs: FL(0), FR(1), BL(2), BR(3)
        private readonly Transform[] upperLegs = new Transform[4];
        private readonly Transform[] kneePivots = new Transform[4];

        // Rest poses
        private Vector3 bodyBaseLocalPos;
        private Vector3 chestBaseLocalScale = Vector3.one;
        private Quaternion neckBaseRot = Quaternion.Euler(32f, 0f, 0f);
        private Quaternion headBaseRot = Quaternion.Euler(24f, 0f, 0f);
        private Vector3 jawBaseLocalPos;
        private Quaternion earLBaseRot = Quaternion.Euler(-18f, -12f, 16f);
        private Quaternion earRBaseRot = Quaternion.Euler(-18f, 12f, -16f);
        private Quaternion tailRootBaseRot = Quaternion.Euler(22f, 0f, 0f);

        private static readonly int HashSpeed     = Animator.StringToHash("Speed");
        private static readonly int HashIsGrazing = Animator.StringToHash("IsGrazing");
        private static readonly int HashIsTurning = Animator.StringToHash("IsTurning");
        private static readonly int HashTurnAngle = Animator.StringToHash("TurnAngle");
        private static readonly int HashState     = Animator.StringToHash("State");

        public HorseState CurrentState => currentState;
        public float CurrentSpeed => currentSpeed;

        void Awake()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Horse);
            EnsureUpgradedVisuals();
            CacheTransforms();
        }

        void Start()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Horse);
            animOffset = Random.Range(0f, 10f);
            animator = GetComponent<Animator>();

            if (penBoundary == null)
            {
                penBoundary = GetComponentInParent<AnimalPenBoundary>();
                if (penBoundary == null)
                {
                    AnimalPenBoundary[] allPens = FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None);
                    foreach (var pen in allPens)
                    {
                        if (pen != null && pen.penType == "horse_stable")
                        {
                            penBoundary = pen;
                            break;
                        }
                    }
                }
            }

            if (penBoundary != null)
            {
                penBoundary.RegisterAnimal(gameObject);
                transform.position = penBoundary.ClampPosition(transform.position, 1.05f);
            }

            SnapFlushToGround();
            EnsureUpgradedVisuals();
            CacheTransforms();
            StartIdle();
        }

        /// <summary>
        /// Upgrades flat primitive hierarchy so scene horses and prefabs
        /// gain articulated legs, neck/head, ears, tail, and blob shadow with realistic proportions.
        /// </summary>
        public void EnsureUpgradedVisuals()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Horse);
            Transform checkLeg = transform.Find("HorseLeg_FL");
            Transform checkBody = transform.Find("HorseBody");
            Transform checkKnee = checkLeg != null ? checkLeg.Find("HorseKnee_FL") : null;

            if (checkLeg == null || checkBody == null || checkKnee == null || checkBody.localPosition.y < 1.1f)
            {
                FarmVisualFactory.BuildHorseVisuals(gameObject);
            }
            else
            {
                FarmVisualFactory.EnsureBlobShadow(transform, 1.25f, 2.45f, 0.008f);
            }

            // Synchronize BoxCollider to realistic horse proportions
            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc == null) bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.95f, 2.30f, 2.60f);
            bc.center = new Vector3(0f, 1.15f, 0.10f);
        }

        public void CacheTransforms()
        {
            bodyRoot = transform.Find("HorseBody");
            if (bodyRoot != null)
            {
                bodyBaseLocalPos = bodyRoot.localPosition;
                chestPart = bodyRoot.Find("HorseChest");
                if (chestPart != null) chestBaseLocalScale = chestPart.localScale;

                neckPivot = bodyRoot.Find("HorseNeck");
                if (neckPivot != null)
                {
                    neckBaseRot = neckPivot.localRotation;
                    headPivot = neckPivot.Find("HorseHead");
                    if (headPivot != null)
                    {
                        headBaseRot = headPivot.localRotation;
                        jawPart = headPivot.Find("HorseJaw");
                        if (jawPart != null) jawBaseLocalPos = jawPart.localPosition;
                        earL = headPivot.Find("HorseEar_L");
                        earR = headPivot.Find("HorseEar_R");
                        if (earL != null) earLBaseRot = earL.localRotation;
                        if (earR != null) earRBaseRot = earR.localRotation;
                    }
                }

                tailRoot = bodyRoot.Find("HorseTailRoot");
                if (tailRoot != null)
                {
                    tailRootBaseRot = tailRoot.localRotation;
                    tailMid = tailRoot.Find("HorseTailMid");
                    if (tailMid != null) tailTip = tailMid.Find("HorseTailTip");
                }
            }

            string[] tags = { "FL", "FR", "BL", "BR" };
            for (int i = 0; i < 4; i++)
            {
                upperLegs[i] = transform.Find($"HorseLeg_{tags[i]}");
                if (upperLegs[i] != null)
                {
                    kneePivots[i] = upperLegs[i].Find($"HorseKnee_{tags[i]}");
                }
            }
        }

        void Update()
        {
            if (bodyRoot == null || upperLegs[0] == null)
            {
                EnsureUpgradedVisuals();
                CacheTransforms();
            }

            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case HorseState.Idle:
                    UpdateIdle();
                    break;
                case HorseState.Turning:
                    UpdateTurning();
                    break;
                case HorseState.Walking:
                case HorseState.Running:
                    UpdateWander();
                    break;
                case HorseState.Grazing:
                    UpdateGrazing();
                    break;
            }

            if (penBoundary != null)
            {
                transform.position = penBoundary.ClampPosition(transform.position, 1.05f);
            }

            SnapFlushToGround();
            AnimateArticulatedRig();
            UpdateAnimatorParameters();
        }

        public void StartIdle()
        {
            currentState = HorseState.Idle;
            stateTimer = Random.Range(idleMinDuration, idleMaxDuration);
            currentSpeed = 0f;
            signedTurnAngle = 0f;
        }

        public void StartGrazing()
        {
            currentState = HorseState.Grazing;
            stateTimer = Random.Range(grazeMinDuration, grazeMaxDuration);
            currentSpeed = 0f;
            signedTurnAngle = 0f;
        }

        public void StartWandering()
        {
            if (penBoundary != null)
            {
                targetDestination = penBoundary.GetRandomPositionInside(1.05f);
            }
            else
            {
                Vector3 randOffset = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
                targetDestination = transform.position + randOffset;
            }

            targetDestination.y = GroundSnapUtility.GetGroundY(targetDestination);

            Vector3 toTarget = targetDestination - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.25f)
            {
                StartGrazing();
                return;
            }

            float angle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
            if (Mathf.Abs(angle) > 32f)
            {
                currentState = HorseState.Turning;
                stateTimer = 2.2f;
            }
            else
            {
                bool shouldRun = toTarget.magnitude > 2.2f && Random.value < runChance;
                currentState = shouldRun ? HorseState.Running : HorseState.Walking;
                stateTimer = Random.Range(4.5f, 8.0f);
            }
        }

        private void UpdateIdle()
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 2.5f);
            signedTurnAngle = Mathf.Lerp(signedTurnAngle, 0f, Time.deltaTime * 6f);

            if (stateTimer <= 0f)
            {
                if (Random.value < grazeChance)
                {
                    StartGrazing();
                }
                else
                {
                    StartWandering();
                }
            }
        }

        private void UpdateTurning()
        {
            Vector3 diff = targetDestination - transform.position;
            diff.y = 0f;
            if (diff.sqrMagnitude < 0.1f)
            {
                StartIdle();
                return;
            }

            Vector3 dir = diff.normalized;
            signedTurnAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);

            // Slight forward step during turn so feet step naturally
            float creepSpeed = moveSpeed * 0.22f;
            Vector3 before = transform.position;
            Vector3 after = before + transform.forward * (creepSpeed * Time.deltaTime);
            if (penBoundary != null)
                after = penBoundary.ClampPosition(after, 1.05f);
            transform.position = after;

            currentSpeed = creepSpeed;
            gaitPhase += Time.deltaTime * 4.5f;

            if (Mathf.Abs(signedTurnAngle) < 15f || stateTimer <= 0f)
            {
                bool shouldRun = diff.magnitude > 2.2f && Random.value < runChance;
                currentState = shouldRun ? HorseState.Running : HorseState.Walking;
                stateTimer = Random.Range(4.5f, 8.0f);
            }
        }

        private void UpdateGrazing()
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 3f);
            signedTurnAngle = Mathf.Lerp(signedTurnAngle, 0f, Time.deltaTime * 5f);

            if (stateTimer <= 0f)
            {
                if (Random.value < 0.6f)
                    StartWandering();
                else
                    StartIdle();
            }
        }

        private void UpdateWander()
        {
            Vector3 diff = targetDestination - transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist < 0.35f || stateTimer <= 0f)
            {
                if (Random.value < grazeChance)
                    StartGrazing();
                else
                    StartIdle();
                return;
            }

            Vector3 moveDir = diff / dist;

            // Smooth fence avoidance before reaching boundary
            if (penBoundary != null && !penBoundary.IsInside(transform.position + moveDir * 1.05f, 1.05f))
            {
                Vector3 center = penBoundary.WorldBounds.center;
                Vector3 toCenter = center - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    moveDir = Vector3.Slerp(moveDir, toCenter.normalized, 0.65f).normalized;
                    targetDestination = penBoundary.GetRandomPositionInside(1.05f);
                }
            }

            signedTurnAngle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            float targetSpd = (currentState == HorseState.Running) ? runSpeed : moveSpeed;
            float turnSlowdown = Mathf.Clamp01(1f - Mathf.Abs(signedTurnAngle) / 115f);
            float actualDesiredSpeed = targetSpd * Mathf.Max(0.35f, turnSlowdown);

            Vector3 before = transform.position;
            Vector3 after = before + transform.forward * (actualDesiredSpeed * Time.deltaTime);
            if (penBoundary != null)
                after = penBoundary.ClampPosition(after, 1.05f);

            transform.position = after;

            Vector3 delta = after - before;
            delta.y = 0f;
            currentSpeed = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);

            // Synchronize gait phase strictly to actual distance traveled (zero foot sliding!)
            float radPerMeter = (Mathf.PI * 2f) / 1.15f;
            gaitPhase += currentSpeed * radPerMeter * Time.deltaTime;
        }

        private void AnimateArticulatedRig()
        {
            float t = Time.time + animOffset;

            // 1. Body Breathing & Locomotion Bob
            if (bodyRoot != null)
            {
                float breatheY = Mathf.Sin(t * 2.1f) * 0.009f;
                float walkBobY = 0f;
                float bodyPitch = 0f;
                float bodyRoll = 0f;

                if (currentState == HorseState.Walking || currentState == HorseState.Running)
                {
                    float speedRatio = Mathf.Clamp01(currentSpeed / runSpeed);
                    walkBobY = Mathf.Abs(Mathf.Sin(gaitPhase)) * Mathf.Lerp(0.016f, 0.032f, speedRatio);
                    bodyPitch = Mathf.Sin(gaitPhase * 2f) * Mathf.Lerp(1.2f, 2.8f, speedRatio);
                    bodyRoll = Mathf.Cos(gaitPhase) * Mathf.Lerp(1.0f, 1.8f, speedRatio);
                }

                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, breatheY + walkBobY, 0f);
                bodyRoot.localRotation = Quaternion.Euler(bodyPitch, 0f, bodyRoll);

                if (chestPart != null)
                {
                    float chestExpand = 1f + Mathf.Sin(t * 2.1f) * 0.022f;
                    chestPart.localScale = new Vector3(
                        chestBaseLocalScale.x * chestExpand,
                        chestBaseLocalScale.y * chestExpand,
                        chestBaseLocalScale.z);
                }
            }

            // 2. Neck, Head & Jaw Articulation
            if (neckPivot != null)
            {
                Quaternion desiredNeckRot = neckBaseRot;
                Quaternion desiredHeadRot = headBaseRot;
                float yawLook = Mathf.Clamp(signedTurnAngle * 0.35f, -22f, 22f);

                if (currentState == HorseState.Grazing)
                {
                    float chewWave = Mathf.Sin(t * 6.5f) * 2.8f;
                    float browseShift = Mathf.Sin(t * 1.1f) * 6f;
                    desiredNeckRot = Quaternion.Euler(84f + chewWave * 0.4f, browseShift, 0f);
                    desiredHeadRot = Quaternion.Euler(-26f + chewWave, browseShift * 0.5f, 0f);

                    if (jawPart != null)
                    {
                        float jawChew = Mathf.Sin(t * 6.5f) * 0.008f;
                        jawPart.localPosition = jawBaseLocalPos + new Vector3(jawChew, -Mathf.Abs(jawChew) * 0.5f, 0f);
                    }
                }
                else if (currentState == HorseState.Walking || currentState == HorseState.Running)
                {
                    float nodAmp = (currentState == HorseState.Running) ? 5.5f : 3.6f;
                    float nod = Mathf.Sin(gaitPhase * 2f) * nodAmp;
                    desiredNeckRot = neckBaseRot * Quaternion.Euler(nod, yawLook * 0.6f, 0f);
                    desiredHeadRot = headBaseRot * Quaternion.Euler(-nod * 0.6f, yawLook * 0.4f, 0f);
                }
                else
                {
                    float idleNod = Mathf.Sin(t * 1.6f) * 1.8f;
                    float idleLook = (currentState == HorseState.Turning)
                        ? yawLook
                        : Mathf.Sin(t * 0.65f) * 5.5f;
                    desiredNeckRot = neckBaseRot * Quaternion.Euler(idleNod, idleLook * 0.5f, 0f);
                    desiredHeadRot = headBaseRot * Quaternion.Euler(-idleNod * 0.4f, idleLook * 0.6f, 0f);
                }

                neckPivot.localRotation = Quaternion.Slerp(neckPivot.localRotation, desiredNeckRot, Time.deltaTime * 6.5f);
                if (headPivot != null)
                {
                    headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, desiredHeadRot, Time.deltaTime * 7.5f);
                }
            }

            // 3. Natural Independent Ear Movement
            if (earL != null && earR != null)
            {
                float earWaveL = Mathf.Sin(t * 2.7f) * 8f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 0.9f)), 6f) * 18f;
                float earWaveR = Mathf.Cos(t * 2.3f + 1.2f) * 8f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.1f + 2f)), 6f) * 18f;
                earL.localRotation = earLBaseRot * Quaternion.Euler(earWaveL * 0.4f, earWaveL, 0f);
                earR.localRotation = earRBaseRot * Quaternion.Euler(earWaveR * 0.4f, -earWaveR, 0f);
            }

            // 4. Multi-Segment Tail Movement
            if (tailRoot != null)
            {
                float moveBoost = (currentState == HorseState.Walking || currentState == HorseState.Running) ? 1.55f : 1.0f;
                float swishMain = Mathf.Sin(t * 1.85f * moveBoost) * 9.5f * moveBoost;
                float flyWhisk = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 0.72f)), 8f) * 16f;
                float totalYaw = swishMain + flyWhisk;
                float lift = (currentState == HorseState.Running) ? 12f : ((currentState == HorseState.Walking) ? 5f : 0f);

                tailRoot.localRotation = tailRootBaseRot * Quaternion.Euler(lift, totalYaw * 0.45f, totalYaw * 0.55f);
                if (tailMid != null)
                {
                    float midWave = Mathf.Sin(t * 1.85f * moveBoost - 0.55f) * 12f * moveBoost;
                    tailMid.localRotation = Quaternion.Euler(4f, 0f, midWave);
                }
                if (tailTip != null)
                {
                    float tipWave = Mathf.Sin(t * 1.85f * moveBoost - 1.1f) * 14f * moveBoost;
                    tailTip.localRotation = Quaternion.Euler(2f, 0f, tipWave);
                }
            }

            // 5. 4-Leg Articulated Gait (FL, FR, BL, BR)
            float[] legPhaseOffsets = { 0f, Mathf.PI, Mathf.PI * 1.5f, Mathf.PI * 0.5f };

            bool isStepping = (currentState == HorseState.Walking || currentState == HorseState.Running || currentState == HorseState.Turning);
            float maxHipAngle = 0f;
            float maxKneeBend = 0f;

            if (currentState == HorseState.Walking || currentState == HorseState.Running)
            {
                float speedNorm = Mathf.Clamp01(currentSpeed / Mathf.Max(0.1f, moveSpeed));
                maxHipAngle = Mathf.Lerp(0f, currentState == HorseState.Running ? 32f : 24f, speedNorm);
                maxKneeBend = Mathf.Lerp(0f, currentState == HorseState.Running ? 38f : 26f, speedNorm);
            }
            else if (currentState == HorseState.Turning)
            {
                maxHipAngle = 10f;
                maxKneeBend = 16f;
            }

            for (int i = 0; i < 4; i++)
            {
                if (upperLegs[i] == null) continue;

                if (isStepping && maxHipAngle > 0.1f)
                {
                    float phase = gaitPhase + legPhaseOffsets[i];
                    float hipSwing = Mathf.Sin(phase) * maxHipAngle;
                    float liftFactor = Mathf.Max(0f, Mathf.Cos(phase));
                    float kneeFlex = liftFactor * maxKneeBend;

                    upperLegs[i].localRotation = Quaternion.Lerp(
                        upperLegs[i].localRotation,
                        Quaternion.Euler(hipSwing, 0f, 0f),
                        Time.deltaTime * 16f);

                    if (kneePivots[i] != null)
                    {
                        kneePivots[i].localRotation = Quaternion.Lerp(
                            kneePivots[i].localRotation,
                            Quaternion.Euler(kneeFlex, 0f, 0f),
                            Time.deltaTime * 16f);
                    }
                }
                else
                {
                    upperLegs[i].localRotation = Quaternion.Lerp(upperLegs[i].localRotation, Quaternion.identity, Time.deltaTime * 10f);
                    if (kneePivots[i] != null)
                    {
                        kneePivots[i].localRotation = Quaternion.Lerp(kneePivots[i].localRotation, Quaternion.identity, Time.deltaTime * 10f);
                    }
                }
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            animator.SetFloat(HashSpeed, currentSpeed);
            animator.SetBool(HashIsGrazing, currentState == HorseState.Grazing);
            animator.SetBool(HashIsTurning, currentState == HorseState.Turning);
            animator.SetFloat(HashTurnAngle, signedTurnAngle);
            animator.SetInteger(HashState, (int)currentState);
        }

        private void SnapFlushToGround()
        {
            float groundY = GroundSnapUtility.GetGroundY(transform.position);
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.y - groundY) > 0.002f)
            {
                pos.y = groundY;
                transform.position = pos;
            }
        }

        public void SimulateStep(float dt = 0.05f)
        {
            Update();
        }

        void OnDestroy()
        {
            if (penBoundary != null)
            {
                penBoundary.UnregisterAnimal(gameObject);
            }
        }
    }
}
