using System;

namespace Yato.Input
{
    // to cover better obfuscation

    public static class KeyCodeConverter
    {
        private static string[] vkStrings = new string[]
        {
            "HOTKEY",
            "LBUTTON",
            "RBUTTON",
            "CANCEL",
            "MBUTTON",
            "XBUTTON1",
            "XBUTTON2",
            "BACK",
            "TAB",
            "CLEAR",
            "RETURN",
            "SHIFT",
            "CONTROL",
            "MENU",
            "PAUSE",
            "CAPITAL",
            "KANA",
            "HANGUL",
            "JUNJA",
            "FINAL",
            "HANJA",
            "KANJI",
            "ESCAPE",
            "CONVERT",
            "NONCONVERT",
            "ACCEPT",
            "MODECHANGE",
            "SPACE",
            "PRIOR",
            "NEXT",
            "END",
            "HOME",
            "LEFT",
            "UP",
            "RIGHT",
            "DOWN",
            "SELECT",
            "PRINT",
            "EXECUTE",
            "SNAPSHOT",
            "INSERT",
            "DELETE",
            "HELP",
            "Zero",
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "A",
            "B",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z",
            "LWIN",
            "RWIN",
            "APPS",
            "SLEEP",
            "NUMPAD0",
            "NUMPAD1",
            "NUMPAD2",
            "NUMPAD3",
            "NUMPAD4",
            "NUMPAD5",
            "NUMPAD6",
            "NUMPAD7",
            "NUMPAD8",
            "NUMPAD9",
            "MULTIPLY",
            "ADD",
            "SEPARATOR",
            "SUBTRACT",
            "DECIMAL",
            "DIVIDE",
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12",
            "F13",
            "F14",
            "F15",
            "F16",
            "F17",
            "F18",
            "F19",
            "F20",
            "F21",
            "F22",
            "F23",
            "F24",
            "NUMLOCK",
            "SCROLL",
            "LSHIFT",
            "RSHIFT",
            "LCONTROL",
            "RCONTROL",
            "LMENU",
            "RMENU",
            "BROWSER_BACK",
            "BROWSER_FORWARD",
            "BROWSER_REFRESH",
            "BROWSER_STOP",
            "BROWSER_SEARCH",
            "BROWSER_FAVORITES",
            "BROWSER_HOME",
            "VOLUME_MUTE",
            "VOLUME_DOWN",
            "VOLUME_UP",
            "MEDIA_NEXT_TRACK",
            "MEDIA_PREV_TRACK",
            "MEDIA_STOP",
            "MEDIA_PLAY_PAUSE",
            "LAUNCH_MAIL",
            "LAUNCH_MEDIA_SELECT",
            "LAUNCH_APP1",
            "LAUNCH_APP2",
            "OEM_1",
            "OEM_PLUS",
            "OEM_COMMA",
            "OEM_MINUS",
            "OEM_PERIOD",
            "OEM_2",
            "OEM_3",
            "OEM_4",
            "OEM_5",
            "OEM_6",
            "OEM_7",
            "OEM_8",
            "OEM_102",
            "PROCESSKEY",
            "PACKET",
            "ATTN",
            "CRSEL",
            "EXSEL",
            "EREOF",
            "PLAY",
            "ZOOM",
            "NONAME",
            "PA1",
            "OEM_CLEAR",
        };

        private static string[] stateStrings = new string[]
        {
            "None",
            "Up",
            "Down"
        };

        public static string ToString(VirtualKeyCode key)
        {
            int index = (int)key;

            if (index < 0) return string.Empty;

            if (index >= vkStrings.Length) return string.Empty;

            return vkStrings[index];
        }

        public static string ToString(int index)
        {
            if (index < 0) return string.Empty;

            if (index >= vkStrings.Length) return string.Empty;

            return vkStrings[index];
        }

        public static VirtualKeyCode ToVirtualKeyCode(string name)
        {
            name = name.ToUpper();

            for(int i = 0; i < vkStrings.Length; i++)
            {
                if (vkStrings[i] == name) return (VirtualKeyCode)i;
            }

            return VirtualKeyCode.NONAME;
        }

        public static VirtualKeyCode ToVirtualKeyCode(int vk)
        {
            return (VirtualKeyCode)vk;
        }

        public static string ToString(KeyState state)
        {
            int index = (int)state;

            if (index < 0) return string.Empty;
            if (index >= stateStrings.Length) return string.Empty;

            return stateStrings[index];
        }

        public static KeyState ToKeyState(string name)
        {
            for (int i = 0; i < stateStrings.Length; i++)
            {
                if (stateStrings[i] == name) return (KeyState)i;
            }

            return KeyState.None;
        }

        public static KeyState ToKeyState(int index)
        {
            return (KeyState)index;
        }
    }
}
