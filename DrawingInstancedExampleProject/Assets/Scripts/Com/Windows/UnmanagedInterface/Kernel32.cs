using System;
using System.Runtime.InteropServices;

namespace Com.Windows
{
    using DWORD = UInt32;

    public static class Kernel32
    {
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern DWORD GetCurrentThreadId();
    }
}