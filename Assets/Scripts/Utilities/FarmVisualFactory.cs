using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gentle wind sway component for crops and environment foliage.
/// Rotates the transform slightly around its base without moving its root position.
/// </summary>
public class CropWindSway : MonoBehaviour
{
    public float swayAngle = 3.2f;
    public float swaySpeed = 1.6f;
    public float secondaryAngle = 1.4f;

    private float phaseOffset;
    private Quaternion baseLocalRotation;
    private bool initialized;

    private void Awake()
    {
        InitIfNeeded();
    }

    public void InitIfNeeded()
    {
        if (initialized) return;
        initialized = true;
        baseLocalRotation = transform.localRotation;
        Vector3 pos = transform.position;
        phaseOffset = (pos.x * 0.73f + pos.z * 1.17f) % (Mathf.PI * 2f);
    }

    public void ResetBaseRotation(Quaternion rot)
    {
        baseLocalRotation = rot;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) InitIfNeeded();
        float t = Time.time * swaySpeed + phaseOffset;
        float pitch = Mathf.Sin(t) * swayAngle + Mathf.Sin(t * 2.1f) * (swayAngle * 0.25f);
        float roll = Mathf.Cos(t * 0.85f + 0.6f) * secondaryAngle;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
    }
}

/// <summary>
/// Centralized factory for shared farm materials, soft blob shadows,
/// articulated low-poly 3D animal rigs (Horse, Pig, Chicken, Cow enhancer),
/// and 5-stage 3D crop models for all 10 crop types.
/// Eliminates duplicate material instances and unnecessary colliders.
/// </summary>
public static class FarmVisualFactory
{
    private static readonly Dictionary<string, Material> SharedMaterials = new Dictionary<string, Material>();
    private static Material cachedShadowMaterial;
    private static Texture2D cachedShadowTexture;

    public static Material GetSharedMaterial(string key, Color color, float smoothness = 0.12f, float metallic = 0.0f)
    {
        string fullKey = $"{key}_{ColorUtility.ToHtmlStringRGBA(color)}_{smoothness:F2}";
        if (SharedMaterials.TryGetValue(fullKey, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
        mat = new Material(sh)
        {
            name = "Shared_" + key,
            color = color
        };
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        mat.enableInstancing = true;
        SharedMaterials[fullKey] = mat;
        return mat;
    }

    public static Material GetBlobShadowMaterial()
    {
        if (cachedShadowMaterial != null) return cachedShadowMaterial;

        if (cachedShadowTexture == null)
        {
            int size = 64;
            cachedShadowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SoftBlobShadowTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.15f, 0.96f, dist)) * 0.36f;
                    cachedShadowTexture.SetPixel(x, y, new Color(0.06f, 0.08f, 0.05f, alpha));
                }
            }
            cachedShadowTexture.Apply();
        }

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        if (sh == null) sh = Shader.Find("Standard");

        cachedShadowMaterial = new Material(sh)
        {
            name = "Shared_BlobShadowMat",
            mainTexture = cachedShadowTexture,
            color = Color.white
        };
        return cachedShadowMaterial;
    }

    public static GameObject EnsureBlobShadow(Transform parent, float width, float length, float yLocal = 0.008f)
    {
        if (parent == null) return null;
        Transform existing = parent.Find("BlobShadow");
        GameObject shadowGo;
        if (existing != null)
        {
            shadowGo = existing.gameObject;
        }
        else
        {
            shadowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadowGo.name = "BlobShadow";
            Collider col = shadowGo.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            shadowGo.transform.SetParent(parent, false);
        }

        shadowGo.transform.localPosition = new Vector3(0f, yLocal, 0f);
        shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadowGo.transform.localScale = new Vector3(width, length, 1f);

        Renderer r = shadowGo.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = GetBlobShadowMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return shadowGo;
    }

    public static Transform CreatePivot(Transform parent, string name, Vector3 localPos, Vector3 localEuler = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    public static GameObject CreatePart(
        PrimitiveType type,
        Transform parent,
        string name,
        Vector3 localPos,
        Vector3 localScale,
        Material mat,
        Vector3 localEuler = default,
        bool castShadows = true)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
        return go;
    }

    public static void ClearVisualChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (Application.isPlaying) Object.Destroy(child.gameObject);
            else Object.DestroyImmediate(child.gameObject);
        }
    }

    // =========================================================================
    // 1. HORSE MODEL BUILDER (Realistic Equine Articulated Proportions)
    // =========================================================================
    /// <summary>
    /// Builds a realistic low-poly articulated Horse model on root.
    /// Features:
    /// - Sits distinctly larger and taller than the Cow (withers ~1.62m, poll/head ~2.25m, length ~2.55m)
    /// - Elongated athletic barrel/torso, broad muscular chest, defined withers, and muscular croup
    /// - 4 long, slender articulated equine legs (Shoulder/Hip -> Knee/Hock -> Cannon -> Fetlock -> Pastern -> Hoof flush at y=0)
    /// - Arched, proud equine neck with 8-segment flowing mane and forelock
    /// - Sculpted head with cheekbones, white facial blaze, tapered muzzle, nostrils, jaw, and alert pointed ears
    /// - 3-segment flowing tail extending gracefully behind
    /// - Synchronized blob shadow at y = 0.008f
    /// </summary>
    public static void BuildHorseVisuals(GameObject root)
    {
        if (root == null) return;
        ClearVisualChildren(root.transform);

        Material coatMat     = GetSharedMaterial("HorseCoat",     new Color(0.53f, 0.31f, 0.16f), 0.16f);
        Material coatDarkMat = GetSharedMaterial("HorseCoatDark", new Color(0.38f, 0.20f, 0.10f), 0.14f);
        Material bellyMat    = GetSharedMaterial("HorseBelly",    new Color(0.58f, 0.36f, 0.20f), 0.14f);
        Material maneMat     = GetSharedMaterial("HorseMane",     new Color(0.12f, 0.08f, 0.05f), 0.10f);
        Material muzzleMat   = GetSharedMaterial("HorseMuzzle",   new Color(0.24f, 0.17f, 0.13f), 0.12f);
        Material sockMat     = GetSharedMaterial("HorseSock",     new Color(0.92f, 0.90f, 0.85f), 0.10f);
        Material hoofMat     = GetSharedMaterial("HorseHoof",     new Color(0.12f, 0.10f, 0.09f), 0.22f);
        Material blazeMat    = GetSharedMaterial("HorseBlaze",    new Color(0.96f, 0.94f, 0.90f), 0.10f);
        Material eyeMat      = GetSharedMaterial("AnimalEye",     new Color(0.08f, 0.07f, 0.07f), 0.45f);
        Material eyeShineMat = GetSharedMaterial("AnimalEyeWh",   new Color(0.95f, 0.95f, 0.95f), 0.30f);

        EnsureBlobShadow(root.transform, 1.25f, 2.45f, 0.008f);

        // --- Torso / Body Root (elevated at y = 1.25f for majestic, long-legged horse stature) ---
        Transform bodyPivot = CreatePivot(root.transform, "HorseBody", new Vector3(0f, 1.25f, 0f));

        // Elongated athletic barrel
        CreatePart(PrimitiveType.Capsule, bodyPivot, "HorseBarrel",
            new Vector3(0f, 0.0f, 0.0f), new Vector3(0.56f, 0.82f, 0.58f), coatMat, new Vector3(90f, 0f, 0f));
        // Underbelly & ribcage curve
        CreatePart(PrimitiveType.Sphere, bodyPivot, "HorseRibcage",
            new Vector3(0f, -0.06f, 0.10f), new Vector3(0.54f, 0.50f, 0.90f), bellyMat);
        // Broad, muscular chest (pectorals)
        CreatePart(PrimitiveType.Sphere, bodyPivot, "HorseChest",
            new Vector3(0f, -0.02f, 0.58f), new Vector3(0.55f, 0.54f, 0.52f), coatMat);
        // Prominent withers (base of neck crest, height ~1.62m above ground)
        CreatePart(PrimitiveType.Sphere, bodyPivot, "HorseWithers",
            new Vector3(0f, 0.20f, 0.44f), new Vector3(0.42f, 0.35f, 0.50f), coatDarkMat);
        // Rounded, muscular croup & hindquarters
        CreatePart(PrimitiveType.Sphere, bodyPivot, "HorseRump",
            new Vector3(0f, 0.06f, -0.58f), new Vector3(0.58f, 0.55f, 0.56f), coatMat);

        // --- Arched Neck & Head Hierarchy (pivots cleanly at withers for grazing & head turning) ---
        Transform neckPivot = CreatePivot(bodyPivot, "HorseNeck", new Vector3(0f, 0.14f, 0.58f), new Vector3(36f, 0f, 0f));

        // Muscular tapering neck segments
        CreatePart(PrimitiveType.Capsule, neckPivot, "NeckLower",
            new Vector3(0f, 0.26f, 0.03f), new Vector3(0.34f, 0.32f, 0.40f), coatMat);
        CreatePart(PrimitiveType.Capsule, neckPivot, "NeckUpper",
            new Vector3(0f, 0.52f, 0.04f), new Vector3(0.28f, 0.28f, 0.32f), coatDarkMat);

        // Prominent 8-segment mane flowing along the dorsal crest
        for (int m = 0; m < 8; m++)
        {
            float t = m / 7f;
            float my = Mathf.Lerp(0.08f, 0.65f, t);
            float mz = Mathf.Lerp(-0.16f, -0.11f, t);
            CreatePart(PrimitiveType.Cube, neckPivot, $"HorseMane_{m + 1}",
                new Vector3(0f, my, mz), new Vector3(0.08f, 0.14f, 0.16f), maneMat, new Vector3(-14f, 0f, 0f));
        }

        // Head pivot at poll (top of neck, height reaches ~2.25m!)
        Transform headPivot = CreatePivot(neckPivot, "HorseHead", new Vector3(0f, 0.70f, 0.08f), new Vector3(22f, 0f, 0f));

        // Cranium / jowls & cheeks
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseJowl",
            new Vector3(0f, -0.02f, 0.03f), new Vector3(0.28f, 0.28f, 0.30f), coatMat);
        CreatePart(PrimitiveType.Cube, headPivot, "HorseBridge",
            new Vector3(0f, -0.04f, 0.22f), new Vector3(0.20f, 0.19f, 0.34f), coatMat, new Vector3(8f, 0f, 0f));

        // White facial blaze stripe
        CreatePart(PrimitiveType.Cube, headPivot, "HorseBlaze",
            new Vector3(0f, 0.055f, 0.23f), new Vector3(0.065f, 0.03f, 0.30f), blazeMat, new Vector3(8f, 0f, 0f));

        // Tapered equine muzzle & nose tip
        CreatePart(PrimitiveType.Cube, headPivot, "HorseMuzzle",
            new Vector3(0f, -0.075f, 0.41f), new Vector3(0.165f, 0.155f, 0.20f), muzzleMat, new Vector3(6f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseNoseTip",
            new Vector3(0f, -0.07f, 0.50f), new Vector3(0.16f, 0.14f, 0.13f), muzzleMat);
        CreatePart(PrimitiveType.Cube, headPivot, "HorseJaw",
            new Vector3(0f, -0.14f, 0.31f), new Vector3(0.135f, 0.07f, 0.24f), coatDarkMat, new Vector3(4f, 0f, 0f));

        // Flared dark nostrils
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseNostril_L",
            new Vector3(-0.068f, -0.045f, 0.485f), new Vector3(0.038f, 0.045f, 0.055f), hoofMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseNostril_R",
            new Vector3(0.068f, -0.045f, 0.485f), new Vector3(0.038f, 0.045f, 0.055f), hoofMat);

        // Glossy dark equine eyes with catchlights
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseEye_L",
            new Vector3(-0.13f, 0.03f, 0.12f), new Vector3(0.052f, 0.052f, 0.058f), eyeMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseEye_R",
            new Vector3(0.13f, 0.03f, 0.12f), new Vector3(0.052f, 0.052f, 0.058f), eyeMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseEyeShine_L",
            new Vector3(-0.15f, 0.04f, 0.13f), new Vector3(0.018f, 0.018f, 0.018f), eyeShineMat, default, false);
        CreatePart(PrimitiveType.Sphere, headPivot, "HorseEyeShine_R",
            new Vector3(0.15f, 0.04f, 0.13f), new Vector3(0.018f, 0.018f, 0.018f), eyeShineMat, default, false);

        // Forelock tuft between ears
        CreatePart(PrimitiveType.Cube, headPivot, "HorseForelock",
            new Vector3(0f, 0.11f, 0.09f), new Vector3(0.08f, 0.08f, 0.15f), maneMat, new Vector3(22f, 0f, 0f));

        // Alert pointed equine ears
        BuildHorseEar(headPivot, "HorseEar_L", new Vector3(-0.095f, 0.12f, -0.02f), new Vector3(-18f, -12f, 16f), coatDarkMat, muzzleMat);
        BuildHorseEar(headPivot, "HorseEar_R", new Vector3(0.095f, 0.12f, -0.02f), new Vector3(-18f, 12f, -16f), coatDarkMat, muzzleMat);

        // --- Multi-Segment Flowing Tail Hierarchy ---
        Transform tailRoot = CreatePivot(bodyPivot, "HorseTailRoot", new Vector3(0f, 0.10f, -0.78f), new Vector3(22f, 0f, 0f));
        CreatePart(PrimitiveType.Capsule, tailRoot, "TailDock",
            new Vector3(0f, -0.14f, -0.04f), new Vector3(0.12f, 0.17f, 0.13f), maneMat, new Vector3(14f, 0f, 0f));

        Transform tailMid = CreatePivot(tailRoot, "HorseTailMid", new Vector3(0f, -0.28f, -0.08f));
        CreatePart(PrimitiveType.Capsule, tailMid, "TailPlumeMid",
            new Vector3(0f, -0.20f, -0.02f), new Vector3(0.15f, 0.26f, 0.16f), maneMat, new Vector3(6f, 0f, 0f));

        Transform tailTip = CreatePivot(tailMid, "HorseTailTip", new Vector3(0f, -0.40f, -0.04f));
        CreatePart(PrimitiveType.Capsule, tailTip, "TailPlumeTip",
            new Vector3(0f, -0.18f, -0.01f), new Vector3(0.11f, 0.22f, 0.12f), maneMat);

        // --- 4 Long, Slender Articulated Legs (Leg root at y = 1.15f, Hoof bottom flush at y = 0.00f!) ---
        BuildHorseLeg(root.transform, "FL", new Vector3(-0.20f, 1.15f,  0.50f), isFront: true,  coatMat, coatDarkMat, sockMat, hoofMat);
        BuildHorseLeg(root.transform, "FR", new Vector3( 0.20f, 1.15f,  0.50f), isFront: true,  coatMat, coatDarkMat, sockMat, hoofMat);
        BuildHorseLeg(root.transform, "BL", new Vector3(-0.21f, 1.15f, -0.55f), isFront: false, coatMat, coatDarkMat, sockMat, hoofMat);
        BuildHorseLeg(root.transform, "BR", new Vector3( 0.21f, 1.15f, -0.55f), isFront: false, coatMat, coatDarkMat, sockMat, hoofMat);
    }

    private static void BuildHorseEar(Transform headPivot, string name, Vector3 localPos, Vector3 baseEuler, Material outerMat, Material innerMat)
    {
        Transform earPivot = CreatePivot(headPivot, name, localPos, baseEuler);
        CreatePart(PrimitiveType.Capsule, earPivot, "EarBase",
            new Vector3(0f, 0.055f, 0f), new Vector3(0.046f, 0.060f, 0.034f), outerMat);
        CreatePart(PrimitiveType.Capsule, earPivot, "EarTip",
            new Vector3(0f, 0.115f, 0.005f), new Vector3(0.030f, 0.044f, 0.024f), outerMat, new Vector3(8f, 0f, 0f));
        CreatePart(PrimitiveType.Cube, earPivot, "EarInner",
            new Vector3(0f, 0.070f, 0.015f), new Vector3(0.024f, 0.080f, 0.010f), innerMat, default, false);
    }

    private static void BuildHorseLeg(
        Transform root,
        string tag,
        Vector3 shoulderOrHipPos,
        bool isFront,
        Material coatMat,
        Material coatDarkMat,
        Material sockMat,
        Material hoofMat)
    {
        // Root leg pivot at y = 1.15f
        Transform legPivot = CreatePivot(root, $"HorseLeg_{tag}", shoulderOrHipPos);

        if (isFront)
        {
            // Muscular shoulder & forearm down to knee at y = -0.55f
            CreatePart(PrimitiveType.Sphere, legPivot, $"Shoulder_{tag}",
                new Vector3(0f, 0.03f, 0.01f), new Vector3(0.18f, 0.23f, 0.20f), coatMat);
            CreatePart(PrimitiveType.Capsule, legPivot, $"UpperLeg_{tag}",
                new Vector3(0f, -0.27f, 0f), new Vector3(0.13f, 0.28f, 0.14f), coatMat);
        }
        else
        {
            // Muscular gaskin & thigh down to hock at y = -0.55f
            CreatePart(PrimitiveType.Sphere, legPivot, $"Thigh_{tag}",
                new Vector3(0f, 0.05f, -0.01f), new Vector3(0.22f, 0.28f, 0.26f), coatMat);
            CreatePart(PrimitiveType.Capsule, legPivot, $"UpperLeg_{tag}",
                new Vector3(0f, -0.27f, -0.02f), new Vector3(0.14f, 0.28f, 0.16f), coatMat, new Vector3(-5f, 0f, 0f));
        }

        // Knee / Hock joint at y = -0.55f (0.60m above ground)
        Transform kneePivot = CreatePivot(legPivot, $"HorseKnee_{tag}", new Vector3(0f, -0.55f, isFront ? 0f : -0.025f));
        CreatePart(PrimitiveType.Sphere, kneePivot, $"Joint_{tag}",
            Vector3.zero, new Vector3(0.11f, 0.11f, 0.12f), coatDarkMat);

        // Slender cannon bone (down to y = -0.36f relative to knee)
        CreatePart(PrimitiveType.Cylinder, kneePivot, $"Cannon_{tag}",
            new Vector3(0f, -0.19f, 0f), new Vector3(0.080f, 0.19f, 0.085f), coatDarkMat);

        // White sock marking on lower cannon
        CreatePart(PrimitiveType.Cylinder, kneePivot, $"Sock_{tag}",
            new Vector3(0f, -0.34f, 0f), new Vector3(0.084f, 0.08f, 0.088f), sockMat);

        // Fetlock joint & angled pastern
        CreatePart(PrimitiveType.Sphere, kneePivot, $"Fetlock_{tag}",
            new Vector3(0f, -0.42f, -0.006f), new Vector3(0.095f, 0.090f, 0.10f), sockMat);
        CreatePart(PrimitiveType.Cylinder, kneePivot, $"Pastern_{tag}",
            new Vector3(0f, -0.48f, 0.016f), new Vector3(0.078f, 0.050f, 0.082f), sockMat, new Vector3(-16f, 0f, 0f));

        // Angled hoof: height 0.09m, center at y = -0.555f -> bottom sits flush at -0.55f - 0.60f = -1.15f (y = 0.00f world!)
        GameObject hoof = CreatePart(PrimitiveType.Cylinder, kneePivot, $"HorseHoof_{tag}",
            new Vector3(0f, -0.555f, 0.032f), new Vector3(0.11f, 0.045f, 0.125f), hoofMat);
        // Beveled front toe
        CreatePart(PrimitiveType.Cube, hoof.transform, $"HoofToe_{tag}",
            new Vector3(0f, -0.22f, 0.22f), new Vector3(0.82f, 0.55f, 0.65f), hoofMat, new Vector3(18f, 0f, 0f));
    }

    // =========================================================================
    // 2. PIG MODEL BUILDER (Proportioned Articulated Adult Pig)
    // =========================================================================
    public static void BuildPigVisuals(GameObject root)
    {
        if (root == null) return;
        ClearVisualChildren(root.transform);

        Material skinMat    = GetSharedMaterial("PigSkin",    new Color(0.95f, 0.69f, 0.67f), 0.14f);
        Material snoutMat   = GetSharedMaterial("PigSnout",   new Color(0.89f, 0.54f, 0.54f), 0.18f);
        Material nostrilMat = GetSharedMaterial("PigNostril", new Color(0.42f, 0.20f, 0.22f), 0.10f);
        Material earMat     = GetSharedMaterial("PigEar",     new Color(0.91f, 0.60f, 0.59f), 0.14f);
        Material hoofMat    = GetSharedMaterial("PigHoof",    new Color(0.28f, 0.20f, 0.17f), 0.20f);
        Material eyeMat     = GetSharedMaterial("AnimalEye",  new Color(0.08f, 0.07f, 0.07f), 0.45f);

        EnsureBlobShadow(root.transform, 0.95f, 1.45f, 0.008f);

        // Robust barrel body at y = 0.50f (adult pig proportions)
        Transform bodyPivot = CreatePivot(root.transform, "PigBody", new Vector3(0f, 0.50f, 0f));
        CreatePart(PrimitiveType.Capsule, bodyPivot, "PigTorso",
            Vector3.zero, new Vector3(0.58f, 0.52f, 0.56f), skinMat, new Vector3(90f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, bodyPivot, "PigBelly",
            new Vector3(0f, -0.05f, 0f), new Vector3(0.60f, 0.52f, 0.76f), skinMat);

        // Articulated Head Pivot at front of body
        Transform headPivot = CreatePivot(bodyPivot, "PigHead", new Vector3(0f, 0.07f, 0.45f));
        CreatePart(PrimitiveType.Sphere, headPivot, "HeadMesh",
            new Vector3(0f, 0f, 0.08f), new Vector3(0.42f, 0.40f, 0.42f), skinMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "JowlMesh",
            new Vector3(0f, -0.07f, 0.06f), new Vector3(0.38f, 0.28f, 0.34f), skinMat);

        // Flat round pig snout with nostrils
        CreatePart(PrimitiveType.Cylinder, headPivot, "PigSnout",
            new Vector3(0f, -0.04f, 0.29f), new Vector3(0.185f, 0.06f, 0.16f), snoutMat, new Vector3(90f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, headPivot, "Nostril_L",
            new Vector3(-0.04f, -0.04f, 0.35f), new Vector3(0.032f, 0.044f, 0.025f), nostrilMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "Nostril_R",
            new Vector3(0.04f, -0.04f, 0.35f), new Vector3(0.032f, 0.044f, 0.025f), nostrilMat);

        // Eyes
        CreatePart(PrimitiveType.Sphere, headPivot, "PigEye_L",
            new Vector3(-0.145f, 0.06f, 0.19f), new Vector3(0.048f, 0.048f, 0.048f), eyeMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "PigEye_R",
            new Vector3(0.145f, 0.06f, 0.19f), new Vector3(0.048f, 0.048f, 0.048f), eyeMat);

        // Floppy forward-tilted ears
        Transform earL = CreatePivot(headPivot, "PigEar_L", new Vector3(-0.15f, 0.15f, 0.07f), new Vector3(26f, -15f, 34f));
        CreatePart(PrimitiveType.Cube, earL, "EarFlap_L",
            new Vector3(-0.03f, 0.05f, 0.03f), new Vector3(0.12f, 0.14f, 0.038f), earMat);
        Transform earR = CreatePivot(headPivot, "PigEar_R", new Vector3(0.15f, 0.15f, 0.07f), new Vector3(26f, 15f, -34f));
        CreatePart(PrimitiveType.Cube, earR, "EarFlap_R",
            new Vector3(0.03f, 0.05f, 0.03f), new Vector3(0.12f, 0.14f, 0.038f), earMat);

        // Curly tail at rear
        Transform tailPivot = CreatePivot(bodyPivot, "PigTail", new Vector3(0f, 0.10f, -0.50f));
        CreatePart(PrimitiveType.Capsule, tailPivot, "TailSeg1",
            new Vector3(0f, 0.03f, -0.06f), new Vector3(0.048f, 0.07f, 0.048f), earMat, new Vector3(45f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, tailPivot, "TailCurl",
            new Vector3(0.02f, 0.075f, -0.085f), new Vector3(0.075f, 0.075f, 0.062f), earMat);

        // 4 Articulated Legs (hip at y = 0.32f, trotters flush at y = 0.00f)
        Vector3[] legPositions =
        {
            new Vector3(-0.175f, 0.32f,  0.28f),
            new Vector3( 0.175f, 0.32f,  0.28f),
            new Vector3(-0.175f, 0.32f, -0.28f),
            new Vector3( 0.175f, 0.32f, -0.28f)
        };
        for (int i = 0; i < 4; i++)
        {
            Transform leg = CreatePivot(root.transform, $"PigLeg_{i + 1}", legPositions[i]);
            CreatePart(PrimitiveType.Capsule, leg, $"Upper_{i + 1}",
                new Vector3(0f, -0.13f, 0f), new Vector3(0.13f, 0.13f, 0.13f), skinMat);
            // Trotters height 0.076m, center at -0.281m -> bottom at -0.32m (y = 0.00m world!)
            CreatePart(PrimitiveType.Cylinder, leg, $"Hoof_{i + 1}",
                new Vector3(0f, -0.281f, 0.01f), new Vector3(0.105f, 0.038f, 0.115f), hoofMat);
        }
    }

    // =========================================================================
    // 3. CHICKEN MODEL BUILDER (Proportioned Articulated Adult Hen)
    // =========================================================================
    public static void BuildChickenVisuals(GameObject root)
    {
        if (root == null) return;
        ClearVisualChildren(root.transform);

        Material featherMat     = GetSharedMaterial("ChickenFeather",     new Color(0.96f, 0.94f, 0.89f), 0.10f);
        Material wingFeatherMat = GetSharedMaterial("ChickenWingFeather", new Color(0.91f, 0.87f, 0.79f), 0.10f);
        Material combMat        = GetSharedMaterial("ChickenComb",        new Color(0.86f, 0.16f, 0.16f), 0.22f);
        Material beakMat        = GetSharedMaterial("ChickenBeak",        new Color(0.95f, 0.68f, 0.14f), 0.25f);
        Material legMat         = GetSharedMaterial("ChickenLeg",         new Color(0.92f, 0.62f, 0.14f), 0.20f);
        Material eyeMat         = GetSharedMaterial("AnimalEye",          new Color(0.08f, 0.07f, 0.07f), 0.45f);

        EnsureBlobShadow(root.transform, 0.48f, 0.58f, 0.008f);

        // Plump hen body at y = 0.32f (well-proportioned adult hen)
        Transform bodyPivot = CreatePivot(root.transform, "ChickenBody", new Vector3(0f, 0.32f, 0f));
        CreatePart(PrimitiveType.Sphere, bodyPivot, "Torso",
            Vector3.zero, new Vector3(0.34f, 0.31f, 0.41f), featherMat);
        CreatePart(PrimitiveType.Sphere, bodyPivot, "Breast",
            new Vector3(0f, -0.015f, 0.11f), new Vector3(0.31f, 0.27f, 0.28f), featherMat);

        // Arched tail feathers
        Transform tailPivot = CreatePivot(bodyPivot, "ChickenTail", new Vector3(0f, 0.07f, -0.18f), new Vector3(-32f, 0f, 0f));
        CreatePart(PrimitiveType.Cube, tailPivot, "TailMain",
            new Vector3(0f, 0.07f, -0.03f), new Vector3(0.11f, 0.17f, 0.10f), wingFeatherMat);
        CreatePart(PrimitiveType.Cube, tailPivot, "TailUpper",
            new Vector3(0f, 0.11f, -0.06f), new Vector3(0.085f, 0.14f, 0.07f), featherMat, new Vector3(-15f, 0f, 0f));

        // Neck & Head pivot
        Transform headPivot = CreatePivot(bodyPivot, "ChickenHead", new Vector3(0f, 0.11f, 0.15f));
        CreatePart(PrimitiveType.Capsule, headPivot, "Neck",
            new Vector3(0f, 0.07f, 0.014f), new Vector3(0.16f, 0.105f, 0.16f), featherMat, new Vector3(14f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, headPivot, "Skull",
            new Vector3(0f, 0.155f, 0.042f), new Vector3(0.175f, 0.168f, 0.182f), featherMat);

        // Beak & red wattle
        CreatePart(PrimitiveType.Cube, headPivot, "ChickenBeak",
            new Vector3(0f, 0.147f, 0.147f), new Vector3(0.058f, 0.045f, 0.09f), beakMat, new Vector3(12f, 0f, 0f));
        CreatePart(PrimitiveType.Sphere, headPivot, "ChickenWattle",
            new Vector3(0f, 0.10f, 0.123f), new Vector3(0.045f, 0.058f, 0.045f), combMat);

        // 3-lobed red comb
        CreatePart(PrimitiveType.Sphere, headPivot, "Comb_1",
            new Vector3(0f, 0.245f, 0.07f), new Vector3(0.036f, 0.062f, 0.052f), combMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "Comb_2",
            new Vector3(0f, 0.255f, 0.03f), new Vector3(0.036f, 0.072f, 0.056f), combMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "Comb_3",
            new Vector3(0f, 0.241f, -0.01f), new Vector3(0.034f, 0.058f, 0.050f), combMat);

        // Eyes
        CreatePart(PrimitiveType.Sphere, headPivot, "Eye_L",
            new Vector3(-0.073f, 0.165f, 0.077f), new Vector3(0.034f, 0.034f, 0.034f), eyeMat);
        CreatePart(PrimitiveType.Sphere, headPivot, "Eye_R",
            new Vector3(0.073f, 0.165f, 0.077f), new Vector3(0.034f, 0.034f, 0.034f), eyeMat);

        // Wings
        Transform wingL = CreatePivot(bodyPivot, "ChickenWing_L", new Vector3(-0.16f, 0.03f, 0.03f));
        CreatePart(PrimitiveType.Sphere, wingL, "WingMesh_L",
            new Vector3(-0.02f, -0.03f, -0.03f), new Vector3(0.06f, 0.18f, 0.26f), wingFeatherMat, new Vector3(8f, 0f, 8f));
        Transform wingR = CreatePivot(bodyPivot, "ChickenWing_R", new Vector3(0.16f, 0.03f, 0.03f));
        CreatePart(PrimitiveType.Sphere, wingR, "WingMesh_R",
            new Vector3(0.02f, -0.03f, -0.03f), new Vector3(0.06f, 0.18f, 0.26f), wingFeatherMat, new Vector3(8f, 0f, -8f));

        // 2 Articulated Legs (hip at y = 0.18f, toes flush at y = 0.00f)
        string[] sides = { "L", "R" };
        float[] xs = { -0.073f, 0.073f };
        for (int i = 0; i < 2; i++)
        {
            Transform leg = CreatePivot(root.transform, $"ChickenLeg_{sides[i]}", new Vector3(xs[i], 0.18f, -0.015f));
            CreatePart(PrimitiveType.Cylinder, leg, $"Shank_{sides[i]}",
                new Vector3(0f, -0.086f, 0f), new Vector3(0.031f, 0.086f, 0.031f), legMat);
            // 3 toes flush on ground (y = -0.170f relative to 0.18f -> bottom at y = 0.00f)
            CreatePart(PrimitiveType.Cube, leg, $"ToeCenter_{sides[i]}",
                new Vector3(0f, -0.170f, 0.035f), new Vector3(0.022f, 0.020f, 0.090f), legMat);
            CreatePart(PrimitiveType.Cube, leg, $"ToeInner_{sides[i]}",
                new Vector3(i == 0 ? 0.021f : -0.021f, -0.170f, 0.028f), new Vector3(0.020f, 0.020f, 0.076f), legMat,
                new Vector3(0f, i == 0 ? 22f : -22f, 0f));
            CreatePart(PrimitiveType.Cube, leg, $"ToeOuter_{sides[i]}",
                new Vector3(i == 0 ? -0.021f : 0.021f, -0.170f, 0.028f), new Vector3(0.020f, 0.020f, 0.076f), legMat,
                new Vector3(0f, i == 0 ? -22f : 22f, 0f));
        }
    }

    // =========================================================================
    // 4. COW VISUAL ENHANCER (Preserves rigged COW.FBX + Adds Soft Shadow & Material Fix)
    // =========================================================================
    public static void EnhanceCowVisuals(GameObject root)
    {
        if (root == null) return;

        // Add soft blob shadow normalized against root scale (since COW.FBX often has scale ~0.18)
        float sx = Mathf.Max(0.01f, Mathf.Abs(root.transform.localScale.x));
        float sy = Mathf.Max(0.01f, Mathf.Abs(root.transform.localScale.y));
        float sz = Mathf.Max(0.01f, Mathf.Abs(root.transform.localScale.z));

        GameObject shadow = EnsureBlobShadow(root.transform, 1.05f / sx, 1.75f / sz, 0.01f / sy);
        if (shadow != null)
        {
            shadow.transform.localScale = new Vector3(1.05f / sx, 1.75f / sz, 1f);
        }

        // Ensure all renderers have clean, non-overbright, shadow-casting materials
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null || r.gameObject.name == "BlobShadow") continue;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;

            Material mat = r.sharedMaterial;
            if (mat == null || mat.shader == null || mat.shader.name.Contains("Error"))
            {
                r.sharedMaterial = GetSharedMaterial("CowFallback", new Color(0.92f, 0.90f, 0.86f), 0.12f);
            }
            else if (mat.HasProperty("_Glossiness") && mat.GetFloat("_Glossiness") > 0.35f)
            {
                mat.SetFloat("_Glossiness", 0.15f);
            }
        }
    }

    // =========================================================================
    // 5. 5-STAGE 3D CROP MODEL BUILDER (10 Crops × 5 Distinct Visual Stages)
    // =========================================================================
    public static readonly string[] StageNames =
    {
        "Stage_0_Seed",
        "Stage_1_Sprout",
        "Stage_2_Young",
        "Stage_3_Mature",
        "Stage_4_HarvestReady"
    };

    /// <summary>
    /// Converts GrowthProgress (0..1) and ready state into a stage index (0..4):
    /// 0: Seed (0.00 - 0.19)
    /// 1: Sprout (0.20 - 0.44)
    /// 2: Young (0.45 - 0.74)
    /// 3: Mature (0.75 - 0.99)
    /// 4: Harvest-ready (1.00 / ReadyToHarvest)
    /// </summary>
    public static int GetCropStageIndex(float progress, bool isReadyToHarvest)
    {
        if (isReadyToHarvest || progress >= 0.995f) return 4;
        if (progress < 0.20f) return 0;
        if (progress < 0.45f) return 1;
        if (progress < 0.75f) return 2;
        return 3;
    }

    /// <summary>
    /// Builds all 5 distinct 3D visual stages for the given cropId inside container.
    /// Positions are flush on the plot surface (y = 0 at soil surface).
    /// </summary>
    public static GameObject[] BuildCropAllStages(Transform container, string cropId)
    {
        if (container == null) return null;
        ClearVisualChildren(container);

        string id = string.IsNullOrEmpty(cropId) ? "wheat" : cropId.Trim().ToLowerInvariant();
        if (id == "grass" || id == "feed") id = "hay";

        GameObject[] stages = new GameObject[5];
        for (int s = 0; s < 5; s++)
        {
            GameObject stageObj = new GameObject(StageNames[s]);
            stageObj.transform.SetParent(container, false);
            stageObj.transform.localPosition = Vector3.zero;
            stageObj.transform.localRotation = Quaternion.identity;
            stageObj.transform.localScale = Vector3.one;

            BuildSingleCropStage(stageObj.transform, id, s);

            // Attach gentle wind sway to stages 1..4 (Sprout -> HarvestReady)
            if (s >= 1)
            {
                CropWindSway sway = stageObj.AddComponent<CropWindSway>();
                sway.swayAngle = Mathf.Lerp(1.5f, 3.6f, s / 4f);
                sway.swaySpeed = 1.55f;
                sway.secondaryAngle = sway.swayAngle * 0.45f;
            }

            stageObj.SetActive(false);
            stages[s] = stageObj;
        }

        return stages;
    }

    // Standard 5 plant cluster offsets on a 2.55m plot so every plot looks full and natural
    private static readonly Vector3[] ClusterOffsets =
    {
        new Vector3(-0.45f, 0f, -0.42f),
        new Vector3( 0.45f, 0f, -0.40f),
        new Vector3( 0.00f, 0f,  0.02f),
        new Vector3(-0.42f, 0f,  0.44f),
        new Vector3( 0.44f, 0f,  0.42f)
    };

    // Organic 6 cluster offsets for dense field crops like wheat/rice and hay/grass
    private static readonly Vector3[] LushOrganicOffsets =
    {
        new Vector3(-0.46f, 0f, -0.42f),
        new Vector3( 0.44f, 0f, -0.38f),
        new Vector3(-0.06f, 0f,  0.04f),
        new Vector3(-0.42f, 0f,  0.44f),
        new Vector3( 0.44f, 0f,  0.42f),
        new Vector3( 0.04f, 0f, -0.38f)
    };

    private static void BuildSingleCropStage(Transform stageRoot, string id, int stage)
    {
        Material soilMoundMat = GetSharedMaterial("CropSoilMound", new Color(0.28f, 0.18f, 0.10f), 0.08f);
        Material sproutMat    = GetSharedMaterial("CropSprout",    new Color(0.42f, 0.72f, 0.26f), 0.14f);
        Material stemGreenMat = GetSharedMaterial("CropStemGreen", new Color(0.30f, 0.58f, 0.20f), 0.12f);
        Material leafGreenMat = GetSharedMaterial("CropLeafGreen", new Color(0.26f, 0.54f, 0.18f), 0.14f);
        Material leafDeepMat  = GetSharedMaterial("CropLeafDeep",  new Color(0.20f, 0.46f, 0.16f), 0.14f);

        // Stage 0: Seed (distinct seed color & shape per crop + small tilled soil mounds)
        if (stage == 0)
        {
            Color seedCol = GetSeedColor(id);
            Material seedMat = GetSharedMaterial("Seed_" + id, seedCol, 0.15f);
            for (int i = 0; i < ClusterOffsets.Length; i++)
            {
                Vector3 pos = ClusterOffsets[i];
                CreatePart(PrimitiveType.Sphere, stageRoot, $"SoilMound_{i}",
                    pos + new Vector3(0f, 0.018f, 0f), new Vector3(0.28f, 0.065f, 0.28f), soilMoundMat, default, false);
                CreatePart(PrimitiveType.Sphere, stageRoot, $"SeedA_{i}",
                    pos + new Vector3(-0.035f, 0.050f, 0.012f), new Vector3(0.065f, 0.044f, 0.082f), seedMat, new Vector3(0f, i * 37f, 0f), false);
                CreatePart(PrimitiveType.Sphere, stageRoot, $"SeedB_{i}",
                    pos + new Vector3(0.040f, 0.048f, -0.024f), new Vector3(0.058f, 0.040f, 0.075f), seedMat, new Vector3(0f, i * 65f + 20f, 0f), false);
            }
            return;
        }

        // Stage 1: Sprout (emerging tender shoots & cotyledon leaves)
        if (stage == 1)
        {
            for (int i = 0; i < ClusterOffsets.Length; i++)
            {
                Vector3 pos = ClusterOffsets[i];
                float yaw = i * 52f;
                Transform p = CreatePivot(stageRoot, $"Sprout_{i}", pos, new Vector3(0f, yaw, 0f));
                CreatePart(PrimitiveType.Sphere, p, "Mound",
                    new Vector3(0f, 0.018f, 0f), new Vector3(0.26f, 0.06f, 0.26f), soilMoundMat, default, false);
                CreatePart(PrimitiveType.Cylinder, p, "Shoot",
                    new Vector3(0f, 0.090f, 0f), new Vector3(0.034f, 0.090f, 0.034f), sproutMat);
                CreatePart(PrimitiveType.Sphere, p, "Leaf_L",
                    new Vector3(-0.058f, 0.160f, 0f), new Vector3(0.115f, 0.028f, 0.062f), sproutMat, new Vector3(0f, 0f, 26f));
                CreatePart(PrimitiveType.Sphere, p, "Leaf_R",
                    new Vector3(0.058f, 0.160f, 0f), new Vector3(0.115f, 0.028f, 0.062f), sproutMat, new Vector3(0f, 0f, -26f));
            }
            return;
        }

        // Stages 2 (Young), 3 (Mature), and 4 (HarvestReady) are specialized per crop
        switch (id)
        {
            case "wheat":
                BuildWheatStage(stageRoot, stage, stemGreenMat, leafGreenMat);
                break;
            case "corn":
                BuildCornStage(stageRoot, stage, stemGreenMat, leafGreenMat);
                break;
            case "carrot":
                BuildCarrotStage(stageRoot, stage, soilMoundMat, leafGreenMat);
                break;
            case "potato":
                BuildPotatoStage(stageRoot, stage, soilMoundMat, stemGreenMat, leafGreenMat);
                break;
            case "tomato":
                BuildTomatoStage(stageRoot, stage, stemGreenMat, leafDeepMat);
                break;
            case "pumpkin":
                BuildPumpkinStage(stageRoot, stage, stemGreenMat, leafDeepMat);
                break;
            case "strawberry":
                BuildStrawberryStage(stageRoot, stage, leafGreenMat, leafDeepMat);
                break;
            case "cabbage":
                BuildCabbageStage(stageRoot, stage, sproutMat, leafGreenMat, leafDeepMat);
                break;
            case "watermelon":
                BuildWatermelonStage(stageRoot, stage, stemGreenMat, leafDeepMat);
                break;
            default: // "hay" / grass / feed crop
                BuildHayGrassStage(stageRoot, stage, sproutMat, stemGreenMat, leafGreenMat);
                break;
        }
    }

    private static Color GetSeedColor(string id)
    {
        switch (id)
        {
            case "wheat":      return new Color(0.84f, 0.70f, 0.30f);
            case "corn":       return new Color(0.92f, 0.76f, 0.18f);
            case "carrot":     return new Color(0.68f, 0.52f, 0.34f);
            case "potato":     return new Color(0.74f, 0.58f, 0.36f);
            case "tomato":     return new Color(0.86f, 0.78f, 0.52f);
            case "pumpkin":    return new Color(0.90f, 0.86f, 0.72f);
            case "strawberry": return new Color(0.76f, 0.42f, 0.36f);
            case "cabbage":    return new Color(0.38f, 0.34f, 0.28f);
            case "watermelon": return new Color(0.22f, 0.18f, 0.16f);
            default:           return new Color(0.62f, 0.72f, 0.36f);
        }
    }

    // --- 1. WHEAT / RICE (Dense clusters of golden stalks with drooping grain heads and organic distribution) ---
    private static void BuildWheatStage(Transform root, int stage, Material stemGreen, Material leafGreen)
    {
        Material goldStalk = GetSharedMaterial("WheatGoldStalk", new Color(0.84f, 0.69f, 0.25f), 0.12f);
        Material goldEar   = GetSharedMaterial("WheatGoldEar",   new Color(0.93f, 0.76f, 0.22f), 0.16f);
        Material paleEar   = GetSharedMaterial("WheatPaleEar",   new Color(0.62f, 0.74f, 0.30f), 0.14f);

        bool isHarvest = stage == 4;
        bool isMature  = stage >= 3;
        float height   = stage == 2 ? 0.44f : (stage == 3 ? 0.68f : 0.84f);
        Material stalkMat = isHarvest ? goldStalk : (stage == 3 ? paleEar : stemGreen);
        Material earMat   = isHarvest ? goldEar   : paleEar;

        for (int i = 0; i < LushOrganicOffsets.Length; i++)
        {
            Transform clump = CreatePivot(root, $"WheatClump_{i}", LushOrganicOffsets[i]);
            int stalkCount = stage == 2 ? 4 : 5;
            for (int s = 0; s < stalkCount; s++)
            {
                float angle = s * (360f / stalkCount) + i * 29f;
                float tilt = 10f + (s % 2) * 8f;
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.08f, 0f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.08f);
                Transform stalkPivot = CreatePivot(clump, $"Stalk_{s}", offset, new Vector3(tilt, angle, 0f));

                float sHeight = height * (0.88f + (s % 3) * 0.10f);
                CreatePart(PrimitiveType.Cylinder, stalkPivot, "Stem",
                    new Vector3(0f, sHeight * 0.5f, 0f), new Vector3(0.026f, sHeight * 0.5f, 0.026f), stalkMat);

                // Arching wheat blade leaves
                CreatePart(PrimitiveType.Cube, stalkPivot, "Blade_1",
                    new Vector3(0.045f, sHeight * 0.40f, 0f), new Vector3(0.13f, 0.015f, 0.030f), isHarvest ? goldStalk : leafGreen, new Vector3(0f, 0f, -28f));
                CreatePart(PrimitiveType.Cube, stalkPivot, "Blade_2",
                    new Vector3(-0.040f, sHeight * 0.26f, 0.02f), new Vector3(0.11f, 0.014f, 0.028f), isHarvest ? goldStalk : leafGreen, new Vector3(0f, 25f, 26f));

                if (isMature)
                {
                    // Plump drooping wheat ear (bông lúa trĩu hạt) + awns at top
                    CreatePart(PrimitiveType.Capsule, stalkPivot, "WheatEar",
                        new Vector3(0f, sHeight + 0.075f, 0f), new Vector3(0.072f, 0.115f, 0.072f), earMat, new Vector3(12f, 0f, 0f));
                    if (isHarvest)
                    {
                        CreatePart(PrimitiveType.Cylinder, stalkPivot, "WheatAwn",
                            new Vector3(0.015f, sHeight + 0.18f, 0f), new Vector3(0.016f, 0.055f, 0.016f), goldEar, new Vector3(18f, 0f, 0f));
                    }
                }
            }
        }
    }

    // --- 2. CORN (Tall double-stalk bush clumps, broad arching leaves, golden tassels & cobs with silk) ---
    private static void BuildCornStage(Transform root, int stage, Material stemGreen, Material leafGreen)
    {
        Material cobMat    = GetSharedMaterial("CornCobGold",  new Color(0.95f, 0.78f, 0.16f), 0.20f);
        Material silkMat   = GetSharedMaterial("CornSilk",     new Color(0.76f, 0.36f, 0.18f), 0.15f);
        Material tasselMat = GetSharedMaterial("CornTassel",   new Color(0.86f, 0.76f, 0.34f), 0.12f);

        float height = stage == 2 ? 0.60f : (stage == 3 ? 1.05f : 1.38f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"CornPlant_{i}", ClusterOffsets[i], new Vector3(0f, i * 42f, 0f));

            // Main tall corn stalk
            CreatePart(PrimitiveType.Cylinder, plant, "CornStalk",
                new Vector3(0f, height * 0.5f, 0f), new Vector3(0.065f, height * 0.5f, 0.065f), stemGreen);

            // Bushy companion sucker / side tiller shoot so corn forms a full, dense bush
            float tillerH = height * 0.65f;
            Transform tiller = CreatePivot(plant, "CornTiller", new Vector3(0.12f, 0f, -0.07f), new Vector3(0f, 35f, -8f));
            CreatePart(PrimitiveType.Cylinder, tiller, "TillerStalk",
                new Vector3(0f, tillerH * 0.5f, 0f), new Vector3(0.045f, tillerH * 0.5f, 0.045f), stemGreen);
            CreatePart(PrimitiveType.Cube, tiller, "TillerLeaf1",
                new Vector3(0.11f, tillerH * 0.45f, 0f), new Vector3(0.20f, 0.016f, 0.065f), leafGreen, new Vector3(0f, 0f, -24f));
            CreatePart(PrimitiveType.Cube, tiller, "TillerLeaf2",
                new Vector3(-0.09f, tillerH * 0.62f, 0.03f), new Vector3(0.18f, 0.016f, 0.060f), leafGreen, new Vector3(0f, 15f, 26f));

            // Broad arching corn leaves along the main stalk (well-spaced to prevent excessive clipping)
            int leafPairs = stage == 2 ? 2 : (stage == 3 ? 3 : 4);
            for (int l = 0; l < leafPairs; l++)
            {
                float ly = height * Mathf.Lerp(0.22f, 0.82f, (l + 0.5f) / leafPairs);
                float side = (l % 2 == 0) ? 1f : -1f;
                float yaw = l * 38f;
                CreatePart(PrimitiveType.Cube, plant, $"LeafA_{l}",
                    new Vector3(side * 0.14f, ly, 0f), new Vector3(0.28f, 0.018f, 0.075f), leafGreen, new Vector3(0f, yaw, -side * 28f));
                CreatePart(PrimitiveType.Cube, plant, $"LeafB_{l}",
                    new Vector3(0f, ly + 0.05f, side * 0.14f), new Vector3(0.075f, 0.018f, 0.28f), leafGreen, new Vector3(side * 28f, yaw, 0f));
            }

            if (stage >= 3)
            {
                // Full 3-part pollen tassel at top of tall corn stalk
                CreatePart(PrimitiveType.Capsule, plant, "TasselCenter",
                    new Vector3(0f, height + 0.10f, 0f), new Vector3(0.036f, 0.095f, 0.036f), tasselMat);
                CreatePart(PrimitiveType.Capsule, plant, "TasselBranchL",
                    new Vector3(-0.045f, height + 0.08f, 0f), new Vector3(0.026f, 0.070f, 0.026f), tasselMat, new Vector3(0f, 0f, 30f));
                CreatePart(PrimitiveType.Capsule, plant, "TasselBranchR",
                    new Vector3(0.045f, height + 0.08f, 0f), new Vector3(0.026f, 0.070f, 0.026f), tasselMat, new Vector3(0f, 0f, -30f));

                // Side corn ear with husk
                Transform ear1 = CreatePivot(plant, "CornEarPivot1", new Vector3(0.07f, height * 0.52f, 0.02f), new Vector3(0f, 0f, -28f));
                CreatePart(PrimitiveType.Capsule, ear1, "CornHusk",
                    new Vector3(0f, 0.07f, 0f), new Vector3(0.082f, 0.100f, 0.082f), leafGreen);

                if (stage == 4)
                {
                    // Primary golden corn cob + silky reddish-brown corn silk (râu ngô) at tip
                    CreatePart(PrimitiveType.Capsule, ear1, "CornCob",
                        new Vector3(0.015f, 0.11f, 0f), new Vector3(0.075f, 0.090f, 0.075f), cobMat);
                    CreatePart(PrimitiveType.Cylinder, ear1, "CornSilkMain",
                        new Vector3(0.022f, 0.21f, 0f), new Vector3(0.032f, 0.050f, 0.032f), silkMat, new Vector3(0f, 0f, -20f));
                    CreatePart(PrimitiveType.Cylinder, ear1, "CornSilkDroop",
                        new Vector3(0.045f, 0.22f, 0.01f), new Vector3(0.025f, 0.045f, 0.025f), silkMat, new Vector3(14f, 0f, -44f));

                    // Second companion ear on the other side for extra yield richness
                    Transform ear2 = CreatePivot(plant, "CornEarPivot2", new Vector3(-0.06f, height * 0.40f, -0.03f), new Vector3(0f, 140f, -24f));
                    CreatePart(PrimitiveType.Capsule, ear2, "CornHusk2",
                        new Vector3(0f, 0.06f, 0f), new Vector3(0.075f, 0.085f, 0.075f), leafGreen);
                    CreatePart(PrimitiveType.Capsule, ear2, "CornCob2",
                        new Vector3(0.012f, 0.09f, 0f), new Vector3(0.068f, 0.080f, 0.068f), cobMat);
                    CreatePart(PrimitiveType.Cylinder, ear2, "CornSilk2",
                        new Vector3(0.018f, 0.17f, 0f), new Vector3(0.028f, 0.042f, 0.028f), silkMat, new Vector3(0f, 0f, -18f));
                }
            }
        }
    }

    // --- 3. CARROT (Feathery green tops above ground + bright orange root tuber emerging at harvest) ---
    private static void BuildCarrotStage(Transform root, int stage, Material soilMat, Material leafGreen)
    {
        Material carrotOrange = GetSharedMaterial("CarrotOrange", new Color(0.95f, 0.45f, 0.10f), 0.18f);
        float frondHeight = stage == 2 ? 0.26f : (stage == 3 ? 0.38f : 0.46f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"Carrot_{i}", ClusterOffsets[i], new Vector3(0f, i * 38f, 0f));
            CreatePart(PrimitiveType.Sphere, plant, "SoilMound",
                new Vector3(0f, 0.025f, 0f), new Vector3(0.32f, 0.08f, 0.32f), soilMat, default, false);

            // Feathery carrot foliage rosette
            int fronds = stage == 2 ? 4 : 6;
            for (int f = 0; f < fronds; f++)
            {
                float ang = f * (360f / fronds);
                Transform frond = CreatePivot(plant, $"Frond_{f}", new Vector3(0f, stage == 4 ? 0.13f : 0.05f, 0f), new Vector3(22f, ang, 0f));
                CreatePart(PrimitiveType.Cylinder, frond, "Stem",
                    new Vector3(0f, frondHeight * 0.45f, 0f), new Vector3(0.025f, frondHeight * 0.45f, 0.025f), leafGreen);
                CreatePart(PrimitiveType.Sphere, frond, "LeafletTop",
                    new Vector3(0f, frondHeight * 0.85f, 0f), new Vector3(0.115f, 0.095f, 0.055f), leafGreen);
            }

            if (stage == 3)
            {
                // Hint of orange carrot shoulder at soil line
                CreatePart(PrimitiveType.Sphere, plant, "CarrotShoulder",
                    new Vector3(0f, 0.065f, 0f), new Vector3(0.13f, 0.07f, 0.13f), carrotOrange);
            }
            else if (stage == 4)
            {
                // Plump orange carrot root clearly visible above the soil mound when ready to harvest!
                CreatePart(PrimitiveType.Capsule, plant, "CarrotRootUpper",
                    new Vector3(0f, 0.105f, 0f), new Vector3(0.155f, 0.115f, 0.155f), carrotOrange);
                CreatePart(PrimitiveType.Capsule, plant, "CarrotRootTaper",
                    new Vector3(0.07f, 0.065f, 0.05f), new Vector3(0.110f, 0.090f, 0.110f), carrotOrange, new Vector3(22f, 18f, -32f));
            }
        }
    }

    // --- 4. POTATO (Bushy green haulm foliage + golden-brown potato tubers at harvest) ---
    private static void BuildPotatoStage(Transform root, int stage, Material soilMat, Material stemGreen, Material leafGreen)
    {
        Material tuberMat   = GetSharedMaterial("PotatoTuber",   new Color(0.78f, 0.60f, 0.34f), 0.14f);
        Material blossomMat = GetSharedMaterial("PotatoBlossom", new Color(0.95f, 0.94f, 0.98f), 0.20f);
        float bushScale = stage == 2 ? 0.85f : (stage == 3 ? 1.08f : 1.20f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"Potato_{i}", ClusterOffsets[i], new Vector3(0f, i * 47f, 0f));
            CreatePart(PrimitiveType.Sphere, plant, "SoilHill",
                new Vector3(0f, 0.035f, 0f), new Vector3(0.38f, 0.10f, 0.38f), soilMat, default, false);

            // Leafy potato bush stems & compound leaves
            CreatePart(PrimitiveType.Cylinder, plant, "MainStem",
                new Vector3(0f, 0.18f * bushScale, 0f), new Vector3(0.045f, 0.18f * bushScale, 0.045f), stemGreen);
            for (int l = 0; l < 5; l++)
            {
                float a = l * 72f;
                Vector3 dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad));
                CreatePart(PrimitiveType.Sphere, plant, $"LeafCluster_{l}",
                    dir * (0.13f * bushScale) + new Vector3(0f, 0.24f * bushScale, 0f),
                    new Vector3(0.18f * bushScale, 0.10f * bushScale, 0.16f * bushScale), leafGreen);
            }

            if (stage == 3)
            {
                // White potato flowers at mature stage
                CreatePart(PrimitiveType.Sphere, plant, "Flower_1",
                    new Vector3(-0.06f, 0.35f, 0.05f), new Vector3(0.062f, 0.040f, 0.062f), blossomMat);
                CreatePart(PrimitiveType.Sphere, plant, "Flower_2",
                    new Vector3(0.07f, 0.34f, -0.04f), new Vector3(0.058f, 0.040f, 0.058f), blossomMat);
            }
            else if (stage == 4)
            {
                // Harvest-ready golden-brown potato tubers unearthed cleanly on the soil hill
                CreatePart(PrimitiveType.Sphere, plant, "PotatoTuber_1",
                    new Vector3(0.13f, 0.075f, 0.09f), new Vector3(0.155f, 0.110f, 0.125f), tuberMat, new Vector3(8f, 25f, 12f));
                CreatePart(PrimitiveType.Sphere, plant, "PotatoTuber_2",
                    new Vector3(-0.12f, 0.070f, 0.10f), new Vector3(0.135f, 0.100f, 0.115f), tuberMat, new Vector3(-10f, -30f, 6f));
                CreatePart(PrimitiveType.Sphere, plant, "PotatoTuber_3",
                    new Vector3(0.02f, 0.068f, -0.14f), new Vector3(0.140f, 0.105f, 0.120f), tuberMat, new Vector3(5f, 50f, -8f));
            }
        }
    }

    // --- 5. TOMATO (Staked vine bushes with yellow flowers/green fruit at Stage 3 & bright red tomatoes at Stage 4) ---
    private static void BuildTomatoStage(Transform root, int stage, Material stemGreen, Material leafDeep)
    {
        Material stakeMat     = GetSharedMaterial("WoodStake",    new Color(0.52f, 0.36f, 0.20f), 0.10f);
        Material unripeMat    = GetSharedMaterial("TomatoGreen",  new Color(0.48f, 0.74f, 0.24f), 0.25f);
        Material flowerMat    = GetSharedMaterial("TomatoFlower", new Color(0.95f, 0.84f, 0.18f), 0.20f);
        Material ripeRedMat   = GetSharedMaterial("TomatoRed",    new Color(0.88f, 0.18f, 0.14f), 0.32f);

        float height = stage == 2 ? 0.48f : (stage == 3 ? 0.72f : 0.82f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"Tomato_{i}", ClusterOffsets[i], new Vector3(0f, i * 33f, 0f));
            // Sturdy wooden support stake
            CreatePart(PrimitiveType.Cube, plant, "SupportStake",
                new Vector3(-0.05f, height * 0.52f, -0.04f), new Vector3(0.040f, height * 1.04f, 0.040f), stakeMat);
            // Main tomato vine stem
            CreatePart(PrimitiveType.Cylinder, plant, "VineStem",
                new Vector3(0f, height * 0.48f, 0f), new Vector3(0.046f, height * 0.48f, 0.046f), stemGreen);

            // Bushy foliage tiers
            int tiers = stage == 2 ? 2 : 3;
            for (int t = 0; t < tiers; t++)
            {
                float ty = height * Mathf.Lerp(0.32f, 0.88f, t / (float)(tiers - 1));
                float r = Mathf.Lerp(0.28f, 0.20f, t / (float)tiers);
                CreatePart(PrimitiveType.Sphere, plant, $"Foliage_{t}",
                    new Vector3(0f, ty, 0f), new Vector3(r, 0.15f, r), leafDeep);
            }

            if (stage == 3)
            {
                // Yellow blossoms & green unripe tomatoes
                CreatePart(PrimitiveType.Sphere, plant, "GreenTomato_1",
                    new Vector3(0.12f, height * 0.45f, 0.08f), new Vector3(0.11f, 0.10f, 0.11f), unripeMat);
                CreatePart(PrimitiveType.Sphere, plant, "GreenTomato_2",
                    new Vector3(-0.11f, height * 0.65f, 0.09f), new Vector3(0.10f, 0.09f, 0.10f), unripeMat);
                CreatePart(PrimitiveType.Sphere, plant, "Blossom",
                    new Vector3(0.08f, height * 0.82f, -0.08f), new Vector3(0.06f, 0.06f, 0.06f), flowerMat);
            }
            else if (stage == 4)
            {
                // Clusters of plump, glossy red ripe tomatoes
                Vector3[] fruitOffsets =
                {
                    new Vector3( 0.14f, height * 0.36f,  0.09f),
                    new Vector3(-0.13f, height * 0.46f,  0.10f),
                    new Vector3( 0.11f, height * 0.66f, -0.11f),
                    new Vector3(-0.10f, height * 0.74f, -0.09f)
                };
                for (int f = 0; f < fruitOffsets.Length; f++)
                {
                    GameObject fruit = CreatePart(PrimitiveType.Sphere, plant, $"RipeTomato_{f}",
                        fruitOffsets[f], new Vector3(0.160f, 0.145f, 0.160f), ripeRedMat);
                    CreatePart(PrimitiveType.Cube, fruit.transform, "Calyx",
                        new Vector3(0f, 0.48f, 0f), new Vector3(0.48f, 0.15f, 0.48f), stemGreen);
                }
            }
        }
    }

    // --- 6. PUMPKIN (Creeping ground vines, broad lobed leaves & round ribbed orange pumpkins) ---
    private static void BuildPumpkinStage(Transform root, int stage, Material vineMat, Material leafMat)
    {
        Material blossomMat   = GetSharedMaterial("PumpkinFlower", new Color(0.96f, 0.74f, 0.12f), 0.18f);
        Material youngPumpMat = GetSharedMaterial("PumpkinYoung",  new Color(0.58f, 0.74f, 0.26f), 0.18f);
        Material ripePumpMat  = GetSharedMaterial("PumpkinRipe",   new Color(0.94f, 0.47f, 0.10f), 0.20f);
        Material ribMat       = GetSharedMaterial("PumpkinRib",    new Color(0.84f, 0.39f, 0.08f), 0.18f);

        // Creeping ground vines radiating across the plot
        float vineLen = stage == 2 ? 0.48f : (stage == 3 ? 0.72f : 0.84f);
        for (int v = 0; v < 4; v++)
        {
            float ang = v * 90f + 25f;
            Transform vine = CreatePivot(root, $"PumpkinVine_{v}", Vector3.zero, new Vector3(0f, ang, 0f));
            CreatePart(PrimitiveType.Capsule, vine, "RunnerStem",
                new Vector3(0f, 0.028f, vineLen * 0.5f), new Vector3(0.040f, vineLen * 0.5f, 0.040f), vineMat, new Vector3(90f, 0f, 0f));
            // Broad pumpkin leaves along the vine
            CreatePart(PrimitiveType.Sphere, vine, "BroadLeaf_1",
                new Vector3(0.12f, 0.09f, vineLen * 0.38f), new Vector3(0.28f, 0.040f, 0.26f), leafMat, new Vector3(12f, 20f, -10f));
            CreatePart(PrimitiveType.Sphere, vine, "BroadLeaf_2",
                new Vector3(-0.12f, 0.08f, vineLen * 0.75f), new Vector3(0.26f, 0.040f, 0.24f), leafMat, new Vector3(10f, -25f, 12f));
        }

        if (stage == 3)
        {
            // Golden trumpet blossoms + developing green-orange young pumpkins resting on soil
            CreatePart(PrimitiveType.Sphere, root, "YoungPumpkin_1",
                new Vector3(-0.30f, 0.12f, -0.24f), new Vector3(0.28f, 0.24f, 0.28f), youngPumpMat);
            CreatePart(PrimitiveType.Sphere, root, "YoungPumpkin_2",
                new Vector3(0.30f, 0.11f, 0.26f), new Vector3(0.26f, 0.22f, 0.26f), youngPumpMat);
            CreatePart(PrimitiveType.Sphere, root, "Flower_1",
                new Vector3(0.22f, 0.12f, -0.30f), new Vector3(0.12f, 0.09f, 0.12f), blossomMat);
        }
        else if (stage == 4)
        {
            // 3 large, round, ribbed harvest-ready orange pumpkins with woody stems, spaced well apart
            Vector3[] pumpPos =
            {
                new Vector3( 0.02f, 0f,  0.00f),
                new Vector3(-0.42f, 0f, -0.36f),
                new Vector3( 0.40f, 0f,  0.38f)
            };
            float[] scales = { 1.15f, 0.98f, 1.05f };
            for (int p = 0; p < pumpPos.Length; p++)
            {
                float s = scales[p];
                Transform pump = CreatePivot(root, $"RipePumpkin_{p}", pumpPos[p], new Vector3(0f, p * 40f, 0f));

                // Spherical core (plump and round, height 0.40*s, center y = 0.205*s -> base sits flush at y = 0.005*s)
                CreatePart(PrimitiveType.Sphere, pump, "Core",
                    new Vector3(0f, 0.205f * s, 0f), new Vector3(0.48f * s, 0.40f * s, 0.48f * s), ripePumpMat);

                // Deep ribbed grooves
                CreatePart(PrimitiveType.Sphere, pump, "RibA",
                    new Vector3(0f, 0.205f * s, 0f), new Vector3(0.51f * s, 0.39f * s, 0.40f * s), ribMat);
                CreatePart(PrimitiveType.Sphere, pump, "RibB",
                    new Vector3(0f, 0.205f * s, 0f), new Vector3(0.40f * s, 0.39f * s, 0.51f * s), ribMat);

                // Curled woody stem on top
                CreatePart(PrimitiveType.Cylinder, pump, "Stem",
                    new Vector3(0f, 0.41f * s, 0f), new Vector3(0.055f * s, 0.055f * s, 0.055f * s), vineMat, new Vector3(14f, 0f, 10f));
            }
        }
    }

    // --- 7. STRAWBERRY (Low leafy crowns, white blossoms at Stage 3, bright red berries at Stage 4) ---
    private static void BuildStrawberryStage(Transform root, int stage, Material leafGreen, Material leafDeep)
    {
        Material flowerWhite = GetSharedMaterial("StrawberryFlower", new Color(0.97f, 0.97f, 0.95f), 0.15f);
        Material berryRed    = GetSharedMaterial("StrawberryRed",    new Color(0.88f, 0.14f, 0.18f), 0.28f);
        Material berryUnripe = GetSharedMaterial("StrawberryPale",   new Color(0.78f, 0.88f, 0.56f), 0.20f);

        float crownScale = stage == 2 ? 0.88f : (stage == 3 ? 1.05f : 1.18f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"Strawberry_{i}", ClusterOffsets[i], new Vector3(0f, i * 48f, 0f));

            // Rosette of trifoliate leaves
            for (int l = 0; l < 6; l++)
            {
                float ang = l * 60f;
                Vector3 dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
                CreatePart(PrimitiveType.Sphere, plant, $"Leaf_{l}",
                    dir * (0.13f * crownScale) + new Vector3(0f, 0.10f * crownScale, 0f),
                    new Vector3(0.16f * crownScale, 0.036f, 0.13f * crownScale),
                    (l % 2 == 0) ? leafGreen : leafDeep,
                    new Vector3(14f, ang, 0f));
            }

            if (stage == 3)
            {
                // White flowers & pale green/pink unripe berries
                CreatePart(PrimitiveType.Sphere, plant, "Flower_1",
                    new Vector3(0.09f, 0.15f, 0.07f), new Vector3(0.062f, 0.025f, 0.062f), flowerWhite);
                CreatePart(PrimitiveType.Sphere, plant, "Flower_2",
                    new Vector3(-0.08f, 0.16f, -0.06f), new Vector3(0.058f, 0.025f, 0.058f), flowerWhite);
                CreatePart(PrimitiveType.Capsule, plant, "UnripeBerry",
                    new Vector3(0.02f, 0.09f, -0.10f), new Vector3(0.062f, 0.050f, 0.062f), berryUnripe, new Vector3(25f, 0f, 0f));
            }
            else if (stage == 4)
            {
                // Plump ripe red strawberries hanging around the base of the leafy crown
                for (int b = 0; b < 4; b++)
                {
                    float a = b * 90f + 35f;
                    Vector3 bPos = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.145f, 0.075f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.145f);
                    GameObject berry = CreatePart(PrimitiveType.Capsule, plant, $"RipeBerry_{b}",
                        bPos, new Vector3(0.088f, 0.068f, 0.088f), berryRed, new Vector3(22f, a, 0f));
                    CreatePart(PrimitiveType.Sphere, berry.transform, "BerryCap",
                        new Vector3(0f, 0.45f, 0f), new Vector3(0.85f, 0.25f, 0.85f), leafGreen);
                }
            }
        }
    }

    // --- 8. CABBAGE (Outer wrapper rosette surrounding a dense pale-green cabbage head) ---
    private static void BuildCabbageStage(Transform root, int stage, Material headPaleMat, Material leafGreen, Material leafDeep)
    {
        Material innerHeadMat = GetSharedMaterial("CabbageHead", new Color(0.56f, 0.82f, 0.40f), 0.16f);

        for (int i = 0; i < ClusterOffsets.Length; i++)
        {
            Transform plant = CreatePivot(root, $"Cabbage_{i}", ClusterOffsets[i], new Vector3(0f, i * 36f, 0f));

            // Cupped outer wrapper leaves
            int outerCount = stage == 2 ? 6 : 8;
            float outerRadius = stage == 2 ? 0.14f : (stage == 3 ? 0.18f : 0.22f);
            for (int l = 0; l < outerCount; l++)
            {
                float ang = l * (360f / outerCount);
                Transform leafPivot = CreatePivot(plant, $"OuterLeaf_{l}", Vector3.zero, new Vector3(0f, ang, 0f));
                CreatePart(PrimitiveType.Sphere, leafPivot, "LeafBlade",
                    new Vector3(0f, 0.08f, outerRadius), new Vector3(0.22f, 0.055f, 0.20f),
                    (l % 2 == 0) ? leafDeep : leafGreen,
                    new Vector3(-28f, 0f, 0f));
            }

            if (stage == 2)
            {
                // Folding inner leaves
                CreatePart(PrimitiveType.Sphere, plant, "YoungHeart",
                    new Vector3(0f, 0.095f, 0f), new Vector3(0.16f, 0.14f, 0.16f), leafGreen);
            }
            else if (stage == 3)
            {
                // Developing cabbage head
                CreatePart(PrimitiveType.Sphere, plant, "MatureHeart",
                    new Vector3(0f, 0.13f, 0f), new Vector3(0.26f, 0.23f, 0.26f), headPaleMat);
            }
            else if (stage == 4)
            {
                // Large, tight, layered harvest-ready cabbage head
                CreatePart(PrimitiveType.Sphere, plant, "CabbageHeadCore",
                    new Vector3(0f, 0.17f, 0f), new Vector3(0.38f, 0.33f, 0.38f), innerHeadMat);
                CreatePart(PrimitiveType.Sphere, plant, "CabbageWrapA",
                    new Vector3(0.025f, 0.175f, 0f), new Vector3(0.395f, 0.31f, 0.36f), headPaleMat, new Vector3(8f, 25f, 0f));
                CreatePart(PrimitiveType.Sphere, plant, "CabbageWrapB",
                    new Vector3(-0.025f, 0.175f, 0f), new Vector3(0.36f, 0.31f, 0.395f), headPaleMat, new Vector3(-8f, -35f, 0f));
            }
        }
    }

    // --- 9. WATERMELON (Creeping vines with lobed leaves & striped dark-green oval watermelons) ---
    private static void BuildWatermelonStage(Transform root, int stage, Material vineMat, Material leafMat)
    {
        Material rindDark   = GetSharedMaterial("MelonRindDark",  new Color(0.14f, 0.42f, 0.18f), 0.26f);
        Material rindStripe = GetSharedMaterial("MelonRindLight", new Color(0.42f, 0.72f, 0.30f), 0.24f);
        Material flowerMat  = GetSharedMaterial("MelonFlower",    new Color(0.95f, 0.84f, 0.20f), 0.18f);

        float vineLen = stage == 2 ? 0.48f : (stage == 3 ? 0.72f : 0.84f);
        for (int v = 0; v < 4; v++)
        {
            float ang = v * 90f + 45f;
            Transform vine = CreatePivot(root, $"MelonVine_{v}", Vector3.zero, new Vector3(0f, ang, 0f));
            CreatePart(PrimitiveType.Capsule, vine, "VineStem",
                new Vector3(0f, 0.026f, vineLen * 0.5f), new Vector3(0.036f, vineLen * 0.5f, 0.036f), vineMat, new Vector3(90f, 0f, 0f));
            CreatePart(PrimitiveType.Sphere, vine, "LobedLeaf_1",
                new Vector3(0.11f, 0.07f, vineLen * 0.40f), new Vector3(0.22f, 0.035f, 0.17f), leafMat, new Vector3(10f, 30f, 0f));
            CreatePart(PrimitiveType.Sphere, vine, "LobedLeaf_2",
                new Vector3(-0.11f, 0.07f, vineLen * 0.76f), new Vector3(0.20f, 0.035f, 0.16f), leafMat, new Vector3(10f, -30f, 0f));
        }

        if (stage == 3)
        {
            // Developing small melons resting on soil + yellow flowers
            CreatePart(PrimitiveType.Sphere, root, "YoungMelon_1",
                new Vector3(-0.26f, 0.11f, 0.22f), new Vector3(0.24f, 0.20f, 0.28f), rindStripe, new Vector3(0f, 30f, 0f));
            CreatePart(PrimitiveType.Sphere, root, "YoungMelon_2",
                new Vector3(0.27f, 0.11f, -0.24f), new Vector3(0.23f, 0.19f, 0.27f), rindStripe, new Vector3(0f, -40f, 0f));
            CreatePart(PrimitiveType.Sphere, root, "Flower",
                new Vector3(0.18f, 0.09f, 0.30f), new Vector3(0.075f, 0.045f, 0.075f), flowerMat);
        }
        else if (stage == 4)
        {
            // Plump striped oval watermelons resting flush on the soil (height 0.34*s, center y = 0.175*s -> base sits flush at y = 0.005*s)
            Vector3[] melonPos =
            {
                new Vector3( 0.03f, 0f,  0.02f),
                new Vector3(-0.42f, 0f,  0.34f),
                new Vector3( 0.40f, 0f, -0.36f)
            };
            float[] scales = { 1.15f, 0.98f, 1.05f };
            for (int m = 0; m < melonPos.Length; m++)
            {
                float s = scales[m];
                Transform melon = CreatePivot(root, $"RipeWatermelon_{m}", melonPos[m], new Vector3(0f, m * 55f + 20f, 0f));
                CreatePart(PrimitiveType.Sphere, melon, "RindBase",
                    new Vector3(0f, 0.175f * s, 0f), new Vector3(0.38f * s, 0.34f * s, 0.50f * s), rindDark);
                // Alternating light-green stripes along the oval melon
                CreatePart(PrimitiveType.Sphere, melon, "StripeA",
                    new Vector3(0f, 0.175f * s, 0f), new Vector3(0.395f * s, 0.325f * s, 0.46f * s), rindStripe);
                CreatePart(PrimitiveType.Sphere, melon, "StripeB",
                    new Vector3(0f, 0.175f * s, 0f), new Vector3(0.30f * s, 0.355f * s, 0.515f * s), rindStripe);
            }
        }
    }

    // --- 10. HAY / GRASS / FEED CROP (Lush multi-blade pasture & alfalfa tufts with seed heads) ---
    private static void BuildHayGrassStage(Transform root, int stage, Material sproutMat, Material stemGreen, Material leafGreen)
    {
        Material seedHeadMat = GetSharedMaterial("GrassSeedHead", new Color(0.68f, 0.76f, 0.32f), 0.12f);
        Material cloverMat   = GetSharedMaterial("AlfalfaClover", new Color(0.58f, 0.42f, 0.72f), 0.16f);

        float bladeHeight = stage == 2 ? 0.38f : (stage == 3 ? 0.58f : 0.72f);
        int bladesPerClump = stage == 2 ? 6 : 9;

        for (int i = 0; i < LushOrganicOffsets.Length; i++)
        {
            Transform clump = CreatePivot(root, $"GrassClump_{i}", LushOrganicOffsets[i]);
            for (int b = 0; b < bladesPerClump; b++)
            {
                float ang = b * (360f / bladesPerClump) + i * 21f;
                float tilt = (b == 0) ? 6f : (14f + (b % 3) * 7f);
                Transform blade = CreatePivot(clump, $"Blade_{b}", Vector3.zero, new Vector3(tilt, ang, 0f));
                float h = bladeHeight * (0.85f + (b % 4) * 0.08f);
                CreatePart(PrimitiveType.Cube, blade, "LeafBlade",
                    new Vector3(0f, h * 0.5f, 0f), new Vector3(0.052f, h, 0.018f), (b % 2 == 0) ? leafGreen : stemGreen);

                if (stage >= 3 && b % 2 == 0)
                {
                    CreatePart(PrimitiveType.Capsule, blade, "SeedPlume",
                        new Vector3(0f, h + 0.055f, 0f), new Vector3(0.042f, 0.065f, 0.042f), seedHeadMat);
                }
            }

            if (stage == 4)
            {
                // Alfalfa / pasture blossoms showing peak feed readiness
                CreatePart(PrimitiveType.Sphere, clump, "FeedBlossom",
                    new Vector3(0.05f, bladeHeight * 0.85f, -0.04f), new Vector3(0.075f, 0.065f, 0.075f), cloverMat);
            }
        }
    }
}

