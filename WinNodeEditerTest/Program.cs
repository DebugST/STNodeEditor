using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ST.Library.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            new Demo_Image.FrmImage().Show();
            Application.Run(new Form1());
        }
    }
}
