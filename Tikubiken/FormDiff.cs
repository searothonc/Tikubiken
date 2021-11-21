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
		private Processor m_processor;

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
			if ( m_processor != null )
			{
				try
				{
					m_processor.Cancel();
				}
				catch {}
				finally
				{
					m_processor.Dispose();
					m_processor = null;
				}
			}
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
		private async void buttonStart_Click(object sender, EventArgs e)
		{
			if ( m_processor == null )
			{
Debug.WriteLine( "[Start] button" );
				AddLogText( "[Start]" );

				try
				{
					using ( m_processor = new Processor(OperateProgress) )
					{
						progressBar.Maximum = m_processor.CurrentProgress.Max = 200;
							//await TestAsync();
							SetupCancelButton();		// 必ずawait直前
							await m_processor.RunAsync();
					}
				}
				//catch (OperationCanceledException) {}
				catch { /* throw; */ }
				finally
				{
					m_processor = null;
					AddLogText( "---Complete---" );
					ResetUIElements();
				}
			}
			else
			{
Debug.WriteLine( "[Cancel] button" );
				AddLogText( "[Cancel]" );
				CancelOperation();
			}
		}

		// Operation to cancel async task
		private void CancelOperation()
		{
			// Cancel only when cancellation token exists.
			if ( m_processor == null ) return;
			m_processor.Cancel();
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
			textBoxSource.Text = "";
			progressBar.Value = 0;
		}

		private void OperateProgress(Processor.ProgressState state)
		{
Debug.WriteLine( $"Usage = {state.Usage}" );

			if ( state.IsValueAvailable() )
			{
				progressBar.Value = state.Value;
			}

			if ( state.IsTextAvailable() )
			{
				AddLogText(state.Text);
			}

		}
	}
}
/*
・[x]押下時にクリーンナップするのではなく、[x]を押せなくする案
◎クリーンナップは常に同じ処理をする案(テンポラリディレクトリを丸ごと消す)
*/
