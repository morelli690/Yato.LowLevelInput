using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using Yato.LowLevelInput.Converters;
using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    /// <summary>
    /// </summary>
    /// <seealso cref="System.IDisposable"/>
    public class InputManager : IDisposable
    {
        private object lockObject;

        private LowLevelKeyboardHook keyboardHook;
        private LowLevelMouseHook mouseHook;

        private Dictionary<VirtualKeyCode, KeyState> mapKeyState;

        private Dictionary<VirtualKeyCode, List<KeyStateChangedCallback>> singleKeyCallback;

        /// <summary>
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="keyCode">The key code.</param>
        public delegate void KeyStateChangedCallback(KeyState state, VirtualKeyCode keyCode);

        /// <summary>
        /// Occurs when [on keyboard event].
        /// </summary>
        public event LowLevelKeyboardHook.KeyboardEventCallback OnKeyboardEvent;

        /// <summary>
        /// Occurs when [on mouse event].
        /// </summary>
        public event LowLevelMouseHook.MouseEventCallback OnMouseEvent;

        /// <summary>
        /// Gets or sets a value indicating whether [capture mouse move].
        /// </summary>
        /// <value><c>true</c> if [capture mouse move]; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets a value indicating whether [clear injected flag].
        /// </summary>
        /// <value><c>true</c> if [clear injected flag]; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="InputManager"/> class.
        /// </summary>
        public InputManager()
        {
            Initialize(false, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputManager"/> class.
        /// </summary>
        /// <param name="captureMouseMove">if set to <c>true</c> [capture mouse move].</param>
        public InputManager(bool captureMouseMove)
        {
            Initialize(captureMouseMove, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputManager"/> class.
        /// </summary>
        /// <param name="captureMouseMove">if set to <c>true</c> [capture mouse move].</param>
        /// <param name="clearInjectedFlag">if set to <c>true</c> [clear injected flag].</param>
        public InputManager(bool captureMouseMove, bool clearInjectedFlag)
        {
            Initialize(captureMouseMove, clearInjectedFlag);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="InputManager"/> class.
        /// </summary>
        ~InputManager()
        {
            Dispose(false);
        }

        private void Initialize(bool captureMouseMove, bool clearInjectedFlag)
        {
            // initialize vars
            lockObject = new object();

            mapKeyState = new Dictionary<VirtualKeyCode, KeyState>();

            foreach (var pair in KeyCodeConverter.EnumerateVirtualKeyCodes())
            {
                mapKeyState.Add(pair.Key, KeyState.None);
            }

            singleKeyCallback = new Dictionary<VirtualKeyCode, List<KeyStateChangedCallback>>();

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
                Task.Factory.StartNew(() =>
                {
                    OnMouseEvent?.Invoke(state, key, x, y);
                });
            }

            if (mapKeyState.ContainsKey(key))
            {
                mapKeyState[key] = state == KeyState.Up && mapKeyState[key] == KeyState.Down
                    ? KeyState.Pressed
                    : state;
            }

            Task.Factory.StartNew(() =>
            {
                var currentKeyCallbackDict = singleKeyCallback; // create a temp var to avoid locking

                if (currentKeyCallbackDict != null && currentKeyCallbackDict.Count != 0)
                {
                    if (currentKeyCallbackDict.ContainsKey(key))
                    {
                        var currentList = currentKeyCallbackDict[key];

                        foreach (var callback in currentList)
                        {
                            callback(state, key);
                        }
                    }
                }
            });
        }

        private void KeyboardHook_OnKeyboardEvent(KeyState state, VirtualKeyCode key)
        {
            if (OnKeyboardEvent != null)
            {
                Task.Factory.StartNew(() =>
                {
                    OnKeyboardEvent?.Invoke(state, key);
                });
            }

            if (mapKeyState.ContainsKey(key))
            {
                mapKeyState[key] = state == KeyState.Up && mapKeyState[key] == KeyState.Down
                    ? KeyState.Pressed
                    : state;
            }

            Task.Factory.StartNew(() =>
            {
                var currentKeyCallbackDict = singleKeyCallback; // create a temp var to avoid locking

                if (currentKeyCallbackDict != null && currentKeyCallbackDict.Count != 0)
                {
                    if (currentKeyCallbackDict.ContainsKey(key))
                    {
                        var currentList = currentKeyCallbackDict[key];

                        foreach (var callback in currentList)
                        {
                            callback(state, key);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Determines whether the specified key is pressed.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key is pressed; otherwise, <c>false</c>.</returns>
        public bool IsPressed(VirtualKeyCode key)
        {
            return this.GetState(key) == KeyState.Down;
        }

        /// <summary>
        /// Determines whether the specified key was pressed.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key was pressed; otherwise, <c>false</c>.</returns>
        public bool WasPressed(VirtualKeyCode key)
        {
            if (this.GetState(key) != KeyState.Pressed)
                return false;

            mapKeyState[key] = KeyState.Up;

            return true;
        }

        /// <summary>
        /// Returns the current state of a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>KeyState</c>.</returns>
        public KeyState GetState(VirtualKeyCode key)
        {
            return mapKeyState[key];
        }

        /// <summary>
        /// Registers the event.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="callback">The callback.</param>
        /// <returns></returns>
        public bool RegisterEvent(VirtualKeyCode key, KeyStateChangedCallback callback)
        {
            if (key == VirtualKeyCode.INVALID) return false;
            if (callback == null) return false;

            lock (lockObject)
            {
                if (!singleKeyCallback.ContainsKey(key))
                {
                    singleKeyCallback.Add(key, new List<KeyStateChangedCallback>());
                }
            }

            singleKeyCallback[key].Add(callback);

            return true;
        }

        /// <summary>
        /// Removes the event.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="callback">The callback.</param>
        /// <returns></returns>
        public bool RemoveEvent(VirtualKeyCode key, KeyStateChangedCallback callback)
        {
            if (key == VirtualKeyCode.INVALID) return false;
            if (callback == null) return false;

            if (!singleKeyCallback.ContainsKey(key)) return false;

            singleKeyCallback[key].Remove(callback);

            return false;
        }

        /// <summary>
        /// Waits for key event.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="state">The state.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public bool WaitForKeyEvent(VirtualKeyCode key, KeyState state = KeyState.Down, int timeout = -1)
        {
            if (key == VirtualKeyCode.INVALID) return false;
            if (state == KeyState.None) return false;

            object threadLock = new object();

            KeyStateChangedCallback callback = (KeyState keystate, VirtualKeyCode keycode) =>
            {
                if (keystate != state) return;
                if (keycode != key) return;

                if (Monitor.TryEnter(threadLock))
                {
                    // someone else has the lock
                    Monitor.PulseAll(threadLock);
                    Monitor.Exit(threadLock);
                }
            };

            if (!RegisterEvent(key, callback)) return false;

            bool result = false;

            Monitor.Enter(threadLock);

            if (timeout < 0)
            {
                Monitor.Wait(threadLock);
                result = true;
            }
            else
            {
                result = Monitor.Wait(threadLock, timeout);
            }

            Monitor.Exit(threadLock);

            RemoveEvent(key, callback);

            return result;
        }

        #region IDisposable Support

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (keyboardHook != null) keyboardHook.Dispose();
                    if (mouseHook != null) mouseHook.Dispose();

                    mapKeyState = null;
                    singleKeyCallback = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}