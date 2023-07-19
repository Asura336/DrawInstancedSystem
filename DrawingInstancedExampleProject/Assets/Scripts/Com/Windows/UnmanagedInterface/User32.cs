using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Com.Windows
{
    using BOOL = Boolean;
    using DWORD = UInt32;
    using HMONITOR = IntPtr;
    using HWND = IntPtr;
    using LONG = Int32;

    public static class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public LONG x;
            public LONG y;

            public POINT(LONG x, LONG y)
            {
                this.x = x;
                this.y = y;
            }

            public void Deconstruct(out LONG x, out LONG y)
            {
                x = this.x;
                y = this.y;
            }

            public static implicit operator Vector2Int(in POINT p) => new Vector2Int(p.x, p.y);
            public static implicit operator Vector2(in POINT p) => new Vector2(p.x, p.y);
            public static implicit operator POINT(in Vector2Int p) => new POINT(p.x, p.y);
            public static implicit operator POINT(in Vector2 p) => new POINT(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

            public override string ToString() => $"({x}, {y})";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public LONG left;
            public LONG top;
            public LONG right;
            public LONG bottom;

            public override string ToString() => $"(left = {left}, right = {right}, top = {top}, bottom = {bottom})";

            public static implicit operator Rect(in RECT self) => new Rect
            {
                xMin = self.left,
                xMax = self.right,
                yMin = self.top,
                yMax = self.bottom
            };

            public static implicit operator RECT(in Rect rect) => new RECT
            {
                left = (int)rect.xMin,
                right = (int)rect.xMax,
                top = (int)rect.yMin,
                bottom = (int)rect.yMax
            };
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            /// <summary>
            /// 标记结构体尺寸
            /// </summary>
            public DWORD size;
            public RECT rcMonitor;
            public RECT rcWork;
            /// <summary>
            /// 主显示器时值为 1
            /// </summary>
            public DWORD dwFlags;
        }

        public enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0,
            MONITOR_DEFAULTTOPRIMARY = 1,
            MONITOR_DEFAULTTONEAREST = 2
        }


        public delegate bool EnumThreadWndProc(HWND Hwnd, HWND lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern BOOL EnumThreadWindows(DWORD dwThreadId, EnumThreadWndProc lpfn, HWND lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern HWND GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern BOOL GetWindowRect(HWND hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern BOOL GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern HMONITOR MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern BOOL SetCursorPos(LONG x, LONG y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern BOOL ScreenToClient(HWND hWnd, ref POINT lpPoint);
    }
}