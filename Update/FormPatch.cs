using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;

using Tikubiken.Properties;

namespace Tikubiken
{
	public partial class FormPatch : Form
	{
		//------------------------------------------------------------
		// Constants & Enums
		//------------------------------------------------------------

		public enum ButtonUsage
		{
			None,
			Start,
			Cancel,
			Exit,
			Processing,
		};

		//------------------------------------------------------------
		// Fields
		//------------------------------------------------------------
		private Processor		m_processor;
		private int				countMax;
		private int				countCurrent;
		private string			strMessage;
		private string			strState;
		private ButtonUsage		buttonUsage;

		//------------------------------------------------------------
		// Initalyzation & Cleaning up
		//------------------------------------------------------------
		public FormPatch()
		{
			InitializeComponent();
		}

		// Window is about to close
		private void FormDiff_FormClosing(object sender, FormClosingEventArgs e)
		{
			// If the button is assigned to processing, window cannot be closed
			if ( buttonUsage == ButtonUsage.Processing )
			{
				e.Cancel = true;
				return;
			}

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

		//------------------------------------------------------------
		// Form events
		//------------------------------------------------------------

		// Form initialization on load
		private void FormPatch_Load(object _sender, EventArgs _e)
		{
			// Set message box title
			URTException.MsgBoxTitle = this.Text;

			// XML Processor
			try
			{
				m_processor = new Processor(OperateProgress);

				// Search for the directory where the patch will be applied to
				// Run asynchronous operation, but not wait to complete
				// *** DO NOT await the returned Task instance(=nowait) ***
				Task nowait = m_processor.DetermineLocation();

				// Layout controls
				LayoutControls(m_processor.GetLayoutXML());

				// Set image by XML definition
				var image = m_processor.GetCoverImage();
				if ( image != null ) this.pictureBox.Image = image;

				// Set window caption by XML definition
				string str = m_processor.GetTitleCaption();
				if ( str != null ) URTException.MsgBoxTitle = this.Text = str;

				// Set UI labels by XML definition
				str = m_processor.GetLocalisedUI("progress");
				if (str != null) labelProgress.Text = str;
				SetupStartButton( ButtonUsage.Start, false );	// Window appears with disabled [Start] button
			}
			catch ( URTException urte )
			{
				urte.MsgBox();
				Unpacked.GetInstance().ErrorReport(urte.ToString(), "FormPatch_Load:User Runtime");
				this.Close();		// close window to exit application
				return;
			}
			catch
			{
				throw;
			}
		}

		// Layout controls by XML
		private void LayoutControls(IEnumerable<XElement> elmsLayout)
		{
			foreach(XElement elmLayout in elmsLayout)
			{
				var ctrl = this.Controls[elmLayout.Attribute("target")?.Value] ?? this;

				// <anchor>
				XElement elm = elmLayout.Element("anchor");
				if ( elm != null )
				{
					AnchorStyles style = AnchorStyles.None;
					foreach(string s in Enum.GetNames(typeof(AnchorStyles)) )
						if ( elm.Attribute("styles").Value.Contains(s) ) 
							style |= Enum.Parse<AnchorStyles>(s);
					ctrl.Anchor = style;
				}

				// <autosize>
				elm = elmLayout.Element("autosize");
				if ( elm != null )
				{
					ctrl.AutoSize = elm.Attribute("value").Value == "true";
				}

				// <location>
				elm = elmLayout.Element("location");
				if ( elm != null )
				{
					System.Drawing.Point location = default;
					location.X = int.Parse(elm.Attribute("x").Value);
					location.Y = int.Parse(elm.Attribute("y").Value);
					ctrl.Location = location;
				}

				// <size>
				elm = elmLayout.Element("size");
				if ( elm != null )
				{
					ctrl.Width  = int.Parse(elm.Attribute("width").Value);
					ctrl.Height = int.Parse(elm.Attribute("height").Value);
				}
			}
		}

		// Form is ready to accept user operation
		private void FormReady()
		{
			// Set title bar caption as detailed title text
			string str = m_processor.GetMessageTextByTypeName("title");
			if ( str != null ) this.Text = str;

			// Check if there is enough disk space
			long requiredSize = m_processor.GetPatchBalance();
			string root = m_processor.SourceDir.FullName;
			DriveInfo drive = new DriveInfo( root );
			if ( requiredSize > drive.TotalFreeSpace )
			{
				ShowXMLMessage("err_shortspace", $"{Resources.error_shortdiskspace}[{root}]");
				return;
			}
			// For using unpacked directory as working space, 
			// there is needed extra free space to store patch applied files
			root = Unpacked.GetInstance().PatchXML.DirectoryName;
			drive = new DriveInfo( root );
			if ( requiredSize > drive.TotalFreeSpace )
			{
				ShowXMLMessage("err_shortspace", $"{Resources.error_shortdiskspace}[{root}]");
				return;
			}

			// Enable UI and show message when ready
			SetupStartButton( ButtonUsage.Start, true );
			ShowXMLMessage("ready", Resources.msg_ready);
		}

		//------------------------------------------------------------
		// [Start] button
		//------------------------------------------------------------

		// Setup start button
		private void SetupStartButton(ButtonUsage usage, bool enabled)
		{
			buttonUsage = usage;

			// Set button label as cancel
			string str = m_processor.GetLocalisedUI(usage.ToString().ToLower());
			buttonStart.Text = str ?? usage switch {
													ButtonUsage.Start		=> Resources.button_start,
													ButtonUsage.Cancel		=> Resources.button_cancel,
													ButtonUsage.Exit		=> Resources.button_exit,
													ButtonUsage.Processing	=> Resources.button_processing,
													_	=> throw new URTException($"No such button usage type:{usage}")
												};
			buttonStart.Enabled = enabled;
		}

		// When [Start] button clicked
		private async void buttonStart_Click(object _sender, EventArgs _e)
		{
			switch ( buttonUsage )
			{
				case ButtonUsage.Start:
					try
					{
						// Initial synchronous operations
						ShowXMLMessage("process", Resources.msg_process);	// Set message a text indicates being in process

						// Assign button to cancel and set disabled to do some initial task synchronously
						SetupStartButton( ButtonUsage.Cancel, false );

						// As a result of initial task, set up progress bar parameters
						(countMax, progressBar.Maximum) = m_processor.Ready(Unpacked.GetInstance().PatchXML.Directory);
						progressBar.Minimum = 0;
						progressBar.Value   = 0;

						// [Cancel] button is now able to use
						buttonStart.Enabled = true;
/*
#if	DEBUG
						Unpacked.GetInstance().ErrorReport(m_processor.ReportBatch(), "Diagnostics");
#endif
*/
						// Runs asynchronous operations
						await m_processor.RunAsync();

						// Write result files to target directory asynchronously
						SetupStartButton( ButtonUsage.Processing, false );
						await m_processor.WriteResultAsync();

						// Operation complete
						ShowXMLMessage("complete", Resources.msg_complete );
					}
					catch (OperationCanceledException)
					{
						ShowXMLMessage("cancel", Resources.msg_cancel );
					}
					catch ( Exception e )
					{
						Unpacked.GetInstance().ErrorReport(e.ToString(), "Start button:catch all");
						ShowXMLMessage("error", Resources.error_abort);
					}
					finally
					{
						SetupStartButton( ButtonUsage.Exit, true );
					}
					break;
				case ButtonUsage.Cancel:
					CancelOperation();
					break;
				case ButtonUsage.Exit:
					this.Close();		// close window to exit application
					break;
			}
		}

		// Operation to cancel async task
		private void CancelOperation()
		{
			// Cancel only when cancellation token exists.
			if ( m_processor == null ) return;
			m_processor.Cancel();
		}

		//------------------------------------------------------------
		// Progress bar
		//------------------------------------------------------------

		// Reflecting progress in the UIs
		private void OperateProgress(ProgressState state)
		{
			if ( state.IsValueAvailable() ) progressBar.Value = state.Value;
			if ( state.IsTextAvailable() ) ShowMessage( state.Text );
			if ( state.IsCountAvailable() ) UpdateMessageCount( state.Count );
			if ( state.HasCompletedSuccess() ) FormReady();
			if ( state.HasFailed() ) FailedToInitialLocate();
		}

		// Failed to DetermineLocation()
		private void FailedToInitialLocate()
		{
			if ( m_processor.IsNewestFound )
			{
				// If the newest version has been found, tell the user there's no need to update
				ShowXMLMessage("err_newest", Resources.error_newest);
			}
			else
			{
				// If any versions specified in XML are not found, notify it
				ShowXMLMessage("err_misplace", Resources.error_misplace);
			}

			// Show exit button to close application
			SetupStartButton( ButtonUsage.Exit, true );
		}

		//------------------------------------------------------------
		// Message Label
		//------------------------------------------------------------

		private void ShowMessage(string msg)
		{
			if ( !string.IsNullOrEmpty(msg) ) strMessage = msg;
			msg = 	strMessage + System.Environment.NewLine +
					(string.IsNullOrEmpty(strState) ? "" : strState) + System.Environment.NewLine +
					"";
			labelMessage.Text = msg;
		}

		private void UpdateMessageCount( int count )
		{
			countCurrent = count;
			strState = $"({countCurrent}/{countMax})";
			ShowMessage(null);
		}

		/// <summary> Show message defined in XML</summary>
		/// <param name="typeAttr">
		///	(string)Type name specified in "type" attribute of &lt;text&gt; element.
		///	</param>
		private void ShowXMLMessage(string typeAttr, string strDefault)
			=> ShowMessage( m_processor.GetMessageTextByTypeName(typeAttr) ?? strDefault );

		//------------------------------------------------------------
		// UI statuses
		//------------------------------------------------------------


	}	// ---- public partial class FormPatch : Form ----------------
}
