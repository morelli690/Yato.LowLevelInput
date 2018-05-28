using System;
using System.Collections.Generic;

using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    internal class CallbackContainer
    {
        private object lockObject;

        private event InputManager.KeyStateChangedCallback OnCallbackEvent;

        public CallbackContainer()
        {
            lockObject = new object();
        }

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

        public void Invoke(KeyState state, VirtualKeyCode keyCode)
        {
            OnCallbackEvent?.Invoke(state, keyCode);
        }
    }
}