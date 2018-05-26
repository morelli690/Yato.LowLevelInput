using System;
using System.Runtime.InteropServices;

namespace Yato.LowLevelInput.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr Hwnd;
        public IntPtr lParam;
        public uint Msg;
        public uint Time;
        public IntPtr wParam;
        public int X;
        public int Y;
    }

    internal static class User32
    {
        public static CallNextHookEx_t CallNextHookEx = WinApi.GetMethod<CallNextHookEx_t>("user32.dll", "CallNextHookEx");

        public static GetMessage_t GetMessage = WinApi.GetMethod<GetMessage_t>("user32.dll", "GetMessageW");

        public static PostThreadMessage_t PostThreadMessage = WinApi.GetMethod<PostThreadMessage_t>("user32.dll", "PostThreadMessageW");

        public static SetWindowsHookEx_t SetWindowsHookEx = WinApi.GetMethod<SetWindowsHookEx_t>("user32.dll", "SetWindowsHookExW");

        public static UnhookWindowsHookEx_t UnhookWindowsHookEx = WinApi.GetMethod<UnhookWindowsHookEx_t>("user32.dll", "UnhookWindowsHookEx");

        public delegate IntPtr CallNextHookEx_t(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        public delegate int GetMessage_t(ref Message lpMessage, IntPtr hwnd, uint msgFilterMin, uint msgFilterMax);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        public delegate int PostThreadMessage_t(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        public delegate IntPtr SetWindowsHookEx_t(int type, IntPtr hookProcedure, IntPtr hModule, uint threadId);

        public delegate int UnhookWindowsHookEx_t(IntPtr hHook);
    }
}