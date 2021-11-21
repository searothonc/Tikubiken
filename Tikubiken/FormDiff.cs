#define DEBUG_LOG_TO_FILE

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Tikubiken.Properties;

namespace Tikubiken
{
	public partial class FormDiff : Form
	{
		//------------------------------------------------------------
		// Fields
		//------------------------------------------------------------
		private Tikubiken.MyApp myApp;
		private CancellationTokenSource ctSource;
		private ProgressState pState;

		//------------------------------------------------------------
		// Propeerties
		//------------------------------------------------------------

		//------------------------------------------------------------
		// Initalyzation & Cleaning up
		//------------------------------------------------------------
		// Constructor
		public FormDiff()
		{
			InitializeComponent();
		}

		//------------------------------------------------------------
		// Form events
		//------------------------------------------------------------

		// Form initialization on load
		private void FormDiff_Load(object sender, EventArgs e)
		{
			// Conditionally, set debug log being outputted into the file
			SetDebugLogToFile();

			// Application Manager
			myApp = new Tikubiken.MyApp();

			// Initialyze UIs
			ClearLogText();
		}

		// Window is about to close
		private void FormDiff_FormClosing(object sender, FormClosingEventArgs e)
		{
		}

		// Closing window
		private void FormDiff_FormClosed(object sender, FormClosedEventArgs e)
		{
			// Save changes to ini file
			myApp.IniSave();
		}

		//------------------------------------------------------------
		// Debug
		//------------------------------------------------------------

		// Conditional initialyzation
		[Conditional("DEBUG_LOG_TO_FILE")]
		private void SetDebugLogToFile()
		{
			string log_file = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\debug.txt";
			StreamWriter sw = new StreamWriter(log_file);
			sw.AutoFlush = true;
			TextWriter tw = TextWriter.Synchronized(sw);
			TextWriterTraceListener twtl = new TextWriterTraceListener(tw, "LogFile");
			Trace.Listeners.Add(twtl);
			string nowtime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			Debug.WriteLine($"[Started: {nowtime}]");
		}

		//------------------------------------------------------------
		// [Source XML] text box and [File] button
		//------------------------------------------------------------

		// [File] button for Source XML
		private void buttonSource_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog()) {
				// dlg.Title = "Open File";
				dlg.InitialDirectory = myApp.lastDir;
				dlg.Filter = Resources.ofd_src_filter;
				dlg.DefaultExt = "*.xml";

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxSource.Text = dlg.FileName;
					myApp.lastDir = System.IO.Path.GetDirectoryName(dlg.FileName);
				} else {
					// dialog closed by [Cancel] button
				}
			}
		}

		// [Source XML] text box
		private void textBoxSource_TextChanged(object sender, EventArgs e)
			=> Init_buttonStart();

		// Set enability for [Source XML]
		private void SetEnable_UIs_source(bool isEnabled)
			=> buttonSource.Enabled = textBoxSource.Enabled = isEnabled;

		//------------------------------------------------------------
		// [Output] text box and [File] button
		//------------------------------------------------------------

		// [File] button for Output
		private void buttonOutput_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog dlg = new SaveFileDialog()) {
				// dlg.Title = "Open File";
				dlg.InitialDirectory = myApp.lastOut;
				dlg.Filter = Resources.ofd_out_filter;
				dlg.DefaultExt = "*.tbp";

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxOutput.Text = dlg.FileName;
					myApp.lastOut = System.IO.Path.GetDirectoryName(dlg.FileName);
				} else {
					// dialog closed by [Cancel] button
				}
			}
		}

		// [Output] text box
		private void textBoxOutput_TextChanged(object sender, EventArgs e)
			=> Init_buttonStart();

		// Set enability for [Output]
		private void SetEnable_UIs_output(bool isEnabled)
			=> buttonOutput.Enabled = textBoxOutput.Enabled = isEnabled;

		// Set enability for [Source XML] & [Output]
		private void SetEnable_UIs_filepath(bool isEnabled)
		{
			SetEnable_UIs_source(isEnabled);
			SetEnable_UIs_output(isEnabled);
		}

		//------------------------------------------------------------
		// Log text box
		//------------------------------------------------------------

		// Add text to log box 
		private void AddLogText(string text)
		{
			if ( textBoxLog.Text.Length > 0 ) textBoxLog.Text += Environment.NewLine;
			textBoxLog.Text += text;
			textBoxLog.Select(textBoxLog.Text.Length, 0);
			textBoxLog.ScrollToCaret();
		}

		// Clear box 
		private void ClearLogText() => textBoxLog.Text = "";

		// Delegate to call AddLogText(string)
		private delegate void VoidString(string s);

		//------------------------------------------------------------
		// [Start] button
		//------------------------------------------------------------

		// Check if the [Start] button is avaiable
		private bool CanStart()
		{
			if ( textBoxSource.Text.Length <= 0 ) return false;
			if ( textBoxOutput.Text.Length <= 0 ) return false;
			return System.IO.File.Exists(textBoxSource.Text);
		}

		// Set the [Start] button availability
		private void Init_buttonStart()
			=> buttonStart.Enabled = CanStart();

		// [Start] button
/*
		private void buttonStart_Click(object sender, EventArgs e)
		{
			AddLogText( "[Start]\n" );
			Debug.WriteLine( "[Start] button" );
		}
*/
		private async void buttonStart_Click(object sender, EventArgs e)
		{
			if ( ctSource == null )
			{
Debug.WriteLine( "[Start] button" );
				AddLogText( "[Start]" );

				// Setup progress state
				var progress = new Progress<ProgressState>(OperateProgress);
				pState ??= new ProgressState();
				pState.Max = 200;
				progressBar.Maximum = pState.Max;

				SetupCancelButton();		// 必ずawait直前
				using ( ctSource = new CancellationTokenSource() )
				{
					try
					{
						//await TestAsync();
						await Task.Run( ()=>TestTask(progress) );
					}
					catch {}
					finally
					{
						AddLogText( "---Complete---" );
						ResetUIElements();
					}
				}
			}
			else
			{
Debug.WriteLine( "[Cancel] button" );
				AddLogText( "[Cancel]" );
				CancelOperation();
			}
		}

		// Reset UI elements
		private void SetupCancelButton()
		{
			SetEnable_UIs_filepath(false);
			buttonStart.Text = Resources.btntext_Cancel;
			buttonStart.Enabled = true;
		}

		// Reset UI elements
		private void ResetUIElements()
		{
			SetEnable_UIs_filepath(true);
			buttonStart.Text = Resources.btntext_Start;
			Init_buttonStart();
		}

		private void OperateProgress(ProgressState state)
		{
			if ( state.IsValueAvailable() )
			{
				progressBar.Value = state.Value;
			}

			if ( state.IsTextAvailable() )
			{
				AddLogText(state.Text);
			}

		}

		//------------------------------------------------------------
		// Async task
		//------------------------------------------------------------

//		private async Task TestAsync()
//		{
//			return Task.Run( ()=>TestTask() );
//		}
		private void TestTask( IProgress<ProgressState> progress )
		{
			try
			{
				// count 20 seconds
				for ( int i=0 ; i<20 /* 1 minutes */ ; ++i )
				{
					// Count one second
					for ( int j=0 ; j<10 ; ++j )
					{
						Thread.Sleep(100);
						// check cancellation every 0.1 seconds
						ctSource.Token.ThrowIfCancellationRequested();
						pState.Value += 1;
						progress.Report(pState);
					}
					// log out every second
//					Invoke(new VoidString(AddLogText), $"Count {i}");
					pState.Text = $"Count {i}";
					progress.Report(pState);
Debug.WriteLine( $"Async {i}" );
				}
				pState.Value = pState.Max;
				progress.Report(pState);
			}
			catch ( OperationCanceledException )
			{
				// do what to do when operation has been cancelled.
			}
			catch
			{
				//Re-throw if the exception other than cancellation occured
				throw;
			}
			finally
			{
				// do what to do whether or not an exception is thrown
				// enter here when the exception thrown out
Debug.WriteLine( "Operation finally" );
				ctSource.Dispose();
				ctSource = null;
			}
		}

		// Operation to cancel async task
		private void CancelOperation()
		{
			// Cancel only when cancellation token exists.
			if ( ctSource == null ) return;
			ctSource.Cancel();
		}

/*
		// Async method
		private async Task StartTask()
		{
			// CancellationTokenSource is dispose pattern.
			try
			{
				using ( ctSource = new CancellationTokenSource() )
				{
					AddLogText( "ctSource created\n" );

					// Register delegate which will be called 
					// when processing is cancelled.
					CancellationToken token = ctSource.Token;
					token.Register( ProcessCancelled );
					// Set cancellation token to throw exception when cancelled
					//token.Register( token.ThrowIfCancellationRequested );

					// Run async task
					await Task.Run( ProcessFile, token );
				}
			}
			finally
			{
				AddLogText( "finally\n" );
				ResetUIElements();
				ctSource = null;
			}
		}

		// Process source XML file
		private void ProcessFile()
		{
			//Task.Delay( 10 * 60*1000 );		//10 minutes
//			Thread.Sleep( 10 * 60*1000 );		//10 minutes
			Thread.Sleep( 10*1000 );		//10 minutes
		}

		// Callback called when process cancelled
		private void ProcessCancelled()
		{
			Debug.WriteLine( "CancelProcessing()" );

			// Reset UIs.
			ResetUIElements();

			// Clean up cancellation token
			ctSource.Dispose();
			ctSource = null;
		}
*/
	}

	/// <summary>
	/// Value container used in async message of Progress<T>
	/// adaptable for both text logging and Progress control
	/// </summary>
	class ProgressState
	{
		public enum Usage
		{
			None		=	0,
			Value		=	0x01,
			Text		=	0x02,
			Both		=	Value|Text
		}

		//------------------------------------------------------------
		// Fields
		//------------------------------------------------------------
		private int		_value;
		private string	_text;

		//------------------------------------------------------------
		// Propeerties
		//------------------------------------------------------------
		public int		Min			{ get; set; }
		public int		Max			{ get; set; }
		public int		Value
		{ 
			get { return _value; }
			set
			{
				if ( value<Min ) value = Min;
				if ( Max<value ) value = Max;
				_value = value;
				Available = Usage.Value;
			}
		}
		public string	Text
		{
			get { return _text; }
			set
			{
				_text = value;
				Available = Usage.Text;
			}
		}
		public Usage	Available	{ get; set; }

		//------------------------------------------------------------
		// Initalyzation & Cleaning up
		//------------------------------------------------------------

		/// <summary>Constructor</summary>
		public ProgressState()
		{
			Min = 0;
			Max = 100;
			Value =0;
			Text = "";
			Available = Usage.None;
		}

		//------------------------------------------------------------
		// Value availability
		//------------------------------------------------------------

		/// <summary>
		/// Check if the value is available.
		/// </summary>
		public bool IsValueAvailable() => (Available & Usage.Value) != 0;

		/// <summary>
		/// Check if the text is available.
		/// </summary>
		public bool IsTextAvailable() => (Available & Usage.Text) != 0;

		//------------------------------------------------------------
		// Conversions
		//------------------------------------------------------------

		/// <summary>
		/// Convert value to rate 0.0-1.0
		/// </summary>
		public float Rate()		=> (float)Value / ((float)Max - (float)Min);

		/// <summary>
		/// Convert value to percentage 0%-100%
		/// </summary>
		public int Percent()	=> (int)Math.Round(Rate());
	}
}
/*
CancellationToken は、 CancellationTokenSource.Cancel()が呼び出されると内容が変わるステートマシンに過ぎず、呼び出された側のスレッドは、キャンセルを検知したら自分で処理を終了させないといけない、
ProcessorをIDisposeモデルにして、内部で確保する方がいい
*/
/*
・[x]押下時にクリーンナップするのではなく、[x]を押せなくする案
◎クリーンナップは常に同じ処理をする案(テンポラリディレクトリを丸ごと消す)
*/
