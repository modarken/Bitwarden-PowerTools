using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Bitwarden.AutoType.Desktop.Windows.Native;

public static class WindowsAPI
{
    public static (IntPtr, Process) GetForegroundProcess()
    {
        IntPtr hWnd = WindowsDLLs.GetForegroundWindow();
        _ = WindowsDLLs.GetWindowThreadProcessId(hWnd, out uint processId);
        return (hWnd, Process.GetProcessById((int)processId));
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        const int maxTitleLength = 256;
        var titleBuilder = new StringBuilder(maxTitleLength);

        if (WindowsDLLs.GetWindowText(hWnd, titleBuilder, maxTitleLength) > 0)
        {
            return titleBuilder.ToString();
        }

        return string.Empty;
    }

    public static string GetWindowClassName(IntPtr hWnd)
    {
        const int maxClassNameLength = 256;
        StringBuilder className = new StringBuilder(maxClassNameLength);
        int classNameLength = WindowsDLLs.GetClassName(hWnd, className, className.Capacity);
        if (classNameLength == 0)
        {
            // An error occurred, handle it as needed
            return string.Empty;
        }
        return className.ToString();
    }
}

public static class WindowsDLLs
{
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unregisterhotkey
    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern ushort VkKeyScan(char ch); // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-vkkeyscana?redirectedfrom=MSDN

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

    // Process and token APIs for elevation detection
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        WindowsConstants.TOKEN_INFORMATION_CLASS tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);
}