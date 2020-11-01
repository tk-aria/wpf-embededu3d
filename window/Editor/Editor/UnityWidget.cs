using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace Editor
{

    public class UnityWidget : HwndHost
    {
        private const uint STARTF_USEPOSITION = 0x0004;
        private const uint STARTF_USESIZE = 0x0002;

        private Process _childProcess;
        private HandleRef _childHandleRef;

        public string AppPath { get; set; }
        public HandleRef Child => _childHandleRef;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var sInfo = new STARTUPINFO { dwFlags = STARTF_USESIZE | STARTF_USEPOSITION };
            var pSec = new SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>() };
            var tSec = new SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>() };
            var cmdline = $"-parentHWND {hwndParent.Handle}";
            var success = Kernel32.CreateProcess(AppPath, cmdline, ref pSec, ref tSec,
                false, 0, IntPtr.Zero, null, ref sInfo, out var pInfo);
            if (!success) throw new Exception("Failed to create child process");
            _childProcess = Process.GetProcessById(pInfo.dwProcessId);
            while (true)
            {
                var hwndChild = User32.FindWindowEx(hwndParent.Handle, IntPtr.Zero, null, null);
                if (hwndChild != IntPtr.Zero)
                {
                    return (_childHandleRef = new HandleRef(this, hwndChild));
                }
                Thread.Sleep(100);
            }
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            _childProcess.Dispose();
        }
    }

    static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
    }

    static class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hParent, IntPtr hChildAfter, string pClassName, string pWindowName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }
}