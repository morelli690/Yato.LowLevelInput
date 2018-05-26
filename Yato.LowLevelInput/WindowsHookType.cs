using System;

namespace Yato.LowLevelInput
{
    public enum WindowsHookType
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
        Debug = 9,
        Shell = 10,
        LowLevelKeyboard = 13,
        LowLevelMouse = 14,
    }
}