using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Yato.Input
{
    class Program
    {
        static void Main(string[] args)
        {
            //printItemDictionary();

            //Console.WriteLine(KeyCodeConverter.ToString(VirtualKeyCode.SPACE));

            //Console.ReadLine();

            //printKeyCodeArray();

            using (var input = new InputHandler(false))
            {
                input.OnInputCaptured += Input_OnInputCaptured;

                Console.ReadLine();
            }
        }

        private static void printItemDictionary()
        {
            Dictionary<string, int> stringToItem = new Dictionary<string, int>();
            Dictionary<int, string> itemToString = new Dictionary<int, string>();

            foreach(var value in Enum.GetValues(typeof(ItemDefinitionIndex)))
            {
                itemToString.Add((int)value, Enum.GetName(typeof(ItemDefinitionIndex), value));
                stringToItem.Add(Enum.GetName(typeof(ItemDefinitionIndex), value), (int)value);
            }

            StringBuilder sb = new StringBuilder();

            foreach(var pair in stringToItem)
            {
                sb.AppendLine("stringToItem.Add(\"" + pair.Key + "\", " + pair.Value.ToString() + ");");
            }

            sb.AppendLine();

            foreach (var pair in itemToString)
            {
                sb.AppendLine("itemToString.Add(" + pair.Key.ToString() + ", \"" + pair.Value + "\");");
            }

            Console.WriteLine(sb.ToString());

            Console.ReadLine();
        }

        private static void printKeyCodeArray()
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

        private static void Input_OnInputCaptured(KeyState state, VirtualKeyCode key, int x, int y)
        {
            Console.WriteLine(state + "\t:\t" + key + "\tX: " + x + " Y: " + y);
        }

        private static void Hook_OnMouseCaptured(KeyState state, VirtualKeyCode key, int x, int y)
        {
            Console.WriteLine(state + "\t:\t" + key + "\tX: " + x + " Y: " + y);
        }

        private static void Hook_OnKeyCaptured(KeyState state, VirtualKeyCode key)
        {
            Console.WriteLine(state.ToString() + "\t:\t" + key.ToString());
        }
    }
}
