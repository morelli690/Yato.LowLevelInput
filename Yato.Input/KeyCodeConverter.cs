using System;

namespace Yato.Input
{
    public static class KeyCodeConverter
    {
        public static string ToString(VirtualKeyCode key)
        {
            return key.ToString();
        }

        public static VirtualKeyCode ToVirtualKeyCode(string name)
        {
            VirtualKeyCode vk = VirtualKeyCode.NONAME;

            if (!Enum.TryParse<VirtualKeyCode>(name, out vk)) return VirtualKeyCode.NONAME;

            return vk;
        }

        public static VirtualKeyCode ToVirtualKeyCode(int vk)
        {
            return (VirtualKeyCode)vk;
        }
    }
}
