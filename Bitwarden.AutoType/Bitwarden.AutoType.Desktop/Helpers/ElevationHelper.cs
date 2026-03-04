using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Helpers;

public static class ElevationHelper
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    public static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a target window requires elevation to automate.
    /// Returns true if the window has higher integrity level than current process.
    /// </summary>
    public static bool DoesWindowRequireElevation(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        try
        {
            // Get the process ID of the target window
            WindowsDLLs.GetWindowThreadProcessId(windowHandle, out uint processId);
            
            if (processId == 0)
                return false;

            // Open the process to check its integrity level
            IntPtr processHandle = WindowsDLLs.OpenProcess(
                WindowsConstants.PROCESS_QUERY_LIMITED_INFORMATION, 
                false, 
                processId);

            if (processHandle == IntPtr.Zero)
            {
                // If we can't open the process, it likely requires elevation
                return true;
            }

            try
            {
                // Get the process token
                if (!WindowsDLLs.OpenProcessToken(processHandle, WindowsConstants.TOKEN_QUERY, out IntPtr tokenHandle))
                {
                    return true;
                }

                try
                {
                    // Get the integrity level of the target process
                    int targetIntegrityLevel = GetProcessIntegrityLevel(tokenHandle);
                    
                    // Get our own integrity level
                    using var currentIdentity = WindowsIdentity.GetCurrent();
                    int currentIntegrityLevel = GetProcessIntegrityLevel(currentIdentity.Token);

                    // If target has higher integrity level, we need elevation
                    return targetIntegrityLevel > currentIntegrityLevel;
                }
                finally
                {
                    WindowsDLLs.CloseHandle(tokenHandle);
                }
            }
            finally
            {
                WindowsDLLs.CloseHandle(processHandle);
            }
        }
        catch
        {
            // If we can't determine, assume it might require elevation
            return false;
        }
    }

    /// <summary>
    /// Restarts the application with administrator privileges.
    /// </summary>
    /// <returns>True if restart was initiated successfully.</returns>
    public static bool RestartAsAdministrator()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas" // This triggers UAC prompt
            };

            Process.Start(startInfo);
            
            // If we successfully started the elevated process, exit this instance
            Environment.Exit(0);
            return true;
        }
        catch (Exception)
        {
            // User cancelled UAC or other error
            return false;
        }
    }

    private static int GetProcessIntegrityLevel(IntPtr tokenHandle)
    {
        try
        {
            // Get the integrity level from the token
            int length = 0;
            WindowsDLLs.GetTokenInformation(
                tokenHandle, 
                WindowsConstants.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, 
                IntPtr.Zero, 
                0, 
                out length);

            IntPtr tokenInfo = Marshal.AllocHGlobal(length);
            try
            {
                if (WindowsDLLs.GetTokenInformation(
                    tokenHandle,
                    WindowsConstants.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                    tokenInfo,
                    length,
                    out length))
                {
                    var til = Marshal.PtrToStructure<WindowsConstants.TOKEN_MANDATORY_LABEL>(tokenInfo);
                    IntPtr pSid = til.Label.Sid;
                    
                    int subAuthorityCount = Marshal.ReadByte(pSid, 1);
                    IntPtr pIntegrityLevel = new IntPtr(pSid.ToInt64() + 8 + (subAuthorityCount - 1) * 4);
                    return Marshal.ReadInt32(pIntegrityLevel);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInfo);
            }
        }
        catch
        {
            // Return medium integrity level as default
            return 0x2000; // SECURITY_MANDATORY_MEDIUM_RID
        }

        return 0x2000;
    }
}
