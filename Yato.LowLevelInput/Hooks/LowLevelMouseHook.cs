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
            CaptureMouseMove = false;
        }

        public LowLevelMouseHook(bool captureMouseMove)
        {
            lockObject = new object();
            CaptureMouseMove = captureMouseMove;
        }

        ~LowLevelMouseHook()
        {
            Dispose(false);
        }

        public delegate void MouseEventCallback(KeyState state, VirtualKeyCode key, int x, int y);

        public event MouseEventCallback OnMouseEvent;

        public bool CaptureMouseMove { get; set; }

        public bool IsLeftMouseButtonPressed { get; set; }
        public bool IsMiddleMouseButtonPressed { get; set; }
        public bool IsRightMouseButtonPressed { get; set; }
        public bool IsXButton1Pressed { get; set; }
        public bool IsXButton2Pressed { get; set; }

        private void Hook_OnHookCalled(IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;

            IsMiddleMouseButtonPressed = false; // important to reset here

            WindowsMessage msg = (WindowsMessage)((uint)wParam.ToInt32());

            int x = Marshal.ReadInt32(lParam);
            int y = Marshal.ReadInt32(lParam + 4);

            int mouseData = Marshal.ReadInt32(lParam + 8);

            switch (msg)
            {
                case WindowsMessage.WM_LBUTTONDOWN:
                    break;

                case WindowsMessage.WM_LBUTTONUP:
                    break;

                case WindowsMessage.WM_MBUTTONDOWN:
                case WindowsMessage.WM_MBUTTONDBLCLK:
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