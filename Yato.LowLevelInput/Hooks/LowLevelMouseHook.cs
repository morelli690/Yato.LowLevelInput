using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            ClearInjectedFlag = false;
        }

        public LowLevelMouseHook(bool captureMouseMove)
        {
            lockObject = new object();
            CaptureMouseMove = captureMouseMove;
            ClearInjectedFlag = false;
        }

        public LowLevelMouseHook(bool captureMouseMove, bool clearInjectedFlag)
        {
            lockObject = new object();
            CaptureMouseMove = captureMouseMove;
            ClearInjectedFlag = clearInjectedFlag;
        }

        ~LowLevelMouseHook()
        {
            Dispose(false);
        }

        public delegate void MouseEventCallback(KeyState state, VirtualKeyCode key, int x, int y);

        public event MouseEventCallback OnMouseEvent;

        public bool CaptureMouseMove { get; set; }
        public bool ClearInjectedFlag { get; set; }
        public bool IsLeftMouseButtonPressed { get; private set; }
        public bool IsMiddleMouseButtonPressed { get; private set; }
        public bool IsRightMouseButtonPressed { get; private set; }
        public bool IsXButton1Pressed { get; private set; }
        public bool IsXButton2Pressed { get; private set; }

        private void Global_OnProcessExit()
        {
            Dispose();
        }

        private void Global_OnUnhandledException()
        {
            Dispose();
        }

        private int HIWORD(int number)
        {
            return (int)BitConverter.ToInt16(BitConverter.GetBytes(number), 2);
        }

        private void Hook_OnHookCalled(IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;

            IsMiddleMouseButtonPressed = false; // important to reset here

            WindowsMessage msg = (WindowsMessage)((uint)wParam.ToInt32());

            int x = Marshal.ReadInt32(lParam);
            int y = Marshal.ReadInt32(lParam + 4);

            int mouseData = Marshal.ReadInt32(lParam + 8);

            if (ClearInjectedFlag)
            {
                Marshal.WriteInt32(lParam + 12, 0);
            }

            switch (msg)
            {
                case WindowsMessage.WM_LBUTTONDBLCLK:
                case WindowsMessage.WM_NCLBUTTONDBLCLK:
                    IsLeftMouseButtonPressed = true;

                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.LBUTTON, x, y);

                    IsLeftMouseButtonPressed = false;

                    InvokeEventListeners(KeyState.Up, VirtualKeyCode.LBUTTON, x, y);
                    break;

                case WindowsMessage.WM_LBUTTONDOWN:
                case WindowsMessage.WM_NCLBUTTONDOWN:
                    IsLeftMouseButtonPressed = true;
                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.LBUTTON, x, y);
                    break;

                case WindowsMessage.WM_LBUTTONUP:
                case WindowsMessage.WM_NCLBUTTONUP:
                    IsLeftMouseButtonPressed = false;
                    InvokeEventListeners(KeyState.Up, VirtualKeyCode.LBUTTON, x, y);
                    break;

                case WindowsMessage.WM_MBUTTONDOWN:
                case WindowsMessage.WM_NCMBUTTONDOWN:
                    IsMiddleMouseButtonPressed = true;
                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                    break;

                case WindowsMessage.WM_MBUTTONDBLCLK:
                case WindowsMessage.WM_NCMBUTTONDBLCLK:
                    IsMiddleMouseButtonPressed = true;

                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);

                    IsMiddleMouseButtonPressed = false;

                    InvokeEventListeners(KeyState.Up, VirtualKeyCode.MBUTTON, x, y);
                    break;

                case WindowsMessage.WM_RBUTTONDBLCLK:
                case WindowsMessage.WM_NCRBUTTONDBLCLK:
                    IsRightMouseButtonPressed = true;

                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.RBUTTON, x, y);

                    IsRightMouseButtonPressed = false;

                    InvokeEventListeners(KeyState.Up, VirtualKeyCode.RBUTTON, x, y);
                    break;

                case WindowsMessage.WM_RBUTTONDOWN:
                case WindowsMessage.WM_NCRBUTTONDOWN:
                    IsRightMouseButtonPressed = true;

                    InvokeEventListeners(KeyState.Down, VirtualKeyCode.RBUTTON, x, y);
                    break;

                case WindowsMessage.WM_RBUTTONUP:
                case WindowsMessage.WM_NCRBUTTONUP:
                    IsRightMouseButtonPressed = false;

                    InvokeEventListeners(KeyState.Up, VirtualKeyCode.RBUTTON, x, y);
                    break;

                case WindowsMessage.WM_XBUTTONDBLCLK:
                case WindowsMessage.WM_NCXBUTTONDBLCLK:
                    if (HIWORD(mouseData) == 0x1)
                    {
                        IsXButton1Pressed = true;

                        InvokeEventListeners(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);

                        IsXButton1Pressed = false;

                        InvokeEventListeners(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                    }
                    else
                    {
                        IsXButton2Pressed = true;

                        InvokeEventListeners(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);

                        IsXButton2Pressed = false;

                        InvokeEventListeners(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                    }
                    break;

                case WindowsMessage.WM_XBUTTONDOWN:
                case WindowsMessage.WM_NCXBUTTONDOWN:
                    if (HIWORD(mouseData) == 0x1)
                    {
                        IsXButton1Pressed = true;

                        InvokeEventListeners(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);
                    }
                    else
                    {
                        IsXButton2Pressed = true;

                        InvokeEventListeners(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);
                    }
                    break;

                case WindowsMessage.WM_XBUTTONUP:
                case WindowsMessage.WM_NCXBUTTONUP:
                    if (HIWORD(mouseData) == 0x1)
                    {
                        IsXButton1Pressed = false;

                        InvokeEventListeners(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                    }
                    else
                    {
                        IsXButton2Pressed = false;

                        InvokeEventListeners(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                    }
                    break;

                case WindowsMessage.WM_MOUSEWHEEL:
                case WindowsMessage.WM_MOUSEHWHEEL:
                    InvokeEventListeners(KeyState.None, VirtualKeyCode.SCROLL, HIWORD(mouseData), HIWORD(mouseData));

                    break;

                case WindowsMessage.WM_MOUSEMOVE:
                case WindowsMessage.WM_NCMOUSEMOVE:
                    if (CaptureMouseMove)
                    {
                        InvokeEventListeners(KeyState.None, VirtualKeyCode.INVALID, x, y);
                    }
                    break;
            }
        }

        private void InvokeEventListeners(KeyState state, VirtualKeyCode key, int x, int y)
        {
            Task.Factory.StartNew(() =>
            {
                OnMouseEvent?.Invoke(state, key, x, y);
            });
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

            Global.OnProcessExit += Global_OnProcessExit;
            Global.OnUnhandledException += Global_OnUnhandledException;

            return true;
        }

        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hook == null) return false;

                Global.OnProcessExit -= Global_OnProcessExit;
                Global.OnUnhandledException -= Global_OnUnhandledException;

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