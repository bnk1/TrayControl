using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TrayControl
{
    public enum TrayArea
    {
        NotificationArea,
        OverflowArea
    }

    public class TrayIconInfo
    {
        public TrayArea Area { get; set; }
        public int IdCommand { get; set; }
        public required string Text { get; set; }
    }

    public static class TrayInterop
    {
        // === Win32 constants/messages ===
        private const int TB_BUTTONCOUNT = 0x0418;
        private const int TB_GETBUTTON = 0x0417;
        private const int TB_GETBUTTONTEXTW = 0x044B;
        private const int TB_HIDEBUTTON = 0x0404;

        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint MEM_RELEASE = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        private struct TBBUTTON
        {
            public int iBitmap;
            public int idCommand;
            public byte fsState;
            public byte fsStyle;
            public byte bReserved0;
            public byte bReserved1;
            public IntPtr dwData;
            public IntPtr iString;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProc, IntPtr baseAddr, IntPtr buffer, IntPtr size, out IntPtr bytesRead);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProc, IntPtr addr, IntPtr size, uint allocType, uint protect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFreeEx(IntPtr hProc, IntPtr addr, IntPtr size, uint freeType);

        // === Public API ===

        public static List<TrayIconInfo> ListTrayIcons()
        {
            var list = new List<TrayIconInfo>();

            IntPtr live = GetLiveTrayToolbar();
            IntPtr overflow = GetOverflowToolbar();

            if (live != IntPtr.Zero)
                ForEachButton(live, TrayArea.NotificationArea, list);

            if (overflow != IntPtr.Zero)
                ForEachButton(overflow, TrayArea.OverflowArea, list);

            return list;
        }

        public static bool HideIcon(int idCommand, TrayArea area)
        {
            return SetHidden(GetToolbar(area), idCommand, true);
        }

        public static bool ShowIcon(int idCommand, TrayArea area)
        {
            return SetHidden(GetToolbar(area), idCommand, false);
        }

        // === Private helpers ===

        private static IntPtr GetToolbar(TrayArea area)
        {
            return area == TrayArea.NotificationArea ? GetLiveTrayToolbar() : GetOverflowToolbar();
        }

        private static IntPtr GetLiveTrayToolbar()
        {
            IntPtr tray = FindWindow("Shell_TrayWnd", null);
            if (tray == IntPtr.Zero)
                return IntPtr.Zero;
            IntPtr notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
            if (notify == IntPtr.Zero)
                return IntPtr.Zero;
            IntPtr sysPager = FindWindowEx(notify, IntPtr.Zero, "SysPager", null);
            if (sysPager == IntPtr.Zero)
                return IntPtr.Zero;
            return FindWindowEx(sysPager, IntPtr.Zero, "ToolbarWindow32", null);
        }

        private static IntPtr GetOverflowToolbar()
        {
            IntPtr overflowWin = FindWindow("NotifyIconOverflowWindow", null);
            if (overflowWin == IntPtr.Zero)
                return IntPtr.Zero;
            return FindWindowEx(overflowWin, IntPtr.Zero, "ToolbarWindow32", null);
        }

        private static void ForEachButton(IntPtr toolbar, TrayArea area, List<TrayIconInfo> results)
        {
            if (toolbar == IntPtr.Zero)
                return;

            uint pid;
            GetWindowThreadProcessId(toolbar, out pid);
            IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_OPERATION, false, pid);
            if (hProc == IntPtr.Zero)
                return;

            int count = (int)SendMessage(toolbar, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            if (count <= 0)
                return;

            int btnSize = Marshal.SizeOf(typeof(TBBUTTON));
            IntPtr remoteBtn = VirtualAllocEx(hProc, IntPtr.Zero, (IntPtr)btnSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteBtn == IntPtr.Zero)
                return;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    SendMessage(toolbar, TB_GETBUTTON, (IntPtr)i, remoteBtn);

                    IntPtr local = Marshal.AllocHGlobal(btnSize);
                    try
                    {
                        IntPtr bytesRead;
                        ReadProcessMemory(hProc, remoteBtn, local, (IntPtr)btnSize, out bytesRead);
                        TBBUTTON btn = Marshal.PtrToStructure<TBBUTTON>(local);

                        string text = GetButtonTextCrossProc(toolbar, btn.idCommand, hProc);
                        results.Add(new TrayIconInfo
                        {
                            Area = area,
                            IdCommand = btn.idCommand,
                            Text = text ?? ""
                        });
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(local);
                    }
                }
            }
            finally
            {
                VirtualFreeEx(hProc, remoteBtn, IntPtr.Zero, MEM_RELEASE);
            }
        }

        private static bool SetHidden(IntPtr toolbar, int idCommand, bool hide)
        {
            if (toolbar == IntPtr.Zero)
                return false;
            IntPtr res = SendMessage(toolbar, TB_HIDEBUTTON, (IntPtr)idCommand, new IntPtr(hide ? 1 : 0));
            return res != IntPtr.Zero;
        }

        private static string GetButtonTextCrossProc(IntPtr toolbar, int idCommand, IntPtr hProc)
        {
            const int MaxChars = 512;
            int bytes = MaxChars * 2;

            IntPtr remoteBuf = VirtualAllocEx(hProc, IntPtr.Zero, (IntPtr)bytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteBuf == IntPtr.Zero)
                return string.Empty;

            IntPtr localBuf = Marshal.AllocHGlobal(bytes);
            try
            {
                IntPtr res = SendMessage(toolbar, TB_GETBUTTONTEXTW, (IntPtr)idCommand, remoteBuf);
                if (res.ToInt64() < 0)
                    return string.Empty;

                IntPtr read;
                if (!ReadProcessMemory(hProc, remoteBuf, localBuf, (IntPtr)bytes, out read))
                    return string.Empty;

                string s = Marshal.PtrToStringUni(localBuf) ?? string.Empty;
                int zero = s.IndexOf('\0');
                if (zero >= 0)
                    s = s.Substring(0, zero);
                return s;
            }
            finally
            {
                Marshal.FreeHGlobal(localBuf);
                VirtualFreeEx(hProc, remoteBuf, IntPtr.Zero, MEM_RELEASE);
            }
        }
    }
}
