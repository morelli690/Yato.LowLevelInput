using System;
using System.Threading;
using System.Collections.Generic;

namespace Yato.Input
{
    public class InputHandler : IDisposable
    {
        private LowLevelKeyboardHook keyboardHook;
        private LowLevelMouseHook mouseHook;

        private VirtualKeyCode nextEventResult = VirtualKeyCode.NONAME;
        private object nextEventResultLock = new object();

        private object namedEventLock = new object();
        private List<VirtualKeyCode> namedEventList = new List<VirtualKeyCode>();
        private List<object> namedEventMonitor = new List<object>();

        public delegate void InputHandlerCallback(KeyState state, VirtualKeyCode key, int x, int y);
        public event InputHandlerCallback OnInputCaptured;

        public bool CaptureMouseMove
        {
            get
            {
                return mouseHook.CaptureMouseMove;
            }
            set
            {
                mouseHook.CaptureMouseMove = value;
            }
        }

        public InputHandler()
        {
            Initialize();
        }

        public InputHandler(bool captureMouseMove = true)
        {
            Initialize(captureMouseMove);
        }

        ~InputHandler()
        {
            Dispose(false);
        }

        public VirtualKeyCode WaitForNextEvent(int timeout = -1)
        {
            if (timeout < -1) timeout *= -1;

            Monitor.Enter(nextEventResultLock);

            if (timeout == -1)
            {
                Monitor.Wait(nextEventResultLock);
            }
            else
            {
                Monitor.Wait(nextEventResultLock, timeout);
            }

            Monitor.Exit(nextEventResultLock);

            return nextEventResult;
        }

        public int RegisterNamedEvent(VirtualKeyCode key)
        {
            lock(namedEventLock)
            {
                if (namedEventList.Contains(key)) return namedEventList.IndexOf(key);

                namedEventList.Add(key);
                namedEventMonitor.Add(new object());

                return namedEventList.Count - 1;
            }
        }

        public void UnregisterNamedEvent(VirtualKeyCode key)
        {
            lock(namedEventLock)
            {
                int index = namedEventList.IndexOf(key);

                if (index == -1) return;

                namedEventList.RemoveAt(index);
                namedEventMonitor.RemoveAt(index);
            }
        }

        public void UnregisterNamedEvent(int index)
        {
            lock(namedEventLock)
            {
                if (index < 0) return;
                if (index >= namedEventList.Count) return;

                namedEventList.RemoveAt(index);
                namedEventMonitor.RemoveAt(index);
            }
        }

        public void ChangeNamedEvent(int eventIndex, VirtualKeyCode key)
        {
            if (eventIndex < 0) return;

            lock (namedEventLock)
            {
                if (eventIndex >= namedEventList.Count) return;

                namedEventList[eventIndex] = key;
            }
        }

        public VirtualKeyCode GetNamedEvent(int eventIndex)
        {
            if (eventIndex < 0) return VirtualKeyCode.NONAME;

            lock (namedEventLock)
            {
                if (eventIndex >= namedEventList.Count) return VirtualKeyCode.NONAME;

                return namedEventList[eventIndex];
            }
        }

        public bool WaitForNamedEvent(int eventIndex, int timeout = 1000)
        {
            if (eventIndex < 0) return false;
            if (eventIndex >= namedEventList.Count) return false;

            // the mouse hook does not receive multiple down or up events
            var key = namedEventList[eventIndex];

            switch(key)
            {
                case VirtualKeyCode.LBUTTON:
                    if (mouseHook.IsLeftMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.RBUTTON:
                    if (mouseHook.IsRightMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.MBUTTON:
                    if (mouseHook.IsMiddleMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.XBUTTON1:
                    if (mouseHook.IsXButton1Pressed) return true;
                    break;
                case VirtualKeyCode.XBUTTON2:
                    if (mouseHook.IsXButton2Pressed) return true;
                    break;
            }

            if (timeout < -1) timeout *= -1;

            bool result = false;

            Monitor.Enter(namedEventMonitor[eventIndex]);

            if(timeout == -1)
            {
                Monitor.Wait(namedEventMonitor[eventIndex]);
                result = true;
            }
            else
            {
                result = Monitor.Wait(namedEventMonitor[eventIndex], timeout);
            }

            return result;
        }

        private void Initialize(bool captureMouseMove = true)
        {
            keyboardHook = new LowLevelKeyboardHook();
            mouseHook = new LowLevelMouseHook();

            mouseHook.CaptureMouseMove = captureMouseMove;

            keyboardHook.OnKeyCaptured += KeyboardHook_OnKeyCaptured;
            mouseHook.OnMouseCaptured += MouseHook_OnMouseCaptured;

            keyboardHook.InstallHook();
            mouseHook.InstallHook();
        }

        private void MouseHook_OnMouseCaptured(KeyState state, VirtualKeyCode key, int x, int y)
        {
            // process events before our callback because they may lock some other thread

            // WaitForNextEvent
            if (state == KeyState.Down && Monitor.TryEnter(nextEventResultLock)) // someone is waiting for a key press to occur
            {
                nextEventResult = key;

                Monitor.PulseAll(nextEventResultLock);
                Monitor.Exit(nextEventResultLock);
            }

            // WaitForNamedEvent
            if (state == KeyState.Down)
            {
                lock (namedEventLock)
                {
                    if (namedEventList.Count != 0)
                    {
                        int index = namedEventList.IndexOf(key);

                        if (index != -1)
                        {
                            Monitor.Enter(namedEventMonitor[index]);
                            Monitor.PulseAll(namedEventMonitor[index]);
                            Monitor.Exit(namedEventMonitor[index]);
                        }
                    }
                }
            }

            OnInputCaptured?.Invoke(state, key, x, y);
        }

        private void KeyboardHook_OnKeyCaptured(KeyState state, VirtualKeyCode key)
        {
            // process events before our callback because they may lock some other thread

            // WaitForNextEvent
            if (state == KeyState.Down && Monitor.TryEnter(nextEventResultLock)) // someone is waiting for a key press to occur
            {
                nextEventResult = key;

                Monitor.PulseAll(nextEventResultLock);
                Monitor.Exit(nextEventResultLock);
            }

            // WaitForNamedEvent
            if (state == KeyState.Down)
            {
                lock (namedEventLock)
                {
                    if (namedEventList.Count != 0)
                    {
                        int index = namedEventList.IndexOf(key);

                        if(index != -1)
                        {
                            Monitor.Enter(namedEventMonitor[index]);
                            Monitor.PulseAll(namedEventMonitor[index]);
                            Monitor.Exit(namedEventMonitor[index]);
                        }
                    }
                }
            }

            OnInputCaptured?.Invoke(state, key, 0, 0);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                try
                {
                    if (Monitor.TryEnter(nextEventResultLock))
                    {
                        Monitor.PulseAll(nextEventResultLock);
                        Monitor.Exit(nextEventResultLock);
                    }
                }
                catch
                {

                }

                try
                {
                    if (Monitor.TryEnter(namedEventLock))
                    {
                        Monitor.PulseAll(namedEventLock);
                        Monitor.Exit(namedEventLock);
                    }
                }
                catch
                {

                }

                for(int i = 0; i < namedEventMonitor.Count; i++)
                {
                    try
                    {
                        if(Monitor.TryEnter(namedEventMonitor[i]))
                        {
                            Monitor.PulseAll(namedEventMonitor[i]);
                            Monitor.Exit(namedEventMonitor[i]);
                        }
                    }
                    catch
                    {

                    }
                }

                if (disposing)
                {
                    namedEventMonitor = null;
                    namedEventList = null;
                }

                if (keyboardHook != null) keyboardHook.Dispose();
                if (mouseHook != null) mouseHook.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
