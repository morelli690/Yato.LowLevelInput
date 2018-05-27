using System;

namespace Yato.LowLevelInput.PInvoke
{
    internal static class NtDll
    {
        public static NtSetInformationThread_t NtSetInformationThread = WinApi.GetMethod<NtSetInformationThread_t>("ntdll.dll", "NtSetInformationThread");

        public delegate int NtSetInformationThread_t(IntPtr hThread, int threadInformationClass, IntPtr threadInformation, int threadInformationLength);
    }
}