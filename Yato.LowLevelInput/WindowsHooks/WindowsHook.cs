﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using Yato.LowLevelInput.PInvoke;
using System.Runtime.CompilerServices;

namespace Yato.LowLevelInput.WindowsHooks
{
    internal class WindowsHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;

        private Action action;
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

            if (action != null)
            {
                // HideThreadFromDebugger
                NtDll.NtSetInformationThread(Kernel32.GetCurrentThread(), 0x11, IntPtr.Zero, 0);
            }

            Message msg = new Message();

            while (User32.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
                if (msg.Msg == (uint)WindowsMessage.WM_QUIT) break;
            }

            User32.UnhookWindowsHookEx(hookHandle);
        }

        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle != IntPtr.Zero) return false;
                if (hookThreadId != 0) return false;

                if (Library.DebugMode)
                {
                    hookThread = new Thread(InitializeHookThread)
                    {
                        IsBackground = true
                    };

                    hookThread.Start();
                }
                else
                {
                    action = new Action(InitializeHookThread);

                    RuntimeHelpers.PrepareDelegate(action);

                    IntPtr startAddress = Marshal.GetFunctionPointerForDelegate(action);
                    uint uselessThreadId = 0;

                    Kernel32.CreateThread(IntPtr.Zero, IntPtr.Zero, startAddress, IntPtr.Zero, 0, ref uselessThreadId);
                }

                return true;
            }
        }

        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle == IntPtr.Zero) return false;
                if (hookThreadId == 0) return false;

                if (User32.PostThreadMessage(hookThreadId, (uint)WindowsMessage.WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
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
                action = null;

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
                    OnHookCalled = null;
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