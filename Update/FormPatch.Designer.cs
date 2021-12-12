
namespace Tikubiken
{
    partial class FormPatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPatch));
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonStart = new System.Windows.Forms.Button();
            this.labelProgress = new System.Windows.Forms.Label();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.labelMessage = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            // 
            // buttonStart
            // 
            resources.ApplyResources(this.buttonStart, "buttonStart");
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // labelProgress
            // 
            resources.ApplyResources(this.labelProgress, "labelProgress");
            this.labelProgress.Name = "labelProgress";
            // 
            // pictureBox
            // 
            resources.ApplyResources(this.pictureBox, "pictureBox");
            this.pictureBox.Image = global::Tikubiken.Properties.Resources.cover_update;
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.TabStop = false;
            // 
            // labelMessage
            // 
            resources.ApplyResources(this.labelMessage, "labelMessage");
            this.labelMessage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelMessage.Name = "labelMessage";
            // 
            // FormPatch
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelMessage);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormPatch";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormDiff_FormClosing);
            this.Load += new System.EventHandler(this.FormPatch_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label labelMessage;
    }
}

