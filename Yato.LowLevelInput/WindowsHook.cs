using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using Yato.LowLevelInput.PInvoke;

namespace Yato.LowLevelInput
{
    public class WindowsHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        private IntPtr hookHandle;
        private User32.HookProc hookProc;
        private Thread hookThread;
        private uint hookThreadId;
        private object lockObject;

        private WindowsHook()
        {
            lockObject = new object();
        }

        public WindowsHook(WindowsHookType windowsHookType)
        {
            lockObject = new object();
            WindowsHookType = windowsHookType;
        }

        ~WindowsHook()
        {
            Dispose(false);
        }

        public delegate void HookCallback(IntPtr wParam, IntPtr lParam);

        public event HookCallback OnHookCalled;

        public WindowsHookType WindowsHookType { get; private set; }

        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0)
            {
                OnHookCalled?.Invoke(wParam, lParam);
            }

            return User32.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private void InitializeHookThread()
        {
            lock (lockObject)
            {
                hookThreadId = Kernel32.GetCurrentThreadId();

                hookProc = new User32.HookProc(HookProcedure);

                IntPtr methodPtr = Marshal.GetFunctionPointerForDelegate(hookProc);

                hookHandle = User32.SetWindowsHookEx((int)WindowsHookType, methodPtr, MainModuleHandle, 0);
            }

            Message msg = new Message();

            while (User32.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
                if (msg.Msg == Constant.WM_QUIT) break;
            }

            User32.UnhookWindowsHookEx(hookHandle);
        }

        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle != IntPtr.Zero) return false;
                if (hookThreadId != 0) return false;
                if (hookThread == null) return false;

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

                if (User32.PostThreadMessage(hookThreadId, Constant.WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    try
                    {
                        hookThread.Join();
                    }
                    catch
                    {
                    }
                }

                hookHandle = IntPtr.Zero;
                hookThreadId = 0;
                hookThread = null;

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