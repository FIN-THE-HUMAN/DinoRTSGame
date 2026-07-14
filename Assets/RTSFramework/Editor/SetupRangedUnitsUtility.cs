#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Buildings;
using RTSFramework.Combat;
using RTSFramework.AI;

namespace RTSFramework.Editor
{
    public static class SetupRangedUnitsUtility
    {
        [MenuItem("RTS Debug/Setup Ranged Units")]
        public static void SetupRangedUnits()
        {
            // 1. Ensure Combat directory exists
            string combatDir = "Assets/Game/Combat";
            if (!AssetDatabase.IsValidFolder(combatDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Game"))
                {
                    AssetDatabase.CreateFolder("Assets", "Game");
                }
                AssetDatabase.CreateFolder("Assets/Game", "Combat");
            }

            // 2. Create the Projectile Prefab
            string projectilePrefabPath = $"{combatDir}/StoneProjectile.prefab";
            GameObject projObj = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePrefabPath);
            if (projObj == null)
            {
                // Create a temporary primitive sphere
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tempSphere.name = "StoneProjectile";
                tempSphere.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                // Add material color (stone grey)
                var renderer = tempSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader != null)
                    {
                        Material mat = new Material(standardShader);
                        mat.color = new Color(0.45f, 0.45f, 0.45f); // Stone grey
                        renderer.sharedMaterial = mat;
                    }
                }

                // Destroy the default collider (we do not want physical collision, projectile is script-guided)
                var col = tempSphere.GetComponent<Collider>();
                if (col != null)
                {
                    Object.DestroyImmediate(col);
                }

                // Add Projectile script component
                Projectile projScript = tempSphere.AddComponent<Projectile>();
                
                // Configure Projectile properties via SerializedObject
                SerializedObject serProj = new SerializedObject(projScript);
                serProj.FindProperty("speed").floatValue = 20f;
                serProj.FindProperty("curveArc").floatValue = 0.5f; // Ballistic arc height
                serProj.ApplyModifiedProperties();

                // Save as Prefab
                projObj = PrefabUtility.SaveAsPrefabAsset(tempSphere, projectilePrefabPath);
                Object.DestroyImmediate(tempSphere);
                Debug.Log($"SetupRangedUnits: Created StoneProjectile prefab at {projectilePrefabPath}.");
            }

            // 3. Create Player Ranged Unit (Raptor Archer) Data and Prefab
            string playerRangedDataPath = "Assets/Game/Units/PlayerRangedUnitData.asset";
            UnitData playerRangedData = AssetDatabase.LoadAssetAtPath<UnitData>(playerRangedDataPath);
            if (playerRangedData == null)
            {
                playerRangedData = ScriptableObject.CreateInstance<UnitData>();
                AssetDatabase.CreateAsset(playerRangedData, playerRangedDataPath);
            }

            string playerRangedPrefabPath = "Assets/Game/Units/PlayerRangedUnit.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerRangedPrefabPath) == null)
            {
                AssetDatabase.CopyAsset("Assets/Game/Units/PlayerUnit.prefab", playerRangedPrefabPath);
            }

            // Load and modify the Player Ranged Unit Prefab contents
            GameObject playerRangedRoot = PrefabUtility.LoadPrefabContents(playerRangedPrefabPath);
            if (playerRangedRoot != null)
            {
                var combat = playerRangedRoot.GetComponent<CombatComponent>();
                if (combat != null)
                {
                    SerializedObject serCombat = new SerializedObject(combat);
                    serCombat.FindProperty("attackDamage").floatValue = 8f; // Ranged base damage
                    serCombat.FindProperty("attackRange").floatValue = 10f; // Ranged attack range (10m)
                    serCombat.FindProperty("attackCooldown").floatValue = 1.2f; // Cooldown
                    serCombat.FindProperty("projectilePrefab").objectReferenceValue = projObj.GetComponent<Projectile>();
                    serCombat.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(playerRangedRoot, playerRangedPrefabPath);
                PrefabUtility.UnloadPrefabContents(playerRangedRoot);
                Debug.Log($"SetupRangedUnits: Configured PlayerRangedUnit prefab at {playerRangedPrefabPath}.");
            }

            // Update Player Ranged UnitData Serialized Properties
            SerializedObject serPlayerData = new SerializedObject(playerRangedData);
            serPlayerData.FindProperty("unitName").stringValue = "Raptor Archer";
            serPlayerData.FindProperty("maxHealth").floatValue = 80f; // Less HP than melee warrior
            serPlayerData.FindProperty("trainingTime").floatValue = 6f;
            serPlayerData.FindProperty("selectionPriority").intValue = 60;
            serPlayerData.FindProperty("unitPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(playerRangedPrefabPath);
            
            // Set Gold Cost (60)
            var playerCostProp = serPlayerData.FindProperty("cost");
            playerCostProp.ClearArray();
            playerCostProp.InsertArrayElementAtIndex(0);
            var playerCostEl = playerCostProp.GetArrayElementAtIndex(0);
            playerCostEl.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            playerCostEl.FindPropertyRelative("amount").intValue = 60;

            serPlayerData.ApplyModifiedProperties();
            EditorUtility.SetDirty(playerRangedData);

            // 4. Create Enemy Ranged Unit Data and Prefab
            string enemyRangedDataPath = "Assets/Game/Units/EnemyRangedUnitData.asset";
            UnitData enemyRangedData = AssetDatabase.LoadAssetAtPath<UnitData>(enemyRangedDataPath);
            if (enemyRangedData == null)
            {
                enemyRangedData = ScriptableObject.CreateInstance<UnitData>();
                AssetDatabase.CreateAsset(enemyRangedData, enemyRangedDataPath);
            }

            string enemyRangedPrefabPath = "Assets/Game/Units/EnemyRangedUnit.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(enemyRangedPrefabPath) == null)
            {
                AssetDatabase.CopyAsset("Assets/Game/Units/EnemyUnit.prefab", enemyRangedPrefabPath);
            }

            // Load and modify the Enemy Ranged Unit Prefab contents
            GameObject enemyRangedRoot = PrefabUtility.LoadPrefabContents(enemyRangedPrefabPath);
            if (enemyRangedRoot != null)
            {
                var combat = enemyRangedRoot.GetComponent<CombatComponent>();
                if (combat != null)
                {
                    SerializedObject serCombat = new SerializedObject(combat);
                    serCombat.FindProperty("attackDamage").floatValue = 8f; // Ranged base damage
                    serCombat.FindProperty("attackRange").floatValue = 10f; // Ranged attack range (10m)
                    serCombat.FindProperty("attackCooldown").floatValue = 1.2f; // Cooldown
                    serCombat.FindProperty("projectilePrefab").objectReferenceValue = projObj.GetComponent<Projectile>();
                    serCombat.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(enemyRangedRoot, enemyRangedPrefabPath);
                PrefabUtility.UnloadPrefabContents(enemyRangedRoot);
                Debug.Log($"SetupRangedUnits: Configured EnemyRangedUnit prefab at {enemyRangedPrefabPath}.");
            }

            // Update Enemy Ranged UnitData Serialized Properties
            SerializedObject serEnemyData = new SerializedObject(enemyRangedData);
            serEnemyData.FindProperty("unitName").stringValue = "Enemy Raptor Archer";
            serEnemyData.FindProperty("maxHealth").floatValue = 80f;
            serEnemyData.FindProperty("trainingTime").floatValue = 6f;
            serEnemyData.FindProperty("selectionPriority").intValue = 60;
            serEnemyData.FindProperty("unitPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(enemyRangedPrefabPath);
            
            // Set Gold Cost (60)
            var enemyCostProp = serEnemyData.FindProperty("cost");
            enemyCostProp.ClearArray();
            enemyCostProp.InsertArrayElementAtIndex(0);
            var enemyCostEl = enemyCostProp.GetArrayElementAtIndex(0);
            enemyCostEl.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            enemyCostEl.FindPropertyRelative("amount").intValue = 60;

            serEnemyData.ApplyModifiedProperties();
            EditorUtility.SetDirty(enemyRangedData);

            // 5. Update Trainable lists on Town Hall GameObjects in the scene
            // Update Player Town Hall
            GameObject playerTownHall = GameObject.Find("TownHall");
            if (playerTownHall != null)
            {
                var prod = playerTownHall.GetComponent<UnitProductionComponent>();
                if (prod != null)
                {
                    SerializedObject serProd = new SerializedObject(prod);
                    var trainableList = serProd.FindProperty("trainableUnits");
                    
                    // Add ranged unit to player town hall trainable units if not already in list
                    bool containsRanged = false;
                    for (int i = 0; i < trainableList.arraySize; i++)
                    {
                        if (trainableList.GetArrayElementAtIndex(i).objectReferenceValue == playerRangedData)
                        {
                            containsRanged = true;
                            break;
                        }
                    }
                    if (!containsRanged)
                    {
                        int index = trainableList.arraySize;
                        trainableList.InsertArrayElementAtIndex(index);
                        trainableList.GetArrayElementAtIndex(index).objectReferenceValue = playerRangedData;
                        serProd.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prod);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerTownHall.scene);
                        Debug.Log("SetupRangedUnits: Added Player Ranged Unit to TownHall's trainableUnits!");
                    }
                }
            }

            // Update Enemy Town Hall
            GameObject enemyTownHall = GameObject.Find("TownHall (1)");
            if (enemyTownHall != null)
            {
                var prod = enemyTownHall.GetComponent<UnitProductionComponent>();
                if (prod != null)
                {
                    SerializedObject serProd = new SerializedObject(prod);
                    var trainableList = serProd.FindProperty("trainableUnits");

                    // Add ranged unit to enemy town hall trainable units
                    bool containsRanged = false;
                    for (int i = 0; i < trainableList.arraySize; i++)
                    {
                        if (trainableList.GetArrayElementAtIndex(i).objectReferenceValue == enemyRangedData)
                        {
                            containsRanged = true;
                            break;
                        }
                    }
                    if (!containsRanged)
                    {
                        int index = trainableList.arraySize;
                        trainableList.InsertArrayElementAtIndex(index);
                        trainableList.GetArrayElementAtIndex(index).objectReferenceValue = enemyRangedData;
                        serProd.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prod);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(enemyTownHall.scene);
                        Debug.Log("SetupRangedUnits: Added Enemy Ranged Unit to TownHall (1)'s trainableUnits!");
                    }
                }

                // Also update the AISkirmishCommander component in the scene!
                var commander = Object.FindAnyObjectByType<AISkirmishCommander>();
                if (commander != null)
                {
                    SerializedObject serComm = new SerializedObject(commander);
                    serComm.FindProperty("combatRangedPrefabData").objectReferenceValue = enemyRangedData;
                    serComm.ApplyModifiedProperties();
                    EditorUtility.SetDirty(commander);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(commander.gameObject.scene);
                    Debug.Log("SetupRangedUnits: Linked Enemy Ranged Unit to AISkirmishCommander!");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SetupRangedUnits: Ranged combat units setup completed successfully!");
        }
    }
}
#endif
