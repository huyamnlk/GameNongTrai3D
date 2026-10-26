using System;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim
{
    /// <summary>
    /// Centralized typography and UI styling system for Irish Farm Sim.
    /// Provides consistent modern rounded sans-serif fonts with full Vietnamese diacritics support (đ, Đ, ă, â, ê, ô, ơ, ư),
    /// high-contrast palettes, responsive text best-fit, shadows, outlines, and unified card/modal styling.
    /// </summary>
    public static class FarmUITheme
    {
        private static Font s_RegularFont;
        private static Font s_BoldFont;

        public static Font RegularFont
        {
            get
            {
                if (s_RegularFont == null)
                {
                    s_RegularFont = Resources.Load<Font>("Fonts/FarmFont_Regular");
                    if (s_RegularFont == null) s_RegularFont = Resources.Load<Font>("FarmFont_Regular");
                    if (s_RegularFont == null)
                    {
                        s_RegularFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return s_RegularFont;
            }
        }

        public static Font BoldFont
        {
            get
            {
                if (s_BoldFont == null)
                {
                    s_BoldFont = Resources.Load<Font>("Fonts/FarmFont_Bold");
                    if (s_BoldFont == null) s_BoldFont = Resources.Load<Font>("FarmFont_Bold");
                    if (s_BoldFont == null)
                    {
                        s_BoldFont = RegularFont;
                    }
                }
                return s_BoldFont;
            }
        }

        // =========================================================================
        // PALETTES & CONTRAST
        // =========================================================================
        public static readonly Color ColorTitle            = new Color(1.00f, 0.90f, 0.28f, 1.0f); // Bright warm gold
        public static readonly Color ColorHeader           = new Color(0.96f, 0.98f, 1.00f, 1.0f); // Clean crisp white
        public static readonly Color ColorBody             = new Color(0.90f, 0.94f, 0.98f, 1.0f); // Soft light blue-white
        public static readonly Color ColorSubtext          = new Color(0.72f, 0.80f, 0.90f, 1.0f); // Secondary hint
        public static readonly Color ColorUnlocked         = new Color(0.35f, 0.95f, 0.50f, 1.0f); // Vibrant light green
        public static readonly Color ColorLocked           = new Color(1.00f, 0.68f, 0.16f, 1.0f); // Warm lock amber
        public static readonly Color ColorLockedGray       = new Color(0.76f, 0.80f, 0.85f, 1.0f); // Highly readable disabled grey
        public static readonly Color ColorDanger           = new Color(1.00f, 0.40f, 0.36f, 1.0f); // Warning red
        public static readonly Color ColorGold             = new Color(1.00f, 0.88f, 0.22f, 1.0f); // Gold coin text
        public static readonly Color ColorXP               = new Color(0.35f, 0.88f, 1.00f, 1.0f); // Cyan XP glow
        public static readonly Color ColorShadowDark       = new Color(0.04f, 0.08f, 0.12f, 0.75f);
        public static readonly Color ColorOutlineSubtle    = new Color(0.02f, 0.04f, 0.06f, 0.65f);

        // Friendly Aliases for convenience
        public static Color GoldYellow => ColorGold;
        public static Color XPGreen => ColorUnlocked;
        public static Color LockedOrange => ColorLocked;
        public static Color GreenPrimary => ColorUnlocked;
        public static Color TextDim => ColorSubtext;

        // =========================================================================
        // STYLING HELPERS
        // =========================================================================
        public static void ApplyTitle(Text text, int size = 24, Color? customColor = null)
        {
            if (text == null) return;
            text.font = BoldFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = customColor ?? ColorTitle;
            EnsureBestFit(text, Mathf.Max(14, size - 8), size + 4);
            AddShadow(text, ColorShadowDark, new Vector2(1.5f, -1.5f));
        }

        public static void ApplyHeader(Text text, int size = 20, Color? customColor = null)
        {
            if (text == null) return;
            text.font = BoldFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = customColor ?? ColorHeader;
            EnsureBestFit(text, Mathf.Max(12, size - 6), size + 2);
            AddShadow(text, ColorShadowDark, new Vector2(1.2f, -1.2f));
        }

        public static void ApplyBody(Text text, int size, bool bold)
        {
            ApplyBody(text, size, null, bold);
        }

        public static void ApplyBody(Text text, int size = 16, Color? customColor = null, bool bold = false)
        {
            if (text == null) return;
            text.font = bold ? BoldFont : RegularFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = customColor ?? ColorBody;
            EnsureBestFit(text, Mathf.Max(11, size - 5), size + 2);
            AddShadow(text, ColorShadowDark, new Vector2(1f, -1f));
        }

        public static void ApplyButton(Text text, int size = 18, Color? customColor = null)
        {
            if (text == null) return;
            text.font = BoldFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = customColor ?? Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            EnsureBestFit(text, Mathf.Max(12, size - 6), size + 2);
            AddShadow(text, ColorShadowDark, new Vector2(1.2f, -1.2f));
        }

        public static void ApplyBadge(Text text, int size = 15, bool isUnlocked = true)
        {
            if (text == null) return;
            text.font = BoldFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = isUnlocked ? ColorUnlocked : ColorLocked;
            EnsureBestFit(text, Mathf.Max(10, size - 4), size + 2);
            AddShadow(text, ColorShadowDark, new Vector2(1f, -1f));
        }

        public static void EnsureBestFit(Text text, int minSize, int maxSize)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static Shadow AddShadow(Text text, Color? shadowColor = null, Vector2? dist = null)
        {
            if (text == null) return null;
            Shadow s = text.GetComponent<Shadow>();
            if (s == null) s = text.gameObject.AddComponent<Shadow>();
            s.effectColor = shadowColor ?? ColorShadowDark;
            s.effectDistance = dist ?? new Vector2(1.2f, -1.2f);
            s.useGraphicAlpha = true;
            return s;
        }

        public static Outline AddOutline(Text text, Color? outlineColor = null, Vector2? dist = null)
        {
            if (text == null) return null;
            return AddOutline(text.gameObject, outlineColor, dist);
        }

        public static Outline AddOutline(GameObject go, Color? outlineColor = null, Vector2? dist = null)
        {
            if (go == null) return null;
            Outline o = go.GetComponent<Outline>();
            if (o == null) o = go.AddComponent<Outline>();
            o.effectColor = outlineColor ?? ColorOutlineSubtle;
            o.effectDistance = dist ?? new Vector2(1.0f, -1.0f);
            o.useGraphicAlpha = true;
            return o;
        }

        /// <summary>
        /// Recursively styles all Text components under root with modern FarmFont and adds soft readable shadow.
        /// Does not clobber intentional special text colors unless forceColor is provided.
        /// </summary>
        public static void StyleAllTextUnder(Transform root, bool includeInactive = true)
        {
            if (root != null) StyleAllTextUnder(root.gameObject, includeInactive);
        }

        public static void StyleAllTextUnder(GameObject root, bool includeInactive = true)
        {
            if (root == null) return;
            Text[] texts = root.GetComponentsInChildren<Text>(includeInactive);
            foreach (var t in texts)
            {
                if (t == null) continue;
                bool isBold = t.fontStyle == FontStyle.Bold || t.fontStyle == FontStyle.BoldAndItalic || t.fontSize >= 17;
                t.font = isBold ? BoldFont : RegularFont;

                // Ensure readable contrast on dark or 3D backgrounds
                if (t.GetComponent<Shadow>() == null && t.GetComponent<Outline>() == null)
                {
                    AddShadow(t, ColorShadowDark, new Vector2(1.2f, -1.2f));
                }

                // Minimum legible font size on mobile (ensure no unreadable tiny text < 13pt)
                if (t.fontSize < 13)
                {
                    t.fontSize = 13;
                }

                // Protect text from clipping while enabling responsive mobile scaling
                if (!t.resizeTextForBestFit)
                {
                    int max = Mathf.Max(13, t.fontSize);
                    int min = Mathf.Max(11, max - 5);
                    EnsureBestFit(t, min, max);
                }
            }
        }

        public static void StyleAllTextMeshUnder(GameObject root)
        {
            if (root == null) return;
            TextMesh[] tms = root.GetComponentsInChildren<TextMesh>(true);
            foreach (var tm in tms)
            {
                if (tm == null) continue;
                tm.font = BoldFont;
                MeshRenderer mr = tm.GetComponent<MeshRenderer>();
                if (mr != null && BoldFont != null && BoldFont.material != null)
                {
                    mr.sharedMaterial = BoldFont.material;
                }
            }
        }
    }
}

