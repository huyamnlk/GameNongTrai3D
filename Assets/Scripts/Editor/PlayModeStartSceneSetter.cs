using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IrishFarmSim.Editor
{
    /// <summary>
    /// Guarantees that pressing Play in Unity Editor always launches the Main Menu scene first,
    /// matching the standalone build and Android lifecycle.
    /// Can also be toggled or configured via IrishFarmSim menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetter
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/Main Menu.unity";

        static PlayModeStartSceneSetter()
        {
            ApplyPlayModeStartScene();
        }

        [MenuItem("IrishFarmSim/Set PlayMode Start Scene to Main Menu", priority = 100)]
        public static void ApplyPlayModeStartScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MAIN_MENU_SCENE_PATH);
            if (sceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log($"[PlayModeStartSceneSetter] Configured Editor playModeStartScene -> {MAIN_MENU_SCENE_PATH}");
            }
            else
            {
                Debug.LogWarning($"[PlayModeStartSceneSetter] Could not find scene asset at '{MAIN_MENU_SCENE_PATH}'!");
            }
        }

        [MenuItem("IrishFarmSim/Clear PlayMode Start Scene (Play Active Scene)", priority = 101)]
        public static void ClearPlayModeStartScene()
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("[PlayModeStartSceneSetter] Cleared playModeStartScene. Editor will now play whichever scene is currently open.");
        }
    }
}
