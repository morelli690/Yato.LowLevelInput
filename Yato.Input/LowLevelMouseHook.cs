using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    public class LowLevelMouseHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        private object lockObject;

        private PInvoke.HookProc mouseProcReference;

        private IntPtr hookHandle;

        private uint hookThreadId;
        private Thread hookThread;

        public delegate void MouseHookCallback(KeyState state, VirtualKeyCode key, int x, int y);
        public event MouseHookCallback OnMouseCaptured;

        public bool CaptureMouseMove;

        public bool IsLeftMouseButtonPressed;
        public bool IsRightMouseButtonPressed;
        public bool IsMiddleMouseButtonPressed;
        public bool IsXButton1Pressed;
        public bool IsXButton2Pressed;

        public LowLevelMouseHook()
        {
            lockObject = new object();
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

        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle != IntPtr.Zero) return false;
                if (hookThreadId != 0) return false;
                if (hookThread != null) return false;

                hookThread = new Thread(InitializeHookThread)
                {
                    IsBackground = true
                };

                hookThread.Start();

                return true;
            }
        }

        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle == IntPtr.Zero) return false;
                if (hookThreadId == 0) return false;
                if (hookThread == null) return false;

                if (PInvoke.PostThreadMessage(hookThreadId, PInvoke.WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    try
                    {
                        hookThread.Join();
                    }
                    catch
                    {
                        // thread already exited
                    }
                }

                hookHandle = IntPtr.Zero;
                hookThreadId = 0;
                hookThread = null;

                return true;
            }
        }

        private void InitializeHookThread()
        {
            lock (lockObject)
            {
                hookThreadId = PInvoke.GetCurrentThreadId();

                mouseProcReference = new PInvoke.HookProc(HookProcedure);

                IntPtr methodPtr = Marshal.GetFunctionPointerForDelegate(mouseProcReference);

                hookHandle = PInvoke.SetWindowsHookEx(PInvoke.WH_MOUSE_LL, methodPtr, MainModuleHandle, 0);

                if (hookHandle == IntPtr.Zero)
                {
                    throw new Exception("Failed to create LowLevelKeyboardHook");
                }
            }

            // we need to start a message loop here to keep the hook working

            PInvoke.Message msg = new PInvoke.Message();

            // we actually do not care on any window messages
            while (PInvoke.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
            }

            // Unhook in the same thread

            PInvoke.UnhookWindowsHookEx(hookHandle);
        }

        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0) // wParam and lParam are set
            {
                IsMiddleMouseButtonPressed = false; // reset

                uint msg = (uint)wParam.ToInt32();

                try
                {
                    if (lParam == IntPtr.Zero) return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
                }
                catch
                {

                }

                int x = Marshal.ReadInt32(lParam);
                int y = Marshal.ReadInt32(lParam + 4);

                int mouseData = Marshal.ReadInt32(lParam + 8);

                switch (msg)
                {
                    case PInvoke.WM_LBUTTONDOWN:
                        IsLeftMouseButtonPressed = true;
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case PInvoke.WM_LBUTTONUP:
                        IsLeftMouseButtonPressed = false;
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case PInvoke.WM_MOUSEHWHEEL:
                        // get the high word:
                        short hiword = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);

                        if(hiword == 120) // clicked the mouse wheel button
                        {
                            IsMiddleMouseButtonPressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;

                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword, hiword);
                        }
                        break;
                    case PInvoke.WM_MOUSEMOVE:
                        if (!CaptureMouseMove) break;
                        OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.NONAME, x, y);
                        break;
                    case PInvoke.WM_MOUSEWHEEL:
                        // get the high word:
                        short hiword_2 = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);

                        if (hiword_2 == 120) // clicked the mouse wheel button
                        {
                            IsMiddleMouseButtonPressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;

                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword_2, hiword_2);
                        }
                        break;
                    case PInvoke.WM_RBUTTONDOWN:
                        IsRightMouseButtonPressed = true;
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case PInvoke.WM_RBUTTONUP:
                        IsRightMouseButtonPressed = false;
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case PInvoke.WM_XBUTTONDOWN:
                        if(mouseData == 65536)
                        {
                            IsXButton1Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_XBUTTONUP:
                        if(mouseData == 65536)
                        {
                            IsXButton1Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_XBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case PInvoke.WM_NCXBUTTONDOWN:
                        if (mouseData == 0x1)
                        {
                            IsXButton1Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_NCXBUTTONUP:
                        if (mouseData == 0x1)
                        {
                            IsXButton1Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_NCXBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                }
            }

            return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
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

                try
                {
                    if (Monitor.TryEnter(lockObject))
                    {
                        Monitor.PulseAll(lockObject);
                        Monitor.Exit(lockObject);
                    }
                }
                catch
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
        #endregion
    }
}