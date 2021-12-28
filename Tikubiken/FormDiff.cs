using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

using Tikubiken.Properties;

namespace Tikubiken
{
    public partial class FormDiff : Form
	{
		//------------------------------------------------------------
		// Fields
		//------------------------------------------------------------
		private Processor m_processor;

		private Dictionary<string,DeltaFormat> dicEncoding = 
			new Dictionary<string,DeltaFormat> {
				{	"VCDiff/RFC3284",			DeltaFormat.VCDiff			},
				{	"open-vcdiff/SDCH",			DeltaFormat.VCDiff_Google	},
				{	"VCDiff/XDelta3",			DeltaFormat.VCDiff_XDelta3	},
				{	"BSDiff+Brotli(Optimal)",	DeltaFormat.BsPlus_Optimal	},
				{	"BSDiff+Brotli(Fastest)",	DeltaFormat.BsPlus_Fastest	},
			};

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

			// Initialyze UIs
			InitDropdown();
			ClearLogText();
			checkBoxClearLog.Checked = Program.App.CheckClearLog;
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
			Program.App.CheckClearLog = checkBoxClearLog.Checked;
			Program.App.IniSave();
		}

		//------------------------------------------------------------
		// [Source XML] text box and [File] button
		//------------------------------------------------------------

		// [File] button for Source XML
		private void buttonSource_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog()) {
				// dlg.Title = "Open File";
				dlg.InitialDirectory = Program.App.LastDir;
				dlg.Filter = Resources.ofd_src_filter;
				dlg.DefaultExt = "*.xml";

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxSource.Text = dlg.FileName;
					Program.App.LastDir = System.IO.Path.GetDirectoryName(dlg.FileName);
				} else {
					// dialog closed by [Cancel] button
				}
			}
		}

		// Get [Source XML] file path
		private string GetSourcePath()
			=> MakeFullPath(textBoxSource.Text, Program.App.LastDir);

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
			using (SaveFileDialog dlg = new SaveFileDialog() ) {
				// dlg.Title = "Save as";
				dlg.InitialDirectory = Program.App.LastOut;
				dlg.Filter = Resources.ofd_out_filter;
				dlg.DefaultExt = "*.exe";
				dlg.OverwritePrompt=false;

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxOutput.Text = dlg.FileName;
					Program.App.LastOut = System.IO.Path.GetDirectoryName(dlg.FileName);
				} else {
					// dialog closed by [Cancel] button
				}
			}
		}

		// [Output] text box
		private void textBoxOutput_TextChanged(object sender, EventArgs e)
			=> Init_buttonStart();

		// Get [Output] file path
		private string GetOutputPath()
			=> MakeFullPath(textBoxOutput.Text, Program.App.LastOut);

		// Ensure path as full path
		private string MakeFullPath(string userInput, string basePath)
		{
			// does the userInput hold full path?
			if ( userInput.Contains(Path.DirectorySeparatorChar) ) return userInput;

			return Ext.JoinPath(basePath, userInput);
		}

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
		// Delta Encoding dropdown list combobox
		//------------------------------------------------------------
		private void InitDropdown()
		{
			// Bind dropdown list
			string[] keys = new string[dicEncoding.Count];
			dicEncoding.Keys.CopyTo(keys,0);
			comboBoxDeltaEncoding.DataSource = keys;

			// Select initial item
			comboBoxDeltaEncoding.SelectedIndex = 0;
		}

		private DeltaFormat GetDropdownValue() 
			=> dicEncoding[comboBoxDeltaEncoding.SelectedItem as string];

		// Set enability for [Delta Encoding Format]
		private void SetEnable_UI_DeltaEncoding(bool isEnabled)
			=> comboBoxDeltaEncoding.Enabled = isEnabled;

		// Set enability for [Clear log before start]
		private void SetEnable_UI_checkBoxClearLog(bool isEnabled)
			=> checkBoxClearLog.Enabled = isEnabled;

		// Set enability for UIs related to operation settings
		private void SetEnable_UIs_EncodingSettings(bool isEnabled)
		{
			SetEnable_UIs_filepath(isEnabled);
			SetEnable_UI_DeltaEncoding(isEnabled);
			SetEnable_UI_checkBoxClearLog(isEnabled);
		}

		//------------------------------------------------------------
		// Log text box
		//------------------------------------------------------------

		// Add text to log box 
		public void AddLogText(string text)
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
		// Query Message Boxes
		//------------------------------------------------------------

		/// <summary>Supply default extension if need</summary>
		/// <returns>(bool)Wheter the process can be started or not</returns>
		private bool SupplyExtension()
		{
			string ext = Path.GetExtension( textBoxOutput.Text );
			if ( ext.ToUpper() == @".EXE" ) return true;

			// Query message box
			DialogResult r = MessageBox.Show( 
										Resources.query_extension, 
										this.Text, 
										MessageBoxButtons.YesNoCancel,
										MessageBoxIcon.Question );
			if ( r == DialogResult.Cancel ) return false;
			if ( r == DialogResult.No ) return true;

			// Add default extension
			ext = textBoxOutput.Text[textBoxOutput.Text.Length-1] == '.'  ? "" : ".";
			ext += "exe";
			textBoxOutput.Text += ext;

			return true;
		}

		/// <summary>Confirm overwrite when the output file exists</summary>
		/// <returns>(bool)Wheter the process can be started or not</returns>
		private bool IsOverwrite()
		{
			if ( !File.Exists(textBoxOutput.Text) ) return true;

			// Query message box
			DialogResult r = MessageBox.Show( 
										Resources.query_overwrite, 
										this.Text, 
										MessageBoxButtons.OKCancel,
										MessageBoxIcon.Question );
			if ( r == DialogResult.OK ) return true;

			return false;
		}

		//------------------------------------------------------------
		// [Start] button
		//------------------------------------------------------------

		// Set the [Start] button availability
		private void Init_buttonStart()
			=> buttonStart.Enabled = CanStart();

		// Check if the [Start] button is avaiable
		private bool CanStart()
		{
			if ( textBoxSource.Text.Length <= 0 ) return false;
			if ( textBoxOutput.Text.Length <= 0 ) return false;
			return System.IO.File.Exists(textBoxSource.Text);
		}

		// [Start] button
		private async void buttonStart_Click(object sender, EventArgs e)
		{
			if ( m_processor == null )
			{
				// Supply default extension
				if ( !SupplyExtension() ) return;

				// Confirm overwrite when the output file exists
				if ( !IsOverwrite() ) return;

				// Disable [Start] button first
				DisableStartButton();

				// Clear logs if checkbox is checked
				if ( checkBoxClearLog.Checked ) textBoxLog.Text = "";

				try
				{
					using ( m_processor = new Processor(OperateProgress) )
					{
						( progressBar.Maximum, progressBar.Value ) = m_processor.Ready(
								GetSourcePath(),
								GetOutputPath(),
								GetDropdownValue()
							);

						// for "/ReportCmd=" commad line option
						// must before asynchronous things
						if ( Program.App.OptCmdReport != null )
						{
							using ( var sw = new StreamWriter(Program.App.OptCmdReport) )
							{
								sw.Write(m_processor.ReportBatch());
								sw.Close();
							}
						}

						// Start and await asyncronous processing
						SetupCancelButton();
						await m_processor.RunAsync(Program.App.OptSaveXML);
						AddLogText( "---Complete---" );
					}
				}
				catch (OperationCanceledException)
				{
					// The user intended to cancel processing.
					// So the exception is simply discarded.
					AddLogText( "---Cancelled---" );
				}
				catch (Error pe)
				{
					// Show reason why the processor has aborted.
					//AddLogText( "!!!Error!!!" );
					AddLogText( pe.ToString() );
					AddLogText( "---Aborted---" );
				}
				#if	!DEBUG
				catch ( FileNotFoundException fnfe )
				{
					// File does not exist
					AddLogText( fnfe.Message );
					//AddLogText( fnfe.FileName );
					AddLogText( "---Aborted---" );
				}
				#endif
				catch (Exception exc)
				{
					// Any other exception caught shows the full information of trace.
					AddLogText( exc.ToString() );
					AddLogText( "---Aborted---" );
				}
				finally
				{
					m_processor = null;
					ResetUIElements();
				}
			}
			else
			{
				//Debug.WriteLine( "[Cancel] button" );
				AddLogText( "Cancelling..." );
				buttonStart.Enabled = false;
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
		private void DisableStartButton()
		{
			SetEnable_UIs_EncodingSettings(false);
			buttonStart.Text = Resources.btntext_Cancel;
			buttonStart.Enabled = false;
		}

		// Reset UI elements
		private void SetupCancelButton()
		{
			SetEnable_UIs_EncodingSettings(false);
			buttonStart.Text = Resources.btntext_Cancel;
			buttonStart.Enabled = true;
		}

		// Reset UI elements
		private void ResetUIElements()
		{
			SetEnable_UIs_EncodingSettings(true);
			buttonStart.Text = Resources.btntext_Start;
			Init_buttonStart();
			textBoxSource.Text = "";
			progressBar.Value = 0;
		}

		//------------------------------------------------------------
		// Progress bar
		//------------------------------------------------------------

		// Reflecting progress in the UIs
		private void OperateProgress(ProgressState state)
		{
			//Debug.WriteLine( $"Usage = {state.Usage}" );
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
