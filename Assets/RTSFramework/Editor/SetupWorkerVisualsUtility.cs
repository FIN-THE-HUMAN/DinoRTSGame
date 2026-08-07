#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Buildings;

namespace RTSFramework.Editor
{
    public static class SetupWorkerVisualsUtility
    {
        [MenuItem("RTS Debug/Setup Worker Visuals")]
        public static void SetupWorkerVisuals()
        {
            // 1. Rig configurations: Set both character and animations to Humanoid so they bind
            ConfigureRigToHumanoid("Assets/ThirdParty/fuse civilian model/source/civilian 1.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Chopping/HumanM@TreeChopping01 - Loop.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Mining/HumanM@Mining01 - Loop Ground.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Hammering/HumanM@HammeringWall01_R - Loop.fbx");
            ConfigureRigToHumanoid("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx");

            // Configure loops
            ConfigureLoopTime("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx", true);
            ConfigureLoopTime("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Chopping/HumanM@TreeChopping01 - Loop.fbx", true);
            ConfigureLoopTime("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Mining/HumanM@Mining01 - Loop Ground.fbx", true);
            ConfigureLoopTime("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Hammering/HumanM@HammeringWall01_R - Loop.fbx", true);
            ConfigureLoopTime("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx", false);

            // 2. Create the Animator Controller if not exists or rebuild it
            string controllerPath = "Assets/Game/Units/WorkerAnimator.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("AnimationState", AnimatorControllerParameterType.Int);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
            var walkClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
            var chopClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Chopping/HumanM@TreeChopping01 - Loop.fbx");
            var mineClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Mining/HumanM@Mining01 - Loop Ground.fbx");
            var hammerClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Work/Hammering/HumanM@HammeringWall01_R - Loop.fbx");
            var deathClip = LoadAnimationClip("Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx");

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var chopState = rootStateMachine.AddState("Chopping");
            chopState.motion = chopClip;

            var mineState = rootStateMachine.AddState("Mining");
            mineState.motion = mineClip;

            var hammerState = rootStateMachine.AddState("Hammering");
            hammerState.motion = hammerClip;

            var deathState = rootStateMachine.AddState("Death");
            deathState.motion = deathClip;

            AddAnyStateTransition(rootStateMachine, idleState, 0);
            AddAnyStateTransition(rootStateMachine, walkState, 1);
            AddAnyStateTransition(rootStateMachine, chopState, 2);
            AddAnyStateTransition(rootStateMachine, mineState, 3);
            AddAnyStateTransition(rootStateMachine, hammerState, 4);
            AddAnyStateTransition(rootStateMachine, deathState, 5);

            // 3. Process all prefabs
            string[] prefabs = new string[]
            {
                "Assets/Game/Units/PlayerUnit.prefab",
                "Assets/Game/Units/PlayerRangedUnit.prefab",
                "Assets/Game/Units/EnemyUnit.prefab",
                "Assets/Game/Units/EnemyRangedUnit.prefab"
            };

            foreach (var path in prefabs)
            {
                ConfigureHumanoidPrefab(path, controller);
            }

            // Save and refresh assets so Unity finalizes the prefab and material assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. Replace any existing unit instances in the active scene with clean prefab instances
            ReplaceSceneInstances("PlayerUnit", "Assets/Game/Units/PlayerUnit.prefab");
            ReplaceSceneInstances("PlayerRangedUnit", "Assets/Game/Units/PlayerRangedUnit.prefab");
            ReplaceSceneInstances("EnemyUnit", "Assets/Game/Units/EnemyUnit.prefab");
            ReplaceSceneInstances("EnemyRangedUnit", "Assets/Game/Units/EnemyRangedUnit.prefab");

            // 5. Mark the active scene as dirty and save it so scene changes are persisted on disk
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            Debug.Log("SetupWorkerVisuals: Unified all units, replaced scene instances, and saved the active scene!");
        }

        private static void ConfigureHumanoidPrefab(string prefabPath, AnimatorController controller)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"ConfigureHumanoidPrefab: Prefab not found at {prefabPath}, skipping.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            // Clear old mesh components
            var oldFilter = root.GetComponent<MeshFilter>();
            if (oldFilter != null) Object.DestroyImmediate(oldFilter, true);

            var oldRenderer = root.GetComponent<MeshRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer, true);

            // Remove primitive children
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name != "SelectionVisual" && child.name != "selectionVisual")
                {
                    Object.DestroyImmediate(child.gameObject, true);
                }
            }

            // Instantiate civilian model
            GameObject civilianModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/fuse civilian model/source/civilian 1.fbx");
            if (civilianModel != null)
            {
                // Instantiate as normal child GameObject (not nested prefab) so modifications save directly to the root prefab asset
                GameObject modelInstance = Object.Instantiate(civilianModel);
                modelInstance.name = "WorkerVisual";
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = new Vector3(0f, -1.0f, 0f);
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                // Map textures
                ApplyHumanoidTextures(modelInstance);

                // Set animator controller
                var animator = modelInstance.GetComponent<Animator>();
                if (animator == null) animator = modelInstance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }

            // Add RTSUnitAnimationController script
            if (root.GetComponent<RTSUnitAnimationController>() == null)
            {
                root.AddComponent<RTSUnitAnimationController>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ApplyHumanoidTextures(GameObject modelInstance)
        {
            var renderer = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null) return;

            var sharedMats = renderer.sharedMaterials;
            Material[] newMats = new Material[sharedMats.Length];

            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material originalMat = sharedMats[i];
                if (originalMat == null) continue;

                string matName = originalMat.name.ToLower();
                string textureName = "";

                if (matName.Contains("body")) textureName = "Civilian 1_Body_diffuse.png";
                else if (matName.Contains("bottom")) textureName = "Civilian 1_Bottom_diffuse.png";
                else if (matName.Contains("top")) textureName = "Civilian 1_Top_diffuse.png";
                else if (matName.Contains("shoes")) textureName = "Civilian 1_Shoes_diffuse.png";
                else if (matName.Contains("hair")) textureName = "Civilian 1_Hair_diffuse.png";
                else if (matName.Contains("beard")) textureName = "Civilian 1_Beard_diffuse.png";
                else if (matName.Contains("moustache")) textureName = "Civilian 1_Moustache_diffuse.png";

                if (!string.IsNullOrEmpty(textureName))
                {
                    string texturePath = $"Assets/ThirdParty/fuse civilian model/textures/{textureName}";
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (tex != null)
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (shader == null) shader = Shader.Find("Standard");
                        if (shader == null) shader = Shader.Find("Diffuse");

                        if (shader != null)
                        {
                            string matDir = "Assets/Game/Units/Materials";
                            if (!AssetDatabase.IsValidFolder(matDir))
                            {
                                if (!AssetDatabase.IsValidFolder("Assets/Game/Units"))
                                {
                                    AssetDatabase.CreateFolder("Assets/Game", "Units");
                                }
                                AssetDatabase.CreateFolder("Assets/Game/Units", "Materials");
                            }

                            string matPath = $"{matDir}/Mat_{originalMat.name}.mat";
                            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                            // Only create if it doesn't already exist on disk, to prevent invalidating its GUID reference in other prefabs
                            if (mat == null)
                            {
                                mat = new Material(shader);
                                mat.name = $"Mat_{originalMat.name}";

                                if (shader.name.Contains("Universal Render Pipeline"))
                                {
                                    mat.SetTexture("_BaseMap", tex);
                                }
                                else
                                {
                                    mat.SetTexture("_MainTex", tex);
                                }

                                AssetDatabase.CreateAsset(mat, matPath);
                            }
                            else
                            {
                                // Make sure the texture is correctly bound in case it was created empty
                                if (shader.name.Contains("Universal Render Pipeline"))
                                {
                                    mat.SetTexture("_BaseMap", tex);
                                }
                                else
                                {
                                    mat.SetTexture("_MainTex", tex);
                                }
                                EditorUtility.SetDirty(mat);
                            }

                            newMats[i] = mat;
                        }
                    }
                }
            }

            // Flush new material assets to disk immediately so Unity generates their persistent GUIDs
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Load the newly created materials back from their saved disk paths to get persistent references
            for (int i = 0; i < newMats.Length; i++)
            {
                if (newMats[i] != null && sharedMats[i] != null)
                {
                    string matPath = $"Assets/Game/Units/Materials/Mat_{sharedMats[i].name}.mat";
                    newMats[i] = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }
                else if (newMats[i] == null)
                {
                    newMats[i] = sharedMats[i];
                }
            }

            renderer.sharedMaterials = newMats;
        }

        private static void ReplaceSceneInstances(string instanceName, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var sceneInstances = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            int replacedCount = 0;
            foreach (var instance in sceneInstances)
            {
                if (instance.name == instanceName || instance.name.StartsWith(instanceName + " ("))
                {
                    Vector3 pos = instance.transform.position;
                    Quaternion rot = instance.transform.rotation;
                    Transform parent = instance.transform.parent;

                    Object.DestroyImmediate(instance.gameObject);

                    GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    newObj.transform.position = pos;
                    newObj.transform.rotation = rot;
                    if (parent != null)
                    {
                        newObj.transform.SetParent(parent, false);
                    }
                    replacedCount++;
                }
            }
            Debug.Log($"SetupWorkerVisuals: Replaced {replacedCount} instances of {instanceName} in the active scene.");
        }

        private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState targetState, int stateValue)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "AnimationState");
            transition.duration = 0.15f;
            transition.canTransitionToSelf = false;
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void ConfigureRigToHumanoid(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureLoopTime(string fbxPath, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                SerializedObject so = new SerializedObject(importer);
                SerializedProperty clipAnimations = so.FindProperty("m_ClipAnimations");

                if (clipAnimations != null && clipAnimations.arraySize > 0)
                {
                    for (int i = 0; i < clipAnimations.arraySize; i++)
                    {
                        SerializedProperty clip = clipAnimations.GetArrayElementAtIndex(i);
                        SerializedProperty loopTimeProp = clip.FindPropertyRelative("loopTime");
                        if (loopTimeProp != null)
                        {
                            loopTimeProp.boolValue = loop;
                        }
                    }
                    so.ApplyModifiedProperties();
                    importer.SaveAndReimport();
                }
                else
                {
                    var defaultClips = importer.defaultClipAnimations;
                    if (defaultClips != null && defaultClips.Length > 0)
                    {
                        foreach (var clip in defaultClips)
                        {
                            clip.loopTime = loop;
                        }
                        importer.clipAnimations = defaultClips;
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
#endif
