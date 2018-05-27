using System;
using System.Runtime.InteropServices;

using Yato.LowLevelInput.PInvoke;
using Yato.LowLevelInput.WindowsHooks;

namespace Yato.LowLevelInput.Hooks
{
    public class LowLevelMouseHook : IDisposable
    {
        private WindowsHook hook;
        private object lockObject;

        public LowLevelMouseHook()
        {
            lockObject = new object();
        }

        ~LowLevelMouseHook()
        {
            Dispose(false);
        }

        public delegate void MouseEventCallback(KeyState state, VirtualKeyCode key);

        public event MouseEventCallback OnMouseEvent;

        private void Hook_OnHookCalled(IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;
            if (OnMouseEvent == null) return;

            WindowsMessage msg = (WindowsMessage)((uint)wParam.ToInt32());

            VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);

            switch (msg)
            {
                case WindowsMessage.WM_KEYDOWN:
                    OnMouseEvent?.Invoke(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_KEYUP:
                    OnMouseEvent?.Invoke(KeyState.Up, key);
                    break;

                case WindowsMessage.WM_SYSKEYDOWN:
                    OnMouseEvent?.Invoke(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_SYSKEYUP:
                    OnMouseEvent?.Invoke(KeyState.Up, key);
                    break;
            }
        }

        private void ProcessEvents_OnProcessExit(System.Diagnostics.Process process)
        {
            Dispose();
        }

        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hook != null) return false;

                hook = new WindowsHook(WindowsHookType.LowLevelMouse);
            }

            hook.OnHookCalled += Hook_OnHookCalled;

            hook.InstallHook();

            ProcessEvents.OnProcessExit += ProcessEvents_OnProcessExit;

            return true;
        }

        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hook == null) return false;

                ProcessEvents.OnProcessExit -= ProcessEvents_OnProcessExit;

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