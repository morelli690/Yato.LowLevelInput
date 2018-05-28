#if DEBUG

using System;
using System.Threading;

using Yato.LowLevelInput.Hooks;

namespace Yato.LowLevelInput
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LowLevelKeyboardHook kbdHook = new LowLevelKeyboardHook();
            LowLevelMouseHook mouseHook = new LowLevelMouseHook();

            kbdHook.OnKeyboardEvent += KbdHook_OnKeyboardEvent;
            mouseHook.OnMouseEvent += MouseHook_OnMouseEvent;

            kbdHook.InstallHook();
            mouseHook.InstallHook();

            Console.ReadLine();
        }

        private static void MouseHook_OnMouseEvent(KeyState state, VirtualKeyCode key, int x, int y)
        {
            Console.WriteLine($"KeyState: {state}, Key: {key}, X: {x}, Y: {y}");
        }

        private static void KbdHook_OnKeyboardEvent(KeyState state, VirtualKeyCode key)
        {
            Console.WriteLine($"KeyState: {state}, Key: {key}");
        }
    }
}

#endif