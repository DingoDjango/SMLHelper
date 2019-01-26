namespace SMLHelper.V2.Patchers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Harmony;
    using SMLHelper.V2.Utility;

    internal class LanguagePatcher
    {
        private static readonly Dictionary<string, string> customLines = new Dictionary<string, string>();

        private static Type languageType = typeof(Language);

        internal static void Postfix(ref Language __instance)
        {
            FieldInfo stringsField = languageType.GetField("strings", BindingFlags.NonPublic | BindingFlags.Instance);
            var strings = stringsField.GetValue(__instance) as Dictionary<string, string>;
            foreach (KeyValuePair<string, string> a in customLines)
            {
                strings[a.Key] = a.Value;
            }
        }

        internal static void Patch(HarmonyInstance harmony)
        {
            LanguageOverride.HandleLanguageOverrides(customLines);

            harmony.Patch(
                original: languageType.GetMethod("LoadLanguageFile", BindingFlags.NonPublic | BindingFlags.Instance),
                prefix: null,
                postfix: new HarmonyMethod(typeof(LanguagePatcher).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)));

            Logger.Log("LanguagePatcher is done.");
        }

        internal static void AddCustomLanguageLine(string modAssemblyName, string lineId, string text)
        {
            LanguageOverride.AddOriginalLanguageLine(modAssemblyName, lineId, text);
            customLines[lineId] = text;
        }
    }
}
