﻿using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Yato.LowLevelInput.PInvoke;
using Yato.LowLevelInput.WindowsHooks;

namespace Yato.LowLevelInput.Hooks
{
    /// <summary>
    /// </summary>
    /// <seealso cref="System.IDisposable"/>
    public class LowLevelKeyboardHook : IDisposable
    {
        private WindowsHook hook;
        private object lockObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="LowLevelKeyboardHook"/> class.
        /// </summary>
        public LowLevelKeyboardHook()
        {
            lockObject = new object();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LowLevelKeyboardHook"/> class.
        /// </summary>
        /// <param name="clearInjectedFlag">if set to <c>true</c> [clear injected flag].</param>
        public LowLevelKeyboardHook(bool clearInjectedFlag)
        {
            lockObject = new object();
            ClearInjectedFlag = clearInjectedFlag;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LowLevelKeyboardHook"/> class.
        /// </summary>
        ~LowLevelKeyboardHook()
        {
            Dispose(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="key">The key.</param>
        public delegate void KeyboardEventCallback(KeyState state, VirtualKeyCode key);

        /// <summary>
        /// Occurs when [on keyboard event].
        /// </summary>
        public event KeyboardEventCallback OnKeyboardEvent;

        /// <summary>
        /// Gets or sets a value indicating whether [clear injected flag].
        /// </summary>
        /// <value><c>true</c> if [clear injected flag]; otherwise, <c>false</c>.</value>
        public bool ClearInjectedFlag { get; set; }

        private void Global_OnProcessExit()
        {
            Dispose();
        }

        private void Global_OnUnhandledException()
        {
            Dispose();
        }

        private void Hook_OnHookCalled(IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;

            if (ClearInjectedFlag)
            {
                int flags = Marshal.ReadInt32(lParam + 8);

                BitArray bits = new BitArray(BitConverter.GetBytes(flags));

                if (bits.Get(1) || bits.Get(4))
                {
                    bits.Set(1, false);
                    bits.Set(4, false);

                    int[] modifiedBits = new int[1];

                    bits.CopyTo(modifiedBits, 0);

                    Marshal.WriteInt32(lParam + 8, modifiedBits[0]);
                }
            }

            if (OnKeyboardEvent == null) return;

            WindowsMessage msg = (WindowsMessage)((uint)wParam.ToInt32());

            VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);

            switch (msg)
            {
                case WindowsMessage.WM_KEYDOWN:
                    InvokeEventListeners(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_KEYUP:
                    InvokeEventListeners(KeyState.Up, key);
                    break;

                case WindowsMessage.WM_SYSKEYDOWN:
                    InvokeEventListeners(KeyState.Down, key);
                    break;

                case WindowsMessage.WM_SYSKEYUP:
                    InvokeEventListeners(KeyState.Up, key);
                    break;
            }
        }

        private void InvokeEventListeners(KeyState state, VirtualKeyCode key)
        {
            Task.Factory.StartNew(() =>
            {
                OnKeyboardEvent?.Invoke(state, key);
            });
        }

        /// <summary>
        /// Installs the hook.
        /// </summary>
        /// <returns></returns>
        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hook != null) return false;

                hook = new WindowsHook(WindowsHookType.LowLevelKeyboard);
            }

            hook.OnHookCalled += Hook_OnHookCalled;

            hook.InstallHook();

            Global.OnProcessExit += Global_OnProcessExit;
            Global.OnUnhandledException += Global_OnUnhandledException;

            return true;
        }

        /// <summary>
        /// Uninstalls the hook.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.
        /// </param>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}