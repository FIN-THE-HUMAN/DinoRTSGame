#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RTSFramework.Resources;
using RTSFramework.Buildings;
using RTSFramework.UI;

namespace RTSFramework.Editor
{
    public static class SetupMinimapUtility
    {
        [MenuItem("RTS Debug/Setup Minimap")]
        public static void SetupMinimap()
        {
            // 1. Register Minimap layer in project settings if not already defined
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            bool layerExists = false;
            int targetLayerIndex = 8; // Default fallback to 8
            for (int i = 8; i < 32; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (p.stringValue == "Minimap")
                {
                    layerExists = true;
                    targetLayerIndex = i;
                    break;
                }
            }

            if (!layerExists)
            {
                // Find first empty slot starting from layer 8
                for (int i = 8; i < 32; i++)
                {
                    var p = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(p.stringValue))
                    {
                        p.stringValue = "Minimap";
                        targetLayerIndex = i;
                        layerExists = true;
                        break;
                    }
                }
                tagManager.ApplyModifiedProperties();
                Debug.Log($"SetupMinimap: Registered layer 'Minimap' at index {targetLayerIndex}.");
            }

            // 2. Disable the Minimap layer from the main camera's Culling Mask
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.cullingMask &= ~(1 << targetLayerIndex);
                EditorUtility.SetDirty(mainCam);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mainCam.gameObject.scene);
                Debug.Log("SetupMinimap: Disabled 'Minimap' layer from main camera Culling Mask!");
            }

            // 3. Attach MinimapMarker component to all Unit and Building Prefabs
            AddMinimapMarkerToPrefab("Assets/Game/Units/PlayerUnit.prefab");
            AddMinimapMarkerToPrefab("Assets/Game/Units/EnemyUnit.prefab");
            AddMinimapMarkerToPrefab("Assets/Game/Units/PlayerRangedUnit.prefab");
            AddMinimapMarkerToPrefab("Assets/Game/Units/EnemyRangedUnit.prefab");
            AddMinimapMarkerToPrefab("Assets/Game/Buildings/Building_TownHall.prefab");
            AddMinimapMarkerToPrefab("Assets/Game/Buildings/Building_Tower.prefab");

            // 4. Attach MinimapMarker to all pre-placed scene resources, buildings, and units
            var sceneResources = Object.FindObjectsByType<ResourceSource>(FindObjectsSortMode.None);
            foreach (var res in sceneResources)
            {
                if (res.GetComponent<MinimapMarker>() == null)
                {
                    res.gameObject.AddComponent<MinimapMarker>();
                    EditorUtility.SetDirty(res);
                }
            }

            var sceneBuildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var bld in sceneBuildings)
            {
                if (bld.GetComponent<MinimapMarker>() == null)
                {
                    bld.gameObject.AddComponent<MinimapMarker>();
                    EditorUtility.SetDirty(bld);
                }
            }

            var sceneUnits = Object.FindObjectsByType<Units.UnitController>(FindObjectsSortMode.None);
            foreach (var unit in sceneUnits)
            {
                if (unit.GetComponent<MinimapMarker>() == null)
                {
                    unit.gameObject.AddComponent<MinimapMarker>();
                    EditorUtility.SetDirty(unit);
                }
            }

            // 5. Construct the UI Panel and RawImage inside the Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SetupMinimap: Canvas object not found in the active scene! Please create a UI Canvas first.");
                return;
            }

            GameObject minimapPanel = GameObject.Find("MinimapPanel");
            if (minimapPanel == null)
            {
                minimapPanel = new GameObject("MinimapPanel", typeof(RectTransform));
                minimapPanel.transform.SetParent(canvas.transform, false);
            }

            var panelRt = minimapPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0f, 0f);
            panelRt.pivot = new Vector2(0f, 0f);
            panelRt.anchoredPosition = new Vector2(15f, 15f); // 15px inset margins from bottom-left corner
            panelRt.sizeDelta = new Vector2(180f, 180f); // 180x180 square dimension

            var bgImage = minimapPanel.GetComponent<Image>();
            if (bgImage == null) bgImage = minimapPanel.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f); // Solid dark grey border frame

            GameObject rawImageObj = null;
            Transform child = minimapPanel.transform.Find("MinimapRawImage");
            if (child != null)
            {
                rawImageObj = child.gameObject;
            }
            else
            {
                rawImageObj = new GameObject("MinimapRawImage", typeof(RectTransform));
                rawImageObj.transform.SetParent(minimapPanel.transform, false);
            }

            var rawRt = rawImageObj.GetComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.sizeDelta = new Vector2(-6f, -6f); // 3px border padding inside frame background
            rawRt.anchoredPosition = Vector2.zero;

            var rawImage = rawImageObj.GetComponent<RawImage>();
            if (rawImage == null) rawImage = rawImageObj.AddComponent<RawImage>();
            rawImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray placeholder in Editor

            // Attach interactive RTSMinimap component
            var minimap = rawImageObj.GetComponent<RTSMinimap>();
            if (minimap == null) minimap = rawImageObj.AddComponent<RTSMinimap>();

            // Hide the panel by default in Edit Mode so the viewport is clean
            minimapPanel.SetActive(false);

            EditorUtility.SetDirty(minimapPanel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SetupMinimap: Minimap setup completed successfully!");
        }

        private static void AddMinimapMarkerToPrefab(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root != null)
            {
                if (root.GetComponent<MinimapMarker>() == null)
                {
                    root.AddComponent<MinimapMarker>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"SetupMinimap: Attached MinimapMarker to prefab at {path}.");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Debug.LogWarning($"SetupMinimap: Failed to load prefab contents at {path}!");
            }
        }
    }
}
#endif
