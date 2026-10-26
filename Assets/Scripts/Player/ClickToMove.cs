using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IrishFarmSim
{
    /// <summary>
    /// Handles click-to-move input via mouse click or touch on the ground/terrain.
    /// Cancels target upon reaching destination or when manual input (WASD/Joystick) is detected.
    /// </summary>
    public class ClickToMove : MonoBehaviour
    {
        [Header("Settings")]
        public float stoppingDistance = 0.6f;
        public LayerMask groundLayerMask = ~0; // default all layers, filtered by tag/component

        [Header("Status")]
        [SerializeField] private bool hasTarget = false;
        [SerializeField] private Vector3 targetPosition;

        private CharacterController characterController;
        private LineRenderer markerRenderer;

        public bool HasTarget => hasTarget;
        public Vector3 TargetPosition => targetPosition;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            SetupClickMarker();
        }

        void Update()
        {
            HandleMouseClick();
            CheckArrival();
        }

        private void HandleMouseClick()
        {
            // Left click or single touch
            if (!Input.GetMouseButtonDown(0)) return;

            // Do not process click if clicking over UI (mouse or touch)
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject()) return;
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            }
            if (SuppliesUI.Instance != null && SuppliesUI.Instance.IsOpen) return;
            if (CropSelectionUI.Instance != null && CropSelectionUI.Instance.IsOpen) return;
            if (CropPlotUI.Instance != null && CropPlotUI.Instance.IsOpen) return;
            if (AnimalUI.Instance != null && AnimalUI.Instance.IsOpen) return;
            if (QuestUI.Instance != null && QuestUI.Instance.IsOpen) return;
            if (AchievementUI.Instance != null && AchievementUI.Instance.IsOpen) return;
            if (ShopUI.Instance != null && ShopUI.Instance.panelRoot != null && ShopUI.Instance.panelRoot.activeSelf) return;
            if (BuildingUI.Instance != null && BuildingUI.Instance.panelRoot != null && BuildingUI.Instance.panelRoot.activeSelf) return;
            if (MachineUI.Instance != null && MachineUI.Instance.panelRoot != null && MachineUI.Instance.panelRoot.activeSelf) return;
            if (StorageUI.Instance != null && StorageUI.Instance.IsOpen) return;
            if (BuildingPlacementController.Instance != null && BuildingPlacementController.Instance.IsPlacing) return;
            if (FarmEditModeController.Instance != null && FarmEditModeController.Instance.IsEditMode) return;
            if (Time.unscaledTime < CropSelectionUI.IgnoreClicksUntilTime) return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, groundLayerMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Ignore player's own character controller or colliders
                if (hit.collider.transform.root == transform.root) continue;

                // If clicked on an interactive animal, plot, building, machine, or pet, don't move to it
                if (hit.collider.GetComponentInParent<CowController>() != null ||
                    hit.collider.GetComponentInParent<CropPlot>() != null ||
                    hit.collider.GetComponentInParent<AnimalCareController>() != null ||
                    hit.collider.GetComponentInParent<FarmBuilding>() != null ||
                    hit.collider.GetComponentInParent<ProductionMachine>() != null ||
                    hit.collider.GetComponentInParent<PetWanderer>() != null)
                {
                    CancelMove();
                    return;
                }

                // Check if hit terrain or ground collider safely without throwing if tag is missing
                bool isGround = hit.collider is TerrainCollider ||
                                (hit.collider.gameObject != null && hit.collider.gameObject.name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                SafeCheckTerrainTag(hit.collider.gameObject);

                if (isGround)
                {
                    SetTarget(hit.point);
                    return;
                }
            }
        }

        private static bool SafeCheckTerrainTag(GameObject go)
        {
            if (go == null) return false;
            try { return go.CompareTag("Terrain"); }
            catch { return false; }
        }

        private Vector3 lastProgressPosition;
        private float lastProgressTime;

        public void SetTarget(Vector3 destination)
        {
            targetPosition = destination;
            hasTarget = true;
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
            ShowMarker(destination);
        }

        public void CancelMove()
        {
            hasTarget = false;
            HideMarker();
        }

        private void CheckArrival()
        {
            if (!hasTarget) return;

            Vector3 currentPos = transform.position;
            currentPos.y = targetPosition.y;

            float dist = Vector3.Distance(currentPos, targetPosition);
            if (dist <= stoppingDistance)
            {
                CancelMove();
                return;
            }

            Vector2 flatCurr = new Vector2(transform.position.x, transform.position.z);
            Vector2 flatLast = new Vector2(lastProgressPosition.x, lastProgressPosition.z);
            if (Vector2.Distance(flatCurr, flatLast) > 0.12f)
            {
                lastProgressPosition = transform.position;
                lastProgressTime = Time.time;
            }
            else if (Time.time - lastProgressTime > 0.4f)
            {
                CancelMove();
            }
        }

        public Vector3 GetMoveDirection(Vector3 currentPosition)
        {
            if (!hasTarget) return Vector3.zero;

            Vector3 dir = targetPosition - currentPosition;
            dir.y = 0;

            if (dir.magnitude <= stoppingDistance)
            {
                CancelMove();
                return Vector3.zero;
            }

            return dir.normalized;
        }

        private void SetupClickMarker()
        {
            GameObject markerGo = new GameObject("ClickMoveMarker");
            markerGo.transform.SetParent(transform);
            markerRenderer = markerGo.AddComponent<LineRenderer>();
            markerRenderer.useWorldSpace = true;
            markerRenderer.loop = true;
            markerRenderer.positionCount = 16;
            markerRenderer.startWidth = 0.08f;
            markerRenderer.endWidth = 0.08f;

            Shader unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                markerRenderer.material = new Material(unlit);
            }
            markerRenderer.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
            markerRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.8f);
            markerRenderer.enabled = false;
        }

        private void ShowMarker(Vector3 pos)
        {
            if (markerRenderer == null) return;
            markerRenderer.enabled = true;
            float radius = 0.5f;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16;
                Vector3 p = new Vector3(pos.x + Mathf.Cos(angle) * radius, pos.y + 0.1f, pos.z + Mathf.Sin(angle) * radius);
                markerRenderer.SetPosition(i, p);
            }
        }

        private void HideMarker()
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = false;
            }
        }
    }
}
