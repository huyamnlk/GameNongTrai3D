using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// AI script for Chickens inside ChickenCoop.
    /// Supports Idle, Walk (with 2-leg alternating stride & head bob), and Peck/Eat state.
    /// Includes fence margin avoidance and penBoundary clamping.
    /// </summary>
    public class ChickenWanderer : MonoBehaviour
    {
        public enum ChickenState
        {
            Idle = 0,
            Walking = 1,
            Pecking = 2
        }

        public AnimalPenBoundary penBoundary;

        [Header("Movement")]
        public float moveSpeed = 0.85f;
        public float turnSpeed = 8f;
        public float idleMinDuration = 2.0f;
        public float idleMaxDuration = 4.5f;
        [Range(0f, 1f)] public float peckChance = 0.50f;

        private ChickenState currentState = ChickenState.Idle;
        private float stateTimer = 0f;
        private Vector3 targetDestination;
        private float animOffset;
        private float gaitPhase;
        private float currentSpeed;

        // Articulated parts
        private Transform bodyRoot;
        private Transform headPivot;
        private Transform wingL;
        private Transform wingR;
        private Transform tailPivot;
        private Transform legL;
        private Transform legR;

        private Vector3 bodyBaseLocalPos;
        private Quaternion tailBaseRot;

        public ChickenState CurrentState => currentState;

        void Awake()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Chicken);
            EnsureUpgradedVisuals();
            CacheTransforms();
        }

        void Start()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Chicken);
            animOffset = Random.Range(0f, 10f);
            if (penBoundary == null)
            {
                penBoundary = GetComponentInParent<AnimalPenBoundary>();
                if (penBoundary == null)
                {
                    AnimalPenBoundary[] allPens = FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None);
                    foreach (var pen in allPens)
                    {
                        if (pen != null && pen.penType == "chicken_coop")
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
                transform.position = penBoundary.ClampPosition(transform.position, 0.55f);
            }

            SnapFlushToGround();
            EnsureUpgradedVisuals();
            CacheTransforms();
            StartIdle();
        }

        public void EnsureUpgradedVisuals()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Chicken);
            Transform checkBody = transform.Find("ChickenBody");
            Transform checkLeg = transform.Find("ChickenLeg_L");
            Transform checkHead = checkBody != null ? checkBody.Find("ChickenHead") : null;

            if (checkBody == null || checkLeg == null || checkHead == null || checkBody.localPosition.y < 0.28f)
            {
                FarmVisualFactory.BuildChickenVisuals(gameObject);
            }
            else
            {
                FarmVisualFactory.EnsureBlobShadow(transform, 0.48f, 0.58f, 0.008f);
            }

            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc == null) bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.45f, 0.68f, 0.55f);
            bc.center = new Vector3(0f, 0.34f, 0f);
        }

        private void CacheTransforms()
        {
            bodyRoot = transform.Find("ChickenBody");
            if (bodyRoot != null)
            {
                bodyBaseLocalPos = bodyRoot.localPosition;
                headPivot = bodyRoot.Find("ChickenHead");
                wingL = bodyRoot.Find("ChickenWing_L");
                wingR = bodyRoot.Find("ChickenWing_R");
                tailPivot = bodyRoot.Find("ChickenTail");
                if (tailPivot != null) tailBaseRot = tailPivot.localRotation;
            }
            legL = transform.Find("ChickenLeg_L");
            legR = transform.Find("ChickenLeg_R");
        }

        void Update()
        {
            if (bodyRoot == null || legL == null)
            {
                EnsureUpgradedVisuals();
                CacheTransforms();
            }

            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case ChickenState.Idle:
                    currentSpeed = 0f;
                    if (stateTimer <= 0f)
                    {
                        if (Random.value < peckChance) StartPecking();
                        else StartWandering();
                    }
                    break;

                case ChickenState.Pecking:
                    currentSpeed = 0f;
                    if (stateTimer <= 0f)
                    {
                        if (Random.value < 0.65f) StartWandering();
                        else StartIdle();
                    }
                    break;

                case ChickenState.Walking:
                    UpdateWander();
                    break;
            }

            if (penBoundary != null)
            {
                transform.position = penBoundary.ClampPosition(transform.position, 0.55f);
            }

            SnapFlushToGround();
            AnimateChickenRig();
        }

        public void StartIdle()
        {
            currentState = ChickenState.Idle;
            stateTimer = Random.Range(idleMinDuration, idleMaxDuration);
            currentSpeed = 0f;
        }

        public void StartPecking()
        {
            currentState = ChickenState.Pecking;
            stateTimer = Random.Range(2.2f, 4.5f);
            currentSpeed = 0f;
        }

        public void StartWandering()
        {
            if (penBoundary != null)
            {
                targetDestination = penBoundary.GetRandomPositionInside(0.60f);
            }
            else
            {
                targetDestination = transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            }
            targetDestination.y = GroundSnapUtility.GetGroundY(targetDestination);

            currentState = ChickenState.Walking;
            stateTimer = Random.Range(3.5f, 6.5f);
        }

        private void UpdateWander()
        {
            Vector3 direction = targetDestination - transform.position;
            direction.y = 0f;

            float dist = direction.magnitude;
            if (dist < 0.22f || stateTimer <= 0f)
            {
                if (Random.value < peckChance) StartPecking();
                else StartIdle();
                return;
            }

            Vector3 moveDir = direction / dist;
            if (penBoundary != null && !penBoundary.IsInside(transform.position + moveDir * 0.55f, 0.55f))
            {
                Vector3 toCenter = penBoundary.WorldBounds.center - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    moveDir = Vector3.Slerp(moveDir, toCenter.normalized, 0.65f).normalized;
                    targetDestination = penBoundary.GetRandomPositionInside(0.60f);
                }
            }

            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * turnSpeed);

            Vector3 before = transform.position;
            Vector3 after = before + transform.forward * (moveSpeed * Time.deltaTime);
            if (penBoundary != null)
                after = penBoundary.ClampPosition(after, 0.55f);

            transform.position = after;

            Vector3 delta = after - before;
            delta.y = 0f;
            currentSpeed = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            gaitPhase += currentSpeed * ((Mathf.PI * 2f) / 0.32f) * Time.deltaTime;
        }

        private void AnimateChickenRig()
        {
            float t = Time.time + animOffset;

            if (bodyRoot != null)
            {
                float breatheY = Mathf.Sin(t * 3.5f) * 0.004f;
                float walkBobY = (currentState == ChickenState.Walking) ? Mathf.Abs(Mathf.Sin(gaitPhase)) * 0.014f : 0f;
                float bodyPitch = 0f;
                float bodyRoll = 0f;

                if (currentState == ChickenState.Pecking)
                {
                    float peckPulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 8.5f)), 4f);
                    bodyPitch = 18f + peckPulse * 24f;
                }
                else if (currentState == ChickenState.Walking)
                {
                    bodyRoll = Mathf.Sin(gaitPhase) * 4.5f;
                }

                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, breatheY + walkBobY, 0f);
                bodyRoot.localRotation = Quaternion.Lerp(
                    bodyRoot.localRotation,
                    Quaternion.Euler(bodyPitch, 0f, bodyRoll),
                    Time.deltaTime * 14f);
            }

            if (headPivot != null)
            {
                Quaternion desiredHead;
                if (currentState == ChickenState.Pecking)
                {
                    float peckPulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 8.5f)), 4f);
                    desiredHead = Quaternion.Euler(22f + peckPulse * 32f, Mathf.Sin(t * 2.5f) * 8f, 0f);
                }
                else if (currentState == ChickenState.Walking)
                {
                    float nod = Mathf.Sin(gaitPhase * 2f) * 14f;
                    desiredHead = Quaternion.Euler(nod, 0f, 0f);
                }
                else
                {
                    float lookYaw = Mathf.Round(Mathf.Sin(t * 1.4f)) * 16f;
                    desiredHead = Quaternion.Euler(Mathf.Sin(t * 3.2f) * 3f, lookYaw, 0f);
                }

                headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, desiredHead, Time.deltaTime * 14f);
            }

            if (wingL != null && wingR != null)
            {
                float flap = (currentState == ChickenState.Walking)
                    ? Mathf.Abs(Mathf.Sin(gaitPhase)) * 10f
                    : Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.2f)), 10f) * 26f;
                wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }

            if (tailPivot != null)
            {
                float tailBob = Mathf.Sin(t * 4.0f) * 6f;
                tailPivot.localRotation = tailBaseRot * Quaternion.Euler(tailBob, 0f, 0f);
            }

            if (legL != null && legR != null)
            {
                if (currentState == ChickenState.Walking && currentSpeed > 0.02f)
                {
                    float swing = Mathf.Sin(gaitPhase) * 34f;
                    legL.localRotation = Quaternion.Lerp(legL.localRotation, Quaternion.Euler(swing, 0f, 0f), Time.deltaTime * 18f);
                    legR.localRotation = Quaternion.Lerp(legR.localRotation, Quaternion.Euler(-swing, 0f, 0f), Time.deltaTime * 18f);
                }
                else
                {
                    legL.localRotation = Quaternion.Lerp(legL.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                    legR.localRotation = Quaternion.Lerp(legR.localRotation, Quaternion.identity, Time.deltaTime * 12f);
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
