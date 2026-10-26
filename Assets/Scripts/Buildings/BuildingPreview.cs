using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Component attached to the ghost preview object during Placement Mode.
    /// Manages collision footprint checking, terrain slope verification, and color tinting (Green for valid, Red for invalid).
    /// </summary>
    public class BuildingPreview : MonoBehaviour
    {
        [Header("Footprint & Collision Settings")]
        public Vector3 footprintSize = new Vector3(8f, 5f, 8f);
        public bool isValidPosition = true;

        private List<Renderer> previewRenderers = new List<Renderer>();
        private static Material sharedPreviewMat;

        private static readonly Color ColorValid = new Color(0.25f, 0.95f, 0.35f, 0.65f);
        private static readonly Color ColorInvalid = new Color(0.95f, 0.25f, 0.25f, 0.65f);

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        public void Initialize(Vector3 size)
        {
            footprintSize = size;
            previewRenderers.Clear();
            previewRenderers.AddRange(GetComponentsInChildren<Renderer>(true));

            // Phase 3: Remove all rigidbodies and colliders from ghost preview
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs) SafeDestroy(rb);

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                c.enabled = false;
                SafeDestroy(c);
            }

            // Create safe shared transparent material compatible with active pipeline
            if (sharedPreviewMat == null)
            {
                Shader shader = null;
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                }
                if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                if (shader == null) shader = Shader.Find("Transparent/Diffuse");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                sharedPreviewMat = new Material(shader);
                if (sharedPreviewMat.HasProperty("_Mode"))
                {
                    sharedPreviewMat.SetFloat("_Mode", 3); // Transparent
                    sharedPreviewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    sharedPreviewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    sharedPreviewMat.SetInt("_ZWrite", 0);
                    sharedPreviewMat.DisableKeyword("_ALPHATEST_ON");
                    sharedPreviewMat.EnableKeyword("_ALPHABLEND_ON");
                    sharedPreviewMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    sharedPreviewMat.renderQueue = 3000;
                }
            }

            // Apply preview material
            foreach (var r in previewRenderers)
            {
                if (r != null)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = new Material(sharedPreviewMat);
                    r.materials = mats;
                }
            }

            SetVisualStatus(true);
        }

        public bool CheckPlacementValidity(Vector3 position)
        {
            // 1. Terrain slope & bounds check
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                isValidPosition = false;
                SetVisualStatus(false);
                return false;
            }

            // Check Farm general boundary (X: 25 to 145, Z: 190 to 310)
            if (position.x < 25f || position.x > 145f || position.z < 190f || position.z > 310f)
            {
                isValidPosition = false;
                SetVisualStatus(false);
                return false;
            }

            // Slope check excluding ghost layer
            int ghostLayer = LayerMask.NameToLayer("PlacementGhost");
            int mask = ~((1 << 2) | (1 << 5) | (ghostLayer >= 0 ? (1 << ghostLayer) : 0));
            Ray ray = new Ray(position + Vector3.up * 5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit groundHit, 15f, mask))
            {
                if (groundHit.normal.y < 0.85f) // Too steep
                {
                    isValidPosition = false;
                    SetVisualStatus(false);
                    return false;
                }
            }

            // 2. Obstacle overlap check
            Vector3 center = position + Vector3.up * (footprintSize.y * 0.5f);
            Vector3 halfExtents = new Vector3(footprintSize.x * 0.45f, footprintSize.y * 0.45f, footprintSize.z * 0.45f);

            Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.transform.root == transform.root || h.transform.IsChildOf(transform)) continue; // Ignore preview's own colliders
                if (h is TerrainCollider) continue; // Ignore terrain ground
                if (h.isTrigger) continue; // Ignore triggers

                // If hit any building, machine, plot, fence, or obstacles
                if (h.GetComponentInParent<FarmBuilding>() != null ||
                    h.GetComponentInParent<ProductionMachine>() != null ||
                    h.GetComponentInParent<CropPlot>() != null ||
                    h.gameObject.name.IndexOf("fence", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.gameObject.name.IndexOf("wall", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.gameObject.name.IndexOf("house", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.gameObject.name.IndexOf("shed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.gameObject.name.IndexOf("road", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isValidPosition = false;
                    SetVisualStatus(false);
                    return false;
                }
            }

            isValidPosition = true;
            SetVisualStatus(true);
            return true;
        }

        public void SetVisualStatus(bool valid)
        {
            Color targetColor = valid ? ColorValid : ColorInvalid;
            foreach (var r in previewRenderers)
            {
                if (r != null)
                {
                    foreach (var m in r.materials)
                    {
                        if (m != null)
                        {
                            m.color = targetColor;
                            if (m.HasProperty("_Color")) m.SetColor("_Color", targetColor);
                        }
                    }
                }
            }
        }
    }
}
