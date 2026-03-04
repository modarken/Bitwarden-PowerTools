#pragma warning disable



using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Bitwarden.AutoType.Desktop.Helpers;

internal class TextFileHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    public const uint WM_SETTEXT = 0x000C;

    public static void OpenTextFileAndTypeText(string fileName, string text)
    {
        // Add .txt extension to the fileName
        fileName = $"{fileName}.txt";

        // Create a temporary .txt file and write the text to it
        string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(tempFilePath, text);

        // Start the associated process for .txt files and open the temporary file
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{tempFilePath}\"",
            UseShellExecute = true
        };
        Process txtProcess = Process.Start(startInfo);

        // Wait for the process to start
        txtProcess.WaitForInputIdle();

        // Delete the temporary file after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                // File deletion failed, handle the exception if needed
            }
        });
    }

}

internal class NotepadHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    public const uint WM_SETTEXT = 0x000C;

    public static void OpenNotepadAndTypeText(string text)
    {
        // Start a new Notepad process
        Process notepad = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "notepad.exe"
            }
        };
        notepad.Start();

        // Wait for the Notepad process to become idle
        notepad.WaitForInputIdle();

        // Get the handle to the edit control
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        IntPtr editControl = FindWindowEx(notepad.MainWindowHandle, IntPtr.Zero, "Edit", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Send the entire text at once
        SendMessage(editControl, WM_SETTEXT, IntPtr.Zero, text);
    }
}

#pragma warning restore