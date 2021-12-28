//#define DEBUG_LOG_TO_FILE

using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace Tikubiken
{
	static class Program
	{
		// https://kuttsun.blogspot.com/2018/03/wpf.html
		[System.Runtime.InteropServices.DllImport("Kernel32.dll")]
		static extern bool FreeConsole();

		[System.Runtime.InteropServices.DllImport("Kernel32.dll")]
		static extern bool AttachConsole( int processHandle );

		private const int ATTACH_PARENT_PROCESS = -1;

		//--------------------------------------------------------
		// Globals
		//--------------------------------------------------------
		public static MyApp		App			{	get;	set;	}

		//--------------------------------------------------------
		// Entrypoint
		//--------------------------------------------------------
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Conditionally, set debug log being outputted into the file
			Ext.SetDebugLogToFile();

			try
			{
				// Application object
				Program.App = new Tikubiken.MyApp();

				if ( Program.App.IsCUIMode )
				{
					AttachConsole(ATTACH_PARENT_PROCESS);
					RunCUI();
					FreeConsole();
				}
				else
				{
					// Run IDE generated Windows form operation
					RunForm();
				}
			}
			catch ( Exception e )
			{
				Console.Error.WriteLine( e.ToString() );
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
			Application.Run(new FormDiff());
		}

		//--------------------------------------------------------
		// Asynchronous process body common for both CUI/GUI
		//--------------------------------------------------------

		static void RunCUI()
		{
			Console.Out.WriteLine();
			try
			{
				using ( var processor = new Processor(OperateProgress) )
				{
					processor.Ready(
								Program.App.SourceXMLName, 
								Program.App.OutputName, 
								Program.App.DeltaFmt
							);

					// Start and await asyncronous processing
					processor.RunAsync(Program.App.OptSaveXML).Wait();
					Console.Out.WriteLine( "---Complete---" );
				}
			}
			catch (OperationCanceledException)
			{
				// The user intended to cancel processing.
				// So the exception is simply discarded.
				Console.Out.WriteLine( "---Cancelled---" );
			}
			catch (Error pe)
			{
				// Show reason why the processor has aborted.
				Console.Error.WriteLine( pe.ToString() );
				Console.Out.WriteLine( "---Aborted---" );
			}
			catch ( FileNotFoundException fnfe )
			{
				// File does not exist
				Console.Error.WriteLine( fnfe.Message );
				Console.Out.WriteLine( "---Aborted---" );
			}
			catch (Exception exc)
			{
				// Any other exception caught shows the full information of trace.
				Console.Error.WriteLine( exc.ToString() );
				Console.Out.WriteLine( "---Aborted---" );
			}
			finally
			{
				Console.Out.Flush();
				Console.Error.Flush();
			}
		}

		//------------------------------------------------------------
		// Show Progress
		//------------------------------------------------------------

		// Reflecting progress in the UIs
		private static void OperateProgress(ProgressState state)
		{
			// No text messages in quiet mode
			if ( Program.App.IsQuiet ) return;

			if ( state.IsTextAvailable() )
			{
				Console.Out.WriteLine(state.Text);
			}

		}

	}	// ------ static class Program
}
