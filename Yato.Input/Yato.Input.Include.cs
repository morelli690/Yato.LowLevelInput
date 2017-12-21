using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Yato.Input
{
    public class InputHandler : IDisposable
    {
        private LowLevelKeyboardHook keyboardHook;
        private LowLevelMouseHook mouseHook;
        private VirtualKeyCode nextEventResult = VirtualKeyCode.NONAME;
        private object nextEventResultLock = new object();
        private object namedEventLock = new object();
        private List<VirtualKeyCode> namedEventList = new List<VirtualKeyCode>();
        private List<object> namedEventMonitor = new List<object>();
        public delegate void InputHandlerCallback(KeyState state, VirtualKeyCode key, int x, int y);
        public event InputHandlerCallback OnInputCaptured;
        public bool CaptureMouseMove
        {
            get
            {
                return mouseHook.CaptureMouseMove;
            }
            set
            {
                mouseHook.CaptureMouseMove = value;
            }
        }
        public InputHandler()
        {
            Initialize();
        }
        public InputHandler(bool captureMouseMove = true)
        {
            Initialize(captureMouseMove);
        }
        ~InputHandler()
        {
            Dispose(false);
        }
        public VirtualKeyCode WaitForNextEvent(int timeout = -1)
        {
            if (timeout < -1) timeout *= -1;
            Monitor.Enter(nextEventResultLock);
            if (timeout == -1)
            {
                Monitor.Wait(nextEventResultLock);
            }
            else
            {
                Monitor.Wait(nextEventResultLock, timeout);
            }
            Monitor.Exit(nextEventResultLock);
            return nextEventResult;
        }
        public int RegisterNamedEvent(VirtualKeyCode key)
        {
            lock(namedEventLock)
            {
                if (namedEventList.Contains(key)) return namedEventList.IndexOf(key);
                namedEventList.Add(key);
                namedEventMonitor.Add(new object());
                return namedEventList.Count - 1;
            }
        }
        public void UnregisterNamedEvent(VirtualKeyCode key)
        {
            lock(namedEventLock)
            {
                int index = namedEventList.IndexOf(key);
                if (index == -1) return;
                namedEventList.RemoveAt(index);
                namedEventMonitor.RemoveAt(index);
            }
        }
        public void UnregisterNamedEvent(int index)
        {
            lock(namedEventLock)
            {
                if (index < 0) return;
                if (index >= namedEventList.Count) return;
                namedEventList.RemoveAt(index);
                namedEventMonitor.RemoveAt(index);
            }
        }
        public void ChangeNamedEvent(int eventIndex, VirtualKeyCode key)
        {
            if (eventIndex < 0) return;
            lock (namedEventLock)
            {
                if (eventIndex >= namedEventList.Count) return;
                namedEventList[eventIndex] = key;
            }
        }
        public VirtualKeyCode GetNamedEvent(int eventIndex)
        {
            if (eventIndex < 0) return VirtualKeyCode.NONAME;
            lock (namedEventLock)
            {
                if (eventIndex >= namedEventList.Count) return VirtualKeyCode.NONAME;
                return namedEventList[eventIndex];
            }
        }
        public bool WaitForNamedEvent(int eventIndex, int timeout = 1000)
        {
            if (eventIndex < 0) return false;
            if (eventIndex >= namedEventList.Count) return false;
            // the mouse hook does not receive multiple down or up events
            var key = namedEventList[eventIndex];
            switch(key)
            {
                case VirtualKeyCode.LBUTTON:
                    if (mouseHook.IsLeftMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.RBUTTON:
                    if (mouseHook.IsRightMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.MBUTTON:
                    if (mouseHook.IsMiddleMouseButtonPressed) return true;
                    break;
                case VirtualKeyCode.XBUTTON1:
                    if (mouseHook.IsXButton1Pressed) return true;
                    break;
                case VirtualKeyCode.XBUTTON2:
                    if (mouseHook.IsXButton2Pressed) return true;
                    break;
            }
            if (timeout < -1) timeout *= -1;
            bool result = false;
            Monitor.Enter(namedEventMonitor[eventIndex]);
            if(timeout == -1)
            {
                Monitor.Wait(namedEventMonitor[eventIndex]);
                result = true;
            }
            else
            {
                result = Monitor.Wait(namedEventMonitor[eventIndex], timeout);
            }
            return result;
        }
        private void Initialize(bool captureMouseMove = true)
        {
            keyboardHook = new LowLevelKeyboardHook();
            mouseHook = new LowLevelMouseHook();
            mouseHook.CaptureMouseMove = captureMouseMove;
            keyboardHook.OnKeyCaptured += KeyboardHook_OnKeyCaptured;
            mouseHook.OnMouseCaptured += MouseHook_OnMouseCaptured;
            keyboardHook.InstallHook();
            mouseHook.InstallHook();
        }
        private void MouseHook_OnMouseCaptured(KeyState state, VirtualKeyCode key, int x, int y)
        {
            // process events before our callback because they may lock some other thread
            // WaitForNextEvent
            if (state == KeyState.Down && Monitor.TryEnter(nextEventResultLock)) // someone is waiting for a key press to occur
            {
                nextEventResult = key;
                Monitor.PulseAll(nextEventResultLock);
                Monitor.Exit(nextEventResultLock);
            }
            // WaitForNamedEvent
            if (state == KeyState.Down)
            {
                lock (namedEventLock)
                {
                    if (namedEventList.Count != 0)
                    {
                        int index = namedEventList.IndexOf(key);
                        if (index != -1)
                        {
                            Monitor.Enter(namedEventMonitor[index]);
                            Monitor.PulseAll(namedEventMonitor[index]);
                            Monitor.Exit(namedEventMonitor[index]);
                        }
                    }
                }
            }
            OnInputCaptured?.Invoke(state, key, x, y);
        }
        private void KeyboardHook_OnKeyCaptured(KeyState state, VirtualKeyCode key)
        {
            // process events before our callback because they may lock some other thread
            // WaitForNextEvent
            if (state == KeyState.Down && Monitor.TryEnter(nextEventResultLock)) // someone is waiting for a key press to occur
            {
                nextEventResult = key;
                Monitor.PulseAll(nextEventResultLock);
                Monitor.Exit(nextEventResultLock);
            }
            // WaitForNamedEvent
            if (state == KeyState.Down)
            {
                lock (namedEventLock)
                {
                    if (namedEventList.Count != 0)
                    {
                        int index = namedEventList.IndexOf(key);
                        if(index != -1)
                        {
                            Monitor.Enter(namedEventMonitor[index]);
                            Monitor.PulseAll(namedEventMonitor[index]);
                            Monitor.Exit(namedEventMonitor[index]);
                        }
                    }
                }
            }
            OnInputCaptured?.Invoke(state, key, 0, 0);
        }
        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                try
                {
                    if (Monitor.TryEnter(nextEventResultLock))
                    {
                        Monitor.PulseAll(nextEventResultLock);
                        Monitor.Exit(nextEventResultLock);
                    }
                }
                catch
                {
                }
                try
                {
                    if (Monitor.TryEnter(namedEventLock))
                    {
                        Monitor.PulseAll(namedEventLock);
                        Monitor.Exit(namedEventLock);
                    }
                }
                catch
                {
                }
                for(int i = 0; i < namedEventMonitor.Count; i++)
                {
                    try
                    {
                        if(Monitor.TryEnter(namedEventMonitor[i]))
                        {
                            Monitor.PulseAll(namedEventMonitor[i]);
                            Monitor.Exit(namedEventMonitor[i]);
                        }
                    }
                    catch
                    {
                    }
                }
                if (disposing)
                {
                    namedEventMonitor = null;
                    namedEventList = null;
                }
                if (keyboardHook != null) keyboardHook.Dispose();
                if (mouseHook != null) mouseHook.Dispose();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
namespace Yato.Input
{
    internal enum ItemDefinitionIndex
    {
        DESERT_DEAGLE = 1,
        DUAL_BERRETAS = 2,
        FIVE_SEVEN = 3,
        GLOCK_18 = 4,
        AK_47 = 7,
        AUG = 8,
        AWP = 9,
        FAMAS = 10,
        G3SG1 = 11,
        GALIL_AR = 13,
        M249 = 14,
        M4A4 = 16,
        MAC_10 = 17,
        P90 = 19,
        UMP_45 = 24,
        XM1014 = 25,
        PP_BIZON = 26,
        MAG_7 = 27,
        NEGEV = 28,
        SAWED_OFF = 29,
        TEC_9 = 30,
        ZEUS_X27 = 31,
        P2000 = 32,
        MP7 = 33,
        MP9 = 34,
        NOVA = 35,
        P250 = 36,
        SCAR_20 = 38,
        SG_553 = 39,
        SSG08 = 40,
        DEFAULT_CT = 42,
        FLASHGRENADE = 43,
        HE_GRENADE = 44,
        SMOKE_GRENADE = 45,
        MOLOTOV_GRENADE = 46,
        DECOY_GRENADE = 47,
        INCENDIARY_GRENADE = 48,
        C4 = 49,
        DEFAULT_T = 59,
        M4A1_S = 60,
        USP_S = 61,
        CZ75_AUTO = 63,
        R8_REVOLVER = 64,
        KNIFE_BAYONET = 500,
        KNIFE_FLIP = 505,
        KNIFE_GUT = 506,
        KNIFE_KARAMBIT = 507,
        KNIFE_M9_BAYONET = 508,
        KNIFE_HUNTSMAN = 509,
        KNIFE_FALCHION = 512,
        KNIFE_BOWIE = 514,
        KNIFE_BUTTERFLY = 515,
        KNIFE_SHADOW = 516
    }
}
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
            "",
            "BACK",
            "TAB",
            "",
            "",
            "CLEAR",
            "RETURN",
            "",
            "",
            "SHIFT",
            "CONTROL",
            "MENU",
            "PAUSE",
            "CAPITAL",
            "HANGUL",
            "HANGUL",
            "JUNJA",
            "FINAL",
            "HANJA",
            "HANJA",
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
            "",
            "",
            "",
            "",
            "",
            "",
            "",
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
            "",
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
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "NUMLOCK",
            "SCROLL",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
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
            "",
            "",
            "OEM_1",
            "OEM_PLUS",
            "OEM_COMMA",
            "OEM_MINUS",
            "OEM_PERIOD",
            "OEM_2",
            "OEM_3",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "OEM_4",
            "OEM_5",
            "OEM_6",
            "OEM_7",
            "OEM_8",
            "",
            "",
            "OEM_102",
            "",
            "",
            "PROCESSKEY",
            "",
            "PACKET",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
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
        //private static string[] vkStrings = new string[]
        //{
        //    "HOTKEY",
        //    "LBUTTON",
        //    "RBUTTON",
        //    "CANCEL",
        //    "MBUTTON",
        //    "XBUTTON1",
        //    "XBUTTON2",
        //    "BACK",
        //    "TAB",
        //    "CLEAR",
        //    "RETURN",
        //    "SHIFT",
        //    "CONTROL",
        //    "MENU",
        //    "PAUSE",
        //    "CAPITAL",
        //    "KANA",
        //    "HANGUL",
        //    "JUNJA",
        //    "FINAL",
        //    "HANJA",
        //    "KANJI",
        //    "ESCAPE",
        //    "CONVERT",
        //    "NONCONVERT",
        //    "ACCEPT",
        //    "MODECHANGE",
        //    "SPACE",
        //    "PRIOR",
        //    "NEXT",
        //    "END",
        //    "HOME",
        //    "LEFT",
        //    "UP",
        //    "RIGHT",
        //    "DOWN",
        //    "SELECT",
        //    "PRINT",
        //    "EXECUTE",
        //    "SNAPSHOT",
        //    "INSERT",
        //    "DELETE",
        //    "HELP",
        //    "Zero",
        //    "One",
        //    "Two",
        //    "Three",
        //    "Four",
        //    "Five",
        //    "Six",
        //    "Seven",
        //    "Eight",
        //    "Nine",
        //    "A",
        //    "B",
        //    "C",
        //    "D",
        //    "E",
        //    "F",
        //    "G",
        //    "H",
        //    "I",
        //    "J",
        //    "K",
        //    "L",
        //    "M",
        //    "N",
        //    "O",
        //    "P",
        //    "Q",
        //    "R",
        //    "S",
        //    "T",
        //    "U",
        //    "V",
        //    "W",
        //    "X",
        //    "Y",
        //    "Z",
        //    "LWIN",
        //    "RWIN",
        //    "APPS",
        //    "SLEEP",
        //    "NUMPAD0",
        //    "NUMPAD1",
        //    "NUMPAD2",
        //    "NUMPAD3",
        //    "NUMPAD4",
        //    "NUMPAD5",
        //    "NUMPAD6",
        //    "NUMPAD7",
        //    "NUMPAD8",
        //    "NUMPAD9",
        //    "MULTIPLY",
        //    "ADD",
        //    "SEPARATOR",
        //    "SUBTRACT",
        //    "DECIMAL",
        //    "DIVIDE",
        //    "F1",
        //    "F2",
        //    "F3",
        //    "F4",
        //    "F5",
        //    "F6",
        //    "F7",
        //    "F8",
        //    "F9",
        //    "F10",
        //    "F11",
        //    "F12",
        //    "F13",
        //    "F14",
        //    "F15",
        //    "F16",
        //    "F17",
        //    "F18",
        //    "F19",
        //    "F20",
        //    "F21",
        //    "F22",
        //    "F23",
        //    "F24",
        //    "NUMLOCK",
        //    "SCROLL",
        //    "LSHIFT",
        //    "RSHIFT",
        //    "LCONTROL",
        //    "RCONTROL",
        //    "LMENU",
        //    "RMENU",
        //    "BROWSER_BACK",
        //    "BROWSER_FORWARD",
        //    "BROWSER_REFRESH",
        //    "BROWSER_STOP",
        //    "BROWSER_SEARCH",
        //    "BROWSER_FAVORITES",
        //    "BROWSER_HOME",
        //    "VOLUME_MUTE",
        //    "VOLUME_DOWN",
        //    "VOLUME_UP",
        //    "MEDIA_NEXT_TRACK",
        //    "MEDIA_PREV_TRACK",
        //    "MEDIA_STOP",
        //    "MEDIA_PLAY_PAUSE",
        //    "LAUNCH_MAIL",
        //    "LAUNCH_MEDIA_SELECT",
        //    "LAUNCH_APP1",
        //    "LAUNCH_APP2",
        //    "OEM_1",
        //    "OEM_PLUS",
        //    "OEM_COMMA",
        //    "OEM_MINUS",
        //    "OEM_PERIOD",
        //    "OEM_2",
        //    "OEM_3",
        //    "OEM_4",
        //    "OEM_5",
        //    "OEM_6",
        //    "OEM_7",
        //    "OEM_8",
        //    "OEM_102",
        //    "PROCESSKEY",
        //    "PACKET",
        //    "ATTN",
        //    "CRSEL",
        //    "EXSEL",
        //    "EREOF",
        //    "PLAY",
        //    "ZOOM",
        //    "NONAME",
        //    "PA1",
        //    "OEM_CLEAR",
        //};
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
namespace Yato.Input
{
    public enum KeyState : int
    {
        None,
        Up,
        Down
    }
}
namespace Yato.Input
{
    public class LowLevelKeyboardHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;
        private object lockObject;
        private PInvoke.HookProc keyboardProcReference;
        private GCHandle gcHandle;
        private IntPtr hookHandle;
        private uint hookThreadId;
        private Thread hookThread;
        public delegate void KeyboardHookCallback(KeyState state, VirtualKeyCode key);
        public event KeyboardHookCallback OnKeyCaptured;
        public LowLevelKeyboardHook()
        {
            lockObject = new object();
        }
        ~LowLevelKeyboardHook()
        {
            Dispose(false);
        }
        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle != IntPtr.Zero) return false;
                if (hookThreadId != 0) return false;
                if (hookThread != null) return false;
                hookThread = new Thread(InitializeHookThread)
                {
                    IsBackground = true
                };
                hookThread.Start();
                return true;
            }
        }
        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle == IntPtr.Zero) return false;
                if (hookThreadId == 0) return false;
                if (hookThread == null) return false;
                if (PInvoke.PostThreadMessage(hookThreadId, PInvoke.WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    try
                    {
                        hookThread.Join();
                    }
                    catch
                    {
                        // thread already exited
                    }
                }
                hookHandle = IntPtr.Zero;
                hookThreadId = 0;
                hookThread = null;
                return true;
            }
        }
        private void InitializeHookThread()
        {
            lock (lockObject)
            {
                hookThreadId = PInvoke.GetCurrentThreadId();
                //You are missing the effect that using a debugger has on the lifetime of local variables.With a debugger attached, the jitter marks the variables in use until the end of the method.Important to make debugging reliable.This however also prevents the GC.Collect() call from collecting the delegate object.
                //This code will crash when you run the Release build of your program without a debugger.
                keyboardProcReference = new PInvoke.HookProc(HookProcedure);
                gcHandle = GCHandle.Alloc(keyboardProcReference);
                GC.KeepAlive(keyboardProcReference); // GC does not touch any variables in this method until the method returns
                hookHandle = PInvoke.SetWindowsHookEx(PInvoke.WH_KEYBOARD_LL, keyboardProcReference, MainModuleHandle, 0);
                if (hookHandle == IntPtr.Zero)
                {
                    throw new Exception("Failed to create LowLevelKeyboardHook");
                }
            }
            // we need to start a message loop here to keep the hook working
            PInvoke.Message msg = new PInvoke.Message();
            // we actually do not care on any window messages
            while (PInvoke.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
            }
            // Unhook in the same thread
            PInvoke.UnhookWindowsHookEx(hookHandle);
            gcHandle.Free();
        }
        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0) // wParam and lParam are set
            {
                uint msg = (uint)wParam.ToInt32();
                try
                {
                    if (lParam == IntPtr.Zero) return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
                }
                catch
                {
                }
                VirtualKeyCode key = (VirtualKeyCode)Marshal.ReadInt32(lParam);
                switch (msg)
                {
                    case PInvoke.WM_KEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case PInvoke.WM_KEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
                        break;
                    case PInvoke.WM_SYSKEYDOWN:
                        OnKeyCaptured?.Invoke(KeyState.Down, key);
                        break;
                    case PInvoke.WM_SYSKEYUP:
                        OnKeyCaptured?.Invoke(KeyState.Up, key);
                        break;
                }
            }
            return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }
        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                try
                {
                    if(Monitor.TryEnter(lockObject))
                    {
                        Monitor.PulseAll(lockObject);
                        Monitor.Exit(lockObject);
                    }
                }
                catch
                {
                }
                UninstallHook();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
namespace Yato.Input
{
    public class LowLevelMouseHook : IDisposable
    {
        private static IntPtr MainModuleHandle = Process.GetCurrentProcess().MainModule.BaseAddress;
        private object lockObject;
        private PInvoke.HookProc mouseProcReference;
        private GCHandle gcHandle;
        private IntPtr hookHandle;
        private uint hookThreadId;
        private Thread hookThread;
        public delegate void MouseHookCallback(KeyState state, VirtualKeyCode key, int x, int y);
        public event MouseHookCallback OnMouseCaptured;
        public bool CaptureMouseMove;
        public bool IsLeftMouseButtonPressed;
        public bool IsRightMouseButtonPressed;
        public bool IsMiddleMouseButtonPressed;
        public bool IsXButton1Pressed;
        public bool IsXButton2Pressed;
        public LowLevelMouseHook()
        {
            lockObject = new object();
        }
        public LowLevelMouseHook(bool captureMouseMove)
        {
            lockObject = new object();
            CaptureMouseMove = captureMouseMove;
        }
        ~LowLevelMouseHook()
        {
            Dispose(false);
        }
        public bool InstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle != IntPtr.Zero) return false;
                if (hookThreadId != 0) return false;
                if (hookThread != null) return false;
                hookThread = new Thread(InitializeHookThread)
                {
                    IsBackground = true
                };
                hookThread.Start();
                return true;
            }
        }
        public bool UninstallHook()
        {
            lock (lockObject)
            {
                if (hookHandle == IntPtr.Zero) return false;
                if (hookThreadId == 0) return false;
                if (hookThread == null) return false;
                if (PInvoke.PostThreadMessage(hookThreadId, PInvoke.WM_QUIT, IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    try
                    {
                        hookThread.Join();
                    }
                    catch
                    {
                        // thread already exited
                    }
                }
                hookHandle = IntPtr.Zero;
                hookThreadId = 0;
                hookThread = null;
                return true;
            }
        }
        private void InitializeHookThread()
        {
            lock (lockObject)
            {
                hookThreadId = PInvoke.GetCurrentThreadId();
                //You are missing the effect that using a debugger has on the lifetime of local variables.With a debugger attached, the jitter marks the variables in use until the end of the method.Important to make debugging reliable.This however also prevents the GC.Collect() call from collecting the delegate object.
                //This code will crash when you run the Release build of your program without a debugger.
                
                mouseProcReference = new PInvoke.HookProc(HookProcedure);
                gcHandle = GCHandle.Alloc(mouseProcReference);
                GC.KeepAlive(mouseProcReference); // GC does not touch any variables in this method until the method returns
                hookHandle = PInvoke.SetWindowsHookEx(PInvoke.WH_MOUSE_LL, HookProcedure, MainModuleHandle, 0);
                if (hookHandle == IntPtr.Zero)
                {
                    throw new Exception("Failed to create LowLevelKeyboardHook");
                }
            }
            // we need to start a message loop here to keep the hook working
            PInvoke.Message msg = new PInvoke.Message();
            // we actually do not care on any window messages
            while (PInvoke.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
            {
            }
            // Unhook in the same thread
            PInvoke.UnhookWindowsHookEx(hookHandle);
            gcHandle.Free();
        }
        private IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == 0) // wParam and lParam are set
            {
                IsMiddleMouseButtonPressed = false; // reset
                uint msg = (uint)wParam.ToInt32();
                try
                {
                    if (lParam == IntPtr.Zero) return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
                }
                catch
                {
                }
                int x = Marshal.ReadInt32(lParam);
                int y = Marshal.ReadInt32(lParam + 4);
                int mouseData = Marshal.ReadInt32(lParam + 8);
                switch (msg)
                {
                    case PInvoke.WM_LBUTTONDOWN:
                        IsLeftMouseButtonPressed = true;
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case PInvoke.WM_LBUTTONUP:
                        IsLeftMouseButtonPressed = false;
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.LBUTTON, x, y);
                        break;
                    case PInvoke.WM_MOUSEHWHEEL:
                        // get the high word:
                        short hiword = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);
                        if(hiword == 120) // clicked the mouse wheel button
                        {
                            IsMiddleMouseButtonPressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;
                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword, hiword);
                        }
                        break;
                    case PInvoke.WM_MOUSEMOVE:
                        if (!CaptureMouseMove) break;
                        OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.NONAME, x, y);
                        break;
                    case PInvoke.WM_MOUSEWHEEL:
                        // get the high word:
                        short hiword_2 = BitConverter.ToInt16(BitConverter.GetBytes(mouseData), 0);
                        if (hiword_2 == 120) // clicked the mouse wheel button
                        {
                            IsMiddleMouseButtonPressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.MBUTTON, x, y);
                        }
                        else
                        {
                            if (!CaptureMouseMove) break;
                            OnMouseCaptured?.Invoke(KeyState.None, VirtualKeyCode.SCROLL, hiword_2, hiword_2);
                        }
                        break;
                    case PInvoke.WM_RBUTTONDOWN:
                        IsRightMouseButtonPressed = true;
                        OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case PInvoke.WM_RBUTTONUP:
                        IsRightMouseButtonPressed = false;
                        OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.RBUTTON, x, y);
                        break;
                    case PInvoke.WM_XBUTTONDOWN:
                        if(mouseData == 0x1)
                        {
                            IsXButton1Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_XBUTTONUP:
                        if(mouseData == 0x1)
                        {
                            IsXButton1Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_XBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                    case PInvoke.WM_NCXBUTTONDOWN:
                        if (mouseData == 0x1)
                        {
                            IsXButton1Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = true;
                            OnMouseCaptured?.Invoke(KeyState.Down, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_NCXBUTTONUP:
                        if (mouseData == 0x1)
                        {
                            IsXButton1Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON1, x, y);
                        }
                        else
                        {
                            IsXButton2Pressed = false;
                            OnMouseCaptured?.Invoke(KeyState.Up, VirtualKeyCode.XBUTTON2, x, y);
                        }
                        break;
                    case PInvoke.WM_NCXBUTTONDBLCLK:
                        OnMouseCaptured?.Invoke(KeyState.Down, mouseData == 0x1 ? VirtualKeyCode.XBUTTON1 : VirtualKeyCode.XBUTTON2, x, y);
                        break;
                }
            }
            return PInvoke.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }
        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                try
                {
                    if (Monitor.TryEnter(lockObject))
                    {
                        Monitor.PulseAll(lockObject);
                        Monitor.Exit(lockObject);
                    }
                }
                catch
                {
                }
                UninstallHook();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
namespace Yato.Input
{
    internal static class PInvoke
    {
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        public const int WH_KEYBOARD_LL = 13;
        public const uint WM_QUIT = 0x0012;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_SYSKEYDOWN = 0x0104;
        public const uint WM_SYSKEYUP = 0x0105;
        public const int WH_MOUSE_LL = 14;
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;
        public const uint WM_MOUSEWHEEL = 0x020A;
        public const uint WM_MOUSEHWHEEL = 0x020E;
        public const uint WM_XBUTTONDOWN = 0x020B;
        public const uint WM_XBUTTONUP = 0x020C;
        public const uint WM_XBUTTONDBLCLK = 0x020D;
        public const uint WM_NCXBUTTONDOWN = 0x00AB;
        public const uint WM_NCXBUTTONUP = 0x00AC;
        public const uint WM_NCXBUTTONDBLCLK = 0x00AD;
        public delegate IntPtr SetWindowsHookEx_t(int type, [MarshalAs(UnmanagedType.FunctionPtr)] HookProc hookProcedure, IntPtr hModule, uint threadId);
        public static SetWindowsHookEx_t SetWindowsHookEx = WinApi.GetMethod<SetWindowsHookEx_t>("user32.dll", "SetWindowsHookExW");
        public delegate int UnhookWindowsHookEx_t(IntPtr hHook);
        public static UnhookWindowsHookEx_t UnhookWindowsHookEx = WinApi.GetMethod<UnhookWindowsHookEx_t>("user32.dll", "UnhookWindowsHookEx");
        public delegate IntPtr CallNextHookEx_t(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);
        public static CallNextHookEx_t CallNextHookEx = WinApi.GetMethod<CallNextHookEx_t>("user32.dll", "CallNextHookEx");
        public delegate int GetMessage_t(ref Message lpMessage, IntPtr hwnd, uint msgFilterMin, uint msgFilterMax);
        public static GetMessage_t GetMessage = WinApi.GetMethod<GetMessage_t>("user32.dll", "GetMessageW");
        public delegate int PostThreadMessage_t(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);
        public static PostThreadMessage_t PostThreadMessage = WinApi.GetMethod<PostThreadMessage_t>("user32.dll", "PostThreadMessageW");
        public delegate uint GetCurrentThreadId_t();
        public static GetCurrentThreadId_t GetCurrentThreadId = WinApi.GetMethod<GetCurrentThreadId_t>("kernel32.dll", "GetCurrentThreadId");
        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr Hwnd;
            public uint Msg;
            public IntPtr lParam;
            public IntPtr wParam;
            public uint Time;
            public int X;
            public int Y;
        }
    }
    #region LoadLibrary and GetProcAddress
    internal static class WinApi
    {
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr getProcAddress(IntPtr hmodule, string procName);
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern IntPtr loadLibraryW(string lpFileName);
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern IntPtr getModuleHandle(string modulename);
        public static IntPtr GetProcAddress(string modulename, string procname)
        {
            IntPtr hModule = getModuleHandle(modulename);
            if (hModule == IntPtr.Zero) hModule = loadLibraryW(modulename);
            return getProcAddress(hModule, procname);
        }
        public static T GetMethod<T>(string modulename, string procname)
        {
            IntPtr hModule = getModuleHandle(modulename);
            if (hModule == IntPtr.Zero) hModule = loadLibraryW(modulename);
            IntPtr procAddress = getProcAddress(hModule, procname);
#if DEBUG
            if (hModule == IntPtr.Zero || procAddress == IntPtr.Zero)
                throw new Exception("module: " + modulename + "\tproc: " + procname);
#endif
            return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, ObfuscatorNeedsThis<T>());
        }
        private static Type ObfuscatorNeedsThis<T>()
        {
            return typeof(T);
        }
    }
    #endregion
}
namespace Yato.Input
{
    public enum VirtualKeyCode : int
    {
        HOTKEY = 0x0,
        ///<summary>
        ///Left mouse button
        ///</summary>
        LBUTTON = 0x01,
        ///<summary>
        ///Right mouse button
        ///</summary>
        RBUTTON = 0x02,
        ///<summary>
        ///Control-break processing
        ///</summary>
        CANCEL = 0x03,
        ///<summary>
        ///Middle mouse button (three-button mouse)
        ///</summary>
        MBUTTON = 0x04,
        ///<summary>
        ///Windows 2000/XP: X1 mouse button
        ///</summary>
        XBUTTON1 = 0x05,
        ///<summary>
        ///Windows 2000/XP: X2 mouse button
        ///</summary>
        XBUTTON2 = 0x06,
        ///<summary>
        ///BACKSPACE key
        ///</summary>
        BACK = 0x08,
        ///<summary>
        ///TAB key
        ///</summary>
        TAB = 0x09,
        ///<summary>
        ///CLEAR key
        ///</summary>
        CLEAR = 0x0C,
        ///<summary>
        ///ENTER key
        ///</summary>
        RETURN = 0x0D,
        ///<summary>
        ///SHIFT key
        ///</summary>
        SHIFT = 0x10,
        ///<summary>
        ///CTRL key
        ///</summary>
        CONTROL = 0x11,
        ///<summary>
        ///ALT key
        ///</summary>
        MENU = 0x12,
        ///<summary>
        ///PAUSE key
        ///</summary>
        PAUSE = 0x13,
        ///<summary>
        ///CAPS LOCK key
        ///</summary>
        CAPITAL = 0x14,
        ///<summary>
        ///Input Method Editor (IME) Kana mode
        ///</summary>
        KANA = 0x15,
        ///<summary>
        ///IME Hangul mode
        ///</summary>
        HANGUL = 0x15,
        ///<summary>
        ///IME Junja mode
        ///</summary>
        JUNJA = 0x17,
        ///<summary>
        ///IME final mode
        ///</summary>
        FINAL = 0x18,
        ///<summary>
        ///IME Hanja mode
        ///</summary>
        HANJA = 0x19,
        ///<summary>
        ///IME Kanji mode
        ///</summary>
        KANJI = 0x19,
        ///<summary>
        ///ESC key
        ///</summary>
        ESCAPE = 0x1B,
        ///<summary>
        ///IME convert
        ///</summary>
        CONVERT = 0x1C,
        ///<summary>
        ///IME nonconvert
        ///</summary>
        NONCONVERT = 0x1D,
        ///<summary>
        ///IME accept
        ///</summary>
        ACCEPT = 0x1E,
        ///<summary>
        ///IME mode change request
        ///</summary>
        MODECHANGE = 0x1F,
        ///<summary>
        ///SPACEBAR
        ///</summary>
        SPACE = 0x20,
        ///<summary>
        ///PAGE UP key
        ///</summary>
        PRIOR = 0x21,
        ///<summary>
        ///PAGE DOWN key
        ///</summary>
        NEXT = 0x22,
        ///<summary>
        ///END key
        ///</summary>
        END = 0x23,
        ///<summary>
        ///HOME key
        ///</summary>
        HOME = 0x24,
        ///<summary>
        ///LEFT ARROW key
        ///</summary>
        LEFT = 0x25,
        ///<summary>
        ///UP ARROW key
        ///</summary>
        UP = 0x26,
        ///<summary>
        ///RIGHT ARROW key
        ///</summary>
        RIGHT = 0x27,
        ///<summary>
        ///DOWN ARROW key
        ///</summary>
        DOWN = 0x28,
        ///<summary>
        ///SELECT key
        ///</summary>
        SELECT = 0x29,
        ///<summary>
        ///PRINT key
        ///</summary>
        PRINT = 0x2A,
        ///<summary>
        ///EXECUTE key
        ///</summary>
        EXECUTE = 0x2B,
        ///<summary>
        ///PRINT SCREEN key
        ///</summary>
        SNAPSHOT = 0x2C,
        ///<summary>
        ///INS key
        ///</summary>
        INSERT = 0x2D,
        ///<summary>
        ///DEL key
        ///</summary>
        DELETE = 0x2E,
        ///<summary>
        ///HELP key
        ///</summary>
        HELP = 0x2F,
        ///<summary>
        ///0 key
        ///</summary>
        Zero = 0x30,
        ///<summary>
        ///1 key
        ///</summary>
        One = 0x31,
        ///<summary>
        ///2 key
        ///</summary>
        Two = 0x32,
        ///<summary>
        ///3 key
        ///</summary>
        Three = 0x33,
        ///<summary>
        ///4 key
        ///</summary>
        Four = 0x34,
        ///<summary>
        ///5 key
        ///</summary>
        Five = 0x35,
        ///<summary>
        ///6 key
        ///</summary>
        Six = 0x36,
        ///<summary>
        ///7 key
        ///</summary>
        Seven = 0x37,
        ///<summary>
        ///8 key
        ///</summary>
        Eight = 0x38,
        ///<summary>
        ///9 key
        ///</summary>
        Nine = 0x39,
        ///<summary>
        ///A key
        ///</summary>
        A = 0x41,
        ///<summary>
        ///B key
        ///</summary>
        B = 0x42,
        ///<summary>
        ///C key
        ///</summary>
        C = 0x43,
        ///<summary>
        ///D key
        ///</summary>
        D = 0x44,
        ///<summary>
        ///E key
        ///</summary>
        E = 0x45,
        ///<summary>
        ///F key
        ///</summary>
        F = 0x46,
        ///<summary>
        ///G key
        ///</summary>
        G = 0x47,
        ///<summary>
        ///H key
        ///</summary>
        H = 0x48,
        ///<summary>
        ///I key
        ///</summary>
        I = 0x49,
        ///<summary>
        ///J key
        ///</summary>
        J = 0x4A,
        ///<summary>
        ///K key
        ///</summary>
        K = 0x4B,
        ///<summary>
        ///L key
        ///</summary>
        L = 0x4C,
        ///<summary>
        ///M key
        ///</summary>
        M = 0x4D,
        ///<summary>
        ///N key
        ///</summary>
        N = 0x4E,
        ///<summary>
        ///O key
        ///</summary>
        O = 0x4F,
        ///<summary>
        ///P key
        ///</summary>
        P = 0x50,
        ///<summary>
        ///Q key
        ///</summary>
        Q = 0x51,
        ///<summary>
        ///R key
        ///</summary>
        R = 0x52,
        ///<summary>
        ///S key
        ///</summary>
        S = 0x53,
        ///<summary>
        ///T key
        ///</summary>
        T = 0x54,
        ///<summary>
        ///U key
        ///</summary>
        U = 0x55,
        ///<summary>
        ///V key
        ///</summary>
        V = 0x56,
        ///<summary>
        ///W key
        ///</summary>
        W = 0x57,
        ///<summary>
        ///X key
        ///</summary>
        X = 0x58,
        ///<summary>
        ///Y key
        ///</summary>
        Y = 0x59,
        ///<summary>
        ///Z key
        ///</summary>
        Z = 0x5A,
        ///<summary>
        ///Left Windows key (Microsoft Natural keyboard) 
        ///</summary>
        LWIN = 0x5B,
        ///<summary>
        ///Right Windows key (Natural keyboard)
        ///</summary>
        RWIN = 0x5C,
        ///<summary>
        ///Applications key (Natural keyboard)
        ///</summary>
        APPS = 0x5D,
        ///<summary>
        ///Computer Sleep key
        ///</summary>
        SLEEP = 0x5F,
        ///<summary>
        ///Numeric keypad 0 key
        ///</summary>
        NUMPAD0 = 0x60,
        ///<summary>
        ///Numeric keypad 1 key
        ///</summary>
        NUMPAD1 = 0x61,
        ///<summary>
        ///Numeric keypad 2 key
        ///</summary>
        NUMPAD2 = 0x62,
        ///<summary>
        ///Numeric keypad 3 key
        ///</summary>
        NUMPAD3 = 0x63,
        ///<summary>
        ///Numeric keypad 4 key
        ///</summary>
        NUMPAD4 = 0x64,
        ///<summary>
        ///Numeric keypad 5 key
        ///</summary>
        NUMPAD5 = 0x65,
        ///<summary>
        ///Numeric keypad 6 key
        ///</summary>
        NUMPAD6 = 0x66,
        ///<summary>
        ///Numeric keypad 7 key
        ///</summary>
        NUMPAD7 = 0x67,
        ///<summary>
        ///Numeric keypad 8 key
        ///</summary>
        NUMPAD8 = 0x68,
        ///<summary>
        ///Numeric keypad 9 key
        ///</summary>
        NUMPAD9 = 0x69,
        ///<summary>
        ///Multiply key
        ///</summary>
        MULTIPLY = 0x6A,
        ///<summary>
        ///Add key
        ///</summary>
        ADD = 0x6B,
        ///<summary>
        ///Separator key
        ///</summary>
        SEPARATOR = 0x6C,
        ///<summary>
        ///Subtract key
        ///</summary>
        SUBTRACT = 0x6D,
        ///<summary>
        ///Decimal key
        ///</summary>
        DECIMAL = 0x6E,
        ///<summary>
        ///Divide key
        ///</summary>
        DIVIDE = 0x6F,
        ///<summary>
        ///F1 key
        ///</summary>
        F1 = 0x70,
        ///<summary>
        ///F2 key
        ///</summary>
        F2 = 0x71,
        ///<summary>
        ///F3 key
        ///</summary>
        F3 = 0x72,
        ///<summary>
        ///F4 key
        ///</summary>
        F4 = 0x73,
        ///<summary>
        ///F5 key
        ///</summary>
        F5 = 0x74,
        ///<summary>
        ///F6 key
        ///</summary>
        F6 = 0x75,
        ///<summary>
        ///F7 key
        ///</summary>
        F7 = 0x76,
        ///<summary>
        ///F8 key
        ///</summary>
        F8 = 0x77,
        ///<summary>
        ///F9 key
        ///</summary>
        F9 = 0x78,
        ///<summary>
        ///F10 key
        ///</summary>
        F10 = 0x79,
        ///<summary>
        ///F11 key
        ///</summary>
        F11 = 0x7A,
        ///<summary>
        ///F12 key
        ///</summary>
        F12 = 0x7B,
        ///<summary>
        ///F13 key
        ///</summary>
        F13 = 0x7C,
        ///<summary>
        ///F14 key
        ///</summary>
        F14 = 0x7D,
        ///<summary>
        ///F15 key
        ///</summary>
        F15 = 0x7E,
        ///<summary>
        ///F16 key
        ///</summary>
        F16 = 0x7F,
        ///<summary>
        ///F17 key  
        ///</summary>
        F17 = 0x80,
        ///<summary>
        ///F18 key  
        ///</summary>
        F18 = 0x81,
        ///<summary>
        ///F19 key  
        ///</summary>
        F19 = 0x82,
        ///<summary>
        ///F20 key  
        ///</summary>
        F20 = 0x83,
        ///<summary>
        ///F21 key  
        ///</summary>
        F21 = 0x84,
        ///<summary>
        ///F22 key, (PPC only) Key used to lock device.
        ///</summary>
        F22 = 0x85,
        ///<summary>
        ///F23 key  
        ///</summary>
        F23 = 0x86,
        ///<summary>
        ///F24 key  
        ///</summary>
        F24 = 0x87,
        ///<summary>
        ///NUM LOCK key
        ///</summary>
        NUMLOCK = 0x90,
        ///<summary>
        ///SCROLL LOCK key
        ///</summary>
        SCROLL = 0x91,
        ///<summary>
        ///Left SHIFT key
        ///</summary>
        LSHIFT = 0xA0,
        ///<summary>
        ///Right SHIFT key
        ///</summary>
        RSHIFT = 0xA1,
        ///<summary>
        ///Left CONTROL key
        ///</summary>
        LCONTROL = 0xA2,
        ///<summary>
        ///Right CONTROL key
        ///</summary>
        RCONTROL = 0xA3,
        ///<summary>
        ///Left MENU key
        ///</summary>
        LMENU = 0xA4,
        ///<summary>
        ///Right MENU key
        ///</summary>
        RMENU = 0xA5,
        ///<summary>
        ///Windows 2000/XP: Browser Back key
        ///</summary>
        BROWSER_BACK = 0xA6,
        ///<summary>
        ///Windows 2000/XP: Browser Forward key
        ///</summary>
        BROWSER_FORWARD = 0xA7,
        ///<summary>
        ///Windows 2000/XP: Browser Refresh key
        ///</summary>
        BROWSER_REFRESH = 0xA8,
        ///<summary>
        ///Windows 2000/XP: Browser Stop key
        ///</summary>
        BROWSER_STOP = 0xA9,
        ///<summary>
        ///Windows 2000/XP: Browser Search key 
        ///</summary>
        BROWSER_SEARCH = 0xAA,
        ///<summary>
        ///Windows 2000/XP: Browser Favorites key
        ///</summary>
        BROWSER_FAVORITES = 0xAB,
        ///<summary>
        ///Windows 2000/XP: Browser Start and Home key
        ///</summary>
        BROWSER_HOME = 0xAC,
        ///<summary>
        ///Windows 2000/XP: Volume Mute key
        ///</summary>
        VOLUME_MUTE = 0xAD,
        ///<summary>
        ///Windows 2000/XP: Volume Down key
        ///</summary>
        VOLUME_DOWN = 0xAE,
        ///<summary>
        ///Windows 2000/XP: Volume Up key
        ///</summary>
        VOLUME_UP = 0xAF,
        ///<summary>
        ///Windows 2000/XP: Next Track key
        ///</summary>
        MEDIA_NEXT_TRACK = 0xB0,
        ///<summary>
        ///Windows 2000/XP: Previous Track key
        ///</summary>
        MEDIA_PREV_TRACK = 0xB1,
        ///<summary>
        ///Windows 2000/XP: Stop Media key
        ///</summary>
        MEDIA_STOP = 0xB2,
        ///<summary>
        ///Windows 2000/XP: Play/Pause Media key
        ///</summary>
        MEDIA_PLAY_PAUSE = 0xB3,
        ///<summary>
        ///Windows 2000/XP: Start Mail key
        ///</summary>
        LAUNCH_MAIL = 0xB4,
        ///<summary>
        ///Windows 2000/XP: Select Media key
        ///</summary>
        LAUNCH_MEDIA_SELECT = 0xB5,
        ///<summary>
        ///Windows 2000/XP: Start Application 1 key
        ///</summary>
        LAUNCH_APP1 = 0xB6,
        ///<summary>
        ///Windows 2000/XP: Start Application 2 key
        ///</summary>
        LAUNCH_APP2 = 0xB7,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_1 = 0xBA,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '+' key
        ///</summary>
        OEM_PLUS = 0xBB,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the ',' key
        ///</summary>
        OEM_COMMA = 0xBC,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '-' key
        ///</summary>
        OEM_MINUS = 0xBD,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '.' key
        ///</summary>
        OEM_PERIOD = 0xBE,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_2 = 0xBF,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard. 
        ///</summary>
        OEM_3 = 0xC0,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard. 
        ///</summary>
        OEM_4 = 0xDB,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard. 
        ///</summary>
        OEM_5 = 0xDC,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard. 
        ///</summary>
        OEM_6 = 0xDD,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard. 
        ///</summary>
        OEM_7 = 0xDE,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_8 = 0xDF,
        ///<summary>
        ///Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
        ///</summary>
        OEM_102 = 0xE2,
        ///<summary>
        ///Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key
        ///</summary>
        PROCESSKEY = 0xE5,
        ///<summary>
        ///Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes.
        ///The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information,
        ///see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
        ///</summary>
        PACKET = 0xE7,
        ///<summary>
        ///Attn key
        ///</summary>
        ATTN = 0xF6,
        ///<summary>
        ///CrSel key
        ///</summary>
        CRSEL = 0xF7,
        ///<summary>
        ///ExSel key
        ///</summary>
        EXSEL = 0xF8,
        ///<summary>
        ///Erase EOF key
        ///</summary>
        EREOF = 0xF9,
        ///<summary>
        ///Play key
        ///</summary>
        PLAY = 0xFA,
        ///<summary>
        ///Zoom key
        ///</summary>
        ZOOM = 0xFB,
        ///<summary>
        ///Reserved 
        ///</summary>
        NONAME = 0xFC,
        ///<summary>
        ///PA1 key
        ///</summary>
        PA1 = 0xFD,
        ///<summary>
        ///Clear key
        ///</summary>
        OEM_CLEAR = 0xFE
    }
}
