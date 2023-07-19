using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Com.Windows
{
    using static User32;
    using HWND = IntPtr;

    public static class CursorUtil
    {
        /// <summary>
        /// 传递指定窗体范围和光标位置，如果当前光标靠近边缘则坐标按窗体范围取余，传出光标预期的位置和假想的光标原点偏移量，不会改变当前光标位置。
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="windowRect"></param>
        /// <param name="outCoord"></param>
        /// <param name="originDeltaX"></param>
        /// <param name="originDeltaY"></param>
        public static void CalculateRoundCursor(in POINT currentCoord, in RECT windowRect, out POINT outCoord, out int originDeltaX, out int originDeltaY)
        {
            const int edgeWidth = 12;
            const int twiceEdge = edgeWidth * 2 + 1;

            originDeltaX = originDeltaY = 0;

            outCoord = currentCoord;
            if (currentCoord.x > windowRect.right - edgeWidth)
            {
                originDeltaX = -(windowRect.right - windowRect.left - twiceEdge);
                outCoord.x = windowRect.left + edgeWidth + 1;
            }
            else if (currentCoord.x < windowRect.left + edgeWidth)
            {
                originDeltaX = windowRect.right - windowRect.left - twiceEdge;
                outCoord.x = windowRect.right - edgeWidth - 1;
            }
            if (currentCoord.y > windowRect.bottom - edgeWidth)
            {
                originDeltaY = -(windowRect.bottom - windowRect.top - twiceEdge);
                outCoord.y = windowRect.top + edgeWidth + 1;
            }
            else if (currentCoord.y < windowRect.top + edgeWidth)
            {
                originDeltaY = windowRect.bottom - windowRect.top - twiceEdge;
                outCoord.y = windowRect.bottom - edgeWidth - 1;
            }
        }

        /// <summary>
        /// 传递指定窗体范围，如果当前光标超出范围则坐标按窗体范围取余，传出重新定位的光标位置和假想的光标原点偏移量。
        /// </summary>
        /// <param name="windowRect"></param>
        /// <param name="currentCoord"></param>
        /// <param name="originDeltaX"></param>
        /// <param name="originDeltaY"></param>
        public static void RoundCursor(in RECT windowRect, out POINT currentCoord, out int originDeltaX, out int originDeltaY)
        {
            GetCursorPos(out var p);
            CalculateRoundCursor(p, windowRect, out currentCoord, out originDeltaX, out originDeltaY);
            SetCursorPos(currentCoord.x, currentCoord.y);
        }

        /// <summary>
        /// 如果当前光标到达所在显示器边缘则坐标按光标所在显示器范围取余，传出重新定位的光标位置和假想的光标原点偏移量。
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="originDeltaX"></param>
        /// <param name="originDeltaY"></param>
        public static void RoundCursor(out POINT currentCoord, out int originDeltaX, out int originDeltaY)
        {
            GetCursorPos(out var p);
            GetMonitorRectByCursorCoord(p, out var windowRect);
            CalculateRoundCursor(p, windowRect, out currentCoord, out originDeltaX, out originDeltaY);
            SetCursorPos(currentCoord.x, currentCoord.y);
        }

        /// <summary>
        /// 从指定光标位置计算光标所在的显示器范围，或者输出主显示器范围。
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="monitorRect"></param>
        /// <returns></returns>
        public static bool GetMonitorRectByCursorCoord(in POINT currentCoord, out RECT monitorRect)
        {
            MONITORINFO monitorInfo = default;
            monitorInfo.size = (uint)Marshal.SizeOf<MONITORINFO>();
            var monitor = MonitorFromPoint(currentCoord, MonitorOptions.MONITOR_DEFAULTTONULL);
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                monitorRect = monitorInfo.rcMonitor;
                return true;
            }
            else
            {
                GetWindowRect(GetDesktopWindow(), out monitorRect);
                return false;
            }
        }

        public static RECT GetSelfWindowRect()
        {
            var window = MainWindowHandle == IntPtr.Zero ? GetDesktopWindow() : MainWindowHandle;
            GetWindowRect(window, out RECT _rect);
            return _rect;
        }


        static HWND MainWindowHandle { get; set; } = IntPtr.Zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void FindSelfWindowInitial()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer) { return; }

            //static bool _call(HWND hWnd, IntPtr lParam)
            //{
            //    MainWindowHandle = hWnd;
            //    return false;
            //}

            //var threadID = GetCurrentThreadId();
            //if (threadID != 0)
            //{
            //    EnumThreadWindows(threadID, _call, IntPtr.Zero);
            //}
            MainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        }
    }
}