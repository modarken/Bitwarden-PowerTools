using System;
using System.Runtime.InteropServices;

namespace Bitwarden.AutoType.Desktop.Windows.Native;

public static class WindowsConstants
{
    public const int WM_HOTKEY = 0x0312;

    public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const int KEYEVENTF_KEYDOWN = 0x0000;
    public const int KEYEVENTF_KEYUP = 0x0002;
    public const int KEYEVENTF_SILENT = 0x0004;
    public const int KEYEVENTF_VK_OFF = 0x00DF;

    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    // Process access rights
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    // Token access rights
    public const uint TOKEN_QUERY = 0x0008;

    // Token information classes
    public enum TOKEN_INFORMATION_CLASS
    {
        TokenIntegrityLevel = 25
    }

    // Integrity levels
    public const int SECURITY_MANDATORY_UNTRUSTED_RID = 0x0000;
    public const int SECURITY_MANDATORY_LOW_RID = 0x1000;
    public const int SECURITY_MANDATORY_MEDIUM_RID = 0x2000;
    public const int SECURITY_MANDATORY_MEDIUM_PLUS_RID = 0x2100;
    public const int SECURITY_MANDATORY_HIGH_RID = 0x3000;
    public const int SECURITY_MANDATORY_SYSTEM_RID = 0x4000;

    // Structures for token integrity level
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_MANDATORY_LABEL
    {
        public SID_AND_ATTRIBUTES Label;
    }
}