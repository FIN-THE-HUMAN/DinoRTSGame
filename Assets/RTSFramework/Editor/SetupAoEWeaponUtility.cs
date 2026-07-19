#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RTSFramework.Combat;
using RTSFramework.Buildings;
using RTSFramework.Units;
using RTSFramework.CameraSystem;
using System.Collections.Generic;

namespace RTSFramework.Editor
{
    public static class SetupAoEWeaponUtility
    {
        [MenuItem("RTS Debug/Setup AoE Weapon")]
        public static void SetupAoEWeapon()
        {
            // 1. Move assets to Resources so they can be loaded dynamically at runtime
            MoveAssetToResources("Assets/Game/Buildings/TownHallData.asset", "Assets/Resources/Buildings", "TownHallData.asset");
            MoveAssetToResources("Assets/Game/Buildings/TowerData.asset", "Assets/Resources/Buildings", "TowerData.asset");
            MoveAssetToResources("Assets/Game/Buildings/ArtilleryTowerData.asset", "Assets/Resources/Buildings", "ArtilleryTowerData.asset");

            MoveAssetToResources("Assets/Game/Units/PlayerUnitData.asset", "Assets/Resources/Units", "PlayerUnitData.asset");
            MoveAssetToResources("Assets/Game/Units/PlayerRangedUnitData.asset", "Assets/Resources/Units", "PlayerRangedUnitData.asset");
            MoveAssetToResources("Assets/Game/Units/EnemyUnitData.asset", "Assets/Resources/Units", "EnemyUnitData.asset");
            MoveAssetToResources("Assets/Game/Units/EnemyRangedUnitData.asset", "Assets/Resources/Units", "EnemyRangedUnitData.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Create and configure CameraShakeData ScriptableObjects
            string combatDir = "Assets/Game/Combat";
            if (!AssetDatabase.IsValidFolder(combatDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Game"))
                {
                    AssetDatabase.CreateFolder("Assets", "Game");
                }
                AssetDatabase.CreateFolder("Assets/Game", "Combat");
            }

            // Normal Shake (Gentle shake, small radius)
            string normalShakePath = $"{combatDir}/NormalExplosionShake.asset";
            CameraShakeData normalShake = AssetDatabase.LoadAssetAtPath<CameraShakeData>(normalShakePath);
            if (normalShake == null)
            {
                normalShake = ScriptableObject.CreateInstance<CameraShakeData>();
                AssetDatabase.CreateAsset(normalShake, normalShakePath);
            }
            SerializedObject serNormalShake = new SerializedObject(normalShake);
            serNormalShake.FindProperty("intensity").floatValue = 0.35f; 
            serNormalShake.FindProperty("duration").floatValue = 0.35f;
            serNormalShake.FindProperty("shakeRadius").floatValue = 22f; // Small radius -> zoom dependent
            serNormalShake.ApplyModifiedProperties();
            EditorUtility.SetDirty(normalShake);

            // Super Shake (Large shake, huge radius)
            string superShakePath = $"{combatDir}/SuperExplosionShake.asset";
            CameraShakeData superShake = AssetDatabase.LoadAssetAtPath<CameraShakeData>(superShakePath);
            if (superShake == null)
            {
                superShake = ScriptableObject.CreateInstance<CameraShakeData>();
                AssetDatabase.CreateAsset(superShake, superShakePath);
            }
            SerializedObject serSuperShake = new SerializedObject(superShake);
            serSuperShake.FindProperty("intensity").floatValue = 0.85f; 
            serSuperShake.FindProperty("duration").floatValue = 0.65f;
            serSuperShake.FindProperty("shakeRadius").floatValue = 80f; // Large radius -> zoom independent
            serSuperShake.ApplyModifiedProperties();
            EditorUtility.SetDirty(superShake);

            // 3. Load templates
            string stonePath = "Assets/Game/Combat/StoneProjectile.prefab";
            GameObject stoneObj = AssetDatabase.LoadAssetAtPath<GameObject>(stonePath);
            
            // 4. Create or Update ExplosiveShell.prefab
            string explosivePath = $"{combatDir}/ExplosiveShell.prefab";
            GameObject explosivePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(explosivePath);
            if (explosivePrefab == null)
            {
                GameObject tempObj;
                if (stoneObj != null)
                {
                    tempObj = Object.Instantiate(stoneObj);
                    tempObj.name = "ExplosiveShell";
                }
                else
                {
                    tempObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    tempObj.name = "ExplosiveShell";
                    tempObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                    var col = tempObj.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);
                }

                var renderer = tempObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader != null)
                    {
                        Material mat = new Material(standardShader);
                        mat.color = new Color(0.95f, 0.38f, 0.05f); 
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(0.4f, 0.12f, 0f));
                        renderer.sharedMaterial = mat;
                    }
                }

                var oldScript = tempObj.GetComponent<Projectile>();
                if (oldScript != null) Object.DestroyImmediate(oldScript);

                var explosiveScript = tempObj.AddComponent<ExplosiveProjectile>();
                
                SerializedObject serProj = new SerializedObject(explosiveScript);
                serProj.FindProperty("speed").floatValue = 12f; 
                serProj.FindProperty("curveArc").floatValue = 1.2f; 
                serProj.FindProperty("aoeRadius").floatValue = 4.5f; 
                serProj.FindProperty("shakeData").objectReferenceValue = normalShake;
                serProj.ApplyModifiedProperties();

                explosivePrefab = PrefabUtility.SaveAsPrefabAsset(tempObj, explosivePath);
                Object.DestroyImmediate(tempObj);
                Debug.Log($"SetupAoEWeapon: Created ExplosiveShell prefab.");
            }
            else
            {
                var script = explosivePrefab.GetComponent<ExplosiveProjectile>();
                if (script != null)
                {
                    SerializedObject serProj = new SerializedObject(script);
                    serProj.FindProperty("shakeData").objectReferenceValue = normalShake;
                    serProj.FindProperty("curveArc").floatValue = 1.2f; 
                    serProj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(explosivePrefab);
                }
            }

            // 5. Revert Building_Tower.prefab back to normal single-target tower
            string towerPrefabPath = "Assets/Game/Buildings/Building_Tower.prefab";
            GameObject towerRoot = PrefabUtility.LoadPrefabContents(towerPrefabPath);
            if (towerRoot != null)
            {
                var combat = towerRoot.GetComponent<CombatComponent>();
                if (combat != null && stoneObj != null)
                {
                    SerializedObject serCombat = new SerializedObject(combat);
                    serCombat.FindProperty("projectilePrefab").objectReferenceValue = stoneObj.GetComponent<Projectile>();
                    serCombat.FindProperty("attackDamage").floatValue = 12f; 
                    serCombat.FindProperty("attackCooldown").floatValue = 1.5f; 
                    serCombat.ApplyModifiedProperties();
                }
                PrefabUtility.SaveAsPrefabAsset(towerRoot, towerPrefabPath);
                PrefabUtility.UnloadPrefabContents(towerRoot);
                Debug.Log("SetupAoEWeapon: Reverted Building_Tower to standard single-target projectile.");
            }

            // 6. Create or Update Building_ArtilleryTower.prefab
            string artTowerPrefabPath = "Assets/Game/Buildings/Building_ArtilleryTower.prefab";
            GameObject artTowerObj = AssetDatabase.LoadAssetAtPath<GameObject>(artTowerPrefabPath);
            if (artTowerObj == null)
            {
                AssetDatabase.CopyAsset(towerPrefabPath, artTowerPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                artTowerObj = AssetDatabase.LoadAssetAtPath<GameObject>(artTowerPrefabPath);
            }

            GameObject artTowerRoot = PrefabUtility.LoadPrefabContents(artTowerPrefabPath);
            if (artTowerRoot != null)
            {
                artTowerRoot.name = "Building_ArtilleryTower";

                Transform head = artTowerRoot.transform.Find("TurretHead");
                if (head != null)
                {
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader != null)
                    {
                        Material artMat = new Material(standardShader);
                        artMat.color = new Color(0.6f, 0.15f, 0.15f); 
                        artMat.EnableKeyword("_EMISSION");
                        artMat.SetColor("_EmissionColor", new Color(0.12f, 0.02f, 0.02f));
                        
                        var rend = head.GetComponent<Renderer>();
                        if (rend != null) rend.sharedMaterial = artMat;

                        Transform barrel = head.Find("LaunchPoint");
                        if (barrel != null)
                        {
                            var bRend = barrel.GetComponent<Renderer>();
                            if (bRend != null) bRend.sharedMaterial = artMat;
                        }
                    }
                }

                var combat = artTowerRoot.GetComponent<CombatComponent>();
                if (combat != null)
                {
                    SerializedObject serCombat = new SerializedObject(combat);
                    serCombat.FindProperty("projectilePrefab").objectReferenceValue = explosivePrefab.GetComponent<Projectile>();
                    serCombat.FindProperty("attackDamage").floatValue = 22f; 
                    serCombat.FindProperty("attackRange").floatValue = 14f;  
                    serCombat.FindProperty("attackCooldown").floatValue = 2.8f; 
                    serCombat.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(artTowerRoot, artTowerPrefabPath);
                PrefabUtility.UnloadPrefabContents(artTowerRoot);
                Debug.Log($"SetupAoEWeapon: Configured Building_ArtilleryTower.prefab.");
            }

            // 7. Configure BuildingData (under the new Resources paths)
            string towerDataPath = "Assets/Resources/Buildings/TowerData.asset";
            BuildingData towerData = AssetDatabase.LoadAssetAtPath<BuildingData>(towerDataPath);
            if (towerData != null)
            {
                SerializedObject serT = new SerializedObject(towerData);
                serT.FindProperty("buildingName").stringValue = "Defensive Tower";
                serT.FindProperty("constructionTime").floatValue = 12f;
                serT.ApplyModifiedProperties();
                EditorUtility.SetDirty(towerData);
            }

            string artTowerDataPath = "Assets/Resources/Buildings/ArtilleryTowerData.asset";
            BuildingData artTowerData = AssetDatabase.LoadAssetAtPath<BuildingData>(artTowerDataPath);
            if (artTowerData == null)
            {
                artTowerData = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(artTowerData, artTowerDataPath);
            }
            SerializedObject serA = new SerializedObject(artTowerData);
            serA.FindProperty("buildingName").stringValue = "Artillery Tower";
            serA.FindProperty("maxHealth").intValue = 400;
            serA.FindProperty("constructionTime").floatValue = 20f;
            serA.FindProperty("gridSize").floatValue = 2f;
            serA.FindProperty("buildingPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(artTowerPrefabPath);
            serA.FindProperty("ghostPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Buildings/Ghost_TownHall.prefab");
            
            var costProp = serA.FindProperty("cost");
            costProp.ClearArray();
            costProp.InsertArrayElementAtIndex(0);
            var goldCost = costProp.GetArrayElementAtIndex(0);
            goldCost.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            goldCost.FindPropertyRelative("amount").intValue = 220;

            costProp.InsertArrayElementAtIndex(1);
            var woodCost = costProp.GetArrayElementAtIndex(1);
            woodCost.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Wood;
            woodCost.FindPropertyRelative("amount").intValue = 140;

            serA.ApplyModifiedProperties();
            EditorUtility.SetDirty(artTowerData);

            // 8. Register on Player Worker prefab buildable list
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
                    bool hasArtTower = false;

                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var val = list.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (val == towerData) hasTower = true;
                        if (val == artTowerData) hasArtTower = true;
                    }

                    if (!hasTower && towerData != null)
                    {
                        int index = list.arraySize;
                        list.InsertArrayElementAtIndex(index);
                        list.GetArrayElementAtIndex(index).objectReferenceValue = towerData;
                    }
                    if (!hasArtTower)
                    {
                        int index = list.arraySize;
                        list.InsertArrayElementAtIndex(index);
                        list.GetArrayElementAtIndex(index).objectReferenceValue = artTowerData;
                    }

                    serBuild.ApplyModifiedProperties();
                    EditorUtility.SetDirty(workerRoot);
                }

                PrefabUtility.SaveAsPrefabAsset(workerRoot, workerPrefabPath);
                PrefabUtility.UnloadPrefabContents(workerRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SetupAoEWeapon: Area-of-Effect structures setup and dynamic relocation completed successfully!");
        }

        private static void MoveAssetToResources(string sourcePath, string targetDir, string fileName)
        {
            if (!AssetDatabase.IsValidFolder(targetDir))
            {
                string[] parts = targetDir.Split('/');
                string currentPath = parts[0]; 
                for (int i = 1; i < parts.Length; i++)
                {
                    string nextPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = nextPath;
                }
            }

            string destPath = targetDir + "/" + fileName;
            if (AssetDatabase.LoadAssetAtPath<Object>(sourcePath) != null && AssetDatabase.LoadAssetAtPath<Object>(destPath) == null)
            {
                string error = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log($"Moved {sourcePath} to {destPath}");
                }
                else
                {
                    Debug.LogError($"Failed to move {sourcePath}: {error}");
                }
            }
        }
    }
}
#endif
