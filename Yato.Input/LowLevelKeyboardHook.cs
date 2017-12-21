using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    public class LowLevelKeyboardHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        private object lockObject;

        private PInvoke.HookProc keyboardProcReference;
        private GCHandle gcHandle;

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

                //You are missing the effect that using a debugger has on the lifetime of local variables.With a debugger attached, the jitter marks the variables in use until the end of the method.Important to make debugging reliable.This however also prevents the GC.Collect() call from collecting the delegate object.
                //This code will crash when you run the Release build of your program without a debugger.

                keyboardProcReference = new PInvoke.HookProc(HookProcedure);
                gcHandle = GCHandle.Alloc(keyboardProcReference);

                GC.KeepAlive(keyboardProcReference); // GC does not touch any variables in this method until the method returns

                hookHandle = PInvoke.SetWindowsHookEx(PInvoke.WH_KEYBOARD_LL, keyboardProcReference, MainModuleHandle, 0);

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
            gcHandle.Free();
        }

        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0) // wParam and lParam are set
            {
                uint msg = (uint)wParam.ToInt32();

                try
                {
                    if (lParam == IntPtr.Zero) return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
                }
                catch
                {

                }

                VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);

                switch (msg)
                {
                    case PInvoke.WM_KEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case PInvoke.WM_KEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
                        break;
                    case PInvoke.WM_SYSKEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case PInvoke.WM_SYSKEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
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
                    if(Monitor.TryEnter(lockObject))
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
