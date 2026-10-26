using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Drives the visible operation of a ProductionMachine: shaking body, spinning parts,
    /// rising steam puffs, glowing parts and a status lamp while producing, and a floating
    /// product marker once the batch is ready to be collected.
    /// Built only from collider-free primitives so it never interferes with clicks or movement.
    /// </summary>
    public class MachineWorkAnimator : MonoBehaviour
    {
        private const int PuffCount = 5;
        private const float PuffLifetime = 2.2f;

        private ProductionMachine machine;
        private Transform steamAnchor;
        private Transform lampAnchor;
        private Transform fxRoot;

        private Renderer lampRenderer;
        private Material lampIdle;
        private Material lampWorking;
        private Material lampDone;

        private Transform[] puffs;
        private Vector3 steamOrigin;
        private Transform marker;
        private Renderer markerRenderer;
        private string markerItemId = "";

        private Transform[] spinners;
        private Vector3[] spinAxes;
        private GameObject[] glowParts;

        private float workBlend;          // 0 = idle, 1 = full speed (eases in / out)
        private float phase;
        private Transform shakeTarget;
        private Vector3 shakeBasePos;
        private Vector3 shakeBaseScale = Vector3.one;
        private bool built;
        private bool spinnersCollected;

        public static MachineWorkAnimator Ensure(ProductionMachine pm)
        {
            if (pm == null) return null;
            MachineWorkAnimator a = pm.GetComponent<MachineWorkAnimator>();
            if (a == null) a = pm.gameObject.AddComponent<MachineWorkAnimator>();
            a.machine = pm;
            return a;
        }

        /// <summary>Forces the effect rig to be (re)built, e.g. after the machine model changed.</summary>
        public void Rebuild()
        {
            DestroyFx();
            built = false;
            spinnersCollected = false;
            shakeTarget = null;
        }

        void Awake()
        {
            if (machine == null) machine = GetComponent<ProductionMachine>();
        }

        void OnDisable()
        {
            RestoreShake();
        }

        void Update()
        {
            if (machine == null) machine = GetComponent<ProductionMachine>();
            if (machine == null || !machine.isPurchased)
            {
                SetFxVisible(false);
                return;
            }

            if (!built) BuildFx();

            bool producing = machine.state == MachineState.Producing;
            bool completed = machine.state == MachineState.Completed;

            float target = producing ? 1f : 0f;
            workBlend = Mathf.MoveTowards(workBlend, target, Time.deltaTime * 2.5f);
            phase += Time.deltaTime;

            AnimateBody(producing);
            AnimateSpinners();
            AnimateGlow(producing);
            AnimatePuffs(producing);
            AnimateLamp(producing, completed);
            AnimateMarker(completed);
        }

        // ------------------------------------------------------------------ build
        private void BuildFx()
        {
            built = true;
            Transform visual = machine.transform.Find("VisualModel");
            Transform proc = machine.transform.Find(MachineVisualFactory.ModelRootName);

            Bounds b = ComputeLocalBounds();

            steamAnchor = FindAnchor(MachineVisualFactory.SteamAnchorName);
            lampAnchor = FindAnchor(MachineVisualFactory.IndicatorAnchorName);

            fxRoot = new GameObject("WorkFx").transform;
            fxRoot.SetParent(machine.transform, false);

            Vector3 steamLocal = steamAnchor != null
                ? machine.transform.InverseTransformPoint(steamAnchor.position)
                : new Vector3(b.center.x, Mathf.Max(b.max.y, 3f), b.center.z);
            Vector3 lampLocal = lampAnchor != null
                ? machine.transform.InverseTransformPoint(lampAnchor.position)
                : new Vector3(b.center.x + b.extents.x * 0.5f, Mathf.Min(2.4f, b.max.y * 0.5f), b.max.z + 0.05f);

            steamOrigin = steamLocal;
            Color steamCol = MachineVisualFactory.GetSteamColor(machine.machineId);
            Material steamMat = FarmVisualFactory.GetSharedMaterial("MachSteamPuff_" + machine.machineId, steamCol, 0.05f);

            puffs = new Transform[PuffCount];
            for (int i = 0; i < PuffCount; i++)
            {
                GameObject p = FarmVisualFactory.CreatePart(PrimitiveType.Sphere, fxRoot, "Puff" + i,
                    steamLocal, Vector3.zero, steamMat, default, false);
                puffs[i] = p.transform;
                p.SetActive(false);
            }

            lampIdle = FarmVisualFactory.GetSharedMaterial("MachLampIdle", new Color(0.25f, 0.27f, 0.30f), 0.4f);
            lampWorking = FarmVisualFactory.GetSharedMaterial("MachLampWorking", new Color(1.00f, 0.72f, 0.12f), 0.5f);
            lampDone = FarmVisualFactory.GetSharedMaterial("MachLampDone", new Color(0.20f, 0.95f, 0.40f), 0.5f);
            GameObject lamp = FarmVisualFactory.CreatePart(PrimitiveType.Sphere, fxRoot, "StatusIndicator",
                lampLocal, new Vector3(0.38f, 0.38f, 0.38f), lampIdle, default, false);
            lampRenderer = lamp.GetComponent<Renderer>();

            GameObject mk = FarmVisualFactory.CreatePart(PrimitiveType.Sphere, fxRoot, "ProductMarker",
                new Vector3(b.center.x, 8.4f, b.center.z), new Vector3(0.9f, 0.9f, 0.9f),
                FarmVisualFactory.GetSharedMaterial("MachMarker_default", new Color(1f, 0.85f, 0.25f), 0.5f), default, false);
            marker = mk.transform;
            markerRenderer = mk.GetComponent<Renderer>();
            marker.gameObject.SetActive(false);

            shakeTarget = proc != null ? proc : visual;
            if (shakeTarget != null)
            {
                shakeBasePos = shakeTarget.localPosition;
                shakeBaseScale = shakeTarget.localScale;
            }

            CollectSpinners();
        }

        private void CollectSpinners()
        {
            spinnersCollected = true;
            var list = new System.Collections.Generic.List<Transform>();
            var axes = new System.Collections.Generic.List<Vector3>();
            var glows = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in machine.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == machine.transform) continue;
                string n = t.name;
                if (n.StartsWith("SpinX_")) { list.Add(t); axes.Add(Vector3.right); }
                else if (n.StartsWith("SpinY_")) { list.Add(t); axes.Add(Vector3.up); }
                else if (n.StartsWith("SpinZ_")) { list.Add(t); axes.Add(Vector3.forward); }
                else if (n.StartsWith("Glow_")) { glows.Add(t.gameObject); }
            }
            spinners = list.ToArray();
            spinAxes = axes.ToArray();
            glowParts = glows.ToArray();
        }

        private Transform FindAnchor(string anchorName)
        {
            foreach (Transform t in machine.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == anchorName) return t;
            }
            return null;
        }

        private Bounds ComputeLocalBounds()
        {
            Bounds result = new Bounds(new Vector3(0f, 2.5f, 0f), new Vector3(4f, 5f, 4f));
            bool any = false;
            foreach (Renderer r in machine.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.GetComponent<TextMesh>() != null) continue;
                if (fxRoot != null && r.transform.IsChildOf(fxRoot)) continue;
                Bounds wb = r.bounds;
                Vector3 lo = machine.transform.InverseTransformPoint(wb.min);
                Vector3 hi = machine.transform.InverseTransformPoint(wb.max);
                Bounds lb = new Bounds((lo + hi) * 0.5f, new Vector3(Mathf.Abs(hi.x - lo.x), Mathf.Abs(hi.y - lo.y), Mathf.Abs(hi.z - lo.z)));
                if (!any) { result = lb; any = true; }
                else result.Encapsulate(lb);
            }
            return result;
        }

        // --------------------------------------------------------------- animate
        private void AnimateBody(bool producing)
        {
            if (shakeTarget == null) return;
            float amp = workBlend;
            float jitter = Mathf.Sin(phase * 31f) * 0.012f * amp;
            float pulse = 1f + Mathf.Sin(phase * 9f) * 0.012f * amp;
            shakeTarget.localPosition = shakeBasePos + new Vector3(jitter, 0f, Mathf.Cos(phase * 27f) * 0.008f * amp);
            shakeTarget.localScale = new Vector3(shakeBaseScale.x * pulse, shakeBaseScale.y * (2f - pulse), shakeBaseScale.z * pulse);
        }

        private void RestoreShake()
        {
            if (shakeTarget == null) return;
            shakeTarget.localPosition = shakeBasePos;
            shakeTarget.localScale = shakeBaseScale;
        }

        private void AnimateSpinners()
        {
            if (!spinnersCollected || spinners == null) return;
            float speed = 220f * workBlend;
            if (speed < 0.01f) return;
            for (int i = 0; i < spinners.Length; i++)
            {
                if (spinners[i] == null) continue;
                spinners[i].Rotate(spinAxes[i], speed * Time.deltaTime, Space.Self);
            }
        }

        private void AnimateGlow(bool producing)
        {
            if (glowParts == null) return;
            bool on = workBlend > 0.05f;
            for (int i = 0; i < glowParts.Length; i++)
            {
                if (glowParts[i] == null) continue;
                if (glowParts[i].activeSelf != on) glowParts[i].SetActive(on);
                if (on)
                {
                    float flicker = 0.85f + Mathf.PerlinNoise(phase * 6f, i * 3.1f) * 0.3f;
                    Vector3 s = glowParts[i].transform.localScale;
                    glowParts[i].transform.localScale = new Vector3(s.x, 0.7f * flicker, s.z);
                }
            }
        }

        private void AnimatePuffs(bool producing)
        {
            if (puffs == null) return;
            for (int i = 0; i < puffs.Length; i++)
            {
                Transform p = puffs[i];
                if (p == null) continue;

                if (workBlend < 0.05f)
                {
                    if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
                    continue;
                }

                float t = ((phase / PuffLifetime) + i / (float)puffs.Length) % 1f;
                float size = Mathf.Sin(t * Mathf.PI) * (0.55f + t * 0.5f) * workBlend;
                float rise = t * 2.2f;
                float sway = Mathf.Sin((t * 4f) + i) * 0.25f * t;

                if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                p.localPosition = new Vector3(steamOrigin.x + sway, steamOrigin.y + rise, steamOrigin.z);
                p.localScale = Vector3.one * size;
            }
        }

        private void AnimateLamp(bool producing, bool completed)
        {
            if (lampRenderer == null) return;
            Material m = lampIdle;
            float s = 0.38f;
            if (producing)
            {
                m = lampWorking;
                s = 0.38f + Mathf.Abs(Mathf.Sin(phase * 5f)) * 0.14f;
            }
            else if (completed)
            {
                m = lampDone;
                s = 0.42f + Mathf.Abs(Mathf.Sin(phase * 3f)) * 0.1f;
            }
            if (lampRenderer.sharedMaterial != m) lampRenderer.sharedMaterial = m;
            lampRenderer.transform.localScale = Vector3.one * s;
        }

        private void AnimateMarker(bool completed)
        {
            if (marker == null) return;
            if (!completed)
            {
                if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
                return;
            }

            string outId = machine.currentRecipe != null ? machine.currentRecipe.outputItemId : "";
            if (outId != markerItemId)
            {
                markerItemId = outId;
                Color c = MachineVisualFactory.GetProductColor(outId);
                markerRenderer.sharedMaterial = FarmVisualFactory.GetSharedMaterial("MachMarker_" + outId, c, 0.5f);
            }

            if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            Vector3 p = marker.localPosition;
            p.y = 8.4f + Mathf.Sin(phase * 3f) * 0.3f;
            marker.localPosition = p;
            marker.Rotate(Vector3.up, 90f * Time.deltaTime, Space.Self);
            float pop = 0.9f + Mathf.Abs(Mathf.Sin(phase * 3f)) * 0.2f;
            marker.localScale = Vector3.one * pop;
        }

        private void SetFxVisible(bool visible)
        {
            if (fxRoot != null && fxRoot.gameObject.activeSelf != visible) fxRoot.gameObject.SetActive(visible);
        }

        private void DestroyFx()
        {
            RestoreShake();
            if (fxRoot != null)
            {
                if (Application.isPlaying) Destroy(fxRoot.gameObject);
                else DestroyImmediate(fxRoot.gameObject);
            }
            fxRoot = null;
            puffs = null;
            marker = null;
            lampRenderer = null;
        }
    }
}
