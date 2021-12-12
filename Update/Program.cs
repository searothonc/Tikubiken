#define DEBUG_LOG_TO_FILE

using System;
using System.Windows.Forms;

using Tikubiken.Properties;

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
			AbortableMain();
		}

		//--------------------------------------------------------
		// Abortable main operation
		//--------------------------------------------------------
		private static void AbortableMain()
		{
			// Conditionally, set debug log being outputted into the file
			Ext.SetDebugLogToFile();

			// Extract data from executable file
			Unpacked unpacked;
			try
			{
				unpacked = Unpacked.GetInstance();
			}
			catch ( URTException urte )
			{
				urte.MsgBox(Resources.error_msgbox);
				return;
			}

			// Run IDE generated Windows form operation
			try
			{
				RunForm();
			}
			catch ( Exception e )
			{
				unpacked.ErrorReport(e.ToString(), "Outmost:catch all");
#if	DEBUG
				throw;
#endif
			}
		}

		//--------------------------------------------------------
		// Windows form
		//--------------------------------------------------------
		static void RunForm()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormPatch());
		}
	}
}
