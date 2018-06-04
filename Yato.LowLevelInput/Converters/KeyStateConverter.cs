﻿using System;

using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput.Converters
{
    /// <summary>
    /// </summary>
    public static class KeyStateConverter
    {
        private static string[] keyStateMap = new string[]
        {
            "None",
            "Up",
            "Down"
        };

        /// <summary>
        /// To the state of the key.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static KeyState ToKeyState(string name)
        {
            if (string.IsNullOrEmpty(name)) return KeyState.None;
            if (string.IsNullOrWhiteSpace(name)) return KeyState.None;

            string tmp = name.ToLower();

            for (int i = 0; i < keyStateMap.Length; i++)
            {
                if (tmp == keyStateMap[i].ToLower()) return (KeyState)i;
            }

            return KeyState.None;
        }

        /// <summary>
        /// To the state of the key.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        public static KeyState ToKeyState(int state)
        {
            if (state < 0) return KeyState.None;
            if (state >= keyStateMap.Length) return KeyState.None;

            return (KeyState)state;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public static string ToString(KeyState state)
        {
            int index = (int)state;

            if (index < 0) return "None";
            if (index >= keyStateMap.Length) return "None";

            return keyStateMap[index];
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public static string ToString(int index)
        {
            if (index < 0) return "None";
            if (index >= keyStateMap.Length) return "None";

            return keyStateMap[index];
        }
    }
}