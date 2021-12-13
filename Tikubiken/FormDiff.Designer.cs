
namespace Tikubiken
{
    partial class FormDiff
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDiff));
            this.labelSource = new System.Windows.Forms.Label();
            this.textBoxSource = new System.Windows.Forms.TextBox();
            this.buttonSource = new System.Windows.Forms.Button();
            this.labelOutput = new System.Windows.Forms.Label();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.buttonOutput = new System.Windows.Forms.Button();
            this.labelProgress = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.labelDeltaEncoding = new System.Windows.Forms.Label();
            this.comboBoxDeltaEncoding = new System.Windows.Forms.ComboBox();
            this.checkBoxClearLog = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // labelSource
            // 
            resources.ApplyResources(this.labelSource, "labelSource");
            this.labelSource.Name = "labelSource";
            // 
            // textBoxSource
            // 
            resources.ApplyResources(this.textBoxSource, "textBoxSource");
            this.textBoxSource.Name = "textBoxSource";
            this.textBoxSource.TextChanged += new System.EventHandler(this.textBoxSource_TextChanged);
            // 
            // buttonSource
            // 
            resources.ApplyResources(this.buttonSource, "buttonSource");
            this.buttonSource.Name = "buttonSource";
            this.buttonSource.UseVisualStyleBackColor = true;
            this.buttonSource.Click += new System.EventHandler(this.buttonSource_Click);
            // 
            // labelOutput
            // 
            resources.ApplyResources(this.labelOutput, "labelOutput");
            this.labelOutput.Name = "labelOutput";
            // 
            // textBoxOutput
            // 
            resources.ApplyResources(this.textBoxOutput, "textBoxOutput");
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.TextChanged += new System.EventHandler(this.textBoxOutput_TextChanged);
            // 
            // buttonOutput
            // 
            resources.ApplyResources(this.buttonOutput, "buttonOutput");
            this.buttonOutput.Name = "buttonOutput";
            this.buttonOutput.UseVisualStyleBackColor = true;
            this.buttonOutput.Click += new System.EventHandler(this.buttonOutput_Click);
            // 
            // labelProgress
            // 
            resources.ApplyResources(this.labelProgress, "labelProgress");
            this.labelProgress.Name = "labelProgress";
            // 
            // buttonStart
            // 
            resources.ApplyResources(this.buttonStart, "buttonStart");
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            // 
            // textBoxLog
            // 
            resources.ApplyResources(this.textBoxLog, "textBoxLog");
            this.textBoxLog.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            // 
            // labelDeltaEncoding
            // 
            resources.ApplyResources(this.labelDeltaEncoding, "labelDeltaEncoding");
            this.labelDeltaEncoding.Name = "labelDeltaEncoding";
            // 
            // comboBoxDeltaEncoding
            // 
            resources.ApplyResources(this.comboBoxDeltaEncoding, "comboBoxDeltaEncoding");
            this.comboBoxDeltaEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDeltaEncoding.FormattingEnabled = true;
            this.comboBoxDeltaEncoding.Name = "comboBoxDeltaEncoding";
            // 
            // checkBoxClearLog
            // 
            resources.ApplyResources(this.checkBoxClearLog, "checkBoxClearLog");
            this.checkBoxClearLog.Checked = true;
            this.checkBoxClearLog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxClearLog.Name = "checkBoxClearLog";
            this.checkBoxClearLog.UseVisualStyleBackColor = true;
            // 
            // FormDiff
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBoxClearLog);
            this.Controls.Add(this.comboBoxDeltaEncoding);
            this.Controls.Add(this.labelDeltaEncoding);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buttonOutput);
            this.Controls.Add(this.textBoxOutput);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.buttonSource);
            this.Controls.Add(this.textBoxSource);
            this.Controls.Add(this.labelSource);
            this.Name = "FormDiff";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormDiff_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormDiff_FormClosed);
            this.Load += new System.EventHandler(this.FormDiff_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSource;
        private System.Windows.Forms.TextBox textBoxSource;
        private System.Windows.Forms.Button buttonSource;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.Button buttonOutput;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.Label labelDeltaEncoding;
        private System.Windows.Forms.ComboBox comboBoxDeltaEncoding;
        private System.Windows.Forms.CheckBox checkBoxClearLog;
    }
}

