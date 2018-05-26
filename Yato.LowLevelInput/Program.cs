#if DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.LowLevelInput
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }

        private static void PrintKeyCodeArray()
        {
            List<string> nameList = new List<string>();

            foreach (var value in Enum.GetValues(typeof(VirtualKeyCode)))
            {
                string name = Enum.GetName(typeof(VirtualKeyCode), value);

                int index = (int)value;

                if (index == nameList.Count)
                {
                    nameList.Add(name);
                }
                else
                {
                    int delta = index - nameList.Count;

                    for (int i = 0; i < delta; i++)
                        nameList.Add("");

                    nameList.Add(name);
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("private static string[] strArray = new string[]");
            sb.AppendLine("{");

            foreach (var name in nameList)
            {
                sb.AppendLine("\"" + name + "\",");
            }

            sb.AppendLine("};");

            Console.WriteLine(sb.ToString());

            Console.ReadLine();
        }
    }
}

#endif