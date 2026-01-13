using System;
using System.Runtime.InteropServices;

namespace SvonyBrowser.Helpers
{
    /// <summary>
    /// Win32 API imports for DLL and window management.
    /// Used primarily for CefSharp DLL loading from custom paths.
    /// </summary>
    public static class Win32
    {
        /// <summary>
        /// Sets the directory from which to load DLLs.
        /// This is critical for loading CefSharp DLLs from Assets\CefSharp folder.
        /// </summary>
        /// <param name="lpPathName">The path to the directory containing DLLs.</param>
        /// <returns>True if successful, false otherwise.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Gets the last Win32 error code.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        // Window style constants
        public const int WS_CAPTION = 0x00C00000;
        public const int GWL_STYLE = -16;

        /// <summary>
        /// Gets the window style for a window handle.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Sets the window style for a window handle.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Gets the window style (wrapper method).
        /// </summary>
        public static int GetWindowStyle(IntPtr hwnd)
        {
            return GetWindowLong(hwnd, GWL_STYLE);
        }

        /// <summary>
        /// Sets the window style (wrapper method).
        /// </summary>
        public static void SetWindowStyle(IntPtr hwnd, int style)
        {
            SetWindowLong(hwnd, GWL_STYLE, style);
        }
    }
}
