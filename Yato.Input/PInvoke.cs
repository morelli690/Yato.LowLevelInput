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

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
        public static extern IntPtr SetWindowsHookEx(int type, [MarshalAs(UnmanagedType.FunctionPtr)] LowLevelKeyboardProc hookProcedure, IntPtr hModule, uint threadId);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessage(ref Message lpMessage, IntPtr hwnd, uint msgFilterMin, uint msgFilterMax);

        [DllImport("user32.dll", EntryPoint = "PostThreadMessageW")]
        public static extern int PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

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
}
