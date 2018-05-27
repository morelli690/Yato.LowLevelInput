using System;

namespace Yato.LowLevelInput.PInvoke
{
    internal static class Kernel32
    {
        public static GetCurrentThreadId_t GetCurrentThreadId = WinApi.GetMethod<GetCurrentThreadId_t>("kernel32.dll", "GetCurrentThreadId");

        public delegate uint GetCurrentThreadId_t();
    }
}