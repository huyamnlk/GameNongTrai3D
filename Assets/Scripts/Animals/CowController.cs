using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
    [System.Serializable]
    public class CowController : MonoBehaviour
    {
        private GameObject playerGO;
        private Animation anim;
        private CameraController cameraControl;
        private Movement movement;
        private VCAnalogJoystickBase joyStick;

        private Vector3 targetDest;
        private Vector3 finalDest;
        private Vector3 lastDirection;
        private Vector3 oldAngle = Vector3.zero;

        private float speed = 1.35f;
        private float rotationSpeed = 5.5f;
        private float width = 1.2f;
        private float heigth = 1.2f;
        private string state = "idle";
        private bool inMart;
        private bool cowGameObject;
        private bool idleRunning;
        private bool cowSelected;
        private float moveTimer = 0f;

        public AudioClip cowSound;
        public Cow cow;
        public FarmUI userInterface;
        public AnimalPenBoundary penBoundary;

        void Awake()
        {
            AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Cow);
            FarmVisualFactory.EnhanceCowVisuals(gameObject);
        }

        void Start()
        {
            try
            {
                AnimalScaleConfig.ApplyScale(gameObject, AnimalType.Cow);
                GroundSnapUtility.SnapToGroundExact(gameObject);
                FarmVisualFactory.EnhanceCowVisuals(gameObject);

                if (cow == null)
                {
                    cow = new Cow("Bò sữa (Cow)", 4, "Holstein Friesian", 8, 60, false, true, 250, "Farm");
                }
                if (string.IsNullOrEmpty(cow.currScene))
                {
                    cow.currScene = GameController.GetSceneName();
                }

                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    width = Mathf.Max(0.8f, col.bounds.size.x);
                    heigth = Mathf.Max(0.8f, col.bounds.size.x);
                }
                anim = GetComponent<Animation>();
                if (anim == null) anim = GetComponentInChildren<Animation>();

                if (anim != null)
                {
                    foreach (AnimationState aState in anim)
                    {
                        aState.wrapMode = WrapMode.Loop;
                    }
                }

                InitUIControl();
                CheckInMart();

                if (!inMart)
                {
                    speed = 1.25f;
                    rotationSpeed = 5.5f;
                    if (string.IsNullOrEmpty(state)) state = "idle";
                }
                else
                {
                    speed = 5.0f;
                    rotationSpeed = 12f;
                }

                if (penBoundary == null && !inMart)
                {
                    penBoundary = GetComponentInParent<AnimalPenBoundary>();
                    if (penBoundary == null)
                    {
                        GameObject cpGo = GameObject.Find("CowPen_Large") ?? GameObject.Find("CowPen_Small") ?? GameObject.Find("Purchased_CowBarn") ?? GameObject.Find("CowPenPrefab");
                        if (cpGo != null)
                        {
                            penBoundary = cpGo.GetComponent<AnimalPenBoundary>() ?? cpGo.GetComponentInChildren<AnimalPenBoundary>(true);
                        }
                    }
                    if (penBoundary == null)
                    {
                        AnimalPenBoundary[] allPens = FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None);
                        foreach (var pen in allPens)
                        {
                            if (pen != null && pen.penType == "cow_barn")
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
                    transform.position = penBoundary.ClampPosition(transform.position, 0.70f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CowController] Start warning: " + e.Message);
            }
        }

        void FixedUpdate()
        {
            if (anim == null)
            {
                anim = GetComponent<Animation>() ?? GetComponentInChildren<Animation>();
                if (anim == null) return;
            }

            switch (state)
            {
                case "waiting":
                    PlayAnimSafe("idle2");
                    break;
                case "moving":
                    Moving();
                    break;
                case "wander":
                    Wander();
                    break;
                case "grazing":
                    PlayAnimSafe("idle3");
                    break;
                case "idle":
                    if (!inMart)
                    {
                        if (!idleRunning)
                        {
                            StartCoroutine(Idle(Random.Range(2.8f, 6.0f)));
                        }
                    }
                    break;
                case "lookingAtPlayer":
                    if (movement != null && movement.currentFocus != this.gameObject)
                    {
                        state = "wander";
                    }
                    PlayAnimSafe("idle3");
                    break;
                case "following":
                    if (playerGO != null && inMart)
                    {
                        Follow(playerGO.transform.position);
                    }
                    else
                    {
                        state = "wander";
                    }
                    break;
                case "":
                case null:
                    if (!inMart)
                    {
                        state = "idle";
                    }
                    break;
                default:
                    Wander();
                    break;
            }

            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

            if (penBoundary != null && !inMart)
            {
                transform.position = penBoundary.ClampPosition(transform.position, 0.70f);
            }
        }

        private void PlayAnimSafe(string clipName)
        {
            if (anim == null) return;
            if (anim.GetClip(clipName) != null)
            {
                if (!anim.IsPlaying(clipName))
                {
                    anim.CrossFade(clipName, 0.25f);
                }
            }
            else if (anim.clip != null && !anim.isPlaying)
            {
                anim.Play();
            }
        }

        public void StopMoving()
        {
            idleRunning = false;
            state = "idle";
            PlayAnimSafe("idle2");
        }

        void OnDestroy()
        {
            if (penBoundary != null)
            {
                penBoundary.UnregisterAnimal(gameObject);
            }
        }

        public void InitUIControl()
        {
            if (!GameController.GetSceneName().Equals("Farm"))
                return;

            GameObject uiGo = GameObject.Find("UserInterface");
            if (uiGo != null) userInterface = uiGo.GetComponent<FarmUI>();

            GameObject jsGo = GameObject.Find("VCAnalogJoystick");
            if (jsGo != null) joyStick = jsGo.GetComponent<VCAnalogJoystickBase>();

            GameObject camGo = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGo != null) cameraControl = camGo.GetComponent<CameraController>();

            playerGO = GameObject.Find("Player") ?? GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) movement = playerGO.GetComponent<Movement>();
        }

        public void Wait()
        {
            state = "waiting";
        }

        public void CheckInMart()
        {
            if (cow != null && !string.IsNullOrEmpty(cow.currScene) && cow.currScene.Equals("Mart"))
                inMart = true;
            else
                inMart = false;
        }

        public void Wander()
        {
            PlayAnimSafe("walk");

            int playSound = Random.Range(1, 6);
            if (playSound == 4 && cowSound != null && GetComponent<AudioSource>() != null)
            {
                StartCoroutine(CowMoo(Random.Range(24, 60)));
            }

            if (penBoundary != null)
            {
                finalDest = penBoundary.GetRandomPositionInside(0.85f);
                targetDest = finalDest;
            }
            else
            {
                finalDest = new Vector3(transform.position.x + Random.Range(-3.0f, 3.0f), transform.position.y, transform.position.z + Random.Range(-3.0f, 3.0f));
                if (Terrain.activeTerrain != null)
                {
                    finalDest.y = Terrain.activeTerrain.SampleHeight(finalDest) + Terrain.activeTerrain.transform.position.y;
                }
                targetDest = finalDest;
            }

            moveTimer = Random.Range(4.0f, 7.5f);
            state = "moving";
        }

        public void MoveTo(Vector3 Destination)
        {
            finalDest = Destination;
            targetDest = Destination;
            moveTimer = 10f;
            state = "moving";
        }

        private Vector3 FindPath(Vector3 position, Vector3 direction)
        {
            int angle = (int)(Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg);

            RaycastHit hit;
            Collider rBL;
            Collider rBR;

            Physics.Raycast(position, direction, out hit);
            float lastDistLeft = hit.distance;
            float lastDistRight = hit.distance;

            Vector3 closestDirection = direction;
            float smalestDist = Vector3.Distance(hit.point, finalDest);

            Vector3 leftSide = transform.position;
            Vector3 rightSide = transform.position;

            leftSide.z += width / 2;
            rightSide.z -= width / 2;

            rBL = hit.collider;
            rBR = hit.collider;

            direction.y = 0;

            for (int i = 0; i < 180; i++)
            {
                direction.x = Mathf.Cos((angle + i) * Mathf.Deg2Rad);
                direction.z = Mathf.Sin((angle + i) * Mathf.Deg2Rad);

                if (oldAngle == Vector3.zero || Vector3.Distance(oldAngle, direction) > .8f)
                {
                    Physics.Raycast(rightSide, direction, out hit);

                    if (hit.collider != rBL)
                    {
                        if (hit.distance > lastDistLeft + width)
                        {
                            direction.x = Mathf.Cos((angle + i + 5) * Mathf.Deg2Rad);
                            direction.z = Mathf.Sin((angle + i + 5) * Mathf.Deg2Rad);
                            lastDistLeft += width * 2;

                            oldAngle = -direction;
                            return direction * lastDistLeft;
                        }
                        rBL = hit.collider;
                    }

                    lastDistLeft = hit.distance;
                }

                if (Vector3.Distance(hit.point, finalDest) < smalestDist)
                {
                    smalestDist = Vector3.Distance(hit.point, finalDest);
                    closestDirection = direction;
                }

                direction.x = Mathf.Cos((angle - i) * Mathf.Deg2Rad);
                direction.y = 0;
                direction.z = Mathf.Sin((angle - i) * Mathf.Deg2Rad);

                if (oldAngle == Vector3.zero || Vector3.Distance(oldAngle, direction) > .8f)
                {
                    Physics.Raycast(leftSide, direction, out hit);

                    if (hit.collider != rBR)
                    {
                        if (hit.distance > lastDistRight + width)
                        {
                            direction.x = Mathf.Cos((angle - i - 5) * Mathf.Deg2Rad);
                            direction.z = Mathf.Sin((angle - i - 5) * Mathf.Deg2Rad);
                            oldAngle = -direction;
                            lastDistRight += width * 2;
                            return direction * lastDistRight;
                        }
                        rBR = hit.collider;
                    }

                    lastDistRight = hit.distance;
                }

                if (Vector3.Distance(hit.point, finalDest) < smalestDist)
                {
                    smalestDist = Vector3.Distance(hit.point, finalDest);
                    closestDirection = direction;
                }
            }
            return closestDirection * (smalestDist - 2);
        }

        public void Moving()
        {
            if (!inMart)
            {
                idleRunning = false;
                moveTimer -= Time.fixedDeltaTime;

                Vector3 flatDiff = targetDest - transform.position;
                flatDiff.y = 0f;
                float flatDist = flatDiff.magnitude;

                if (flatDist < 0.45f || moveTimer <= 0f || targetDest == Vector3.zero)
                {
                    targetDest = finalDest;
                    state = "idle";
                    return;
                }

                Vector3 direction = flatDiff.normalized;

                // Smooth fence margin avoidance so cows never walk into fences or line up along rails
                if (penBoundary != null && !penBoundary.IsInside(transform.position + direction * 0.85f, 0.70f))
                {
                    Vector3 toCenter = penBoundary.WorldBounds.center - transform.position;
                    toCenter.y = 0f;
                    if (toCenter.sqrMagnitude > 0.01f)
                    {
                        direction = Vector3.Slerp(direction, toCenter.normalized, 0.65f).normalized;
                        targetDest = penBoundary.GetRandomPositionInside(0.85f);
                    }
                }

                PlayAnimSafe("walk");
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
                }

                float turnSlowdown = Mathf.Clamp01(1f - Vector3.Angle(transform.forward, direction) / 115f);
                float walkSpeed = Mathf.Clamp(speed, 0.8f, 1.5f) * Mathf.Max(0.45f, turnSlowdown);

                Vector3 before = transform.position;
                Vector3 after = before + transform.forward * (walkSpeed * Time.fixedDeltaTime);
                if (penBoundary != null)
                {
                    after = penBoundary.ClampPosition(after, 0.70f);
                }
                transform.position = after;

                // Synchronize walk clip playback speed to actual ground speed (zero foot sliding)
                float actualSpd = (after - before).magnitude / Mathf.Max(0.0001f, Time.fixedDeltaTime);
                if (anim != null && anim.GetClip("walk") != null && anim["walk"] != null)
                {
                    anim["walk"].speed = Mathf.Clamp(actualSpd / 1.20f, 0.45f, 1.35f);
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, finalDest) < 2)
                {
                    Wait();
                    return;
                }

                if (Vector3.Distance(transform.position, targetDest) < 2)
                {
                    targetDest = finalDest;
                }

                if (Physics.Raycast(transform.position, (targetDest - transform.position).normalized, Vector3.Distance(transform.position, targetDest)) || Physics.Raycast(transform.position, transform.forward, 2))
                {
                    targetDest = transform.position + FindPath(transform.position, (finalDest - transform.position).normalized);
                }

                if (targetDest != Vector3.zero)
                {
                    PlayAnimSafe("walk");
                    Vector3 direction = (targetDest - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(direction);

                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
                    transform.position += transform.forward * speed * Time.deltaTime;
                }
            }
        }

        public void Follow(Vector3 goToPosition)
        {
            PlayAnimSafe("walk");
            Vector3 targetPosition = goToPosition;

            targetPosition += playerGO.transform.forward * 7;
            targetPosition.y = transform.position.y;
            transform.LookAt(targetPosition);
            transform.position += transform.forward * 4 * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetPosition) < 1)
            {
                transform.LookAt(playerGO.transform);
                state = "lookingAtPlayer";
                StartCoroutine(WaitFor(20f, 1));
            }
        }

        public Vector3 ReturnPosition()
        {
            return this.gameObject.transform.position;
        }

        void OnMouseDown()
        {
            if (FarmEditModeController.IsEditModeActive ||
                (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                return;

            if (!inMart)
            {
                AnimalCareController care = GetComponent<AnimalCareController>();
                if (care != null)
                {
                    care.OpenAnimalCareUI();
                    return;
                }
            }

            if (inMart || GameController.Instance().cowSelected || GameController.Instance().IsMenuOpen())
                return;

            if (joyStick != null) joyStick.gameObject.SetActive(false);
            state = "following";

            if (movement != null) movement.LookAt(this.gameObject);

            if (playerGO != null && cameraControl != null)
            {
                Vector3 position = playerGO.transform.position - transform.position;
                Vector3 target = position;
                position += playerGO.transform.position;
                target = playerGO.transform.position - target;
                target.y = transform.position.y;
                position.y = transform.position.y + 6;
                cameraControl.MoveToLookAt(position, target);
            }

            if (userInterface != null)
            {
                userInterface.cow = cow;
                userInterface.cowGameObject = this.gameObject;
            }

            GameController.Instance().farmCowUI = true;
            GameController.Instance().cowSelected = true;
        }

        private IEnumerator WaitFor(float seconds, int option)
        {
            yield return new WaitForSeconds(seconds);

            switch (option)
            {
                case 0:
                    state = "idle";
                    break;
                case 1:
                    state = "wander";
                    break;
                default:
                    state = "wander";
                    break;
            }
        }

        private IEnumerator Idle(float seconds)
        {
            idleRunning = true;
            // Alternate naturally between standing idle ("idle1"/"idle2") and grazing ("idle3")
            bool shouldGraze = Random.value < 0.45f;
            string chosenIdle = shouldGraze ? "idle3" : (Random.value < 0.5f ? "idle1" : "idle2");
            if (anim != null && anim.GetClip(chosenIdle) == null)
            {
                chosenIdle = "idle2";
            }
            PlayAnimSafe(chosenIdle);

            yield return new WaitForSeconds(seconds);

            if (state == "idle" || state == "grazing" || state == "idle1" || state == "idle2" || state == "idle3")
            {
                state = "wander";
            }
            idleRunning = false;
        }

        private IEnumerator CowMoo(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            AudioSource src = GetComponent<AudioSource>();
            if (src != null && cowSound != null)
            {
                src.PlayOneShot(cowSound, Random.Range(0.3f, 0.6f));
            }
        }
    }
}