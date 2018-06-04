using System;
using System.Collections.Generic;

using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    internal class CallbackContainer
    {
        private object lockObject;

        private event InputManager.KeyStateChangedCallback OnCallbackEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallbackContainer"/> class.
        /// </summary>
        public CallbackContainer()
        {
            lockObject = new object();
        }

        /// <summary>
        /// Adds the specified callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void Add(InputManager.KeyStateChangedCallback callback)
        {
            lock (lockObject)
            {
                try
                {
                    OnCallbackEvent += callback;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Removes the specified callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void Remove(InputManager.KeyStateChangedCallback callback)
        {
            lock (lockObject)
            {
                try
                {
                    OnCallbackEvent -= callback;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Invokes the specified state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="keyCode">The key code.</param>
        public void Invoke(KeyState state, VirtualKeyCode keyCode)
        {
            OnCallbackEvent?.Invoke(state, keyCode);
        }
    }
}