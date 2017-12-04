using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yato.Input
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadLine();
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
