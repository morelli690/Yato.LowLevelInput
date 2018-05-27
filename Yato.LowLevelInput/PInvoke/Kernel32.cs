using System;

namespace Yato.LowLevelInput.PInvoke
{
    internal static class Kernel32
    {
        public static CreateThread_t CreateThread = WinApi.GetMethod<CreateThread_t>("kernel32.dll", "CreateThread");
        public static GetCurrentThread_t GetCurrentThread = WinApi.GetMethod<GetCurrentThread_t>("kernel32.dll", "GetCurrentThread");
        public static GetCurrentThreadId_t GetCurrentThreadId = WinApi.GetMethod<GetCurrentThreadId_t>("kernel32.dll", "GetCurrentThreadId");

        public delegate IntPtr CreateThread_t(IntPtr lpThreadAttributes, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, ref uint lpThreadId);

        public delegate IntPtr GetCurrentThread_t();

        public delegate uint GetCurrentThreadId_t();
    }
}