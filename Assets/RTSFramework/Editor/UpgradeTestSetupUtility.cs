#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RTSFramework.Upgrades;
using RTSFramework.Buildings;

namespace RTSFramework.Editor
{
    public static class UpgradeTestSetupUtility
    {
        [MenuItem("RTS Debug/Setup Upgrade Test")]
        public static void SetupUpgradeTest()
        {
            // 1. Ensure directory exists
            string folderPath = "Assets/Game/Upgrades";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Upgrades");
            }

            // 2. Create Speed Upgrade config
            string speedPath = $"{folderPath}/TestSpeedUpgrade.asset";
            UpgradeData speedUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(speedPath);
            if (speedUpgrade == null)
            {
                speedUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
            }

            SerializedObject serializedSpeed = new SerializedObject(speedUpgrade);
            serializedSpeed.FindProperty("upgradeId").stringValue = "TestSpeed";
            serializedSpeed.FindProperty("upgradeName").stringValue = "Speed Boost I";
            serializedSpeed.FindProperty("researchTime").floatValue = 8f;
            
            // Set Wood Cost (100)
            var costProp = serializedSpeed.FindProperty("cost");
            costProp.ClearArray();
            costProp.InsertArrayElementAtIndex(0);
            var element = costProp.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Wood;
            element.FindPropertyRelative("amount").intValue = 100;

            // Set speed boost effects
            var effectsProp = serializedSpeed.FindProperty("effects");
            effectsProp.ClearArray();
            effectsProp.InsertArrayElementAtIndex(0);
            var effectElement = effectsProp.GetArrayElementAtIndex(0);
            effectElement.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.MoveSpeed;
            effectElement.FindPropertyRelative("value").floatValue = 1.5f; // +150% Speed
            effectElement.FindPropertyRelative("isPercent").boolValue = true;
            effectElement.FindPropertyRelative("targetUnitTag").stringValue = ""; // applies to all

            serializedSpeed.ApplyModifiedProperties();
            
            if (AssetDatabase.LoadAssetAtPath<UpgradeData>(speedPath) == null)
            {
                AssetDatabase.CreateAsset(speedUpgrade, speedPath);
            }
            else
            {
                EditorUtility.SetDirty(speedUpgrade);
            }

            // 3. Create Damage Upgrade config
            string damagePath = $"{folderPath}/TestDamageUpgrade.asset";
            UpgradeData damageUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(damagePath);
            if (damageUpgrade == null)
            {
                damageUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
            }

            SerializedObject serializedDamage = new SerializedObject(damageUpgrade);
            serializedDamage.FindProperty("upgradeId").stringValue = "TestDamage";
            serializedDamage.FindProperty("upgradeName").stringValue = "Iron Claws I";
            serializedDamage.FindProperty("researchTime").floatValue = 6f;

            // Set Gold Cost (150)
            var damageCostProp = serializedDamage.FindProperty("cost");
            damageCostProp.ClearArray();
            damageCostProp.InsertArrayElementAtIndex(0);
            var damageCostElement = damageCostProp.GetArrayElementAtIndex(0);
            damageCostElement.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            damageCostElement.FindPropertyRelative("amount").intValue = 150;

            // Set flat damage boost effects
            var damageEffectsProp = serializedDamage.FindProperty("effects");
            damageEffectsProp.ClearArray();
            damageEffectsProp.InsertArrayElementAtIndex(0);
            var damageEffectElement = damageEffectsProp.GetArrayElementAtIndex(0);
            damageEffectElement.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.AttackDamage;
            damageEffectElement.FindPropertyRelative("value").floatValue = 15f; // +15 flat damage
            damageEffectElement.FindPropertyRelative("isPercent").boolValue = false;
            damageEffectElement.FindPropertyRelative("targetUnitTag").stringValue = ""; // applies to all

            serializedDamage.ApplyModifiedProperties();

            if (AssetDatabase.LoadAssetAtPath<UpgradeData>(damagePath) == null)
            {
                AssetDatabase.CreateAsset(damageUpgrade, damagePath);
            }
            else
            {
                EditorUtility.SetDirty(damageUpgrade);
            }

            AssetDatabase.SaveAssets();

            // 4. Update the Building_TownHall.prefab
            string prefabPath = "Assets/Game/Buildings/Building_TownHall.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot != null)
            {
                var researchComp = prefabRoot.GetComponent<TechnologyResearchComponent>();
                if (researchComp == null)
                {
                    researchComp = prefabRoot.AddComponent<TechnologyResearchComponent>();
                }

                // Attach the new ScriptableObject Upgrades to the prefab list
                SerializedObject serializedComp = new SerializedObject(researchComp);
                var listProp = serializedComp.FindProperty("researchableUpgrades");
                listProp.ClearArray();
                listProp.InsertArrayElementAtIndex(0);
                listProp.GetArrayElementAtIndex(0).objectReferenceValue = speedUpgrade;
                listProp.InsertArrayElementAtIndex(1);
                listProp.GetArrayElementAtIndex(1).objectReferenceValue = damageUpgrade;
                serializedComp.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("UpgradeTestSetupUtility: Building_TownHall.prefab updated with TechnologyResearchComponent and test upgrades!");
            }
            else
            {
                Debug.LogError("UpgradeTestSetupUtility: Failed to load Building_TownHall.prefab contents!");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
