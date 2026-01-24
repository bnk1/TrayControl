using System;
using System.Windows.Forms;

namespace CompactAppWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Ensure per-monitor DPI awareness for sharp text on high-DPI displays
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // .NET 6+ Windows Forms bootstrap
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
