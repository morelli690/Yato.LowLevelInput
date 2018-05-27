using System;
using System.Runtime.InteropServices;

using Yato.LowLevelInput.PInvoke;
using Yato.LowLevelInput.WindowsHooks;

namespace Yato.LowLevelInput.Hooks
{
    public class LowLevelKeyboardHook : IDisposable
    {
        private WindowsHook hook;
        private object lockObject;

        public LowLevelKeyboardHook()
        {
            lockObject = new object();
        }

        ~LowLevelKeyboardHook()
        {
            Dispose(false);
        }

        public delegate void KeyboardEventCallback(KeyState state, VirtualKeyCode key);

        public event KeyboardEventCallback OnKeyboardEvent;

        private void Hook_OnHookCalled(IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;
            if (OnKeyboardEvent == null) return;

            WindowsMessage msg = (WindowsMessage)((uint)wParam.ToInt32());

            VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);

            switch (msg)
            {
                case WindowsMessage.WM_KEYDOWN:
                    OnKeyboardEvent?.Invoke(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_KEYUP:
                    OnKeyboardEvent?.Invoke(KeyState.Up, key);
                    break;

                case WindowsMessage.WM_SYSKEYDOWN:
                    OnKeyboardEvent?.Invoke(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_SYSKEYUP:
                    OnKeyboardEvent?.Invoke(KeyState.Up, key);
                    break;
            }
        }

        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hook != null) return false;

                hook = new WindowsHook(WindowsHookType.LowLevelKeyboard);

                hook.OnHookCalled += Hook_OnHookCalled;

                hook.InstallHook();

                return true;
            }
        }

        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hook == null) return false;

                hook.OnHookCalled -= Hook_OnHookCalled;

                hook.UninstallHook();

                hook.Dispose();

                hook = null;

                return true;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                UninstallHook();

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