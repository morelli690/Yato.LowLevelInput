#if DEBUG

using System;
using System.Threading;

using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    public class Program
    {
        public static InputManager manager;

        public static void Main(string[] args)
        {
            manager = new InputManager();

            // cheat loop

            while (true/*Aimbot*/)
            {
                if (!manager.WaitForKeyEvent(VirtualKeyCode.LCONTROL, KeyState.Down, 1000)) continue;

                while (manager.IsPressed(VirtualKeyCode.LCONTROL))
                {
                }
            }
        }

        private static void Callback(KeyState state, VirtualKeyCode key)
        {
            Console.WriteLine($"State: {state}, VK: {key}");
        }

        //public static void Main(string[] args)
        //{
        //    LowLevelKeyboardHook kbdHook = new LowLevelKeyboardHook();
        //    LowLevelMouseHook mouseHook = new LowLevelMouseHook();

        // kbdHook.OnKeyboardEvent += KbdHook_OnKeyboardEvent; mouseHook.OnMouseEvent += MouseHook_OnMouseEvent;

        // kbdHook.InstallHook(); mouseHook.InstallHook();

        //    Console.ReadLine();
        //}

        //private static void MouseHook_OnMouseEvent(KeyState state, VirtualKeyCode key, int x, int y)
        //{
        //    Console.WriteLine($"KeyState: {state}, Key: {key}, X: {x}, Y: {y}");
        //}

        //private static void KbdHook_OnKeyboardEvent(KeyState state, VirtualKeyCode key)
        //{
        //    Console.WriteLine($"KeyState: {state}, Key: {key}");
        //}
    }

    public static class Feature
    {
        private static object lockObject = new object();
        private static bool execute = true;

        public static void Callback(KeyState state, VirtualKeyCode key)
        {
            if (state != KeyState.Down) return;

            if (!AcquireLock()) return;

            while (Program.manager.IsPressed(key))
            {
            }

            ReleaseLock();
        }

        private static bool AcquireLock()
        {
            lock (lockObject)
            {
                if (!execute) return false;

                execute = false;

                return true;
            }
        }

        private static void ReleaseLock()
        {
            lock (lockObject)
            {
                execute = true;
            }
        }
    }
}

#endif