using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IrishFarmSim
{
    public enum EditActionType
    {
        Move,
        Rotate,
        Delete
    }

    public class EditAction
    {
        public EditActionType type;
        public MovableFarmObject target;
        public Vector3 previousPosition;
        public Quaternion previousRotation;
        public Vector3 newPosition;
        public Quaternion newRotation;
        public bool wasDeleted;
    }

    /// <summary>
    /// Master controller for Farm Edit Mode (Chế độ chỉnh sửa bố cục nông trại).
    /// Allows selecting, dragging, rotating, deleting, and undoing buildings, pens, plots, machines,
    /// straw bales, lumber piles, and ladders.
    /// Supports category filtering ("RƠM", "GỖ", "CẦU THANG"), full undo history, and persistent save/load.
    /// </summary>
    public class FarmEditModeController : MonoBehaviour
    {
        public static FarmEditModeController Instance { get; private set; }

        public static bool IsEditModeActive => Instance != null && Instance.IsEditMode;

        [Header("Edit Mode State")]
        public bool isEditMode = false;
        public bool IsEditMode => isEditMode;

        public MovableFarmObject selectedObject = null;
        public bool isDragging = false;

        private Vector3 dragOffset = Vector3.zero;
        private Camera targetCamera;
        private float lastDragPlaneY = 0f;

        // Hover tracking
        private MovableFarmObject hoveredObject = null;

        // Undo history
        private readonly Stack<EditAction> undoStack = new Stack<EditAction>();

        // Category cycling index
        private readonly Dictionary<string, int> categoryCycleIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Camera preservation
        private Vector3 savedCameraPos;
        private Quaternion savedCameraRot;
        private float savedCameraFOV = 60f;
        private bool cameraStateRecorded = false;

        [Header("UI References")]
        private GameObject editCanvasGo;
        private GameObject topBannerGo;
        private Text textBannerTitle;
        private Text textBannerInstruction;
        private GameObject categoryBarGo;
        private GameObject selectedPanelGo;
        private Text textSelectedName;
        private Button btnDeselect;
        private Button btnRotateLeft;
        private Button btnRotateRight;
        private Button btnConfirm;
        private Button btnCancelMove;
        private Button btnDelete;
        private Button btnUndo;
        private Button btnTopUndo;
        private Button btnDone;

        // Visual feedback indicator during drag
        private GameObject dragGhostIndicator;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static FarmEditModeController EnsureInstance()
        {
            if (Instance != null) return Instance;

            FarmEditModeController found = FindAnyObjectByType<FarmEditModeController>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("FarmEditModeController");
            Instance = go.AddComponent<FarmEditModeController>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            // Purge any redundant top-left edit button canvas
            GameObject strayHud = GameObject.Find("FarmEditModeHUDCanvas");
            if (strayHud != null) DestroyImmediate(strayHud);
            GameObject strayBtn = GameObject.Find("Btn_🛠 CHỈNH SỬA (B)");
            if (strayBtn != null) DestroyImmediate(strayBtn);

            CreateEditModeUI();
        }

        void Start()
        {
            targetCamera = Camera.main;
        }

        public void EnterEditMode()
        {
            if (isEditMode) return;

            // 1. Freeze and preserve Camera state completely
            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                savedCameraPos = targetCamera.transform.position;
                savedCameraRot = targetCamera.transform.rotation;
                savedCameraFOV = targetCamera.fieldOfView;
                cameraStateRecorded = true;
            }

            CameraController camControl = FindAnyObjectByType<CameraController>();
            if (camControl != null)
            {
                camControl.transitioning = false;
                camControl.watchTarget = false;
            }

            // 2. Close any open gameplay windows
            if (ShopUI.Instance != null && ShopUI.Instance.panelRoot != null) ShopUI.Instance.panelRoot.SetActive(false);
            if (StorageUI.Instance != null) StorageUI.Instance.CloseStorage();
            if (AnimalUI.Instance != null) AnimalUI.Instance.Close();
            if (CropSelectionUI.Instance != null) CropSelectionUI.Instance.Close();
            if (BuildingUI.Instance != null && BuildingUI.Instance.panelRoot != null) BuildingUI.Instance.panelRoot.SetActive(false);
            if (MachineUI.Instance != null && MachineUI.Instance.panelRoot != null) MachineUI.Instance.panelRoot.SetActive(false);

            // 3. Halt Player movement & interactions
            ClickToMove ctm = FindAnyObjectByType<ClickToMove>();
            if (ctm != null) ctm.CancelMove();

            FarmInteractionController fic = FindAnyObjectByType<FarmInteractionController>();
            if (fic != null) fic.enabled = false;

            // 4. Disable mobile virtual joystick canvas to free screen for dragging
            GameObject joyCanvas = GameObject.Find("MobileControlsCanvas");
            if (joyCanvas != null) joyCanvas.SetActive(false);
            if (MobileJoystick.Instance != null) MobileJoystick.Instance.gameObject.SetActive(false);

            // 5. Scan and register all map objects (Straw, Lumber, Ladder, Fences, etc.)
            MovableFarmObject.ScanAndRegisterAllMapObjects();

            isEditMode = true;
            undoStack.Clear();

            if (editCanvasGo != null) editCanvasGo.SetActive(true);

            DeselectCurrent();
            UpdateUI();

            CropSelectionUI.ShowToast("Đã bật chế độ Chỉnh Sửa Nông Trại! Rê chuột để xem trước, chạm/kéo để di chuyển hoặc xóa.");
            Debug.Log("[FarmEditModeController] Entered Farm Edit Mode.");
        }

        public void ExitEditMode()
        {
            if (!isEditMode) return;

            // If an object is currently being dragged, finalize it
            if (isDragging && selectedObject != null)
            {
                FinishDrag();
            }

            if (selectedObject != null)
            {
                selectedObject.SetSelected(false);
                selectedObject = null;
            }

            if (hoveredObject != null)
            {
                hoveredObject.SetHovered(false);
                hoveredObject = null;
            }

            isDragging = false;
            ClearDragIndicator();

            // Purge any lingering selection/hover indicators
            MovableFarmObject[] allMovables = FindObjectsByType<MovableFarmObject>(FindObjectsInactive.Include);
            foreach (var m in allMovables)
            {
                if (m != null)
                {
                    m.ClearSelectionIndicator();
                    m.ClearHoverIndicator();
                }
            }

            // Ensure all moved fences and walls are grounded
            FenceGroundAligner.AlignAllInScene();

            isEditMode = false;

            if (editCanvasGo != null) editCanvasGo.SetActive(false);

            // 1. Restore camera
            if (cameraStateRecorded && Camera.main != null)
            {
                Camera.main.transform.position = savedCameraPos;
                Camera.main.transform.rotation = savedCameraRot;
                Camera.main.fieldOfView = savedCameraFOV;
            }

            CameraController camControl = FindAnyObjectByType<CameraController>();
            if (camControl != null)
            {
                camControl.followTarget = true;
                camControl.watchTarget = false;
                camControl.transitioning = false;
            }

            // 2. Restore Player movement & interactions
            FarmInteractionController fic = FindAnyObjectByType<FarmInteractionController>();
            if (fic != null) fic.enabled = true;

            // 3. Restore mobile virtual joystick
            GameObject joyCanvas = GameObject.Find("MobileControlsCanvas");
            if (joyCanvas != null) joyCanvas.SetActive(true);
            if (MobileJoystick.Instance != null) MobileJoystick.Instance.gameObject.SetActive(true);

            // 4. Save layout changes to disk
            SaveLoadSystem.SaveGame();

            CropSelectionUI.ShowToast("Đã hoàn tất chỉnh sửa và lưu bố cục nông trại!");
            Debug.Log("[FarmEditModeController] Exited Farm Edit Mode.");
        }

        public void ToggleEditMode()
        {
            if (isEditMode) ExitEditMode();
            else EnterEditMode();
        }

        public void SetEditMode(bool enable)
        {
            if (enable) EnterEditMode();
            else ExitEditMode();
        }

        public void SelectMovable(MovableFarmObject movable)
        {
            if (selectedObject != null && selectedObject != movable)
            {
                selectedObject.SetSelected(false);
            }

            if (hoveredObject != null)
            {
                hoveredObject.SetHovered(false);
                hoveredObject = null;
            }

            selectedObject = movable;
            if (selectedObject != null)
            {
                selectedObject.RecordCurrentTransform();
                selectedObject.SetSelected(true);
            }

            UpdateUI();
        }

        public void DeselectCurrent()
        {
            if (selectedObject != null)
            {
                selectedObject.SetSelected(false);
                selectedObject = null;
            }
            isDragging = false;
            ClearDragIndicator();
            UpdateUI();
        }

        public void RotateCurrent(float angleDelta)
        {
            if (selectedObject == null) return;

            Quaternion oldRot = selectedObject.transform.rotation;
            Quaternion newRot = oldRot * Quaternion.Euler(0f, angleDelta, 0f);

            // Validate rotation
            if (FarmObjectPlacementValidator.ValidatePlacement(selectedObject.gameObject, selectedObject.transform.position, newRot, selectedObject.footprintSize, out string reason))
            {
                // Push to undo stack
                undoStack.Push(new EditAction
                {
                    type = EditActionType.Rotate,
                    target = selectedObject,
                    previousPosition = selectedObject.transform.position,
                    previousRotation = oldRot,
                    newPosition = selectedObject.transform.position,
                    newRotation = newRot,
                    wasDeleted = false
                });

                selectedObject.transform.rotation = newRot;
                GroundSnapUtility.SnapObjectToGround(selectedObject.gameObject);
                selectedObject.RecordCurrentTransform();

                // If pen, notify animals
                AnimalPenBoundary pen = selectedObject.GetComponentInChildren<AnimalPenBoundary>(true);
                if (pen != null)
                {
                    pen.NotifyPenMoved(Vector3.zero, newRot * Quaternion.Inverse(oldRot));
                }

                CropSelectionUI.ShowToast($"Đã xoay {selectedObject.displayName}!");
                SaveLoadSystem.SaveGame();
            }
            else
            {
                CropSelectionUI.ShowToast("Không thể xoay: " + reason);
            }
        }

        public void ConfirmCurrentPlacement()
        {
            if (selectedObject == null) return;

            Vector3 finalPos = selectedObject.transform.position;
            string selName = selectedObject.name.ToLower();
            if (selName.Contains("tractor") || selName.Contains("vehicle"))
            {
                GroundSnapUtility.SnapTractorToGroundExact(selectedObject.gameObject);
            }
            else
            {
                GroundSnapUtility.SnapToGroundExact(selectedObject.gameObject);
            }
            finalPos = selectedObject.transform.position;

            if (FarmObjectPlacementValidator.ValidatePlacement(selectedObject.gameObject, finalPos, selectedObject.transform.rotation, selectedObject.footprintSize, out string failureReason))
            {
                if (Vector3.Distance(finalPos, selectedObject.originalPosition) > 0.05f || Quaternion.Angle(selectedObject.transform.rotation, selectedObject.originalRotation) > 1f)
                {
                    undoStack.Push(new EditAction
                    {
                        type = EditActionType.Move,
                        target = selectedObject,
                        previousPosition = selectedObject.originalPosition,
                        previousRotation = selectedObject.originalRotation,
                        newPosition = finalPos,
                        newRotation = selectedObject.transform.rotation,
                        wasDeleted = false
                    });
                }

                selectedObject.RecordCurrentTransform();
                AnimalPenBoundary pen = selectedObject.GetComponentInChildren<AnimalPenBoundary>(true);
                if (pen != null)
                {
                    pen.NotifyPenMoved(Vector3.zero, Quaternion.identity);
                }
                CropSelectionUI.ShowToast($"Đã xác nhận vị trí mới cho {selectedObject.displayName}!");
                SaveLoadSystem.SaveGame();
                DeselectCurrent();
            }
            else
            {
                CropSelectionUI.ShowToast("Vị trí không hợp lệ (" + failureReason + ")!");
            }
        }

        public void CancelCurrentMove()
        {
            if (selectedObject != null)
            {
                selectedObject.RevertToOriginal();
                CropSelectionUI.ShowToast($"Đã hoàn tác vị trí của {selectedObject.displayName}.");
            }
            DeselectCurrent();
        }

        public void DeleteCurrent()
        {
            if (selectedObject == null) return;

            MovableFarmObject toDelete = selectedObject;
            string objName = toDelete.displayName;

            // Push delete action to undo stack
            undoStack.Push(new EditAction
            {
                type = EditActionType.Delete,
                target = toDelete,
                previousPosition = toDelete.transform.position,
                previousRotation = toDelete.transform.rotation,
                newPosition = toDelete.transform.position,
                newRotation = toDelete.transform.rotation,
                wasDeleted = false
            });

            toDelete.isDeleted = true;
            toDelete.ClearSelectionIndicator();
            toDelete.ClearHoverIndicator();
            toDelete.gameObject.SetActive(false);

            DeselectCurrent();
            SaveLoadSystem.SaveGame();

            CropSelectionUI.ShowToast($"Đã xóa {objName} khỏi nông trại!");
            Debug.Log($"[FarmEditModeController] Deleted {objName}.");
        }

        public void UndoLastAction()
        {
            if (undoStack.Count == 0)
            {
                CropSelectionUI.ShowToast("Không có thao tác nào để hoàn tác!");
                return;
            }

            EditAction action = undoStack.Pop();
            if (action.target == null)
            {
                CropSelectionUI.ShowToast("Đối tượng không còn tồn tại để hoàn tác.");
                return;
            }

            if (action.type == EditActionType.Delete)
            {
                action.target.gameObject.SetActive(true);
                action.target.isDeleted = false;
                action.target.transform.position = action.previousPosition;
                action.target.transform.rotation = action.previousRotation;
                GroundSnapUtility.SnapToGroundExact(action.target.gameObject);
                action.target.EnsureCollider();
                action.target.RecordCurrentTransform();

                SelectMovable(action.target);
                SaveLoadSystem.SaveGame();
                CropSelectionUI.ShowToast($"Đã hoàn tác: Khôi phục {action.target.displayName}!");
            }
            else if (action.type == EditActionType.Move || action.type == EditActionType.Rotate)
            {
                action.target.transform.position = action.previousPosition;
                action.target.transform.rotation = action.previousRotation;
                GroundSnapUtility.SnapToGroundExact(action.target.gameObject);
                action.target.RecordCurrentTransform();

                SelectMovable(action.target);
                SaveLoadSystem.SaveGame();
                CropSelectionUI.ShowToast($"Đã hoàn tác vị trí của {action.target.displayName}!");
            }
        }

        /// <summary>
        /// Cycles through objects of a specific category ("RƠM", "GỖ", "CẦU THANG", "CÔNG TRÌNH", "TẤT CẢ").
        /// </summary>
        public void SelectNextInCategory(string category)
        {
            MovableFarmObject[] allMovables = FindObjectsByType<MovableFarmObject>();
            List<MovableFarmObject> matching = new List<MovableFarmObject>();

            foreach (var m in allMovables)
            {
                if (m == null || m.isDeleted || !m.gameObject.activeInHierarchy) continue;

                if (category == "TẤT CẢ" ||
                    string.Equals(m.categoryGroupName, category, StringComparison.OrdinalIgnoreCase) ||
                    (category == "MÁNG NƯỚC" && (m.objectType == MovableType.Trough || m.categoryGroupName == "MÁNG NƯỚC" || m.name.ToLower().Contains("trough") || m.name.ToLower().Contains("well") || m.name.ToLower().Contains("manger"))) ||
                    (category == "RƠM" && m.objectType == MovableType.Straw) ||
                    (category == "GỖ" && m.objectType == MovableType.Lumber) ||
                    (category == "CẦU THANG" && m.objectType == MovableType.Ladder) ||
                    (category == "TRANG TRÍ" && (m.objectType == MovableType.Decoration || m.objectType == MovableType.Fence)) ||
                    (category == "CÔNG TRÌNH" && (m.objectType == MovableType.Building || m.objectType == MovableType.Pen)))
                {
                    matching.Add(m);
                }
            }

            if (matching.Count == 0)
            {
                CropSelectionUI.ShowToast($"Không có vật thể thuộc nhóm '{category}' trên map.");
                return;
            }

            categoryCycleIndices.TryGetValue(category, out int idx);
            if (idx >= matching.Count) idx = 0;

            MovableFarmObject target = matching[idx];
            categoryCycleIndices[category] = (idx + 1) % matching.Count;

            SelectMovable(target);
            CropSelectionUI.ShowToast($"Đang chọn [{target.categoryGroupName}]: {target.displayName} ({idx + 1}/{matching.Count})");
        }

        void LateUpdate()
        {
            // Keep Camera completely frozen in Edit Mode
            if (isEditMode && cameraStateRecorded && targetCamera != null)
            {
                targetCamera.transform.position = savedCameraPos;
                targetCamera.transform.rotation = savedCameraRot;
                targetCamera.fieldOfView = savedCameraFOV;
            }
        }

        void Update()
        {
            // Hotkey B to toggle Edit Mode
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleEditMode();
                return;
            }

            if (!isEditMode) return;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            // Hotkeys inside Edit Mode
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                if (selectedObject != null)
                {
                    DeleteCurrent();
                    return;
                }
            }

            // Undo: Ctrl+Z or standalone Z
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z))
            {
                UndoLastAction();
                return;
            }

            // Rotate keys: Q (left) and E (right)
            if (Input.GetKeyDown(KeyCode.Q) && selectedObject != null)
            {
                RotateCurrent(-45f);
                return;
            }
            if (Input.GetKeyDown(KeyCode.E) && selectedObject != null)
            {
                RotateCurrent(45f);
                return;
            }

            // Confirm key: Enter
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && selectedObject != null)
            {
                ConfirmCurrentPlacement();
                return;
            }

            // Unified pointer handling
            Vector2 pointerPos = Input.mousePosition;
            bool pointerDown = Input.GetMouseButtonDown(0);
            bool pointerHeld = Input.GetMouseButton(0);
            bool pointerUp = Input.GetMouseButtonUp(0);

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                pointerPos = t.position;
                pointerDown = (t.phase == TouchPhase.Began);
                pointerHeld = (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary);
                pointerUp = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
            }

            // Hover tracking when not dragging
            if (!isDragging && !IsPointerOverUI(pointerPos))
            {
                Ray hoverRay = targetCamera.ScreenPointToRay(pointerPos);
                int layerMask = ~((1 << 2) | (1 << 5));
                RaycastHit[] hits = Physics.RaycastAll(hoverRay, 1000f, layerMask, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                MovableFarmObject hitMovable = null;
                foreach (var h in hits)
                {
                    hitMovable = MovableFarmObject.ResolveMovable(h.collider.gameObject);
                    if (hitMovable != null && !hitMovable.isDeleted) break;
                }

                if (hoveredObject != hitMovable)
                {
                    if (hoveredObject != null && hoveredObject != selectedObject)
                    {
                        hoveredObject.SetHovered(false);
                    }
                    hoveredObject = hitMovable;
                    if (hoveredObject != null && hoveredObject != selectedObject)
                    {
                        hoveredObject.SetHovered(true);
                    }
                }
            }
            else if (hoveredObject != null)
            {
                hoveredObject.SetHovered(false);
                hoveredObject = null;
            }

            // 1. Pointer Down: Select & begin drag
            if (pointerDown)
            {
                if (!IsPointerOverUI(pointerPos))
                {
                    Ray ray = targetCamera.ScreenPointToRay(pointerPos);
                    int layerMask = ~((1 << 2) | (1 << 5));
                    RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, layerMask, QueryTriggerInteraction.Collide);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    MovableFarmObject clickedMovable = null;
                    Vector3 hitPoint = Vector3.zero;

                    foreach (var h in hits)
                    {
                        clickedMovable = MovableFarmObject.ResolveMovable(h.collider.gameObject);
                        if (clickedMovable != null && !clickedMovable.isDeleted)
                        {
                            hitPoint = h.point;
                            break;
                        }
                    }

                    if (clickedMovable != null)
                    {
                        SelectMovable(clickedMovable);
                        isDragging = true;
                        dragOffset = clickedMovable.transform.position - hitPoint;
                        dragOffset.y = 0f;
                        lastDragPlaneY = clickedMovable.transform.position.y;
                        CreateDragIndicator(clickedMovable);
                    }
                    else
                    {
                        if (!isDragging)
                        {
                            DeselectCurrent();
                        }
                    }
                }
            }

            // 2. Dragging on Terrain surface
            if (isDragging && selectedObject != null)
            {
                if (pointerHeld)
                {
                    Ray ray = targetCamera.ScreenPointToRay(pointerPos);
                    int terrainMask = ~((1 << 2) | (1 << 5));

                    RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, terrainMask);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    RaycastHit? groundHit = null;
                    foreach (var h in hits)
                    {
                        if (h.transform == selectedObject.transform || h.transform.IsChildOf(selectedObject.transform))
                        {
                            continue;
                        }

                        if (h.collider != null && (h.collider.name.StartsWith("DragIndicator") || h.collider.name.StartsWith("SelectionIndicator") || h.collider.name.StartsWith("HoverIndicator")))
                        {
                            continue;
                        }

                        if (h.collider is TerrainCollider || h.collider.CompareTag("Terrain") ||
                            (h.collider.gameObject != null && h.collider.gameObject.name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0) ||
                            h.normal.y > 0.4f)
                        {
                            groundHit = h;
                            break;
                        }
                    }

                    Vector3 hitPoint;
                    if (groundHit.HasValue)
                    {
                        hitPoint = groundHit.Value.point;
                    }
                    else
                    {
                        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, lastDragPlaneY, 0f));
                        if (groundPlane.Raycast(ray, out float enter))
                        {
                            hitPoint = ray.GetPoint(enter);
                        }
                        else
                        {
                            hitPoint = selectedObject.transform.position;
                        }
                    }

                    Vector3 rawTarget = hitPoint + dragOffset;
                    rawTarget.x = Mathf.Round(rawTarget.x * 2f) / 2f;
                    rawTarget.z = Mathf.Round(rawTarget.z * 2f) / 2f;

                    if (Terrain.activeTerrain != null)
                    {
                        rawTarget.y = Terrain.activeTerrain.SampleHeight(rawTarget) + Terrain.activeTerrain.transform.position.y;
                    }
                    else
                    {
                        rawTarget.y = lastDragPlaneY;
                    }

                    Vector3 delta = rawTarget - selectedObject.transform.position;
                    selectedObject.transform.position = rawTarget;

                    bool isValid = FarmObjectPlacementValidator.ValidatePlacement(selectedObject.gameObject, rawTarget, selectedObject.transform.rotation, selectedObject.footprintSize, out _);
                    UpdateDragIndicator(isValid);

                    AnimalPenBoundary pen = selectedObject.GetComponentInChildren<AnimalPenBoundary>(true);
                    if (pen != null)
                    {
                        pen.NotifyPenMoved(delta, Quaternion.identity);
                    }
                }
                else if (pointerUp)
                {
                    FinishDrag();
                }
            }
            else if (pointerUp && isDragging)
            {
                FinishDrag();
            }

            // Escape or Right Click to deselect / cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectedObject != null)
                {
                    CancelCurrentMove();
                }
                else
                {
                    ExitEditMode();
                }
            }
        }

        private void FinishDrag()
        {
            if (!isDragging || selectedObject == null) return;
            isDragging = false;
            ClearDragIndicator();

            Vector3 finalPos = selectedObject.transform.position;
            string sName = selectedObject.name.ToLower();
            if (sName.Contains("tractor") || sName.Contains("vehicle"))
            {
                GroundSnapUtility.SnapTractorToGroundExact(selectedObject.gameObject);
            }
            else
            {
                GroundSnapUtility.SnapToGroundExact(selectedObject.gameObject);
            }
            finalPos = selectedObject.transform.position;

            if (FarmObjectPlacementValidator.ValidatePlacement(selectedObject.gameObject, finalPos, selectedObject.transform.rotation, selectedObject.footprintSize, out string failureReason))
            {
                if (Vector3.Distance(finalPos, selectedObject.originalPosition) > 0.05f)
                {
                    undoStack.Push(new EditAction
                    {
                        type = EditActionType.Move,
                        target = selectedObject,
                        previousPosition = selectedObject.originalPosition,
                        previousRotation = selectedObject.originalRotation,
                        newPosition = finalPos,
                        newRotation = selectedObject.transform.rotation,
                        wasDeleted = false
                    });
                }

                Vector3 delta = finalPos - selectedObject.originalPosition;
                selectedObject.RecordCurrentTransform();

                AnimalPenBoundary pen = selectedObject.GetComponentInChildren<AnimalPenBoundary>(true);
                if (pen != null)
                {
                    pen.NotifyPenMoved(delta, Quaternion.identity);
                }

                CropSelectionUI.ShowToast($"Đã đặt {selectedObject.displayName} tại vị trí mới!");
                SaveLoadSystem.SaveGame();
            }
            else
            {
                selectedObject.RevertToOriginal();
                CropSelectionUI.ShowToast("Vị trí không hợp lệ (" + failureReason + ")! Đã trả về vị trí cũ.");
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject == null) continue;

                if (r.gameObject.GetComponentInParent<Button>() != null ||
                    r.gameObject.GetComponentInParent<InputField>() != null ||
                    r.gameObject.GetComponentInParent<ScrollRect>() != null ||
                    (topBannerGo != null && r.gameObject.transform.IsChildOf(topBannerGo.transform)) ||
                    (selectedPanelGo != null && r.gameObject.transform.IsChildOf(selectedPanelGo.transform)) ||
                    (btnDone != null && r.gameObject.transform.IsChildOf(btnDone.transform)))
                {
                    return true;
                }
            }
            return false;
        }

        private void CreateDragIndicator(MovableFarmObject movable)
        {
            ClearDragIndicator();
            dragGhostIndicator = new GameObject("DragIndicator_Temp");
            dragGhostIndicator.transform.SetParent(movable.transform, false);
            dragGhostIndicator.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            LineRenderer lr = dragGhostIndicator.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.18f;
            lr.endWidth = 0.18f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lr.startColor = new Color(0.2f, 0.95f, 0.35f, 0.9f);
            lr.endColor = new Color(0.2f, 0.95f, 0.35f, 0.9f);

            float hx = movable.footprintSize.x * 0.5f;
            float hz = movable.footprintSize.z * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-hx, 0f, -hz),
                new Vector3(-hx, 0f,  hz),
                new Vector3( hx, 0f,  hz),
                new Vector3( hx, 0f, -hz)
            };
            lr.positionCount = 4;
            lr.SetPositions(corners);
        }

        private void UpdateDragIndicator(bool isValid)
        {
            if (dragGhostIndicator == null) return;
            LineRenderer lr = dragGhostIndicator.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Color c = isValid ? new Color(0.2f, 0.95f, 0.35f, 0.9f) : new Color(0.95f, 0.25f, 0.25f, 0.9f);
                lr.startColor = c;
                lr.endColor = c;
            }
        }

        private void ClearDragIndicator()
        {
            if (dragGhostIndicator != null)
            {
                dragGhostIndicator.SetActive(false);
                DestroyImmediate(dragGhostIndicator);
                dragGhostIndicator = null;
            }
        }

        private void UpdateUI()
        {
            if (selectedPanelGo == null) return;

            if (selectedObject != null)
            {
                selectedPanelGo.SetActive(true);
                textSelectedName.text = $"[{selectedObject.categoryGroupName}]  {selectedObject.displayName}";
            }
            else
            {
                selectedPanelGo.SetActive(false);
            }
        }

        // ================== UI CREATION ==================
        private void CreateEditModeUI()
        {
            if (editCanvasGo != null) return;

            editCanvasGo = new GameObject("FarmEditModeCanvas");
            editCanvasGo.transform.SetParent(transform, false);

            Canvas c = editCanvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 135;

            CanvasScaler scaler = editCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            editCanvasGo.AddComponent<GraphicRaycaster>();

            // 1. Top Banner
            topBannerGo = new GameObject("TopBanner");
            topBannerGo.transform.SetParent(editCanvasGo.transform, false);
            RectTransform brt = topBannerGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -12f);
            brt.sizeDelta = new Vector2(1160f, 102f);

            Image bImg = topBannerGo.AddComponent<Image>();
            bImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topBannerGo.transform, false);
            RectTransform trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.65f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = Vector2.zero;

            textBannerTitle = titleGo.AddComponent<Text>();
            textBannerTitle.text = "CHẾ ĐỘ CHỈNH SỬA NÔNG TRẠI";
            textBannerTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textBannerTitle.fontSize = 20;
            textBannerTitle.fontStyle = FontStyle.Bold;
            textBannerTitle.alignment = TextAnchor.MiddleCenter;
            textBannerTitle.color = new Color(1f, 0.88f, 0.35f);

            // Category Bar inside Banner: [TẤT CẢ] [RƠM] [GỖ] [CẦU THANG] [TRANG TRÍ] [CÔNG TRÌNH] [+ ĐẶT ĐỒ MỚI]
            categoryBarGo = new GameObject("CategoryBar");
            categoryBarGo.transform.SetParent(topBannerGo.transform, false);
            RectTransform cbrt = categoryBarGo.AddComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0f, 0.28f);
            cbrt.anchorMax = new Vector2(1f, 0.65f);
            cbrt.sizeDelta = Vector2.zero;

            CreateCategoryButton(categoryBarGo.transform, "TẤT CẢ", new Vector2(-460f, 0f), "TẤT CẢ", new Color(0.35f, 0.40f, 0.50f));
            CreateCategoryButton(categoryBarGo.transform, "MÁNG NƯỚC", new Vector2(-340f, 0f), "MÁNG NƯỚC", new Color(0.20f, 0.50f, 0.70f));
            CreateCategoryButton(categoryBarGo.transform, "RƠM", new Vector2(-220f, 0f), "RƠM", new Color(0.80f, 0.65f, 0.20f));
            CreateCategoryButton(categoryBarGo.transform, "GỖ", new Vector2(-100f, 0f), "GỖ", new Color(0.65f, 0.45f, 0.25f));
            CreateCategoryButton(categoryBarGo.transform, "CẦU THANG", new Vector2(20f, 0f), "CẦU THANG", new Color(0.25f, 0.55f, 0.70f));
            CreateCategoryButton(categoryBarGo.transform, "TRANG TRÍ", new Vector2(140f, 0f), "TRANG TRÍ", new Color(0.60f, 0.35f, 0.70f));
            CreateCategoryButton(categoryBarGo.transform, "CÔNG TRÌNH", new Vector2(260f, 0f), "CÔNG TRÌNH", new Color(0.40f, 0.55f, 0.40f));
            CreateAddDecorationButton(categoryBarGo.transform, "+ ĐẶT ĐỒ MỚI", new Vector2(400f, 0f), new Color(0.20f, 0.68f, 0.35f));

            GameObject instGo = new GameObject("Instruction");
            instGo.transform.SetParent(topBannerGo.transform, false);
            RectTransform irt = instGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(1f, 0.28f);
            irt.sizeDelta = Vector2.zero;

            textBannerInstruction = instGo.AddComponent<Text>();
            textBannerInstruction.text = "Rê chuột xem trước • Kéo để Di Chuyển • Xoay / Xác Nhận / Xóa bên dưới • Phím Delete để Xóa";
            textBannerInstruction.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textBannerInstruction.fontSize = 13;
            textBannerInstruction.alignment = TextAnchor.MiddleCenter;
            textBannerInstruction.color = new Color(0.85f, 0.90f, 0.95f);

            // 2. Top-Right Buttons: [HOÀN TÁC (Ctrl+Z)] and [HOÀN TẤT]
            btnTopUndo = CreateActionButton(editCanvasGo.transform, "HOÀN TÁC", new Vector2(-185f, -20f), new Vector2(140f, 60f), new Color(0.42f, 0.32f, 0.62f), anchorTopRight: true);
            btnTopUndo.onClick.AddListener(UndoLastAction);

            btnDone = CreateActionButton(editCanvasGo.transform, "HOÀN TẤT", new Vector2(-25f, -20f), new Vector2(140f, 60f), new Color(0.20f, 0.65f, 0.30f), anchorTopRight: true);
            btnDone.onClick.AddListener(ExitEditMode);

            // 3. Bottom Selected Controls Panel
            selectedPanelGo = new GameObject("SelectedControlsPanel");
            selectedPanelGo.transform.SetParent(editCanvasGo.transform, false);
            RectTransform srt = selectedPanelGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 25f);
            srt.sizeDelta = new Vector2(1060f, 100f);

            Image sImg = selectedPanelGo.AddComponent<Image>();
            sImg.color = new Color(0.10f, 0.14f, 0.20f, 0.96f);

            // Selected Name Text
            GameObject nameGo = new GameObject("SelectedNameText");
            nameGo.transform.SetParent(selectedPanelGo.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.58f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.sizeDelta = Vector2.zero;

            textSelectedName = nameGo.AddComponent<Text>();
            textSelectedName.text = "Đang chọn: [RƠM] Cục rơm (Straw 1)";
            textSelectedName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textSelectedName.fontSize = 17;
            textSelectedName.fontStyle = FontStyle.Bold;
            textSelectedName.alignment = TextAnchor.MiddleCenter;
            textSelectedName.color = new Color(1f, 0.88f, 0.35f);

            // Control Buttons Row: [XOAY TRÁI] [XOAY PHẢI] [XÁC NHẬN] [HỦY] [XÓA] [HOÀN TÁC] [✕]
            btnRotateLeft = CreateActionButton(selectedPanelGo.transform, "XOAY TRÁI", new Vector2(-380f, 24f), new Vector2(125f, 44f), new Color(0.18f, 0.48f, 0.72f));
            btnRotateLeft.onClick.AddListener(() => RotateCurrent(-45f));

            btnRotateRight = CreateActionButton(selectedPanelGo.transform, "XOAY PHẢI", new Vector2(-245f, 24f), new Vector2(125f, 44f), new Color(0.18f, 0.48f, 0.72f));
            btnRotateRight.onClick.AddListener(() => RotateCurrent(45f));

            btnConfirm = CreateActionButton(selectedPanelGo.transform, "XÁC NHẬN", new Vector2(-95f, 24f), new Vector2(140f, 44f), new Color(0.20f, 0.65f, 0.30f));
            btnConfirm.onClick.AddListener(ConfirmCurrentPlacement);

            btnCancelMove = CreateActionButton(selectedPanelGo.transform, "HỦY", new Vector2(50f, 24f), new Vector2(120f, 44f), new Color(0.85f, 0.50f, 0.15f));
            btnCancelMove.onClick.AddListener(CancelCurrentMove);

            btnDelete = CreateActionButton(selectedPanelGo.transform, "XÓA", new Vector2(185f, 24f), new Vector2(120f, 44f), new Color(0.85f, 0.22f, 0.22f));
            btnDelete.onClick.AddListener(DeleteCurrent);

            btnUndo = CreateActionButton(selectedPanelGo.transform, "HOÀN TÁC", new Vector2(325f, 24f), new Vector2(130f, 44f), new Color(0.42f, 0.32f, 0.62f));
            btnUndo.onClick.AddListener(UndoLastAction);

            btnDeselect = CreateActionButton(selectedPanelGo.transform, "✕ BỎ CHỌN", new Vector2(455f, 24f), new Vector2(100f, 44f), new Color(0.38f, 0.42f, 0.48f));
            btnDeselect.onClick.AddListener(DeselectCurrent);

            editCanvasGo.SetActive(false);

            // Clean up any obsolete stray Edit HUD button objects
            GameObject oldHudGo = GameObject.Find("FarmEditModeHUDCanvas");
            if (oldHudGo != null && oldHudGo != editCanvasGo)
            {
                DestroyImmediate(oldHudGo);
            }
        }

        private void CreateCategoryButton(Transform parent, string label, Vector2 anchoredPos, string categoryName, Color color)
        {
            GameObject bGo = new GameObject("CatBtn_" + label);
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(125f, 28f);

            Image img = bGo.AddComponent<Image>();
            img.color = color;

            Button btn = bGo.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectNextInCategory(categoryName));

            GameObject tGo = new GameObject("Text");
            tGo.transform.SetParent(bGo.transform, false);
            RectTransform trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = tGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private Button CreateActionButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color, bool anchorTopRight = false)
        {
            GameObject bGo = new GameObject("Btn_" + label);
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            if (anchorTopRight)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = bGo.AddComponent<Image>();
            img.color = color;

            Button btn = bGo.AddComponent<Button>();

            GameObject tGo = new GameObject("Text");
            tGo.transform.SetParent(bGo.transform, false);
            RectTransform trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = tGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = (size.y > 50f) ? 16 : 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private GameObject decoPaletteGo;

        private void CreateAddDecorationButton(Transform parent, string label, Vector2 anchoredPos, Color color)
        {
            GameObject bGo = new GameObject("CatBtn_AddDeco");
            bGo.transform.SetParent(parent, false);

            RectTransform rt = bGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(130f, 28f);

            Image img = bGo.AddComponent<Image>();
            img.color = color;

            Button btn = bGo.AddComponent<Button>();
            btn.onClick.AddListener(ToggleDecorationPalette);

            GameObject tGo = new GameObject("Text");
            tGo.transform.SetParent(bGo.transform, false);
            RectTransform trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = tGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        public void ToggleDecorationPalette()
        {
            if (decoPaletteGo == null)
            {
                CreateDecorationPalette();
            }
            else
            {
                decoPaletteGo.SetActive(!decoPaletteGo.activeSelf);
            }
        }

        private void CreateDecorationPalette()
        {
            if (editCanvasGo == null) return;

            decoPaletteGo = new GameObject("DecorationPalettePanel");
            decoPaletteGo.transform.SetParent(editCanvasGo.transform, false);

            RectTransform prt = decoPaletteGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -125f);
            prt.sizeDelta = new Vector2(980f, 95f);

            Image bg = decoPaletteGo.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            // Horizontal ScrollView
            GameObject scrollGo = new GameObject("DecoScrollView");
            scrollGo.transform.SetParent(decoPaletteGo.transform, false);
            RectTransform srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(10f, 6f);
            srt.offsetMax = new Vector2(-10f, -6f);

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 28f;

            GameObject vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            RectTransform vprt = vpGo.AddComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.sizeDelta = Vector2.zero;

            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            vpGo.AddComponent<RectMask2D>();
            scroll.viewport = vprt;

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            RectTransform crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(400f, 0f);

            HorizontalLayoutGroup hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = crt;

            var decos = FarmDecorationManager.AvailableDecorations;
            for (int i = 0; i < decos.Count; i++)
            {
                var d = decos[i];
                GameObject btnGo = new GameObject("DecoBtn_" + d.id);
                btnGo.transform.SetParent(contentGo.transform, false);

                RectTransform brt = btnGo.AddComponent<RectTransform>();
                brt.sizeDelta = new Vector2(165f, 72f);

                Image bImg = btnGo.AddComponent<Image>();
                bImg.color = new Color(0.24f, 0.32f, 0.44f, 0.95f);
                bImg.raycastTarget = true;

                Button btn = btnGo.AddComponent<Button>();
                string decoId = d.id;
                int price = d.priceGold;
                string dName = d.displayName;
                btn.onClick.AddListener(() => SpawnNewDecoration(decoId, price, dName));

                GameObject txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                RectTransform trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.sizeDelta = Vector2.zero;

                Text txt = txtGo.AddComponent<Text>();
                txt.text = $"{d.displayName}\n<color=#FFDE40>{d.priceGold} Vàng</color>";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.supportRichText = true;
                txt.color = Color.white;
            }
        }

        public void SpawnNewDecoration(string decoId, int priceGold, string displayName)
        {
            if (PlayerProgression.Instance != null && PlayerProgression.Instance.Gold < priceGold)
            {
                CropSelectionUI.ShowToast($"Không đủ Vàng ({priceGold} G) để mua {displayName}!");
                return;
            }

            Vector3 spawnPos = new Vector3(80f, 0f, 240f);
            if (targetCamera != null)
            {
                Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.4f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                {
                    spawnPos = hit.point;
                }
            }

            if (PlayerProgression.TrySpendGold(priceGold))
            {
                GameObject spawned = FarmDecorationManager.EnsureInstance().SpawnDecoration(decoId, spawnPos, Quaternion.identity);
                if (spawned != null)
                {
                    MovableFarmObject mfo = spawned.GetComponent<MovableFarmObject>();
                    if (mfo != null)
                    {
                        SelectMovable(mfo);
                    }
                    CropSelectionUI.ShowToast($"Đã đặt {displayName} ({priceGold} G)! Kéo và xoay để chỉnh vị trí.");
                    SaveLoadSystem.SaveGame();
                }
            }

            if (decoPaletteGo != null) decoPaletteGo.SetActive(false);
        }
    }
}
