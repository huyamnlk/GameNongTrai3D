using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
    /// <summary>
    /// Smooth third-person camera controller with player following,
    /// right-mouse orbit rotation, and terrain collision prevention.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target & Follow")]
        public GameObject target;
        public bool followTarget = true;
        public bool watchTarget = false;
        public float speed = 10f;
        public float followSmoothness = 10f;
        public float rotationSmoothness = 10f;
        public float lookOffsetHeight = 1.5f;

        [Header("Terrain Avoidance")]
        public float minHeightAboveTerrain = 1.5f;

        [Header("Orbit Controls")]
        public float orbitSpeed = 4f;

        [Header("Offset")]
        public Vector3 difVec;

        private Vector3 startingAngle;
        private Vector3 endingAngle;
        private Vector3 defaultPosition;
        private Vector3 startPositon;
        private Vector3 endPosition;
        private float startTime;
        private float journeyLength;

        public bool transitioning = false;
        private float playerY = 0f;

        void Start()
        {
            try
            {
                if (target == null)
                {
                    target = GameObject.Find("Player");
                }
                if (target == null)
                {
                    GameObject pTag = GameObject.FindGameObjectWithTag("Player");
                    if (pTag != null) target = pTag;
                }

                if (target != null)
                {
                    difVec = transform.position - target.transform.position;
                    // If camera was at origin or overlapping, provide default third-person offset
                    if (difVec.sqrMagnitude < 1f)
                    {
                        difVec = new Vector3(0f, 6f, -9f);
                    }
                    StartCoroutine(getAveragePosY());
                }

                defaultPosition = transform.position;
                followTarget = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CameraController] Start warning: " + e.Message);
            }
        }

        public void MoveToLookAt(Vector3 position, Vector3 targetPos)
        {
            if (FarmEditModeController.IsEditModeActive) return;

            startTime = Time.time;
            journeyLength = Vector3.Distance(transform.position, position);
            if (journeyLength < 0.01f) journeyLength = 0.01f;

            startPositon = transform.position;
            startingAngle = transform.forward;

            endPosition = position;
            endingAngle = (targetPos - position).normalized;

            transitioning = true;
            watchTarget = false;
            followTarget = false;
        }

        public void LookAt(Vector3 targetPos)
        {
            if (FarmEditModeController.IsEditModeActive) return;

            startTime = Time.time;
            journeyLength = Vector3.Distance(targetPos - transform.position, transform.forward) / 4f;
            if (journeyLength < 0.01f) journeyLength = 0.01f;

            startPositon = transform.position;
            startingAngle = transform.forward;
            endingAngle = (targetPos - transform.position).normalized;

            transitioning = true;
            watchTarget = false;
            followTarget = false;
        }

        public void WatchTarget(GameObject newTarget)
        {
            if (FarmEditModeController.IsEditModeActive) return;

            this.target = newTarget;
            LookAt(newTarget.transform.position);
            watchTarget = true;
        }

        public void WatchPlayer()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            target = GameObject.Find("Player");
            if (target != null)
            {
                Vector3 playerLoc = target.transform.position;
                playerLoc.y = playerY;
                MoveToLookAt(defaultPosition, playerLoc);
                watchTarget = true;
            }
        }

        public void FollowPlayer()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            target = GameObject.Find("Player");
            if (target != null)
            {
                Vector3 playerLoc = target.transform.position;
                playerLoc.y = playerY;
                MoveToLookAt(playerLoc + difVec, playerLoc);
                followTarget = true;
            }
        }

        void LateUpdate()
        {
            if (FarmEditModeController.IsEditModeActive)
            {
                transitioning = false;
                watchTarget = false;
                return; // Strictly preserve exact camera position, angle, and FOV in Farm Edit Mode
            }

            if (target == null)
            {
                target = GameObject.Find("Player");
                if (target == null) return;
            }

            if (transitioning)
            {
                float distCovered = (Time.time - startTime) * speed;
                float fracComplete = distCovered / journeyLength;

                if (fracComplete <= 1f)
                {
                    transform.position = Vector3.Lerp(startPositon, endPosition, fracComplete);
                    transform.forward = Vector3.Slerp(startingAngle, endingAngle, fracComplete);
                }
                else
                {
                    transform.position = Vector3.Lerp(startPositon, endPosition, 1f);
                    transform.forward = Vector3.Slerp(startingAngle, endingAngle, 1f);
                    transitioning = false;
                }
            }
            else if (watchTarget)
            {
                transform.LookAt(target.transform.position + Vector3.up * lookOffsetHeight);
            }
            else if (followTarget)
            {
                // Right Mouse Button orbit rotation
                if (Input.GetMouseButton(1))
                {
                    float mouseX = Input.GetAxis("Mouse X") * orbitSpeed;
                    difVec = Quaternion.AngleAxis(mouseX, Vector3.up) * difVec;
                }

                Vector3 desiredPos = target.transform.position + difVec;

                // Terrain height clamping to prevent camera from dipping below terrain
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    float terrainY = terrain.SampleHeight(desiredPos) + terrain.transform.position.y;
                    if (desiredPos.y < terrainY + minHeightAboveTerrain)
                    {
                        desiredPos.y = terrainY + minHeightAboveTerrain;
                    }
                }

                // Smooth following
                transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSmoothness);

                Vector3 lookTarget = target.transform.position + Vector3.up * lookOffsetHeight;
                Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSmoothness);
            }
        }

        private IEnumerator getAveragePosY()
        {
            for (int i = 0; i < 10; i++)
            {
                if (target != null)
                {
                    playerY += target.transform.position.y;
                }
                yield return new WaitForSeconds(0.01f);
            }
            playerY = playerY / 10f;
        }
    }
}