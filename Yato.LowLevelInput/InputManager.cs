using System;
using System.Collections.Generic;

using Yato.LowLevelInput.Converters;
using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    public class InputManager : IDisposable
    {
        private object lockObject;

        private LowLevelKeyboardHook keyboardHook;
        private LowLevelMouseHook mouseHook;

        private Dictionary<VirtualKeyCode, bool> mapIsPressed;

        private Dictionary<VirtualKeyCode, CallbackContainer> singleKeyCallback;

        public delegate void KeyStateChangedCallback(KeyState state, VirtualKeyCode keyCode);

        public event LowLevelKeyboardHook.KeyboardEventCallback OnKeyboardEvent;

        public event LowLevelMouseHook.MouseEventCallback OnMouseEvent;

        public bool CaptureMouseMove
        {
            get
            {
                if (mouseHook == null) return false;

                return mouseHook.CaptureMouseMove;
            }
            set
            {
                if (mouseHook == null) return;

                mouseHook.CaptureMouseMove = value;
            }
        }

        public bool ClearInjectedFlag
        {
            get
            {
                if (keyboardHook != null) return keyboardHook.ClearInjectedFlag;
                if (mouseHook != null) return mouseHook.ClearInjectedFlag;

                return false;
            }
            set
            {
                if (keyboardHook != null) keyboardHook.ClearInjectedFlag = value;
                if (mouseHook != null) mouseHook.ClearInjectedFlag = value;
            }
        }

        public InputManager()
        {
            Initialize(false, false);
        }

        public InputManager(bool captureMouseMove)
        {
            Initialize(captureMouseMove, false);
        }

        public InputManager(bool captureMouseMove, bool clearInjectedFlag)
        {
            Initialize(captureMouseMove, clearInjectedFlag);
        }

        ~InputManager()
        {
            Dispose(false);
        }

        private void Initialize(bool captureMouseMove, bool clearInjectedFlag)
        {
            // initialize vars
            mapIsPressed = new Dictionary<VirtualKeyCode, bool>();

            foreach (var pair in KeyCodeConverter.EnumerateVirtualKeyCodes())
            {
                mapIsPressed.Add(pair.Key, false);
            }

            singleKeyCallback = new Dictionary<VirtualKeyCode, CallbackContainer>();

            // initialize hooks
            keyboardHook = new LowLevelKeyboardHook(clearInjectedFlag);
            mouseHook = new LowLevelMouseHook(captureMouseMove, clearInjectedFlag);

            keyboardHook.OnKeyboardEvent += KeyboardHook_OnKeyboardEvent;
            mouseHook.OnMouseEvent += MouseHook_OnMouseEvent;

            keyboardHook.InstallHook();
            mouseHook.InstallHook();
        }

        private void MouseHook_OnMouseEvent(KeyState state, VirtualKeyCode key, int x, int y)
        {
            if (OnMouseEvent != null)
            {
                Global.StartNewTask(() =>
                {
                    OnMouseEvent?.Invoke(state, key, x, y);
                });
            }

            if (mapIsPressed.ContainsKey(key))
            {
                mapIsPressed[key] = state == KeyState.Down ? true : false;
            }

            var currentKeyCallbackDict = singleKeyCallback;

            if (currentKeyCallbackDict != null && currentKeyCallbackDict.Count != 0)
            {
                if (currentKeyCallbackDict.ContainsKey(key))
                {
                    currentKeyCallbackDict[key].Invoke(state, key);
                }
            }
        }

        private void KeyboardHook_OnKeyboardEvent(KeyState state, VirtualKeyCode key)
        {
            if (OnKeyboardEvent != null)
            {
                Global.StartNewTask(() =>
                {
                    OnKeyboardEvent?.Invoke(state, key);
                });
            }

            if (mapIsPressed.ContainsKey(key))
            {
                mapIsPressed[key] = state == KeyState.Down ? true : false;
            }

            var currentKeyCallbackDict = singleKeyCallback;

            if (currentKeyCallbackDict != null && currentKeyCallbackDict.Count != 0)
            {
                if (currentKeyCallbackDict.ContainsKey(key))
                {
                    currentKeyCallbackDict[key].Invoke(state, key);
                }
            }
        }

        public bool IsPressed(VirtualKeyCode key)
        {
            if (!mapIsPressed.ContainsKey(key)) return false;

            return mapIsPressed[key];
        }

        public bool RegisterEvent(VirtualKeyCode key, KeyStateChangedCallback callback)
        {
            if (key == VirtualKeyCode.INVALID) return false;
            if (callback == null) return false;

            lock (lockObject)
            {
                if (!singleKeyCallback.ContainsKey(key)) singleKeyCallback.Add(key, new CallbackContainer());
            }

            singleKeyCallback[key].Add(callback);

            return true;
        }

        public bool RemoveEvent(VirtualKeyCode key, KeyStateChangedCallback callback)
        {
            if (key == VirtualKeyCode.INVALID) return false;
            if (callback == null) return false;

            if (!singleKeyCallback.ContainsKey(key)) return false;

            singleKeyCallback[key].Remove(callback);

            return false;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (keyboardHook != null) keyboardHook.Dispose();
                    if (mouseHook != null) mouseHook.Dispose();

                    mapIsPressed = null;
                    singleKeyCallback = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}