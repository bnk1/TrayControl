using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TrayControl
{
    public enum TrayArea { NotificationArea, OverflowArea }

    public class TrayIconInfo
    {
        public TrayArea Area { get; set; }
        public int IdCommand { get; set; }
        public string Text { get; set; } = string.Empty;
        public Image Icon { get; set; }  // may be null at runtime, keep guarded in your UI
    }

    public static class TrayInterop
    {
        // ===== Toolbar messages (WM_USER + n) =====
        private const int TB_HIDEBUTTON            = 0x0404;
        private const int TB_GETBUTTON             = 0x0417;
        private const int TB_BUTTONCOUNT           = 0x0418;
        private const int TB_GETBUTTONTEXTW        = 0x044B;
        private const int TB_GETIMAGELIST          = 0x0431;
        private const int TB_GETHOTIMAGELIST       = 0x0435;
        private const int TB_GETDISABLEDIMAGELIST  = 0x0437;
        private const int TB_GETRECT               = 0x0433;

        // Button style/state bits
        private const byte TBSTYLE_SEP   = 0x01;
        private const byte TBSTATE_HIDDEN = 0x08;

        // ImageList flags
        private const int ILD_NORMAL = 0x0000;

        // Process memory flags
        private const uint PROCESS_VM_READ           = 0x0010;
        private const uint PROCESS_VM_OPERATION      = 0x0008;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint MEM_COMMIT   = 0x1000;
        private const uint MEM_RESERVE  = 0x2000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint MEM_RELEASE  = 0x8000;

        // ===== Structs =====
        [StructLayout(LayoutKind.Sequential)]
        private struct TBBUTTON
        {
            public int iBitmap;
            public int idCommand;
            public byte fsState;
            public byte fsStyle;
            public byte bReserved0; // x64 padding
            public byte bReserved1; // x64 padding
            public IntPtr dwData;   // explorer pointer (unused here)
            public IntPtr iString;  // explorer pointer to text or index
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
            public int Width { get { return right - left; } }
            public int Height { get { return bottom - top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType, bmWidth, bmHeight, bmWidthBytes;
            public ushort bmPlanes, bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        // ===== P/Invoke =====
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string windowName);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("comctl32.dll", SetLastError = false)]
        private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int GetObject(IntPtr h, int c, out BITMAP pv);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        // ===== Public API =====

        /// <summary>
        /// Enumerate tray icons from Notification and Overflow areas.
        /// </summary>
        public static List<TrayIconInfo> ListTrayIcons(int iconWidth = 16, int iconHeight = 16)
        {
            var list = new List<TrayIconInfo>();

            IntPtr live = GetLiveTrayToolbar();
            IntPtr overflow = GetOverflowToolbar();

            if (live != IntPtr.Zero)
                ForEachButton(live, TrayArea.NotificationArea, list, iconWidth, iconHeight);

            if (overflow != IntPtr.Zero)
                ForEachButton(overflow, TrayArea.OverflowArea, list, iconWidth, iconHeight);

            return list;
        }

        public static bool HideIcon(int idCommand, TrayArea area)
            => SetHidden(GetToolbar(area), idCommand, true);

        public static bool ShowIcon(int idCommand, TrayArea area)
            => SetHidden(GetToolbar(area), idCommand, false);

        // ===== Internals =====

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

        private static void ForEachButton(IntPtr toolbar, TrayArea area, List<TrayIconInfo> results, int iconW, int iconH)
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
                        // Ask explorer to write TBBUTTON[i] into its own memory we provided
                        SendMessage(toolbar, TB_GETBUTTON, (IntPtr)i, remoteBtn);

                        // Copy back into our process
                        IntPtr local = Marshal.AllocHGlobal(btnSize);
                        try
                        {
                            IntPtr bytesRead;
                            ReadProcessMemory(hProc, remoteBtn, local, (IntPtr)btnSize, out bytesRead);

                            TBBUTTON btn = Marshal.PtrToStructure<TBBUTTON>(local);

                            // skip separators and hidden
                            if ((btn.fsStyle & TBSTYLE_SEP) == TBSTYLE_SEP)
                                continue;
                            if ((btn.fsState & TBSTATE_HIDDEN) == TBSTATE_HIDDEN)
                                continue;

                            string text = GetButtonTextCrossProc(toolbar, btn.idCommand, hProc);

                            // 1) PRIMARY: screen capture of the real button (by INDEX!)
                            Image icon = CaptureButtonIconFromScreen(toolbar, hProc, i, iconW, iconH, Color.White);

                            // 2) FALLBACK: imagelist icon (alpha-safe)
                            if (icon == null)
                                icon = GetButtonIcon(himl, btn.iBitmap, iconW, iconH);

                            results.Add(new TrayIconInfo
                            {
                                Area = area,
                                IdCommand = btn.idCommand,
                                Text = text,
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

        private static bool SetHidden(IntPtr toolbar, int idCommand, bool hide)
        {
            if (toolbar == IntPtr.Zero)
                return false;
            IntPtr res = SendMessage(toolbar, TB_HIDEBUTTON, (IntPtr)idCommand, new IntPtr(hide ? 1 : 0));
            return res != IntPtr.Zero;
        }

        // --------- Cross-process text (TB_GETBUTTONTEXTW) ----------
        private static string GetButtonTextCrossProc(IntPtr toolbar, int idCommand, IntPtr hProc)
        {
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

                string s = Marshal.PtrToStringUni(localBuf);
                if (s == null)
                    return string.Empty;
                int z = s.IndexOf('\0');
                return z >= 0 ? s.Substring(0, z) : s;
            }
            finally
            {
                Marshal.FreeHGlobal(localBuf);
                VirtualFreeEx(hProc, remoteBuf, IntPtr.Zero, MEM_RELEASE);
            }
        }

        // --------- Primary icon capture: screen pixels of the toolbar button ----------
        private static Image CaptureButtonIconFromScreen(IntPtr toolbar, IntPtr hProc, int buttonIndex, int canvasW, int canvasH, Color back)
        {
            // 1) Get button RECT from explorer (client coords) using INDEX
            int rectSize = Marshal.SizeOf(typeof(RECT));
            IntPtr remoteRect = VirtualAllocEx(hProc, IntPtr.Zero, (IntPtr)rectSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteRect == IntPtr.Zero)
                return null;

            RECT r;
            IntPtr localRect = Marshal.AllocHGlobal(rectSize);
            try
            {
                SendMessage(toolbar, TB_GETRECT, (IntPtr)buttonIndex, remoteRect);
                if (!ReadProcessMemory(hProc, remoteRect, localRect, (IntPtr)rectSize, out _))
                    return null;

                r = Marshal.PtrToStructure<RECT>(localRect);
            }
            finally
            {
                Marshal.FreeHGlobal(localRect);
                VirtualFreeEx(hProc, remoteRect, IntPtr.Zero, MEM_RELEASE);
            }

            if (r.Width <= 0 || r.Height <= 0)
                return null;

            // 2) Convert client -> screen
            POINT tl = new POINT { X = r.left, Y = r.top };
            POINT br = new POINT { X = r.right, Y = r.bottom };
            if (!ClientToScreen(toolbar, ref tl) || !ClientToScreen(toolbar, ref br))
                return null;

            int w = Math.Max(1, br.X - tl.X);
            int h = Math.Max(1, br.Y - tl.Y);

            // 3) Copy the exact pixels the user sees (DWM composited)
            using (var src = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(src))
                {
                    try
                    { g.CopyFromScreen(tl.X, tl.Y, 0, 0, new Size(w, h)); }
                    catch { return null; }
                }

                // 4) Fit into canvas WITHOUT upscaling; center; white background
                return FitIntoCanvasNoUpscale(src, canvasW, canvasH, back);
            }
        }

        // --------- Fallback: imagelist HICON (alpha-safe) ----------
        private static Image GetButtonIcon(IntPtr himl, int index, int canvasW, int canvasH)
        {
            if (himl == IntPtr.Zero || index < 0)
                return null;

            IntPtr hIcon = ImageList_GetIcon(himl, index, ILD_NORMAL);
            if (hIcon == IntPtr.Zero)
                return null;

            try
            {
                return IconToCanvas(hIcon, canvasW, canvasH);
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        private static Image IconToCanvas(IntPtr hIcon, int canvasW, int canvasH)
        {
            // Determine native HICON size (avoid upscaling tiny icons)
            Size nat = GetIconNativeSize(hIcon);
            float scale = Math.Min(1f, Math.Min((float)canvasW / nat.Width, (float)canvasH / nat.Height));
            int drawW = Math.Max(1, (int)Math.Round(nat.Width * scale));
            int drawH = Math.Max(1, (int)Math.Round(nat.Height * scale));
            int offX = (canvasW - drawW) / 2;
            int offY = (canvasH - drawH) / 2;

            using (Icon ico = Icon.FromHandle(hIcon))
            {
                Bitmap bmp = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = (scale < 1f)
                        ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                        : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawIcon(ico, new Rectangle(offX, offY, drawW, drawH));
                }
                return bmp;
            }
        }

        private static Size GetIconNativeSize(IntPtr hIcon)
        {
            ICONINFO ii;
            if (!GetIconInfo(hIcon, out ii))
                return new Size(16, 16);

            try
            {
                IntPtr hbmp = ii.hbmColor != IntPtr.Zero ? ii.hbmColor : ii.hbmMask;
                if (hbmp == IntPtr.Zero)
                    return new Size(16, 16);

                BITMAP bmp;
                if (GetObject(hbmp, Marshal.SizeOf(typeof(BITMAP)), out bmp) == 0)
                    return new Size(16, 16);

                int w = Math.Max(1, bmp.bmWidth);
                int h = Math.Max(1, Math.Abs(bmp.bmHeight));
                if (ii.hbmColor == IntPtr.Zero && (h % 2) == 0)
                    h /= 2; // mask is often double height
                return new Size(w, h);
            }
            finally
            {
                if (ii.hbmColor != IntPtr.Zero)
                    DeleteObject(ii.hbmColor);
                if (ii.hbmMask != IntPtr.Zero)
                    DeleteObject(ii.hbmMask);
            }
        }

        private static Bitmap FitIntoCanvasNoUpscale(Image src, int canvasW, int canvasH, Color back)
        {
            int srcW = Math.Max(1, src.Width);
            int srcH = Math.Max(1, src.Height);

            float scale = Math.Min(1f, Math.Min((float)canvasW / srcW, (float)canvasH / srcH));
            int drawW = Math.Max(1, (int)Math.Round(srcW * scale));
            int drawH = Math.Max(1, (int)Math.Round(srcH * scale));
            int offX = (canvasW - drawW) / 2;
            int offY = (canvasH - drawH) / 2;

            Bitmap outBmp = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(outBmp))
            {
                g.Clear(back); // e.g., Color.White to avoid dark backgrounds
                g.InterpolationMode = (scale < 1f)
                    ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(src, new Rectangle(offX, offY, drawW, drawH));
            }
            return outBmp;
        }
    }
}
