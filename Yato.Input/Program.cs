﻿using System;
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
            //using (var hook = new LowLevelKeyboardHook())
            //{
            //    hook.InstallHook();

            //    hook.OnKeyCaptured += Hook_OnKeyCaptured;

            //    Console.ReadLine();
            //}

            using (var hook = new LowLevelMouseHook())
            {
                hook.InstallHook();

                hook.OnMouseCaptured += Hook_OnMouseCaptured;

                Console.ReadLine();
            }
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
