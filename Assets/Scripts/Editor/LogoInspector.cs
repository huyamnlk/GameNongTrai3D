using UnityEditor;
using UnityEngine;

namespace IrishFarmSim.Editor
{
    public static class LogoInspector
    {
        [MenuItem("IrishFarmSim/Inspect Logo Prefab")]
        public static void Inspect()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NGUI/Main Menu Scene/Logo.prefab");
            if (prefab == null)
            {
                Debug.LogError("Logo.prefab not found!");
                return;
            }

            Debug.Log($"=== Inspecting Logo.prefab: {prefab.name} ===");
            foreach (var comp in prefab.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                if (comp is UITexture uit)
                {
                    Debug.Log($"UITexture: name={uit.name}, mainTexture={uit.mainTexture?.name}, path={AssetDatabase.GetAssetPath(uit.mainTexture)}, width={uit.width}, height={uit.height}");
                }
                else if (comp is UISprite uis)
                {
                    Debug.Log($"UISprite: name={uis.name}, spriteName={uis.spriteName}, atlas={uis.atlas?.name}");
                }
                else if (comp is UILabel uil)
                {
                    Debug.Log($"UILabel: name={uil.name}, text='{uil.text}', font={uil.trueTypeFont?.name ?? uil.bitmapFont?.name}");
                }
                else
                {
                    Debug.Log($"Component: {comp.GetType().Name} on {comp.name}");
                }
            }
        }
    }
}
