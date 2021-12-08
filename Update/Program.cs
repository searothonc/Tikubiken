using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
//using System.Management;

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
			ExtractData();
			ShowForm();
		}

		//--------------------------------------------------------
		// Windows form
		//--------------------------------------------------------
		static void ShowForm()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormPatch());
		}

		//--------------------------------------------------------
		// Extract data from executable file
		//--------------------------------------------------------
		private static void ExtractData()
		{
			var proc = Process.GetCurrentProcess();
			MessageBox.Show(proc.MainModule.FileName, "TEST", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		//--------------------------------------------------------
		// Get parent process info
		//--------------------------------------------------------
/*
		private static Process GetParentProcess()
		{
			var procId = Process.GetCurrentProcess().Id;
			string query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId={procId}";
			int parentProcId;

			using ( var s = new ManagementObjectSearcher(@"root\CIMV2", query) )
			using ( var results = s.Get().GetEnumerator() )
			{

				if (!results.MoveNext()) throw new ApplicationException("Couldn't Get ParrentProcessId.");

				var queryResult = results.Current;
				parentProcId = (int) queryResult["ParentProcessId"];
			}

			return Process.GetProcessById( parentProcId );
		}
*/
	}
}
