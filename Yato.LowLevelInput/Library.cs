using System;
using System.Reflection;

namespace Yato.LowLevelInput
{
    /// <summary>
    /// </summary>
    public static class Library
    {
        /// <summary>
        /// Gets the author.
        /// </summary>
        /// <value>The author.</value>
        public static string Author
        {
            get
            {
                return "Yato";
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public static string Name
        {
            get
            {
                return "Yato.LowLevelInput";
            }
        }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public static string URL
        {
            get
            {
                return "https://github.com/YatoDev/Yato.LowLevelInput";
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
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