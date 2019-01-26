﻿namespace SMLHelper.V2.Options
{
    using SMLHelper.V2.Utility;
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// Contains all the information about a keybind changed event.
    /// </summary>
    public class KeybindChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the <see cref="ModKeybindOption"/> that was changed.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The new value for the <see cref="ModKeybindOption"/>.
        /// </summary>
        public KeyCode Key { get; }

        /// <summary>
        /// The new value for the <see cref="ModKeybindOption"/> parsed as a <see cref="string"/>
        /// </summary>
        public string KeyName { get; }

        public KeybindChangedEventArgs(string id, KeyCode key)
        {
            Id = id;
            Key = key;
            KeyName = KeyCodeUtils.KeyCodeToString(key);
        }
    }

    public abstract partial class ModOptions
    {
        /// <summary>
        /// The event that is called whenever a keybind is changed. Subscribe to this in the constructor.
        /// </summary>
        protected event EventHandler<KeybindChangedEventArgs> KeybindChanged;

        /// <summary>
        /// Notifies a keybind change to all subscribed event handlers.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="key"></param>
        internal void OnKeybindChange(string id, KeyCode key)
        {
            KeybindChanged(this, new KeybindChangedEventArgs(id, key));
        }

        /// <summary>
        /// Adds a new <see cref="ModKeybindOption"/> to this instance.
        /// </summary>
        /// <param name="id">The internal ID for the toggle option.</param>
        /// <param name="label">The display text to use in the in-game menu.</param>
        /// <param name="device">The device name.</param>
        /// <param name="key">The starting keybind value.</param>
        protected void AddKeybindOption(string id, string label, GameInput.Device device, KeyCode key)
        {
            AddOriginalLabel(id, label);
            _options.Add(id, new ModKeybindOption(id, label, device, key));
        }
        /// <summary>
        /// Adds a new <see cref="ModKeybindOption"/> to this instance.
        /// </summary>
        /// <param name="id">The internal ID for the toggle option.</param>
        /// <param name="label">The display text to use in the in-game menu.</param>
        /// <param name="device">The device name.</param>
        /// <param name="key">The starting keybind value.</param>
        protected void AddKeybindOption(string id, string label, GameInput.Device device, string key)
        {
            AddKeybindOption(id, label, device, KeyCodeUtils.StringToKeyCode(key));
        }
    }

    /// <summary>
    /// A mod option class for handling an option that is a keybind.
    /// </summary>
    public class ModKeybindOption : ModOption
    {
        public KeyCode Key { get; }
        public GameInput.Device Device { get; }

        /// <summary>
        /// Instantiates a new <see cref="ModKeybindOption"/> for handling an option that is a keybind.
        /// </summary>
        /// <param name="id">The internal ID of this option.</param>
        /// <param name="label">The display text to show on the in-game menus.</param>
        /// <param name="device">The device name.</param>
        /// <param name="key">The starting keybind value.</param>
        internal ModKeybindOption(string id, string label, GameInput.Device device, KeyCode key) : base(ModOptionType.Keybind, label, id)
        {
            Device = device;
            Key = key;
        }

        internal static GameObject AddBindingOptionWithCallback(uGUI_OptionsPanel panel, int tab, string label, KeyCode key, GameInput.Device device, UnityAction<KeyCode> callback)
        {
            // Add item
            GameObject gameObject = panel.AddItem(tab, panel.bindingOptionPrefab);

            // Update text
            Text text = gameObject.GetComponentInChildren<Text>();
            if (text != null)
            {
                gameObject.GetComponentInChildren<TranslationLiveUpdate>().translationKey = label;
                text.text = Language.main.Get(label);
                //text.text = label;
            }

            // Create bindings
            uGUI_Bindings bindings = gameObject.GetComponentInChildren<uGUI_Bindings>();
            uGUI_Binding binding = bindings.bindings.First();

            // Destroy secondary bindings
            UnityEngine.Object.Destroy(bindings.bindings.Last().gameObject);
            UnityEngine.Object.Destroy(bindings);

            // Update bindings
            binding.device = device;
            binding.value = KeyCodeUtils.KeyCodeToString(key);
            binding.onValueChanged.RemoveAllListeners();
            binding.onValueChanged.AddListener(new UnityAction<string>((string s) => callback?.Invoke(KeyCodeUtils.StringToKeyCode(s))));

            return gameObject;
        }
    }
}
