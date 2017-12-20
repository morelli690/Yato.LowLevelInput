using System;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    internal static class PInvoke
    {
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public const int WH_KEYBOARD_LL = 13;

        public const uint WM_QUIT = 0x0012;

        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_SYSKEYDOWN = 0x0104;
        public const uint WM_SYSKEYUP = 0x0105;

        public const int WH_MOUSE_LL = 14;

        public const uint WM_MOUSEMOVE = 0x0200;

        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;

        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;

        public const uint WM_MOUSEWHEEL = 0x020A;
        public const uint WM_MOUSEHWHEEL = 0x020E;

        public const uint WM_XBUTTONDOWN = 0x020B;
        public const uint WM_XBUTTONUP = 0x020C;

        public const uint WM_XBUTTONDBLCLK = 0x020D;

        public const uint WM_NCXBUTTONDOWN = 0x00AB;
        public const uint WM_NCXBUTTONUP = 0x00AC;

        public const uint WM_NCXBUTTONDBLCLK = 0x00AD;

        public delegate IntPtr SetWindowsHookEx_t(int type, [MarshalAs(UnmanagedType.FunctionPtr)] LowLevelKeyboardProc hookProcedure, IntPtr hModule, uint threadId);
        public static SetWindowsHookEx_t SetWindowsHookEx = WinApi.GetMethod<SetWindowsHookEx_t>("user32.dll", "SetWindowsHookExW");

        public delegate int UnhookWindowsHookEx_t(IntPtr hHook);
        public static UnhookWindowsHookEx_t UnhookWindowsHookEx = WinApi.GetMethod<UnhookWindowsHookEx_t>("user32.dll", "UnhookWindowsHookEx");

        public delegate IntPtr CallNextHookEx_t(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);
        public static CallNextHookEx_t CallNextHookEx = WinApi.GetMethod<CallNextHookEx_t>("user32.dll", "CallNextHookEx");

        public delegate int GetMessage_t(ref Message lpMessage, IntPtr hwnd, uint msgFilterMin, uint msgFilterMax);
        public static GetMessage_t GetMessage = WinApi.GetMethod<GetMessage_t>("user32.dll", "GetMessageW");

        public delegate int PostThreadMessage_t(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);
        public static PostThreadMessage_t PostThreadMessage = WinApi.GetMethod<PostThreadMessage_t>("user32.dll", "PostThreadMessageW");

        public delegate uint GetCurrentThreadId_t();
        public static GetCurrentThreadId_t GetCurrentThreadId = WinApi.GetMethod<GetCurrentThreadId_t>("kernel32.dll", "GetCurrentThreadId");

        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr Hwnd;
            public uint Msg;
            public IntPtr lParam;
            public IntPtr wParam;
            public uint Time;
            public int X;
            public int Y;
        }
    }

    #region LoadLibrary and GetProcAddress

    internal static class WinApi
    {
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr getProcAddress(IntPtr hmodule, string procName);

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern IntPtr loadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern IntPtr getModuleHandle(string modulename);

        public static IntPtr GetProcAddress(string modulename, string procname)
        {
            IntPtr hModule = getModuleHandle(modulename);

            if (hModule == IntPtr.Zero) hModule = loadLibraryW(modulename);

            return getProcAddress(hModule, procname);
        }

        public static T GetMethod<T>(string modulename, string procname)
        {
            IntPtr hModule = getModuleHandle(modulename);

            if (hModule == IntPtr.Zero) hModule = loadLibraryW(modulename);

            IntPtr procAddress = getProcAddress(hModule, procname);

#if DEBUG
            if (hModule == IntPtr.Zero || procAddress == IntPtr.Zero)
                throw new Exception("module: " + modulename + "\tproc: " + procname);
#endif

            if (hModule == IntPtr.Zero || procAddress == IntPtr.Zero)
                return default(T);

            return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, ObfuscatorNeedsThis<T>());
        }

        private static Type ObfuscatorNeedsThis<T>()
        {
            return typeof(T);
        }
    }

    #endregion
}
