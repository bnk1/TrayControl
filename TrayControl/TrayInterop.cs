using System;
using System.Collections.Generic;
using System.Drawing;
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
        public string Text { get; set; } = string.Empty; // never null
        public Image? Icon { get; set; }                 // may be null
    }

    public static class TrayInterop
    {
        // ==== Toolbar messages (WM_USER + n) ====
        private const int TB_HIDEBUTTON            = 0x0404; // +4
        private const int TB_GETBUTTON             = 0x0417; // +23
        private const int TB_BUTTONCOUNT           = 0x0418; // +24
        private const int TB_GETBUTTONTEXTW        = 0x044B; // +75
        private const int TB_GETIMAGELIST          = 0x0431; // +49  (correct)
        private const int TB_GETHOTIMAGELIST       = 0x0435; // +53  (fallback)
        private const int TB_GETDISABLEDIMAGELIST  = 0x0437; // +55  (fallback)
        private const int TB_GETRECT               = 0x0433; // +51  (RECT for idCommand; client coords)

        // ==== ImageList flags ====
        private const int ILD_NORMAL               = 0x0000;

        // ==== Process memory flags ====
        private const uint PROCESS_VM_READ           = 0x0010;
        private const uint PROCESS_VM_OPERATION      = 0x0008;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint MEM_COMMIT                = 0x1000;
        private const uint MEM_RESERVE               = 0x2000;
        private const uint PAGE_READWRITE            = 0x04;
        private const uint MEM_RELEASE               = 0x8000;

        // ==== PrintWindow flags ====
        private const uint PW_CLIENTONLY             = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct TBBUTTON
        {
            public int iBitmap;
            public int idCommand;
            public byte fsState;
            public byte fsStyle;
            public byte bReserved0; // padding x64
            public byte bReserved1; // padding x64
            public IntPtr dwData;   // explorer.exe pointer
            public IntPtr iString;  // explorer.exe pointer
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Width => right - left;
            public int Height => bottom - top;
        }

        // ===== P/Invokes =====
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? className, string? windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProc, IntPtr baseAddr, IntPtr buffer, IntPtr size, out IntPtr bytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProc, IntPtr addr, IntPtr size, uint allocType, uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProc, IntPtr addr, IntPtr size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("comctl32.dll", SetLastError = false)]
        private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        // ===== Public API =====

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
            => SetHidden(GetToolbar(area), idCommand, hide: true);

        public static bool ShowIcon(int idCommand, TrayArea area)
            => SetHidden(GetToolbar(area), idCommand, hide: false);

        // ===== Private helpers =====

        private static IntPtr GetToolbar(TrayArea area)
            => area == TrayArea.NotificationArea ? GetLiveTrayToolbar() : GetOverflowToolbar();

        private static IntPtr GetLiveTrayToolbar()
        {
            // Shell_TrayWnd → TrayNotifyWnd → SysPager → ToolbarWindow32
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
            // NotifyIconOverflowWindow → ToolbarWindow32
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

            try
            {
                int count = (int)SendMessage(toolbar, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
                if (count <= 0)
                    return;

                int btnSize = Marshal.SizeOf(typeof(TBBUTTON));
                IntPtr remoteBtn = VirtualAllocEx(hProc, IntPtr.Zero, (IntPtr)btnSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remoteBtn == IntPtr.Zero)
                    return;

                IntPtr himl = GetToolbarImageList(toolbar);

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Request TBBUTTON[i] be written into explorer’s memory we provided
                        SendMessage(toolbar, TB_GETBUTTON, (IntPtr)i, remoteBtn);

                        // Copy structure back into our process
                        IntPtr local = Marshal.AllocHGlobal(btnSize);
                        try
                        {
                            IntPtr bytesRead;
                            ReadProcessMemory(hProc, remoteBtn, local, (IntPtr)btnSize, out bytesRead);

                            TBBUTTON btn = Marshal.PtrToStructure<TBBUTTON>(local);

                            string text = GetButtonTextCrossProc(toolbar, btn.idCommand, hProc);

                            // Try imagelist first
                            Image? icon = GetButtonIcon(himl, btn.iBitmap);

                            // Fallback: capture the button rect from a PrintWindow of the toolbar
                            if (icon == null)
                            {
                                icon = CaptureButtonIcon(toolbar, hProc, btn.idCommand);
                            }

                            results.Add(new TrayIconInfo
                            {
                                Area = area,
                                IdCommand = btn.idCommand,
                                Text = text ?? string.Empty,
                                Icon = icon
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
            finally
            {
                CloseHandle(hProc);
            }
        }

        private static IntPtr GetToolbarImageList(IntPtr toolbar)
        {
            IntPtr himl = SendMessage(toolbar, TB_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero);
            if (himl != IntPtr.Zero)
                return himl;

            himl = SendMessage(toolbar, TB_GETHOTIMAGELIST, IntPtr.Zero, IntPtr.Zero);
            if (himl != IntPtr.Zero)
                return himl;

            return SendMessage(toolbar, TB_GETDISABLEDIMAGELIST, IntPtr.Zero, IntPtr.Zero);
        }

        private static Image? GetButtonIcon(IntPtr himl, int index)
        {
            if (himl == IntPtr.Zero || index < 0)
                return null;

            IntPtr hIcon = ImageList_GetIcon(himl, index, ILD_NORMAL);
            if (hIcon == IntPtr.Zero)
                return null;

            try
            {
                using (var ico = Icon.FromHandle(hIcon))
                    return (Image)ico.ToBitmap().Clone(); // clone so we can destroy handle
            }
            finally
            {
                DestroyIcon(hIcon);
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
            // Allocate text buffer in explorer.exe, ask TB_GETBUTTONTEXTW to fill it, then copy back
            const int MaxChars = 512;
            int bytes = MaxChars * 2; // UTF-16

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
                return TrimAtNull(s);
            }
            finally
            {
                Marshal.FreeHGlobal(localBuf);
                VirtualFreeEx(hProc, remoteBuf, IntPtr.Zero, MEM_RELEASE);
            }
        }

        // ---- Fallback: capture icon by printing toolbar and cropping button rect ----

        private static Image? CaptureButtonIcon(IntPtr toolbar, IntPtr hProc, int idCommand)
        {
            // 1) Get button rect in client coordinates (cross-process TB_GETRECT)
            int rectSize = Marshal.SizeOf(typeof(RECT));
            IntPtr remoteRect = VirtualAllocEx(hProc, IntPtr.Zero, (IntPtr)rectSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteRect == IntPtr.Zero)
                return null;

            RECT rect;
            IntPtr localRect = Marshal.AllocHGlobal(rectSize);
            try
            {
                SendMessage(toolbar, TB_GETRECT, (IntPtr)idCommand, remoteRect);

                IntPtr read;
                if (!ReadProcessMemory(hProc, remoteRect, localRect, (IntPtr)rectSize, out read))
                    return null;

                rect = Marshal.PtrToStructure<RECT>(localRect);
            }
            finally
            {
                Marshal.FreeHGlobal(localRect);
                VirtualFreeEx(hProc, remoteRect, IntPtr.Zero, MEM_RELEASE);
            }

            if (rect.Width <= 0 || rect.Height <= 0)
                return null;

            // 2) Print the toolbar's client area to a bitmap
            if (!GetClientRect(toolbar, out RECT client))
                return null;

            int w = Math.Max(client.Width, rect.right);   // be generous
            int h = Math.Max(client.Height, rect.bottom);

            if (w <= 0 || h <= 0)
                return null;

            using (var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        // PW_CLIENTONLY prints the client area (no border)
                        if (!PrintWindow(toolbar, hdc, PW_CLIENTONLY))
                            return null;
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
                }

                // 3) Crop to button rect, clamp inside bitmap
                int x = Math.Max(0, rect.left);
                int y = Math.Max(0, rect.top);
                int rw = Math.Min(rect.Width,  bmp.Width  - x);
                int rh = Math.Min(rect.Height, bmp.Height - y);
                if (rw <= 0 || rh <= 0)
                    return null;

                Rectangle crop = new Rectangle(x, y, rw, rh);
                using (var cropped = bmp.Clone(crop, bmp.PixelFormat))
                {
                    // 4) Resize to 16x16
                    var outBmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var gg = Graphics.FromImage(outBmp))
                    {
                        gg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        gg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gg.DrawImage(cropped, new Rectangle(0, 0, 16, 16));
                    }
                    return outBmp;
                }
            }
        }

        private static string TrimAtNull(string s)
        {
            int i = s.IndexOf('\0');
            return i >= 0 ? s.Substring(0, i) : s;
        }
    }
}
