using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim.Editor
{
    public static class InspectMainMenuScene
    {
        [MenuItem("IrishFarmSim/Inspect Main Menu Hierarchy", priority = 200)]
        public static void Inspect()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
            Debug.Log($"=== DUMP OF SCENE: {scene.name} ===");
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                DumpGameObject(go, 0);
            }
        }

        public static void InspectBatch()
        {
            Inspect();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void DumpGameObject(GameObject go, int indent)
        {
            string ind = new string(' ', indent * 2);
            string comps = "";
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                string extra = "";
                if (c is UILabel lbl) extra = $" (text='{lbl.text}', font={lbl.trueTypeFont?.name ?? lbl.bitmapFont?.name})";
                else if (c is UITexture uit) extra = $" (mainTexture={uit.mainTexture?.name ?? "null"})";
                else if (c is UISprite uis) extra = $" (spriteName={uis.spriteName}, atlas={uis.atlas?.name})";
                else if (c is UnityEngine.UI.Text txt) extra = $" (text='{txt.text}')";
                comps += $" [{c.GetType().Name}{extra}]";
            }
            Debug.Log($"{ind}- {go.name} (active={go.activeSelf}){comps}");

            for (int i = 0; i < go.transform.childCount; i++)
            {
                DumpGameObject(go.transform.GetChild(i).gameObject, indent + 1);
            }
        }
    }
}
