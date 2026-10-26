using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// AI script for Pigs inside Pig Pen.
    /// Supports Idle breathing, Walk with 4-leg articulated stride synced to speed, and Eat/Rooting state.
    /// Includes fence margin avoidance and penBoundary clamping.
    /// </summary>
    public class PigWanderer : MonoBehaviour
    {
        public enum PigState
        {
            Idle = 0,
            Walking = 1,
            Eating = 2
        }

        public AnimalPenBoundary penBoundary;

        [Header("Movement")]
        public float moveSpeed = 0.85f;
        public float turnSpeed = 6f;
        public float idleMinDuration = 2.0f;
        public float idleMaxDuration = 5.0f;
        [Range(0f, 1f)] public float eatChance = 0.45f;

        private PigState currentState = PigState.Idle;
        private float stateTimer = 0f;
        private Vector3 targetDestination;
        private float animOffset;
        private float gaitPhase;
        private float currentSpeed;

        // Articulated parts
        private Transform bodyRoot;
        private Transform headPivot;
        private Transform earL;
        private Transform earR;
        private Transform tailPivot;
        private readonly Transform[] legs = new Transform[4];

        private Vector3 bodyBaseLocalPos;
        private Quaternion earLBaseRot;
        private Quaternion earRBaseRot;

        public PigState CurrentState => currentState;

        void Awake()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Pig);
            EnsureUpgradedVisuals();
            CacheTransforms();
        }

        void Start()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Pig);
            animOffset = Random.Range(0f, 10f);
            if (penBoundary == null)
            {
                penBoundary = GetComponentInParent<AnimalPenBoundary>();
                if (penBoundary == null)
                {
                    AnimalPenBoundary[] allPens = FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None);
                    foreach (var pen in allPens)
                    {
                        if (pen != null && pen.penType == "pig_pen")
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
                transform.position = penBoundary.ClampPosition(transform.position, 0.75f);
            }

            SnapFlushToGround();
            EnsureUpgradedVisuals();
            CacheTransforms();
            StartIdle();
        }

        public void EnsureUpgradedVisuals()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Pig);
            Transform checkBody = transform.Find("PigBody");
            Transform checkLeg = transform.Find("PigLeg_1");
            Transform checkHead = checkBody != null ? checkBody.Find("PigHead") : null;

            if (checkBody == null || checkLeg == null || checkHead == null || checkBody.localPosition.y < 0.42f)
            {
                FarmVisualFactory.BuildPigVisuals(gameObject);
            }
            else
            {
                FarmVisualFactory.EnsureBlobShadow(transform, 0.95f, 1.45f, 0.008f);
            }

            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc == null) bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.75f, 1.00f, 1.45f);
            bc.center = new Vector3(0f, 0.50f, 0.05f);
        }

        private void CacheTransforms()
        {
            bodyRoot = transform.Find("PigBody");
            if (bodyRoot != null)
            {
                bodyBaseLocalPos = bodyRoot.localPosition;
                headPivot = bodyRoot.Find("PigHead");
                if (headPivot != null)
                {
                    earL = headPivot.Find("PigEar_L");
                    earR = headPivot.Find("PigEar_R");
                    if (earL != null) earLBaseRot = earL.localRotation;
                    if (earR != null) earRBaseRot = earR.localRotation;
                }
                tailPivot = bodyRoot.Find("PigTail");
            }

            for (int i = 0; i < 4; i++)
            {
                legs[i] = transform.Find($"PigLeg_{i + 1}");
            }
        }

        void Update()
        {
            if (bodyRoot == null || legs[0] == null)
            {
                EnsureUpgradedVisuals();
                CacheTransforms();
            }

            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case PigState.Idle:
                    currentSpeed = 0f;
                    if (stateTimer <= 0f)
                    {
                        if (Random.value < eatChance) StartEating();
                        else StartWandering();
                    }
                    break;

                case PigState.Eating:
                    currentSpeed = 0f;
                    if (stateTimer <= 0f)
                    {
                        if (Random.value < 0.65f) StartWandering();
                        else StartIdle();
                    }
                    break;

                case PigState.Walking:
                    UpdateWander();
                    break;
            }

            if (penBoundary != null)
            {
                transform.position = penBoundary.ClampPosition(transform.position, 0.75f);
            }

            SnapFlushToGround();
            AnimatePigRig();
        }

        public void StartIdle()
        {
            currentState = PigState.Idle;
            stateTimer = Random.Range(idleMinDuration, idleMaxDuration);
            currentSpeed = 0f;
        }

        public void StartEating()
        {
            currentState = PigState.Eating;
            stateTimer = Random.Range(2.8f, 5.5f);
            currentSpeed = 0f;
        }

        public void StartWandering()
        {
            if (penBoundary != null)
            {
                targetDestination = penBoundary.GetRandomPositionInside(0.75f);
            }
            else
            {
                targetDestination = transform.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            }
            targetDestination.y = GroundSnapUtility.GetGroundY(targetDestination);

            currentState = PigState.Walking;
            stateTimer = Random.Range(3.5f, 6.5f);
        }

        private void UpdateWander()
        {
            Vector3 diff = targetDestination - transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist < 0.30f || stateTimer <= 0f)
            {
                if (Random.value < eatChance) StartEating();
                else StartIdle();
                return;
            }

            Vector3 moveDir = diff / dist;
            if (penBoundary != null && !penBoundary.IsInside(transform.position + moveDir * 0.75f, 0.75f))
            {
                Vector3 toCenter = penBoundary.WorldBounds.center - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    moveDir = Vector3.Slerp(moveDir, toCenter.normalized, 0.65f).normalized;
                    targetDestination = penBoundary.GetRandomPositionInside(0.75f);
                }
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            Vector3 before = transform.position;
            Vector3 after = before + transform.forward * (moveSpeed * Time.deltaTime);
            if (penBoundary != null)
                after = penBoundary.ClampPosition(after, 0.75f);

            transform.position = after;

            Vector3 delta = after - before;
            delta.y = 0f;
            currentSpeed = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            gaitPhase += currentSpeed * ((Mathf.PI * 2f) / 0.50f) * Time.deltaTime;
        }

        private void AnimatePigRig()
        {
            float t = Time.time + animOffset;

            if (bodyRoot != null)
            {
                float breatheY = Mathf.Sin(t * 2.6f) * 0.006f;
                float walkBobY = (currentState == PigState.Walking) ? Mathf.Abs(Mathf.Sin(gaitPhase)) * 0.015f : 0f;
                float waddleRoll = (currentState == PigState.Walking) ? Mathf.Sin(gaitPhase) * 3.2f : 0f;

                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, breatheY + walkBobY, 0f);
                bodyRoot.localRotation = Quaternion.Euler(0f, 0f, waddleRoll);
            }

            if (headPivot != null)
            {
                Quaternion desiredHead;
                if (currentState == PigState.Eating)
                {
                    float rootWave = Mathf.Sin(t * 8.0f) * 4.5f;
                    float sideRoot = Mathf.Cos(t * 3.5f) * 6.0f;
                    desiredHead = Quaternion.Euler(28f + rootWave, sideRoot, 0f);
                }
                else if (currentState == PigState.Walking)
                {
                    desiredHead = Quaternion.Euler(Mathf.Sin(gaitPhase * 2f) * 3.5f, 0f, 0f);
                }
                else
                {
                    desiredHead = Quaternion.Euler(Mathf.Sin(t * 2.0f) * 2.0f, Mathf.Sin(t * 0.8f) * 5.0f, 0f);
                }

                headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, desiredHead, Time.deltaTime * 8f);
            }

            if (earL != null && earR != null)
            {
                float flap = Mathf.Sin(t * 3.8f) * 6f;
                earL.localRotation = earLBaseRot * Quaternion.Euler(flap, 0f, flap * 0.5f);
                earR.localRotation = earRBaseRot * Quaternion.Euler(flap, 0f, -flap * 0.5f);
            }

            if (tailPivot != null)
            {
                float wag = Mathf.Sin(t * 6.5f) * 16f;
                tailPivot.localRotation = Quaternion.Euler(0f, wag, wag * 0.5f);
            }

            float[] offsets = { 0f, Mathf.PI, Mathf.PI, 0f };
            for (int i = 0; i < 4; i++)
            {
                if (legs[i] == null) continue;
                if (currentState == PigState.Walking && currentSpeed > 0.02f)
                {
                    float swing = Mathf.Sin(gaitPhase + offsets[i]) * 24f;
                    legs[i].localRotation = Quaternion.Lerp(legs[i].localRotation, Quaternion.Euler(swing, 0f, 0f), Time.deltaTime * 15f);
                }
                else
                {
                    legs[i].localRotation = Quaternion.Lerp(legs[i].localRotation, Quaternion.identity, Time.deltaTime * 10f);
                }
            }
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
