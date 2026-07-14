#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Buildings;
using RTSFramework.Combat;
using RTSFramework.AI;

namespace RTSFramework.Editor
{
    public static class SetupTowersUtility
    {
        [MenuItem("RTS Debug/Setup Defensive Towers")]
        public static void SetupDefensiveTowers()
        {
            // 1. Load projectile prefab
            string projectilePath = "Assets/Game/Combat/StoneProjectile.prefab";
            GameObject projObj = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath);
            if (projObj == null)
            {
                Debug.LogError("SetupTowers: StoneProjectile.prefab not found at Assets/Game/Combat/! Please setup ranged units first.");
                return;
            }

            // 2. Create the Tower Prefab
            string towerPrefabPath = "Assets/Game/Buildings/Building_Tower.prefab";
            GameObject towerObj = AssetDatabase.LoadAssetAtPath<GameObject>(towerPrefabPath);
            if (towerObj == null)
            {
                // Create a temporary primitive cylinder (Tower body)
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = "Building_Tower";
                cylinder.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);

                // Destroy CapsuleCollider, replace with BoxCollider for better grid placement
                var capsule = cylinder.GetComponent<CapsuleCollider>();
                if (capsule != null) Object.DestroyImmediate(capsule);

                var box = cylinder.AddComponent<BoxCollider>();
                box.center = Vector3.zero;
                box.size = new Vector3(1.5f, 2.0f, 1.5f);

                // Create a child Cube representing the rotating Turret Head
                GameObject turretHeadObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                turretHeadObj.name = "TurretHead";
                turretHeadObj.transform.SetParent(cylinder.transform, false);
                turretHeadObj.transform.localPosition = new Vector3(0f, 1.15f, 0f); // Top of the tower cylinder
                turretHeadObj.transform.localScale = new Vector3(0.8f, 0.4f, 1.2f); // Elongated head looking forward

                // Remove collider on the head to avoid overlap issues
                var headCol = turretHeadObj.GetComponent<Collider>();
                if (headCol != null) Object.DestroyImmediate(headCol);

                // Create a barrel sub-element to make the front direction obvious
                GameObject barrelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrelObj.name = "LaunchPoint";
                barrelObj.transform.SetParent(turretHeadObj.transform, false);
                barrelObj.transform.localPosition = new Vector3(0f, 0f, 0.6f); // extended forward
                barrelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                barrelObj.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);

                var barrelCol = barrelObj.GetComponent<Collider>();
                if (barrelCol != null) Object.DestroyImmediate(barrelCol);

                // Apply Materials
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    Material bodyMat = new Material(standardShader);
                    bodyMat.color = new Color(0.55f, 0.45f, 0.35f); // Brick/Wood brown
                    cylinder.GetComponent<Renderer>().sharedMaterial = bodyMat;

                    Material headMat = new Material(standardShader);
                    headMat.color = new Color(0.22f, 0.22f, 0.22f); // Dark metallic
                    turretHeadObj.GetComponent<Renderer>().sharedMaterial = headMat;
                    barrelObj.GetComponent<Renderer>().sharedMaterial = headMat;
                }

                // Add Building component
                Building bldComp = cylinder.AddComponent<Building>();
                
                // Add Health component and set values
                Health healthComp = cylinder.AddComponent<Health>();
                SerializedObject serHealth = new SerializedObject(healthComp);
                serHealth.FindProperty("maxHealth").floatValue = 300f;
                serHealth.ApplyModifiedProperties();

                // Add CombatComponent and set values
                CombatComponent combatComp = cylinder.AddComponent<CombatComponent>();
                SerializedObject serCombat = new SerializedObject(combatComp);
                serCombat.FindProperty("attackDamage").floatValue = 12f; // High single target damage
                serCombat.FindProperty("attackRange").floatValue = 12f; // Long range defense
                serCombat.FindProperty("attackCooldown").floatValue = 1.5f; // Fire cooldown
                serCombat.FindProperty("projectilePrefab").objectReferenceValue = projObj.GetComponent<Projectile>();
                serCombat.FindProperty("launchPoint").objectReferenceValue = barrelObj.transform;
                serCombat.ApplyModifiedProperties();

                // Add DefensiveTower component and set properties
                DefensiveTower towerComp = cylinder.AddComponent<DefensiveTower>();
                SerializedObject serTower = new SerializedObject(towerComp);
                serTower.FindProperty("turretHead").objectReferenceValue = turretHeadObj.transform;
                serTower.FindProperty("rotationSpeed").floatValue = 12f; // Fast rotation tracking
                serTower.ApplyModifiedProperties();

                // Save as Prefab
                towerObj = PrefabUtility.SaveAsPrefabAsset(cylinder, towerPrefabPath);
                Object.DestroyImmediate(cylinder);
                
                // Force Asset Database refresh so Unity registers the new prefab components immediately
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                towerObj = AssetDatabase.LoadAssetAtPath<GameObject>(towerPrefabPath);
                Debug.Log($"SetupDefensiveTowers: Created and registered Building_Tower.prefab at {towerPrefabPath}.");
            }

            // 3. Create Tower BuildingData Asset
            string towerDataPath = "Assets/Game/Buildings/TowerData.asset";
            BuildingData towerData = AssetDatabase.LoadAssetAtPath<BuildingData>(towerDataPath);
            if (towerData == null)
            {
                towerData = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(towerData, towerDataPath);
            }

            // Setup BuildingData serialized fields
            SerializedObject serData = new SerializedObject(towerData);
            serData.FindProperty("buildingName").stringValue = "Defensive Tower";
            serData.FindProperty("maxHealth").intValue = 300;
            serData.FindProperty("constructionTime").floatValue = 15f;
            serData.FindProperty("gridSize").floatValue = 2f; // Medium 2x2 grid footprint

            var buildingComp = towerObj != null ? towerObj.GetComponent<Building>() : null;
            if (buildingComp == null && towerObj != null)
            {
                // Fallback attempt
                AssetDatabase.Refresh();
                GameObject tempObj = AssetDatabase.LoadAssetAtPath<GameObject>(towerPrefabPath);
                if (tempObj != null)
                {
                    buildingComp = tempObj.GetComponent<Building>();
                }
            }
            Debug.Log($"SetupDefensiveTowers: towerObj={towerObj}");
            serData.FindProperty("buildingPrefab").objectReferenceValue = towerObj;
            serData.FindProperty("ghostPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Buildings/Ghost_TownHall.prefab");

            // Set Cost: 150 Gold, 100 Wood
            var costProp = serData.FindProperty("cost");
            costProp.ClearArray();
            costProp.InsertArrayElementAtIndex(0);
            var goldCost = costProp.GetArrayElementAtIndex(0);
            goldCost.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            goldCost.FindPropertyRelative("amount").intValue = 150;

            costProp.InsertArrayElementAtIndex(1);
            var woodCost = costProp.GetArrayElementAtIndex(1);
            woodCost.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Wood;
            woodCost.FindPropertyRelative("amount").intValue = 100;

            serData.ApplyModifiedProperties();
            EditorUtility.SetDirty(towerData);

            // 4. Update the Player Worker prefab Build HUD capability list
            string workerPrefabPath = "Assets/Game/Units/PlayerUnit.prefab";
            GameObject workerRoot = PrefabUtility.LoadPrefabContents(workerPrefabPath);
            if (workerRoot != null)
            {
                var builder = workerRoot.GetComponent<BuilderComponent>();
                if (builder != null)
                {
                    SerializedObject serBuild = new SerializedObject(builder);
                    var list = serBuild.FindProperty("buildableBuildings");
                    
                    bool hasTower = false;
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == towerData)
                        {
                            hasTower = true;
                            break;
                        }
                    }

                    if (!hasTower)
                    {
                        int index = list.arraySize;
                        list.InsertArrayElementAtIndex(index);
                        list.GetArrayElementAtIndex(index).objectReferenceValue = towerData;
                        serBuild.ApplyModifiedProperties();
                        Debug.Log("SetupDefensiveTowers: Registered Tower in Player Worker's build list!");
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(workerRoot, workerPrefabPath);
                PrefabUtility.UnloadPrefabContents(workerRoot);
            }

            // 5. Update the Scene Skirmish AI component
            var AICommander = Object.FindAnyObjectByType<AISkirmishCommander>();
            if (AICommander != null)
            {
                SerializedObject serComm = new SerializedObject(AICommander);
                serComm.FindProperty("towerPrefabData").objectReferenceValue = towerData;
                serComm.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(AICommander);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(AICommander.gameObject.scene);
                Debug.Log("SetupDefensiveTowers: Linked TowerData to AISkirmishCommander scene instance!");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SetupDefensiveTowers: Defensive Towers setup completed successfully!");
        }
    }
}
#endif
