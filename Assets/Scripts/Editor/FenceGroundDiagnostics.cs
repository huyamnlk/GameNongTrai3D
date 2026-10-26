#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using IrishFarmSim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batchmode diagnostics: measures fence/wall ground gaps and lists ground marker discs in Farm.unity.
/// Usage: -executeMethod FenceGroundDiagnostics.Run  (writes Logs/fence_ground_report.txt)
/// </summary>
public static class FenceGroundDiagnostics
{
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Farm.unity", OpenSceneMode.Single);
            Report(sb);
        }
        catch (System.Exception ex)
        {
            sb.AppendLine("EXCEPTION: " + ex);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/fence_ground_report.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }

    public static void Report(StringBuilder sb)
    {
        sb.AppendLine("=== MARKER / DISC OBJECTS ===");
        Dictionary<string, int> markerCounts = new Dictionary<string, int>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = t.name;
            string key = null;
            if (n.StartsWith("MoistSoilApron") || n.StartsWith("PenDirtPatch") || n.StartsWith("DirtPathStep") ||
                n == "BlobShadow" || n.StartsWith("SelectionIndicator") || n.StartsWith("DragIndicator") ||
                n == "ClickMoveMarker" || n.StartsWith("MeadowGrassTuft"))
            {
                key = System.Text.RegularExpressions.Regex.Replace(n, @"_\d+(_\d+)?$", "");
                if (n == "BlobShadow") key += " (parent=" + (t.parent != null ? System.Text.RegularExpressions.Regex.Replace(t.parent.name, @"[_ ]?\d+$", "") : "-") + ")";
            }
            if (key == null) continue;
            Renderer r = t.GetComponent<Renderer>();
            key += (r != null && r.enabled && t.gameObject.activeInHierarchy) ? " [RENDERING]" : " [hidden]";
            markerCounts.TryGetValue(key, out int c);
            markerCounts[key] = c + 1;
        }
        foreach (var kv in markerCounts) sb.AppendLine($"  {kv.Value,4} x {kv.Key}");

        sb.AppendLine("=== FENCE / WALL SEGMENTS (gap = meshBottom - groundMin; >0 floating, <0 sunk) ===");
        int floating = 0, sunk = 0, ok = 0;
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!IsFenceSegment(r.transform)) continue;
            Bounds b = r.bounds;
            float gMin = float.MaxValue, gMax = float.MinValue;
            foreach (Vector3 p in new[] {
                new Vector3(b.min.x, 0, b.min.z), new Vector3(b.max.x, 0, b.min.z),
                new Vector3(b.min.x, 0, b.max.z), new Vector3(b.max.x, 0, b.max.z), new Vector3(b.center.x, 0, b.center.z) })
            {
                float g = FenceGroundAligner.SampleGroundY(new Vector3(p.x, b.center.y, p.z), r.transform.root);
                gMin = Mathf.Min(gMin, g); gMax = Mathf.Max(gMax, g);
            }
            float gap = b.min.y - gMin;
            bool isPost = r.name.Contains("Post") || r.name.ToLower().Contains("wall") || r.name.ToLower().Contains("fence");
            if (!isPost) continue; // rails are elevated by design
            string state = gap > 0.03f ? "FLOAT" : (gap < -0.25f ? "SUNK" : "ok");
            if (state == "FLOAT") floating++; else if (state == "SUNK") sunk++; else ok++;
            if (state != "ok")
                sb.AppendLine($"  {state,-5} gap={gap,7:F3} slope={gMax - gMin,6:F3} {Path(r.transform)}");
        }
        sb.AppendLine($"  SUMMARY ground-contact segments: ok={ok}, floating={floating}, sunk={sunk}");

        sb.AppendLine("=== DUPLICATE SEGMENTS (same name+pos within 0.05m) ===");
        Dictionary<string, int> dupes = new Dictionary<string, int>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!IsFenceSegment(r.transform)) continue;
            Vector3 p = r.transform.position;
            string k = $"{r.transform.root.name}/{r.name}@{Mathf.Round(p.x * 20)},{Mathf.Round(p.y * 20)},{Mathf.Round(p.z * 20)}";
            dupes.TryGetValue(k, out int c); dupes[k] = c + 1;
        }
        int dupCount = 0;
        foreach (var kv in dupes) if (kv.Value > 1) { dupCount++; sb.AppendLine($"  {kv.Value}x {kv.Key}"); }
        sb.AppendLine($"  duplicates={dupCount}");

        sb.AppendLine("=== PEN ROOT SCALE / PARENT SCALE ===");
        foreach (AnimalPenBoundary pen in Object.FindObjectsByType<AnimalPenBoundary>(FindObjectsSortMode.None))
        {
            Transform t = pen.transform;
            sb.AppendLine($"  {t.name}: lossyScale={t.lossyScale} parent={(t.parent ? t.parent.name + " " + t.parent.lossyScale : "-")} posY={t.position.y:F3} groundY={FenceGroundAligner.SampleGroundY(t.position, t):F3}");
        }
    }

    private static bool IsFenceSegment(Transform t)
    {
        string n = t.name;
        string l = n.ToLower();
        if (n.StartsWith("Post_") || n.StartsWith("GatePost_") || n.StartsWith("Rail_") || n == "GateHeaderBeam") return true;
        if (l.Contains("stone_wall") || (l.Contains("fence") && !l.Contains("fallback"))) return true;
        return false;
    }

    private static string Path(Transform t)
    {
        string p = t.name;
        int depth = 0;
        while (t.parent != null && depth++ < 3) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
