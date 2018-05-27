using System;
using System.Diagnostics;

namespace Yato.LowLevelInput
{
    internal static class ProcessEvents
    {
        private static Process currentProcess;

        static ProcessEvents()
        {
            currentProcess = Process.GetCurrentProcess();

            currentProcess.Exited += CurrentProcess_Exited;
            currentProcess.EnableRaisingEvents = true;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public delegate void ProcessExitCallback(Process process);

        public delegate void UnhandledExceptionCallback(Exception exception);

        public static event ProcessExitCallback OnProcessExit;

        public static event UnhandledExceptionCallback OnUnhandledException;

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            OnProcessExit?.Invoke(currentProcess);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e == null) return;

            OnUnhandledException?.Invoke((Exception)e.ExceptionObject);

            if (e.IsTerminating)
            {
                OnProcessExit?.Invoke(currentProcess);
            }
        }

        private static void CurrentProcess_Exited(object sender, EventArgs e)
        {
            OnProcessExit?.Invoke(currentProcess);
        }
    }
}