﻿namespace SMLHelper.V2.Patchers
{
    using Harmony;
    using Options;
    using SMLHelper.V2.Utility;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Events;

    internal class OptionsPanelPatcher
    {
        internal static List<ModOptions> modOptions = new List<ModOptions>();

        internal static void Patch(HarmonyInstance harmony)
        {
            Type uGUI_OptionsPanelType = typeof(uGUI_OptionsPanel);
            Type thisType = typeof(OptionsPanelPatcher);
            MethodInfo startMethod = uGUI_OptionsPanelType.GetMethod("AddTabs", BindingFlags.NonPublic | BindingFlags.Instance);

            harmony.Patch(startMethod, null, new HarmonyMethod(thisType.GetMethod("AddTabs_Postfix", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        internal static void AddTabs_Postfix(uGUI_OptionsPanel __instance)
        {
            uGUI_OptionsPanel optionsPanel = __instance;
            int modsTab = optionsPanel.AddTab("Mods");

            foreach (ModOptions modOption in modOptions)
            {
                LanguageOverride.HandleLanguageOverrides(modOption.LanguageLines);
                modOption.UpdateLanguageLabels();

                optionsPanel.AddHeading(modsTab, modOption.Name);

                foreach (ModOption option in modOption.Options)
                {
                    switch (option.Type)
                    {
                        case ModOptionType.Slider:
                            var slider = (ModSliderOption)option;

                            optionsPanel.AddSliderOption(modsTab, slider.Label, slider.Value, slider.MinValue, slider.MaxValue, slider.Value,
                                new UnityAction<float>((float sliderVal) =>
                                    modOption.OnSliderChange(slider.Id, sliderVal)));
                            break;
                        case ModOptionType.Toggle:
                            var toggle = (ModToggleOption)option;

                            optionsPanel.AddToggleOption(modsTab, toggle.Label, toggle.Value,
                                new UnityAction<bool>((bool toggleVal) =>
                                    modOption.OnToggleChange(toggle.Id, toggleVal)));
                            break;
                        case ModOptionType.Choice:
                            var choice = (ModChoiceOption)option;

                            optionsPanel.AddChoiceOption(modsTab, choice.Label, choice.Options, choice.Index,
                                new UnityAction<int>((int index) =>
                                    modOption.OnChoiceChange(choice.Id, index, choice.Options[index])));
                            break;
                        case ModOptionType.Keybind:
                            var keybind = (ModKeybindOption)option;

                            ModKeybindOption.AddBindingOptionWithCallback(optionsPanel, modsTab, keybind.Label, keybind.Key, keybind.Device,
                                new UnityAction<KeyCode>((KeyCode key) => 
                                    modOption.OnKeybindChange(keybind.Id, key)));
                            break;
                        default:
                            V2.Logger.Log($"Invalid ModOptionType detected for option: {option.Id} ({option.Type.ToString()})");
                            break;
                    }
                }
            }
        }
    }
}
