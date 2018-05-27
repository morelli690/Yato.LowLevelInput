using System;

namespace Yato.LowLevelInput.WindowsHooks
{
    internal enum WindowsHookType
    {
        MsgFilter = -1,
        JournalRecord = 0,
        JournalPlayback = 1,
        Keyboard = 2,
        GetMessage = 3,
        CallWndProc = 4,
        CBT = 5,
        SysMsgFilter = 6,
        Mouse = 7,
        Undocumented = 8,
        Debug = 9,
        Shell = 10,
        ForegroundIdle = 11,
        CallWndProcRet = 12,
        LowLevelKeyboard = 13,
        LowLevelMouse = 14,
    }
}