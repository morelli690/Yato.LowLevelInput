using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Yato.LowLevelInput
{
    internal static class Global
    {
        private static TaskFactory libraryTaskFactory;

        static Global()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            libraryTaskFactory = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.None);
        }

        public delegate void ProcessExitCallback();

        public delegate void UnhandledExceptionCallback();

        public static event ProcessExitCallback OnProcessExit;

        public static event UnhandledExceptionCallback OnUnhandledException;

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            OnProcessExit?.Invoke();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            OnUnhandledException?.Invoke();
        }

        public static void StartNewTask(Action action)
        {
            libraryTaskFactory.StartNew(action);
        }
    }
}