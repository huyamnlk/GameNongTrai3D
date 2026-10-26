using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Persistent per-segment record of the designed height above ground for fence pieces (rails, beams,
    /// wall colliders, pen props). Recorded once from the flat-ground design layout so repeated alignments
    /// (Edit Mode moves, Load, Spawn/Restore) are idempotent and never accumulate drift.
    /// </summary>
    [DisallowMultipleComponent]
    public class FenceGroundAnchor : MonoBehaviour
    {
        public bool recorded;
        public float designHeight;              // local Y of segment center above pen base (flat design)
        public Quaternion designLocalRotation = Quaternion.identity;
        public Vector3 designColliderCenter;     // for Collider_Wall_* BoxColliders
    }

    /// <summary>
    /// Grounds every fence segment individually:
    /// - Procedural pens (Cow/Chicken/Pig/Horse): each post sits on the terrain under its own footprint,
    ///   rails/beams/wall colliders follow the terrain slope between their ends, flat bedding tilts to fit.
    /// - Standalone stone walls / wooden fences: per-segment offset from Renderer.bounds.min.y and
    ///   Collider.bounds.min.y vs the pivot, placed so the lowest point touches the lowest ground under the segment.
    /// Horizontal (yaw) rotation is always preserved.
    /// </summary>
    public static class FenceGroundAligner
    {
        /// <summary>How deep the base of a post/wall is embedded to hide micro gaps (kept tiny so nothing looks sunk).</summary>
        public const float EmbedDepth = 0.03f;
        /// <summary>Max tolerated embed of a standalone segment on steep ground before it is considered sunk.</summary>
        public const float MaxEmbedDepth = 0.35f;

        private static readonly string[] FlatGroundLayers = { "PenBedding", "MudWallow", "StrawPatch", "HorseStallStraw", "PigShelterStraw" };

        // ------------------------------------------------------------------ ground sampling
        /// <summary>
        /// Raycasts straight down from above <paramref name="pos"/> to the Terrain / ground surface
        /// (triggers and the object itself are ignored). Falls back to Terrain.SampleHeight.
        /// </summary>
        public static float SampleGroundY(Vector3 pos, Transform ignoreRoot = null)
        {
            Vector3 start = new Vector3(pos.x, pos.y + 60f, pos.z);
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                    if (h.collider is TerrainCollider || IsGroundName(h.collider.gameObject.name))
                        return h.point.y;
                }
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
                return terrain.SampleHeight(pos) + terrain.transform.position.y;
            return float.NaN;
        }

        private static bool IsGroundName(string n)
        {
            string l = n.ToLowerInvariant();
            return l.Contains("terrain") || l == "ground" || l.Contains("farmground") || l.Contains("groundplane");
        }

        private static bool HasGround()
        {
            return Terrain.activeTerrain != null || !float.IsNaN(SampleGroundY(Vector3.zero));
        }

        // ------------------------------------------------------------------ classification
        public static bool IsPenFenceSegment(string n)
        {
            return n.StartsWith("Post_") || n.StartsWith("GatePost_") || n.StartsWith("Rail_") ||
                   n == "GateHeaderBeam" || n.StartsWith("Collider_Wall_");
        }

        /// <summary>True for a standalone wall/fence segment root (has its own renderer or LODGroup, not part of a pen).</summary>
        public static bool IsStandaloneFenceSegment(GameObject go)
        {
            if (go == null) return false;
            string l = go.name.ToLowerInvariant();
            if (!(l.Contains("stone_wall") || l.Contains("fence") || l.StartsWith("wall") || l.Contains("_wall")))
                return false;
            if (l.Contains("fallback") || l.StartsWith("collider_wall")) return false;
            if (go.GetComponentInParent<FarmBuilding>(true) != null) return false;
            if (go.GetComponent<LODGroup>() == null && go.GetComponent<Renderer>() == null) return false;

            // Skip LOD children whose parent is already the segment root
            Transform p = go.transform.parent;
            while (p != null)
            {
                if (p.GetComponent<LODGroup>() != null) return false;
                p = p.parent;
            }
            return true;
        }

        // ------------------------------------------------------------------ pens
        /// <summary>
        /// Places the pen pivot (designed at fence base) on the ground at its center, then grounds every
        /// post, rail, beam, wall collider and floor layer individually.
        /// </summary>
        public static void AlignPen(GameObject penRoot, bool snapRoot = true)
        {
            if (penRoot == null) return;
            Transform model = penRoot.transform.Find("ProceduralModel");
            if (model == null) return;

            RecordAnchors(model);
            if (!HasGround()) return; // prefab asset context: anchors only

            Physics.SyncTransforms();
            if (snapRoot)
            {
                float gy = SampleGroundY(penRoot.transform.position, penRoot.transform);
                if (!float.IsNaN(gy))
                {
                    Vector3 p = penRoot.transform.position;
                    penRoot.transform.position = new Vector3(p.x, gy, p.z);
                }
            }

            float sy = Mathf.Max(0.0001f, model.lossyScale.y);
            for (int i = 0; i < model.childCount; i++)
            {
                Transform seg = model.GetChild(i);
                string n = seg.name;
                if (n.StartsWith("Post_") || n.StartsWith("GatePost_"))
                    AlignPost(seg, penRoot.transform, sy);
                else if (n.StartsWith("Rail_") || n == "GateHeaderBeam")
                    AlignSpan(seg, penRoot.transform, model, sy, false);
                else if (n.StartsWith("Collider_Wall_"))
                    AlignSpan(seg, penRoot.transform, model, sy, true);
                else if (System.Array.IndexOf(FlatGroundLayers, StripIndex(n)) >= 0)
                    AlignFlatLayer(seg, penRoot.transform, sy);
                else if (seg.GetComponent<TextMesh>() == null && n != "Billboard")
                    AlignProp(seg, penRoot.transform, sy);
            }
        }

        private static string StripIndex(string n)
        {
            int u = n.LastIndexOf('_');
            if (u > 0 && int.TryParse(n.Substring(u + 1), out _)) return n.Substring(0, u);
            return n;
        }

        /// <summary>Stores the flat-design height of every pen child once (idempotent).</summary>
        public static void RecordAnchors(Transform model)
        {
            for (int i = 0; i < model.childCount; i++)
            {
                Transform seg = model.GetChild(i);
                if (seg.name.StartsWith("Post_") || seg.name.StartsWith("GatePost_")) continue; // derived from scale
                FenceGroundAnchor a = seg.GetComponent<FenceGroundAnchor>();
                if (a == null) a = seg.gameObject.AddComponent<FenceGroundAnchor>();
                if (a.recorded) continue;
                a.designHeight = seg.localPosition.y;
                a.designLocalRotation = seg.localRotation;
                BoxCollider bc = seg.GetComponent<BoxCollider>();
                if (bc != null) a.designColliderCenter = bc.center;
                a.recorded = true;
            }
        }

        private static void AlignPost(Transform post, Transform ignoreRoot, float sy)
        {
            // Lowest ground under the 4 footprint corners so no side of the post floats.
            Vector3 c = post.position;
            float hx = post.lossyScale.x * 0.5f, hz = post.lossyScale.z * 0.5f;
            float gMin = float.MaxValue;
            foreach (Vector3 o in new[] { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), Vector3.zero })
            {
                float g = SampleGroundY(c + post.rotation * o, ignoreRoot);
                if (!float.IsNaN(g)) gMin = Mathf.Min(gMin, g);
            }
            if (gMin == float.MaxValue) return;
            float halfH = post.localScale.y * sy * 0.5f;
            post.position = new Vector3(c.x, gMin - EmbedDepth + halfH, c.z);
        }

        /// <summary>Rails / header beam / wall colliders: center follows mean ground of both ends, pitched along the slope.</summary>
        private static void AlignSpan(Transform seg, Transform ignoreRoot, Transform model, float sy, bool isCollider)
        {
            FenceGroundAnchor a = seg.GetComponent<FenceGroundAnchor>();
            if (a == null || !a.recorded) return;

            Vector3 size = seg.localScale;
            BoxCollider bc = isCollider ? seg.GetComponent<BoxCollider>() : null;
            if (bc != null) size = bc.size;
            bool alongX = size.x >= size.z;
            float halfLen = (alongX ? size.x : size.z) * 0.5f;

            seg.localRotation = a.designLocalRotation; // reset before measuring
            if (bc != null) bc.center = a.designColliderCenter;
            Vector3 axisWorld = seg.TransformDirection(alongX ? Vector3.right : Vector3.forward);
            axisWorld.y = 0f;
            Vector3 centerWorld = bc != null ? seg.TransformPoint(bc.center) : seg.position;
            float worldHalfLen = isCollider
                ? halfLen * (alongX ? seg.lossyScale.x : seg.lossyScale.z)
                : halfLen * (alongX ? model.lossyScale.x : model.lossyScale.z);
            Vector3 dir = axisWorld.normalized;

            float gA = SampleGroundY(centerWorld - dir * worldHalfLen, ignoreRoot);
            float gB = SampleGroundY(centerWorld + dir * worldHalfLen, ignoreRoot);
            if (float.IsNaN(gA) || float.IsNaN(gB)) return;

            float heightAbove = (isCollider ? (a.designHeight + a.designColliderCenter.y) : a.designHeight) * sy;
            float targetCenterY = (gA + gB) * 0.5f + heightAbove - (isCollider ? 0f : EmbedDepth);
            float pitch = Mathf.Atan2(gB - gA, worldHalfLen * 2f) * Mathf.Rad2Deg;
            seg.localRotation = a.designLocalRotation * (alongX ? Quaternion.Euler(0f, 0f, pitch) : Quaternion.Euler(-pitch, 0f, 0f));

            Vector3 currentCenter = bc != null ? seg.TransformPoint(bc.center) : seg.position;
            seg.position += new Vector3(0f, targetCenterY - currentCenter.y, 0f);
        }

        /// <summary>Flat floor layers (bedding/straw/mud) are pitched/rolled to fit the ground plane under them.</summary>
        private static void AlignFlatLayer(Transform seg, Transform ignoreRoot, float sy)
        {
            FenceGroundAnchor a = seg.GetComponent<FenceGroundAnchor>();
            if (a == null || !a.recorded) return;
            seg.localRotation = a.designLocalRotation;
            Vector3 c = seg.position;
            Vector3 rx = seg.right * (seg.lossyScale.x * 0.5f);
            Vector3 fz = seg.forward * (seg.lossyScale.z * 0.5f);
            float gL = SampleGroundY(c - rx, ignoreRoot), gR = SampleGroundY(c + rx, ignoreRoot);
            float gB = SampleGroundY(c - fz, ignoreRoot), gF = SampleGroundY(c + fz, ignoreRoot);
            float gC = SampleGroundY(c, ignoreRoot);
            if (float.IsNaN(gL) || float.IsNaN(gR) || float.IsNaN(gB) || float.IsNaN(gF) || float.IsNaN(gC)) return;
            float roll = Mathf.Atan2(gR - gL, rx.magnitude * 2f) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(gF - gB, fz.magnitude * 2f) * Mathf.Rad2Deg;
            seg.localRotation = a.designLocalRotation * Quaternion.Euler(-pitch, 0f, roll);
            float mean = (gL + gR + gB + gF + gC) / 5f;
            seg.position = new Vector3(c.x, mean + a.designHeight * sy, c.z);
        }

        /// <summary>Props (troughs, shed, hay rack, signs) keep their designed height above the ground right under them.</summary>
        private static void AlignProp(Transform seg, Transform ignoreRoot, float sy)
        {
            FenceGroundAnchor a = seg.GetComponent<FenceGroundAnchor>();
            if (a == null || !a.recorded) return;
            float g = SampleGroundY(seg.position, ignoreRoot);
            if (float.IsNaN(g)) return;
            Vector3 c = seg.position;
            seg.position = new Vector3(c.x, g + a.designHeight * sy, c.z);
        }

        // ------------------------------------------------------------------ standalone walls / fences
        /// <summary>
        /// Grounds one standalone wall/fence segment. Offset is measured from pivot to the lowest
        /// Renderer.bounds.min.y / Collider.bounds.min.y; the segment is lowered/raised so that lowest
        /// point touches the lowest ground under its footprint (never floating), embed capped by MaxEmbedDepth.
        /// </summary>
        public static bool AlignStandaloneSegment(GameObject seg)
        {
            if (seg == null) return false;
            Bounds b;
            if (!TryGetSegmentBounds(seg, out b)) return false;

            float gMin = float.MaxValue, gMax = float.MinValue;
            foreach (Vector3 p in new[] {
                new Vector3(b.min.x, b.center.y, b.min.z), new Vector3(b.max.x, b.center.y, b.min.z),
                new Vector3(b.min.x, b.center.y, b.max.z), new Vector3(b.max.x, b.center.y, b.max.z), b.center })
            {
                float g = SampleGroundY(p, seg.transform);
                if (float.IsNaN(g)) continue;
                gMin = Mathf.Min(gMin, g); gMax = Mathf.Max(gMax, g);
            }
            if (gMin == float.MaxValue) return false;

            float pivotToBottom = seg.transform.position.y - b.min.y;
            float targetBottom = gMin - EmbedDepth;
            // On slopes, never let the uphill side be buried more than MaxEmbedDepth.
            if (gMax - targetBottom > MaxEmbedDepth + b.size.y * 0.5f) targetBottom = gMax - MaxEmbedDepth - b.size.y * 0.5f;
            Vector3 pos = seg.transform.position;
            seg.transform.position = new Vector3(pos.x, targetBottom + pivotToBottom, pos.z);
            return true;
        }

        private static bool TryGetSegmentBounds(GameObject seg, out Bounds b)
        {
            b = new Bounds();
            bool has = false;
            foreach (Renderer r in seg.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r.GetComponent<TextMesh>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            foreach (Collider c in seg.GetComponentsInChildren<Collider>(false))
            {
                if (c == null || !c.enabled || c.isTrigger) continue;
                if (!has) { b = c.bounds; has = true; } else b.Encapsulate(c.bounds);
            }
            return has;
        }

        // ------------------------------------------------------------------ scene-wide
        /// <summary>Grounds all pens and standalone wall/fence segments in the loaded scene. Returns segments aligned.</summary>
        public static int AlignAllInScene()
        {
            if (!HasGround()) return 0;
            Physics.SyncTransforms();
            int count = 0;
            HashSet<GameObject> pens = new HashSet<GameObject>();
            foreach (AnimalPenBoundary pen in Object.FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None))
            {
                if (pen == null || pens.Contains(pen.gameObject)) continue;
                pens.Add(pen.gameObject);
                AlignPen(pen.gameObject);
                count++;
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || !IsStandaloneFenceSegment(t.gameObject)) continue;
                if (AlignStandaloneSegment(t.gameObject)) count++;
            }
            Physics.SyncTransforms();
            return count;
        }

        /// <summary>Re-grounds a single moved/spawned object (pen or standalone fence). Returns true if handled.</summary>
        public static bool AlignObject(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponent<AnimalPenBoundary>() != null && go.transform.Find("ProceduralModel") != null)
            {
                AlignPen(go);
                return true;
            }
            if (IsStandaloneFenceSegment(go))
            {
                return AlignStandaloneSegment(go);
            }
            return false;
        }
    }
}
