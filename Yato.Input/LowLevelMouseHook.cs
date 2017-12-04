using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    public class LowLevelMouseHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        #region PInvoke

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;

        private const uint WM_QUIT = 0x0012;

        private const uint WM_MOUSEMOVE = 0x0200;

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;

        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;

        private const uint WM_MOUSEWHEEL = 0x020A;
        private const uint WM_MOUSEHWHEEL = 0x020E;

        private const uint WM_XBUTTONDOWN = 0x020B;
        private const uint WM_XBUTTONUP = 0x020C;

        private const uint WM_XBUTTONDBLCLK = 0x020D;

        private const uint WM_NCXBUTTONDOWN = 0x00AB;
        private const uint WM_NCXBUTTONUP = 0x00AC;

        private const uint WM_NCXBUTTONDBLCLK = 0x00AD;

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
        private static extern IntPtr SetWindowsHookEx(int type, [MarshalAs(UnmanagedType.FunctionPtr)] LowLevelMouseProc hookProcedure, IntPtr hModule, uint threadId);

        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetMessage(ref Message lpMessage, IntPtr hwnd, uint msgFilterMin, uint msgFilterMax);

        [DllImport("user32.dll", EntryPoint = "PostThreadMessageW")]
        private static extern int PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [StructLayout(LayoutKind.Sequential)]
        private struct Message
        {
            public IntPtr Hwnd;
            public uint Msg;
            public IntPtr lParam;
            public IntPtr wParam;
            public uint Time;
            public int X;
            public int Y;
        }

        #endregion

        private object lockObject;

        private IntPtr hookHandle;

        private uint hookThreadId;
        private Thread hookThread;

        public delegate void MouseHookCallback(KeyState state, VirtualKeyCode key, int x, int y);
        public event MouseHookCallback OnMouseCaptured;

        public bool CaptureMouseMove;

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

                if (PostThreadMessage(hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
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
                hookThreadId = GetCurrentThreadId();

                hookHandle = SetWindowsHookEx(WH_MOUSE_LL, HookProcedure, MainModuleHandle, 0);

                if (hookHandle == IntPtr.Zero)
                {
                    throw new Exception("Failed to create LowLevelKeyboardHook");
                }
            }

            // we need to start a message loop here to keep the hook working

            Message msg = new Message();

            // we actually do not care on any window messages
            while (GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
            }

            // Unhook in the same thread

            UnhookWindowsHookEx(hookHandle);
        }

        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0) // wParam and lParam are set
            {
                uint msg = (uint)wParam.ToInt32();

                int x = Marshal.ReadInt32(lParam);
                int y = Marshal.ReadInt32(lParam + 4);

                int mouseData = Marshal.ReadInt32(lParam + 8);

                switch (msg)
                {
                    case WM_LBUTTONDOWN:
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case WM_LBUTTONUP:
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case WM_MOUSEHWHEEL:
                        // get the high word:
                        short hiword = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);

                        if(hiword == 120) // clicked the mouse wheel button
                        {
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;

                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword, hiword);
                        }
                        break;
                    case WM_MOUSEMOVE:
                        if (!CaptureMouseMove) break;
                        OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.NONAME, x, y);
                        break;
                    case WM_MOUSEWHEEL:
                        // get the high word:
                        short hiword_2 = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);

                        if (hiword_2 == 120) // clicked the mouse wheel button
                        {
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;

                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword_2, hiword_2);
                        }
                        break;
                    case WM_RBUTTONDOWN:
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case WM_RBUTTONUP:
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case WM_XBUTTONDOWN:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case WM_XBUTTONUP:
                        OnMouseCaptured?.Invoke(KeyState.Up, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case WM_XBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case WM_NCXBUTTONDOWN:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case WM_NCXBUTTONUP:
                        OnMouseCaptured?.Invoke(KeyState.Up, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case WM_NCXBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                }
            }

            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
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