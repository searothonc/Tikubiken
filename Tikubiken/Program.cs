#define DEBUG_LOG_TO_FILE

using System;
using System.Windows.Forms;

namespace Tikubiken
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			// Conditionally, set debug log being outputted into the file
			Ext.SetDebugLogToFile();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormDiff());
        }
    }
}
