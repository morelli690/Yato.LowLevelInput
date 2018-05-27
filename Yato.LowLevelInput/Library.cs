using System;
using System.Reflection;

namespace Yato.LowLevelInput
{
    public static class Library
    {
        public static string Author
        {
            get
            {
                return "Yato";
            }
        }

        public static bool DebugMode { get; set; }

        public static string Name
        {
            get
            {
                return "Yato.LowLevelInput";
            }
        }

        public static string URL
        {
            get
            {
                return "https://github.com/YatoDev/Yato.LowLevelInput";
            }
        }

        public static string Version
        {
            get
            {
                try
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    AssemblyName assemblyName = assembly.GetName();

                    return assemblyName.Version.ToString();
                }
                catch
                {
                    return "1.0.0.0";
                }
            }
        }
    }
}