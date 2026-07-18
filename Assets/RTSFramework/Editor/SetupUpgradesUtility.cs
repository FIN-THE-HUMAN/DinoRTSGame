#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RTSFramework.Upgrades;
using RTSFramework.Buildings;

namespace RTSFramework.Editor
{
    public static class SetupUpgradesUtility
    {
        [MenuItem("RTS Debug/Setup Technology Upgrades")]
        public static void SetupTechnologyUpgrades()
        {
            string folderPath = "Assets/Game/Upgrades";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Game"))
                {
                    AssetDatabase.CreateFolder("Assets", "Game");
                }
                AssetDatabase.CreateFolder("Assets/Game", "Upgrades");
            }

            // 1. Create Raptor Claws I
            string clawsPath = $"{folderPath}/RaptorClaws.asset";
            UpgradeData clawsUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(clawsPath);
            if (clawsUpgrade == null)
            {
                clawsUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(clawsUpgrade, clawsPath);
            }

            SerializedObject serClaws = new SerializedObject(clawsUpgrade);
            serClaws.FindProperty("upgradeId").stringValue = "RaptorClaws";
            serClaws.FindProperty("upgradeName").stringValue = "Raptor Claws I";
            serClaws.FindProperty("researchTime").floatValue = 12f;

            var clawsCost = serClaws.FindProperty("cost");
            clawsCost.ClearArray();
            clawsCost.InsertArrayElementAtIndex(0);
            var goldCostEl = clawsCost.GetArrayElementAtIndex(0);
            goldCostEl.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            goldCostEl.FindPropertyRelative("amount").intValue = 150;

            var clawsEffects = serClaws.FindProperty("effects");
            clawsEffects.ClearArray();
            
            // Effect 1: Warrior damage
            clawsEffects.InsertArrayElementAtIndex(0);
            var eff1 = clawsEffects.GetArrayElementAtIndex(0);
            eff1.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.AttackDamage;
            eff1.FindPropertyRelative("value").floatValue = 4f;
            eff1.FindPropertyRelative("isPercent").boolValue = false;
            eff1.FindPropertyRelative("targetUnitTag").stringValue = "Raptor Warrior";

            // Effect 2: Enemy Warrior damage
            clawsEffects.InsertArrayElementAtIndex(1);
            var eff2 = clawsEffects.GetArrayElementAtIndex(1);
            eff2.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.AttackDamage;
            eff2.FindPropertyRelative("value").floatValue = 4f;
            eff2.FindPropertyRelative("isPercent").boolValue = false;
            eff2.FindPropertyRelative("targetUnitTag").stringValue = "Enemy Raptor Warrior";

            // Effect 3: Archer damage
            clawsEffects.InsertArrayElementAtIndex(2);
            var eff3 = clawsEffects.GetArrayElementAtIndex(2);
            eff3.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.AttackDamage;
            eff3.FindPropertyRelative("value").floatValue = 3f;
            eff3.FindPropertyRelative("isPercent").boolValue = false;
            eff3.FindPropertyRelative("targetUnitTag").stringValue = "Raptor Archer";

            // Effect 4: Enemy Archer damage
            clawsEffects.InsertArrayElementAtIndex(3);
            var eff4 = clawsEffects.GetArrayElementAtIndex(3);
            eff4.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.AttackDamage;
            eff4.FindPropertyRelative("value").floatValue = 3f;
            eff4.FindPropertyRelative("isPercent").boolValue = false;
            eff4.FindPropertyRelative("targetUnitTag").stringValue = "Enemy Raptor Archer";

            serClaws.ApplyModifiedProperties();
            EditorUtility.SetDirty(clawsUpgrade);

            // 2. Create Hardened Scales I
            string scalesPath = $"{folderPath}/HardenedScales.asset";
            UpgradeData scalesUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(scalesPath);
            if (scalesUpgrade == null)
            {
                scalesUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(scalesUpgrade, scalesPath);
            }

            SerializedObject serScales = new SerializedObject(scalesUpgrade);
            serScales.FindProperty("upgradeId").stringValue = "HardenedScales";
            serScales.FindProperty("upgradeName").stringValue = "Hardened Scales I";
            serScales.FindProperty("researchTime").floatValue = 15f;

            var scalesCost = serScales.FindProperty("cost");
            scalesCost.ClearArray();
            scalesCost.InsertArrayElementAtIndex(0);
            var scalesGold = scalesCost.GetArrayElementAtIndex(0);
            scalesGold.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            scalesGold.FindPropertyRelative("amount").intValue = 120;
            scalesCost.InsertArrayElementAtIndex(1);
            var scalesWood = scalesCost.GetArrayElementAtIndex(1);
            scalesWood.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Wood;
            scalesWood.FindPropertyRelative("amount").intValue = 80;

            var scalesEffects = serScales.FindProperty("effects");
            scalesEffects.ClearArray();
            scalesEffects.InsertArrayElementAtIndex(0);
            var effScales = scalesEffects.GetArrayElementAtIndex(0);
            effScales.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.MaxHealth;
            effScales.FindPropertyRelative("value").floatValue = 30f;
            effScales.FindPropertyRelative("isPercent").boolValue = false;
            effScales.FindPropertyRelative("targetUnitTag").stringValue = ""; // applies to all

            serScales.ApplyModifiedProperties();
            EditorUtility.SetDirty(scalesUpgrade);

            // 3. Create Feathered Agility I
            string agilityPath = $"{folderPath}/FeatheredAgility.asset";
            UpgradeData agilityUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(agilityPath);
            if (agilityUpgrade == null)
            {
                agilityUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(agilityUpgrade, agilityPath);
            }

            SerializedObject serAgility = new SerializedObject(agilityUpgrade);
            serAgility.FindProperty("upgradeId").stringValue = "FeatheredAgility";
            serAgility.FindProperty("upgradeName").stringValue = "Feathered Agility I";
            serAgility.FindProperty("researchTime").floatValue = 10f;

            var agilityCost = serAgility.FindProperty("cost");
            agilityCost.ClearArray();
            agilityCost.InsertArrayElementAtIndex(0);
            var agilityGold = agilityCost.GetArrayElementAtIndex(0);
            agilityGold.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Gold;
            agilityGold.FindPropertyRelative("amount").intValue = 100;
            agilityCost.InsertArrayElementAtIndex(1);
            var agilityWood = agilityCost.GetArrayElementAtIndex(1);
            agilityWood.FindPropertyRelative("resourceType").enumValueIndex = (int)Resources.ResourceType.Wood;
            agilityWood.FindPropertyRelative("amount").intValue = 100;

            var agilityEffects = serAgility.FindProperty("effects");
            agilityEffects.ClearArray();
            agilityEffects.InsertArrayElementAtIndex(0);
            var effAgility = agilityEffects.GetArrayElementAtIndex(0);
            effAgility.FindPropertyRelative("effectType").enumValueIndex = (int)UpgradeEffectType.MoveSpeed;
            effAgility.FindPropertyRelative("value").floatValue = 0.2f; // +20% Speed
            effAgility.FindPropertyRelative("isPercent").boolValue = true;
            effAgility.FindPropertyRelative("targetUnitTag").stringValue = ""; // applies to all

            serAgility.ApplyModifiedProperties();
            EditorUtility.SetDirty(agilityUpgrade);

            AssetDatabase.SaveAssets();

            // 4. Update the Building_TownHall.prefab with researchable upgrades
            string prefabPath = "Assets/Game/Buildings/Building_TownHall.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot != null)
            {
                var researchComp = prefabRoot.GetComponent<TechnologyResearchComponent>();
                if (researchComp == null)
                {
                    researchComp = prefabRoot.AddComponent<TechnologyResearchComponent>();
                }

                // Add the upgrades to the prefab
                SerializedObject serComp = new SerializedObject(researchComp);
                var listProp = serComp.FindProperty("researchableUpgrades");
                listProp.ClearArray();
                listProp.InsertArrayElementAtIndex(0);
                listProp.GetArrayElementAtIndex(0).objectReferenceValue = clawsUpgrade;
                listProp.InsertArrayElementAtIndex(1);
                listProp.GetArrayElementAtIndex(1).objectReferenceValue = scalesUpgrade;
                listProp.InsertArrayElementAtIndex(2);
                listProp.GetArrayElementAtIndex(2).objectReferenceValue = agilityUpgrade;
                serComp.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("SetupUpgrades: Building_TownHall.prefab updated with 3 premium dinosaur upgrades!");
            }
            else
            {
                Debug.LogError("SetupUpgrades: Failed to load Building_TownHall.prefab contents!");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SetupUpgrades: Technology upgrades setup completed successfully!");
        }
    }
}
#endif
