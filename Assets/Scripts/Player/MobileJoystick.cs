using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Virtual joystick for mobile and desktop touch/mouse drag controls.
    /// Works with Unity UI EventSystem and maintains compatibility with legacy VCAnalogJoystick.
    /// </summary>
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static MobileJoystick Instance { get; private set; }

        [Header("Joystick References")]
        public RectTransform background;
        public RectTransform handle;

        [Header("Settings")]
        public float handleRange = 1f;
        public float deadZone = 0.05f;

        private Canvas canvas;
        private Camera targetCamera;
        private Vector2 inputVector = Vector2.zero;

        public Vector2 Direction => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                targetCamera = canvas.worldCamera;
            }

            if (background == null)
            {
                background = GetComponent<RectTransform>();
            }

            if (handle == null && transform.childCount > 0)
            {
                handle = transform.GetChild(0).GetComponent<RectTransform>();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool isPointerDown = false;
        private int activeFingerId = -1;

        void Update()
        {
            // Direct mouse/touch tracking fallback ensuring joystick never drops drag input
            if (isPointerDown)
            {
                if (Input.touchCount > 0)
                {
                    bool foundTouch = false;
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.fingerId == activeFingerId || activeFingerId == -1)
                        {
                            UpdateJoystickPosition(t.position);
                            foundTouch = true;
                            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                            {
                                ResetJoystick();
                            }
                            break;
                        }
                    }
                    if (!foundTouch && activeFingerId != -1)
                    {
                        ResetJoystick();
                    }
                }
                else if (Input.GetMouseButton(0))
                {
                    UpdateJoystickPosition(Input.mousePosition);
                }
                else
                {
                    ResetJoystick();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            activeFingerId = eventData.pointerId;
            UpdateJoystickPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isPointerDown)
            {
                UpdateJoystickPosition(eventData.position);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick();
        }

        public void UpdateJoystickPosition(Vector2 screenPosition)
        {
            if (background == null || handle == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                screenPosition,
                targetCamera,
                out localPoint
            );

            Vector2 radius = background.sizeDelta / 2f;
            if (radius.x <= 0) radius.x = 50f;
            if (radius.y <= 0) radius.y = 50f;

            // Normalize coordinates
            float rawX = localPoint.x / radius.x;
            float rawY = localPoint.y / radius.y;

            inputVector = new Vector2(rawX, rawY);
            if (inputVector.magnitude > 1f)
            {
                inputVector = inputVector.normalized;
            }

            if (inputVector.magnitude < deadZone)
            {
                inputVector = Vector2.zero;
            }

            // Move handle visually
            handle.anchoredPosition = new Vector2(
                inputVector.x * (radius.x * handleRange),
                inputVector.y * (radius.y * handleRange)
            );
        }

        public void ResetJoystick()
        {
            isPointerDown = false;
            activeFingerId = -1;
            inputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// Automatically ensures that a MobileJoystick Canvas exists in the scene.
        /// </summary>
        public static MobileJoystick EnsureJoystickInScene()
        {
            if (Instance != null) return Instance;

            MobileJoystick existing = FindAnyObjectByType<MobileJoystick>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            // Ensure EventSystem exists
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // Create Canvas for Mobile Joystick
            GameObject canvasGo = new GameObject("MobileControlsCanvas");
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasGroup cg = canvasGo.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;

            // Create Joystick Background
            GameObject bgGo = new GameObject("MobileJoystick");
            bgGo.transform.SetParent(canvasGo.transform, false);

            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(0f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(180f, 180f);
            bgRect.sizeDelta = new Vector2(200f, 200f);

            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = CreateCircleSprite(128, new Color(0.1f, 0.1f, 0.1f, 0.5f), new Color(1f, 1f, 1f, 0.7f));
            bgImg.raycastTarget = true;

            // Create Joystick Handle
            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(bgGo.transform, false);

            RectTransform handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);

            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = CreateCircleSprite(64, new Color(1f, 1f, 1f, 0.8f), new Color(0.8f, 0.8f, 0.8f, 1f));
            handleImg.raycastTarget = false;

            MobileJoystick joystick = bgGo.AddComponent<MobileJoystick>();
            joystick.background = bgRect;
            joystick.handle = handleRect;
            joystick.handleRange = 1f;

            Instance = joystick;
            Debug.Log("[MobileJoystick] Dynamic mobile joystick created successfully.");
            return joystick;
        }

        private static Sprite CreateCircleSprite(int size, Color fillColor, Color borderColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 2) / 2f;
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - 2)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
