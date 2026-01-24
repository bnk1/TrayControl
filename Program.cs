using System;
using System.Windows.Forms;

namespace CompactAppWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // .NET 6+ Windows Forms bootstrap
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
