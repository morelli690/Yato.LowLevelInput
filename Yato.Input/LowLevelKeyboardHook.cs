using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    public class LowLevelKeyboardHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        #region PInvoke

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;

        private const uint WM_QUIT = 0x0012;

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_SYSKEYDOWN = 0x0104;
        private const uint WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
        private static extern IntPtr SetWindowsHookEx(int type, [MarshalAs(UnmanagedType.FunctionPtr)] LowLevelKeyboardProc hookProcedure, IntPtr hModule, uint threadId);

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

        public delegate void KeyboardHookCallback(KeyState state, VirtualKeyCode key);
        public event KeyboardHookCallback OnKeyCaptured;

        public LowLevelKeyboardHook()
        {
            lockObject = new object();
        }

        ~LowLevelKeyboardHook()
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

                hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, HookProcedure, MainModuleHandle, 0);

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
                VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);

                switch (msg)
                {
                    case WM_KEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case WM_KEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
                        break;
                    case WM_SYSKEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case WM_SYSKEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
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
