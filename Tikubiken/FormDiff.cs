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
		private Tikubiken.MyApp myApp;
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
			// Application Manager
			myApp = new Tikubiken.MyApp();

			// Initialyze UIs
			InitDropdown();
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
		// [Source XML] text box and [File] button
		//------------------------------------------------------------

		// [File] button for Source XML
		private void buttonSource_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog()) {
				// dlg.Title = "Open File";
				dlg.InitialDirectory = myApp.LastDir;
				dlg.Filter = Resources.ofd_src_filter;
				dlg.DefaultExt = "*.xml";

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxSource.Text = dlg.FileName;
					myApp.LastDir = System.IO.Path.GetDirectoryName(dlg.FileName);
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
			using (SaveFileDialog dlg = new SaveFileDialog() ) {
				// dlg.Title = "Save as";
				dlg.InitialDirectory = myApp.LastOut;
				dlg.Filter = Resources.ofd_out_filter;
				dlg.DefaultExt = "*.exe";
				dlg.OverwritePrompt=false;

				if (dlg.ShowDialog() == DialogResult.OK) {
					// dialog closed by [OK] button
					textBoxOutput.Text = dlg.FileName;
					myApp.LastOut = System.IO.Path.GetDirectoryName(dlg.FileName);
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

		// Set enability for [Output]
		private void SetEnable_UI_DeltaEncoding(bool isEnabled)
			=> comboBoxDeltaEncoding.Enabled = isEnabled;

		// Set enability for UIs related to operation settings
		private void SetEnable_UIs_EncodingSettings(bool isEnabled)
		{
			SetEnable_UIs_filepath(isEnabled);
			SetEnable_UI_DeltaEncoding(isEnabled);
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
				// Overwrite prompt
				if ( File.Exists(textBoxOutput.Text) )
				{
				}

				// Disable [Start] button first
				DisableStartButton();

				try
				{
					using ( m_processor = new Processor(OperateProgress) )
					{
						( progressBar.Maximum, progressBar.Value ) = m_processor.Ready(
								textBoxSource.Text,
								textBoxOutput.Text,
								GetDropdownValue()
							);
						// for "/ReportCmd=" commad line option
						// must before asynchronous things
						if ( myApp.OptCmdReport != null )
						{
							using ( var sw = new StreamWriter(myApp.OptCmdReport) )
							{
								sw.Write(m_processor.ReportBatch());
								sw.Close();
							}
						}

						// Start and await asyncronous processing
						SetupCancelButton();
						await m_processor.RunAsync(myApp.OptSaveXML);
						AddLogText( "---Complete---" );
					}
				}
				catch (OperationCanceledException)
				{
					// The user intended to cancel processing.
					// So the exception is simply discarded.
					AddLogText( "---Cancelled---" );
				}
				catch (Processor.Error pe)
				{
					// Show reason why the processor has aborted.
					AddLogText( "!!!Error!!!" );
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
