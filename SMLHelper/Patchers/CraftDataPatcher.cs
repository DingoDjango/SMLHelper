﻿namespace SMLHelper.V2.Patchers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Assets;
    using Harmony;

    internal class CraftDataPatcher
    {
        #region Internal Fields
        
        internal static IDictionary<TechType, ITechData> CustomTechData = new SelfCheckingDictionary<TechType, ITechData>("CustomTechData");
        internal static IDictionary<TechType, TechType> CustomHarvestOutputList = new SelfCheckingDictionary<TechType, TechType>("CustomHarvestOutputList");
        internal static IDictionary<TechType, HarvestType> CustomHarvestTypeList = new SelfCheckingDictionary<TechType, HarvestType>("CustomHarvestTypeList");
        internal static IDictionary<TechType, int> CustomFinalCutBonusList = new SelfCheckingDictionary<TechType, int>("CustomFinalCutBonusList", TechTypeExtensions.sTechTypeComparer);
        internal static IDictionary<TechType, Vector2int> CustomItemSizes = new SelfCheckingDictionary<TechType, Vector2int>("CustomItemSizes");
        internal static IDictionary<TechType, EquipmentType> CustomEquipmentTypes = new SelfCheckingDictionary<TechType, EquipmentType>("CustomEquipmentTypes");
        internal static IDictionary<TechType, QuickSlotType> CustomSlotTypes = new SelfCheckingDictionary<TechType, QuickSlotType>("CustomSlotTypes");
        internal static IDictionary<TechType, float> CustomCraftingTimes = new SelfCheckingDictionary<TechType, float>("CustomCraftingTimes");
        internal static IDictionary<TechType, TechType> CustomCookedCreatureList = new SelfCheckingDictionary<TechType, TechType>("CustomCookedCreatureList");
        internal static IDictionary<TechType, CraftData.BackgroundType> CustomBackgroundTypes = new SelfCheckingDictionary<TechType, CraftData.BackgroundType>("CustomBackgroundTypes", TechTypeExtensions.sTechTypeComparer);
        internal static List<TechType> CustomBuildables = new List<TechType>();

        #endregion

        #region Reflection
        private static readonly Type CraftDataType = typeof(CraftData);

        private static readonly FieldInfo GroupsField =
            CraftDataType.GetField("groups", BindingFlags.NonPublic | BindingFlags.Static);

        #endregion

        #region Group Handling

        internal static void AddToCustomGroup(TechGroup group, TechCategory category, TechType techType, TechType after)
        {
            var groups = GroupsField.GetValue(null) as Dictionary<TechGroup, Dictionary<TechCategory, List<TechType>>>;
            Dictionary<TechCategory, List<TechType>> techGroup = groups[group];
            if (techGroup == null)
            {
                // Should never happen, but doesn't hurt to add it.
                Logger.Log("Invalid TechGroup!", LogLevel.Error);
                return;
            }

            List<TechType> techCategory = techGroup[category];
            if (techCategory == null)
            {
                Logger.Log($"Invalid TechCategory Combination! TechCategory: {category} TechGroup: {group}", LogLevel.Error);
                return;
            }

            int index = techCategory.IndexOf(after);

            if(index == -1) // Not found
            {
                techCategory.Add(techType);
                Logger.Log($"Added \"{techType.AsString():G}\" to groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
            }
            else
            {
                techCategory.Insert(index + 1, techType);

                Logger.Log($"Added \"{techType.AsString():G}\" to groups under \"{group:G}->{category:G}\" after \"{after.AsString():G}\"", LogLevel.Debug);
            }
        }

        internal static void RemoveFromCustomGroup(TechGroup group, TechCategory category, TechType techType)
        {
            var groups = GroupsField.GetValue(null) as Dictionary<TechGroup, Dictionary<TechCategory, List<TechType>>>;
            Dictionary<TechCategory, List<TechType>> techGroup = groups[group];
            if (techGroup == null)
            {
                // Should never happen, but doesn't hurt to add it.
                Logger.Log("Invalid TechGroup!", LogLevel.Error);
                return;
            }

            List<TechType> techCategory = techGroup[category];
            if (techCategory == null)
            {
                Logger.Log($"Invalid TechCategory Combination! TechCategory: {category} TechGroup: {group}", LogLevel.Error);
                return;
            }

            techCategory.Remove(techType);

            Logger.Log($"Removed \"{techType.AsString():G}\" from groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
        }

        internal static void AddToCustomTechData(TechType techType, ITechData techData)
        {
            CustomTechData.Add(techType, techData);
        }

        #endregion

        #region Patching

        internal static void Patch(HarmonyInstance harmony)
        {
            PatchUtils.PatchDictionary(CraftDataType, "harvestOutputList", CustomHarvestOutputList, BindingFlags.Static | BindingFlags.Public);
            PatchUtils.PatchDictionary(CraftDataType, "harvestTypeList", CustomHarvestTypeList);
            PatchUtils.PatchDictionary(CraftDataType, "harvestFinalCutBonusList", CustomFinalCutBonusList);
            PatchUtils.PatchDictionary(CraftDataType, "itemSizes", CustomItemSizes);
            PatchUtils.PatchDictionary(CraftDataType, "equipmentTypes", CustomEquipmentTypes);
            PatchUtils.PatchDictionary(CraftDataType, "slotTypes", CustomSlotTypes);
            PatchUtils.PatchDictionary(CraftDataType, "craftingTimes", CustomCraftingTimes);
            PatchUtils.PatchDictionary(CraftDataType, "cookedCreatureList", CustomCookedCreatureList);
            PatchUtils.PatchDictionary(CraftDataType, "backgroundTypes", CustomBackgroundTypes);
            PatchUtils.PatchList(CraftDataType, "buildables", CustomBuildables);

            MethodInfo preparePrefabIDCache = CraftDataType.GetMethod("PreparePrefabIDCache", BindingFlags.Public | BindingFlags.Static);

            harmony.Patch(preparePrefabIDCache, null,
                new HarmonyMethod(typeof(CraftDataPatcher).GetMethod("PreparePrefabIDCachePostfix", BindingFlags.NonPublic | BindingFlags.Static)));

            AddCustomTechDataToOriginalDictionary();

            Logger.Log("CraftDataPatcher is done.", LogLevel.Debug);
        }

        private static void PreparePrefabIDCachePostfix()
        {
            var techMapping = CraftDataType.GetField("techMapping", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as Dictionary<TechType, string>;
            var entClassTechTable = CraftDataType.GetField("entClassTechTable", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as Dictionary<string, TechType>;

            foreach (ModPrefab prefab in ModPrefab.Prefabs)
            {
                techMapping[prefab.TechType] = prefab.ClassID;
                entClassTechTable[prefab.ClassID] = prefab.TechType;
            }
        }

        private static void AddCustomTechDataToOriginalDictionary()
        {
            Type CraftDataType = typeof(CraftData);
            Type TechDataType = CraftDataType.GetNestedType("TechData", BindingFlags.NonPublic);
            Type IngredientType = CraftDataType.GetNestedType("Ingredient", BindingFlags.NonPublic);
            Type IngredientsType = CraftDataType.GetNestedType("Ingredients", BindingFlags.NonPublic);

            MethodInfo Ingredients_Add = IngredientsType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            FieldInfo TechData_techTypeField = TechDataType.GetField("_techType", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo TechData_craftAmountField = TechDataType.GetField("_craftAmount", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo TechData_ingredientsField = TechDataType.GetField("_ingredients", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo TechData_linkedItemsField = TechDataType.GetField("_linkedItems", BindingFlags.Public | BindingFlags.Instance);

            FieldInfo CraftData_techdata = CraftDataType.GetField("techData", BindingFlags.NonPublic | BindingFlags.Static);
            object techData = CraftData_techdata.GetValue(null);
            Type techDataType = techData.GetType();

            MethodInfo techData_Add = techDataType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo techData_Contains = techDataType.GetMethod("ContainsKey", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo techData_Remove = techDataType.GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);

            short added = 0;
            short replaced = 0;
            foreach (TechType techType in CustomTechData.Keys)
            {
                ITechData smlTechData = CustomTechData[techType];

                object techDataInstance = Activator.CreateInstance(TechDataType, true);

                TechData_techTypeField.SetValue(techDataInstance, techType);

                TechData_craftAmountField.SetValue(techDataInstance, smlTechData.craftAmount);

                object ingredientsList = Activator.CreateInstance(IngredientsType, true);

                if (smlTechData.ingredientCount > 0)
                {
                    for (int i = 0; i < smlTechData.ingredientCount; i++)
                    {
                        IIngredient smlIngredient = smlTechData.GetIngredient(i);

                        object ingredient = Activator.CreateInstance(IngredientType, new object[] { smlIngredient.techType, smlIngredient.amount });

                        Ingredients_Add.Invoke(ingredientsList, new object[] { smlIngredient.techType, smlIngredient.amount });
                    }

                    TechData_ingredientsField.SetValue(techDataInstance, ingredientsList);
                }

                if (smlTechData.linkedItemCount > 0)
                {
                    var linkedItems = new List<TechType>();
                    for (int l = 0; l < smlTechData.linkedItemCount; l++)
                    {
                        linkedItems.Add(smlTechData.GetLinkedItem(l));
                    }

                    TechData_linkedItemsField.SetValue(techDataInstance, linkedItems);
                }

                bool techDataExists = (bool)techData_Contains.Invoke(techData, new object[] { techType });

                if (techDataExists)
                {
                    techData_Remove.Invoke(techData, new object[] { techType });
                    Logger.Log($"{techType} TechType already existed in the CraftData.techData dictionary. Original value was replaced.", LogLevel.Warn);
                    replaced++;
                }
                else
                {
                    added++;
                }

                techData_Add.Invoke(techData, new object[] { techType, techDataInstance });
            }

            Logger.Log($"Added {added} new entries to the CraftData.techData dictionary.", LogLevel.Info);

            if (replaced > 0)
                Logger.Log($"Replaced {replaced} existing entries to the CraftData.techData dictionary.", LogLevel.Info);
        }

        #endregion
    }
}
